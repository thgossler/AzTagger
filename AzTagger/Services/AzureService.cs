// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using AzTagger.Models;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
using Azure.ResourceManager.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AzTagger.Services;

public class AzureService
{
    private Settings _settings;
    private InteractiveBrowserCredential _credential;
    private ArmClient _armClient;
    private TenantResource _tenantResource;

    public AzureService(Settings settings)
    {
        _settings = settings;
    }

    public async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.TenantId))
        {
            throw new Exception("TenantId is not set in the settings.");
        }

        if (_credential != null && _armClient != null && _tenantResource != null)
        {
            if (await IsSessionValidAsync())
            {
                return;
            }
        }

        await Task.Run(async () =>
        {
            _credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = _settings.TenantId,
                ClientId = _settings.ClientAppId,
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = $"AzTaggerTokenCache_{_settings.TenantId}"
                },
                RedirectUri = new Uri("http://localhost")
            });

            ArmEnvironment azEnvironment = ArmEnvironment.AzurePublicCloud;
            if (_settings.AzureEnvironment.Contains("China", System.StringComparison.OrdinalIgnoreCase))
            {
                azEnvironment = ArmEnvironment.AzureChina;
            }
            else if (_settings.AzureEnvironment.Contains("German", System.StringComparison.OrdinalIgnoreCase))
            {
                azEnvironment = ArmEnvironment.AzureGermany;
            }
            else if (_settings.AzureEnvironment.Contains("Government", System.StringComparison.OrdinalIgnoreCase))
            {
                azEnvironment = ArmEnvironment.AzureGovernment;
            }

            var armClientOptions = new ArmClientOptions { Environment = azEnvironment };
            _armClient = new ArmClient(_credential, _settings.TenantId, armClientOptions);
            var tenants = new List<TenantResource>();
            await foreach (var tenant in _armClient.GetTenants().GetAllAsync())
            {
                tenants.Add(tenant);
            }
            _tenantResource = tenants.FirstOrDefault(t => t.Data.Id.EndsWith(_settings.TenantId, StringComparison.OrdinalIgnoreCase));
        });

        if (_tenantResource == null)
        {
            throw new Exception("Failed to sign in to Azure.");
        }
    }

    private async Task<bool> IsSessionValidAsync()
    {
        try
        {
            var requestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
            var token = await _credential.GetTokenAsync(requestContext, default);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetAzurePortalUrl()
    {
        var url = "https://portal.azure.com";
        if (_settings.AzureEnvironment.Contains("China", System.StringComparison.OrdinalIgnoreCase))
        {
            url = "https://portal.azure.cn";
        }
        else if (_settings.AzureEnvironment.Contains("German", System.StringComparison.OrdinalIgnoreCase))
        {
            url = "https://portal.microsoftazure.de";
        }
        else if (_settings.AzureEnvironment.Contains("Government", System.StringComparison.OrdinalIgnoreCase))
        {
            url = "https://portal.azure.us";
        }
        return url;
    }

    public async Task<List<Resource>> QueryResourcesAsync(string query)
    {
        var result = new List<Resource>();
        if (query == null)
        {
            return result;
        }

        string skipToken = null;
        do
        {
            var resourceQuery = new ResourceQueryContent(query)
            {
                Options = new ResourceQueryRequestOptions
                {
                    ResultFormat = ResultFormat.ObjectArray,
                    SkipToken = skipToken
                }
            };
            var azureResult = await _tenantResource.GetResourcesAsync(resourceQuery);
            var response = azureResult.GetRawResponse();
            if (response.Status != 200)
            {
                throw new Exception($"Resource graph query failed with status code {response.Status}");
            }

            var queryResult = azureResult.Value;
            if (queryResult == null)
            {
                break;
            }

            if (queryResult.Data != null)
            {
                var pageResults = queryResult.Data.ToObjectFromJson<List<Resource>>(new JsonSerializerOptions { });
                if (pageResults != null)
                {
                    result.AddRange(pageResults);
                }
            }

            skipToken = queryResult.SkipToken;
        }
        while (!string.IsNullOrEmpty(skipToken));

        return result;
    }

    public async Task UpdateTagsAsync(List<Resource> resources, Dictionary<string, string> tagsToUpdate, Dictionary<string, string> tagsToRemove)
    {
        var maxDegreeOfParallelism = 10;
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

        var tasks = resources.Select(async resource =>
        {
            await semaphore.WaitAsync();
            try
            {
                var resourceIdentifier = new ResourceIdentifier(resource.Id);
                var genericResource = _armClient.GetGenericResource(resourceIdentifier);

                // Retrieve the current tags using GetAsync()
                var resourceResponse = await genericResource.GetAsync();
                var currentTags = resourceResponse.Value.Data.Tags;

                // Update tags
                if (tagsToUpdate != null && tagsToUpdate.Count > 0)
                {
                    foreach (var tagToUpdate in tagsToUpdate)
                    {
                        currentTags[tagToUpdate.Key] = tagToUpdate.Value;
                    }
                }

                // Remove tags
                if (tagsToRemove != null && tagsToRemove.Count > 0)
                {
                    foreach (var tagToRemove in tagsToRemove)
                    {
                        if (currentTags.ContainsKey(tagToRemove.Key))
                        {
                            currentTags.Remove(tagToRemove.Key);
                        }
                    }
                }

                // Set the updated tags
                await genericResource.SetTagsAsync(currentTags);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}
