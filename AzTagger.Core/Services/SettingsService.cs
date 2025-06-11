// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using AzTagger.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AzTagger.Services;

public class SettingsService
{
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

        settings.SanitizeAzureContextsSetting();
        var selectedAzureContext = settings.AzureContexts.FirstOrDefault(x => x.Name == settings.SelectedAzureContext);
        if (selectedAzureContext == null)
        {
            if (settings.AzureContexts.Count > 0)
            {
                selectedAzureContext = settings.AzureContexts[0];
                settings.SelectedAzureContext = selectedAzureContext.Name;
            }
            else
            {
                throw new Exception("The selected Azure context could not be determined and a default context could not be set.");
            }
        }
        if (string.IsNullOrWhiteSpace(selectedAzureContext.TenantId))
        {
            TryUpdatingTenantIdFromLocalDevSettings(settings);
        }

        settings.RemoveDuplicatesFromRecentSearches();

        return settings;
    }

    private static void TryUpdatingTenantIdFromLocalDevSettings(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(Environment.CurrentDirectory))
        {
            return;
        }
        var azureContext = settings.AzureContexts?.FirstOrDefault();
        if (azureContext == null || !string.IsNullOrEmpty(azureContext.TenantId))
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
                azureContext.TenantId = localSettings["TenantId"];
            }
        }
    }

    public static void Save(Settings settings)
    {
        settings.RemoveDuplicatesFromRecentSearches();
        var settingsJson = JsonSerializer.Serialize(settings,
            new JsonSerializerOptions
            {
                IndentSize = 2,
                WriteIndented = true
            });

        var settingsDir = Path.GetDirectoryName(SettingsFilePath);
        if (!Directory.Exists(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }
        File.WriteAllText(SettingsFilePath, settingsJson);
    }
}

