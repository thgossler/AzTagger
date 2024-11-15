// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using AzTagger.Models;
using AzTagger.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AzTagger;

public partial class MainForm : Form
{
    private readonly Settings _settings;
    private List<TagTemplate> _tagTemplates;
    private AzureService _azureService;
    private List<Resource> _resources;
    private ContextMenuStrip _contextMenu;
    private DataGridViewCell _contextMenuClickedCell;
    private string _fullQuery = string.Empty;
    private Timer _resizeTimer;
    private DateTime _lastResizeTime;
    private string _currentSortColumn = string.Empty;
    private bool _sortAscending = true;
    private const int debounceDelayMsecs = 500;
    private Timer _debounceTimer1;
    private Timer _debounceTimer2;

    enum QueryMode
    {
        KqlFilter,
        Regex,
        KqlFull
    }
    private QueryMode _queryMode = QueryMode.Regex;

    public MainForm(Settings settings)
    {
        InitializeComponent();
        InitializeResultsDataGridView();
        InitializeTagsDataGridView();
        InitializeContextMenu();
        InitializeResizeTimer();
        InitializeQuickFilterComboBoxes(); // Added initialization for quick filter combo boxes
        InitializeDebounceTimers();

        _settings = settings;
        _fullQuery = BuildQuery();
        _resources = new List<Resource>();

        Load += Form_Load;
    }

    private void InitializeDebounceTimers()
    {
        _debounceTimer1 = new Timer();
        _debounceTimer1.Interval = debounceDelayMsecs;
        _debounceTimer1.Tick += DebounceTimer1_Tick;

        _debounceTimer2 = new Timer();
        _debounceTimer2.Interval = debounceDelayMsecs;
        _debounceTimer2.Tick += DebounceTimer2_Tick;
    }

    private void InitializeQuickFilterComboBoxes()
    {
        var resourceProperties = typeof(Resource).GetProperties().Select(p => p.Name).ToArray();
        _cboQuickFilter1Column.Items.AddRange(resourceProperties);
        _cboQuickFilter2Column.Items.AddRange(resourceProperties);
    }

    private void InitializeResizeTimer()
    {
        _resizeTimer = new System.Windows.Forms.Timer();
        _resizeTimer.Interval = 500;
        _resizeTimer.Tick += ResizeTimer_Tick;
        _resizeTimer.Start();
    }

    private void InitializeContextMenu()
    {
        _contextMenu = new ContextMenuStrip();

        var openInAzurePortalMenuItem = new ToolStripMenuItem("Open in Azure Portal");
        openInAzurePortalMenuItem.Click += OpenInAzurePortalMenuItem_Click;
        _contextMenu.Items.Add(openInAzurePortalMenuItem);

        var openUrlsInTagValuesMenuItem = new ToolStripMenuItem("Open URLs in tags");
        openUrlsInTagValuesMenuItem.Click += OpenUrlsInTagValuesMenuItem_Click;
        _contextMenu.Items.Add(openUrlsInTagValuesMenuItem);

        var copyCellValueMenuItem = new ToolStripMenuItem("Copy cell value");
        copyCellValueMenuItem.Click += CopyCellValueMenuItem_Click;
        _contextMenu.Items.Add(copyCellValueMenuItem);
    }

    private void OpenUrlsInTagValuesMenuItem_Click(object sender, EventArgs e)
    {
        if (_gvwResults.SelectedRows.Count > 0)
        {
            var selectedRow = _gvwResults.SelectedRows[0];
            var resource = selectedRow.DataBoundItem as Resource;

            // Extract all hyperlink URLs from all tag values based on a regular expression
            var urls = new List<string>();
            var tags = GetEntityTags(resource);
            foreach (var tag in tags)
            {
                var matches = Regex.Matches(tag.Value, @"https?://\S+");
                foreach (Match match in matches)
                {
                    urls.Add(match.Value);
                }
            }

            if (urls.Count > 0)
            {
                foreach (var url in urls)
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            else
            {
                MessageBox.Show(this, "No tags with URLs found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void OpenInAzurePortalMenuItem_Click(object sender, EventArgs e)
    {
        if (_gvwResults.SelectedRows.Count > 0)
        {
            var selectedRow = _gvwResults.SelectedRows[0];
            var resource = selectedRow.DataBoundItem as Resource;
            if (resource != null)
            {
                OpenResourceIdInAzurePortal(resource.Id);
            }
        }
    }

    private void CopyCellValueMenuItem_Click(object sender, EventArgs e)
    {
        if (_contextMenuClickedCell?.Value != null)
        {
            if (_contextMenuClickedCell.Value is IDictionary<string, string> tags)
            {
                Clipboard.SetText(string.Join(Environment.NewLine, tags.Select(tag => $"\"{tag.Key}\": \"{tag.Value}\"")));
            }
            else
            {
                Clipboard.SetText(_contextMenuClickedCell.Value.ToString());
            }
        }
    }

    private void DataGridView_Results_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
        {
            _gvwResults.ClearSelection();
            _gvwResults.Rows[e.RowIndex].Selected = true;
            _contextMenuClickedCell = _gvwResults.Rows[e.RowIndex].Cells[e.ColumnIndex];
            _contextMenu.Show(Cursor.Position);
        }
    }

    private void DataGridView_Results_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            var selectedRow = _gvwResults.Rows[e.RowIndex];
            var resource = selectedRow.DataBoundItem as Resource;
            if (resource != null)
            {
                OpenResourceIdInAzurePortal(resource.Id);
            }
        }
    }

    private void OpenResourceIdInAzurePortal(string resourceId)
    {
        var url = $"https://portal.azure.com/#@{_settings.TenantId}/resource{resourceId}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void Form_Load(object sender, EventArgs e)
    {
        LoadRecentSearches();
        LoadTagTemplates();
    }

    private void LoadTagTemplates()
    {
        _tagTemplates = TagTemplates.Load();
        _cboTagTemplates.Items.Clear();
        _cboTagTemplates.Items.AddRange(_tagTemplates.Select(t => t.TemplateName).ToArray());
        _cboTagTemplates.SelectedIndex = -1;
    }

    private void LoadRecentSearches()
    {
        _cboRecentSearches.Items.Clear();
        foreach (var queryText in _settings.RecentSearches)
        {
            _cboRecentSearches.Items.Add(new RecentSearchItem(queryText));
        }
        _cboRecentSearches.SelectedIndex = -1;
    }

    private void SaveRecentSearch(string queryText)
    {
        if (!string.IsNullOrEmpty(queryText))
        {
            _settings.RecentSearches.Insert(0, queryText);
            _settings.RemoveDuplicatesFromRecentSearches();
            if (_settings.RecentSearches.Count > 10)
            {
                _settings.RecentSearches.RemoveAt(10);
            }
            _settings.Save();

            var displayText = queryText.Replace("\r\n", " ").Replace("\n", " ");
            var itemsToRemove = _cboRecentSearches.Items.Cast<RecentSearchItem>().Where(i => i.DisplayText.Equals(displayText, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var item in itemsToRemove)
            {
                _cboRecentSearches.Items.Remove(item);
            }
            _cboRecentSearches.Items.Insert(0, new RecentSearchItem(queryText));

            if (_cboRecentSearches.Items.Count > 10)
            {
                _cboRecentSearches.Items.RemoveAt(_cboRecentSearches.Items.Count - 1);
            }
        }
    }

    private async void Button_PerformSearch_Click(object sender, EventArgs e)
    {
        if (_queryMode == QueryMode.KqlFull)
        {
            MessageBox.Show(this, "KQL full expressions are not supported! Please use KQL filter-only expressions or a regular expression.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        bool isSignedIn = await SignInToAzureAsync();
        if (!isSignedIn)
        {
            MessageBox.Show(this, "Azure sign-in failed. Please check your credentials and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _gvwResults.DataSource = new List<Resource>();
        UpdateResultsCountLabel(true);

        _progressBar.Visible = true;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 0;
        _progressBar.Style = ProgressBarStyle.Marquee;

        await SearchResourcesAsync();
        SaveRecentSearch(_txtSearchQuery.Text);

        _progressBar.Visible = false;
    }

    private async Task<bool> SignInToAzureAsync()
    {
        try
        {
            if (_azureService == null)
            {
                _azureService = new AzureService(_settings);
            }
            await _azureService.SignInAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Azure sign-in failed.");
            return false;
        }
    }

    private async Task SearchResourcesAsync()
    {
        try
        {
            var query = BuildQuery();
            _fullQuery = query;
            _resources = await _azureService.QueryResourcesAsync(query);
            DisplayResults();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Search failed.");
            MessageBox.Show(this, "Search failed! Please check the error log file in the\nprogram's local app data folder for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string BuildQuery()
    {
        var baseQuery = @"resources
| join kind=leftouter (
    resourcecontainers
    | where type =~ ""microsoft.resources/subscriptions/resourcegroups""
    | project rg_subscriptionId = subscriptionId, resourceGroup = name, resourceGroupTags = tags
) on $left.subscriptionId == $right.rg_subscriptionId and $left.resourceGroup == $right.resourceGroup
| join kind=leftouter (
    resourcecontainers
    | where type =~ ""microsoft.resources/subscriptions""
    | project sub_subscriptionId = subscriptionId, subscriptionTags = tags, subscriptionName = name
) on $left.subscriptionId == $right.sub_subscriptionId
| project 
    EntityType = ""Resource"",
    Id = id, 
    SubscriptionName = subscriptionName, 
    SubscriptionId = subscriptionId, 
    ResourceGroup = resourceGroup, 
    ResourceName = name, 
    ResourceType = type, 
    SubscriptionTags = subscriptionTags, 
    ResourceGroupTags = resourceGroupTags, 
    ResourceTags = tags
| union (
    resourcecontainers
    | where type =~ ""microsoft.resources/subscriptions/resourcegroups""
    | join kind=leftouter (
        resourcecontainers
        | where type =~ ""microsoft.resources/subscriptions""
        | project subscriptionId, subscriptionTags = tags, subscriptionName = name
    ) on $left.subscriptionId == $right.subscriptionId
    | project 
        EntityType = ""ResourceGroup"",
        Id = id, 
        SubscriptionName = subscriptionName, 
        SubscriptionId = subscriptionId, 
        ResourceGroup = name, 
        ResourceName = """", 
        ResourceType = """", 
        SubscriptionTags = subscriptionTags, 
        ResourceGroupTags = tags, 
        ResourceTags = """"
)
| union (
    resourcecontainers
    | where type =~ ""microsoft.resources/subscriptions""
    | project 
        EntityType = ""Subscription"",
        Id = id, 
        SubscriptionName = name, 
        SubscriptionId = subscriptionId, 
        ResourceGroup = """", 
        ResourceName = """", 
        ResourceType = """", 
        SubscriptionTags = tags, 
        ResourceGroupTags = """", 
        ResourceTags = """"
)
| project 
    EntityType,
    Id, 
    SubscriptionName, 
    SubscriptionId, 
    ResourceGroup, 
    ResourceName, 
    ResourceType, 
    SubscriptionTags, 
    ResourceGroupTags = ResourceGroupTags_dynamic, 
    ResourceTags = ResourceTags_dynamic
";

        if (!string.IsNullOrEmpty(_txtSearchQuery.Text) && _queryMode != QueryMode.KqlFull)
        {
            var filter = string.Empty;
            if (_queryMode == QueryMode.KqlFilter)
            {
                filter = _txtSearchQuery.Text;
            }
            else
            {
                filter = $@"| where ResourceName matches regex @""(?i){_txtSearchQuery.Text}""
       or ResourceGroup matches regex @""(?i){_txtSearchQuery.Text}""
       or SubscriptionName matches regex @""(?i){_txtSearchQuery.Text}""";
            }
            baseQuery += filter;
        }

        return baseQuery;
    }

    private void InitializeResultsDataGridView()
    {
        _gvwResults.AutoGenerateColumns = true;
        _gvwResults.CellFormatting += DataGridView_Results_CellFormatting;
        _gvwResults.CellMouseClick += DataGridView_Results_CellMouseClick;
        _gvwResults.CellDoubleClick += DataGridView_Results_CellDoubleClick;
        _gvwResults.SelectionChanged += DataGridView_Results_SelectionChanged;
        _gvwResults.ColumnHeaderMouseClick += DataGridView_Results_ColumnHeaderMouseClick;
        _gvwResults.DataSource = new List<Resource>();
        UpdateResultsColumnsWidth();
    }

    private void DataGridView_Results_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        string columnName = _gvwResults.Columns[e.ColumnIndex].DataPropertyName;

        if (_currentSortColumn == columnName)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _currentSortColumn = columnName;
            _sortAscending = true;
        }

        _resources = _sortAscending
            ? _resources.OrderBy(r => GetPropertyValue(r, columnName)).ToList()
            : _resources.OrderByDescending(r => GetPropertyValue(r, columnName)).ToList();

        DisplayResults();
    }
    private object GetPropertyValue(Resource resource, string propertyName)
    {
        var propertyInfo = typeof(Resource).GetProperty(propertyName);
        var value = propertyInfo?.GetValue(resource, null);

        if (value is IDictionary<string, string> tags)
        {
            var sortedTags = tags.OrderBy(tag => tag.Key);
            var jsonTags = System.Text.Json.JsonSerializer.Serialize(sortedTags);
            return jsonTags;
        }
        else
        {
            return value;
        }
    }

    private void InitializeTagsDataGridView()
    {
        _gvwTags.AutoGenerateColumns = true;
    }

    private void DataGridView_Results_SelectionChanged(object sender, EventArgs e)
    {
        _gvwTags.Rows.Clear();

        if (_gvwResults.SelectedRows.Count > 0)
        {
            var selectedRow = _gvwResults.SelectedRows[0];
            var resource = selectedRow.DataBoundItem as Resource;

            if (resource != null)
            {
                var tags = GetEntityTags(resource);
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        _gvwTags.Rows.Add(tag.Key, tag.Value);
                    }
                }
            }
        }
    }

    private void DataGridView_Results_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_gvwResults.Columns[e.ColumnIndex].DataPropertyName.EndsWith("Tags") && e.Value is IDictionary<string, string> tags)
        {
            var sortedTags = tags.OrderBy(tag => tag.Key);
            e.Value = string.Join(", \n", sortedTags.Select(tag => $"\"{tag.Key}\": \"{tag.Value}\""));
            e.FormattingApplied = true;
        }
    }

    private List<Resource> ApplyQuickFilters(List<Resource> resources)
    {
        var filtered = resources;

        // Apply Quick Filter 1
        if (_cboQuickFilter1Column.SelectedItem != null && !string.IsNullOrWhiteSpace(_txtQuickFilter1Text.Text))
        {
            string column1 = _cboQuickFilter1Column.SelectedItem.ToString();
            string pattern1 = _txtQuickFilter1Text.Text;

            try
            {
                var regex1 = new Regex(pattern1, RegexOptions.IgnoreCase);

                filtered = filtered.Where(r =>
                {
                    var value = GetPropertyValue(r, column1)?.ToString() ?? string.Empty;
                    return regex1.IsMatch(value);
                }).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Invalid regex pattern in Quick Filter 1.");
                MessageBox.Show(this, "Invalid regex pattern in Quick Filter 1.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Apply Quick Filter 2
        if (_cboQuickFilter2Column.SelectedItem != null && !string.IsNullOrWhiteSpace(_txtQuickFilter2Text.Text))
        {
            string column2 = _cboQuickFilter2Column.SelectedItem.ToString();
            string pattern2 = _txtQuickFilter2Text.Text;

            try
            {
                var regex2 = new Regex(pattern2, RegexOptions.IgnoreCase);

                filtered = filtered.Where(r =>
                {
                    var value = GetPropertyValue(r, column2)?.ToString() ?? string.Empty;
                    return regex2.IsMatch(value);
                }).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Invalid regex pattern in Quick Filter 2.");
                MessageBox.Show(this, "Invalid regex pattern in Quick Filter 2.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        return filtered;
    }

    private void DisplayResults(bool resizeColumns = true)
    {
        var isInitialDisplay = _gvwResults.DataSource == null || ((List<Resource>)_gvwResults.DataSource).Count == 0;

        var displayResources = ApplyQuickFilters(_resources);

        _gvwResults.DataSource = displayResources;

        if (isInitialDisplay || resizeColumns)
        {
            UpdateResultsColumnsWidth();
        }

        UpdateResultsCountLabel();
        UpdateFilteredResultsCountLabel();
    }

    private void UpdateResultsColumnsWidth()
    {
        if (_gvwResults.Columns.Count == 0)
        {
            return;
        }
        var columnWidth = (_gvwResults.Width - _gvwResults.RowHeadersWidth - 18) / _gvwResults.Columns.Count;
        foreach (DataGridViewColumn column in _gvwResults.Columns)
        {
            column.Width = columnWidth;
        }
    }

    private void UpdateResultsCountLabel(bool reset = false)
    {
        int subscriptionsCount = _resources.Count(r => r.EntityType.Equals("Subscription", StringComparison.OrdinalIgnoreCase));
        int resourceGroupsCount = _resources.Count(r => r.EntityType.Equals("ResourceGroup", StringComparison.OrdinalIgnoreCase));
        int resourcesCount = _resources.Count(r => r.EntityType.Equals("Resource", StringComparison.OrdinalIgnoreCase));
        int overallItems = subscriptionsCount + resourceGroupsCount + resourcesCount;

        _lblResultsCount.Text = $"({subscriptionsCount} subscriptions, {resourceGroupsCount} resource groups, {resourcesCount} resources, {overallItems} items)";

        UpdateFilteredResultsCountLabel();
    }

    private void UpdateFilteredResultsCountLabel()
    {
        if (_gvwResults.DataSource is List<Resource> displayedResources)
        {
            int filteredSubscriptionsCount = displayedResources.Count(r => r.EntityType.Equals("Subscription", StringComparison.OrdinalIgnoreCase));
            int filteredResourceGroupsCount = displayedResources.Count(r => r.EntityType.Equals("ResourceGroup", StringComparison.OrdinalIgnoreCase));
            int filteredResourcesCount = displayedResources.Count(r => r.EntityType.Equals("Resource", StringComparison.OrdinalIgnoreCase));
            int filteredOverallItems = filteredSubscriptionsCount + filteredResourceGroupsCount + filteredResourcesCount;

            _lblResultsFilteredCount.Text = $"({filteredSubscriptionsCount} subscriptions, {filteredResourceGroupsCount} resource groups, {filteredResourcesCount} resources, {filteredOverallItems} items)";
        }
        else
        {
            _lblResultsFilteredCount.Text = "(0 subscriptions, 0 resource groups, 0 resources, 0 items)";
        }
    }

    private async void Button_ApplyTags_Click(object sender, EventArgs e)
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
            MessageBox.Show(this, "Tags applied successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply tags.");
            MessageBox.Show(this, "Failed to apply tags. Please check the error log file in the program's AppData Local folder for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private List<Resource> GetSelectedResources()
    {
        var selectedResources = new List<Resource>();
        foreach (DataGridViewRow row in _gvwResults.SelectedRows)
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
        foreach (DataGridViewRow row in _gvwTags.Rows)
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
            var resTags = GetEntityTags(resource);

            // Add or update tags
            foreach (var tag in tags)
            {
                resTags[tag.Key] = tag.Value;
            }

            // Remove tags not present in the input dictionary
            var keysToRemove = resTags.Keys.Except(tags.Keys).ToList();
            foreach (var key in keysToRemove)
            {
                resTags.Remove(key);
            }
        }
        _gvwResults.Refresh();
    }

    private static IDictionary<string, string> GetEntityTags(Resource resource)
    {
        var resTags = resource.ResourceTags;
        if (resource.EntityType.Equals("ResourceGroup", StringComparison.OrdinalIgnoreCase))
        {
            resTags = resource.ResourceGroupTags;
        }
        else if (resource.EntityType.Equals("Subscription", StringComparison.OrdinalIgnoreCase))
        {
            resTags = resource.SubscriptionTags;
        }
        return resTags;
    }

    private void ComboBox_RecentSearches_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_cboRecentSearches.SelectedItem is RecentSearchItem item)
        {
            _txtSearchQuery.Text = item.ActualText;
            _cboRecentSearches.SelectedIndex = -1;
        }
    }

    private void TextBox_SearchQuery_TextChanged(object sender, EventArgs e)
    {
        var normalizedQuery = _txtSearchQuery.Text.ToLower().Replace(" ", "").Replace("\r\n", "").Replace("\n", "");
        if (normalizedQuery.StartsWith("resources|") || normalizedQuery.StartsWith("resourcecontainers|"))
        {
            _lblQueryMode.Text = "(KQL full expression) --> not supported";
            _queryMode = QueryMode.KqlFull;
        }
        else if (normalizedQuery.StartsWith("|"))
        {
            _lblQueryMode.Text = "(KQL filter-only expression)";
            _queryMode = QueryMode.KqlFilter;
        }
        else
        {
            _lblQueryMode.Text = "(regular expression, applied to SubscriptionName, ResourceGroup and ResourceName)";
            _queryMode = QueryMode.Regex;
        }
        _fullQuery = BuildQuery();
    }

    private void TextBox_SearchQuery_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == 10 && ModifierKeys.HasFlag(Keys.Control))
        {
            e.Handled = true;
            Button_PerformSearch_Click(sender, e);
        }
    }

    private void ComboBox_TagTemplates_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_cboTagTemplates.SelectedIndex < 0)
        {
            return;
        }
        var tags = _tagTemplates[_cboTagTemplates.SelectedIndex].Tags;
        foreach (var tag in tags)
        {
            var existingRow = _gvwTags.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells["Key"].Value?.ToString() == tag.Key);
            if (existingRow != null)
            {
                if (!string.IsNullOrEmpty(tag.Value))
                {
                    existingRow.Cells["Value"].Value = tag.Value;
                }
            }
            else
            {
                _gvwTags.Rows.Add(tag.Key, tag.Value);
            }
        }
        _cboTagTemplates.SelectedIndex = -1;
    }

    private void MainForm_ResizeEnd(object sender, EventArgs e)
    {
        UpdateResultsColumnsWidth();
    }

    private void Button_CopyQuery_Click(object sender, EventArgs e)
    {
        Clipboard.SetText(_fullQuery);
    }

    private void MainForm_SizeChanged(object sender, EventArgs e)
    {
        _lastResizeTime = DateTime.Now;
    }

    private void ResizeTimer_Tick(object sender, EventArgs e)
    {
        var timeDiff = DateTime.Now - _lastResizeTime;
        var threshold = TimeSpan.FromMilliseconds(500);
        if (timeDiff > threshold && timeDiff < TimeSpan.FromSeconds(3))
        {
            MainForm_ResizeEnd(this, EventArgs.Empty);
        }
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        LoadRecentSearches();
        LoadTagTemplates();

        _cboQuickFilter1Column.SelectedIndexChanged += FilterInputsChanged;
        _txtQuickFilter1Text.TextChanged += QuickFilter1_TextChanged;

        _cboQuickFilter2Column.SelectedIndexChanged += FilterInputsChanged;
        _txtQuickFilter2Text.TextChanged += QuickFilter2_TextChanged;
    }

    private void FilterInputsChanged(object sender, EventArgs e)
    {
        if (sender == _cboQuickFilter1Column)
        {
            if (!string.IsNullOrWhiteSpace(_txtQuickFilter1Text.Text))
            {
                DisplayResults(false);
            }
        }
        else if (sender == _cboQuickFilter2Column)
        {
            if (!string.IsNullOrWhiteSpace(_txtQuickFilter2Text.Text))
            {
                DisplayResults(false);
            }
        }
    }

    private void QuickFilter1_TextChanged(object sender, EventArgs e)
    {
        _debounceTimer1.Stop();
        _debounceTimer1.Start();
    }

    private void QuickFilter2_TextChanged(object sender, EventArgs e)
    {
        _debounceTimer2.Stop();
        _debounceTimer2.Start();
    }

    private void DebounceTimer1_Tick(object sender, EventArgs e)
    {
        _debounceTimer1.Stop();
        DisplayResults();
    }

    private void DebounceTimer2_Tick(object sender, EventArgs e)
    {
        _debounceTimer2.Stop();
        DisplayResults();
    }

    private void LinkLabel_ResetQuickFilters_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        _cboQuickFilter1Column.SelectedIndex = -1;
        _txtQuickFilter1Text.Text = string.Empty;

        _cboQuickFilter2Column.SelectedIndex = -1;
        _txtQuickFilter2Text.Text = string.Empty;
    }
}
