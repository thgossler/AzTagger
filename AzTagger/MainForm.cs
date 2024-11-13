using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Azure.ResourceManager.Subscription;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.Resources;
using Serilog;
using System.IO;
using Newtonsoft.Json;

namespace AzTagger
{
    public partial class MainForm : Form
    {
        private IPublicClientApplication _msalClient;
        private List<string> _tenants;
        private string _selectedTenant;
        private string _selectedEnvironment;
        private List<Resource> _resources;
        private List<Tag> _tags;
        private Settings _settings;
        private List<Dictionary<string, string>> _tagTemplates;
        private string _currentQuery;
        private System.Threading.CancellationTokenSource _searchCancellationTokenSource;

        public MainForm()
        {
            InitializeComponent();
            InitializeAuthentication();
            LoadSettings();
            LoadTagTemplates();
        }

        private void InitializeAuthentication()
        {
            _msalClient = PublicClientApplicationBuilder.Create("your-client-id")
                .WithRedirectUri("http://localhost")
                .Build();
            SignInUser();
        }

        private async void SignInUser()
        {
            try
            {
                var accounts = await _msalClient.GetAccountsAsync();
                var result = await _msalClient.AcquireTokenSilent(new[] { "User.Read" }, accounts.FirstOrDefault())
                    .ExecuteAsync();
                LoadTenants(result.Account);
            }
            catch (MsalUiRequiredException)
            {
                var result = await _msalClient.AcquireTokenInteractive(new[] { "User.Read" })
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync();
                LoadTenants(result.Account);
            }
        }

        private async void LoadTenants(IAccount account)
        {
            try
            {
                var authResult = await _msalClient.AcquireTokenSilent(new[] { "https://management.azure.com/.default" }, account)
                    .ExecuteAsync();

                var credentials = new TokenCredentials(authResult.AccessToken);
                var subscriptionClient = new SubscriptionClient(credentials);

                var tenants = await subscriptionClient.Tenants.ListAsync();
                _tenants = tenants.Select(t => t.TenantId.ToString()).ToList();
                tenantComboBox.DataSource = _tenants;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Failed to load tenants. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSettings()
        {
            _settings = Settings.Load();
            _selectedTenant = _settings.SelectedTenant;
            _selectedEnvironment = _settings.SelectedEnvironment;
            tenantComboBox.SelectedItem = _selectedTenant;
            environmentComboBox.SelectedItem = _selectedEnvironment;
        }

        private void SaveSettings()
        {
            _settings.SelectedTenant = _selectedTenant;
            _settings.SelectedEnvironment = _selectedEnvironment;
            _settings.Save();
        }

        private void LoadTagTemplates()
        {
            var tagTemplatesFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AzTagger",
                "tagtemplates.json");

            if (!File.Exists(tagTemplatesFilePath))
            {
                var defaultTemplates = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>
                    {
                        { "TemplateName", "Default" },
                        { "Tags", JsonConvert.SerializeObject(new Dictionary<string, string>
                            {
                                { "Owner", "" },
                                { "Purpose", "" }
                            })
                        }
                    }
                };
                var json = JsonConvert.SerializeObject(defaultTemplates, Formatting.Indented);
                File.WriteAllText(tagTemplatesFilePath, json);
            }

            var tagTemplatesJson = File.ReadAllText(tagTemplatesFilePath);
            _tagTemplates = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(tagTemplatesJson);
            tagTemplatesComboBox.DataSource = _tagTemplates.Select(t => t["TemplateName"]).ToList();
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            if (searchAsYouTypeCheckBox.Checked)
            {
                _searchCancellationTokenSource?.Cancel();
                _searchCancellationTokenSource = new System.Threading.CancellationTokenSource();
                var token = _searchCancellationTokenSource.Token;

                Task.Delay(1000, token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        PerformSearch();
                    }
                }, token);
            }
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            PerformSearch();
        }

        private async void PerformSearch()
        {
            try
            {
                var authResult = await _msalClient.AcquireTokenSilent(new[] { "https://management.azure.com/.default" }, _msalClient.GetAccountsAsync().Result.FirstOrDefault())
                    .ExecuteAsync();

                var credentials = new TokenCredentials(authResult.AccessToken);
                var resourceGraphClient = new ResourceGraphClient(credentials);

                var query = new QueryRequest
                {
                    Subscriptions = new List<string> { _selectedTenant },
                    Query = _currentQuery
                };

                var response = await resourceGraphClient.ResourcesAsync(query);
                _resources = response.Data.ToObject<List<Resource>>();
                mainDataGridView.DataSource = _resources;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Failed to perform search. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void editQueryButton_Click(object sender, EventArgs e)
        {
            // Open modal dialog for editing query
            using (var dialog = new EditQueryDialog(_currentQuery))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Update query text and perform search
                    _currentQuery = dialog.QueryText;
                    PerformSearch();
                }
            }
        }

        private void applyTagsButton_Click(object sender, EventArgs e)
        {
            // Apply tags to selected resources
            ApplyTagsToResources();
            SaveSettings();
        }

        private async void ApplyTagsToResources()
        {
            try
            {
                var authResult = await _msalClient.AcquireTokenSilent(new[] { "https://management.azure.com/.default" }, _msalClient.GetAccountsAsync().Result.FirstOrDefault())
                    .ExecuteAsync();

                var credentials = new TokenCredentials(authResult.AccessToken);
                var resourceManagementClient = new ResourceManagementClient(credentials);

                foreach (var resource in _resources)
                {
                    var parameters = new GenericResource
                    {
                        Tags = resource.Tags
                    };
                    await resourceManagementClient.Resources.UpdateAsync(resource.Id, parameters);
                }

                MessageBox.Show("Tags applied successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Failed to apply tags. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void mainDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            // Update tags table based on selected items
            var selectedResources = mainDataGridView.SelectedRows.Cast<DataGridViewRow>()
                .Select(row => row.DataBoundItem as Resource)
                .ToList();
            UpdateTagsTable(selectedResources);
        }

        private void UpdateTagsTable(List<Resource> selectedResources)
        {
            // Update tags table logic
            // This is a placeholder for the actual update logic
            _tags = new List<Tag>(); // Replace with actual logic to get common tags
            tagsDataGridView.DataSource = _tags;
        }

        private void tagsDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Handle tag value changes
            var changedTag = tagsDataGridView.Rows[e.RowIndex].DataBoundItem as Tag;
            if (changedTag != null)
            {
                // Update the tag value in the local in-memory data
                var resource = _resources.FirstOrDefault(r => r.Tags.ContainsKey(changedTag.Key));
                if (resource != null)
                {
                    resource.Tags[changedTag.Key] = changedTag.Value;
                }
            }
        }

        private void tagsDataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            // Handle tag deletion
            var deletingTag = e.Row.DataBoundItem as Tag;
            if (deletingTag != null)
            {
                // Remove the tag from the local in-memory data
                foreach (var resource in _resources)
                {
                    if (resource.Tags.ContainsKey(deletingTag.Key))
                    {
                        resource.Tags.Remove(deletingTag.Key);
                    }
                }
            }
        }

        private void tagsDataGridView_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            // Handle adding new tags
            var newTag = e.Row.DataBoundItem as Tag;
            if (newTag != null && !string.IsNullOrEmpty(newTag.Key))
            {
                // Add the new tag to the local in-memory data
                foreach (var resource in _resources)
                {
                    if (!resource.Tags.ContainsKey(newTag.Key))
                    {
                        resource.Tags.Add(newTag.Key, newTag.Value);
                    }
                }
            }
        }

        private void tagTemplatesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedTemplate = tagTemplatesComboBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedTemplate))
            {
                var template = _tagTemplates.FirstOrDefault(t => t["TemplateName"] == selectedTemplate);
                if (template != null)
                {
                    var tags = JsonConvert.DeserializeObject<Dictionary<string, string>>(template["Tags"]);
                    foreach (var kv in tags)
                    {
                        var existingTag = _tags.FirstOrDefault(t => t.Key == kv.Key);
                        if (existingTag != null)
                        {
                            existingTag.Value = kv.Value;
                        }
                        else
                        {
                            _tags.Add(new Tag { Key = kv.Key, Value = kv.Value });
                        }
                    }
                    tagsDataGridView.DataSource = null;
                    tagsDataGridView.DataSource = _tags;
                    tagTemplatesComboBox.SelectedIndex = -1;
                }
            }
        }
    }
}
