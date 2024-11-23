// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json.Serialization;

namespace AzTagger;

public class SavedSearchItem
{
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("SearchQuery")]
    public string SearchQuery { get; set; }

    [JsonConstructor]
    public SavedSearchItem(string name, string searchQuery)
    {
        Name = name;
        SearchQuery = searchQuery;
    }

    public override string ToString() => Name;
}
