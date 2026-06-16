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

        public string GetMaxSizeHeader(double value) => value == 0 ? "不限制" : $"{value} px";

        public string GetMaxFpsHeader(double value) => value == 0 ? "不限制" : $"{value} FPS";

        public Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsSessionMode(string currentMode, string targetMode) => currentMode == targetMode ? Visibility.Visible : Visibility.Collapsed;

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
            else
            {
                this.Frame.Navigate(typeof(DevicePage));
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
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

        private async void BrowseAdbButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
                filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
                filePicker.FileTypeFilter.Add(".exe");

                // Retrieve the window handle (HWND) of the current WinUI 3 window.
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
                
                // Initialize the folder picker with the window handle.
                WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

                var file = await filePicker.PickSingleFileAsync();
                if (file != null)
                {
                    ViewModel.CustomAdbPath = file.Path;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting file: {ex.Message}");
            }
        }
    }
}
