using System;
using System.IO;

namespace ScrcpyGui.Services
{
    public class PathService
    {
        // For a portable app, store settings in the same directory as the executable on disk.
        // When running from a temporary directory (e.g. 7z-SFX single-file), persist to %APPDATA%\ScrcpyGui.
        public static string AppDataDirectory
        {
            get
            {
                var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
                var dir = (!string.IsNullOrEmpty(exeDir) && Directory.Exists(exeDir))
                    ? exeDir
                    : AppDomain.CurrentDomain.BaseDirectory;

                var tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (dir.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase))
                {
                    var roamingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScrcpyGui");
                    Directory.CreateDirectory(roamingDir);
                    return roamingDir;
                }

                return dir;
            }
        }

        public static string LogsDirectory => Path.Combine(AppDataDirectory, "logs");

        public static string CrashLogPath => Path.Combine(LogsDirectory, "crash.log");

        public string ScrcpyDirectory { get; set; } = string.Empty;
        public string CustomAdbPath { get; set; } = string.Empty;

        public string AdbPath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CustomAdbPath) && File.Exists(CustomAdbPath))
                {
                    return CustomAdbPath;
                }

                if (!string.IsNullOrWhiteSpace(ScrcpyDirectory))
                {
                    var scrcpyAdb = Path.Combine(ScrcpyDirectory, "adb.exe");
                    if (File.Exists(scrcpyAdb))
                    {
                        return scrcpyAdb;
                    }
                }

                // Check application base directory
                var appDirAdb = Path.Combine(AppDataDirectory, "adb.exe");
                if (File.Exists(appDirAdb))
                {
                    return appDirAdb;
                }

                // Check system PATH
                var pathAdb = FindExecutableInPath("adb.exe");
                if (!string.IsNullOrEmpty(pathAdb))
                {
                    return pathAdb;
                }

                return !string.IsNullOrWhiteSpace(ScrcpyDirectory) ? Path.Combine(ScrcpyDirectory, "adb.exe") : "adb.exe";
            }
        }

        public string ScrcpyPath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ScrcpyDirectory))
                {
                    var scrcpyExe = Path.Combine(ScrcpyDirectory, "scrcpy.exe");
                    if (File.Exists(scrcpyExe))
                    {
                        return scrcpyExe;
                    }
                }

                // Check application base directory
                var appDirScrcpy = Path.Combine(AppDataDirectory, "scrcpy.exe");
                if (File.Exists(appDirScrcpy))
                {
                    return appDirScrcpy;
                }

                // Check system PATH
                var pathScrcpy = FindExecutableInPath("scrcpy.exe");
                if (!string.IsNullOrEmpty(pathScrcpy))
                {
                    return pathScrcpy;
                }

                return !string.IsNullOrWhiteSpace(ScrcpyDirectory) ? Path.Combine(ScrcpyDirectory, "scrcpy.exe") : "scrcpy.exe";
            }
        }

        public bool BinariesExist =>
            !string.IsNullOrWhiteSpace(AdbPath) && File.Exists(AdbPath) &&
            !string.IsNullOrWhiteSpace(ScrcpyPath) && File.Exists(ScrcpyPath);

        private static string? FindExecutableInPath(string exeName)
        {
            try
            {
                var pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (string.IsNullOrWhiteSpace(pathEnv)) return null;

                var paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in paths)
                {
                    var fullPath = Path.Combine(p.Trim('\"'), exeName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        public static void WriteCrashLog(string content)
        {
            try
            {
                Directory.CreateDirectory(LogsDirectory);
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {content}{Environment.NewLine}----------------------------------------{Environment.NewLine}";
                File.AppendAllText(CrashLogPath, logEntry);
            }
            catch
            {
            }
        }
    }
}
