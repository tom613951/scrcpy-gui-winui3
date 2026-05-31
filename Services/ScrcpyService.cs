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
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _pathService.ScrcpyPath,
                        Arguments = args,
                        WorkingDirectory = _pathService.ScrcpyDirectory,
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
    }
}
