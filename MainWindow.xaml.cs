using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScrcpyGui.Views;

namespace ScrcpyGui
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            AppWindow.SetIcon("Assets/AppIcon.ico");

            // Select the first item on startup
            NavView.SelectedItem = DeviceItem;
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer != null)
            {
                var tag = args.SelectedItemContainer.Tag?.ToString();
                switch (tag)
                {
                    case "Devices":
                        ContentFrame.Navigate(typeof(DevicePage));
                        break;
                    case "PackageManager":
                        ContentFrame.Navigate(typeof(DownloaderPage));
                        break;
                }
            }
        }
    }
}
