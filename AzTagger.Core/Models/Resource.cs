// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace AzTagger.Models;

public class Resource
{
    public string EntityType { get; set; }
    public string Id { get; set; }
    public string SubscriptionName { get; set; }
    public string SubscriptionId { get; set; }
    public string ResourceGroup { get; set; }
    public string ResourceName { get; set; }
    public string ResourceType { get; set; }
    public IDictionary<string, string> SubscriptionTags { get; set; }
    public IDictionary<string, string> ResourceGroupTags { get; set; }
    public IDictionary<string, string> ResourceTags { get; set; }
    public IDictionary<string, string> CombinedTags { get; set; }

    public string CombinedTagsFormatted
    {
        get
        {
            if (CombinedTags == null || CombinedTags.Count == 0)
                return string.Empty;
            return string.Join(", ", CombinedTags
                .OrderBy(k => k.Key)
                .Select(kv => $"{kv.Key}={kv.Value}"));
        }
    }
}
