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

            // Navigate directly to DevicePage on startup
            ContentFrame.Navigate(typeof(DevicePage));
        }
    }
}
