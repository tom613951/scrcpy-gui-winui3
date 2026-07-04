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

            // Clean up adb when closing
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            try
            {
                var pathService = (ScrcpyGui.Services.PathService)App.Services.GetService(typeof(ScrcpyGui.Services.PathService));
                if (pathService != null && System.IO.File.Exists(pathService.AdbPath))
                {
                    // Run adb kill-server independently so it completes even as the app exits
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = pathService.AdbPath,
                        Arguments = "kill-server",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
            }
            catch { }
        }
    }
}
