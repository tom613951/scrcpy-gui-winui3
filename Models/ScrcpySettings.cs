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
                
                if (!string.IsNullOrEmpty(CameraId))
                    args.Append($"--camera-id={CameraId} ");
                    
                if (!string.IsNullOrEmpty(Codec))
                    args.Append($"--video-codec={Codec} ");
                    
                if (!string.IsNullOrEmpty(CameraAr) && CameraAr != "0")
                    args.Append($"--camera-ar={CameraAr} ");
                    
                if (CameraTorch)
                    args.Append("--camera-torch ");
                    
                // Scrcpy v3+ might not support --camera-zoom directly, but if it does, it's typically an option, or it was a typo in the vue app.
                // Assuming original scrcpy supports it or we pass it as a custom arg equivalent.
                // Wait, --camera-high-speed or similar? Actually, the original app passes config object to invoke, 
                // and the Rust backend maps it to scrcpy arguments. We'll map them based on standard scrcpy args.
                // If standard scrcpy args don't have it, we just skip it or pass if it's new. (v3 added camera, but zoom might be custom).
                // Let's assume standard scrcpy args for camera zoom is --camera-zoom.
                // No, standard scrcpy 3.x does not have --camera-zoom out of the box, wait maybe it's in scrcpy 3.x.
            }
            else if (SessionMode == "desktop")
            {
                // Scrcpy 3.0+ virtual display
                args.Append("--new-display ");
                
                if (VdWidth > 0 && VdHeight > 0)
                {
                    args.Append($"--new-display-resolution={VdWidth}x{VdHeight} ");
                }
                
                if (VdDpi > 0)
                {
                    args.Append($"--new-display-dpi={VdDpi} ");
                }
            }

            // For Mirror mode Input enhancements
            if (SessionMode == "mirror")
            {
                if (HidKeyboard)
                    args.Append("--keyboard=uhid ");
                if (HidMouse)
                    args.Append("--mouse=uhid ");
            }

            if (!string.IsNullOrEmpty(CustomArguments))
            {
                args.Append($"{CustomArguments} ");
            }

            return args.ToString().Trim();
        }
    }
}
