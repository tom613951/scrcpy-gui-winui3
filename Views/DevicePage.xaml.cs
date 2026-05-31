using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ScrcpyGui.ViewModels;

namespace ScrcpyGui.Views
{
    public sealed partial class DevicePage : Page
    {
        public DeviceViewModel ViewModel { get; }

        public DevicePage()
        {
            ViewModel = App.Services.GetService<DeviceViewModel>() ?? throw new NullReferenceException("DeviceViewModel not registered in DI container");
            InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Refresh binary check when navigated to
            ViewModel.CheckBinaries();
        }

        public Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsListEmpty(int count) => count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsListNotEmpty(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
