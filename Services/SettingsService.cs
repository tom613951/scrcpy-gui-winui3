using System;
using System.IO;
using System.Text.Json;
using ScrcpyGui.Models;

namespace ScrcpyGui.Services
{
    public class SettingsService
    {
        private readonly PathService _pathService;
        private readonly string _settingsFilePath;

        public ScrcpySettings Settings { get; private set; }

        public SettingsService(PathService pathService)
        {
            _pathService = pathService;
            
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ScrcpyGui"
            );
            Directory.CreateDirectory(folder);
            _settingsFilePath = Path.Combine(folder, "settings.json");

            Settings = LoadSettings();
            ApplySettings();
        }

        private ScrcpySettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<ScrcpySettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }

            return new ScrcpySettings();
        }

        public void SaveSettings()
        {
            try
            {
                ApplySettings();
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void ApplySettings()
        {
            _pathService.ScrcpyDirectory = Settings.CustomScrcpyPath;
            _pathService.CustomAdbPath = Settings.CustomAdbPath;
        }
    }
}
