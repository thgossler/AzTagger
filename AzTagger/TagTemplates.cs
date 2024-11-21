// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AzTagger;

public class TagTemplate
{
    public string TemplateName { get; set; }
    public Dictionary<string, string> Tags { get; set; }
}

public class TagTemplates
{
    public static readonly string TagTemplatesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzTagger", "tagtemplates.json");

    public static List<TagTemplate> Load()
    {
        var result = new List<TagTemplate>();
        if (File.Exists(TagTemplatesFilePath))
        {
            var tagTemplatesJson = File.ReadAllText(TagTemplatesFilePath);
            result = JsonSerializer.Deserialize<List<TagTemplate>>(tagTemplatesJson);
        }
        else
        {
            result = new List<TagTemplate>
            {
                new TagTemplate
                {
                    TemplateName = "Created",
                    Tags = new Dictionary<string, string>
                    {
                        { "CreatedBy", "{User}" },
                        { "CreatedTime", "{DateTime}" }
                    }
                },
                new TagTemplate
                {
                    TemplateName = "Purpose",
                    Tags = new Dictionary<string, string>
                    {
                        { "Purpose", "" }
                    }
                },
                new TagTemplate
                {
                    TemplateName = "Accounting",
                    Tags = new Dictionary<string, string>
                    {
                        { "Organization", "" },
                        { "Project", "" },
                        { "CostCenter", "" }
                    }
                },
                new TagTemplate
                {
                    TemplateName = "Classification",
                    Tags = new Dictionary<string, string>
                    {
                        { "Scope", "" },
                        { "Environment", "" },
                        { "Asset", "" },
                        { "SecurityContact", "" }
                    }
                },
                new TagTemplate
                {
                    TemplateName = "ReviewRequired",
                    Tags = new Dictionary<string, string>
                    {
                        { "ReviewRequiredBy", "{User}" },
                        { "ReviewRequiredTime", "{DateTime}" }
                    }
                }
            };
            Save(result);
        };
        return result;
    }

    public static void Save(List<TagTemplate> tagTemplates)
    {
        var tagTemplatesJson = JsonSerializer.Serialize(tagTemplates,
            new JsonSerializerOptions
            {
                IndentSize = 2,
                WriteIndented = true
            });
        File.WriteAllText(TagTemplatesFilePath, tagTemplatesJson);
    }
}
