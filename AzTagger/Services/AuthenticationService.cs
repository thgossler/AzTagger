using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using System.Text.Json;

namespace AzTagger.Services
{
    public class AuthenticationService
    {
        private readonly IPublicClientApplication _publicClientApp;
        private readonly string[] _scopes = { "https://management.azure.com/.default" };

        public AuthenticationService()
        {
            _publicClientApp = PublicClientApplicationBuilder.Create("your-client-id")
                .WithAuthority(AzureCloudInstance.AzurePublic, "common")
                .WithDefaultRedirectUri()
                .Build();
        }

        public async Task SignInAsync()
        {
            var accounts = await _publicClientApp.GetAccountsAsync();
            AuthenticationResult result;
            try
            {
                result = await _publicClientApp.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                result = await _publicClientApp.AcquireTokenInteractive(_scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync();
            }

            // Store the token for future use
            var token = result.AccessToken;
        }

        public async Task<List<Tenant>> GetTenantsAsync()
        {
            var accounts = await _publicClientApp.GetAccountsAsync();
            var result = await _publicClientApp.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                .ExecuteAsync();

            // Use the token to get the list of tenants
            var token = result.AccessToken;
            var tenants = await FetchTenantsFromGraphApiAsync(token);
            return tenants;
        }

        private async Task<List<Tenant>> FetchTenantsFromGraphApiAsync(string token)
        {
            var tenants = new List<Tenant>();
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/organization");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(jsonResponse);
                    var tenantArray = jsonDoc.RootElement.GetProperty("value").EnumerateArray();
                    foreach (var tenant in tenantArray)
                    {
                        tenants.Add(new Tenant
                        {
                            TenantId = tenant.GetProperty("id").GetString(),
                            DisplayName = tenant.GetProperty("displayName").GetString()
                        });
                    }
                }
            }
            return tenants;
        }
    }

    public class Tenant
    {
        public string TenantId { get; set; }
        public string DisplayName { get; set; }
    }
}
