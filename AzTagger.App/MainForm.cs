// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using AzTagger.Models;
using AzTagger.Services;

namespace AzTagger.App;

public partial class MainForm : Form
{
    // Fields and properties
    private Settings _settings;
    private AzureService _azureService;

    // UI Controls
    private TextArea _txtSearchQuery;
    private  GridView _gvwResults;
    private  GridView _gvwTags;
    private  Splitter _splitter;
    private  Button _btnSearch;
    private  Button _btnClearQuery;
    private  Button _btnCopyQuery;
    private  Button _btnSaveQuery;
    private  Button _btnApplyTags;
    private  DropDown _cboTagTemplates;
    private  Button _btnEditTemplates;

    private  DropDown _cboRecentSearches;
    private  DropDown _cboSavedQueries;

    private  DropDown _cboQuickFilter1Column;
    private  TextBox _txtQuickFilter1Text;
    private  Button _btnQuickFilter1Exclude;
    private  DropDown _cboQuickFilter2Column;
    private  TextBox _txtQuickFilter2Text;
    private  Button _btnQuickFilter2Exclude;
    private  Label _lblResultsCount;
    private  Label _lblQueryMode;

    private System.Threading.Timer _quickFilter1Timer;
    private System.Threading.Timer _quickFilter2Timer;
    private System.Threading.Timer _resizeTimer;
    private System.Threading.Timer _splitterTimer;

    private  Button _btnFirstPage;
    private  Button _btnPreviousPage;
    private  Button _btnNextPage;
    private  Button _btnLastPage;
    private  Label _lblPageInfo;
    private  ComboBox _cboPageSize;

    private  LinkButton _lnkRegExDocs;
    private  LinkButton _lnkResourceGraphDocs;
    private  LinkButton _lnkGitHub;
    private  LinkButton _lnkDonation;
    private  LinkButton _lnkEditSettings;
    private  LinkButton _lnkShowErrorLog;
    private  LinkButton _lnkResetDefaults;
    private  Label _lblVersion;
    private  ContextMenu _resultsContextMenu;
    private  ContextMenu _tagsContextMenu;

    // State
    private string _sortColumn = string.Empty;
    private bool _sortAscending = true;

    private int _resultsContextRow = -1;
    private GridColumn _resultsContextColumn;
    private int _tagsContextRow = -1;
    private GridColumn _tagsContextColumn;
    private  Dictionary<GridColumn, string> _columnPropertyMap = new();

    private List<TagTemplate> _tagTemplates = new();

    private  ObservableCollection<TagEntry> _tags = new();
    private  PaginatedResourceCollection _paginatedResults = new();
    private List<Resource> _allResults = new();

    private int _fixedTagsPanelHeight = 200;
    
    private int MinResultsPanelHeight 
    { 
        get 
        {
            var availableHeight = GetActualSplitterHeight();
            return Math.Max(GetDpiScaledSize(60), (int)(availableHeight * 0.2));
        }
    }
    
    private int MinTagsPanelHeight 
    { 
        get 
        {
            var availableHeight = GetActualSplitterHeight(); 
            return Math.Max(GetDpiScaledSize(60), (int)(availableHeight * 0.2));
        }
    }
    
    private bool _isProgrammaticSplitterUpdate = false;
    private bool _isClosing = false;

    private  ProgressBar _searchProgress;
    private  ProgressBar _applyTagsProgress;
    private  ProgressBar _resultsRefreshProgress;

    // Constants
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
        // Initialize progress bars and settings
        _searchProgress = new ProgressBar { Indeterminate = true, Visible = false, Width = 24, Height = 24 };
        _applyTagsProgress = new ProgressBar { Indeterminate = true, Visible = false, Width = 24, Height = 24 };
        _resultsRefreshProgress = new ProgressBar { Indeterminate = true, Visible = false, Width = 24, Height = 24 };
        _settings = SettingsService.Load();

        UpdateTitle();
        SetupIconAndWindowProperties();

        _azureService = new AzureService(_settings);
        _tagTemplates = TagTemplatesService.Load();

        // Initialize UI controls
        InitializeControls();
        
        // Create UI layout
        CreateLayout();
        
        // Load data and set initial state
        LoadInitialData();
        
        // Setup event handlers
        SetupFormEvents();
    }

    private void InitializeControls()
    {
        // Initialize text controls
        _txtSearchQuery = new TextArea 
        { 
            Height = 80,
            SpellCheck = false,
            TextReplacements = TextReplacements.None
        };
        _txtQuickFilter1Text = new TextBox { Width = GetDpiScaledSize(150), PlaceholderText = "Quick filter 1 regex..." };
        _btnQuickFilter1Exclude = new Button { Text = "!", Width = GetDpiScaledSize(6), Height = GetDpiScaledSize(6), ToolTip = "Toggle include/exclude filter" };
        _txtQuickFilter2Text = new TextBox { Width = GetDpiScaledSize(150), PlaceholderText = "Quick filter 2 regex..." };
        _btnQuickFilter2Exclude = new Button { Text = "!", Width = GetDpiScaledSize(6), Height = GetDpiScaledSize(6), ToolTip = "Toggle include/exclude filter" };

        // Initialize buttons
        _btnSearch = new Button { Text = "Search" };
        _btnClearQuery = new Button { Text = "Clear" };
        _btnCopyQuery = new Button { Text = "Copy Query" };
        _btnSaveQuery = new Button { Text = "Save Query" };
        _btnApplyTags = new Button { Text = "Apply Tags" };
        _btnEditTemplates = new Button { Text = "Edit Templates" };

        // Initialize pagination controls
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

        // Initialize dropdowns
        _cboRecentSearches = new DropDown();
        _cboSavedQueries = new DropDown();
        _cboQuickFilter1Column = new DropDown { Width = GetDpiScaledSize(130) };
        _cboQuickFilter2Column = new DropDown { Width = GetDpiScaledSize(130) };
        _cboTagTemplates = new DropDown { Width = GetDpiScaledSize(200) };

        // Initialize labels
        _lblResultsCount = new Label();
        _lblQueryMode = new Label { Text = string.Empty, TextColor = Colors.Gray, Font = new Font(SystemFont.Default) };
        
        var version = typeof(MainForm).Assembly.GetName().Version?.ToString() ?? "";
        if (version.Contains('.'))
            version = version[..version.LastIndexOf('.')];
        _lblVersion = new Label { Text = $"Version: {version}" };

        // Initialize link buttons
        _lnkRegExDocs = new LinkButton { Text = ".NET RegEx Docs" };
        _lnkResourceGraphDocs = new LinkButton { Text = "Resource Graph Docs" };
        _lnkGitHub = new LinkButton { Text = "GitHub" };
        _lnkDonation = new LinkButton { Text = "Donate" };
        _lnkEditSettings = new LinkButton { Text = "Edit Settings File" };
        _lnkShowErrorLog = new LinkButton { Text = "Show Error Log" };
        _lnkResetDefaults = new LinkButton { Text = "Reset UI to Defaults" };

        // Initialize grids
        _gvwResults = new GridView { DataStore = _paginatedResults.DisplayedItems, AllowMultipleSelection = true };
        _gvwTags = new GridView { DataStore = _tags, AllowMultipleSelection = false };

        // Setup individual control behaviors
        CreateMenuBar();
        SetupKeyboardShortcuts();
        CreateSearchControls();
        CreateResultsGrid();
        CreateTagsGrid();
        CreateContextMenus();
        CreatePaginationControls();
        CreateQuickFilterControls();
        CreateTagTemplateControls();
        CreateLinkButtons();
    }

    private void CreateLayout()
    {
        var configureButton = new Button { Text = "Configure Azure Context" };
        configureButton.Click += (_, _) =>
        {
            var dlg = new AzureContextConfigDialog(_settings, _azureService);
            if (dlg.ShowModal(this))
            {
                SettingsService.Save(_settings);
                UpdateTitle(); // Update title when Azure context changes
            }
        };

        var searchProgressPanel = new Panel { Content = _searchProgress, Width = 28, Height = 24, MinimumSize = new Size(28, 24) };
        var topRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items =
            {
                _btnSearch,
                searchProgressPanel,
                new Panel { Width = GetDpiScaledSize(10) },
                _btnSaveQuery,
                _btnCopyQuery,
                _btnClearQuery,
                new Panel { Width = GetDpiScaledSize(10) },
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
            new TableCell(new Panel { Width = GetDpiScaledSize(3) }, false), // separator
            new TableCell(_cboSavedQueries, false)
        ));
        _cboRecentSearches.Width = -1;
        _cboSavedQueries.Width = GetDpiScaledSize(250);
        layout.Items.Add(new StackLayoutItem(recentSavedRow, HorizontalAlignment.Stretch));
        
        layout.Items.Add(new StackLayoutItem(new StackLayout {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalContentAlignment = VerticalAlignment.Center,
            Items = {
                new Label { Text = "Search Query:" },
                _lblQueryMode,
                new StackLayoutItem(null, true), // stretch
                _lnkResourceGraphDocs
            }
        }, HorizontalAlignment.Stretch));
        
        layout.Items.Add(new StackLayoutItem(_txtSearchQuery, HorizontalAlignment.Stretch) { Expand = false });
        layout.Items.Add(new StackLayoutItem(topRow, HorizontalAlignment.Stretch));
        layout.Items.Add(new Panel { Padding = new Padding(0, 5, 0, 0), Content = new Label { Text = "Results:" } });
        
        var _lnkClearFilters = new LinkButton { Text = "Clear filters" };
        _lnkClearFilters.Click += (_, _) =>
        {
            _cboQuickFilter1Column.SelectedValue = Constants.QuickFilterNone;
            _cboQuickFilter2Column.SelectedValue = Constants.QuickFilterNone;
            _txtQuickFilter1Text.Text = string.Empty;
            _txtQuickFilter2Text.Text = string.Empty;
        };

        var quickFilterRow = new TableLayout();
        quickFilterRow.Rows.Add(new TableRow(
            new TableCell(_txtQuickFilter1Text, true),
            new TableCell(_btnQuickFilter1Exclude, false),
            new TableCell(_cboQuickFilter1Column, false),
            new TableCell(new Panel { Width = GetDpiScaledSize(5) }, false), // separator
            new TableCell(_txtQuickFilter2Text, true),
            new TableCell(_btnQuickFilter2Exclude, false),
            new TableCell(_cboQuickFilter2Column, false),
            new TableCell(new Panel { Width = GetDpiScaledSize(5) }, false), // separator
            new TableCell(_lnkClearFilters, false),
            new TableCell(new Panel { Width = GetDpiScaledSize(5) }, false), // separator
            new TableCell(_lnkRegExDocs, false) 
        ));
        layout.Items.Add(new StackLayoutItem(quickFilterRow, HorizontalAlignment.Stretch) { VerticalAlignment = VerticalAlignment.Center });
        
        var resultsPanel = new TableLayout { Spacing = new Size(5, 5) };
        resultsPanel.Rows.Add(new TableRow(new TableCell(_gvwResults, true)) { ScaleHeight = true });
        
        var paginationRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            VerticalContentAlignment = VerticalAlignment.Center,
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
                _applyTagsProgress,
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
            SplitterWidth = GetDpiScaledSize(8)
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

        layout.Items.Add(new Panel { Height = GetDpiScaledSize(2) });

        var linksRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalContentAlignment = VerticalAlignment.Center,
            Items =
            {
                _lnkEditSettings,
                _lnkShowErrorLog,
                _lnkResetDefaults,
                new StackLayoutItem(null, true),
                _lnkGitHub,
                _lnkDonation,
                _lblVersion
            }
        };
        layout.Items.Add(new StackLayoutItem(linksRow, HorizontalAlignment.Stretch));

        Content = layout;
    }

    private void LoadInitialData()
    {
        LoadRecentSearches();
        LoadSavedSearches();
        ReloadTagTemplates();

        _txtSearchQuery.Text = _settings.LastSearchQuery;
        _txtQuickFilter1Text.Text = _settings.LastQuickFilter1Text;
        _txtQuickFilter2Text.Text = _settings.LastQuickFilter2Text;
    }
}
