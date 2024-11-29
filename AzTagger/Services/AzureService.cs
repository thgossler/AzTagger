// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using AzTagger.Models;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
using Azure.ResourceManager.Resources;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzTagger.Services;

public class AzureService
{
    private Settings _settings;
    private AzureContext _azContext;

    private class SigninContext
    {
        public string AzureContextName;
        public InteractiveBrowserCredential Credential;
        public ArmClient ArmClient;
        public TenantResource TenantResource;
    }
    List<SigninContext> _signinContexts = new List<SigninContext>();

    private SigninContext CurrentSigninContext {
        get { 
            return _signinContexts.FirstOrDefault(sc => sc.AzureContextName.Equals(_azContext.Name));
        } 
    }

    public InteractiveBrowserCredential CurrentCredential
    {
        get
        {
            return CurrentSigninContext?.Credential;
        }
    }

    public AzureService(Settings settings)
    {
        _settings = settings;
        _azContext = _settings.GetAzureContext();
    }

    public async Task<List<TenantData>> GetAvailableTenantsAsync(string environmentName = null)
    {
        var armEnvironment = GetAzureEnvironmentByName(environmentName) ?? _azContext.ArmEnvironment;

        var authorityHost = AzureAuthorityHosts.AzurePublicCloud;
        if (armEnvironment == ArmEnvironment.AzureChina)
        {
            authorityHost = AzureAuthorityHosts.AzureChina;
        }
        else if (armEnvironment == ArmEnvironment.AzureGovernment)
        {
            authorityHost = AzureAuthorityHosts.AzureGovernment;
        }

        var options = new InteractiveBrowserCredentialOptions
        {
            AuthorityHost = authorityHost,
            TenantId = "organizations",
            RedirectUri = new Uri("http://localhost"),
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
            {
                Name = $"AzTaggerTokenCache_{environmentName ?? _azContext.AzureEnvironmentName}"
            },
        };

        var credential = new InteractiveBrowserCredential(options);
        var armClient = new ArmClient(credential, null, new ArmClientOptions
        {
            Environment = armEnvironment
        });

        var tenants = new List<TenantData>();
        await foreach (var tenant in armClient.GetTenants().GetAllAsync())
        {
            tenants.Add(tenant.Data);
        }
        return tenants;
    }

    public async Task SignInAsync(bool refresh = false)
    {
        _azContext = _settings.GetAzureContext();

        if (string.IsNullOrWhiteSpace(_azContext.TenantId))
        {
            throw new Exception("TenantId is not set in the settings.");
        }

        var signinContext = CurrentSigninContext;

        if (signinContext != null && signinContext.Credential != null && signinContext.ArmClient != null && signinContext.TenantResource != null)
        {
            if (!refresh && await IsSessionValidAsync(signinContext))
            {
                return;
            }
        }

        signinContext = new SigninContext { AzureContextName = _azContext.Name };
        _signinContexts.Add(signinContext);

        await Task.Run(async () =>
        {
            var authorityHost = AzureAuthorityHosts.AzurePublicCloud;
            if (_azContext.ArmEnvironment == ArmEnvironment.AzureChina)
            {
                authorityHost = AzureAuthorityHosts.AzureChina;
            }
            else if (_azContext.ArmEnvironment == ArmEnvironment.AzureGovernment)
            {
                authorityHost = AzureAuthorityHosts.AzureGovernment;
            }
            var options = new InteractiveBrowserCredentialOptions
            {
                AuthorityHost = authorityHost,
                RedirectUri = new Uri("http://localhost"),
                TenantId = _azContext.TenantId,
                ClientId = _azContext.ClientAppId,
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = $"AzTaggerTokenCache_{_azContext.AzureEnvironmentName}"
                }
            };
            signinContext.Credential = new InteractiveBrowserCredential(options);

            var armClientOptions = new ArmClientOptions { Environment = _azContext.ArmEnvironment };
            signinContext.ArmClient = new ArmClient(signinContext.Credential, _azContext.TenantId, armClientOptions);
            var tenants = new List<TenantResource>();
            await foreach (var tenant in signinContext.ArmClient.GetTenants().GetAllAsync())
            {
                tenants.Add(tenant);
            }
            signinContext.TenantResource = tenants.FirstOrDefault(t => t.Data.Id.EndsWith(_azContext.TenantId, StringComparison.OrdinalIgnoreCase));
        });

        if (signinContext.TenantResource == null)
        {
            throw new Exception("Failed to sign in to Azure.");
        }
    }

    private async Task<bool> IsSessionValidAsync(SigninContext signinContext)
    {
        try
        {
            var requestContext = new TokenRequestContext([_azContext.ArmEnvironment.DefaultScope]);
            var token = await signinContext.Credential.GetTokenAsync(requestContext, default);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string[] GetAzureEnvironmentNames()
    {
        var azEnvNames = typeof(ArmEnvironment).GetFields().Select(f => f.Name).Where(n => n.StartsWith("Azure") && !n.Contains("Germany")).ToArray();
        return azEnvNames;
    }

    public ArmEnvironment? GetAzureEnvironmentByName(string environmentName)
    {
        var field = typeof(ArmEnvironment).GetField(environmentName);
        if (field == null)
        {
            return null;
        }
        return (ArmEnvironment)field.GetValue(null);
    }

    public string GetAzurePortalUrl()
    {
        var url = "https://portal.azure.com";
        if (_azContext.ArmEnvironment == ArmEnvironment.AzureChina)
        {
            url = "https://portal.azure.cn";
        }
        else if (_azContext.ArmEnvironment == ArmEnvironment.AzureGovernment)
        {
            url = "https://portal.azure.us";
        }
        return url;
    }

    public async Task<List<Resource>> QueryResourcesAsync(string query, CancellationToken cancellationToken = default)
    {
        var result = new List<Resource>();
        if (query == null)
        {
            return result;
        }

        var signinContext = CurrentSigninContext;

        if (signinContext == null || signinContext.TenantResource == null)
        {
            throw new InvalidOperationException("Signin context or tenant resource is not initialized.");
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
            var azureResult = await signinContext.TenantResource.GetResourcesAsync(resourceQuery, cancellationToken);
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
                var pageResults = queryResult.Data.ToObjectFromJson<List<Resource>>();
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

    public async Task<string[]> UpdateTagsAsync(List<Resource> resources, Dictionary<string, string> tagsToUpdate, Dictionary<string, string> tagsToRemove)
    {
        var signinContext = CurrentSigninContext;
        
        var maxDegreeOfParallelism = 10;
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

        var errors = new List<string>();

        var tasks = resources.Select(async resource =>
        {
            await semaphore.WaitAsync();
            try
            {
                var resourceIdentifier = new ResourceIdentifier(resource.Id);
                var genericResource = signinContext.ArmClient.GetGenericResource(resourceIdentifier);

                var resourceResponse = await genericResource.GetAsync();
                var currentTags = resourceResponse.Value.Data.Tags;

                if (tagsToUpdate != null && tagsToUpdate.Count > 0)
                {
                    foreach (var tagToUpdate in tagsToUpdate)
                    {
                        currentTags[tagToUpdate.Key] = tagToUpdate.Value;
                    }
                }

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

                await genericResource.SetTagsAsync(currentTags);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error updating tags for resource {resource.Id}");
                errors.Add(ex.Message.Split(Environment.NewLine)[0]);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return errors.ToArray();
    }
}
