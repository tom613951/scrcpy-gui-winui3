using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScrcpyGui.Models;

namespace ScrcpyGui.Services
{
    public class AdbService
    {
        private readonly PathService _pathService;

        public AdbService(PathService pathService)
        {
            _pathService = pathService;
        }

        private async Task<string> RunAdbCommandAsync(string arguments, int timeoutMs = 2000)
        {
            if (!File.Exists(_pathService.AdbPath))
            {
                return string.Empty;
            }

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _pathService.AdbPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                var readOutputTask = process.StandardOutput.ReadToEndAsync();
                var readErrorTask = process.StandardError.ReadToEndAsync();

                var completedTask = await Task.WhenAny(
                    Task.Delay(timeoutMs),
                    Task.WhenAll(readOutputTask, readErrorTask)
                );

                if (completedTask == readOutputTask || completedTask.Status == TaskStatus.RanToCompletion)
                {
                    // Process finished within timeout
                    await process.WaitForExitAsync();
                    return await readOutputTask;
                }
                else
                {
                    // Timeout occurred
                    try { process.Kill(); } catch { }
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error running ADB command: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<List<AdbDevice>> GetDevicesAsync()
        {
            var devices = new List<AdbDevice>();
            var output = await RunAdbCommandAsync("devices");

            if (string.IsNullOrEmpty(output))
            {
                return devices;
            }

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var deviceTasks = new List<Task<AdbDevice?>>();

            foreach (var line in lines)
            {
                // Skip header line
                if (line.StartsWith("List of devices")) continue;

                var match = Regex.Match(line, @"^([^\s]+)\s+([^\s]+)");
                if (match.Success)
                {
                    var serial = match.Groups[1].Value;
                    var status = match.Groups[2].Value;

                    deviceTasks.Add(GetDeviceDetailsAsync(serial, status));
                }
            }

            var results = await Task.WhenAll(deviceTasks);
            foreach (var dev in results)
            {
                if (dev != null)
                {
                    devices.Add(dev);
                }
            }

            return devices;
        }

        private async Task<AdbDevice?> GetDeviceDetailsAsync(string serial, string status)
        {
            var device = new AdbDevice
            {
                Serial = serial,
                Status = status,
                ConnectionType = (serial.Contains('.') || serial.Contains(':')) ? "Wireless" : "USB"
            };

            if (status.Equals("device", StringComparison.OrdinalIgnoreCase))
            {
                // Query device model
                var modelOutput = await RunAdbCommandAsync($"-s {serial} shell getprop ro.product.model", 1000);
                if (!string.IsNullOrEmpty(modelOutput))
                {
                    device.Model = modelOutput.Trim();
                }
            }
            else if (status.Equals("unauthorized", StringComparison.OrdinalIgnoreCase))
            {
                device.Model = "未授权设备";
            }
            else
            {
                device.Model = "离线设备";
            }

            return device;
        }

        public async Task<string> ConnectWirelessAsync(string ipAddress, int port = 5555)
        {
            var output = await RunAdbCommandAsync($"connect {ipAddress}:{port}", 5000);
            return output.Trim();
        }

        public async Task<string> PairWirelessAsync(string ipAddress, int port, string pairingCode)
        {
            var output = await RunAdbCommandAsync($"pair {ipAddress}:{port} {pairingCode}", 5000);
            return output.Trim();
        }

        public async Task<string> KillServerAsync()
        {
            return await RunAdbCommandAsync("kill-server");
        }

        public async Task<string> StartServerAsync()
        {
            return await RunAdbCommandAsync("start-server");
        }

        public async Task<string> PushFileAsync(string serial, string localFilePath, string remotePath = "/sdcard/Download/")
        {
            // Increase timeout for file transfers (e.g., 60 seconds)
            return await RunAdbCommandAsync($"-s {serial} push \"{localFilePath}\" \"{remotePath}\"", 60000);
        }

        public async Task<string> InstallApkAsync(string serial, string apkFilePath)
        {
            // Increase timeout for apk installation (e.g., 60 seconds)
            return await RunAdbCommandAsync($"-s {serial} install \"{apkFilePath}\"", 60000);
        }
    }
}
