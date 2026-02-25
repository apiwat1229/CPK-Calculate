using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPK_Calculate
{
    // ชื่อคลาสต้องเป็น CPKPage และต้องมี partial เพื่อเชื่อมกับ XAML
    public sealed partial class CPKPage : Page
    {
        public CPKPage()
        {
            this.InitializeComponent(); // ถ้าชื่อคลาสตรงกัน Error นี้จะหายไปเอง
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
                if (numbers.Count < 2)
                {
                    ShowError("Please input at least 2 numbers.");
                    return;
                }

                // เรียกใช้ Engine (ต้องมั่นใจว่ามีคลาส CPKEngine อยู่ในโปรเจกต์)
                var res = CPKEngine.Calculate(numbers, LSLInput.Value, USLInput.Value, (int)SizeInput.Value);

                // Update UI Within
                StDevWTxt.Text = res.StdevWithin.ToString("F3");
                CpTxt.Text = res.Cp.ToString("F2");
                CpkTxt.Text = res.Cpk.ToString("F2");
                PpmWTxt.Text = res.PpmWithin.ToString("F2");
                BarCpk.Value = Math.Min(res.Cpk, 2.0); // ป้องกัน Bar เกิน Max
                BarCpkVal.Text = res.Cpk.ToString("F2");

                // Update UI Overall
                StDevOTxt.Text = res.StdevOverall.ToString("F3");
                PpTxt.Text = res.Pp.ToString("F2");
                PpkTxt.Text = res.Ppk.ToString("F2");
                CpmTxt.Text = res.Cpm.ToString("F2");
                PpmOTxt.Text = res.PpmOverall.ToString("F2");
                BarPpk.Value = Math.Min(res.Ppk, 2.0);
                BarPpkVal.Text = res.Ppk.ToString("F2");

                SummaryTxt.Text = $"Mean: {res.Mean:F3}";

                UpdateInterpretation(res.Cpk, CpkStatusTxt, CpkStatusBorder);
                UpdateInterpretation(res.Ppk, PpkStatusTxt, PpkStatusBorder);
            }
            catch (Exception ex)
            {
                ShowError("Error: " + ex.Message);
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
                statusLabel.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
                statusBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 0, 255, 0));
            }
        }

        private async void ShowError(string message)
        {
            if (this.XamlRoot == null) return;
            ContentDialog dialog = new ContentDialog
            {
                Title = "Notification",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}