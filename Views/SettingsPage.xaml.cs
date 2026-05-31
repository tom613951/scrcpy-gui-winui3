using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScrcpyGui.ViewModels;

namespace ScrcpyGui.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage()
        {
            try
            {
                ViewModel = App.Services.GetService<SettingsViewModel>() ?? throw new NullReferenceException("SettingsViewModel not registered in DI container");
                InitializeComponent();
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.WriteAllText(@"C:\Users\26503\Documents\antigravity\crash.log", ex.ToString());
                }
                catch { }
                throw;
            }
        }

        public string GetMaxSizeHeader(double value)
        {
            return value == 0 ? "Resolution: Native" : $"Resolution: {value}px";
        }

        public string GetMaxFpsHeader(double value)
        {
            return value == 0 ? "FPS Limit: Unlimited" : $"FPS Limit: {value} FPS";
        }

        public Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                // Retrieve the window handle (HWND) of the current WinUI 3 window.
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
                
                // Initialize the folder picker with the window handle.
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    ViewModel.CustomScrcpyPath = folder.Path;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting folder: {ex.Message}");
            }
        }
    }
}
