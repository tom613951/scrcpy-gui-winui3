using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ScrcpyGui.Models;

namespace ScrcpyGui.Services
{
    public class ScrcpyService
    {
        private readonly PathService _pathService;

        public ScrcpyService(PathService pathService)
        {
            _pathService = pathService;
        }

        public async Task<Process?> StartMirroringAsync(AdbDevice device, ScrcpySettings settings, Action<string> onOutputReceived, Action<int> onExit)
        {
            if (!File.Exists(_pathService.ScrcpyPath))
            {
                onOutputReceived?.Invoke("Error: scrcpy.exe not found! Please download scrcpy binaries first.");
                return null;
            }

            var args = settings.GetArguments(device.Serial);

            try
            {
                var workingDir = !string.IsNullOrWhiteSpace(_pathService.ScrcpyDirectory) && Directory.Exists(_pathService.ScrcpyDirectory)
                    ? _pathService.ScrcpyDirectory
                    : (Path.GetDirectoryName(_pathService.ScrcpyPath) ?? AppDomain.CurrentDomain.BaseDirectory);

                var startInfo = new ProcessStartInfo
                {
                    FileName = _pathService.ScrcpyPath,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Critical: Tell scrcpy which adb executable to use via ADB environment variable
                if (!string.IsNullOrWhiteSpace(_pathService.AdbPath) && File.Exists(_pathService.AdbPath))
                {
                    startInfo.EnvironmentVariables["ADB"] = _pathService.AdbPath;

                    // Also prepend ADB directory and scrcpy directory to PATH
                    var adbDir = Path.GetDirectoryName(_pathService.AdbPath);
                    var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                    var pathParts = new System.Collections.Generic.List<string>();
                    if (!string.IsNullOrEmpty(workingDir)) pathParts.Add(workingDir);
                    if (!string.IsNullOrEmpty(adbDir) && adbDir != workingDir) pathParts.Add(adbDir);
                    if (!string.IsNullOrEmpty(currentPath)) pathParts.Add(currentPath);
                    startInfo.EnvironmentVariables["PATH"] = string.Join(Path.PathSeparator, pathParts);
                }

                var process = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        onOutputReceived?.Invoke(e.Data);
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        onOutputReceived?.Invoke(e.Data);
                    }
                };

                process.Exited += (s, e) =>
                {
                    onExit?.Invoke(process.ExitCode);
                };

                onOutputReceived?.Invoke($"Starting mirroring for {device.Model} ({device.Serial})...");
                onOutputReceived?.Invoke($"Command: scrcpy {args}");

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                return process;
            }
            catch (Exception ex)
            {
                onOutputReceived?.Invoke($"Failed to start scrcpy: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetScrcpyVersionAsync()
        {
            if (!File.Exists(_pathService.ScrcpyPath)) return string.Empty;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _pathService.ScrcpyPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                return output.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
