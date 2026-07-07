using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScrcpyGui.Services;
using ScrcpyGui.ViewModels;
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

            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            try
            {
                App.Services.GetService<DeviceViewModel>()?.Shutdown();
            }
            catch
            {
            }

            try
            {
                var adbService = App.Services.GetService<AdbService>();
                if (adbService == null)
                {
                    return;
                }

                Task.Run(async () => await adbService.StopServerAsync()).Wait(8000);
                adbService.KillResidualProcesses();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to stop adb on exit: {ex.Message}");
            }
        }
    }
}
