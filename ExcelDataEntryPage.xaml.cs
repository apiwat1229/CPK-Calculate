using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.System;

namespace CPK_Calculate
{
    // Model สำหรับแต่ละแถวในตาราง
    public class MeasurementRow : INotifyPropertyChanged
    {
        public string Ratio { get; set; } = "";
        public string ProdDate { get; set; } = "";
        public string LotNo { get; set; } = "";

        private int _sampleNo;
        public int SampleNo
        {
            get => _sampleNo;
            set
            {
                if (_sampleNo != value)
                {
                    _sampleNo = value;
                    OnPropertyChanged();
                }
            }
        }

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

        // Undo/Redo stacks: each entry is a batch of rows added
        private readonly Stack<List<MeasurementRow>> _undoStack = new();
        private readonly Stack<List<MeasurementRow>> _redoStack = new();

        public ExcelDataEntryPage()
        {
            this.InitializeComponent();
            MeasurementGrid.ItemsSource = _tableData;
            this.KeyboardAccelerators.Add(BuildAccelerator(Windows.System.VirtualKey.Z, UndoAccelerator_Invoked));
            this.KeyboardAccelerators.Add(BuildAccelerator(Windows.System.VirtualKey.Y, RedoAccelerator_Invoked));
        }

        private static Microsoft.UI.Xaml.Input.KeyboardAccelerator BuildAccelerator(
            Windows.System.VirtualKey key,
            Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
        {
            var accel = new Microsoft.UI.Xaml.Input.KeyboardAccelerator
            {
                Key = key,
                Modifiers = Windows.System.VirtualKeyModifiers.Control
            };
            accel.Invoked += handler;
            return accel;
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

            // Force NaN (empty) → 0
            if (double.IsNaN(RatioCL.Value)) RatioCL.Value = 0;
            if (double.IsNaN(RatioUSS.Value)) RatioUSS.Value = 0;
            if (double.IsNaN(RatioBK.Value)) RatioBK.Value = 0;

            int cl = (int)RatioCL.Value;
            int uss = (int)RatioUSS.Value;
            int bk = (int)RatioBK.Value;
            int total = cl + uss + bk;
            int remaining = Math.Max(0, 100 - total);

            RatioCL.Maximum = cl + remaining;
            RatioUSS.Maximum = uss + remaining;
            RatioBK.Maximum = bk + remaining;

            RatioSumTxt.Text = $"Total: {total} / 100";
            if (total == 100)
            {
                RatioSumTxt.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
            }
            else if (total > 100)
            {
                RatioSumTxt.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            else
            {
                RatioSumTxt.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
            }

            _updatingRatio = false;
        }

        private void RatioBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is NumberBox nb && double.IsNaN(nb.Value))
                nb.Value = 0;
        }

        private void RatioBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is NumberBox nb)
            {
                var innerTextBox = FindChild<TextBox>(nb);
                if (innerTextBox != null)
                {
                    innerTextBox.BeforeTextChanging += (s, args) =>
                    {
                        args.Cancel = !string.IsNullOrEmpty(args.NewText) && !args.NewText.All(char.IsDigit);
                    };
                }
            }
        }

        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private void RatioBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter) return;
            e.Handled = true;

            if (sender == RatioCL)
                RatioUSS.Focus(FocusState.Programmatic);
            else if (sender == RatioUSS)
                RatioBK.Focus(FocusState.Programmatic);
            else if (sender == RatioBK)
                LotNumberInput.Focus(FocusState.Programmatic);
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

            if (cl + uss + bk != 100)
            {
                ShowError("Ratio total must be exactly 100 before adding rows.");
                return;
            }

            string ratio = $"USS{uss} : CL{cl} : BK{bk}";
            string lotNo = LotNumberInput.Text ?? "";
            int count = double.IsNaN(SampleCountInput.Value) ? 0 : (int)SampleCountInput.Value;

            if (count <= 0) return;

            int startNo = _tableData.Count + 1;
            var batch = new List<MeasurementRow>();
            for (int i = 0; i < count; i++)
            {
                var row = new MeasurementRow
                {
                    Ratio = ratio,
                    ProdDate = _parameters.Date?.ToString("dd-MMM-yyyy") ?? "",
                    LotNo = lotNo,
                    SampleNo = startNo + i,
                    Value = null
                };
                batch.Add(row);
                _tableData.Add(row);
            }
            _undoStack.Push(batch);
            _redoStack.Clear();
            UpdateCount();
        }

        private async void Calculate_Click(object sender, RoutedEventArgs e)
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

            // Ask for LSL / USL via dialog
            var lslBox = new NumberBox
            {
                Header = "LSL (Lower Spec Limit)",
                PlaceholderText = "e.g. 40",
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Width = 260
            };
            var uslBox = new NumberBox
            {
                Header = "USL (Upper Spec Limit)",
                PlaceholderText = "e.g. 50",
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Width = 260
            };
            var subgroupBox = new NumberBox
            {
                Header = "Subgroup Size",
                Value = values.Count,
                Minimum = 1,
                Maximum = values.Count,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Width = 260
            };

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(lslBox);
            panel.Children.Add(uslBox);
            panel.Children.Add(subgroupBox);

            var dialog = new ContentDialog
            {
                Title = "Specification Limits",
                Content = panel,
                PrimaryButtonText = "Calculate",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            double lsl = double.IsNaN(lslBox.Value) ? 0 : lslBox.Value;
            double usl = double.IsNaN(uslBox.Value) ? 0 : uslBox.Value;
            int subgroupSize = double.IsNaN(subgroupBox.Value) ? values.Count : (int)subgroupBox.Value;

            if (usl <= lsl)
            {
                ShowError("USL must be greater than LSL.");
                return;
            }

            var res = CPKEngine.Calculate(values, lsl, usl, subgroupSize);
            SummaryTxt.Text = $"Mean: {res.Mean:F3} | CPK: {res.Cpk:F2}";
            UpdateCount();

            // Open result window
            var data = new CPKResultData
            {
                Values = values,
                LSL = lsl,
                USL = usl,
                SubgroupSize = subgroupSize,
                Results = res,
                Title = _parameters != null ? $"{_parameters.Grade} — CPK Analysis" : "CPK Analysis",
                Date = _parameters?.Date?.ToString("dd-MMM-yyyy") ?? ""
            };

            var resultWindow = new CPKResultWindow();
            resultWindow.LoadData(data);
            resultWindow.Activate();
        }

        private void UpdateCount()
        {
            int filledCount = _tableData.Count(r => r.Value.HasValue);
            DataCountTxt.Text = $"Count: {filledCount} / {_tableData.Count}";
        }

        private void UndoRows()
        {
            if (_undoStack.Count == 0) return;
            var batch = _undoStack.Pop();
            foreach (var row in batch)
                _tableData.Remove(row);
            _redoStack.Push(batch);
            RenumberRows();
            UpdateCount();
        }

        private void RedoRows()
        {
            if (_redoStack.Count == 0) return;
            var batch = _redoStack.Pop();
            foreach (var row in batch)
            {
                row.SampleNo = _tableData.Count + 1;
                _tableData.Add(row);
            }
            _undoStack.Push(batch);
            RenumberRows();
            UpdateCount();
        }

        private void RenumberRows()
        {
            for (int i = 0; i < _tableData.Count; i++)
                _tableData[i].SampleNo = i + 1;
        }

        private void UndoAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            UndoRows();
            args.Handled = true;
        }

        private void RedoAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            RedoRows();
            args.Handled = true;
        }

        private void UndoMenu_Click(object sender, RoutedEventArgs e) => UndoRows();
        private void RedoMenu_Click(object sender, RoutedEventArgs e) => RedoRows();

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementGrid.SelectedItem is MeasurementRow selected)
            {
                _tableData.Remove(selected);
                RenumberRows();
                UpdateCount();
            }
        }

        private void ClearAllRows_Click(object sender, RoutedEventArgs e)
        {
            if (_tableData.Count == 0) return;
            var allBatch = _tableData.ToList();
            _undoStack.Push(allBatch);
            _redoStack.Clear();
            _tableData.Clear();
            UpdateCount();
            SummaryTxt.Text = "Mean: 0.00";
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

        private void POValueBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            if (string.IsNullOrEmpty(args.NewText)) return;
            args.Cancel = !args.NewText.All(c => char.IsDigit(c) || c == '.' || c == '-');
        }

        private void POValueBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter || sender is not TextBox tb) return;
            e.Handled = true;

            // Find which row this TextBox belongs to
            var dataContext = tb.DataContext as MeasurementRow;
            if (dataContext == null) return;

            int idx = _tableData.IndexOf(dataContext);
            if (idx < 0 || idx >= _tableData.Count - 1) return;

            // Find the next row's container and focus its PO Value TextBox
            var nextContainer = MeasurementGrid.ContainerFromIndex(idx + 1) as ListViewItem;
            if (nextContainer == null) return;

            var nextTextBox = FindChildTextBox(nextContainer);
            nextTextBox?.Focus(FocusState.Programmatic);
        }

        private static TextBox? FindChildTextBox(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox tb && tb.Name == "" && tb.TextAlignment == TextAlignment.Center)
                    return tb;
                var result = FindChildTextBox(child);
                if (result != null) return result;
            }
            return null;
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