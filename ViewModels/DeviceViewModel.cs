using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScrcpyGui.Models;
using ScrcpyGui.Services;

namespace ScrcpyGui.ViewModels
{
    public partial class DeviceViewModel : ObservableObject
    {
        private readonly AdbService _adbService;
        private readonly ScrcpyService _scrcpyService;
        private readonly SettingsService _settingsService;
        private readonly PathService _pathService;

        [ObservableProperty]
        private ObservableCollection<AdbDevice> _devices = new();

        [ObservableProperty]
        private AdbDevice? _selectedDevice;

        [ObservableProperty]
        private bool _isRefreshing = false;

        [ObservableProperty]
        private string _logOutput = string.Empty;

        [ObservableProperty]
        private string _wirelessIpAddress = string.Empty;

        [ObservableProperty]
        private double _wirelessPort = 5555;

        [ObservableProperty]
        private string _pairingCode = string.Empty;

        [ObservableProperty]
        private string _wirelessStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBinariesMissing = false;

        [ObservableProperty]
        private string _terminalInput = string.Empty;

        public IAsyncRelayCommand RefreshDevicesCommand { get; }
        public IAsyncRelayCommand StartMirroringCommand { get; }
        public IAsyncRelayCommand WirelessConnectCommand { get; }
        public IAsyncRelayCommand WirelessPairCommand { get; }
        public IRelayCommand ClearLogCommand { get; }
        public IAsyncRelayCommand KillAdbCommand { get; }
        public IAsyncRelayCommand RunTerminalCommand { get; }

        public DeviceViewModel(
            AdbService adbService, 
            ScrcpyService scrcpyService, 
            SettingsService settingsService,
            PathService pathService)
        {
            _adbService = adbService;
            _scrcpyService = scrcpyService;
            _settingsService = settingsService;
            _pathService = pathService;

            RefreshDevicesCommand = new AsyncRelayCommand(RefreshDevicesAsync);
            StartMirroringCommand = new AsyncRelayCommand(StartMirroringAsync, () => SelectedDevice != null && !IsBinariesMissing);
            WirelessConnectCommand = new AsyncRelayCommand(WirelessConnectAsync);
            WirelessPairCommand = new AsyncRelayCommand(WirelessPairAsync);
            ClearLogCommand = new RelayCommand(() => LogOutput = string.Empty);
            KillAdbCommand = new AsyncRelayCommand(KillAdbAsync);
            RunTerminalCommand = new AsyncRelayCommand(RunTerminalCommandAsync);

            // Initial verification of binaries
            CheckBinaries();

            // Refresh on start
            _ = RefreshDevicesAsync();
        }

        public void CheckBinaries()
        {
            IsBinariesMissing = !_pathService.BinariesExist;
            StartMirroringCommand.NotifyCanExecuteChanged();
        }

        private async Task RefreshDevicesAsync()
        {
            IsRefreshing = true;
            CheckBinaries();
            
            try
            {
                Devices.Clear();
                var list = await _adbService.GetDevicesAsync();
                foreach (var device in list)
                {
                    Devices.Add(device);
                }

                if (Devices.Count > 0)
                {
                    SelectedDevice = Devices[0];
                }
            }
            catch (Exception ex)
            {
                AppendLog($"设备检测错误: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task StartMirroringAsync()
        {
            if (SelectedDevice == null) return;

            AppendLog("----------------------------------------");
            AppendLog($"正在启动设备投屏: {SelectedDevice.Model}");
            
            await _scrcpyService.StartMirroringAsync(
                SelectedDevice,
                _settingsService.Settings,
                output =>
                {
                    // Dispatch output log back to the UI thread
                    App.MainWindowInstance?.DispatcherQueue.TryEnqueue(() =>
                    {
                        AppendLog(output);
                    });
                },
                exitCode =>
                {
                    App.MainWindowInstance?.DispatcherQueue.TryEnqueue(() =>
                    {
                        AppendLog($"scrcpy 进程已退出，退出代码: {exitCode}");
                    });
                }
            );
        }

        private async Task WirelessConnectAsync()
        {
            if (string.IsNullOrEmpty(WirelessIpAddress))
            {
                WirelessStatusMessage = "请输入 IP 地址";
                return;
            }

            WirelessStatusMessage = "正在连接...";
            var result = await _adbService.ConnectWirelessAsync(WirelessIpAddress, (int)WirelessPort);
            WirelessStatusMessage = result;
            
            await RefreshDevicesAsync();
        }

        private async Task WirelessPairAsync()
        {
            if (string.IsNullOrEmpty(WirelessIpAddress) || string.IsNullOrEmpty(PairingCode))
            {
                WirelessStatusMessage = "请输入 IP 地址和配对码";
                return;
            }

            WirelessStatusMessage = "正在配对...";
            var result = await _adbService.PairWirelessAsync(WirelessIpAddress, (int)WirelessPort, PairingCode);
            WirelessStatusMessage = result;
        }

        public async Task PushFileOrInstallApkAsync(string filePath)
        {
            if (SelectedDevice == null)
            {
                AppendLog("拖拽操作失败：未选择设备！");
                return;
            }

            try
            {
                if (filePath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                {
                    AppendLog($"正在安装 APK: {filePath}");
                    var result = await _adbService.InstallApkAsync(SelectedDevice.Serial, filePath);
                    AppendLog($"安装结果: {result}");
                }
                else
                {
                    AppendLog($"正在推送文件: {filePath}");
                    var result = await _adbService.PushFileAsync(SelectedDevice.Serial, filePath);
                    AppendLog($"推送结果: {result}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"传输错误: {ex.Message}");
            }
        }

        private async Task KillAdbAsync()
        {
            IsRefreshing = true;
            AppendLog("正在终止 ADB 服务...");
            var result = await _adbService.KillServerAsync();
            AppendLog($"ADB 服务已终止。{result}");
            
            // Wait a moment and refresh
            await Task.Delay(500);
            await RefreshDevicesAsync();
        }

        private async Task RunTerminalCommandAsync()
        {
            var command = TerminalInput?.Trim();
            if (string.IsNullOrEmpty(command)) return;

            // Clear input
            TerminalInput = string.Empty;

            AppendLog($"\n> {command}");

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"{command.Replace("\"", "\\\"")}\"",
                        WorkingDirectory = System.IO.Directory.Exists(_pathService.ScrcpyDirectory) ? _pathService.ScrcpyDirectory : AppDomain.CurrentDomain.BaseDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        App.MainWindowInstance?.DispatcherQueue.TryEnqueue(() => AppendLog(e.Data));
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        App.MainWindowInstance?.DispatcherQueue.TryEnqueue(() => AppendLog(e.Data));
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                AppendLog($"执行命令失败: {ex.Message}");
            }
        }

        private void AppendLog(string message)
        {
            LogOutput += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }

        partial void OnSelectedDeviceChanged(AdbDevice? value)
        {
            StartMirroringCommand.NotifyCanExecuteChanged();
        }
    }
}
