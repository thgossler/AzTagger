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

    // Delayed filter timers
    private System.Threading.Timer? _quickFilter1Timer;
    private System.Threading.Timer? _quickFilter2Timer;
    private System.Threading.Timer? _resizeTimer;
    private System.Threading.Timer? _splitterTimer;

    // Pagination controls
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

    // Splitter management - tags panel maintains fixed height, results panel grows/shrinks
    // Use base values that will be DPI-scaled when used
    private int _fixedTagsPanelHeight = 200; // Fixed height for tags panel (6 rows + controls)
    
    // Calculate minimum panel heights as 15% of available height with absolute minimums
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
    
    private bool _isProgrammaticSplitterUpdate = false; // Flag to prevent splitter position feedback loops

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
        
        // Set minimum window size
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

        // Create MenuBar with File menu and platform-specific Exit item with keyboard shortcuts
        CreateMenuBar();

        // Add keyboard shortcut handlers
        SetupKeyboardShortcuts();

        _txtSearchQuery = new TextArea 
        { 
            Height = 80,
            SpellCheck = false,
            TextReplacements = TextReplacements.None
        };
        
        // Ensure text replacements are thoroughly disabled for macOS
        _txtSearchQuery.GotFocus += (_, _) => 
        {
            // Force disable all text replacements when the control gets focus
            _txtSearchQuery.TextReplacements = TextReplacements.None;
            // Also explicitly disable spell check to prevent any interference
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

        // Set up GridViews - remove fixed heights so they can resize with splitter
        _gvwResults = new GridView { DataStore = _paginatedResults.DisplayedItems, AllowMultipleSelection = true };
        
        // Dynamically add columns for all Resource properties except CombinedTagsFormatted
        var resourceProps = typeof(Resource).GetProperties()
            .Where(p => p.Name != nameof(Resource.CombinedTagsFormatted));
        foreach (var prop in resourceProps)
        {
            // Set reasonable default widths based on property name
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
                    _resultsContextRow = cell.RowIndex;
                    _resultsContextColumn = cell.Column;
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
        _cboQuickFilter1Column.DataStore = new List<string>(new[] { string.Empty }.Concat(propertyNames));
        _cboQuickFilter2Column.DataStore = new List<string>(new[] { string.Empty }.Concat(propertyNames));
        _cboQuickFilter1Column.SelectedIndexChanged += (_, _) => FilterResults();
        _cboQuickFilter2Column.SelectedIndexChanged += (_, _) => FilterResults();

        _txtQuickFilter1Text = new TextBox { Width = GetDpiScaledWidth(180) };
        _txtQuickFilter2Text = new TextBox { Width = GetDpiScaledWidth(180) };
        _txtQuickFilter1Text.TextChanged += (_, _) => ScheduleDelayedFilter(1);
        _txtQuickFilter2Text.TextChanged += (_, _) => ScheduleDelayedFilter(2);

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

        // Wire up paginated collection events
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
                new Panel { Width = GetDpiScaledWidth(10) }, // Fixed separator space after Search button (about half button width)
                _btnSaveQuery,
                _btnCopyQuery,
                _btnClearQuery,
                new Panel { Width = GetDpiScaledWidth(10) }, // Fixed separator space before Configure button (about half button width)
                configureButton,
                null
            }
        };

        // Use StackLayout for the main container to ensure full width utilization
        var layout = new StackLayout 
        { 
            Orientation = Orientation.Vertical,
            Spacing = 5, 
            Padding = 10,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        
        // Add controls to stack layout
        var recentSavedRow = new TableLayout
        {
            Spacing = new Size(5, 0)
        };
        recentSavedRow.Rows.Add(new TableRow(
            new TableCell(_cboRecentSearches, true),  // scaleWidth = true for both
            new TableCell(new Panel { Width = GetDpiScaledWidth(3) }, false), // 2px separator
            new TableCell(_cboSavedQueries, false)
        ));
        // Create custom sizing by setting specific widths relative to parent
        _cboRecentSearches.Width = -1; // Use default for now, will be managed by TableLayout
        _cboSavedQueries.Width = GetDpiScaledWidth(150); // Fixed width for saved queries dropdown
        layout.Items.Add(new StackLayoutItem(recentSavedRow, HorizontalAlignment.Stretch));
        
        layout.Items.Add(new Panel { Padding = new Padding(0, 5, 0, 0), Content = new Label { Text = "Search Query:" } });
        layout.Items.Add(new StackLayoutItem(_txtSearchQuery, HorizontalAlignment.Stretch) { Expand = false });
        layout.Items.Add(new StackLayoutItem(topRow, HorizontalAlignment.Stretch));
        layout.Items.Add(new Panel { Padding = new Padding(0, 5, 0, 0), Content = new Label { Text = "Results:" } });
        
        var quickFilterRow = new TableLayout();
        var cboQuickFilter2WithMargin = new Panel { Padding = new Padding(GetDpiScaledWidth(4), 0, 0, 0), Content = _cboQuickFilter2Column };
        quickFilterRow.Rows.Add(new TableRow(new TableCell(_cboQuickFilter1Column, false), new TableCell(_txtQuickFilter1Text, true), new TableCell(cboQuickFilter2WithMargin, false), new TableCell(_txtQuickFilter2Text, true), new TableCell(null, true)));
        layout.Items.Add(new StackLayoutItem(quickFilterRow, HorizontalAlignment.Stretch));
        
        // Results panel with grid that resizes and fixed pagination at bottom
        var resultsPanel = new TableLayout { Spacing = new Size(5, 5) };
        resultsPanel.Rows.Add(new TableRow(new TableCell(_gvwResults, true)) { ScaleHeight = true }); // Grid stretches full width and height
        
        // Pagination controls anchored to the right
        var paginationRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items =
            {
                _lblResultsCount,
                new StackLayoutItem(null, true), // Spacer to push pagination controls to the right
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
        // Add 5px space at bottom of results panel (above splitter line)
        resultsPanel.Rows.Add(new TableRow(new Panel { Height = 5 }));

        // Tags panel with grid that resizes and fixed controls at bottom
        var tagsPanel = new TableLayout { Spacing = new Size(5, 5) };
        // Add 5px space at top of tags panel (below splitter line)
        tagsPanel.Rows.Add(new TableRow(new Panel { Height = 5 }));
        tagsPanel.Rows.Add(new TableRow(new TableCell(_gvwTags, true)) { ScaleHeight = true }); // Grid stretches full width and height
        
        // Tag templates row - Apply Tags anchored left, Tag Templates + Edit Templates anchored right
        var tagTemplatesRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items = 
            { 
                _btnApplyTags,
                new StackLayoutItem(null, true), // Spacer to push tag templates to the right
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
            Position = Math.Max(100, _settings.SplitterPosition), // Set a conservative initial position
            SplitterWidth = GetDpiScaledWidth(8) // Make splitter thicker and more visible for easier interaction
        };
        
        // Handle manual splitter position changes to update fixed tags panel height
        // Use a flag to prevent feedback loops between manual changes and automatic updates
        _splitter.PositionChanged += (_, _) =>
        {
            if (_isProgrammaticSplitterUpdate) return;
            
            var availableHeight = GetAvailableHeightForSplitter();
            if (availableHeight > 0)
            {
                // Calculate the tags panel height based on current splitter position
                var tagsPanelHeight = availableHeight - _splitter.Position;
                // Update the fixed tags panel height when user manually moves splitter
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, tagsPanelHeight);
            }
            
            // Schedule delayed enforcement of minimum heights after user stops dragging
            ScheduleDelayedSplitterConstraint();
        };
        
        // Initial calculation will be done in Shown event when layout is complete
        
        layout.Items.Add(new StackLayoutItem(_splitter, HorizontalAlignment.Stretch, true)); // Splitter expands vertically

        // Add 2px space before footer
        layout.Items.Add(new Panel { Height = GetDpiScaledWidth(2) });

        // Fixed links and version at bottom - stretch across full width
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
                new StackLayoutItem(null, true), // Spacer to push version to the right
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

        // Save settings and quit when the window is closed
        Closing += (_, _) => SaveSettings();
        Closed += (_, _) => Application.Instance.Quit();

        Shown += (_, _) => 
        {
            // Additional safeguard: ensure text replacements are disabled after the form is fully loaded
            _txtSearchQuery.TextReplacements = TextReplacements.None;
            _txtSearchQuery.SpellCheck = false;
            
            // Now that layout is complete, calculate proper initial splitter position
            CalculateInitialTagsPanelHeight();
            UpdateSplitterPosition();
            
            // Ensure splitter is properly drawn at the initial position
            _splitter.Invalidate();
            
            // Delay column resizing to ensure layout is fully established
            Application.Instance.AsyncInvoke(() => 
            {
                ResizeResultsGridColumns();
                ResizeTagsGridColumns();
            });
        };
        SizeChanged += (_, _) => 
        {
            // Use delayed resize to avoid excessive calls during window resizing
            ScheduleDelayedResize();
        };
    }

    private void CreateMenuBar()
    {
        // Create Close command with Command+W for macOS
        var closeCommand = new Command
        {
            MenuText = "&Close",
            ToolBarText = "Close",
            Shortcut = Application.Instance.CommonModifier | Keys.W  // Cmd+W on Mac, Ctrl+W on others
        };
        closeCommand.Executed += (_, _) => ExitApplication();

        // Create Exit command with platform-specific keyboard shortcuts
        var exitCommand = new Command
        {
            MenuText = "E&xit",
            ToolBarText = "Exit",
            Shortcut = Application.Instance.CommonModifier | Keys.Q  // Cmd+Q on Mac, Ctrl+Q on others
        };
        exitCommand.Executed += (_, _) => ExitApplication();

        // Create additional exit command for Alt+F4 (Windows standard)
        var altF4ExitCommand = new Command
        {
            Shortcut = Keys.Alt | Keys.F4
        };
        altF4ExitCommand.Executed += (_, _) => ExitApplication();

        // Create About menu item for macOS application menu
        var aboutItem = new ButtonMenuItem { Text = "&About AzTagger..." };
        aboutItem.Click += (_, _) => ShowAboutDialog();

        // Create File menu
        var fileMenu = new SubMenuItem
        {
            Text = "&File",
            Items = { closeCommand, exitCommand }
        };

        // Create MenuBar and add File menu
        var menuBar = new MenuBar
        {
            Items = { fileMenu }
        };

        // Set required items for proper macOS menu handling
        menuBar.QuitItem = exitCommand;  // This populates the "Quit AzTagger" item
        menuBar.AboutItem = aboutItem;   // This populates the "About AzTagger" item and prevents empty menu items

        // Add application commands for keyboard shortcuts (only add Alt+F4, not duplicate Cmd+Q)
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            menuBar.ApplicationItems.Add(altF4ExitCommand);
        }

        Menu = menuBar;
    }

    private void SetupKeyboardShortcuts()
    {
        // Handle Escape key to close application
        KeyDown += (_, e) =>
        {
            if (e.Key == Keys.Escape)
            {
                ExitApplication();
                e.Handled = true;
            }
            // Handle Page Up/Down for pagination
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
            // Handle Ctrl+Home/End for first/last page
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

    // Helper method to get DPI-scaled width to handle high-DPI displays properly
    private int GetDpiScaledWidth(int baseWidth)
    {
        // Get the logical DPI scale factor
        var scale = Screen.LogicalPixelSize;
        return (int)(baseWidth * scale);
    }

    private void ResizeResultsGridColumns()
    {
        if (_gvwResults.Columns.Count == 0)
            return;
        
        // Account for DPI scaling when calculating tolerance and width
        int tolerance = GetDpiScaledWidth(20); // Reduced tolerance for better utilization
        int actualGridWidth = _gvwResults.Width - tolerance;
        
        // Don't impose artificial minimum - let it shrink naturally
        if (actualGridWidth <= GetDpiScaledWidth(100)) // Only skip if extremely small
            return;
        
        int availableWidth = actualGridWidth;
        
        int colCount = _gvwResults.Columns.Count;
        
        // Set more reasonable column widths based on content type
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
                totalPreferredWidth += GetDpiScaledWidth(150); // Default width
            }
        }
        
        // Scale widths proportionally to fit available space
        double scaleFactor = (double)availableWidth / totalPreferredWidth;
        
        // Apply scaling to all columns to ensure they always fit the available width
        for (int i = 0; i < colCount; i++)
        {
            var column = _gvwResults.Columns[i];
            int preferredWidth = GetDpiScaledWidth(150); // Default
            
            if (_columnPropertyMap.TryGetValue(column, out var propertyName) && 
                columnWidths.TryGetValue(propertyName, out var configuredWidth))
            {
                preferredWidth = configuredWidth;
            }
            
            // Apply scale factor and ensure minimum width (allows aggressive shrinking)
            int scaledWidth = (int)(preferredWidth * scaleFactor);
            int finalWidth = Math.Max(GetDpiScaledWidth(40), scaledWidth); // Allow shrinking to 40px minimum
            column.Width = finalWidth;
        }
        
        // If total minimum widths exceed available space, proportionally reduce all columns
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
        
        // Account for DPI scaling when calculating tolerance and width
        int tolerance = GetDpiScaledWidth(20); // Reduced tolerance for better utilization
        int actualGridWidth = _gvwTags.Width - tolerance;
        
        // Don't impose artificial minimum - let it shrink naturally
        if (actualGridWidth <= GetDpiScaledWidth(80)) // Only skip if extremely small
            return;
        
        int availableWidth = actualGridWidth;
        
        // For tags grid, we have Key and Value columns
        // Key column gets 1/3, Value column gets 2/3 of available width
        int keyColWidth = availableWidth / 3;
        int valueColWidth = availableWidth - keyColWidth;
        
        // Set more aggressive minimum widths to allow better shrinking
        int minKeyWidth = GetDpiScaledWidth(50); // Reduced from 80
        int minValueWidth = GetDpiScaledWidth(80); // Reduced from 120
        
        keyColWidth = Math.Max(keyColWidth, minKeyWidth);
        valueColWidth = Math.Max(valueColWidth, minValueWidth);
        
        // If minimum widths exceed available space, distribute evenly
        if (minKeyWidth + minValueWidth > availableWidth && availableWidth > GetDpiScaledWidth(60))
        {
            keyColWidth = availableWidth / 3;
            valueColWidth = availableWidth - keyColWidth;
            keyColWidth = Math.Max(GetDpiScaledWidth(20), keyColWidth);
            valueColWidth = Math.Max(GetDpiScaledWidth(30), valueColWidth);
        }
        
        if (_gvwTags.Columns.Count >= 2)
        {
            _gvwTags.Columns[0].Width = keyColWidth;  // Key column
            _gvwTags.Columns[1].Width = valueColWidth; // Value column
        }
    }

    private void ScheduleDelayedResize()
    {
        // Cancel any existing resize timer
        _resizeTimer?.Dispose();
        
        // Schedule a new resize operation after a delay
        _resizeTimer = new System.Threading.Timer(
            _ => Application.Instance.Invoke(() => 
            {
                ResizeResultsGridColumns();
                ResizeTagsGridColumns();
                UpdateSplitterPosition();
            }),
            null,
            TimeSpan.FromMilliseconds(300), // 300ms delay to avoid excessive calls during resizing
            TimeSpan.FromMilliseconds(-1) // Run only once
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
            
            // Check if the error is due to missing Azure context configuration
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
                        // Optionally retry the search automatically after configuration
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
        if (_gvwResults.SelectedItem is Resource res && res.CombinedTags != null)
        {
            foreach (var kvp in res.CombinedTags.OrderBy(k => k.Key))
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
            // Cancel any existing timer
            _quickFilter1Timer?.Dispose();
            
            // Only schedule filtering if the dropdown has a non-empty value selected
            if (_cboQuickFilter1Column.SelectedIndex > 0)
            {
                _quickFilter1Timer = new System.Threading.Timer(
                    _ => Application.Instance.Invoke(FilterResults),
                    null,
                    TimeSpan.FromMilliseconds(250),
                    TimeSpan.FromMilliseconds(-1) // Don't repeat
                );
            }
        }
        else if (filterNumber == 2)
        {
            // Cancel any existing timer
            _quickFilter2Timer?.Dispose();
            
            // Only schedule filtering if the dropdown has a non-empty value selected
            if (_cboQuickFilter2Column.SelectedIndex > 0)
            {
                _quickFilter2Timer = new System.Threading.Timer(
                    _ => Application.Instance.Invoke(FilterResults),
                    null,
                    TimeSpan.FromMilliseconds(250),
                    TimeSpan.FromMilliseconds(-1) // Don't repeat
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
            ClientSize = new Size(350, 200),
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
                new Label { Text = "A tool for querying and managing Azure resources and tags.", TextAlignment = TextAlignment.Center, Wrap = WrapMode.Word },
                new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Items =
                    {
                        new Button { Text = "OK", Command = new Command((_, _) => aboutDialog.Close()) }
                    }
                }
            }
        };

        aboutDialog.Content = content;
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
        
        // Dispose timers
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
        var currentPage = _paginatedResults.CurrentPage + 1; // Convert to 1-based
        var totalPages = _paginatedResults.TotalPages;
        var totalItems = _paginatedResults.TotalFilteredCount;
        
        // Count different entity types in filtered results
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

    private void AddToFilterQuery(bool exclude)
    {
        if (_resultsContextRow < 0 || _resultsContextColumn == null)
            return;
        if (_resultsContextRow >= _paginatedResults.DisplayedItems.Count)
            return;

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
            return;

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
            return;
        if (_resultsContextRow >= _paginatedResults.DisplayedItems.Count)
            return;

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
            return;

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

    // Helper for displaying any property in the grid
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
            const string indent = "    "; // 4 spaces for indentation
            
            var formattedLines = new List<string>();
            
            foreach (var kv in dict.OrderBy(kvp => kvp.Key))
            {
                // Format the key-value pair with potential wrapping
                var keyValueLine = FormatKeyValueWithWrapping(kv.Key, kv.Value, maxLineWidth, indent);
                formattedLines.Add(keyValueLine);
            }
            
            return string.Join("\n", formattedLines);
        }
        return value?.ToString() ?? string.Empty;
    }
    
    private static string FormatKeyValueWithWrapping(string key, string value, int maxLineWidth, string indent)
    {
        // Start with the key (we can't make it bold in plain text tooltips, but we can make it prominent)
        var keyPart = $"{key}:";
        var fullLine = $"{keyPart} {value}";
        
        // If the full line fits within the max width, return it as-is
        if (fullLine.Length <= maxLineWidth)
        {
            return fullLine;
        }
        
        var result = new List<string>();
        var remainingValue = value;
        var availableWidth = maxLineWidth - keyPart.Length - 1; // -1 for the space after colon
        
        // First line: try to fit as much of the value as possible after the key
        if (remainingValue.Length <= availableWidth)
        {
            // Value fits on the same line as the key
            result.Add($"{keyPart} {remainingValue}");
        }
        else
        {
            // Value needs to be wrapped - find the best break point
            var breakIndex = FindBestBreakIndex(remainingValue, availableWidth);
            var firstPart = remainingValue.Substring(0, breakIndex);
            remainingValue = remainingValue.Substring(breakIndex);
            
            result.Add($"{keyPart} {firstPart}");
            
            // Continue with remaining value on indented lines
            var indentedMaxWidth = maxLineWidth - indent.Length;
            while (remainingValue.Length > 0)
            {
                if (remainingValue.Length <= indentedMaxWidth)
                {
                    // Remaining value fits on one indented line
                    result.Add($"{indent}{remainingValue}");
                    break;
                }
                else
                {
                    // Need to break the remaining value further
                    var chunkBreakIndex = FindBestBreakIndex(remainingValue, indentedMaxWidth);
                    var chunk = remainingValue.Substring(0, chunkBreakIndex);
                    remainingValue = remainingValue.Substring(chunkBreakIndex);
                    
                    result.Add($"{indent}{chunk}");
                }
            }
        }
        
        return string.Join("\n", result);
    }
    
    private static int FindBestBreakIndex(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text.Length;
        }
        
        // Priority 1: Break after '/' (good for URLs and file paths)
        for (int i = Math.Min(maxLength - 1, text.Length - 1); i >= maxLength / 3; i--)
        {
            if (text[i] == '/' && i + 1 < text.Length)
            {
                return i + 1; // Include the '/' in the current line
            }
        }
        
        // Priority 2: Break before '@' (good for email addresses)
        for (int i = Math.Min(maxLength - 1, text.Length - 1); i >= maxLength / 3; i--)
        {
            if (text[i] == '@')
            {
                return i; // Don't include the '@' in the current line
            }
        }
        
        // Priority 3: Break at word boundaries (spaces)
        for (int i = Math.Min(maxLength - 1, text.Length - 1); i >= maxLength / 2; i--)
        {
            if (text[i] == ' ')
            {
                return i + 1; // Skip the space
            }
        }
        
        // Priority 4: Break after other common separators
        var separators = new char[] { '-', '_', '.', ':', ';', ',', '&', '?', '=', '|', '\\' };
        for (int i = Math.Min(maxLength - 1, text.Length - 1); i >= maxLength / 3; i--)
        {
            if (separators.Contains(text[i]) && i + 1 < text.Length)
            {
                return i + 1; // Include the separator in the current line
            }
        }
        
        // Last resort: Hard break at maxLength
        return Math.Min(maxLength, text.Length);
    }

    private void CalculateInitialTagsPanelHeight()
    {
        // Calculate the initial fixed tags panel height based on saved splitter position
        var availableHeight = GetAvailableHeightForSplitter();
        if (availableHeight > 0)
        {
            if (_settings.SplitterPosition > 0 && _settings.SplitterPosition < availableHeight)
            {
                // Use saved position if available and valid
                var tagsPanelHeight = availableHeight - _settings.SplitterPosition;
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, tagsPanelHeight);
            }
            else
            {
                // First start or invalid saved position: Set tags panel to 1/3 of available height (results panel gets 2/3)
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, (int)(availableHeight / 3.0));
            }
        }
        else
        {
            // Fallback when no height is available yet
            _fixedTagsPanelHeight = GetDpiScaledWidth(200); // Default tags panel height with DPI scaling
        }
    }

    private void UpdateSplitterPosition()
    {
        // Calculate new splitter position to maintain fixed tags panel height
        var availableHeight = GetAvailableHeightForSplitter();
        if (availableHeight <= 0) return;

        // Ensure our fixed tags panel height is reasonable for the current available height
        var maxAllowedTagsHeight = (int)(availableHeight * 0.7); // Tags panel should not exceed 70% of available height
        _fixedTagsPanelHeight = Math.Min(_fixedTagsPanelHeight, maxAllowedTagsHeight);
        _fixedTagsPanelHeight = Math.Max(_fixedTagsPanelHeight, MinTagsPanelHeight);

        // Calculate desired position to maintain fixed tags panel height
        var desiredPosition = availableHeight - _fixedTagsPanelHeight;
        
        // Ensure minimum sizes are respected
        var minPosition = MinResultsPanelHeight;
        var maxPosition = availableHeight - MinTagsPanelHeight;
        
        var newPosition = Math.Max(minPosition, Math.Min(maxPosition, desiredPosition));
        
        // Only update if position difference is significant to avoid jitter
        if (Math.Abs(_splitter.Position - newPosition) > 5)
        {
            // Set flag to prevent PositionChanged event from updating our fixed height
            _isProgrammaticSplitterUpdate = true;
            _splitter.Position = newPosition;
            _isProgrammaticSplitterUpdate = false;
            
            // Force splitter to redraw at the new position
            _splitter.Invalidate();
            
            // Also invalidate the parent layout to ensure proper redraw
            Content?.Invalidate();
        }
        
        // Update the actual fixed height based on the final position
        var actualTagsPanelHeight = availableHeight - _splitter.Position;
        if (actualTagsPanelHeight != _fixedTagsPanelHeight)
        {
            _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, actualTagsPanelHeight);
        }
    }

    private void ScheduleDelayedSplitterConstraint()
    {
        // Cancel any existing timer
        _splitterTimer?.Dispose();
        
        // Schedule constraint enforcement after a short delay to detect end of dragging
        _splitterTimer = new System.Threading.Timer(
            _ => Application.Instance.Invoke(EnforceSplitterMinimumHeights),
            null,
            TimeSpan.FromMilliseconds(300), // Wait 300ms after last position change
            TimeSpan.FromMilliseconds(-1) // Don't repeat
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
            
            // Check if current position violates minimum heights
            if (currentPosition < minPosition || currentPosition > maxPosition)
            {
                // Reset to a valid position
                var correctedPosition = Math.Max(minPosition, Math.Min(maxPosition, currentPosition));
                
                _isProgrammaticSplitterUpdate = true;
                _splitter.Position = correctedPosition;
                _isProgrammaticSplitterUpdate = false;
                
                // Force splitter to redraw at the corrected position
                _splitter.Invalidate();
                Content?.Invalidate();
                
                // Update the fixed tags panel height based on corrected position
                var tagsPanelHeight = availableHeight - correctedPosition;
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, tagsPanelHeight);
            }
        }
    }

    private int GetActualSplitterHeight()
    {
        // Get the actual height available to the splitter control
        // This avoids circular dependency in minimum height calculations
        if (_splitter != null && _splitter.Height > 0)
        {
            return _splitter.Height;
        }
        
        // Fallback calculation when splitter isn't available yet
        // Use more accurate estimation based on actual control heights
        int baseUIHeight = 320; // More accurate estimate for all non-splitter UI elements (includes margins and spacing)
        int nonSplitterUIHeight = GetDpiScaledWidth(baseUIHeight);
        var estimatedHeight = Math.Max(300, ClientSize.Height - nonSplitterUIHeight); // Ensure reasonable minimum
        
        return estimatedHeight;
    }

    private int GetAvailableHeightForSplitter()
    {
        // Return the actual height available to the splitter control
        return GetActualSplitterHeight();
    }
}
