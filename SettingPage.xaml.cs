using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CPK_Calculate
{
    public sealed partial class SettingPage : Page
    {
        public SettingPage()
        {
            this.InitializeComponent();
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ป้องกันไม่ให้ทำงานเองตอนเปิดหน้าครั้งแรก
            if (!this.IsLoaded) return;

            if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
            {
                string theme = item.Tag?.ToString();
                ElementTheme newTheme = ElementTheme.Default;

                if (theme == "Light") newTheme = ElementTheme.Light;
                else if (theme == "Dark") newTheme = ElementTheme.Dark;

                // สั่งเปลี่ยนสีผ่าน MainWindow
                MainWindow.Current?.UpdateTheme(newTheme);
            }
        }

        private void NavComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ป้องกันไม่ให้ทำงานเองตอนเปิดหน้าครั้งแรก
            if (!this.IsLoaded) return;

            if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
            {
                string navStyle = item.Tag?.ToString();

                var mode = navStyle == "Top"
                    ? NavigationViewPaneDisplayMode.Top
                    : NavigationViewPaneDisplayMode.Left;

                // สั่งสลับเมนูผ่าน MainWindow
                MainWindow.Current?.UpdateNavigationStyle(mode);
            }
        }
    }
}