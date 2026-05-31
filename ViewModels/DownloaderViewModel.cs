using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScrcpyGui.Services;

namespace ScrcpyGui.ViewModels
{
    public partial class DownloaderViewModel : ObservableObject
    {
        private readonly UpdateService _updateService;
        private readonly PathService _pathService;

        [ObservableProperty]
        private double _progressValue = 0;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isDownloading = false;

        [ObservableProperty]
        private bool _isIndeterminate = false;

        [ObservableProperty]
        private string _latestVersion = "Unknown";

        public IAsyncRelayCommand StartDownloadCommand { get; }
        public IAsyncRelayCommand CheckVersionCommand { get; }

        public DownloaderViewModel(UpdateService updateService, PathService pathService)
        {
            _updateService = updateService;
            _pathService = pathService;

            StartDownloadCommand = new AsyncRelayCommand(StartDownloadAsync, () => !IsDownloading);
            CheckVersionCommand = new AsyncRelayCommand(CheckVersionAsync);

            // Fetch info on load
            _ = CheckVersionAsync();
        }

        public async Task CheckVersionAsync()
        {
            StatusMessage = "Checking for updates...";
            var info = await _updateService.GetLatestReleaseInfoAsync();
            LatestVersion = info.Version;
            StatusMessage = _pathService.BinariesExist ? "scrcpy binaries found." : "scrcpy binaries not found. Please click Download.";
        }

        private async Task StartDownloadAsync()
        {
            IsDownloading = true;
            StartDownloadCommand.NotifyCanExecuteChanged();

            try
            {
                StatusMessage = "Fetching download URL...";
                var info = await _updateService.GetLatestReleaseInfoAsync();
                if (string.IsNullOrEmpty(info.DownloadUrl))
                {
                    StatusMessage = "Error: Could not find Windows 64-bit download asset.";
                    return;
                }

                await _updateService.DownloadAndExtractScrcpyAsync(
                    info.DownloadUrl,
                    progress =>
                    {
                        if (progress < 0)
                        {
                            IsIndeterminate = true;
                        }
                        else
                        {
                            IsIndeterminate = false;
                            ProgressValue = progress;
                        }
                    },
                    status =>
                    {
                        StatusMessage = status;
                    }
                );
            }
            catch (Exception ex)
            {
                StatusMessage = $"Download failed: {ex.Message}";
            }
            finally
            {
                IsDownloading = false;
                IsIndeterminate = false;
                StartDownloadCommand.NotifyCanExecuteChanged();
            }
        }
    }
}
