namespace AzTagger.Models
{
    public class Resource
    {
        public string SubscriptionName { get; set; }
        public string SubscriptionId { get; set; }
        public string Type { get; set; }
        public string ResourceGroup { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }
}
