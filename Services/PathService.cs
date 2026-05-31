using System;
using System.IO;

namespace ScrcpyGui.Services
{
    public class PathService
    {
        private string _customScrcpyDirectory = string.Empty;

        public string DefaultScrcpyDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScrcpyGui",
            "scrcpy-win64"
        );

        public string ScrcpyDirectory
        {
            get => string.IsNullOrEmpty(_customScrcpyDirectory) ? DefaultScrcpyDirectory : _customScrcpyDirectory;
            set => _customScrcpyDirectory = value;
        }

        public string AdbPath => Path.Combine(ScrcpyDirectory, "adb.exe");
        public string ScrcpyPath => Path.Combine(ScrcpyDirectory, "scrcpy.exe");

        public bool BinariesExist => File.Exists(AdbPath) && File.Exists(ScrcpyPath);

        public void ResetToDefault()
        {
            _customScrcpyDirectory = string.Empty;
        }
    }
}
