using System;
using System.IO;

namespace ScrcpyGui.Services
{
    public class PathService
    {
        public string ScrcpyDirectory { get; set; } = string.Empty;
        public string CustomAdbPath { get; set; } = string.Empty;

        public string AdbPath => !string.IsNullOrEmpty(CustomAdbPath) ? CustomAdbPath : Path.Combine(ScrcpyDirectory, "adb.exe");
        public string ScrcpyPath => Path.Combine(ScrcpyDirectory, "scrcpy.exe");

        public bool BinariesExist => File.Exists(AdbPath) && File.Exists(ScrcpyPath);
    }
}
