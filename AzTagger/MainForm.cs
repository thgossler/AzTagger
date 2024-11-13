using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Identity.Client;
using Serilog;

namespace AzTagger
{
    public partial class MainForm : Form
    {
        private readonly Settings _settings;
        private readonly AuthenticationService _authService;
        private readonly AzureService _azureService;
        private List<Resource> _resources;
        private List<Tenant> _tenants;

        public MainForm(Settings settings)
        {
            InitializeComponent();
            _settings = settings;
            _authService = new AuthenticationService();
            _azureService = new AzureService();
            _resources = new List<Resource>();
            _tenants = new List<Tenant>();

            Load += MainForm_Load;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await AuthenticateUserAsync();
            await LoadTenantsAsync();
            LoadSettings();
            LoadRecentSearches();
        }

        private async Task AuthenticateUserAsync()
        {
            try
            {
                await _authService.SignInAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Authentication failed.");
                MessageBox.Show("Authentication failed. Please check the error log for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadTenantsAsync()
        {
            try
            {
                _tenants = await _authService.GetTenantsAsync();
                tenantComboBox.DataSource = _tenants;
                tenantComboBox.DisplayMember = "DisplayName";
                tenantComboBox.ValueMember = "TenantId";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load tenants.");
                MessageBox.Show("Failed to load tenants. Please check the error log for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSettings()
        {
            if (!string.IsNullOrEmpty(_settings.SelectedTenantId))
            {
                tenantComboBox.SelectedValue = _settings.SelectedTenantId;
            }

            if (!string.IsNullOrEmpty(_settings.AzureEnvironment))
            {
                azureEnvironmentComboBox.SelectedItem = _settings.AzureEnvironment;
            }
        }

        private void LoadRecentSearches()
        {
            recentSearchesComboBox.Items.Clear();
            recentSearchesComboBox.Items.AddRange(_settings.RecentSearches.ToArray());
            recentSearchesComboBox.SelectedIndex = -1;
        }

        private void SaveRecentSearch(string searchText)
        {
            if (!string.IsNullOrEmpty(searchText))
            {
                _settings.RecentSearches.Remove(searchText);
                _settings.RecentSearches.Insert(0, searchText);
                if (_settings.RecentSearches.Count > 10)
                {
                    _settings.RecentSearches.RemoveAt(10);
                }
                _settings.Save();
            }
        }

        private async void searchButton_Click(object sender, EventArgs e)
        {
            await SearchResourcesAsync();
            SaveRecentSearch(searchTextBox.Text);
        }

        private async Task SearchResourcesAsync()
        {
            try
            {
                var query = BuildQuery();
                _resources = await _azureService.SearchResourcesAsync(query);
                DisplayResults();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Search failed.");
                MessageBox.Show("Search failed. Please check the error log for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string BuildQuery()
        {
            var baseQuery = @"
                resourcecontainers
                | where type == 'microsoft.resources/subscriptions'
                | project subscriptionId, resourceGroup = '', name, type, tags
                | union (
                    resourcecontainers
                    | where type == 'microsoft.resources/subscriptions/resourcegroups'
                    | project subscriptionId, resourceGroup = name, name, type, tags
                ), (
                    resources
                    | project subscriptionId, resourceGroup, name, type, tags
                )
                | join kind=leftouter (
                    resourcecontainers
                    | where type == 'microsoft.resources/subscriptions'
                    | project subscriptionId, subscriptionName = name
                ) on subscriptionId
                | project subscriptionName, subscriptionId, type, resourceGroup, name, tags";

            if (!string.IsNullOrEmpty(searchTextBox.Text))
            {
                var filter = $@"
                    | where name matches regex @""(?i){searchTextBox.Text}""
                       or resourceGroup matches regex @""(?i){searchTextBox.Text}""
                       or subscriptionName matches regex @""(?i){searchTextBox.Text}""";
                baseQuery += filter;
            }

            return baseQuery;
        }

        private void DisplayResults()
        {
            resultsDataGridView.DataSource = _resources;
        }

        private async void applyTagsButton_Click(object sender, EventArgs e)
        {
            await ApplyTagsAsync();
        }

        private async Task ApplyTagsAsync()
        {
            try
            {
                var selectedResources = GetSelectedResources();
                var tags = GetTagsFromDataGridView();
                await _azureService.UpdateTagsAsync(selectedResources, tags);
                UpdateLocalTags(selectedResources, tags);
                MessageBox.Show("Tags applied successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply tags.");
                MessageBox.Show("Failed to apply tags. Please check the error log for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<Resource> GetSelectedResources()
        {
            var selectedResources = new List<Resource>();
            foreach (DataGridViewRow row in resultsDataGridView.SelectedRows)
            {
                var resource = row.DataBoundItem as Resource;
                if (resource != null)
                {
                    selectedResources.Add(resource);
                }
            }
            return selectedResources;
        }

        private Dictionary<string, string> GetTagsFromDataGridView()
        {
            var tags = new Dictionary<string, string>();
            foreach (DataGridViewRow row in tagsDataGridView.Rows)
            {
                if (row.Cells["Key"].Value != null && row.Cells["Value"].Value != null)
                {
                    var key = row.Cells["Key"].Value.ToString();
                    var value = row.Cells["Value"].Value.ToString();
                    if (!string.IsNullOrEmpty(key))
                    {
                        tags[key] = value;
                    }
                }
            }
            return tags;
        }

        private void UpdateLocalTags(List<Resource> resources, Dictionary<string, string> tags)
        {
            foreach (var resource in resources)
            {
                foreach (var tag in tags)
                {
                    resource.Tags[tag.Key] = tag.Value;
                }
            }
            resultsDataGridView.Refresh();
        }

        private void recentSearchesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (recentSearchesComboBox.SelectedItem != null)
            {
                searchTextBox.Text = recentSearchesComboBox.SelectedItem.ToString();
            }
        }

        private void findItemsWithoutTagsButton_Click(object sender, EventArgs e)
        {
            var filterExpression = BuildFilterExpressionForMissingTags();
            searchTextBox.Text = filterExpression;
        }

        private string BuildFilterExpressionForMissingTags()
        {
            var tagKeys = new List<string>();
            foreach (DataGridViewRow row in tagsDataGridView.Rows)
            {
                if (row.Cells["Key"].Value != null)
                {
                    var key = row.Cells["Key"].Value.ToString();
                    if (!string.IsNullOrEmpty(key))
                    {
                        tagKeys.Add(key);
                    }
                }
            }

            var filterExpression = string.Join(" or ", tagKeys.Select(key => $"(tags['{key}'] == null or tags['{key}'] == '')"));
            return filterExpression;
        }
    }
}
