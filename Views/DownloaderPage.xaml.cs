using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ScrcpyGui.ViewModels;

namespace ScrcpyGui.Views
{
    public sealed partial class DownloaderPage : Page
    {
        public DownloaderViewModel ViewModel { get; }

        public DownloaderPage()
        {
            ViewModel = App.Services.GetService<DownloaderViewModel>() ?? throw new System.NullReferenceException("DownloaderViewModel not registered in DI container");
            InitializeComponent();
        }

        public Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NegateBoolToVisibility(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
        public string GetProgressText(double progress) => $"{progress:F0}%";
    }
}
