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

        // AI Feature Properties
        private ObservableCollection<UiChatMessage> _aiChatHistory = new();
        private string _aiInputMessage = string.Empty;
        private string _aiLogOutput = string.Empty;
        private bool _isAiThinking = false;

        public ObservableCollection<UiChatMessage> AiChatHistory
        {
            get => _aiChatHistory;
            set => SetProperty(ref _aiChatHistory, value);
        }

        public string AiInputMessage
        {
            get => _aiInputMessage;
            set => SetProperty(ref _aiInputMessage, value);
        }

        public string AiLogOutput
        {
            get => _aiLogOutput;
            set => SetProperty(ref _aiLogOutput, value);
        }

        public bool IsAiThinking
        {
            get => _isAiThinking;
            set
            {
                if (SetProperty(ref _isAiThinking, value))
                {
                    OnPropertyChanged(nameof(IsNotAiThinking));
                }
            }
        }

        public bool IsNotAiThinking => !IsAiThinking;

        public IAsyncRelayCommand RefreshDevicesCommand { get; }
        public IAsyncRelayCommand StartMirroringCommand { get; }
        public IAsyncRelayCommand WirelessConnectCommand { get; }
        public IAsyncRelayCommand WirelessPairCommand { get; }
        public IRelayCommand ClearLogCommand { get; }
        public IAsyncRelayCommand KillAdbCommand { get; }
        public IAsyncRelayCommand RunTerminalCommand { get; }
        public IRelayCommand StopMirroringCommand { get; }
        public IAsyncRelayCommand SendToAiCommand { get; }

        private readonly AiService _aiService;

        public DeviceViewModel(
            AdbService adbService, 
            ScrcpyService scrcpyService, 
            SettingsService settingsService,
            PathService pathService,
            AiService aiService)
        {
            _adbService = adbService;
            _scrcpyService = scrcpyService;
            _settingsService = settingsService;
            _pathService = pathService;
            _aiService = aiService;

            RefreshDevicesCommand = new AsyncRelayCommand(RefreshDevicesAsync);
            StartMirroringCommand = new AsyncRelayCommand(StartMirroringAsync, CanStartMirroring);
            StopMirroringCommand = new RelayCommand(StopMirroring, () => IsMirroring);
            WirelessConnectCommand = new AsyncRelayCommand(WirelessConnectAsync);
            WirelessPairCommand = new AsyncRelayCommand(WirelessPairAsync);
            ClearLogCommand = new RelayCommand(() => LogOutput = string.Empty);
            KillAdbCommand = new AsyncRelayCommand(KillAdbAsync);
            RunTerminalCommand = new AsyncRelayCommand(RunTerminalCommandAsync);
            SendToAiCommand = new AsyncRelayCommand(SendToAiAsync);

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

        private void AppendAiLog(string message)
        {
            App.MainWindowInstance?.DispatcherQueue.TryEnqueue(() => 
            {
                AiLogOutput += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            });
        }

        private async Task ExecuteRpaActionsAsync(System.Collections.Generic.List<RpaAction> actions)
        {
            if (SelectedDevice == null) return;
            
            var res = await _adbService.GetScreenResolutionAsync(SelectedDevice.Serial);
            double scaleX = res.HasValue ? res.Value.width / 1000.0 : 1.0;
            double scaleY = res.HasValue ? res.Value.height / 1000.0 : 1.0;

            if (res.HasValue)
            {
                AppendAiLog($"设备分辨率：{res.Value.width}x{res.Value.height}。缩放因子 X:{scaleX}, Y:{scaleY}");
            }
            else
            {
                AppendAiLog("获取设备分辨率失败，将使用默认坐标(1000x1000比例)。");
            }

            foreach (var action in actions)
            {
                AppendAiLog($"正在执行: {action.Action} {action.Position?.X},{action.Position?.Y} '{action.Text}'");

                string? adbCommand = null;
                switch (action.Action.ToLower())
                {
                    case "tap":
                        if (action.Position != null)
                        {
                            int x = (int)(action.Position.X * scaleX);
                            int y = (int)(action.Position.Y * scaleY);
                            adbCommand = $"-s {SelectedDevice.Serial} shell input tap {x} {y}";
                        }
                        break;
                    case "swipe":
                        if (action.Position != null && action.TargetPosition != null)
                        {
                            int x1 = (int)(action.Position.X * scaleX);
                            int y1 = (int)(action.Position.Y * scaleY);
                            int x2 = (int)(action.TargetPosition.X * scaleX);
                            int y2 = (int)(action.TargetPosition.Y * scaleY);
                            adbCommand = $"-s {SelectedDevice.Serial} shell input swipe {x1} {y1} {x2} {y2}";
                        }
                        break;
                    case "input_text":
                        if (!string.IsNullOrEmpty(action.Text))
                        {
                            string safeText = action.Text.Replace(" ", "%s");
                            adbCommand = $"-s {SelectedDevice.Serial} shell input text '{safeText}'";
                        }
                        break;
                    case "keyevent":
                        if (!string.IsNullOrEmpty(action.Text))
                        {
                            adbCommand = $"-s {SelectedDevice.Serial} shell input keyevent {action.Text}";
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(adbCommand))
                {
                    var executeResult = await _adbService.ExecuteCommandAsync(adbCommand, 5000);
                    if (!string.IsNullOrWhiteSpace(executeResult))
                    {
                        AppendAiLog($"ADB 输出: {executeResult}");
                    }
                }
                
                await Task.Delay(1000);
            }
        }

        private async Task SendToAiAsync()
        {
            if (string.IsNullOrWhiteSpace(AiInputMessage)) return;
            if (SelectedDevice == null)
            {
                AiChatHistory.Add(new UiChatMessage { Role = "System", Text = "请先连接并选择一台设备。" });
                return;
            }

            var userText = AiInputMessage;
            AiInputMessage = string.Empty;
            AiChatHistory.Add(new UiChatMessage { Role = "User", Text = userText });
            IsAiThinking = true;

            try
            {
                AppendAiLog("正在截取屏幕...");
                var base64Image = await _adbService.CaptureScreenAsBase64Async(SelectedDevice.Serial);

                if (string.IsNullOrEmpty(base64Image))
                {
                    AiChatHistory.Add(new UiChatMessage { Role = "System", Text = "截图失败，请确保设备在线。" });
                    return;
                }

                AppendAiLog($"截图成功，请求 AI...");
                var response = await _aiService.AnalyzeScreenAndPlanAsync(
                    base64Image, 
                    userText, 
                    _settingsService.Settings.AiBaseUrl, 
                    _settingsService.Settings.AiModelName,
                    _settingsService.Settings.AiApiKey);

                AiChatHistory.Add(new UiChatMessage { Role = "Agent", Text = response.Explanation });

                if (response.Actions != null && response.Actions.Count > 0)
                {
                    AppendAiLog($"获取到 {response.Actions.Count} 个动作，开始执行...");
                    await ExecuteRpaActionsAsync(response.Actions);
                    AppendAiLog("动作执行完成。");
                }
            }
            catch (Exception ex)
            {
                AiChatHistory.Add(new UiChatMessage { Role = "System", Text = $"AI 服务出错: {ex.Message}" });
                AppendAiLog($"AI 请求异常: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsAiThinking = false;
            }
        }

    }
}
