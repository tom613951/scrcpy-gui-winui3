using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
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

        private async Task<string> RunAdbCommandAsync(int timeoutMs, params string[] arguments)
        {
            var result = await RunAdbCommandDetailedAsync(timeoutMs, arguments);
            return result.CombinedOutput;
        }

        private async Task<AdbCommandResult> RunAdbCommandDetailedAsync(int timeoutMs, params string[] arguments)
        {
            if (!File.Exists(_pathService.AdbPath))
            {
                return AdbCommandResult.FromError(arguments, "Error: adb.exe not found.");
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _pathService.AdbPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                foreach (var argument in arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }

                using var process = new Process
                {
                    StartInfo = startInfo
                };

                process.Start();

                var readOutputTask = process.StandardOutput.ReadToEndAsync();
                var readErrorTask = process.StandardError.ReadToEndAsync();

                try
                {
                    using var timeout = new System.Threading.CancellationTokenSource(timeoutMs);
                    await process.WaitForExitAsync(timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                        await process.WaitForExitAsync();
                    }
                    catch
                    {
                        // The process may have exited between the timeout and Kill().
                    }

                    var command = string.Join(" ", arguments.Select(QuoteForLog));
                    return AdbCommandResult.FromError(arguments, $"Error: adb command timed out after {timeoutMs / 1000.0:0.#}s: adb {command}");
                }

                var output = (await readOutputTask).Trim();
                var error = (await readErrorTask).Trim();
                return new AdbCommandResult(arguments, output, error, process.ExitCode);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error running ADB command: {ex.Message}");
                return AdbCommandResult.FromError(arguments, $"Error running ADB command: {ex.Message}");
            }
        }

        private static string QuoteForLog(string value)
        {
            return value.Any(char.IsWhiteSpace) ? $"\"{value.Replace("\"", "\\\"")}\"" : value;
        }

        public async Task<List<AdbDevice>> GetDevicesAsync()
        {
            var devices = new List<AdbDevice>();
            await StartServerWithRecoveryAsync();
            var result = await RunAdbCommandDetailedAsync(8000, "devices", "-l");
            if (IsRecoverableAdbFailure(result))
            {
                KillResidualProcesses();
                await Task.Delay(1200);
                await StartServerWithRecoveryAsync();
                result = await RunAdbCommandDetailedAsync(8000, "devices", "-l");
            }

            var output = result.Stdout;

            if (string.IsNullOrEmpty(output) || IsAdbErrorOutput(output))
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

                    if (!IsDeviceStatus(status))
                    {
                        continue;
                    }

                    var listedModel = ExtractListedModel(line);
                    deviceTasks.Add(GetDeviceDetailsAsync(serial, status, listedModel));
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

        private async Task<AdbDevice?> GetDeviceDetailsAsync(string serial, string status, string? listedModel)
        {
            var device = new AdbDevice
            {
                Serial = serial,
                Model = string.IsNullOrWhiteSpace(listedModel) ? "未知设备" : listedModel,
                Status = status,
                ConnectionType = (serial.Contains('.') || serial.Contains(':')) ? "Wireless" : "USB"
            };

            if (status.Equals("device", StringComparison.OrdinalIgnoreCase))
            {
                // Query device model
                if (string.IsNullOrWhiteSpace(listedModel))
                {
                    var modelResult = await RunAdbCommandDetailedAsync(1000, "-s", serial, "shell", "getprop", "ro.product.model");
                    if (!string.IsNullOrEmpty(modelResult.Stdout) && !IsAdbErrorOutput(modelResult.Stdout))
                    {
                        device.Model = modelResult.Stdout.Trim();
                    }
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

        private static string? ExtractListedModel(string deviceLine)
        {
            var match = Regex.Match(deviceLine, @"(?:^|\s)model:([^\s]+)");
            return match.Success ? match.Groups[1].Value.Replace('_', ' ') : null;
        }

        private static bool IsAdbErrorOutput(string output)
        {
            return output.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)
                || output.StartsWith("Error running ADB command:", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDeviceStatus(string status)
        {
            return status.Equals("device", StringComparison.OrdinalIgnoreCase)
                || status.Equals("unauthorized", StringComparison.OrdinalIgnoreCase)
                || status.Equals("offline", StringComparison.OrdinalIgnoreCase)
                || status.Equals("recovery", StringComparison.OrdinalIgnoreCase)
                || status.Equals("sideload", StringComparison.OrdinalIgnoreCase)
                || status.Equals("bootloader", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class AdbCommandResult
        {
            public AdbCommandResult(string[] arguments, string stdout, string stderr, int? exitCode)
            {
                Arguments = arguments;
                Stdout = stdout;
                Stderr = stderr;
                ExitCode = exitCode;
            }

            public string[] Arguments { get; }
            public string Stdout { get; }
            public string Stderr { get; }
            public int? ExitCode { get; }

            public string CombinedOutput
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(Stderr))
                    {
                        return Stdout;
                    }

                    if (string.IsNullOrWhiteSpace(Stdout))
                    {
                        return Stderr;
                    }

                    return $"{Stdout}{Environment.NewLine}{Stderr}";
                }
            }

            public static AdbCommandResult FromError(string[] arguments, string error)
            {
                return new AdbCommandResult(arguments, string.Empty, error, null);
            }
        }

        public async Task<string> ConnectWirelessAsync(string ipAddress, int port = 5555)
        {
            var output = await RunAdbCommandAsync(5000, "connect", $"{ipAddress}:{port}");
            return output.Trim();
        }

        public async Task<string> PairWirelessAsync(string ipAddress, int port, string pairingCode)
        {
            var output = await RunAdbCommandAsync(5000, "pair", $"{ipAddress}:{port}", pairingCode);
            return output.Trim();
        }

        public async Task<string> KillServerAsync()
        {
            return await RunAdbCommandAsync(5000, "kill-server");
        }

        public async Task StopServerAsync()
        {
            await RunAdbCommandDetailedAsync(5000, "kill-server");
            KillResidualProcesses();
        }

        public void KillResidualProcesses()
        {
            if (string.IsNullOrWhiteSpace(_pathService.AdbPath))
            {
                return;
            }

            var configuredAdbPath = Path.GetFullPath(_pathService.AdbPath);
            foreach (var process in Process.GetProcessesByName("adb"))
            {
                try
                {
                    var processPath = process.MainModule?.FileName;
                    if (!string.Equals(processPath, configuredAdbPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(2000);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to kill residual adb process: {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        public async Task<string> StartServerAsync()
        {
            var result = await StartServerWithRecoveryAsync();
            return result.CombinedOutput;
        }

        private async Task<AdbCommandResult> StartServerWithRecoveryAsync()
        {
            var result = await RunAdbCommandDetailedAsync(12000, "start-server");
            if (!IsRecoverableAdbFailure(result))
            {
                return result;
            }

            KillResidualProcesses();
            await Task.Delay(1500);

            return await RunAdbCommandDetailedAsync(12000, "start-server");
        }

        private static bool IsRecoverableAdbFailure(AdbCommandResult result)
        {
            var output = result.CombinedOutput;
            return result.ExitCode != 0
                || output.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                || output.Contains("failed to read response from server", StringComparison.OrdinalIgnoreCase)
                || output.Contains("protocol fault", StringComparison.OrdinalIgnoreCase)
                || output.Contains("connection reset", StringComparison.OrdinalIgnoreCase)
                || output.Contains("cannot connect to daemon", StringComparison.OrdinalIgnoreCase)
                || output.Contains("failed to start daemon", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> PushFileAsync(string serial, string localFilePath, string remotePath = "/sdcard/Download/")
        {
            // Increase timeout for file transfers (e.g., 60 seconds)
            return await RunAdbCommandAsync(60000, "-s", serial, "push", localFilePath, remotePath);
        }

        public async Task<string> InstallApkAsync(string serial, string apkFilePath)
        {
            // Increase timeout for apk installation (e.g., 60 seconds)
            return await RunAdbCommandAsync(60000, "-s", serial, "install", apkFilePath);
        }
    }
}
