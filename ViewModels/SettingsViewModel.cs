using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ScrcpyGui.Models;
using ScrcpyGui.Services;

namespace ScrcpyGui.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private readonly PathService _pathService;

        public ScrcpySettings Settings => _settingsService.Settings;

        public SettingsViewModel(SettingsService settingsService, PathService pathService)
        {
            _settingsService = settingsService;
            _pathService = pathService;
        }

        public double MaxSize
        {
            get => Settings.MaxSize;
            set
            {
                if (Settings.MaxSize != (int)value)
                {
                    Settings.MaxSize = (int)value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public string Bitrate
        {
            get => Settings.Bitrate;
            set
            {
                if (Settings.Bitrate != value)
                {
                    Settings.Bitrate = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public int BitrateIndex
        {
            get => Settings.Bitrate switch
            {
                "2M" => 0,
                "4M" => 1,
                "8M" => 2,
                "16M" => 3,
                "32M" => 4,
                _ => 2
            };
            set
            {
                string val = value switch
                {
                    0 => "2M",
                    1 => "4M",
                    2 => "8M",
                    3 => "16M",
                    4 => "32M",
                    _ => "8M"
                };
                if (Settings.Bitrate != val)
                {
                    Settings.Bitrate = val;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public double MaxFps
        {
            get => Settings.MaxFps;
            set
            {
                if (Settings.MaxFps != (int)value)
                {
                    Settings.MaxFps = (int)value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public bool DisableAudio
        {
            get => Settings.DisableAudio;
            set
            {
                if (Settings.DisableAudio != value)
                {
                    Settings.DisableAudio = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public bool StayAwake
        {
            get => Settings.StayAwake;
            set
            {
                if (Settings.StayAwake != value)
                {
                    Settings.StayAwake = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public bool TurnScreenOff
        {
            get => Settings.TurnScreenOff;
            set
            {
                if (Settings.TurnScreenOff != value)
                {
                    Settings.TurnScreenOff = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public bool RecordScreen
        {
            get => Settings.RecordScreen;
            set
            {
                if (Settings.RecordScreen != value)
                {
                    Settings.RecordScreen = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public string RecordFormat
        {
            get => Settings.RecordFormat;
            set
            {
                if (Settings.RecordFormat != value)
                {
                    Settings.RecordFormat = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public bool OtgMode
        {
            get => Settings.OtgMode;
            set
            {
                if (Settings.OtgMode != value)
                {
                    Settings.OtgMode = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public bool KeepOnTop
        {
            get => Settings.KeepOnTop;
            set
            {
                if (Settings.KeepOnTop != value)
                {
                    Settings.KeepOnTop = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public bool Fullscreen
        {
            get => Settings.Fullscreen;
            set
            {
                if (Settings.Fullscreen != value)
                {
                    Settings.Fullscreen = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public string CustomArguments
        {
            get => Settings.CustomArguments;
            set
            {
                if (Settings.CustomArguments != value)
                {
                    Settings.CustomArguments = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public string CustomScrcpyPath
        {
            get => Settings.CustomScrcpyPath;
            set
            {
                if (Settings.CustomScrcpyPath != value)
                {
                    Settings.CustomScrcpyPath = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                    OnPropertyChanged(nameof(CurrentScrcpyDirectory));
                }
            }
        }

        public string CurrentScrcpyDirectory => _pathService.ScrcpyDirectory;

        public string CustomAdbPath
        {
            get => Settings.CustomAdbPath;
            set
            {
                if (Settings.CustomAdbPath != value)
                {
                    Settings.CustomAdbPath = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                    OnPropertyChanged(nameof(CurrentAdbPath));
                }
            }
        }

        public string CurrentAdbPath => _pathService.AdbPath;

        // Session Mode
        public string SessionMode
        {
            get => Settings.SessionMode;
            set
            {
                if (Settings.SessionMode != value)
                {
                    Settings.SessionMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCameraMode));
                    OnPropertyChanged(nameof(IsDesktopMode));
                    _settingsService.SaveSettings();
                }
            }
        }

        public bool IsCameraMode => SessionMode == "camera";
        public bool IsDesktopMode => SessionMode == "desktop";

        // HID Keyboard
        public bool HidKeyboard
        {
            get => Settings.HidKeyboard;
            set
            {
                if (Settings.HidKeyboard != value)
                {
                    Settings.HidKeyboard = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        // HID Mouse
        public bool HidMouse
        {
            get => Settings.HidMouse;
            set
            {
                if (Settings.HidMouse != value)
                {
                    Settings.HidMouse = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        // Camera Id
        public string CameraId
        {
            get => Settings.CameraId;
            set
            {
                if (Settings.CameraId != value)
                {
                    Settings.CameraId = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        // Codec
        public string Codec
        {
            get => Settings.Codec;
            set
            {
                if (Settings.Codec != value)
                {
                    Settings.Codec = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        // Camera AR
        public string CameraAr
        {
            get => Settings.CameraAr;
            set
            {
                if (Settings.CameraAr != value)
                {
                    Settings.CameraAr = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        // Camera Torch
        public bool CameraTorch
        {
            get => Settings.CameraTorch;
            set
            {
                if (Settings.CameraTorch != value)
                {
                    Settings.CameraTorch = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public double CameraZoom
        {
            get => Settings.CameraZoom;
            set
            {
                var normalized = double.IsNaN(value) || value <= 0 ? 1.0 : Math.Round(value, 2);
                if (Math.Abs(Settings.CameraZoom - normalized) > 0.001)
                {
                    Settings.CameraZoom = normalized;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        // VdWidth
        public double VdWidth
        {
            get => Settings.VdWidth;
            set
            {
                if (Settings.VdWidth != (int)value)
                {
                    Settings.VdWidth = (int)value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        // VdHeight
        public double VdHeight
        {
            get => Settings.VdHeight;
            set
            {
                if (Settings.VdHeight != (int)value)
                {
                    Settings.VdHeight = (int)value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        // VdDpi
        public double VdDpi
        {
            get => Settings.VdDpi;
            set
            {
                if (Settings.VdDpi != (int)value)
                {
                    Settings.VdDpi = (int)value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        // FlexDisplay
        public bool FlexDisplay
        {
            get => Settings.FlexDisplay;
            set
            {
                if (Settings.FlexDisplay != value)
                {
                    Settings.FlexDisplay = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        // BackgroundColor
        public string BackgroundColor
        {
            get => Settings.BackgroundColor;
            set
            {
                if (Settings.BackgroundColor != value)
                {
                    Settings.BackgroundColor = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }
        // AI Settings
        public string AiBaseUrl
        {
            get => Settings.AiBaseUrl;
            set
            {
                if (Settings.AiBaseUrl != value)
                {
                    Settings.AiBaseUrl = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public string AiApiKey
        {
            get => Settings.AiApiKey;
            set
            {
                if (Settings.AiApiKey != value)
                {
                    Settings.AiApiKey = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public string AiModelName
        {
            get => Settings.AiModelName;
            set
            {
                if (Settings.AiModelName != value)
                {
                    Settings.AiModelName = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                }
            }
        }

        public class AiProviderOption
        {
            public string Name { get; set; } = string.Empty;
            public string BaseUrl { get; set; } = string.Empty;
        }

        public System.Collections.ObjectModel.ObservableCollection<AiProviderOption> AiProviders { get; } = new()
        {
            new AiProviderOption { Name = "自定义 (Custom)", BaseUrl = "" },
            new AiProviderOption { Name = "OpenAI", BaseUrl = "https://api.openai.com/v1" },
            new AiProviderOption { Name = "OpenClaw", BaseUrl = "https://api.openclaw.ai/v1" },
            new AiProviderOption { Name = "DeepSeek", BaseUrl = "https://api.deepseek.com/v1" },
            new AiProviderOption { Name = "Anthropic", BaseUrl = "https://api.anthropic.com/v1" }
        };

        private AiProviderOption? _selectedAiProvider;
        public AiProviderOption? SelectedAiProvider
        {
            get => _selectedAiProvider;
            set
            {
                if (_selectedAiProvider != value)
                {
                    _selectedAiProvider = value;
                    OnPropertyChanged();
                    if (value != null && !string.IsNullOrEmpty(value.BaseUrl))
                    {
                        AiBaseUrl = value.BaseUrl;
                    }
                }
            }
        }
    }
}
