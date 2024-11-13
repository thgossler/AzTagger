using System;
using System.IO;
using System.Text.Json;

namespace AzTagger
{
    public class Settings
    {
        public string SelectedTenantId { get; set; }
        public string AzureEnvironment { get; set; }

        private static readonly string SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzTagger", "settings.json");

        public static Settings Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                var settingsJson = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<Settings>(settingsJson);
            }
            return new Settings();
        }

        public void Save()
        {
            var settingsJson = JsonSerializer.Serialize(this);
            File.WriteAllText(SettingsFilePath, settingsJson);
        }
    }
}
