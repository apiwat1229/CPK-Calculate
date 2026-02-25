using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CPK_Calculate
{
    public class HistoryRowItem
    {
        public int RowNumber { get; set; }
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string DateText { get; set; } = "";
        public string TimeText { get; set; } = "";
    }

    public sealed partial class HistoryPage : Page
    {
        private readonly ObservableCollection<HistoryRowItem> _items = new();

        public HistoryPage()
        {
            this.InitializeComponent();
            HistoryListView.ItemsSource = _items;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadData();
        }

        private async void LoadData()
        {
            ShowState("loading");

            try
            {
                var analyses = await CpkApiService.GetAllAsync();

                _items.Clear();

                if (analyses.Count == 0)
                {
                    RecordCountTxt.Text = "0 records";
                    ShowState("empty");
                    return;
                }

                int row = 1;
                foreach (var a in analyses)
                {
                    string dateText = "-";
                    string timeText = "-";

                    if (DateTimeOffset.TryParse(a.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        dateText = dt.LocalDateTime.ToString("dd-MMM-yyyy");
                        timeText = dt.LocalDateTime.ToString("HH:mm");
                    }

                    _items.Add(new HistoryRowItem
                    {
                        RowNumber = row++,
                        Id = a.Id,
                        Title = string.IsNullOrWhiteSpace(a.Title) ? "(Untitled)" : a.Title,
                        DateText = dateText,
                        TimeText = timeText
                    });
                }

                RecordCountTxt.Text = $"{_items.Count} records";
                ShowState("data");
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Unable to connect to server.\n{ex.Message}";
                ShowState("error");
            }
        }

        private void ShowState(string state)
        {
            LoadingPanel.Visibility = state == "loading" ? Visibility.Visible : Visibility.Collapsed;
            ErrorPanel.Visibility = state == "error" ? Visibility.Visible : Visibility.Collapsed;
            EmptyPanel.Visibility = state == "empty" ? Visibility.Visible : Visibility.Collapsed;
            DataPanel.Visibility = state == "data" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string id) return;

            var item = _items.FirstOrDefault(i => i.Id == id);
            string title = item?.Title ?? id;

            var dialog = new ContentDialog
            {
                Title = "Confirm Delete",
                Content = $"Delete \"{title}\"?\nThis action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                await CpkApiService.DeleteAsync(id);
                LoadData();
            }
            catch (Exception ex)
            {
                var errDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to delete: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errDialog.ShowAsync();
            }
        }

        private async void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not HistoryRowItem row) return;

            DetailLoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                var detail = await CpkApiService.GetByIdAsync(row.Id);
                if (detail == null || detail.DataPoints.Count < 2)
                {
                    DetailLoadingOverlay.Visibility = Visibility.Collapsed;
                    var errDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = "No data points found in this analysis record.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errDialog.ShowAsync();
                    return;
                }

                var results = CPKEngine.Calculate(detail.DataPoints, detail.Lsl, detail.Usl, detail.SubgroupSize);

                string dateStr = "";
                if (DateTimeOffset.TryParse(detail.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    dateStr = dt.LocalDateTime.ToString("dd-MMM-yyyy");
                }

                var resultData = new CPKResultData
                {
                    Values = detail.DataPoints,
                    LSL = detail.Lsl,
                    USL = detail.Usl,
                    SubgroupSize = detail.SubgroupSize,
                    Results = results,
                    Title = detail.Title,
                    Date = dateStr
                };

                var resultWindow = new CPKResultWindow();
                resultWindow.LoadData(resultData);
                resultWindow.Activate();
            }
            catch (Exception ex)
            {
                var errDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to load analysis detail:\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errDialog.ShowAsync();
            }
            finally
            {
                DetailLoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
    }
}
