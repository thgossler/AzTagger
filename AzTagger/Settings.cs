using System;
using System.IO;
using Newtonsoft.Json;

namespace AzTagger
{
    public class Settings
    {
        public string SelectedTenant { get; set; }
        public string SelectedEnvironment { get; set; }

        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AzTagger",
            "settings.json");

        public static Settings Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonConvert.DeserializeObject<Settings>(json);
            }
            return new Settings();
        }

        public void Save()
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
        }
    }
}
