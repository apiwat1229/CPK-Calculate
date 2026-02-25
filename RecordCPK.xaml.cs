using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CPK_Calculate
{
    public sealed partial class RecordCPK : Page
    {
        public RecordCPK()
        {
            this.InitializeComponent();
            DateInput.Date = DateTimeOffset.Now;
        }

        private ExcelSheetParameters? BuildParameters()
        {
            var selectedGradeItem = GradeInput.SelectedItem as ComboBoxItem;
            string grade = selectedGradeItem?.Content?.ToString() ?? "";

            if (string.IsNullOrEmpty(grade))
            {
                ShowStatus("Validation Error", "Please select a Grade.", InfoBarSeverity.Error);
                return null;
            }

            return new ExcelSheetParameters
            {
                Date = DateInput.Date,
                Grade = grade
            };
        }

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            var parameters = BuildParameters();
            if (parameters != null)
            {
                this.Frame.Navigate(typeof(ExcelDataEntryPage), parameters);
            }
        }

        private void ShowStatus(string title, string message, InfoBarSeverity severity)
        {
            StatusInfoBar.Title = title;
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.IsOpen = true;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetFields();
        }

        private void ResetFields()
        {
            DateInput.Date = DateTimeOffset.Now;
            GradeInput.SelectedIndex = -1;
            StatusInfoBar.IsOpen = false;
        }
    }
}