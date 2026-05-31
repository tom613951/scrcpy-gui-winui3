using System;

namespace ScrcpyGui.Models
{
    public class ScrcpySettings
    {
        public int MaxSize { get; set; } = 0; // 0 = native
        public string Bitrate { get; set; } = "8M"; // 2M, 4M, 8M, 16M
        public int MaxFps { get; set; } = 0; // 0 = unlimited
        public bool DisableAudio { get; set; } = false;
        public bool StayAwake { get; set; } = false;
        public bool TurnScreenOff { get; set; } = false;
        public bool RecordScreen { get; set; } = false;
        public string RecordFormat { get; set; } = "mp4"; // mp4, mkv
        public bool OtgMode { get; set; } = false;
        public bool KeepOnTop { get; set; } = false;
        public bool Fullscreen { get; set; } = false;
        public string CustomArguments { get; set; } = string.Empty;
        
        // Custom binary path settings
        public string CustomScrcpyPath { get; set; } = string.Empty;
        public bool UseCustomScrcpyPath { get; set; } = false;

        public string GetArguments(string serial)
        {
            var args = new System.Text.StringBuilder();
            
            if (!string.IsNullOrEmpty(serial))
            {
                args.Append($"-s \"{serial}\" ");
            }

            if (OtgMode)
            {
                args.Append("--otg ");
                return args.ToString().Trim(); // OTG mode overrides normal video/audio mirroring
            }

            if (MaxSize > 0)
            {
                args.Append($"-m {MaxSize} ");
            }

            if (!string.IsNullOrEmpty(Bitrate))
            {
                args.Append($"-b {Bitrate} ");
            }

            if (MaxFps > 0)
            {
                args.Append($"--max-fps {MaxFps} ");
            }

            if (DisableAudio)
            {
                args.Append("--no-audio ");
            }

            if (StayAwake)
            {
                args.Append("-w ");
            }

            if (TurnScreenOff)
            {
                args.Append("-S ");
            }

            if (KeepOnTop)
            {
                args.Append("--always-on-top ");
            }

            if (Fullscreen)
            {
                args.Append("-f ");
            }

            if (RecordScreen)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var userVideosDir = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                var recordPath = System.IO.Path.Combine(userVideosDir, $"scrcpy_record_{timestamp}.{RecordFormat}");
                args.Append($"--record \"{recordPath}\" ");
            }

            if (!string.IsNullOrEmpty(CustomArguments))
            {
                args.Append($"{CustomArguments} ");
            }

            return args.ToString().Trim();
        }
    }
}
