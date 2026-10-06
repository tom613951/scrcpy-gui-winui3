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

        public async Task<string> ExecuteCommandAsync(string arguments, int timeoutMs = 10000)
        {
            var argsList = ParseCommandLine(arguments);
            return await RunAdbCommandAsync(timeoutMs, argsList);
        }

        public static string[] ParseCommandLine(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return Array.Empty<string>();

            var list = new List<string>();
            var current = new StringBuilder();
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool escaping = false;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                if (escaping)
                {
                    current.Append(c);
                    escaping = false;
                    continue;
                }

                if (c == '\\' && !inSingleQuote)
                {
                    escaping = true;
                    continue;
                }

                if (c == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    continue;
                }

                if (c == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inSingleQuote && !inDoubleQuote)
                {
                    if (current.Length > 0)
                    {
                        list.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
            {
                list.Add(current.ToString());
            }

            return list.ToArray();
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
            // Try query devices directly first for snappy UI response
            var result = await RunAdbCommandDetailedAsync(5000, "devices", "-l");
            if (IsRecoverableAdbFailure(result))
            {
                KillResidualProcesses();
                await Task.Delay(800);
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

        public async Task<string> CaptureScreenAsBase64Async(string serial)
        {
            if (!File.Exists(_pathService.AdbPath)) return string.Empty;

            var tempPath = Path.Combine(Path.GetTempPath(), $"scrcpy_screencap_{Guid.NewGuid()}.png");
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _pathService.AdbPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                startInfo.ArgumentList.Add("-s");
                startInfo.ArgumentList.Add(serial);
                startInfo.ArgumentList.Add("exec-out");
                startInfo.ArgumentList.Add("screencap");
                startInfo.ArgumentList.Add("-p");

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                using (var timeout = new System.Threading.CancellationTokenSource(10000))
                {
                    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await process.StandardOutput.BaseStream.CopyToAsync(fs, timeout.Token);
                    }
                    await process.WaitForExitAsync(timeout.Token);
                }

                if (File.Exists(tempPath))
                {
                    var fileInfo = new FileInfo(tempPath);
                    if (fileInfo.Length > 100)
                    {
                        var bytes = await File.ReadAllBytesAsync(tempPath);
                        return Convert.ToBase64String(bytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error capturing screen: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
            
            return string.Empty;
        }

        public async Task<(int width, int height)?> GetScreenResolutionAsync(string serial)
        {
            var result = await RunAdbCommandDetailedAsync(5000, "-s", serial, "shell", "wm", "size");
            var output = result.Stdout;
            if (string.IsNullOrWhiteSpace(output)) return null;

            var match = Regex.Match(output, @"size:\s*(\d+)x(\d+)", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int width) && int.TryParse(match.Groups[2].Value, out int height))
            {
                return (width, height);
            }
            return null;
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

            string configuredAdbPath;
            try
            {
                configuredAdbPath = Path.GetFullPath(_pathService.AdbPath);
            }
            catch
            {
                return;
            }

            foreach (var process in Process.GetProcessesByName("adb"))
            {
                try
                {
                    string? processPath = null;
                    try
                    {
                        processPath = process.MainModule?.FileName;
                    }
                    catch
                    {
                        // Ignore permission/architecture mismatches
                    }

                    if (processPath != null && !string.Equals(processPath, configuredAdbPath, StringComparison.OrdinalIgnoreCase))
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
            // Increase timeout for apk installation, support reinstall with -r
            return await RunAdbCommandAsync(60000, "-s", serial, "install", "-r", apkFilePath);
        }

        public async Task<string> GetAdbVersionAsync()
        {
            var result = await RunAdbCommandDetailedAsync(3000, "version");
            return result.Stdout;
        }
    }
}
