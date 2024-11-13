using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AzTagger
{
    public class TagTemplate
    {
        public string TemplateName { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    public class TagTemplates
    {
        private static readonly string TagTemplatesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzTagger", "tagtemplates.json");

        public static List<TagTemplate> Load()
        {
            if (File.Exists(TagTemplatesFilePath))
            {
                var tagTemplatesJson = File.ReadAllText(TagTemplatesFilePath);
                return JsonSerializer.Deserialize<List<TagTemplate>>(tagTemplatesJson);
            }
            return new List<TagTemplate>
            {
                new TagTemplate
                {
                    TemplateName = "Default",
                    Tags = new Dictionary<string, string>
                    {
                        { "Owner", "" },
                        { "Purpose", "" }
                    }
                }
            };
        }

        public static void Save(List<TagTemplate> tagTemplates)
        {
            var tagTemplatesJson = JsonSerializer.Serialize(tagTemplates);
            File.WriteAllText(TagTemplatesFilePath, tagTemplatesJson);
        }
    }
}
