// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

namespace AzTagger;

public class SavedQuery
{
    public string Name { get; set; }
    public string QueryText { get; set; }

    public SavedQuery(string name, string queryText)
    {
        Name = name;
        QueryText = queryText;
    }

    public override string ToString() => Name;
}
