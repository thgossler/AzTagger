// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using AzTagger.Models;
using AzTagger.Services;
using Microsoft.Graph;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace AzTagger;

public partial class MainForm : Form
{
    private const int ClickDelayMsecs = 300;
    private const int DebounceDelayMsecs = 500;
    private const int MaxToolTipLineLength = 60;

    private readonly Settings _settings;

    private Timer _resizeTimer;
    private DateTime _lastResizeTime;

    private AzureService _azureService;

    private List<Resource> _resources;
    private ContextMenuStrip _resultsDataGridViewContextMenu;
    private DataGridViewCell _contextMenuClickedCell;
    private string _fullQuery = string.Empty;
    private CancellationTokenSource _queryCancellationTokenSource;
    private string _currentSortColumn = string.Empty;
    private Timer _debounceTimer1;
    private Timer _debounceTimer2;
    private Timer _headerColumnClickTimer;
    private bool _isHeaderColumnDoubleClick = false;
    private int _headerColumnClickColumnIndex = 0;
    private bool _sortAscending = true;
    private CustomToolTipForm _customToolTipForm;
    private DataGridViewCell _lastCellWithToolTip;
    private Timer _tooltipTimer;
    private DataGridViewCellEventArgs _currentCellEventArgs;

    private ContextMenuStrip _quickFilterContextMenu;
    private ToolStripMenuItem _quickFilterExcludeCurrentTextMenuItem;
    private ToolStripMenuItem _quickFilterExcludeTextRegExMenuItem;

    private List<TagTemplate> _tagTemplates;
    private List<string> _tagsToRemove = new List<string>();

    private const string _baseQuery = @"resources
| join kind=leftouter (
    resourcecontainers
    | where type =~ ""microsoft.resources/subscriptions/resourcegroups""
    | project rg_subscriptionId = subscriptionId, resourceGroup = name, resourceGroupTags = tags
) on $left.subscriptionId == $right.rg_subscriptionId and $left.resourceGroup == $right.resourceGroup
| join kind=leftouter (
    resourcecontainers
    | where type =~ ""microsoft.resources/subscriptions"" and not(properties['state'] =~ 'disabled')
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
        ResourceType = type, 
        SubscriptionTags = subscriptionTags, 
        ResourceGroupTags = tags, 
        ResourceTags = """"
)
| union (
    resourcecontainers
    | where type =~ ""microsoft.resources/subscriptions"" and not(properties['state'] =~ 'disabled')
    | project 
        EntityType = ""Subscription"",
        Id = id, 
        SubscriptionName = name, 
        SubscriptionId = subscriptionId, 
        ResourceGroup = """", 
        ResourceName = """", 
        ResourceType = type, 
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
| order by EntityType desc, (tolower(SubscriptionName)) asc, (tolower(ResourceGroup)) asc, (tolower(ResourceName)) asc
";

    enum QueryMode
    {
        KqlFilter,
        Regex,
        KqlFull
    }
    private QueryMode _queryMode = QueryMode.Regex;

    private enum ActivityIndicatorType
    {
        Query,
        Results,
        All
    }

    public MainForm(Settings settings)
    {
        _settings = settings;
        _azureService = new AzureService(_settings);

        InitializeComponent();
        AutoScaleMode = AutoScaleMode.Dpi;

        _customToolTipForm = new CustomToolTipForm();

        var version = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
        version = version.Substring(0, version.LastIndexOf('.'));
        _lblVersion.Text = $"Version: {version}";

        _fullQuery = BuildQuery();
        _resources = new List<Resource>();

        _lblQueryMode.Text = string.Empty;
        InitializeResultsDataGridView();
        InitializeTagsDataGridView();
        InitializeResultsDataGridViewContextMenu();
        InitializeQuickFilterTextBoxContextMenus();
        InitializeResizeTimer();
        InitializeQuickFilterComboBoxes();
        InitializeDebounceTimers();

        _headerColumnClickTimer = new Timer();
        _headerColumnClickTimer.Interval = ClickDelayMsecs;
        _headerColumnClickTimer.Tick += ClickTimer_Tick;

        _tooltipTimer = new Timer();
        _tooltipTimer.Interval = _toolTip.AutomaticDelay;
        _tooltipTimer.Tick += ToolTipTimer_Tick;

        if (Application.IsDarkModeEnabled)
        {
            HandleDarkModeExceptions(this);
        }
    }

    private void InitializeResultsDataGridViewContextMenu()
    {
        _resultsDataGridViewContextMenu = new ContextMenuStrip();

        var openInAzurePortalMenuItem = new ToolStripMenuItem("Open in Azure Portal");
        openInAzurePortalMenuItem.Click += MenuItem_OpenInAzurePortal_Click;
        _resultsDataGridViewContextMenu.Items.Add(openInAzurePortalMenuItem);

        var openUrlsInTagValuesMenuItem = new ToolStripMenuItem("Open URLs in tags");
        openUrlsInTagValuesMenuItem.Click += MenuItem_OpenUrlsInTagValues_Click;
        _resultsDataGridViewContextMenu.Items.Add(openUrlsInTagValuesMenuItem);

        var copyCellValueMenuItem = new ToolStripMenuItem("Copy cell value");
        copyCellValueMenuItem.Click += MenuItem_CopyCellValue_Click;
        _resultsDataGridViewContextMenu.Items.Add(copyCellValueMenuItem);

        var addToFilterQueryMenuItem = new ToolStripMenuItem("Add to filter query");
        addToFilterQueryMenuItem.Name = "AddToFilterQueryMenuItem";
        addToFilterQueryMenuItem.Click += MenuItem_AddToFilterQuery_Click;
        _resultsDataGridViewContextMenu.Items.Add(addToFilterQueryMenuItem);

        var excludeInFilterQueryMenuItem = new ToolStripMenuItem("Exclude in filter query");
        excludeInFilterQueryMenuItem.Name = "ExcludeInFilterQueryMenuItem";
        excludeInFilterQueryMenuItem.Click += MenuItem_AddToFilterQuery_Click;
        _resultsDataGridViewContextMenu.Items.Add(excludeInFilterQueryMenuItem);

        var refreshTagsMenuItem = new ToolStripMenuItem("Refresh tags from Azure");
        refreshTagsMenuItem.Click += MenuItem_RefreshTags_Click;
        _resultsDataGridViewContextMenu.Items.Add(refreshTagsMenuItem);
    }

    private void InitializeQuickFilterTextBoxContextMenus()
    {
        _quickFilterContextMenu = new ContextMenuStrip();

        _quickFilterExcludeTextRegExMenuItem = new ToolStripMenuItem("Replace with RegEx to exclude text");
        _quickFilterExcludeTextRegExMenuItem.Click += MenuItem_QuickFilterExcludeTextRegEx_Click;
        _quickFilterContextMenu.Items.Add(_quickFilterExcludeTextRegExMenuItem);

        _quickFilterExcludeCurrentTextMenuItem = new ToolStripMenuItem("Convert to RegEx excluding the current search text");
        _quickFilterExcludeCurrentTextMenuItem.Click += MenuItem_QuickFilterExcludeCurrentText_Click;
        _quickFilterContextMenu.Items.Add(_quickFilterExcludeCurrentTextMenuItem);

        _quickFilterContextMenu.Opening += ContextMenu_QuickFilter_Opening;

        _txtQuickFilter1Text.ContextMenuStrip = _quickFilterContextMenu;
        _txtQuickFilter2Text.ContextMenuStrip = _quickFilterContextMenu;
    }

    private void InitializeResultsDataGridView()
    {
        _gvwResults.AutoGenerateColumns = true;
        _gvwResults.RowTemplate.Height = 24;
        _gvwResults.CellFormatting += DataGridView_Results_CellFormatting;
        _gvwResults.CellMouseClick += DataGridView_Results_CellMouseClick;
        _gvwResults.CellDoubleClick += DataGridView_Results_CellDoubleClick;
        _gvwResults.SelectionChanged += DataGridView_Results_SelectionChanged;
        _gvwResults.ColumnHeaderMouseClick += DataGridView_Results_ColumnHeaderMouseClick;
        _gvwResults.ColumnHeaderMouseDoubleClick += DataGridView_Results_ColumnHeaderMouseDoubleClick;
        _gvwResults.CellMouseEnter += DataGridView_Results_CellMouseEnter;
        _gvwResults.CellMouseLeave += DataGridView_Results_CellMouseLeave;
        _gvwResults.MouseLeave += DataGridView_Results_MouseLeave;
        _gvwResults.DataSource = new List<Resource>();

        if (Application.IsDarkModeEnabled)
        {
            _gvwResults.BackgroundColor = Color.FromArgb(30, 30, 30);
            _gvwResults.ForeColor = Color.White;
            _gvwResults.GridColor = Color.FromArgb(45, 45, 45);
            _gvwResults.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            _gvwResults.DefaultCellStyle.ForeColor = Color.White;
            _gvwResults.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 153, 255);
            _gvwResults.DefaultCellStyle.SelectionForeColor = Color.Black;
            _gvwResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            _gvwResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _gvwResults.EnableHeadersVisualStyles = false;
            _gvwResults.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            _gvwResults.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
        }

        foreach (DataGridViewColumn column in _gvwResults.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.HeaderCell.ToolTipText = WrapText("Double-click to add the column name to the search query. Single-click to toggle the column’s sort order between ascending and descending.", MaxToolTipLineLength);
        }

        UpdateResultsColumnsWidth();
    }

    private void InitializeQuickFilterComboBoxes()
    {
        var resourceProperties = typeof(Resource).GetProperties().Select(p => p.Name).ToArray();
        _cboQuickFilter1Column.Items.Add(string.Empty);
        _cboQuickFilter1Column.Items.AddRange(resourceProperties);
        _cboQuickFilter2Column.Items.Add(string.Empty);
        _cboQuickFilter2Column.Items.AddRange(resourceProperties);
    }

    private void InitializeTagsDataGridView()
    {
        _gvwTags.AutoGenerateColumns = true;
        _gvwTags.CellFormatting += DataGridView_Results_CellFormatting;
        _gvwTags.KeyDown += DataGridView_Tags_KeyDown;

        if (Application.IsDarkModeEnabled)
        {
            _gvwTags.BackgroundColor = Color.FromArgb(30, 30, 30);
            _gvwTags.ForeColor = Color.White;
            _gvwTags.GridColor = Color.FromArgb(45, 45, 45);
            _gvwTags.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            _gvwTags.DefaultCellStyle.ForeColor = Color.White;
            _gvwTags.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 153, 255);
            _gvwTags.DefaultCellStyle.SelectionForeColor = Color.Black;
            _gvwTags.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            _gvwTags.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _gvwTags.EnableHeadersVisualStyles = false;
            _gvwTags.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            _gvwTags.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
        }
    }

    private void InitializeDebounceTimers()
    {
        _debounceTimer1 = new Timer();
        _debounceTimer1.Interval = DebounceDelayMsecs;
        _debounceTimer1.Tick += Timer_DebounceTimer1_Tick;

        _debounceTimer2 = new Timer();
        _debounceTimer2.Interval = DebounceDelayMsecs;
        _debounceTimer2.Tick += Timer_DebounceTimer2_Tick;
    }

    private void InitializeResizeTimer()
    {
        _resizeTimer = new System.Windows.Forms.Timer();
        _resizeTimer.Interval = 500;
        _resizeTimer.Tick += ResizeTimer_Tick;
        _resizeTimer.Start();
    }

    private void HandleDarkModeExceptions(Control parent)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (ctrl is LinkLabel linkLabel)
            {
                linkLabel.LinkColor = Color.LightBlue;
                linkLabel.ActiveLinkColor = Color.LightBlue;
            }

            if (ctrl is Button button)
            {
                button.BackColor = Color.FromArgb(45, 45, 48);
                button.ForeColor = Color.White;
            }

            if (ctrl is ProgressBar progressBar)
            {
                progressBar.BringToFront();
                progressBar.BackColor = Color.FromArgb(45, 45, 48);
                progressBar.ForeColor = Color.GreenYellow;
            }

            if (ctrl.HasChildren)
            {
                HandleDarkModeExceptions(ctrl);
            }
        }
    }

    private async void Form_Load(object sender, EventArgs e)
    {
        RestoreLastWindowState();

        ProcessToolTips(this, _toolTip, MaxToolTipLineLength);

        LoadRecentSearchQueries();
        LoadSavedSearchQueries();
        LoadTagTemplates();

        _cboQuickFilter1Column.SelectedIndexChanged += ComboBox_QuickFilter_SelectedIndexChanged;
        _txtQuickFilter1Text.TextChanged += TextBox_QuickFilter1_TextChanged;

        _cboQuickFilter2Column.SelectedIndexChanged += ComboBox_QuickFilter_SelectedIndexChanged;
        _txtQuickFilter2Text.TextChanged += TextBox_QuickFilter2_TextChanged;

        UpdateAzureContextDropDownList();

        var selectedAzureContext = _settings.GetAzureContext();
        if (string.IsNullOrWhiteSpace(selectedAzureContext.TenantId))
        {
            var dialog = new AzureContextConfigDialog(_settings);
            var result = await dialog.ShowDialogAsync(this);
            if (result == DialogResult.OK)
            {
                selectedAzureContext = _settings.GetAzureContext();
                if (string.IsNullOrWhiteSpace(selectedAzureContext.TenantId) || string.IsNullOrWhiteSpace(selectedAzureContext.ClientAppId))
                {
                    Log.Error("No valid Azure Context is configured. The application will now close.");
                    MessageBox.Show(this, "No valid Azure Context is configured. The application will now close.", "AzTagger", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }
                SettingsService.Save(_settings);
            }
            else
            {
                Close();
                return;
            }
        }

        _cboAzureContext.SelectedItem = _settings.GetAzureContext().Name;
    }

    private void Form_SizeChanged(object sender, EventArgs e)
    {
        _lastResizeTime = DateTime.Now;
    }

    private void Form_ResizeEnd(object sender, EventArgs e)
    {
        UpdateResultsColumnsWidth();
        UpdateTagsColumnsWidth();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        WindowState = FormWindowState.Normal;
        _settings.WindowSize = Size;
        _settings.WindowLocation = Location;
        _settings.SplitterPosition = _splitContainer.SplitterDistance;
        _settings.LastSearchQuery = _txtSearchQuery.Text;
        _settings.LastQuickFilter1Text = _txtQuickFilter1Text.Text;
        _settings.LastQuickFilter2Text = _txtQuickFilter2Text.Text;
        SettingsService.Save(_settings);
        _customToolTipForm?.Dispose();
        base.OnFormClosing(e);
    }

    private async void Button_PerformSearch_Click(object sender, EventArgs e)
    {
        if (_queryMode == QueryMode.KqlFull)
        {
            MessageBox.Show(this, "KQL full expressions are not supported! Please use KQL filter-only expressions or a regular expression.",
                "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        bool isSignedIn = await SignInToAzureAsync();
        if (!isSignedIn)
        {
            MessageBox.Show(this, "Azure sign-in failed. Please check your credentials and try again.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _gvwResults.DataSource = new List<Resource>();
        UpdateResultsCountLabel(true);

        if (_queryCancellationTokenSource != null)
        {
            _queryCancellationTokenSource.Cancel();
            await Task.Delay(300);
        }
        _queryCancellationTokenSource = new CancellationTokenSource();

        ShowActivityIndicator(ActivityIndicatorType.Query, true);
        SaveRecentSearch(_txtSearchQuery.Text);

        _cboQuickFilter1Column.SelectedIndex = -1;
        _cboQuickFilter2Column.SelectedIndex = -1;

        await SearchResourcesAsync(_queryCancellationTokenSource.Token);

        ShowActivityIndicator(ActivityIndicatorType.Query, false);
    }

    private async void Button_RefreshSignin_Click(object sender, EventArgs e)
    {
        bool isSignedIn = await SignInToAzureAsync(true);
        if (!isSignedIn)
        {
            MessageBox.Show(this, "Azure sign-in failed. Please check your credentials and try again.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
    }

    private void Button_ClearSearchQuery_Click(object sender, EventArgs e)
    {
        _txtSearchQuery.Text = string.Empty;
        _txtSearchQuery.Focus();
    }

    private void Button_SaveQuery_Click(object sender, EventArgs e)
    {
        string queryText = _txtSearchQuery.Text;
        if (string.IsNullOrWhiteSpace(queryText))
        {
            MessageBox.Show(this, "Cannot save an empty query.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using (var inputDialog = new InputDialog(this, "Enter a name for the saved query:"))
        {
            if (inputDialog.ShowDialog(this) == DialogResult.OK)
            {
                string queryName = inputDialog.InputText.Trim();
                if (string.IsNullOrWhiteSpace(queryName))
                {
                    MessageBox.Show(this, "Query name cannot be empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var existingQuery = _settings.SavedSearches.FirstOrDefault(q => q.Name.Equals(queryName, StringComparison.OrdinalIgnoreCase));
                if (existingQuery != null)
                {
                    var result = MessageBox.Show(this, $"A query with the name '{queryName}' already exists. Do you want to overwrite it?",
                        "Confirm Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                    {
                        return;
                    }
                    existingQuery.SearchQuery = queryText;
                }
                else
                {
                    var savedQuery = new SavedSearchItem(queryName, queryText);
                    _settings.SavedSearches.Add(savedQuery);
                    _cboSavedQueries.Items.Add(savedQuery);
                }

                SettingsService.Save(_settings);
            }
        }
    }

    private void Button_CopyQuery_Click(object sender, EventArgs e)
    {
        Clipboard.SetText(_fullQuery);
    }

    private async void Button_ApplyTags_Click(object sender, EventArgs e)
    {
        _btnApplyTags.Enabled = false;
        ShowActivityIndicator(ActivityIndicatorType.Results, true);

        await ApplyTagsAsync();

        ShowActivityIndicator(ActivityIndicatorType.Results, false);
        _btnApplyTags.Enabled = true;
    }

    private void MenuItem_OpenUrlsInTagValues_Click(object sender, EventArgs e)
    {
        if (_gvwResults.SelectedRows.Count > 0)
        {
            var selectedRow = _gvwResults.SelectedRows[0];
            var resource = selectedRow.DataBoundItem as Resource;

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

    private void MenuItem_OpenInAzurePortal_Click(object sender, EventArgs e)
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

    private void MenuItem_CopyCellValue_Click(object sender, EventArgs e)
    {
        if (_contextMenuClickedCell?.Value != null)
        {
            if (_contextMenuClickedCell.Value is IDictionary<string, string> tags)
            {
                Clipboard.SetText(FormatTags(tags, Environment.NewLine));
            }
            else
            {
                Clipboard.SetText(_contextMenuClickedCell.Value.ToString());
            }
        }
    }

    private void MenuItem_AddToFilterQuery_Click(object sender, EventArgs e)
    {
        if (_contextMenuClickedCell?.Value == null)
        {
            return;
        }

        var menuItem = sender as ToolStripMenuItem;
        if (menuItem == null)
        {
            return;
        }

        var queryText = _txtSearchQuery.Text;
        var columnName = _gvwResults.Columns[_contextMenuClickedCell.ColumnIndex].DataPropertyName;

        if (_contextMenuClickedCell.Value is IDictionary<string, string> tags)
        {
            var cellValue = _contextMenuClickedCell.Value;
            var tagFilter = string.Join(" and ", tags.Select(tag => $"{columnName}[\"{tag.Key}\"] =~ '{tag.Value}'"));
            queryText = queryText.TrimEnd();
            if (menuItem.Name.Equals("ExcludeInFilterQueryMenuItem"))
            {
                queryText += Environment.NewLine + $"| where not({tagFilter})";
            }
            else
            {
                queryText += Environment.NewLine + $"| where {tagFilter}";
            }
        }
        else
        {
            var cellValue = _contextMenuClickedCell.Value.ToString();
            queryText = queryText.TrimEnd();
            if (menuItem.Name.Equals("ExcludeInFilterQueryMenuItem"))
            {
                queryText += Environment.NewLine + $"| where {columnName} != '{cellValue}'";
            }
            else
            {
                queryText += Environment.NewLine + $"| where {columnName} =~ '{cellValue}'";
            }
        }

        _txtSearchQuery.Text = queryText;
    }

    private async void MenuItem_RefreshTags_Click(object sender, EventArgs e)
    {
        if (_gvwResults.SelectedRows.Count == 0)
        {
            return;
        }

        ShowActivityIndicator(ActivityIndicatorType.Query, true);

        try
        {
            var selectedResources = _gvwResults.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(row => row.DataBoundItem as Resource)
                .Where(resource => resource != null)
                .ToList();

            var resourceIds = string.Join(", ", selectedResources.Select(r => $"'{r.Id}'"));
            var query = _baseQuery + $@"| where Id in ({resourceIds}) | project Id, SubscriptionTags, ResourceGroupTags, ResourceTags";

            var updatedResources = await _azureService.QueryResourcesAsync(query);

            foreach (var updatedResource in updatedResources)
            {
                var localResource = _resources.FirstOrDefault(r => r.Id == updatedResource.Id);
                if (localResource != null)
                {
                    localResource.SubscriptionTags = updatedResource.SubscriptionTags;
                    localResource.ResourceGroupTags = updatedResource.ResourceGroupTags;
                    localResource.ResourceTags = updatedResource.ResourceTags;
                }
            }

            _gvwResults.Refresh();

            DataGridView_Results_SelectionChanged(null, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh tags for selected resources from Azure.");
        }
        finally
        {
            ShowActivityIndicator(ActivityIndicatorType.Query, false);
        }
    }

    private void ContextMenu_QuickFilter_Opening(object sender, CancelEventArgs e)
    {
        if (sender is ContextMenuStrip cms && cms.SourceControl is TextBox tb)
        {
            string text = tb.Text;

            char[] forbiddenChars = ['!', '?', '.', ':', '+', '*', '(', ')', '[', ']', '{', '}', '\\', '^', '<', '>'];
            _quickFilterExcludeCurrentTextMenuItem.Enabled = !text.Any(c => forbiddenChars.Contains(c)) && text.Trim().Length > 0;

            _quickFilterExcludeTextRegExMenuItem.Enabled = text.Trim().Length == 0;
        }
    }

    private void MenuItem_QuickFilterExcludeCurrentText_Click(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem)
        {
            var cms = ((ToolStripMenuItem)sender).Owner as ContextMenuStrip;
            if (cms?.SourceControl is TextBox tb)
            {
                string searchText = tb.Text;
                string newRegex = $"^(?!.*{searchText}).+$";
                tb.Text = newRegex;
            }
        }
    }

    private void MenuItem_QuickFilterExcludeTextRegEx_Click(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem)
        {
            var cms = ((ToolStripMenuItem)sender).Owner as ContextMenuStrip;
            if (cms?.SourceControl is TextBox tb)
            {
                string searchText = "TEXT";
                string newRegex = $"^(?!.*{searchText}).+$";
                tb.Text = newRegex;
            }
        }
    }

    private void DataGridView_Results_SelectionChanged(object sender, EventArgs e)
    {
        _gvwTags.Rows.Clear();
        _tagsToRemove.Clear();

        if (_gvwResults.SelectedRows.Count > 0)
        {
            var selectedResources = _gvwResults.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(row => row.DataBoundItem as Resource)
                .Where(resource => resource != null)
                .ToList();

            if (selectedResources.Count == 1)
            {
                var resource = selectedResources.First();
                var tags = GetEntityTags(resource);
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        _gvwTags.Rows.Add(tag.Key, tag.Value);
                    }
                }
            }
            else
            {
                var commonTags = selectedResources
                    .Select(r => GetEntityTags(r))
                    .Where(tags => tags != null)
                    .Aggregate((prev, next) => prev.Keys.Intersect(next.Keys)
                                                    .ToDictionary(k => k, k => prev[k]));

                foreach (var tag in commonTags)
                {
                    bool allValuesEqual = selectedResources.All(r =>
                        GetEntityTags(r).TryGetValue(tag.Key, out var value) && value == tag.Value);
                    if (allValuesEqual)
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
            e.Value = FormatTags(tags);
            e.FormattingApplied = true;
        }
    }

    private void UpdateAzureContextDropDownList()
    {
        _cboAzureContext.Items.Clear();
        _cboAzureContext.Items.AddRange(_settings.AzureContexts.Select(c => c.Name).ToArray());
        _cboAzureContext.Items.Add("Edit...");
    }

    private void RestoreLastWindowState()
    {
        Size = _settings.WindowSize;
        StartPosition = FormStartPosition.Manual;
        if (_settings.WindowLocation != Point.Empty)
        {
            Location = _settings.WindowLocation;
        }
        else
        {
            CenterToScreen();
        }
        _splitContainer.SplitterDistance = _settings.SplitterPosition;
        _txtSearchQuery.Text = _settings.LastSearchQuery;
        _txtQuickFilter1Text.Text = _settings.LastQuickFilter1Text;
        _txtQuickFilter2Text.Text = _settings.LastQuickFilter2Text;
    }

    private string FormatTags(IDictionary<string, string> tags, string joinWith = ", \n")
    {
        var sortedTags = tags.OrderBy(tag => tag.Key);
        return string.Join(joinWith, sortedTags.Select(tag => $"\"{tag.Key}\": \"{tag.Value}\""));
    }

    private void DataGridView_Results_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
        {
            _gvwResults.ClearSelection();
            _gvwResults.Rows[e.RowIndex].Selected = true;
            _contextMenuClickedCell = _gvwResults.Rows[e.RowIndex].Cells[e.ColumnIndex];
            _resultsDataGridViewContextMenu.Show(Cursor.Position);
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

    private void DataGridView_Results_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            var column = _gvwResults.Columns[e.ColumnIndex];
            if (column != null &&
                column.DataPropertyName == "ResourceTags" ||
                column.DataPropertyName == "ResourceGroupTags" ||
                column.DataPropertyName == "SubscriptionTags")
            {
                _currentCellEventArgs = e;
                _tooltipTimer.Start();
                _gvwResults.ShowCellToolTips = false;
            }
            else
            {
                _gvwResults.ShowCellToolTips = true;
            }
        }
    }

    private void DataGridView_Results_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
    {
        _tooltipTimer.Stop();
        _customToolTipForm.Hide();
        _lastCellWithToolTip = null;
        _currentCellEventArgs = null;
        _gvwResults.ShowCellToolTips = true;
    }

    private void DataGridView_Results_MouseLeave(object sender, EventArgs e)
    {
        _gvwResults.ShowCellToolTips = true;
    }

    private void ToolTipTimer_Tick(object sender, EventArgs e)
    {
        _tooltipTimer.Stop();

        if (_currentCellEventArgs == null)
        {
            return;
        }

        var cellEventArgs = _currentCellEventArgs;
        var cell = _gvwResults[cellEventArgs.ColumnIndex, cellEventArgs.RowIndex];
        if (_lastCellWithToolTip == cell) return;

        var column = _gvwResults.Columns[cellEventArgs.ColumnIndex];
        if (column.DataPropertyName == "ResourceTags" ||
            column.DataPropertyName == "ResourceGroupTags" ||
            column.DataPropertyName == "SubscriptionTags")
        {
            var tags = cell.Value as IDictionary<string, string>;
            if (tags != null && tags.Count > 0)
            {
                string toolTipText = FormatTags(tags, Environment.NewLine);
                var mousePosition = Cursor.Position;
                _customToolTipForm.ShowToolTip(this, toolTipText, mousePosition, _gvwResults.Font);
                _lastCellWithToolTip = cell;
            }
        }
    }

    private void ClickTimer_Tick(object sender, EventArgs e)
    {
        _headerColumnClickTimer?.Stop();
        if (!_isHeaderColumnDoubleClick)
        {
            string columnName = _gvwResults.Columns[_headerColumnClickColumnIndex].DataPropertyName;

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
                ? _resources.OrderBy(r => GetPropertyValue(r, columnName)?.ToString(), StringComparer.OrdinalIgnoreCase).ToList()
                : _resources.OrderByDescending(r => GetPropertyValue(r, columnName)?.ToString(), StringComparer.OrdinalIgnoreCase).ToList();

            DisplayResults(false);
        }
    }

    private void ResizeTimer_Tick(object sender, EventArgs e)
    {
        var timeDiff = DateTime.Now - _lastResizeTime;
        var threshold = TimeSpan.FromMilliseconds(500);
        if (timeDiff > threshold && timeDiff < TimeSpan.FromSeconds(3))
        {
            Form_ResizeEnd(this, EventArgs.Empty);
        }
    }

    private void DataGridView_Results_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        _headerColumnClickColumnIndex = e.ColumnIndex;
        _isHeaderColumnDoubleClick = false;
        _headerColumnClickTimer?.Start();
    }

    private void DataGridView_Results_ColumnHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        _isHeaderColumnDoubleClick = true;
        _headerColumnClickTimer?.Stop();

        var columnName = _gvwResults.Columns[e.ColumnIndex].DataPropertyName;
        var selectionStart = _txtSearchQuery.SelectionStart;
        var selectionLength = _txtSearchQuery.SelectionLength;
        bool endsWithSpace = false;

        if (_txtSearchQuery.Text.Length == 0)
        {
            if (columnName == "ResourceTags" ||
                columnName == "ResourceGroupTags" ||
                columnName == "SubscriptionTags")
            {
                var queryText = $"| where {columnName}[''] =~ ''";
                _txtSearchQuery.Text = queryText;
                _txtSearchQuery.SelectionStart = queryText.Length - 8;
            }
            else
            {
                var queryText = $"| where {columnName} =~ ''";
                _txtSearchQuery.Text = queryText;
                _txtSearchQuery.SelectionStart = queryText.Length - 1;
            }
        }
        else
        {
            var queryText = _txtSearchQuery.Text;

            if (selectionLength > 0)
            {
                var selectedText = queryText.Substring(selectionStart, selectionLength);
                endsWithSpace = selectedText.EndsWith(" ");
                queryText = queryText.Remove(selectionStart, selectionLength);
            }

            var newText = queryText.Insert(selectionStart, columnName + (endsWithSpace ? " " : ""));
            _txtSearchQuery.Text = newText;
            _txtSearchQuery.SelectionStart = selectionStart + columnName.Length + (endsWithSpace ? 1 : 0);
        }

        _txtSearchQuery.Focus();
    }

    private void DataGridView_Tags_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete && _gvwTags.SelectedRows.Count > 0)
        {
            foreach (DataGridViewRow row in _gvwTags.SelectedRows)
            {
                MarkTagForDeletion(row);
            }
            e.Handled = true;
        }
    }

    private void ComboBox_RecentSearches_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_cboRecentSearches.SelectedItem is RecentSearchItem item)
        {
            _txtSearchQuery.Text = item.ActualText;
            _txtSearchQuery.SelectionStart = _txtSearchQuery.Text.Length;
            _txtSearchQuery.SelectionLength = 0;
            _txtSearchQuery.Focus();
            _cboRecentSearches.SelectedIndex = 0;
        }
    }

    private void ComboBox_SavedQueries_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_cboSavedQueries.SelectedItem is SavedSearchItem selectedQuery)
        {
            _txtSearchQuery.Text = selectedQuery.SearchQuery;
            _txtSearchQuery.SelectionStart = _txtSearchQuery.Text.Length;
            _txtSearchQuery.SelectionLength = 0;
            _txtSearchQuery.Focus();
            _cboSavedQueries.SelectedIndex = 0;
        }
    }

    private void TextBox_SearchQuery_TextChanged(object sender, EventArgs e)
    {
        var normalizedQuery = _txtSearchQuery.Text.ToLower().Replace(" ", "").Replace("\r\n", "").Replace("\n", "").Trim();
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
        else if (normalizedQuery.Length > 0)
        {
            _lblQueryMode.Text = "(regular expression, applied to SubscriptionName, ResourceGroup and ResourceName)";
            _queryMode = QueryMode.Regex;
        }
        else
        {
            _lblQueryMode.Text = string.Empty;
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

    private void TextBox_SearchQuery_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        int index = _txtSearchQuery.GetCharIndexFromPosition(e.Location);
        string text = _txtSearchQuery.Text;

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int start = index;
        int end = index;

        string delimiters = " \t\r\n.,;:!?()[]{}<>\"'=/~%$§#+*|";

        while (start > 0 && !delimiters.Contains(text[start - 1]))
        {
            start--;
        }

        while (end < text.Length && !delimiters.Contains(text[end]))
        {
            end++;
        }

        _txtSearchQuery.SelectionStart = start;
        _txtSearchQuery.SelectionLength = end - start;
    }

    private async void ComboBox_AzureContext_SelectedValueChanged(object sender, EventArgs e)
    {
        var selectedItem = _cboAzureContext.SelectedItem?.ToString();
        if (selectedItem.StartsWith("Edit"))
        {
            var dialog = new AzureContextConfigDialog(_settings);
            await dialog.ShowDialogAsync(this);
            UpdateAzureContextDropDownList();
        }
        else if (!string.IsNullOrWhiteSpace(selectedItem))
        {
            _settings.SelectAzureContext(selectedItem);
            SettingsService.Save(_settings);
        }
        _cboAzureContext.SelectedItem = _settings.GetAzureContext().Name;
    }

    private async void ComboBox_TagTemplates_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedIndex = _cboTagTemplates.SelectedIndex - 1;
        if (selectedIndex < 0)
        {
            return;
        }
        var tags = _tagTemplates[selectedIndex].Tags;
        tags = await ResolveTagVariables(tags);
        foreach (var tag in tags)
        {
            if (tag.Key.StartsWith("-"))
            {
                var tagKey = tag.Key.Substring(1);
                var row = _gvwTags.Rows.OfType<DataGridViewRow>()
                    .FirstOrDefault(r => r.Cells["Key"].Value?.ToString() == tagKey);
                MarkTagForDeletion(row);
            }
            else
            {
                var row = _gvwTags.Rows.OfType<DataGridViewRow>()
                    .FirstOrDefault(r => r.Cells["Key"].Value?.ToString() == tag.Key);
                if (row != null)
                {
                    if (!string.IsNullOrEmpty(tag.Value))
                    {
                        row.Cells["Value"].Value = tag.Value;
                        MarkTagForDeletion(row, false);
                    }
                }
                else
                {
                    _gvwTags.Rows.Add(tag.Key, tag.Value);
                }
            }
        }
        _cboTagTemplates.SelectedIndex = 0;
    }

    private void ComboBox_QuickFilter_SelectedIndexChanged(object sender, EventArgs e)
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

    private void TextBox_QuickFilter1_TextChanged(object sender, EventArgs e)
    {
        _debounceTimer1.Stop();
        _debounceTimer1.Start();
    }

    private void TextBox_QuickFilter2_TextChanged(object sender, EventArgs e)
    {
        _debounceTimer2.Stop();
        _debounceTimer2.Start();
    }

    private void Timer_DebounceTimer1_Tick(object sender, EventArgs e)
    {
        _debounceTimer1.Stop();
        DisplayResults();
    }

    private void Timer_DebounceTimer2_Tick(object sender, EventArgs e)
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

    private void LinkLabel_EditTagTemplates_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        var editor = Environment.GetEnvironmentVariable("EDITOR") ?? "notepad";
        Process.Start(editor, TagTemplatesService.TagTemplatesFilePath);
    }

    private void LinkLabel_Donation_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        const string url = "https://www.paypal.com/donate/?hosted_button_id=JVG7PFJ8DMW7J";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    private void LinkLabel_GitHubLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        const string url = "https://github.com/thgossler/AzTagger";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void LinkLabel_ResourceGraphDocs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        const string url = "https://learn.microsoft.com/en-us/azure/governance/resource-graph/concepts/query-language";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void LinkLabel_DotNetRegExDocs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        const string url = "https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void LinkLabel_EditSettingsFile_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        var editor = Environment.GetEnvironmentVariable("EDITOR") ?? "notepad";
        Process.Start(editor, SettingsService.SettingsFilePath);
    }

    private void LinkLabel_ResetToDefaults_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        _settings.ResetToWindowDefaults();
        RestoreLastWindowState();
    }

    private void ProcessToolTips(Control parent, System.Windows.Forms.ToolTip toolTip, int maxLineLength)
    {
        foreach (Control ctrl in parent.Controls)
        {
            string currentToolTip = toolTip.GetToolTip(ctrl);

            if (!string.IsNullOrEmpty(currentToolTip))
            {
                string wrappedToolTip = WrapText(currentToolTip, maxLineLength);

                toolTip.SetToolTip(ctrl, wrappedToolTip);
            }

            if (ctrl.HasChildren)
            {
                ProcessToolTips(ctrl, toolTip, maxLineLength);
            }
        }
    }

    private void MarkTagForDeletion(DataGridViewRow row, bool markForDeletion = true)
    {
        if (row == null)
        {
            return;
        }
        var tagKey = row.Cells["Key"]?.Value?.ToString().Trim();
        if (!string.IsNullOrEmpty(tagKey))
        {
            if (markForDeletion)
            {
                if (!_tagsToRemove.Contains(tagKey))
                {
                    _tagsToRemove.Add(tagKey);
                }
                var deletedFont = new Font(row.InheritedStyle.Font, FontStyle.Strikeout);
                row.DefaultCellStyle.Font = deletedFont;
                row.InheritedStyle.Font = deletedFont;
            }
            else
            {
                _tagsToRemove.Remove(tagKey);
                row.DefaultCellStyle.Font = null;
                row.InheritedStyle.Font = null;
            }
        }
    }

    private string WrapText(string text, int maxLineLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var words = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder wrappedText = new StringBuilder();
        int currentLineLength = 0;

        foreach (var word in words)
        {
            if (currentLineLength + word.Length + 1 > maxLineLength)
            {
                wrappedText.AppendLine();
                currentLineLength = 0;
            }

            wrappedText.Append(word + " ");
            currentLineLength += word.Length + 1;
        }

        return wrappedText.ToString().TrimEnd();
    }

    private void ShowActivityIndicator(ActivityIndicatorType type, bool visible)
    {
        if (type == ActivityIndicatorType.Query || type == ActivityIndicatorType.All)
        {
            _queryActivityIndicator.Style = ProgressBarStyle.Continuous;
            _queryActivityIndicator.Value = 0;
            _queryActivityIndicator.Style = ProgressBarStyle.Marquee;
            _queryActivityIndicator.MarqueeAnimationSpeed = 15;
            _queryActivityIndicator.Visible = visible;
        }
        if (type == ActivityIndicatorType.Results || type == ActivityIndicatorType.All)
        {
            _resultsActivityIndicator.Style = ProgressBarStyle.Continuous;
            _resultsActivityIndicator.Value = 0;
            _resultsActivityIndicator.Style = ProgressBarStyle.Marquee;
            _resultsActivityIndicator.MarqueeAnimationSpeed = 15;
            _resultsActivityIndicator.Visible = visible;
        }
    }

    private string BuildQuery()
    {
        var resultQuery = _baseQuery;

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
            resultQuery += filter;
        }

        return resultQuery;
    }

    private async Task<bool> SignInToAzureAsync(bool refresh = false)
    {
        try
        {
            if (_azureService == null)
            {
                _azureService = new AzureService(_settings);
            }
            await _azureService.SignInAsync(refresh);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Azure sign-in failed.");
            return false;
        }
    }

    private async Task SearchResourcesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _currentSortColumn = "EntityType";
            _sortAscending = false;

            var query = BuildQuery();
            _fullQuery = query;
            _resources = await _azureService.QueryResourcesAsync(query, cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                DisplayResults();
            }
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Log.Error(ex, "Search failed.");
                MessageBox.Show(this, "Search failed! Please check the error log file in the\nprogram's local app data folder for details.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OpenResourceIdInAzurePortal(string resourceId)
    {
        var portalUrl = _azureService != null ? _azureService.GetAzurePortalUrl() : "https://portal.azure.com";
        var url = $"{portalUrl}/#@{_settings.GetAzureContext().TenantId}/resource{resourceId}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void LoadRecentSearchQueries()
    {
        _cboRecentSearches.Items.Clear();
        _cboRecentSearches.Items.Add("Recent Search Queries");
        foreach (var queryText in _settings.RecentSearches)
        {
            _cboRecentSearches.Items.Add(new RecentSearchItem(queryText));
        }
        _cboRecentSearches.SelectedIndex = 0;
    }

    private void LoadSavedSearchQueries()
    {
        _cboSavedQueries.Items.Clear();
        _cboSavedQueries.Items.Add("Saved Search Queries");
        var queries = _settings.SavedSearches;
        queries.Sort();
        _cboSavedQueries.Items.AddRange(queries.ToArray());
        _cboSavedQueries.SelectedIndex = 0;
    }

    private void LoadTagTemplates()
    {
        _tagTemplates = TagTemplatesService.Load();
        _cboTagTemplates.Items.Clear();
        _cboTagTemplates.Items.Add("Tag Templates");
        _cboTagTemplates.Items.AddRange(_tagTemplates.Select(t => t.TemplateName).ToArray());
        _cboTagTemplates.SelectedIndex = 0;
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
            SettingsService.Save(_settings);

            var displayText = queryText.Replace("\r\n", " ").Replace("\n", " ");
            var itemsToRemove = _cboRecentSearches.Items.OfType<RecentSearchItem>().
                Where(i => i.DisplayText.Equals(displayText, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var item in itemsToRemove)
            {
                _cboRecentSearches.Items.Remove(item);
            }
            _cboRecentSearches.Items.Insert(1, new RecentSearchItem(queryText));

            if (_cboRecentSearches.Items.Count > 11)
            {
                _cboRecentSearches.Items.RemoveAt(_cboRecentSearches.Items.Count - 1);
            }
        }
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

    private List<Resource> ApplyQuickFilters(List<Resource> resources)
    {
        var filtered = resources;

        if (_cboQuickFilter1Column.SelectedItem != null &&
            _cboQuickFilter1Column.SelectedItem.ToString() != string.Empty &&
            !string.IsNullOrWhiteSpace(_txtQuickFilter1Text.Text))
        {
            string column1 = _cboQuickFilter1Column.SelectedItem.ToString();
            string pattern1 = _txtQuickFilter1Text.Text;

            try
            {
                var regex1 = new Regex(pattern1, RegexOptions.IgnoreCase);

                filtered = filtered.Where(r =>
                {
                    var value = GetPropertyValue(r, column1)?.ToString() ?? string.Empty;
                    var isMatch = regex1.IsMatch(value);
                    return isMatch;
                }).ToList();
                errorProvider1.SetError(_txtQuickFilter1Text, string.Empty);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Invalid regex pattern in Quick Filter 1.");
                errorProvider1.SetError(_txtQuickFilter1Text, "Invalid regex pattern");
            }
        }
        else
        {
            errorProvider1.SetError(_txtQuickFilter1Text, string.Empty);
        }

        if (_cboQuickFilter2Column.SelectedItem != null &&
            _cboQuickFilter2Column.SelectedItem.ToString() != string.Empty &&
            !string.IsNullOrWhiteSpace(_txtQuickFilter2Text.Text))
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
                errorProvider2.SetError(_txtQuickFilter2Text, string.Empty);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Invalid regex pattern in Quick Filter 2.");
                errorProvider2.SetError(_txtQuickFilter2Text, "Invalid regex pattern");
            }
        }
        else
        {
            errorProvider2.SetError(_txtQuickFilter2Text, string.Empty);
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
        var currentWidthAllColumns = 0;
        foreach (DataGridViewColumn column in _gvwResults.Columns)
        {
            currentWidthAllColumns += column.Width;
        }
        var newWidthAllColumns = _gvwResults.Width - _gvwResults.RowHeadersWidth - 18;
        foreach (DataGridViewColumn column in _gvwResults.Columns)
        {
            var factor = column.Width / (double)currentWidthAllColumns;
            column.Width = (int)(factor * newWidthAllColumns);
        }
    }

    private void UpdateTagsColumnsWidth()
    {
        var currentWidthAllColumns = 0;
        foreach (DataGridViewColumn column in _gvwTags.Columns)
        {
            currentWidthAllColumns += column.Width;
        }
        var newWidthAllColumns = _gvwTags.Width - _gvwTags.RowHeadersWidth - 18;
        foreach (DataGridViewColumn column in _gvwTags.Columns)
        {
            var factor = column.Width / (double)currentWidthAllColumns;
            column.Width = (int)(factor * newWidthAllColumns);
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

    private async Task ApplyTagsAsync()
    {
        try
        {
            var selectedResources = GetSelectedResources();
            var tagsToUpdate = GetTagsFromDataGridView();

            Dictionary<string, string> tagsToRemove = null;
            if (_tagsToRemove.Any())
            {
                tagsToRemove = _tagsToRemove.ToDictionary(k => k, v => (string)null);
            }

            var errors = await _azureService.UpdateTagsAsync(selectedResources, tagsToUpdate, tagsToRemove);
            if (errors.Length == 0)
            {
                UpdateLocalTags(selectedResources, tagsToUpdate, tagsToRemove);

                RemoveDeletedTagsFromView();
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("Not all tags could be applied successfully.").AppendLine();
                const int limit = 3;
                sb.AppendLine("Errors:");
                foreach (var error in errors.Distinct().Take(limit))
                {
                    sb.AppendLine(error).AppendLine();
                }
                if (errors.Length > limit)
                {
                    sb.Append($"... (overall {errors.Length} errors)");
                }
                var message = sb.ToString();
                Log.Error(message);

                ShowActivityIndicator(ActivityIndicatorType.All, false);

                MessageBox.Show(this, message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.Error(message);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply tags.");
            MessageBox.Show(this, "Not all tags could be applied successfully. Please check the error log file in the program's AppData Local folder for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    private void UpdateLocalTags(List<Resource> resources, Dictionary<string, string> tagsToUpdate, Dictionary<string, string> tagsToRemove)
    {
        var multipleSelected = resources.Count > 1;

        foreach (var resource in resources)
        {
            var resTags = GetEntityTags(resource);

            foreach (var tag in tagsToUpdate)
            {
                resTags[tag.Key] = tag.Value;
            }

            if (tagsToRemove != null)
            {
                foreach (var tagKey in tagsToRemove.Keys)
                {
                    resTags.Remove(tagKey);
                }
            }

            if (!multipleSelected)
            {
                var keysToRemove = resTags.Keys.Except(tagsToUpdate.Keys).ToList();
                foreach (var key in keysToRemove)
                {
                    resTags.Remove(key);
                }
            }
        }
        _gvwResults.Refresh();
    }

    private void RemoveDeletedTagsFromView()
    {
        foreach (var tagKey in _tagsToRemove)
        {
            var rowToRemove = _gvwTags.Rows
                .Cast<DataGridViewRow>()
                .FirstOrDefault(r => r.Cells["Key"].Value?.ToString() == tagKey);
            if (rowToRemove != null)
            {
                _gvwTags.Rows.Remove(rowToRemove);
            }
        }
        _tagsToRemove.Clear();
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
        return resTags ?? new Dictionary<string, string>();
    }

    public async Task<Dictionary<string, string>> ResolveTagVariables(Dictionary<string, string> tags)
    {
        var resolvedTags = new Dictionary<string, string>();
        var userEmail = await GetUserEmail();
        foreach (var tag in tags)
        {
            resolvedTags.Add(tag.Key, tag.Value
                .Replace("{Date}", DateTime.UtcNow.ToString("yyyy-MM-dd"))
                .Replace("{Time}", DateTime.UtcNow.ToString("HH:mm:ss"))
                .Replace("{DateTime}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"))
                .Replace("{User}", userEmail));
        }
        return resolvedTags;
    }

    private async Task<string> GetUserEmail()
    {
        if (_azureService == null)
        {
            return Environment.UserName;
        }
        var credential = _azureService.CurrentCredential;
        if (credential == null)
        {
            return Environment.UserName;
        }
        var scopes = new[] { "User.Read" };
        var graphClient = new GraphServiceClient(_azureService.CurrentCredential, scopes);
        var user = await graphClient.Me.GetAsync();
        return user.Mail ?? user.UserPrincipalName ?? Environment.UserName;
    }
}
