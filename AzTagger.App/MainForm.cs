#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Graph;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using AzTagger.Models;
using AzTagger.Services;

namespace AzTagger.App;

public class MainForm : Form
{
    private readonly Settings _settings;
    private AzureService _azureService;

    private readonly TextArea _txtSearchQuery;
    private readonly GridView _gvwResults;
    private readonly GridView _gvwTags;
    private readonly Splitter _splitter;
    private readonly Button _btnSearch;
    private readonly Button _btnClearQuery;
    private readonly Button _btnCopyQuery;
    private readonly Button _btnSaveQuery;
    private readonly Button _btnApplyTags;
    private readonly DropDown _cboTagTemplates;
    private readonly Button _btnEditTemplates;

    private readonly DropDown _cboRecentSearches;
    private readonly DropDown _cboSavedQueries;

    private readonly DropDown _cboQuickFilter1Column;
    private readonly TextBox _txtQuickFilter1Text;
    private readonly DropDown _cboQuickFilter2Column;
    private readonly TextBox _txtQuickFilter2Text;
    private readonly Label _lblResultsCount;

    private System.Threading.Timer? _quickFilter1Timer;
    private System.Threading.Timer? _quickFilter2Timer;
    private System.Threading.Timer? _resizeTimer;
    private System.Threading.Timer? _splitterTimer;

    private readonly Button _btnFirstPage;
    private readonly Button _btnPreviousPage;
    private readonly Button _btnNextPage;
    private readonly Button _btnLastPage;
    private readonly Label _lblPageInfo;
    private readonly ComboBox _cboPageSize;

    private readonly LinkButton _lnkRegExDocs;
    private readonly LinkButton _lnkResourceGraphDocs;
    private readonly LinkButton _lnkGitHub;
    private readonly LinkButton _lnkDonation;
    private readonly LinkButton _lnkEditSettings;
    private readonly LinkButton _lnkShowErrorLog;
    private readonly LinkButton _lnkResetDefaults;
    private readonly Label _lblVersion;

    private readonly ContextMenu _resultsContextMenu;
    private readonly ContextMenu _quickFilterContextMenu;
    private readonly ContextMenu _tagsContextMenu;

    private string _sortColumn = string.Empty;
    private bool _sortAscending = true;

    private int _resultsContextRow = -1;
    private GridColumn? _resultsContextColumn;
    private int _tagsContextRow = -1;
    private GridColumn? _tagsContextColumn;
    private readonly Dictionary<GridColumn, string> _columnPropertyMap = new();

    private List<TagTemplate> _tagTemplates = new();

    private readonly ObservableCollection<TagEntry> _tags = new();
    private readonly PaginatedResourceCollection _paginatedResults = new();
    private List<Resource> _allResults = new();

    private int _fixedTagsPanelHeight = 200;
    
    private int MinResultsPanelHeight 
    { 
        get 
        {
            var availableHeight = GetActualSplitterHeight();
            return Math.Max(GetDpiScaledWidth(60), (int)(availableHeight * 0.2));
        }
    }
    
    private int MinTagsPanelHeight 
    { 
        get 
        {
            var availableHeight = GetActualSplitterHeight(); 
            return Math.Max(GetDpiScaledWidth(60), (int)(availableHeight * 0.2));
        }
    }
    
    private bool _isProgrammaticSplitterUpdate = false;

    private const string BaseQuery = """
resources
| join kind=leftouter (
    resourcecontainers
    | where type =~ "microsoft.resources/subscriptions/resourcegroups"
    | project rg_subscriptionId = subscriptionId, resourceGroup = name, resourceGroupTags = tags
) on $left.subscriptionId == $right.rg_subscriptionId and $left.resourceGroup == $right.resourceGroup
| join kind=inner (
    resourcecontainers
    | where type =~ "microsoft.resources/subscriptions" and not(properties['state'] =~ 'disabled')
    | project sub_subscriptionId = subscriptionId, subscriptionTags = tags, subscriptionName = name
) on $left.subscriptionId == $right.sub_subscriptionId
| project
    EntityType = "Resource",
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
    | where type =~ "microsoft.resources/subscriptions/resourcegroups"
    | join kind=inner (
        resourcecontainers
        | where type =~ "microsoft.resources/subscriptions" and not(properties['state'] =~ 'disabled')
        | project subscriptionId, subscriptionTags = tags, subscriptionName = name
    ) on $left.subscriptionId == $right.subscriptionId
    | project
        EntityType = "ResourceGroup",
        Id = id,
        SubscriptionName = subscriptionName,
        SubscriptionId = subscriptionId,
        ResourceGroup = name,
        ResourceName = "",
        ResourceType = type,
        SubscriptionTags = subscriptionTags,
        ResourceGroupTags = tags,
        ResourceTags = ""
)
| union (
    resourcecontainers
    | where type =~ "microsoft.resources/subscriptions" and not(properties['state'] =~ 'disabled')
    | project
        EntityType = "Subscription",
        Id = id,
        SubscriptionName = name,
        SubscriptionId = subscriptionId,
        ResourceGroup = "",
        ResourceName = "",
        ResourceType = type,
        SubscriptionTags = tags,
        ResourceGroupTags = "",
        ResourceTags = ""
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
| extend CombinedTags = bag_merge(ResourceTags, ResourceGroupTags, SubscriptionTags)
| order by EntityType desc, (tolower(SubscriptionName)) asc, (tolower(ResourceGroup)) asc, (tolower(ResourceName)) asc
""";

    public MainForm()
    {
        _settings = SettingsService.Load();

        Title = "AzTagger";
        
        MinimumSize = new Size(1024, 768);
        
        if (_settings.WindowSize.Width > 0 && _settings.WindowSize.Height > 0)
        {
            var width = Math.Max(_settings.WindowSize.Width, 1024);
            var height = Math.Max(_settings.WindowSize.Height, 768);
            ClientSize = new Size(width, height);
        }
        else
            ClientSize = new Size(1024, 768);

        if (_settings.WindowLocation.X > 0 || _settings.WindowLocation.Y > 0)
            Location = new Point(_settings.WindowLocation.X, _settings.WindowLocation.Y);
        _azureService = new AzureService(_settings);

        _tagTemplates = TagTemplatesService.Load();

        CreateMenuBar();
        SetupKeyboardShortcuts();

        _txtSearchQuery = new TextArea 
        { 
            Height = 80,
            SpellCheck = false,
            TextReplacements = TextReplacements.None
        };
        
        _txtSearchQuery.GotFocus += (_, _) => 
        {
            _txtSearchQuery.TextReplacements = TextReplacements.None;
            _txtSearchQuery.SpellCheck = false;
        };
        _txtSearchQuery.TextChanged += new EventHandler<EventArgs>((_, _) =>
        {
            var normalizedQuery = _txtSearchQuery.Text.ToLower().Replace(" ", "").Replace("\r\n", "").Replace("\n", "").Trim();
            if (normalizedQuery.StartsWith("resources|") || normalizedQuery.StartsWith("resourcecontainers|"))
            {
                //_lblQueryMode.Text = "(KQL full expression) --> not supported";
                _queryMode = QueryMode.KqlFull;
            }
            else if (normalizedQuery.StartsWith("|"))
            {
                //_lblQueryMode.Text = "(KQL filter-only expression)";
                _queryMode = QueryMode.KqlFilter;
            }
            else if (normalizedQuery.Length > 0)
            {
                //_lblQueryMode.Text = "(regular expression, applied to SubscriptionName, ResourceGroup and ResourceName)";
                _queryMode = QueryMode.Regex;
            }
            else
            {
                //_lblQueryMode.Text = string.Empty;
                _queryMode = QueryMode.Regex;
            }
        });

        _btnSearch = new Button { Text = "Search" };
        _btnSearch.Click += async (_, _) => await SearchAsync();

        _btnClearQuery = new Button { Text = "Clear" };
        _btnClearQuery.Click += (_, _) => _txtSearchQuery.Text = string.Empty;

        _btnCopyQuery = new Button { Text = "Copy Query" };
        _btnCopyQuery.Click += (_, _) => Clipboard.Instance.Text = BuildQuery();

        _btnSaveQuery = new Button { Text = "Save Query" };
        _btnSaveQuery.Click += (_, _) => SaveQuery();

        _cboRecentSearches = new DropDown();
        _cboRecentSearches.SelectedIndexChanged += (_, _) => OnRecentSearchSelected();

        _cboSavedQueries = new DropDown();
        _cboSavedQueries.SelectedIndexChanged += (_, _) => OnSavedQuerySelected();

        var configureButton = new Button { Text = "Configure Azure Context" };
        configureButton.Click += (_, _) =>
        {
            var dlg = new AzureContextConfigDialog(_settings, _azureService);
            if (dlg.ShowModal(this))
            {
                SettingsService.Save(_settings);
            }
        };

        _gvwResults = new GridView { DataStore = _paginatedResults.DisplayedItems, AllowMultipleSelection = true };
        
        var resourceProps = typeof(Resource).GetProperties()
            .Where(p => p.Name != nameof(Resource.CombinedTagsFormatted));
        foreach (var prop in resourceProps)
        {
            var defaultWidth = prop.Name switch
            {
                "EntityType" => GetDpiScaledWidth(100),
                "SubscriptionName" => GetDpiScaledWidth(150),
                "ResourceGroup" => GetDpiScaledWidth(150),
                "ResourceName" => GetDpiScaledWidth(200),
                "ResourceType" => GetDpiScaledWidth(200),
                "Id" => GetDpiScaledWidth(300),
                "SubscriptionId" => GetDpiScaledWidth(280),
                "SubscriptionTags" => GetDpiScaledWidth(200),
                "ResourceGroupTags" => GetDpiScaledWidth(200),
                "ResourceTags" => GetDpiScaledWidth(200),
                _ => GetDpiScaledWidth(150)
            };
            
            var cell = new TextBoxCell { Binding = Binding.Delegate<Resource, string>(r => FormatPropertyForGrid(r, prop.Name)) };
            
            GridColumn col = new GridColumn
            {
                HeaderText = prop.Name,
                DataCell = cell,
                CellToolTipBinding = Binding.Delegate<Resource, string>(r => FormatPropertyForTooltip(r, prop.Name)),
                Sortable = true,
                Width = defaultWidth
            };
            _columnPropertyMap[col] = prop.Name;
            _gvwResults.Columns.Add(col);
        }

        _gvwResults.SelectionChanged += (_, _) => LoadTagsForSelection();
        _gvwResults.CellDoubleClick += (_, _) => OpenSelectedResourceInPortal();
        _gvwResults.MouseDown += (s, e) =>
        {
            if (e.Buttons == MouseButtons.Alternate)
            {
                var cell = _gvwResults.GetCellAt(e.Location);
                if (cell != null && cell.RowIndex >= 0 && cell.Column != null)
                {
                    // Add bounds checking
                    if (cell.RowIndex >= (_paginatedResults?.DisplayedItems?.Count ?? 0))
                    {
                        return;
                    }
                    
                    _resultsContextRow = cell.RowIndex;
                    _resultsContextColumn = cell.Column;
                    _gvwResults.UnselectAll();
                    _gvwResults.SelectRow(cell.RowIndex);
                }
            }
        };
        _gvwResults.ColumnHeaderClick += (_, e) => SortResults(e.Column);

        _gvwTags = new GridView { DataStore = _tags, AllowMultipleSelection = false };
        var keyCol = new GridColumn
        {
            HeaderText = "Key",
            Editable = true,
            DataCell = new TextBoxCell { Binding = Binding.Property<TagEntry, string>(t => t.Key) },
            CellToolTipBinding = Binding.Property<TagEntry, string>(t => t.Key),
            Width = GetDpiScaledWidth(150)
        };
        var valueCol = new GridColumn
        {
            HeaderText = "Value",
            Editable = true,
            DataCell = new TextBoxCell { Binding = Binding.Property<TagEntry, string>(t => t.Value) },
            CellToolTipBinding = Binding.Property<TagEntry, string>(t => t.Value),
            Width = GetDpiScaledWidth(250)
        };
        _gvwTags.Columns.Add(keyCol);
        _gvwTags.Columns.Add(valueCol);

        var tagAddFilterItem = new ButtonMenuItem { Text = "Add to filter query" };
        tagAddFilterItem.Click += (_, _) => AddTagToFilterQuery(false);
        var tagExcludeFilterItem = new ButtonMenuItem { Text = "Exclude in filter query" };
        tagExcludeFilterItem.Click += (_, _) => AddTagToFilterQuery(true);
        var tagDeleteItem = new ButtonMenuItem { Text = "Delete tag" };
        tagDeleteItem.Click += (_, _) =>
        {
            if (_tagsContextRow >= 0 && _tagsContextRow < _tags.Count)
                _tags.RemoveAt(_tagsContextRow);
        };
        var tagCopyValueItem = new ButtonMenuItem { Text = "Copy cell value" };
        tagCopyValueItem.Click += (_, _) => CopyTagContextCellValue();
        _tagsContextMenu = new ContextMenu { Items = { tagAddFilterItem, tagExcludeFilterItem, tagDeleteItem, tagCopyValueItem } };
        _gvwTags.ContextMenu = _tagsContextMenu;
        _gvwTags.KeyDown += (s, e) =>
        {
            if (e.Key == Keys.Delete && _gvwTags.SelectedItem is TagEntry tag)
            {
                _tags.Remove(tag);
                e.Handled = true;
            }
        };
        _gvwTags.MouseDown += (s, e) =>
        {
            if (e.Buttons == MouseButtons.Alternate)
            {
                var cell = _gvwTags.GetCellAt(e.Location);
                if (cell != null && cell.RowIndex >= 0)
                {
                    _tagsContextRow = cell.RowIndex;
                    _tagsContextColumn = _gvwTags.Columns[cell.ColumnIndex];
                    _gvwTags.UnselectAll();
                    _gvwTags.SelectRow(cell.RowIndex);
                }
            }
        };

        _cboTagTemplates = new DropDown { Width = GetDpiScaledWidth(200) }; // 2/3 of previous width (300 -> 200) with DPI scaling
        _cboTagTemplates.SelectedIndexChanged += async (_, _) => await OnTagTemplateSelectedAsync();

        _btnEditTemplates = new Button { Text = "Edit Templates" };
        _btnEditTemplates.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = TagTemplatesService.TagTemplatesFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Failed to open tag templates file");
                MessageBox.Show(this, "Failed to open tag templates file", MessageBoxButtons.OK, MessageBoxType.Error);
            }
            ReloadTagTemplates();
        };

        _btnApplyTags = new Button { Text = "Apply Tags" };
        _btnApplyTags.Click += async (_, _) => await ApplyTagsAsync();

        _cboQuickFilter1Column = new DropDown { Width = GetDpiScaledWidth(80) };
        _cboQuickFilter2Column = new DropDown { Width = GetDpiScaledWidth(80) };
        var propertyNames = typeof(Resource).GetProperties()
            .Where(p => p.Name != nameof(Resource.CombinedTagsFormatted))
            .Select(p => p.Name).ToList();
        _cboQuickFilter1Column.DataStore = new List<string>(new[] { string.Empty, "All" }.Concat(propertyNames));
        _cboQuickFilter2Column.DataStore = new List<string>(new[] { string.Empty, "All" }.Concat(propertyNames));
        _cboQuickFilter1Column.SelectedIndexChanged += (_, _) => FilterResults();
        _cboQuickFilter2Column.SelectedIndexChanged += (_, _) => FilterResults();

        _txtQuickFilter1Text = new TextBox { Width = GetDpiScaledWidth(180) };
        _txtQuickFilter2Text = new TextBox { Width = GetDpiScaledWidth(180) };
        _txtQuickFilter1Text.TextChanged += (_, _) => ScheduleDelayedFilter(1);
        _txtQuickFilter2Text.TextChanged += (_, _) => ScheduleDelayedFilter(2);

        _btnFirstPage = new Button { Text = "⏮", ToolTip = "First page" };
        _btnPreviousPage = new Button { Text = "◀", ToolTip = "Previous page" };
        _btnNextPage = new Button { Text = "▶", ToolTip = "Next page" };
        _btnLastPage = new Button { Text = "⏭", ToolTip = "Last page" };
        _lblPageInfo = new Label { Text = "Page 0 of 0" };
        _cboPageSize = new ComboBox { 
            DataStore = new[] { "100", "500", "1000", "2000", "5000" }, 
            SelectedIndex = 2,
            ToolTip = "Number of items to display per page"
        };

        _btnFirstPage.Click += (_, _) => { _paginatedResults.GoToPage(0); UpdatePaginationControls(); };
        _btnPreviousPage.Click += (_, _) => { _paginatedResults.PreviousPage(); UpdatePaginationControls(); };
        _btnNextPage.Click += (_, _) => { _paginatedResults.NextPage(); UpdatePaginationControls(); };
        _btnLastPage.Click += (_, _) => { _paginatedResults.GoToPage(_paginatedResults.TotalPages - 1); UpdatePaginationControls(); };
        _cboPageSize.SelectedIndexChanged += (_, _) => 
        {
            if (int.TryParse(_cboPageSize.SelectedValue?.ToString(), out int pageSize))
            {
                _paginatedResults.SetPageSize(pageSize);
                UpdatePaginationControls();
            }
        };

        _paginatedResults.FilterChanged += (_, _) => UpdatePaginationControls();

        _quickFilterContextMenu = CreateQuickFilterContextMenu(_txtQuickFilter1Text);
        _txtQuickFilter1Text.MouseUp += (s, e) => { if (e.Buttons == MouseButtons.Alternate) _quickFilterContextMenu.Show(_txtQuickFilter1Text); };
        var menu2 = CreateQuickFilterContextMenu(_txtQuickFilter2Text);
        _txtQuickFilter2Text.MouseUp += (s, e) => { if (e.Buttons == MouseButtons.Alternate) menu2.Show(_txtQuickFilter2Text); };

        _lblResultsCount = new Label();

        _lnkRegExDocs = new LinkButton { Text = ".NET RegEx Docs" };
        _lnkRegExDocs.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference",
            UseShellExecute = true
        });

        _lnkResourceGraphDocs = new LinkButton { Text = "Resource Graph Docs" };
        _lnkResourceGraphDocs.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://learn.microsoft.com/en-us/azure/governance/resource-graph/concepts/query-language",
            UseShellExecute = true
        });

        _lnkGitHub = new LinkButton { Text = "GitHub" };
        _lnkGitHub.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/thgossler/AzTagger",
            UseShellExecute = true
        });

        _lnkDonation = new LinkButton { Text = "Donate" };
        _lnkDonation.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.paypal.com/donate/?hosted_button_id=JVG7PFJ8DMW7J",
            UseShellExecute = true
        });

        _lnkEditSettings = new LinkButton { Text = "Edit Settings File" };
        _lnkEditSettings.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = SettingsService.SettingsFilePath,
            UseShellExecute = true
        });

        _lnkShowErrorLog = new LinkButton { Text = "Show Error Log" };
        _lnkShowErrorLog.Click += (_, _) =>
        {
            var file = LoggingService.GetLatestLogFile();
            if (!string.IsNullOrEmpty(file))
                Process.Start(new ProcessStartInfo { FileName = file, UseShellExecute = true });
        };

        _lnkResetDefaults = new LinkButton { Text = "Reset UI to Defaults" };
        _lnkResetDefaults.Click += (_, _) =>
        {
            _settings.ResetToWindowDefaults();
            ClientSize = new Size(_settings.WindowSize.Width, _settings.WindowSize.Height);
            Location = new Point(_settings.WindowLocation.X, _settings.WindowLocation.Y);
        };

        var version = typeof(MainForm).Assembly.GetName().Version?.ToString() ?? "";
        if (version.Contains('.'))
            version = version[..version.LastIndexOf('.')];
        _lblVersion = new Label { Text = $"Version: {version}" };

        var openPortalItem = new ButtonMenuItem { Text = "Open in Azure Portal" };
        openPortalItem.Click += (_, _) => OpenSelectedResourceInPortal();

        var openUrlsItem = new ButtonMenuItem { Text = "Open URLs in tags" };
        openUrlsItem.Click += (_, _) => OpenUrlsInSelectedTags();

        var copyEmailsItem = new ButtonMenuItem { Text = "Copy email addresses" };
        copyEmailsItem.Click += (_, _) => CopyEmailAddressesInSelectedTags();

        var copyTagsItem = new ButtonMenuItem { Text = "Copy tags as JSON" };
        copyTagsItem.Click += (_, _) => CopySelectedTagsAsJson();

        var addFilterItem = new ButtonMenuItem { Text = "Add to filter query" };
        addFilterItem.Click += (_, _) => AddToFilterQuery(false);

        var excludeFilterItem = new ButtonMenuItem { Text = "Exclude in filter query" };
        excludeFilterItem.Click += (_, _) => AddToFilterQuery(true);

        var refreshTagsItem = new ButtonMenuItem { Text = "Refresh tags from Azure" };
        refreshTagsItem.Click += async (_, _) => await RefreshTagsAsync();

        var copyCellValueItem = new ButtonMenuItem { Text = "Copy cell value" };
        copyCellValueItem.Click += (_, _) => CopyContextCellValue();

        _resultsContextMenu = new ContextMenu
        {
            Items =
            {
                openPortalItem,
                openUrlsItem,
                copyEmailsItem,
                copyTagsItem,
                copyCellValueItem,
                addFilterItem,
                excludeFilterItem,
                refreshTagsItem
            }
        };
        _gvwResults.ContextMenu = _resultsContextMenu;

        var topRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items =
            {
                _btnSearch,
                new Panel { Width = GetDpiScaledWidth(10) },
                _btnSaveQuery,
                _btnCopyQuery,
                _btnClearQuery,
                new Panel { Width = GetDpiScaledWidth(10) },
                configureButton,
                null
            }
        };

        var layout = new StackLayout 
        { 
            Orientation = Orientation.Vertical,
            Spacing = 5, 
            Padding = 10,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        
        var recentSavedRow = new TableLayout
        {
            Spacing = new Size(5, 0)
        };
        recentSavedRow.Rows.Add(new TableRow(
            new TableCell(_cboRecentSearches, true),  // scaleWidth = true for both
            new TableCell(new Panel { Width = GetDpiScaledWidth(3) }, false), // 2px separator
            new TableCell(_cboSavedQueries, false)
        ));
        _cboRecentSearches.Width = -1;
        _cboSavedQueries.Width = GetDpiScaledWidth(150);
        layout.Items.Add(new StackLayoutItem(recentSavedRow, HorizontalAlignment.Stretch));
        
        layout.Items.Add(new Panel { Padding = new Padding(0, 5, 0, 0), Content = new Label { Text = "Search Query:" } });
        layout.Items.Add(new StackLayoutItem(_txtSearchQuery, HorizontalAlignment.Stretch) { Expand = false });
        layout.Items.Add(new StackLayoutItem(topRow, HorizontalAlignment.Stretch));
        layout.Items.Add(new Panel { Padding = new Padding(0, 5, 0, 0), Content = new Label { Text = "Results:" } });
        
        var quickFilterRow = new TableLayout();
        var cboQuickFilter2WithMargin = new Panel { Padding = new Padding(GetDpiScaledWidth(4), 0, 0, 0), Content = _cboQuickFilter2Column };
        quickFilterRow.Rows.Add(new TableRow(new TableCell(_cboQuickFilter1Column, false), new TableCell(_txtQuickFilter1Text, true), new TableCell(cboQuickFilter2WithMargin, false), new TableCell(_txtQuickFilter2Text, true), new TableCell(null, true)));
        layout.Items.Add(new StackLayoutItem(quickFilterRow, HorizontalAlignment.Stretch));
        
        var resultsPanel = new TableLayout { Spacing = new Size(5, 5) };
        resultsPanel.Rows.Add(new TableRow(new TableCell(_gvwResults, true)) { ScaleHeight = true });
        
        var paginationRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items =
            {
                _lblResultsCount,
                new StackLayoutItem(null, true),
                new Label { Text = "Page size:" },
                _cboPageSize,
                _btnFirstPage,
                _btnPreviousPage,
                _lblPageInfo,
                _btnNextPage,
                _btnLastPage
            }
        };
        resultsPanel.Rows.Add(new TableRow(new TableCell(paginationRow, true)));
        resultsPanel.Rows.Add(new TableRow(new Panel { Height = 5 }));

        var tagsPanel = new TableLayout { Spacing = new Size(5, 5) };
        tagsPanel.Rows.Add(new TableRow(new Panel { Height = 5 }));
        tagsPanel.Rows.Add(new TableRow(new TableCell(_gvwTags, true)) { ScaleHeight = true });
        
        var tagTemplatesRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items = 
            { 
                _btnApplyTags,
                new StackLayoutItem(null, true),
                _cboTagTemplates, 
                _btnEditTemplates 
            }
        };
        tagsPanel.Rows.Add(new TableRow(new TableCell(tagTemplatesRow, true)));

        _splitter = new Splitter
        {
            Orientation = Orientation.Vertical,
            FixedPanel = SplitterFixedPanel.None,
            Panel1 = resultsPanel,
            Panel2 = tagsPanel,
            Position = Math.Max(100, _settings.SplitterPosition),
            SplitterWidth = GetDpiScaledWidth(8)
        };
        
        _splitter.PositionChanged += (_, _) =>
        {
            if (_isProgrammaticSplitterUpdate) return;
            
            var availableHeight = GetAvailableHeightForSplitter();
            if (availableHeight > 0)
            {
                var tagsPanelHeight = availableHeight - _splitter.Position;
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, tagsPanelHeight);
            }
            
            ScheduleDelayedSplitterConstraint();
        };
        
        layout.Items.Add(new StackLayoutItem(_splitter, HorizontalAlignment.Stretch, true));

        layout.Items.Add(new Panel { Height = GetDpiScaledWidth(2) });

        var linksRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Items =
            {
                _lnkRegExDocs,
                _lnkResourceGraphDocs,
                _lnkGitHub,
                _lnkDonation,
                _lnkEditSettings,
                _lnkShowErrorLog,
                _lnkResetDefaults,
                new StackLayoutItem(null, true),
                _lblVersion
            }
        };
        layout.Items.Add(new StackLayoutItem(linksRow, HorizontalAlignment.Stretch));

        LoadRecentSearches();
        LoadSavedSearches();
        ReloadTagTemplates();

        _txtSearchQuery.Text = _settings.LastSearchQuery;
        _txtQuickFilter1Text.Text = _settings.LastQuickFilter1Text;
        _txtQuickFilter2Text.Text = _settings.LastQuickFilter2Text;

        Content = layout;

        Closing += (_, _) => 
        {
            SaveSettings();
            LoggingService.CloseAndFlush();
        };
        Closed += (_, _) => 
        {
            LoggingService.CloseAndFlush();
            Application.Instance.Quit();
        };

        Shown += (_, _) => 
        {
            _txtSearchQuery.TextReplacements = TextReplacements.None;
            _txtSearchQuery.SpellCheck = false;
            
            CalculateInitialTagsPanelHeight();
            UpdateSplitterPosition();
            
            _splitter.Invalidate();
            
            Application.Instance.AsyncInvoke(() => 
            {
                ResizeResultsGridColumns();
                ResizeTagsGridColumns();
            });
        };
        SizeChanged += (_, _) => 
        {
            ScheduleDelayedResize();
        };
    }

    private void CreateMenuBar()
    {
        var closeCommand = new Command
        {
            MenuText = "&Close",
            ToolBarText = "Close",
            Shortcut = Application.Instance.CommonModifier | Keys.W 
        };
        closeCommand.Executed += (_, _) => ExitApplication();

        var exitCommand = new Command
        {
            MenuText = "E&xit",
            ToolBarText = "Exit",
            Shortcut = Application.Instance.CommonModifier | Keys.Q 
        };
        exitCommand.Executed += (_, _) => ExitApplication();

        var altF4ExitCommand = new Command
        {
            Shortcut = Keys.Alt | Keys.F4
        };
        altF4ExitCommand.Executed += (_, _) => ExitApplication();

        var aboutItem = new ButtonMenuItem { Text = "&About AzTagger..." };
        aboutItem.Click += (_, _) => ShowAboutDialog();

        var fileMenu = new SubMenuItem
        {
            Text = "&File"
        };

        fileMenu.Items.Add(exitCommand);

        var menuBar = new MenuBar
        {
            Items = { fileMenu }
        };

        menuBar.QuitItem = exitCommand;
        menuBar.AboutItem = aboutItem;

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            menuBar.ApplicationItems.Add(altF4ExitCommand);
        }

        Menu = menuBar;
    }

    private void SetupKeyboardShortcuts()
    {
        KeyDown += (_, e) =>
        {
            if (e.Key == Keys.Escape)
            {
                ExitApplication();
                e.Handled = true;
            }
            else if (e.Key == Keys.PageUp && e.Modifiers == Keys.None)
            {
                _paginatedResults.PreviousPage();
                UpdatePaginationControls();
                e.Handled = true;
            }
            else if (e.Key == Keys.PageDown && e.Modifiers == Keys.None)
            {
                _paginatedResults.NextPage();
                UpdatePaginationControls();
                e.Handled = true;
            }
            else if (e.Key == Keys.Home && e.Modifiers == Keys.Control)
            {
                _paginatedResults.GoToPage(0);
                UpdatePaginationControls();
                e.Handled = true;
            }
            else if (e.Key == Keys.End && e.Modifiers == Keys.Control)
            {
                _paginatedResults.GoToPage(_paginatedResults.TotalPages - 1);
                UpdatePaginationControls();
                e.Handled = true;
            }
        };
    }

    private void ExitApplication()
    {
        SaveSettings();
        Application.Instance.Quit();
    }

    private int GetDpiScaledWidth(int baseWidth)
    {
        var scale = Screen.LogicalPixelSize;
        return (int)(baseWidth * scale);
    }

    private void ResizeResultsGridColumns()
    {
        if (_gvwResults.Columns.Count == 0)
            return;
        
        int tolerance = GetDpiScaledWidth(20);
        int actualGridWidth = _gvwResults.Width - tolerance;
        
        if (actualGridWidth <= GetDpiScaledWidth(100))
            return;
        
        int availableWidth = actualGridWidth;
        
        int colCount = _gvwResults.Columns.Count;
        
        var columnWidths = new Dictionary<string, int>
        {
            ["EntityType"] = GetDpiScaledWidth(100),
            ["SubscriptionName"] = GetDpiScaledWidth(150),
            ["ResourceGroup"] = GetDpiScaledWidth(150),
            ["ResourceName"] = GetDpiScaledWidth(200),
            ["ResourceType"] = GetDpiScaledWidth(200),
            ["Id"] = GetDpiScaledWidth(300),
            ["SubscriptionId"] = GetDpiScaledWidth(280),
            ["SubscriptionTags"] = GetDpiScaledWidth(200),
            ["ResourceGroupTags"] = GetDpiScaledWidth(200),
            ["ResourceTags"] = GetDpiScaledWidth(200)
        };
        
        int totalPreferredWidth = 0;
        for (int i = 0; i < colCount; i++)
        {
            var column = _gvwResults.Columns[i];
            if (_columnPropertyMap.TryGetValue(column, out var propertyName) && 
                columnWidths.TryGetValue(propertyName, out var preferredWidth))
            {
                totalPreferredWidth += preferredWidth;
            }
            else
            {
                totalPreferredWidth += GetDpiScaledWidth(150);
            }
        }
        
        double scaleFactor = (double)availableWidth / totalPreferredWidth;
        
        for (int i = 0; i < colCount; i++)
        {
            var column = _gvwResults.Columns[i];
            int preferredWidth = GetDpiScaledWidth(150);
            
            if (_columnPropertyMap.TryGetValue(column, out var propertyName) && 
                columnWidths.TryGetValue(propertyName, out var configuredWidth))
            {
                preferredWidth = configuredWidth;
            }
            
            int scaledWidth = (int)(preferredWidth * scaleFactor);
            int finalWidth = Math.Max(GetDpiScaledWidth(40), scaledWidth);
            column.Width = finalWidth;
        }
        
        int totalMinimumWidth = colCount * GetDpiScaledWidth(40);
        if (totalMinimumWidth > availableWidth && availableWidth > GetDpiScaledWidth(100))
        {
            int evenDistributedWidth = availableWidth / colCount;
            for (int i = 0; i < colCount; i++)
            {
                _gvwResults.Columns[i].Width = Math.Max(GetDpiScaledWidth(25), evenDistributedWidth);
            }
        }
    }

    private void ResizeTagsGridColumns()
    {
        if (_gvwTags.Columns.Count == 0)
            return;
        
        int tolerance = GetDpiScaledWidth(20);
        int actualGridWidth = _gvwTags.Width - tolerance;
        
        if (actualGridWidth <= GetDpiScaledWidth(80))
            return;
        
        int availableWidth = actualGridWidth;
        
        int keyColWidth = availableWidth / 3;
        int valueColWidth = availableWidth - keyColWidth;
        
        int minKeyWidth = GetDpiScaledWidth(50);
        int minValueWidth = GetDpiScaledWidth(80);
        
        keyColWidth = Math.Max(keyColWidth, minKeyWidth);
        valueColWidth = Math.Max(valueColWidth, minValueWidth);
        
        if (minKeyWidth + minValueWidth > availableWidth && availableWidth > GetDpiScaledWidth(60))
        {
            keyColWidth = availableWidth / 3;
            valueColWidth = availableWidth - keyColWidth;
            keyColWidth = Math.Max(GetDpiScaledWidth(20), keyColWidth);
            valueColWidth = Math.Max(GetDpiScaledWidth(30), valueColWidth);
        }
        
        if (_gvwTags.Columns.Count >= 2)
        {
            _gvwTags.Columns[0].Width = keyColWidth;
            _gvwTags.Columns[1].Width = valueColWidth;
        }
    }

    private void ScheduleDelayedResize()
    {
        _resizeTimer?.Dispose();
        
        _resizeTimer = new System.Threading.Timer(
            _ => Application.Instance.Invoke(() => 
            {
                ResizeResultsGridColumns();
                ResizeTagsGridColumns();
                UpdateSplitterPosition();
            }),
            null,
            TimeSpan.FromMilliseconds(300),
            TimeSpan.FromMilliseconds(-1)
        );
    }

    private async Task EnsureSignedInAsync()
    {
        if (_azureService == null)
            _azureService = new AzureService(_settings);
        await _azureService.SignInAsync();
    }

    enum QueryMode
    {
        KqlFilter,
        Regex,
        KqlFull
    }
    private QueryMode _queryMode = QueryMode.Regex;

    private string BuildQuery()
    {
        var resultQuery = BaseQuery;

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

    private async Task SearchAsync()
    {
        if (_queryMode == QueryMode.KqlFull)
        {
            MessageBox.Show(this, "KQL full expressions are not supported! Please use KQL filter-only expressions or a regular expression.",
                "Info", MessageBoxButtons.OK, MessageBoxType.Information);
            return;
        }

        _btnSearch.Enabled = false;
        try
        {
            await EnsureSignedInAsync();
            var query = BuildQuery();
            var items = (await _azureService.QueryResourcesAsync(query)).ToList();
            _allResults = items;
            _paginatedResults.SetAllItems(_allResults);
            UpdatePaginationControls();
            SaveRecentSearch(_txtSearchQuery.Text);
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Search failed");
            
            if (ex.Message.Contains("TenantId is not set in the settings") || 
                ex.Message.Contains("TenantId") && ex.Message.Contains("settings"))
            {
                var result = MessageBox.Show(this, 
                    "Azure context is not configured. Would you like to configure it now?",
                    "Configuration Required", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxType.Question);
                
                if (result == DialogResult.Yes)
                {
                    var dlg = new AzureContextConfigDialog(_settings, _azureService);
                    if (dlg.ShowModal(this))
                    {
                        SettingsService.Save(_settings);
                        _btnSearch.Enabled = true;
                        await SearchAsync();
                        return;
                    }
                }
            }
            else
            {
                MessageBox.Show(this, "Search failed", MessageBoxButtons.OK, MessageBoxType.Error);
            }
        }
        finally
        {
            _btnSearch.Enabled = true;
        }
    }

    private void LoadTagsForSelection()
    {
        _tags.Clear();
        
        var selectedResources = _gvwResults.SelectedItems.Cast<Resource>().ToList();
        
        if (selectedResources.Count == 0)
        {
            return;
        }

        if (selectedResources.Count == 1)
        {
            // Single selection - show all tags
            var res = selectedResources[0];
            
            if (res.CombinedTags != null)
            {
                foreach (var kvp in res.CombinedTags.OrderBy(k => k.Key))
                {
                    _tags.Add(new TagEntry { Key = kvp.Key, Value = kvp.Value });
                }
            }
        }
        else
        {
            // Multiple selection - show only common tags with same values
            var commonTags = new Dictionary<string, string>();
            
            // Start with tags from the first resource
            var firstResource = selectedResources[0];
            
            if (firstResource.CombinedTags != null)
            {
                foreach (var kvp in firstResource.CombinedTags)
                {
                    commonTags[kvp.Key] = kvp.Value;
                }
            }
            
            // Remove tags that don't exist in all other resources or have different values
            for (int i = 1; i < selectedResources.Count; i++)
            {
                var resource = selectedResources[i];
                
                if (resource.CombinedTags == null)
                {
                    commonTags.Clear();
                    break;
                }
                
                var tagsToRemove = new List<string>();
                foreach (var commonTag in commonTags)
                {
                    if (!resource.CombinedTags.TryGetValue(commonTag.Key, out var value))
                    {
                        tagsToRemove.Add(commonTag.Key);
                    }
                    else if (value != commonTag.Value)
                    {
                        tagsToRemove.Add(commonTag.Key);
                    }
                }
                
                foreach (var tagKey in tagsToRemove)
                {
                    commonTags.Remove(tagKey);
                }
            }
            
            // Add common tags to the display
            foreach (var kvp in commonTags.OrderBy(k => k.Key))
            {
                _tags.Add(new TagEntry { Key = kvp.Key, Value = kvp.Value });
            }
        }
    }

    private async Task ApplyTagsAsync()
    {
        if (!_gvwResults.SelectedItems.Cast<object>().Any())
            return;

        var selected = _gvwResults.SelectedItems.Cast<Resource>().ToList();
        var tagsToUpdate = _tags
            .Where(t => !string.IsNullOrWhiteSpace(t.Key))
            .ToDictionary(t => t.Key, t => t.Value);

        try
        {
            var errors = await _azureService.UpdateTagsAsync(selected, tagsToUpdate, null);
            if (errors.Length > 0)
            {
                MessageBox.Show(this, string.Join("\n", errors.Distinct()), "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                return;
            }

            foreach (var res in selected)
            {
                var tags = GetEntityTags(res);
                foreach (var kv in tagsToUpdate)
                    tags[kv.Key] = kv.Value;
                res.CombinedTags = new Dictionary<string, string>(tags);
            }

            MessageBox.Show(this, "Tags updated", MessageBoxButtons.OK);
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Failed to apply tags");
            MessageBox.Show(this, "Failed to apply tags", MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }

    private static IDictionary<string, string> GetEntityTags(Resource res)
    {
        return res.EntityType switch
        {
            "ResourceGroup" => res.ResourceGroupTags ??= new Dictionary<string, string>(),
            "Subscription" => res.SubscriptionTags ??= new Dictionary<string, string>(),
            _ => res.ResourceTags ??= new Dictionary<string, string>()
        };
    }

    private void ReloadTagTemplates()
    {
        _tagTemplates = TagTemplatesService.Load();
        var items = new List<string> { "Tag Templates" };
        items.AddRange(_tagTemplates.Select(t => t.TemplateName));
        _cboTagTemplates.DataStore = items;
        _cboTagTemplates.SelectedIndex = 0;
    }

    private async Task OnTagTemplateSelectedAsync()
    {
        var index = _cboTagTemplates.SelectedIndex - 1;
        if (index < 0)
            return;

        var template = _tagTemplates[index];
        var tags = await ResolveTagVariables(template.Tags);
        foreach (var kvp in tags)
        {
            if (kvp.Key.StartsWith("-"))
            {
                var key = kvp.Key.Substring(1);
                var existing = _tags.FirstOrDefault(t => t.Key == key);
                if (existing != null)
                    _tags.Remove(existing);
            }
            else
            {
                var existing = _tags.FirstOrDefault(t => t.Key == kvp.Key);
                if (existing != null)
                    existing.Value = kvp.Value;
                else
                    _tags.Add(new TagEntry { Key = kvp.Key, Value = kvp.Value });
            }
        }

        _cboTagTemplates.SelectedIndex = 0;
    }

    private async Task<Dictionary<string, string>> ResolveTagVariables(Dictionary<string, string> tags)
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
            return Environment.UserName;

        var credential = _azureService.CurrentCredential;
        if (credential == null)
            return Environment.UserName;

        var scopes = new[] { "User.Read" };
        var graphClient = new GraphServiceClient(_azureService.CurrentCredential, scopes);
        var user = await graphClient.Me.GetAsync();
        return user?.Mail ?? user?.UserPrincipalName ?? Environment.UserName;
    }

    private void OpenSelectedResourceInPortal()
    {
        if (_gvwResults.SelectedItem is not Resource res)
            return;

        var url = $"{_azureService.GetAzurePortalUrl()}/#@{_settings.GetAzureContext().TenantId}/resource{res.Id}";
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Failed to open Azure Portal");
            MessageBox.Show(this, "Failed to open Azure Portal", MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }

    private void CopySelectedTagsAsJson()
    {
        if (_gvwResults.SelectedItem is not Resource res || res.CombinedTags == null)
            return;

        try
        {
            Clipboard.Instance.Text = JsonSerializer.Serialize(res.CombinedTags.OrderBy(t => t.Key), new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Failed to copy tags as JSON");
            MessageBox.Show(this, "Failed to copy tags", MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }

    private void OpenUrlsInSelectedTags()
    {
        if (_gvwResults.SelectedItem is not Resource res || res.CombinedTags == null)
            return;

        var urls = new List<string>();
        foreach (var value in res.CombinedTags.Values)
        {
            foreach (Match match in Regex.Matches(value, @"https?://\S+"))
                urls.Add(match.Value);
        }

        if (urls.Count == 0)
        {
            MessageBox.Show(this, "No tags with URLs found.", MessageBoxButtons.OK);
            return;
        }

        foreach (var url in urls)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Failed to open URL");
            }
        }
    }

    private void CopyEmailAddressesInSelectedTags()
    {
        if (_gvwResults.SelectedItem is not Resource res || res.CombinedTags == null)
            return;

        var emails = new HashSet<string>();
        var regex = new Regex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}");
        foreach (var value in res.CombinedTags.Values)
        {
            foreach (Match match in regex.Matches(value))
                emails.Add(match.Value);
        }

        if (emails.Count == 0)
        {
            MessageBox.Show(this, "No email addresses found.", MessageBoxButtons.OK);
            return;
        }

        Clipboard.Instance.Text = string.Join("; ", emails);
    }

    private ContextMenu CreateQuickFilterContextMenu(TextBox textBox)
    {
        var excludeCurrent = new ButtonMenuItem { Text = "Convert to RegEx excluding the current text" };
        excludeCurrent.Click += (_, _) => textBox.Text = $"^(?!.*{textBox.Text}).+$";

        var excludeTemplate = new ButtonMenuItem { Text = "Replace with RegEx to exclude text" };
        excludeTemplate.Click += (_, _) => textBox.Text = "^(?!.*TEXT).+$";

        return new ContextMenu { Items = { excludeTemplate, excludeCurrent } };
    }

    private static object? GetPropertyValue(Resource resource, string propertyName)
    {
        var prop = typeof(Resource).GetProperty(propertyName);
        var value = prop?.GetValue(resource);
        if (value is IDictionary<string, string> tags)
        {
            return JsonSerializer.Serialize(tags.OrderBy(t => t.Key));
        }
        return value;
    }

    private static string FormatTags(IDictionary<string, string> tags, string joinWith = ", \n")
    {
        return string.Join(joinWith, tags.OrderBy(t => t.Key).Select(t => $"\"{t.Key}\": \"{t.Value}\""));
    }

    private void FilterResults()
    {
        var filter1 = ResourceFilters.CreateRegexFilter(
            _cboQuickFilter1Column.SelectedIndex > 0 ? _cboQuickFilter1Column.SelectedValue?.ToString() : null,
            _txtQuickFilter1Text.Text);
            
        var filter2 = ResourceFilters.CreateRegexFilter(
            _cboQuickFilter2Column.SelectedIndex > 0 ? _cboQuickFilter2Column.SelectedValue?.ToString() : null,
            _txtQuickFilter2Text.Text);

        _paginatedResults.SetFilters(filter1, filter2);
        UpdatePaginationControls();
    }

    private void ScheduleDelayedFilter(int filterNumber)
    {
        if (filterNumber == 1)
        {
            _quickFilter1Timer?.Dispose();
            
            if (_cboQuickFilter1Column.SelectedIndex > 0)
            {
                _quickFilter1Timer = new System.Threading.Timer(
                    _ => Application.Instance.Invoke(FilterResults),
                    null,
                    TimeSpan.FromMilliseconds(250),
                    TimeSpan.FromMilliseconds(-1)
                );
            }
        }
        else if (filterNumber == 2)
        {
            _quickFilter2Timer?.Dispose();
            
            if (_cboQuickFilter2Column.SelectedIndex > 0)
            {
                _quickFilter2Timer = new System.Threading.Timer(
                    _ => Application.Instance.Invoke(FilterResults),
                    null,
                    TimeSpan.FromMilliseconds(250),
                    TimeSpan.FromMilliseconds(-1)
                );
            }
        }
    }

    private void ShowAboutDialog()
    {
        var version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown";
        
        var aboutDialog = new Dialog
        {
            Title = "About AzTagger",
            ClientSize = new Size(350, 150),
            Resizable = false
        };

        var content = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            Padding = 20,
            Items =
            {
                new Label { Text = "AzTagger", Font = new Font(FontFamilies.Sans, 18, FontStyle.Bold), TextAlignment = TextAlignment.Center },
                new Label { Text = $"Version {version}", TextAlignment = TextAlignment.Center },
                new Label { Text = "A tool for querying and managing Azure resources and tags.", TextAlignment = TextAlignment.Center, Wrap = WrapMode.Word }
            }
        };

        aboutDialog.Content = content;
        
        // Handle ESC key to close the dialog
        aboutDialog.KeyDown += (_, e) =>
        {
            if (e.Key == Keys.Escape)
            {
                aboutDialog.Close();
                e.Handled = true;
            }
        };

        aboutDialog.ShowModal(this);
    }

    private void SaveSettings()
    {
        _settings.WindowSize = new Settings.WinSize(ClientSize.Width, ClientSize.Height);
        _settings.WindowLocation = new Settings.WinLocation(Location.X, Location.Y);
        _settings.LastSearchQuery = _txtSearchQuery.Text;
        _settings.LastQuickFilter1Text = _txtQuickFilter1Text.Text;
        _settings.LastQuickFilter2Text = _txtQuickFilter2Text.Text;
        _settings.SplitterPosition = _splitter.Position;
        
        _quickFilter1Timer?.Dispose();
        _quickFilter2Timer?.Dispose();
        _resizeTimer?.Dispose();
        _splitterTimer?.Dispose();
        
        SettingsService.Save(_settings);
    }

    private void LoadRecentSearches()
    {
        var items = new List<string> { "Recent Queries" };
        items.AddRange(_settings.RecentSearches);
        _cboRecentSearches.DataStore = items;
        _cboRecentSearches.SelectedIndex = 0;
    }

    private void LoadSavedSearches()
    {
        var items = new List<string> { "Saved Queries" };
        items.AddRange(_settings.SavedSearches.Select(s => s.Name));
        _cboSavedQueries.DataStore = items;
        _cboSavedQueries.SelectedIndex = 0;
    }

    private void SaveRecentSearch(string queryText)
    {
        queryText = queryText.Trim();
        if (string.IsNullOrEmpty(queryText))
            return;

        _settings.RecentSearches.Insert(0, queryText);
        _settings.RemoveDuplicatesFromRecentSearches();
        if (_settings.RecentSearches.Count > 10)
            _settings.RecentSearches.RemoveAt(10);
        SettingsService.Save(_settings);

        LoadRecentSearches();
    }

    private void OnRecentSearchSelected()
    {
        var index = _cboRecentSearches.SelectedIndex - 1;
        if (index >= 0)
        {
            _txtSearchQuery.Text = _settings.RecentSearches[index];
            _cboRecentSearches.SelectedIndex = 0;
        }
    }

    private void OnSavedQuerySelected()
    {
        var index = _cboSavedQueries.SelectedIndex - 1;
        if (index >= 0)
        {
            _txtSearchQuery.Text = _settings.SavedSearches[index].SearchQuery;
            _cboSavedQueries.SelectedIndex = 0;
        }
    }

    private void SaveQuery()
    {
        var dlg = new InputDialog("Save query as...", "Enter a name for the saved query:");
        var name = dlg.ShowModal(this);
        if (name == null)
            return;
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        var existing = _settings.SavedSearches.FirstOrDefault(q => q.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            if (MessageBox.Show(this, $"Overwrite '{name}'?", MessageBoxButtons.YesNo) == DialogResult.No)
                return;
            existing.SearchQuery = _txtSearchQuery.Text;
        }
        else
        {
            _settings.SavedSearches.Add(new SavedSearchItem(name, _txtSearchQuery.Text));
            _settings.SavedSearches.Sort();
        }
        SettingsService.Save(_settings);
        LoadSavedSearches();
    }

    private void UpdatePaginationControls()
    {
        try
        {
            var currentPage = _paginatedResults.CurrentPage + 1;
            var totalPages = _paginatedResults.TotalPages;
            var totalItems = _paginatedResults.TotalFilteredCount;
            
            var subscriptionCount = _paginatedResults.FilteredItems.Count(r => r.EntityType == "Subscription");
            var resourceGroupCount = _paginatedResults.FilteredItems.Count(r => r.EntityType == "ResourceGroup");
            var resourceCount = _paginatedResults.FilteredItems.Count(r => r.EntityType == "Resource");
            
            _lblPageInfo.Text = totalPages > 0 ? $"Page {currentPage} of {totalPages}" : "Page 0 of 0";
            _lblResultsCount.Text = $"Results: {totalItems} (Subscriptions: {subscriptionCount}, Resource Groups: {resourceGroupCount}, Resources: {resourceCount})";
            
            _btnFirstPage.Enabled = _paginatedResults.HasPreviousPage;
            _btnPreviousPage.Enabled = _paginatedResults.HasPreviousPage;
            _btnNextPage.Enabled = _paginatedResults.HasNextPage;
            _btnLastPage.Enabled = _paginatedResults.HasNextPage;
        }
        catch (ArgumentOutOfRangeException)
        {
            _lblPageInfo.Text = "Page 0 of 0";
            _lblResultsCount.Text = "Results: 0";
            _btnFirstPage.Enabled = false;
            _btnPreviousPage.Enabled = false;
            _btnNextPage.Enabled = false;
            _btnLastPage.Enabled = false;
        }
        catch (Exception)
        {
        }
    }

    private void AddToFilterQuery(bool exclude)
    {
        if (_resultsContextRow < 0 || _resultsContextColumn == null)
        {
            return;
        }
        if (_resultsContextRow >= _paginatedResults.DisplayedItems.Count)
        {
            return;
        }

        var item = _paginatedResults.DisplayedItems[_resultsContextRow];
        if (!_columnPropertyMap.TryGetValue(_resultsContextColumn, out var columnName))
            return;

        var value = GetPropertyValue(item, columnName);
        string filter;
        if (value is IDictionary<string, string> tags)
        {
            var tagFilter = string.Join(" and ", tags.Select(t => $"{columnName}['{t.Key}'] =~ '{t.Value}'"));
            filter = exclude ? $"| where not({tagFilter})" : $"| where {tagFilter}";
        }
        else
        {
            var text = value?.ToString()?.Replace("'", "''") ?? string.Empty;
            filter = exclude ? $"| where {columnName} != '{text}'" : $"| where {columnName} =~ '{text}'";
        }

        _txtSearchQuery.Text = (_txtSearchQuery.Text.TrimEnd() + "\n" + filter).Trim();
    }

    private void AddTagToFilterQuery(bool exclude)
    {
        if (_tagsContextRow < 0 || _tagsContextRow >= _tags.Count)
        {
            return;
        }

        var tag = _tags[_tagsContextRow];
        var selected = _gvwResults.SelectedItems.Cast<Resource>().ToList();
        if (selected.Count == 0)
            return;

        if (selected.Count == 1)
        {
            var columnName = selected[0].EntityType switch
            {
                "ResourceGroup" => nameof(Resource.ResourceGroupTags),
                "Subscription" => nameof(Resource.SubscriptionTags),
                _ => nameof(Resource.ResourceTags)
            };
            var clause = $"{columnName}['{tag.Key}'] =~ '{tag.Value}'";
            var filter = exclude ? $"| where not({clause})" : $"| where {clause}";
            _txtSearchQuery.Text = (_txtSearchQuery.Text.TrimEnd() + "\n" + filter).Trim();
        }
        else
        {
            var clause = $"(SubscriptionTags['{tag.Key}'] =~ '{tag.Value}' or ResourceGroupTags['{tag.Key}'] =~ '{tag.Value}' or ResourceTags['{tag.Key}'] =~ '{tag.Value}')";
            var filter = exclude ? $"| where not({clause})" : $"| where {clause}";
            _txtSearchQuery.Text = (_txtSearchQuery.Text.TrimEnd() + "\n" + filter).Trim();
        }
    }

    private void CopyContextCellValue()
    {
        if (_resultsContextRow < 0 || _resultsContextColumn == null)
        {
            return;
        }
        if (_resultsContextRow >= _paginatedResults.DisplayedItems.Count)
        {
            return;
        }

        var item = _paginatedResults.DisplayedItems[_resultsContextRow];
        if (!_columnPropertyMap.TryGetValue(_resultsContextColumn, out var columnName))
            return;

        var value = GetPropertyValue(item, columnName);
        if (value is IDictionary<string, string> tags)
            Clipboard.Instance.Text = FormatTags(tags, Environment.NewLine);
        else
            Clipboard.Instance.Text = value?.ToString() ?? string.Empty;
    }

    private void CopyTagContextCellValue()
    {
        if (_tagsContextRow < 0 || _tagsContextRow >= _tags.Count || _tagsContextColumn == null)
        {
            LoggingService.LogError(new InvalidOperationException(), $"CopyTagContextCellValue: Invalid context - RowIndex: {_tagsContextRow}, Tags.Count: {_tags.Count}, Column: {_tagsContextColumn != null}");
            return;
        }

        var tag = _tags[_tagsContextRow];
        var value = _tagsContextColumn.HeaderText.Contains("Key") ? tag.Key : tag.Value;
        Clipboard.Instance.Text = value ?? string.Empty;
    }

    private void SortResults(GridColumn column)
    {
        if (!_columnPropertyMap.TryGetValue(column, out var columnName))
            return;

        if (_sortColumn == columnName)
            _sortAscending = !_sortAscending;
        else
        {
            _sortColumn = columnName;
            _sortAscending = true;
        }

        _allResults = _sortAscending
            ? _allResults.OrderBy(r => GetPropertyValue(r, columnName)?.ToString()).ToList()
            : _allResults.OrderByDescending(r => GetPropertyValue(r, columnName)?.ToString()).ToList();

        _paginatedResults.SetAllItems(_allResults);
        FilterResults();
    }

    private async Task RefreshTagsAsync()
    {
        var selected = _gvwResults.SelectedItems.Cast<Resource>().ToList();
        if (selected.Count == 0)
            return;

        try
        {
            await EnsureSignedInAsync();
            var ids = string.Join(", ", selected.Select(r => $"'{r.Id}'"));
            var query = BaseQuery + "\n| where Id in (" + ids + ") | project Id, SubscriptionTags, ResourceGroupTags, ResourceTags, CombinedTags";
            var updated = await _azureService.QueryResourcesAsync(query);
            foreach (var up in updated)
            {
                var local = _allResults.FirstOrDefault(r => r.Id == up.Id);
                if (local != null)
                {
                    local.SubscriptionTags = up.SubscriptionTags;
                    local.ResourceGroupTags = up.ResourceGroupTags;
                    local.ResourceTags = up.ResourceTags;
                    local.CombinedTags = up.CombinedTags;
                }
            }
            LoadTagsForSelection();
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Failed to refresh tags");
            MessageBox.Show(this, "Failed to refresh tags", MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }

    private static string FormatPropertyForGrid(Resource r, string propertyName)
    {
        var prop = typeof(Resource).GetProperty(propertyName);
        var value = prop?.GetValue(r);
        if (value is IDictionary<string, string> dict)
        {
            if (dict.Count == 0) return string.Empty;
            return string.Join(", ", dict.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));
        }
        return value?.ToString() ?? string.Empty;
    }

    private static string FormatPropertyForTooltip(Resource r, string propertyName)
    {
        var prop = typeof(Resource).GetProperty(propertyName);
        var value = prop?.GetValue(r);
        if (value is IDictionary<string, string> dict)
        {
            if (dict.Count == 0) return string.Empty;
            
            const int maxLineWidth = 50;
            const string indent = "    ";
            
            var formattedLines = new List<string>();
            
            foreach (var kv in dict.OrderBy(kvp => kvp.Key))
            {
                var keyValueLine = FormatKeyValueWithWrapping(kv.Key, kv.Value, maxLineWidth, indent);
                formattedLines.Add(keyValueLine);
            }
            
            return string.Join("\n", formattedLines);
        }
        return value?.ToString() ?? string.Empty;
    }
    
    private static string FormatKeyValueWithWrapping(string key, string value, int maxLineWidth, string indent)
    {
        try {
            // Add bounds checking to prevent negative calculations
            if (maxLineWidth <= 0)
            {
                return $"{key}: {value}"; // Return simple format
            }
            
            var keyPart = $"{key}:";
            var fullLine = $"{keyPart} {value}";
            
            if (fullLine.Length <= maxLineWidth)
            {
                return fullLine;
            }
            
            var result = new List<string>();
            var remainingValue = value;
            var availableWidth = maxLineWidth - keyPart.Length - 1;
            
            // Ensure availableWidth is positive
            if (availableWidth <= 0)
            {
                return $"{key}: {value}"; // Return simple format
            }
            
            if (remainingValue.Length <= availableWidth)
            {
                result.Add($"{keyPart} {remainingValue}");
            }
            else
            {
                var breakIndex = FindBestBreakIndex(remainingValue, availableWidth);
                
                if (breakIndex < 0 || breakIndex > remainingValue.Length)
                {
                    return $"{key}: {value}"; // Return simple format
                }
                
                var firstPart = remainingValue.Substring(0, breakIndex);
                remainingValue = remainingValue.Substring(breakIndex);
                
                result.Add($"{keyPart} {firstPart}");
                
                var indentedMaxWidth = maxLineWidth - indent.Length;
                
                // Ensure indentedMaxWidth is positive
                if (indentedMaxWidth <= 0)
                {
                    indentedMaxWidth = 10; // Minimum useful width
                }
                
                while (remainingValue.Length > 0)
                {
                    if (remainingValue.Length <= indentedMaxWidth)
                    {
                        result.Add($"{indent}{remainingValue}");
                        break;
                    }
                    else
                    {
                        var chunkBreakIndex = FindBestBreakIndex(remainingValue, indentedMaxWidth);
                        
                        if (chunkBreakIndex <= 0 || chunkBreakIndex > remainingValue.Length)
                        {
                            result.Add($"{indent}{remainingValue}"); // Add remaining text and break
                            break;
                        }
                        
                        var chunk = remainingValue.Substring(0, chunkBreakIndex);
                        remainingValue = remainingValue.Substring(chunkBreakIndex);
                        
                        result.Add($"{indent}{chunk}");
                    }
                }
            }
            
            return string.Join("\n", result);
        }
        catch (Exception)
        {
            return $"{key}: {value}"; // Return simple format as fallback
        }
    }
    
    private static int FindBestBreakIndex(string text, int maxLength)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }
            
            if (maxLength <= 0)
            {
                return Math.Min(1, text.Length); // Return minimal safe value
            }
            
            if (text.Length <= maxLength)
            {
                return text.Length;
            }
            
            // Ensure we don't go out of bounds
            var safeMaxIndex = Math.Min(maxLength - 1, text.Length - 1);
            var safeMinIndex = Math.Max(0, maxLength / 3);
            
            if (safeMaxIndex < 0 || safeMinIndex > safeMaxIndex)
            {
                return Math.Min(maxLength, text.Length);
            }
            
            // Look for '/' character
            for (int i = safeMaxIndex; i >= safeMinIndex; i--)
            {
                if (text[i] == '/' && i + 1 < text.Length)
                {
                    return i + 1;
                }
            }
            
            // Look for '@' character
            for (int i = safeMaxIndex; i >= safeMinIndex; i--)
            {
                if (text[i] == '@')
                {
                    return i;
                }
            }
            
            // Look for space character (with different range)
            var spaceMinIndex = Math.Max(0, maxLength / 2);
            for (int i = safeMaxIndex; i >= spaceMinIndex; i--)
            {
                if (text[i] == ' ')
                {
                    return i + 1;
                }
            }
            
            // Look for other separators
            var separators = new char[] { '-', '_', '.', ':', ';', ',', '&', '?', '=', '|', '\\' };
            for (int i = safeMaxIndex; i >= safeMinIndex; i--)
            {
                if (separators.Contains(text[i]) && i + 1 < text.Length)
                {
                    return i + 1;
                }
            }
            
            return Math.Min(maxLength, text.Length);
        }
        catch (Exception)
        {
            return Math.Min(Math.Max(1, maxLength), text?.Length ?? 0);
        }
    }

    private void CalculateInitialTagsPanelHeight()
    {
        var availableHeight = GetAvailableHeightForSplitter();
        if (availableHeight > 0)
        {
            if (_settings.SplitterPosition > 0 && _settings.SplitterPosition < availableHeight)
            {
                var tagsPanelHeight = availableHeight - _settings.SplitterPosition;
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, tagsPanelHeight);
            }
            else
            {
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, (int)(availableHeight / 3.0));
            }
        }
        else
        {
            _fixedTagsPanelHeight = GetDpiScaledWidth(200);
        }
    }

    private void UpdateSplitterPosition()
    {
        var availableHeight = GetAvailableHeightForSplitter();
        if (availableHeight <= 0) return;

        var maxAllowedTagsHeight = (int)(availableHeight * 0.7);
        _fixedTagsPanelHeight = Math.Min(_fixedTagsPanelHeight, maxAllowedTagsHeight);
        _fixedTagsPanelHeight = Math.Max(_fixedTagsPanelHeight, MinTagsPanelHeight);

        var desiredPosition = availableHeight - _fixedTagsPanelHeight;
        
        var minPosition = MinResultsPanelHeight;
        var maxPosition = availableHeight - MinTagsPanelHeight;
        
        var newPosition = Math.Max(minPosition, Math.Min(maxPosition, desiredPosition));
        
        if (Math.Abs(_splitter.Position - newPosition) > 5)
        {
            _isProgrammaticSplitterUpdate = true;
            _splitter.Position = newPosition;
            _isProgrammaticSplitterUpdate = false;
            
            _splitter.Invalidate();
            
            Content?.Invalidate();
        }
        
        var actualTagsPanelHeight = availableHeight - _splitter.Position;
        if (actualTagsPanelHeight != _fixedTagsPanelHeight)
        {
            _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, actualTagsPanelHeight);
        }
    }

    private void ScheduleDelayedSplitterConstraint()
    {
        _splitterTimer?.Dispose();
        
        _splitterTimer = new System.Threading.Timer(
            _ => Application.Instance.Invoke(EnforceSplitterMinimumHeights),
            null,
            TimeSpan.FromMilliseconds(300),
            TimeSpan.FromMilliseconds(-1) 
        );
    }

    private void EnforceSplitterMinimumHeights()
    {
        if (_isProgrammaticSplitterUpdate) return;
        
        var availableHeight = GetAvailableHeightForSplitter();
        if (availableHeight > 0)
        {
            var minPosition = MinResultsPanelHeight;
            var maxPosition = availableHeight - MinTagsPanelHeight;
            var currentPosition = _splitter.Position;
            
            if (currentPosition < minPosition || currentPosition > maxPosition)
            {
                var correctedPosition = Math.Max(minPosition, Math.Min(maxPosition, currentPosition));
                
                _isProgrammaticSplitterUpdate = true;
                _splitter.Position = correctedPosition;
                _isProgrammaticSplitterUpdate = false;
                
                _splitter.Invalidate();
                Content?.Invalidate();
                
                var tagsPanelHeight = availableHeight - correctedPosition;
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, tagsPanelHeight);
            }
        }
    }

    private int GetActualSplitterHeight()
    {
        if (_splitter != null && _splitter.Height > 0)
        {
            return _splitter.Height;
        }
        
        int baseUIHeight = 320;
        int nonSplitterUIHeight = GetDpiScaledWidth(baseUIHeight);
        var estimatedHeight = Math.Max(300, ClientSize.Height - nonSplitterUIHeight);
        
        return estimatedHeight;
    }

    private int GetAvailableHeightForSplitter()
    {
        return GetActualSplitterHeight();
    }
}
