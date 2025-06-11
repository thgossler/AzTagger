// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AzTagger.Models;

public class TagTemplate
{
    public string TemplateName { get; set; }
    public Dictionary<string, string> Tags { get; set; }
}
