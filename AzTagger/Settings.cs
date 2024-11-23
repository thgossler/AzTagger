// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzTagger;

public class Settings
{
    [JsonPropertyName("AzureEnvironment")]
    public string AzureEnvironment { get; set; } = "AzurePublicCloud";

    [JsonPropertyName("ClientAppId")]
    public string ClientAppId { get; set; } = "5221c0b8-f9e6-4663-ac3c-5baf539290dc";

    [JsonPropertyName("TenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("RecentSearches")]
    public List<string> RecentSearches { get; set; } = new List<string>();

    [JsonPropertyName("SavedSearches")]
    public List<SavedSearchItem> SavedSearches { get; set; } = new List<SavedSearchItem>();

    [JsonIgnore]
    public static readonly string SettingsFilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzTagger", "settings.json");

    public static Settings Load()
    {
        var settings = new Settings();

        if (File.Exists(SettingsFilePath))
        {
            var settingsJson = File.ReadAllText(SettingsFilePath);
            settings = JsonSerializer.Deserialize<Settings>(settingsJson);
        }
        else
        {
            settings.RecentSearches.Add("test");
            settings.RecentSearches.Add("| where ResourceGroup contains 'test'");
            settings.RecentSearches.Add("| where SubscriptionName matches regex 'Sandbox$'");
            settings.RecentSearches.Add("| where ResourceTags['SubjectForDeletion'] =~ 'suspected'");
            settings.RecentSearches.Add("| where SubscriptionTags['Owner'] =~ 'Finance' and ResourceGroupTags['Purpose'] contains 'Reporting'");
            settings.RecentSearches.Add("| where SubscriptionName =~ 'Prototypes'\r\n| where ResourceGroup =~ 'WebApp'\r\n| where ResourceName =~ 'webappstore'");
        }

        settings.RemoveDuplicatesFromRecentSearches();

        if (string.IsNullOrWhiteSpace(settings.TenantId))
        {
            UpdateTenantIdFromLocalDevSettings(settings);
        }

        return settings;
    }

    public void RemoveDuplicatesFromRecentSearches()
    {
        var distinctRecentSearches = new List<string>();
        foreach (var recentSearch in RecentSearches)
        {
            if (!distinctRecentSearches.Any(x => string.Equals(x, recentSearch, StringComparison.OrdinalIgnoreCase)))
            {
                distinctRecentSearches.Add(recentSearch);
            }
        }
        RecentSearches = distinctRecentSearches;
    }

    private static void UpdateTenantIdFromLocalDevSettings(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(Environment.CurrentDirectory))
        {
            return;
        }
        var currentDir = Environment.CurrentDirectory;
        var projectDir = ".";
        if (currentDir.IndexOf("bin") > 0)
        {
            projectDir = currentDir.Substring(0, currentDir.IndexOf("bin"));
        }
        var localSettingsFilePath = Path.Combine(projectDir, "local.settings.json");
        if (File.Exists(localSettingsFilePath))
        {
            var localSettingsJson = File.ReadAllText(localSettingsFilePath);
            var localSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(localSettingsJson);
            if (localSettings.ContainsKey("TenantId"))
            {
                settings.TenantId = localSettings["TenantId"];
            }
        }
        else
        {
            settings.TenantId = string.Empty;
        }
    }

    public void Save()
    {
        RemoveDuplicatesFromRecentSearches();
        var settingsJson = JsonSerializer.Serialize(this,
            new JsonSerializerOptions
            {
                IndentSize = 2,
                WriteIndented = true
            });
        File.WriteAllText(SettingsFilePath, settingsJson);
    }
}
