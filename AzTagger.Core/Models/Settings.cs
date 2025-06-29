// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;

namespace AzTagger.Models;

public class Settings
{
    static public readonly WinLocation DefaultWindowLocation = WinLocation.Empty;
    static public readonly WinSize DefaultWindowSize = new WinSize(1280, 768);
    static public readonly int DefaultSplitterPosition = 260;

    [JsonPropertyName("AzureContexts")]
    public List<AzureContext> AzureContexts { get; set; } = new List<AzureContext>();

    [JsonPropertyName("SelectedAzureContext")]
    public string SelectedAzureContext { get; set; } = string.Empty;

    [JsonPropertyName("RecentSearches")]
    public List<string> RecentSearches { get; set; } = new List<string>();

    [JsonPropertyName("SavedSearches")]
    public List<SavedSearchItem> SavedSearches { get; set; } = new List<SavedSearchItem>();

    [JsonPropertyName("WindowSize")]
    public WinSize WindowSize { get; set; } = DefaultWindowSize;

    [JsonPropertyName("WindowLocation")]
    public WinLocation WindowLocation { get; set; } = DefaultWindowLocation;

    [JsonPropertyName("SplitterPosition")]
    public int SplitterPosition { get; set; } = DefaultSplitterPosition;

    [JsonPropertyName("ColorMode")]
    public string ColorMode { get; set; } = "System";

    [JsonPropertyName("LastSearchQuery")]
    public string LastSearchQuery { get; set; } = string.Empty;

    [JsonPropertyName("LastQuickFilter1Text")]
    public string LastQuickFilter1Text { get; set; } = string.Empty;

    [JsonPropertyName("LastQuickFilter2Text")]
    public string LastQuickFilter2Text { get; set; } = string.Empty;

    public void SanitizeAzureContextsSetting()
    {
        if (AzureContexts == null || AzureContexts.Count == 0)
        {
            AddAzureContext(new AzureContext());
            SelectedAzureContext = AzureContexts[0].Name;
        }
        else
        {
            SanitizeAzureContexts(AzureContexts);
        }
    }

    public static void SanitizeAzureContexts(IList<AzureContext> azureContexts)
    {
        foreach (var azureContext in azureContexts.Reverse())
        {
            var azureContextNames = azureContexts.Select(x => x.Name).ToList();
            var azureContextName = azureContext.Name;
            var i = 1;
            while (azureContextNames.Count(x => x == azureContextName) > 1)
            {
                azureContextName = $"{azureContext.Name} ({i})";
                i++;
            }
            azureContext.Name = azureContextName;
        }
    }

    public void AddAzureContext(AzureContext azureContext)
    {
        if (azureContext == null)
        {
            throw new ArgumentNullException(nameof(azureContext));
        }
        var azureContextNames = AzureContexts.Select(x => x.Name).ToList();
        var azureContextName = azureContext.Name;
        var i = 1;
        while (azureContextNames.Contains(azureContextName))
        {
            azureContextName = $"{azureContext.Name} ({i})";
            i++;
        }
        azureContext.Name = azureContextName;

        AzureContexts.Add(azureContext);
    }

    public void SelectAzureContext(string azureContextName)
    {
        if (string.IsNullOrEmpty(azureContextName))
        {
            throw new ArgumentNullException(nameof(azureContextName));
        }
        if (!AzureContexts.Any(x => x.Name == azureContextName))
        {
            throw new Exception($"The Azure context '{azureContextName}' was not found in the configuration.");
        }
        SelectedAzureContext = azureContextName;
    }

    public AzureContext GetAzureContext(string azureContextName = "")
    {
        if (AzureContexts == null || AzureContexts.Count == 0)
        {
            AddAzureContext(new AzureContext());
            SelectedAzureContext = AzureContexts[0].Name;
        }
        if (string.IsNullOrWhiteSpace(azureContextName))
        {
            azureContextName = SelectedAzureContext;
        }
        var azureContext = AzureContexts.FirstOrDefault(x => x.Name == azureContextName);
        if (azureContext == null)
        {
            azureContext = AzureContexts.FirstOrDefault();
        }
        return azureContext;
    }

    public void ResetToWindowDefaults()
    {
        WindowLocation = DefaultWindowLocation;
        WindowSize = DefaultWindowSize;
        SplitterPosition = DefaultSplitterPosition;
        LastSearchQuery = string.Empty;
        LastQuickFilter1Text = string.Empty;
        LastQuickFilter2Text = string.Empty;
        ColorMode = "System";
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

    public class WinSize
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public WinSize(int width, int height)
        {
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
