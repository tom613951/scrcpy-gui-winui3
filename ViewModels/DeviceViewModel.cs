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

        public IAsyncRelayCommand RefreshDevicesCommand { get; }
        public IAsyncRelayCommand StartMirroringCommand { get; }
        public IAsyncRelayCommand WirelessConnectCommand { get; }
        public IAsyncRelayCommand WirelessPairCommand { get; }
        public IRelayCommand ClearLogCommand { get; }

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
                AppendLog($"Error discovering devices: {ex.Message}");
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
            AppendLog($"Starting screen mirror for: {SelectedDevice.Model}");
            
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
                        AppendLog($"scrcpy exited with code: {exitCode}");
                    });
                }
            );
        }

        private async Task WirelessConnectAsync()
        {
            if (string.IsNullOrEmpty(WirelessIpAddress))
            {
                WirelessStatusMessage = "Please enter IP Address";
                return;
            }

            WirelessStatusMessage = "Connecting...";
            var result = await _adbService.ConnectWirelessAsync(WirelessIpAddress, (int)WirelessPort);
            WirelessStatusMessage = result;
            
            await RefreshDevicesAsync();
        }

        private async Task WirelessPairAsync()
        {
            if (string.IsNullOrEmpty(WirelessIpAddress) || string.IsNullOrEmpty(PairingCode))
            {
                WirelessStatusMessage = "Please enter IP Address and Pairing Code";
                return;
            }

            WirelessStatusMessage = "Pairing...";
            var result = await _adbService.PairWirelessAsync(WirelessIpAddress, (int)WirelessPort, PairingCode);
            WirelessStatusMessage = result;
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
