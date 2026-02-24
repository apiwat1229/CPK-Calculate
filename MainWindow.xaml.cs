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
        // ใช้ new เพื่อระบุว่าเราต้องการสร้างสมาชิกใหม่ทับของเดิม และใช้ ? เพื่อรองรับ Nullable
        public static new MainWindow? Current { get; private set; }

        public MainWindow()
        {
            Current = this;
            this.InitializeComponent();

            // Setup Custom TitleBar
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            // Set Tall TitleBar height if supported
            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            {
                this.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            }

            // Register Window Events
            this.Activated += MainWindow_Activated;
            this.Closed += MainWindow_Closed;

            // ตั้งค่าเริ่มต้นของสีปุ่ม TitleBar ตามธีมที่แอปกำลังแสดงผล
            if (this.Content is FrameworkElement rootElement)
            {
                // ตรวจสอบธีมเริ่มต้นเมื่อเปิดแอป
                UpdateTitleBarColors(rootElement.ActualTheme);
            }
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            args.Handled = true; // หยุดการปิดหน้าต่างไว้ก่อน

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

            // โหลดหน้าแรกโดยอัตโนมัติ
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
                "DashBoardPage1" => typeof(DashBoardPage1),
                "CPKPage" => typeof(CPKPage),
                "SamplePage3" => typeof(SamplePage3),
                "SettingPage" => typeof(SettingPage),
                _ => null
            };

            if (pageType == null || contentFrame == null) return;

            // ป้องกันการโหลดหน้าเดิมซ้ำ
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
                // อัปเดตสี TitleBar ทันทีที่เปลี่ยนธีมโดยใช้ธีมที่แสดงผลจริง
                UpdateTitleBarColors(rootElement.ActualTheme);
            }
        }

        public void UpdateNavigationStyle(NavigationViewPaneDisplayMode mode)
        {
            nvSample.PaneDisplayMode = mode;
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