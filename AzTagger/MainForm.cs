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

        private async void searchButton_Click(object sender, EventArgs e)
        {
            await SearchResourcesAsync();
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

        private void applyTagsButton_Click(object sender, EventArgs e)
        {
            ApplyTags();
        }

        private void ApplyTags()
        {
            // Implement tag application logic here
        }
    }
}
