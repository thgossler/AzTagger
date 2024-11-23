// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json.Serialization;

namespace AzTagger;

public class RecentSearchItem
{
    [JsonIgnore]
    public string DisplayText { get; set; }

    [JsonIgnore]
    public string ActualText { get; set; }

    [JsonConstructor]
    public RecentSearchItem(string searchQueryText)
    {
        var displayText = searchQueryText.Replace("\r\n", " ").Replace("\n", " ");
        DisplayText = displayText;
        ActualText = searchQueryText;
    }

    public override string ToString() => DisplayText;
}
