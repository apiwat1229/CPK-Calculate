using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CPK_Calculate
{
    // Model สำหรับแต่ละแถวในตาราง
    public class MeasurementRow : INotifyPropertyChanged
    {
        public string Ratio { get; set; } = "";
        public string ProdDate { get; set; } = "";
        public string LotNo { get; set; } = "";
        public int SampleNo { get; set; }

        private string _valueText = "";
        public string ValueText
        {
            get => _valueText;
            set
            {
                if (_valueText != value)
                {
                    _valueText = value;
                    OnPropertyChanged();
                }
            }
        }

        public double? Value
        {
            get => double.TryParse(_valueText, out var v) ? v : null;
            set => ValueText = value?.ToString() ?? "";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed partial class ExcelDataEntryPage : Page
    {
        private ExcelSheetParameters? _parameters;
        private ObservableCollection<MeasurementRow> _tableData = new();
        private bool _updatingRatio;

        public ExcelDataEntryPage()
        {
            this.InitializeComponent();
            MeasurementGrid.ItemsSource = _tableData;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ExcelSheetParameters p)
            {
                _parameters = p;
                UpdateHeaderUI(p);
            }
        }

        private void UpdateHeaderUI(ExcelSheetParameters p)
        {
            PageTitle.Text = $"{p.Grade} Analysis Sheet";
            InfoDateTxt.Value = p.Date?.ToString("dd-MMM-yyyy") ?? "-";
            InfoGradeTxt.Value = p.Grade;
        }

        private void Ratio_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (RatioSumTxt == null || _updatingRatio) return;
            _updatingRatio = true;

            int cl = double.IsNaN(RatioCL.Value) ? 0 : (int)RatioCL.Value;
            int uss = double.IsNaN(RatioUSS.Value) ? 0 : (int)RatioUSS.Value;
            int bk = double.IsNaN(RatioBK.Value) ? 0 : (int)RatioBK.Value;
            int total = cl + uss + bk;
            int remaining = Math.Max(0, 100 - total);

            RatioCL.Maximum = cl + remaining;
            RatioUSS.Maximum = uss + remaining;
            RatioBK.Maximum = bk + remaining;

            RatioSumTxt.Text = $"Total: {total} / 100";
            RatioSumTxt.Foreground = total >= 100
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green)
                : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

            _updatingRatio = false;
        }

        private int GetRatioTotal()
        {
            int cl = double.IsNaN(RatioCL.Value) ? 0 : (int)RatioCL.Value;
            int uss = double.IsNaN(RatioUSS.Value) ? 0 : (int)RatioUSS.Value;
            int bk = double.IsNaN(RatioBK.Value) ? 0 : (int)RatioBK.Value;
            return cl + uss + bk;
        }

        private void AddRows_Click(object sender, RoutedEventArgs e)
        {
            if (_parameters == null) return;

            int cl = double.IsNaN(RatioCL.Value) ? 0 : (int)RatioCL.Value;
            int uss = double.IsNaN(RatioUSS.Value) ? 0 : (int)RatioUSS.Value;
            int bk = double.IsNaN(RatioBK.Value) ? 0 : (int)RatioBK.Value;

            if (cl + uss + bk > 100)
            {
                ShowError("Ratio total must not exceed 100.");
                return;
            }

            string ratio = $"USS{uss} : CL{cl} : BK{bk}";
            string lotNo = LotNumberInput.Text ?? "";
            int count = double.IsNaN(SampleCountInput.Value) ? 0 : (int)SampleCountInput.Value;

            if (count <= 0) return;

            int startNo = _tableData.Count + 1;
            for (int i = 0; i < count; i++)
            {
                _tableData.Add(new MeasurementRow
                {
                    Ratio = ratio,
                    ProdDate = _parameters.Date?.ToString("dd-MMM-yyyy") ?? "",
                    LotNo = lotNo,
                    SampleNo = startNo + i,
                    Value = null
                });
            }
            UpdateCount();
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            var values = _tableData
                .Where(r => r.Value.HasValue)
                .Select(r => r.Value!.Value)
                .ToList();

            if (values.Count < 2)
            {
                ShowError("Please enter at least 2 measurement values.");
                return;
            }

            var res = CPKEngine.Calculate(values, 0, 0, values.Count);
            SummaryTxt.Text = $"Mean: {res.Mean:F3} | CPK: {res.Cpk:F2}";
            UpdateCount();
        }

        private void UpdateCount()
        {
            int filledCount = _tableData.Count(r => r.Value.HasValue);
            DataCountTxt.Text = $"Count: {filledCount} / {_tableData.Count}";
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in _tableData) row.Value = null;
            UpdateCount();
            SummaryTxt.Text = "Mean: 0.00";
        }

        private void LotNumberInput_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = !string.IsNullOrEmpty(args.NewText) && !args.NewText.All(char.IsDigit);
        }

        private async void ShowError(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    public class InfoLabel : StackPanel
    {
        private readonly TextBlock _valueTxt = new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
        private readonly TextBlock _labelTxt = new TextBlock { Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"], VerticalAlignment = VerticalAlignment.Center };
        private readonly FontIcon _icon = new FontIcon { FontSize = 14, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] };

        public InfoLabel()
        {
            this.Orientation = Orientation.Horizontal;
            this.Spacing = 6;
            this.Children.Add(_icon);
            this.Children.Add(_labelTxt);
            this.Children.Add(_valueTxt);
        }

        public string Icon { set => _icon.Glyph = value; }
        public string Label { set => _labelTxt.Text = value; }
        public string Value { get => _valueTxt.Text; set => _valueTxt.Text = value; }
    }
}