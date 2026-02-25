using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.UI;

namespace CPK_Calculate
{
    public sealed partial class MainWindow : Window
    {
        public static new MainWindow? Current { get; private set; }
        public ElementTheme CurrentTheme { get; private set; } = ElementTheme.Default;
        public event Action<ElementTheme>? ThemeChanged;

        public MainWindow()
        {
            Current = this;
            this.InitializeComponent();

            if (Application.Current?.Resources.ContainsKey("AppTitle") == true &&
                Application.Current.Resources["AppTitle"] is string titleText)
            {
                this.Title = titleText;
            }

            // Setup Custom TitleBar
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            {
                this.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            }

            this.Activated += MainWindow_Activated;
            this.Closed += MainWindow_Closed;

            if (this.Content is FrameworkElement rootElement)
            {
                UpdateTitleBarColors(rootElement.ActualTheme);
            }
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            args.Handled = true;

            ContentDialog confirmDialog = new ContentDialog
            {
                Title = "Confirm Exit",
                Content = "Are you sure you want to close the CPK Calculation System?\nUnsaved changes may be lost.",
                PrimaryButtonText = "Exit",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                this.Closed -= MainWindow_Closed;
                this.Close();
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= MainWindow_Activated;

            if (nvSample.MenuItems.Count > 0 && nvSample.MenuItems[0] is NavigationViewItem firstItem)
            {
                nvSample.SelectedItem = firstItem;
                string tag = firstItem.Tag?.ToString() ?? string.Empty;
                NavigateToTag(tag);
            }
        }

        private void NvSample_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavigateToTag("SettingPage");
            }
            else if (args.InvokedItemContainer is NavigationViewItem nvi)
            {
                string tag = nvi.Tag?.ToString() ?? string.Empty;
                NavigateToTag(tag);
            }
        }

        private void NavigateToTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            Type? pageType = tag switch
            {
                "DashBoardPage" => typeof(DashBoardPage),
                "CPKPage" => typeof(CPKPage),
                "RecordCPK" => typeof(RecordCPK),
                "ImportDataPage" => typeof(ImportDataPage),
                "HistoryPage" => typeof(HistoryPage),
                "SettingPage" => typeof(SettingPage),
                _ => null
            };

            if (pageType == null || contentFrame == null) return;

            if (contentFrame.Content?.GetType() == pageType) return;

            try
            {
                contentFrame.Navigate(pageType);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(tag, ex.Message);
            }
        }

        private async void ShowErrorDialog(string tag, string message)
        {
            if (this.Content.XamlRoot == null) return;
            ContentDialog dialog = new ContentDialog
            {
                Title = "Navigation Error",
                Content = $"Unable to load page: {tag}\n{message}",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        public void UpdateTheme(ElementTheme theme)
        {
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
                UpdateTitleBarColors(rootElement.ActualTheme);
            }

            CurrentTheme = theme;
            ThemeChanged?.Invoke(theme);
        }

        public void UpdateNavigationStyle(NavigationViewPaneDisplayMode mode)
        {
            nvSample.PaneDisplayMode = mode;
        }

        public void NavigateToResult(CPKResultData data)
        {
            if (contentFrame == null) return;
            contentFrame.Navigate(typeof(CPKResultPage), data);
        }

        private void UpdateTitleBarColors(ElementTheme theme)
        {
            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = this.AppWindow.TitleBar;

                // ตรวจสอบธีมที่ส่งมา (รองรับกรณีเป็น Default ให้ดึงค่าจริงมาใช้)
                bool isLight = theme == ElementTheme.Light;
                if (theme == ElementTheme.Default && this.Content is FrameworkElement root)
                {
                    isLight = root.ActualTheme == ElementTheme.Light;
                }

                if (isLight)
                {
                    // โหมดสีสว่าง (ปุ่มดำ พื้นหลังสว่าง)
                    titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                    titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
                    titleBar.ButtonHoverBackgroundColor = Color.FromArgb(20, 0, 0, 0);
                    titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.Black;
                    titleBar.ButtonPressedBackgroundColor = Color.FromArgb(40, 0, 0, 0);
                    titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.DarkGray;
                }
                else
                {
                    // โหมดสีมืด (ปุ่มขาว พื้นหลังมืด)
                    titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                    titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                    titleBar.ButtonHoverBackgroundColor = Color.FromArgb(20, 255, 255, 255);
                    titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                    titleBar.ButtonPressedBackgroundColor = Color.FromArgb(40, 255, 255, 255);
                    titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                }

                titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            }
        }
    }
}