using System;
using System.IO;
using System.Text.Json;
using Tascade.Models;

namespace Tascade.Services
{
    public class FileStorageService
    {
        private readonly string _appDataPath;
        private readonly string _settingsFilePath;

        public FileStorageService()
        {
            _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tascade");
            _settingsFilePath = Path.Combine(_appDataPath, "settings.json");
            
            Directory.CreateDirectory(_appDataPath);
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    var defaultSettings = new AppSettings();
                    defaultSettings.Notepads.Add(new NotepadData { Title = "Main" });
                    SaveSettings(defaultSettings);
                    return defaultSettings;
                }

                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                // Ensure at least one notepad exists
                if (settings?.Notepads == null || settings.Notepads.Count == 0)
                {
                    settings = settings ?? new AppSettings();
                    settings.Notepads.Add(new NotepadData { Title = "Main" });
                    SaveSettings(settings);
                }
                
                return settings ?? new AppSettings();
            }
            catch (Exception)
            {
                // Return default settings if loading fails
                var defaultSettings = new AppSettings();
                defaultSettings.Notepads.Add(new NotepadData { Title = "Main" });
                return defaultSettings;
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true 
                });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception)
            {
                // Handle save errors silently for now
            }
        }
    }
}
