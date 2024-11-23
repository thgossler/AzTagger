// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
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

    [JsonPropertyName("WindowSize")]
    public WinSize WindowSize { get; set; } = new WinSize(1500, 1000);

    [JsonPropertyName("WindowLocation")]
    public WinLocation WindowLocation { get; set; } = WinLocation.Empty;

    [JsonPropertyName("SplitterPosition")]
    public int SplitterPosition { get; set; } = 425;

    [JsonPropertyName("LastSearchQuery")]
    public string LastSearchQuery { get; set; } = string.Empty;

    [JsonPropertyName("LastQuickFilter1Text")]
    public string LastQuickFilter1Text { get; set; } = string.Empty;

    [JsonPropertyName("LastQuickFilter2Text")]
    public string LastQuickFilter2Text { get; set; } = string.Empty;

    [JsonIgnore]
    public static readonly string SettingsFilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzTagger", "settings.json");

    public void ResetToWindowDefaults()
    {
        WindowSize = new WinSize(1500, 1000);
        WindowLocation = WinLocation.Empty;
        SplitterPosition = 425;
        LastSearchQuery = string.Empty;
        LastQuickFilter1Text = string.Empty;
        LastQuickFilter2Text = string.Empty;
    }

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

    public class WinSize
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public WinSize(int width, int height) {
            Width = width;
            Height = height;
        }

        public static readonly WinSize Empty = new WinSize(0, 0);

        public static implicit operator Size(WinSize winSize)
        {
            return new Size(winSize.Width, winSize.Height);
        }

        public static implicit operator WinSize(Size size)
        {
            return new WinSize(size.Width, size.Height);
        }
    }

    public class WinLocation
    {
        public int X { get; set; }
        public int Y { get; set; }

        public WinLocation(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static readonly WinLocation Empty = new WinLocation(0, 0);

        public static implicit operator Point(WinLocation winLocation)
        {
            return new Point(winLocation.X, winLocation.Y);
        }

        public static implicit operator WinLocation(Point point)
        {
            return new WinLocation(point.X, point.Y);
        }
    }
}
