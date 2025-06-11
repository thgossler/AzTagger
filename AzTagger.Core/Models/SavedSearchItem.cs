// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Text.Json.Serialization;

namespace AzTagger.Models;

public class SavedSearchItem : IComparable<SavedSearchItem>
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

    // Implement IComparable<SavedSearchItem>
    public int CompareTo(SavedSearchItem other)
    {
        if (other == null) return 1; // Null is considered less than any instance
        return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }
}
