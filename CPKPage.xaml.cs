using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPK_Calculate
{
    public sealed partial class CPKPage : Page
    {
        public CPKPage()
        {
            this.InitializeComponent();
        }

        private void DataInputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDataCount();
        }

        private void UpdateDataCount()
        {
            DataCountTxt.Text = GetNumbersFromInput().Count.ToString();
        }

        private List<double> GetNumbersFromInput()
        {
            if (string.IsNullOrWhiteSpace(DataInputBox.Text)) return new List<double>();
            return DataInputBox.Text
                .Split(new[] { '\r', '\n', ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.TryParse(s, out double n) ? n : (double?)null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList();
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var numbers = GetNumbersFromInput();
                if (numbers.Count < 2) return;

                // ดึงค่าจาก UI
                var res = CPKEngine.Calculate(numbers, LSLInput.Value, USLInput.Value, (int)SizeInput.Value);

                // อัปเดตฝั่ง WITHIN
                StDevWTxt.Text = res.StdevWithin.ToString("F3");
                CpTxt.Text = res.Cp.ToString("F2");
                CpkTxt.Text = res.Cpk.ToString("F2");
                PpmWTxt.Text = res.PpmWithin.ToString("F2");
                BarCpk.Value = res.Cpk;
                BarCpkVal.Text = res.Cpk.ToString("F2");

                // อัปเดตฝั่ง OVERALL
                StDevOTxt.Text = res.StdevOverall.ToString("F3");
                PpTxt.Text = res.Pp.ToString("F2");
                PpkTxt.Text = res.Ppk.ToString("F2");
                CpmTxt.Text = res.Cpm.ToString("F2");
                PpmOTxt.Text = res.PpmOverall.ToString("F2");
                BarPpk.Value = res.Ppk;
                BarPpkVal.Text = res.Ppk.ToString("F2");

                SummaryTxt.Text = $"Mean: {res.Mean:F3}";

                // แปลผลคุณภาพ
                UpdateInterpretation(res.Cpk, CpkStatusTxt, CpkStatusBorder);
                UpdateInterpretation(res.Ppk, PpkStatusTxt, PpkStatusBorder);
            }
            catch (Exception ex)
            {
                ShowError("เกิดข้อผิดพลาด: " + ex.Message);
            }
        }

        private void UpdateInterpretation(double val, TextBlock statusLabel, Border statusBorder)
        {
            if (val < 1.00)
            {
                statusLabel.Text = "INADEQUATE";
                statusLabel.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                statusBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 0, 0));
            }
            else if (val <= 1.33)
            {
                statusLabel.Text = "MARGINAL";
                statusLabel.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
                statusBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 165, 0));
            }
            else
            {
                statusLabel.Text = val > 1.67 ? "EXCELLENT" : "SATISFACTORY";
                var successBrush = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                statusLabel.Foreground = successBrush;
                statusBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 0, 255, 0));
            }
        }

        private async void ShowError(string message)
        {
            if (this.XamlRoot == null) return;
            ContentDialog dialog = new ContentDialog { Title = "แจ้งเตือน", Content = message, CloseButtonText = "ตกลง", XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
        }
    }
}