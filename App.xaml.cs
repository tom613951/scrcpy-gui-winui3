using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using ScrcpyGui.Services;
using ScrcpyGui.ViewModels;

namespace ScrcpyGui
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static MainWindow? MainWindowInstance { get; private set; }
        public static IServiceProvider Services => ((App)Current).ServiceProvider;
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            var services = new ServiceCollection();

            // Register Services
            services.AddSingleton<PathService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<AdbService>();
            services.AddSingleton<ScrcpyService>();
            services.AddSingleton<UpdateService>();

            // Register ViewModels
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<DownloaderViewModel>();
            services.AddSingleton<DeviceViewModel>();

            ServiceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindowInstance = new MainWindow();
            MainWindowInstance.Activate();
        }
    }
}
