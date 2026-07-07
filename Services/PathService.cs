using System;
using System.IO;

namespace ScrcpyGui.Services
{
    public class PathService
    {
        public static string AppDataDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScrcpyGui"
        );

        public static string LogsDirectory => Path.Combine(AppDataDirectory, "logs");

        public static string CrashLogPath => Path.Combine(LogsDirectory, "crash.log");

        public string ScrcpyDirectory { get; set; } = string.Empty;
        public string CustomAdbPath { get; set; } = string.Empty;

        public string AdbPath => !string.IsNullOrEmpty(CustomAdbPath) ? CustomAdbPath : Path.Combine(ScrcpyDirectory, "adb.exe");
        public string ScrcpyPath => Path.Combine(ScrcpyDirectory, "scrcpy.exe");

        public bool BinariesExist => File.Exists(AdbPath) && File.Exists(ScrcpyPath);

        public static void WriteCrashLog(string content)
        {
            Directory.CreateDirectory(LogsDirectory);
            File.WriteAllText(CrashLogPath, content);
        }
    }
}
