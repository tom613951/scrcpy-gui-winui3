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
        private Process? _scrcpyProcess;
        private const int MaxLogLength = 100_000;

        private ObservableCollection<AdbDevice> _devices = new();
        private AdbDevice? _selectedDevice;
        private bool _isRefreshing = false;
        private string _logOutput = string.Empty;
        private string _wirelessIpAddress = string.Empty;
        private double _wirelessPort = 5555;
        private string _pairingCode = string.Empty;
        private string _wirelessStatusMessage = string.Empty;
        private bool _isBinariesMissing = false;
        private bool _isMirroring = false;
        private string _terminalInput = string.Empty;

        public ObservableCollection<AdbDevice> Devices
        {
            get => _devices;
            set => SetProperty(ref _devices, value);
        }

        public AdbDevice? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    NotifyMirroringCommandStates();
                }
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public string LogOutput
        {
            get => _logOutput;
            set => SetProperty(ref _logOutput, value);
        }

        public string WirelessIpAddress
        {
            get => _wirelessIpAddress;
            set => SetProperty(ref _wirelessIpAddress, value);
        }

        public double WirelessPort
        {
            get => _wirelessPort;
            set => SetProperty(ref _wirelessPort, value);
        }

        public string PairingCode
        {
            get => _pairingCode;
            set => SetProperty(ref _pairingCode, value);
        }

        public string WirelessStatusMessage
        {
            get => _wirelessStatusMessage;
            set => SetProperty(ref _wirelessStatusMessage, value);
        }

        public bool IsBinariesMissing
        {
            get => _isBinariesMissing;
            set
            {
                if (SetProperty(ref _isBinariesMissing, value))
                {
                    NotifyMirroringCommandStates();
                }
            }
        }

        public bool IsMirroring
        {
            get => _isMirroring;
            set
            {
                if (SetProperty(ref _isMirroring, value))
                {
                    NotifyMirroringCommandStates();
                }
            }
        }

        public string TerminalInput
        {
            get => _terminalInput;
            set => SetProperty(ref _terminalInput, value);
        }

        public IAsyncRelayCommand RefreshDevicesCommand { get; }
        public IAsyncRelayCommand StartMirroringCommand { get; }
        public IAsyncRelayCommand WirelessConnectCommand { get; }
        public IAsyncRelayCommand WirelessPairCommand { get; }
        public IRelayCommand ClearLogCommand { get; }
        public IAsyncRelayCommand KillAdbCommand { get; }
        public IAsyncRelayCommand RunTerminalCommand { get; }
        public IRelayCommand StopMirroringCommand { get; }

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
            StartMirroringCommand = new AsyncRelayCommand(StartMirroringAsync, CanStartMirroring);
            StopMirroringCommand = new RelayCommand(StopMirroring, () => IsMirroring);
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
            NotifyMirroringCommandStates();
        }

        private async Task RefreshDevicesAsync()
        {
            IsRefreshing = true;
            CheckBinaries();
            
            try
            {
                Devices.Clear();
                var list = await _adbService.GetDevicesAsync();
                if (list.Count == 0)
                {
                    await Task.Delay(700);
                    list = await _adbService.GetDevicesAsync();
                }

                foreach (var device in list)
                {
                    Devices.Add(device);
                }

                if (Devices.Count > 0)
                {
                    SelectedDevice = Devices[0];
                }
                else
                {
                    SelectedDevice = null;
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
            if (!CanStartMirroring()) return;

            AppendLog("----------------------------------------");
            AppendLog($"正在启动设备投屏: {SelectedDevice!.Model}");
            
            var process = await _scrcpyService.StartMirroringAsync(
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
                        ClearMirroringProcess();
                    });
                }
            );

            if (process != null)
            {
                _scrcpyProcess = process;
                IsMirroring = true;
            }
        }

        private bool CanStartMirroring()
        {
            return SelectedDevice?.IsAuthorized == true && !IsBinariesMissing && !IsMirroring;
        }

        private void StopMirroring()
        {
            var process = _scrcpyProcess;
            if (process == null)
            {
                ClearMirroringProcess();
                return;
            }

            try
            {
                if (process.HasExited)
                {
                    ClearMirroringProcess();
                    return;
                }

                AppendLog("正在停止 scrcpy 进程...");
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                AppendLog($"停止 scrcpy 失败: {ex.Message}");
                ClearMirroringProcess();
            }
        }

        public void Shutdown()
        {
            StopMirroring();
        }

        private void ClearMirroringProcess()
        {
            _scrcpyProcess?.Dispose();
            _scrcpyProcess = null;
            IsMirroring = false;
        }

        private async Task WirelessConnectAsync()
        {
            if (string.IsNullOrEmpty(WirelessIpAddress))
            {
                WirelessStatusMessage = "请输入 IP 地址";
                return;
            }

            WirelessStatusMessage = "正在连接...";
            if (!TryGetWirelessPort(out var port))
            {
                return;
            }

            var result = await _adbService.ConnectWirelessAsync(WirelessIpAddress.Trim(), port);
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
            if (!TryGetWirelessPort(out var port))
            {
                return;
            }

            var result = await _adbService.PairWirelessAsync(WirelessIpAddress.Trim(), port, PairingCode.Trim());
            WirelessStatusMessage = result;
        }

        private bool TryGetWirelessPort(out int port)
        {
            port = 0;

            if (double.IsNaN(WirelessPort) || WirelessPort < 1 || WirelessPort > 65535)
            {
                WirelessStatusMessage = "请输入 1-65535 之间的端口";
                return false;
            }

            port = (int)WirelessPort;
            return true;
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
            AppendLog("正在重启 ADB 服务...");

            try
            {
                await _adbService.StopServerAsync();
                AppendLog("ADB 服务已停止。");
                await Task.Delay(1200);

                var startResult = await _adbService.StartServerAsync();
                if (!string.IsNullOrWhiteSpace(startResult))
                {
                    AppendLog(startResult);
                }
                else
                {
                    AppendLog("ADB 服务已启动。");
                }

                await Task.Delay(1500);
            }
            finally
            {
                IsRefreshing = false;
            }

            await RefreshDevicesAsync();
        }

        private Task RunTerminalCommandAsync()
        {
            var command = TerminalInput?.Trim();
            if (string.IsNullOrEmpty(command)) return Task.CompletedTask;

            // Clear input
            TerminalInput = string.Empty;

            AppendLog($"\n> {command}");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    WorkingDirectory = System.IO.Directory.Exists(_pathService.ScrcpyDirectory) ? _pathService.ScrcpyDirectory : AppDomain.CurrentDomain.BaseDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                startInfo.ArgumentList.Add("-NoProfile");
                startInfo.ArgumentList.Add("-Command");
                startInfo.ArgumentList.Add(command);

                var process = new Process
                {
                    StartInfo = startInfo,
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
                _ = MonitorTerminalProcessAsync(process);
            }
            catch (Exception ex)
            {
                AppendLog($"执行命令失败: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private async Task MonitorTerminalProcessAsync(Process process)
        {
            try
            {
                await process.WaitForExitAsync();
                var exitCode = process.ExitCode;
                App.MainWindowInstance?.DispatcherQueue.TryEnqueue(() =>
                {
                    AppendLog($"命令已退出，退出代码: {exitCode}");
                });
            }
            catch (Exception ex)
            {
                App.MainWindowInstance?.DispatcherQueue.TryEnqueue(() =>
                {
                    AppendLog($"命令状态监控失败: {ex.Message}");
                });
            }
            finally
            {
                process.Dispose();
            }
        }

        private void AppendLog(string message)
        {
            LogOutput += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            if (LogOutput.Length > MaxLogLength)
            {
                LogOutput = LogOutput[^MaxLogLength..];
            }
        }

        private void NotifyMirroringCommandStates()
        {
            StartMirroringCommand.NotifyCanExecuteChanged();
            StopMirroringCommand.NotifyCanExecuteChanged();
        }

    }
}
