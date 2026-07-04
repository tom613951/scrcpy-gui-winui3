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

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            }
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        if (item is Windows.Storage.StorageFile file)
                        {
                            await ViewModel.PushFileOrInstallApkAsync(file.Path);
                        }
                    }
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private void TerminalInput_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (ViewModel.RunTerminalCommand.CanExecute(null))
                {
                    ViewModel.RunTerminalCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
