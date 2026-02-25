using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CPK_Calculate
{
    public class HistoryRowItem
    {
        public int RowNumber { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public string TimeText { get; set; } = string.Empty;
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
            _items.Clear();

            try
            {
                var analyses = await CpkApiService.GetAllAsync();

                if (analyses == null || !analyses.Any())
                {
                    RecordCountTxt.Text = "0 records";
                    ShowState("empty");
                    return;
                }

                int row = 1;
                foreach (var a in analyses.OrderByDescending(x => x.CreatedAt))
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
                        Id = a.Id ?? string.Empty,
                        Title = string.IsNullOrWhiteSpace(a.Title) ? "Untitled Analysis" : a.Title,
                        DateText = dateText,
                        TimeText = timeText
                    });
                }

                RecordCountTxt.Text = $"{_items.Count} records";
                ShowState("data");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load Error: {ex.Message}");
                ShowState("empty");
            }
        }

        private void ShowState(string state)
        {
            LoadingPanel.Visibility = state == "loading" ? Visibility.Visible : Visibility.Collapsed;
            EmptyPanel.Visibility = state == "empty" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string id }) return;

            var dialog = new ContentDialog
            {
                Title = "Confirm Delete",
                Content = "Are you sure you want to delete this analysis record?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                try
                {
                    await CpkApiService.DeleteAsync(id);
                    LoadData();
                }
                catch { /* Handle error */ }
            }
        }

        private async void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not HistoryRowItem row) return;

            DetailLoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var detail = await CpkApiService.GetByIdAsync(row.Id);
                if (detail != null && detail.DataPoints != null)
                {
                    var results = CPKEngine.Calculate(detail.DataPoints, detail.Lsl, detail.Usl, detail.SubgroupSize);

                    var resultData = new CPKResultData
                    {
                        Values = detail.DataPoints,
                        LSL = detail.Lsl,
                        USL = detail.Usl,
                        SubgroupSize = detail.SubgroupSize,
                        Results = results,
                        Title = detail.Title ?? "Result",
                        Date = row.DateText
                    };

                    var resultWindow = new CPKResultWindow();
                    resultWindow.LoadData(resultData);
                    resultWindow.Activate();
                }
            }
            catch { /* Handle error */ }
            finally
            {
                DetailLoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
    }
}