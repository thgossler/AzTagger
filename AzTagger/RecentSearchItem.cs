// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

namespace AzTagger;

public class RecentSearchItem
{
    public RecentSearchItem(string queryText)
    {
        var displayText = queryText.Replace("\r\n", " ").Replace("\n", " ");
        DisplayText = displayText;
        ActualText = queryText;
    }

    public string DisplayText { get; set; }
    public string ActualText { get; set; }

    public override string ToString() => DisplayText;
}
