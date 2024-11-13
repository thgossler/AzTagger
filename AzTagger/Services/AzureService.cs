using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceGraph;
using Azure.ResourceGraph.Models;
using AzTagger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzTagger.Services
{
    public class AzureService
    {
        private readonly ArmClient _armClient;
        private readonly ResourceGraphClient _resourceGraphClient;

        public AzureService(string tenantId, string clientId, string clientSecret, string subscriptionId)
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _armClient = new ArmClient(credential, subscriptionId);
            _resourceGraphClient = new ResourceGraphClient(credential);
        }

        public async Task<List<Resource>> QueryResourcesAsync(string query)
        {
            var request = new QueryRequest
            {
                Subscriptions = new List<string> { _armClient.DefaultSubscription.Id },
                Query = query
            };

            var response = await _resourceGraphClient.ResourcesAsync(request);
            var resources = response.Data.Select(r => new Resource
            {
                SubscriptionName = r["subscriptionName"]?.ToString(),
                SubscriptionId = r["subscriptionId"]?.ToString(),
                Type = r["type"]?.ToString(),
                ResourceGroup = r["resourceGroup"]?.ToString(),
                Name = r["name"]?.ToString(),
                Tags = r["tags"]?.ToObject<Dictionary<string, string>>()
            }).ToList();

            return resources;
        }

        public async Task UpdateTagsAsync(List<Resource> resources, Dictionary<string, string> tags)
        {
            foreach (var resource in resources)
            {
                var resourceIdentifier = new ResourceIdentifier(resource.Id);
                var genericResource = _armClient.GetGenericResource(resourceIdentifier);
                var resourceTags = await genericResource.GetTagsAsync();

                foreach (var tag in tags)
                {
                    resourceTags.Value.Data.Properties.Tags[tag.Key] = tag.Value;
                }

                await genericResource.SetTagsAsync(resourceTags.Value.Data.Properties.Tags);
            }
        }
    }
}
