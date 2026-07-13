using System;
using System.Globalization;

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
        public bool IgnoreEncoderConstraints { get; set; } = false;
        
        public string CustomScrcpyPath { get; set; } = string.Empty;
        public string CustomAdbPath { get; set; } = string.Empty;

        // Session Mode: "mirror", "camera", "desktop"
        public string SessionMode { get; set; } = "mirror";

        // Input Enhancements
        public bool HidKeyboard { get; set; } = false;
        public bool HidMouse { get; set; } = false;

        // Camera Mode Settings
        public string CameraId { get; set; } = string.Empty;
        public string Codec { get; set; } = string.Empty; // e.g. h264, h265, av1
        public string CameraAr { get; set; } = string.Empty; // e.g. 16:9, 4:3
        public bool CameraTorch { get; set; } = false;
        public double CameraZoom { get; set; } = 1.0;

        // Desktop Mode Settings
        public int VdWidth { get; set; } = 1920;
        public int VdHeight { get; set; } = 1080;
        public int VdDpi { get; set; } = 420;
        public bool FlexDisplay { get; set; } = false;
        public string BackgroundColor { get; set; } = string.Empty;

        // AI Copilot Settings
        public string AiBaseUrl { get; set; } = string.Empty;
        public string AiApiKey { get; set; } = string.Empty;
        public string AiModelName { get; set; } = string.Empty;

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

            if (SessionMode == "camera")
            {
                args.Append("--video-source=camera ");
                
                // Set default orientation for camera mode so portrait holding gives upright picture
                args.Append("--orientation=90 ");
                
                if (!string.IsNullOrEmpty(CameraId))
                    args.Append($"--camera-id={CameraId} ");
                    
                    
                if (!string.IsNullOrEmpty(CameraAr) && CameraAr != "0")
                    args.Append($"--camera-ar={CameraAr} ");
                    
                if (CameraTorch)
                    args.Append("--camera-torch ");

                if (CameraZoom > 0 && Math.Abs(CameraZoom - 1.0) > 0.001)
                    args.Append($"--camera-zoom={CameraZoom.ToString("0.###", CultureInfo.InvariantCulture)} ");
            }
            else if (SessionMode == "desktop")
            {
                // Scrcpy 3.0+ virtual display uses --new-display[=<width>x<height>[/<dpi>]]
                if (VdWidth > 0 && VdHeight > 0)
                {
                    if (VdDpi > 0)
                    {
                        args.Append($"--new-display={VdWidth}x{VdHeight}/{VdDpi} ");
                    }
                    else
                    {
                        args.Append($"--new-display={VdWidth}x{VdHeight} ");
                    }
                }
                else
                {
                    if (VdDpi > 0)
                    {
                        args.Append($"--new-display=/{VdDpi} ");
                    }
                    else
                    {
                        args.Append("--new-display ");
                    }
                }

                if (FlexDisplay)
                    args.Append("--flex-display ");

                var backgroundColor = BackgroundColor.Trim();
                if (IsValidHexColor(backgroundColor))
                    args.Append($"--background-color={backgroundColor} ");
            }

            // For Mirror mode Input enhancements
            if (SessionMode == "mirror")
            {
                if (HidKeyboard)
                    args.Append("--keyboard=uhid ");
                if (HidMouse)
                    args.Append("--mouse=uhid ");
            }

            if (!string.IsNullOrEmpty(Codec))
            {
                args.Append($"--video-codec={Codec} ");
            }

            if (IgnoreEncoderConstraints)
            {
                args.Append("--ignore-video-encoder-constraints ");
            }

            if (!string.IsNullOrEmpty(CustomArguments))
            {
                args.Append($"{CustomArguments} ");
            }

            return args.ToString().Trim();
        }

        private static bool IsValidHexColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var start = value[0] == '#' ? 1 : 0;
            var length = value.Length - start;
            if (length != 3 && length != 6)
            {
                return false;
            }

            for (var i = start; i < value.Length; i++)
            {
                if (!Uri.IsHexDigit(value[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
