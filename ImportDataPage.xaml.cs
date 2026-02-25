using ExcelDataReader;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CPK_Calculate
{
    public class ImportRow
    {
        public int Index { get; set; }
        public string Ratio { get; set; } = string.Empty;
        public DateTime? ProductionDate { get; set; }
        public string ProductionDateText => ProductionDate?.ToString("dd-MMM-yyyy") ?? "-";
        public string LotNo { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public sealed partial class ImportDataPage : Page
    {
        private readonly ObservableCollection<ImportRow> _previewRows = new();
        private readonly List<double> _values = new();
        private StorageFile? _currentFile;
        private static bool _encodingRegistered;

        public ImportDataPage()
        {
            this.InitializeComponent();
            PreviewListView.ItemsSource = _previewRows;

            if (!_encodingRegistered)
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                _encodingRegistered = true;
            }
        }

        private async void PickFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".xlsx");
            picker.FileTypeFilter.Add(".xls");

            var hwnd = WindowNative.GetWindowHandle(MainWindow.Current);
            InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile? file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            await LoadExcelAsync(file);
        }

        private async Task LoadExcelAsync(StorageFile file)
        {
            try
            {
                BusyOverlay.Visibility = Visibility.Visible;
                _values.Clear();
                _previewRows.Clear();

                using Stream stream = await file.OpenStreamForReadAsync();
                using IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream);

                var rows = new List<ImportRow>();
                do
                {
                    bool headerSkipped = false;
                    while (reader.Read())
                    {
                        if (!headerSkipped)
                        {
                            headerSkipped = true;
                            continue;
                        }

                        if (IsEmptyRow(reader))
                        {
                            continue;
                        }

                        string ratio = reader.GetValue(0)?.ToString()?.Trim() ?? string.Empty;
                        double? value = ReadDouble(reader.GetValue(4));
                        if (string.IsNullOrWhiteSpace(ratio) || value == null)
                        {
                            continue;
                        }

                        var row = new ImportRow
                        {
                            Ratio = ratio,
                            ProductionDate = ReadDate(reader.GetValue(1)),
                            LotNo = reader.GetValue(2)?.ToString()?.Trim() ?? string.Empty,
                            Value = value.Value
                        };

                        rows.Add(row);
                    }
                }
                while (reader.NextResult());

                if (rows.Count == 0)
                {
                    ShowStatus("Import Failed", "ไม่พบข้อมูลที่สามารถอ่านได้จากไฟล์นี้", InfoBarSeverity.Error);
                    ResetState();
                    return;
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    rows[i].Index = i + 1;
                }

                foreach (var row in rows.Take(200))
                {
                    _previewRows.Add(row);
                }

                _values.AddRange(rows.Select(r => r.Value));
                RowCountText.Text = $"{rows.Count} rows";
                FileInfoText.Text = file.Name;
                _currentFile = file;

                SubgroupBox.Maximum = rows.Count;
                if (double.IsNaN(SubgroupBox.Value) || SubgroupBox.Value <= 0)
                {
                    SubgroupBox.Value = Math.Min(25, rows.Count);
                }
                else
                {
                    SubgroupBox.Value = Math.Min(SubgroupBox.Value, rows.Count);
                }

                ShowStatus("Loaded", $"ดึงข้อมูลสำเร็จ {rows.Count} แถว", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatus("Import Failed", ex.Message, InfoBarSeverity.Error);
                ResetState();
            }
            finally
            {
                BusyOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private static bool IsEmptyRow(IExcelDataReader reader)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetValue(i) != null && !string.IsNullOrWhiteSpace(reader.GetValue(i)?.ToString()))
                {
                    return false;
                }
            }
            return true;
        }

        private static DateTime? ReadDate(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is DateTime dt)
            {
                return dt;
            }

            if (value is double oaDate)
            {
                return DateTime.FromOADate(oaDate);
            }

            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static double? ReadDouble(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is double d)
            {
                return d;
            }

            if (value is int i)
            {
                return i;
            }

            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }

            return null;
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_values.Count == 0)
            {
                ShowStatus("Missing Data", "กรุณาเลือกไฟล์ Excel ก่อน", InfoBarSeverity.Warning);
                return;
            }

            string title = TitleBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                ShowStatus("Validation", "กรุณากรอกชื่อรายการ", InfoBarSeverity.Warning);
                return;
            }

            double? lsl = ReadNumberBox(LslBox);
            double? usl = ReadNumberBox(UslBox);
            if (lsl == null || usl == null || usl <= lsl)
            {
                ShowStatus("Validation", "USL ต้องมากกว่า LSL", InfoBarSeverity.Warning);
                return;
            }

            int subgroup = (int)(double.IsNaN(SubgroupBox.Value) ? _values.Count : SubgroupBox.Value);
            subgroup = Math.Clamp(subgroup, 1, _values.Count);

            var request = new CpkAnalysisCreateRequest
            {
                Title = title,
                Lsl = lsl.Value,
                Usl = usl.Value,
                SubgroupSize = subgroup,
                DataPoints = _values.ToList(),
                RecordedBy = RecordedByBox.Text?.Trim() ?? string.Empty,
                Note = NoteBox.Text?.Trim() ?? string.Empty
            };

            try
            {
                BusyOverlay.Visibility = Visibility.Visible;
                var response = await CpkApiService.CreateAsync(request);
                string message = response != null ? $"บันทึกข้อมูลแล้ว (ID: {response.Id})" : "บันทึกข้อมูลสำเร็จ";
                ShowStatus("Success", message, InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatus("Import Failed", ex.Message, InfoBarSeverity.Error);
            }
            finally
            {
                BusyOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private static double? ReadNumberBox(NumberBox box)
        {
            if (box == null)
            {
                return null;
            }

            if (double.IsNaN(box.Value))
            {
                return null;
            }

            return box.Value;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _values.Clear();
            _previewRows.Clear();
            TitleBox.Text = string.Empty;
            NoteBox.Text = string.Empty;
            RecordedByBox.Text = string.Empty;
            LslBox.Value = double.NaN;
            UslBox.Value = double.NaN;
            SubgroupBox.Value = 5;
            RowCountText.Text = "0 rows";
            FileInfoText.Text = "No file selected";
            _currentFile = null;
            StatusInfoBar.IsOpen = false;
        }

        private void ResetState()
        {
            _values.Clear();
            _previewRows.Clear();
            RowCountText.Text = "0 rows";
            FileInfoText.Text = "No file selected";
            _currentFile = null;
        }

        private void ShowStatus(string title, string message, InfoBarSeverity severity)
        {
            StatusInfoBar.Title = title;
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.IsOpen = true;
        }
    }
}
