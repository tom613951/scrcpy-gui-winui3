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
        private string _statusMessage = "准备就绪";

        [ObservableProperty]
        private bool _isDownloading = false;

        [ObservableProperty]
        private bool _isIndeterminate = false;

        [ObservableProperty]
        private string _latestVersion = "未知";

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
            StatusMessage = "正在检查更新...";
            var info = await _updateService.GetLatestReleaseInfoAsync();
            LatestVersion = info.Version;
            StatusMessage = _pathService.BinariesExist ? "已检测到 scrcpy 运行组件。" : "未找到 scrcpy 运行组件，请点击上方“下载并安装”进行配置。";
        }

        private async Task StartDownloadAsync()
        {
            IsDownloading = true;
            StartDownloadCommand.NotifyCanExecuteChanged();

            try
            {
                StatusMessage = "正在获取下载地址...";
                var info = await _updateService.GetLatestReleaseInfoAsync();
                if (string.IsNullOrEmpty(info.DownloadUrl))
                {
                    StatusMessage = "错误：未能找到 Windows 64位版本的下载资源。";
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
                StatusMessage = $"下载失败: {ex.Message}";
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
