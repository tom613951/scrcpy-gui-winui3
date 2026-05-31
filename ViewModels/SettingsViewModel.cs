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

        public bool UseCustomScrcpyPath
        {
            get => Settings.UseCustomScrcpyPath;
            set
            {
                if (Settings.UseCustomScrcpyPath != value)
                {
                    Settings.UseCustomScrcpyPath = value;
                    OnPropertyChanged();
                    _settingsService.SaveSettings();
                    OnPropertyChanged(nameof(CurrentScrcpyDirectory));
                }
            }
        }

        public string CurrentScrcpyDirectory => _pathService.ScrcpyDirectory;
    }
}
