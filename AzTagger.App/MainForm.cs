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
    private readonly ComboBox _cboTagTemplates;
    private readonly Button _btnEditTemplates;

    private readonly ComboBox _cboRecentSearches;
    private readonly ComboBox _cboSavedQueries;

    private readonly ComboBox _cboQuickFilter1Column;
    private readonly TextBox _txtQuickFilter1Text;
    private readonly ComboBox _cboQuickFilter2Column;
    private readonly TextBox _txtQuickFilter2Text;
    private readonly Label _lblResultsCount;

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

    private readonly ObservableCollection<Resource> _results = new();
    private readonly ObservableCollection<TagEntry> _tags = new();
    private List<Resource> _allResults = new();

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
        if (_settings.WindowSize.Width > 0 && _settings.WindowSize.Height > 0)
            ClientSize = new Size(_settings.WindowSize.Width, _settings.WindowSize.Height);
        else
            ClientSize = new Size(900, 600);

        if (_settings.WindowLocation.X > 0 || _settings.WindowLocation.Y > 0)
            Location = new Point(_settings.WindowLocation.X, _settings.WindowLocation.Y);
        _azureService = new AzureService(_settings);

        _tagTemplates = TagTemplatesService.Load();

        _txtSearchQuery = new TextArea { Height = 80 };

        _btnSearch = new Button { Text = "Search" };
        _btnSearch.Click += async (_, _) => await SearchAsync();

        _btnClearQuery = new Button { Text = "Clear" };
        _btnClearQuery.Click += (_, _) => _txtSearchQuery.Text = string.Empty;

        _btnCopyQuery = new Button { Text = "Copy Query" };
        _btnCopyQuery.Click += (_, _) => Clipboard.Instance.Text = BuildQuery();

        _btnSaveQuery = new Button { Text = "Save Query" };
        _btnSaveQuery.Click += (_, _) => SaveQuery();

        _cboRecentSearches = new ComboBox();
        _cboRecentSearches.SelectedIndexChanged += (_, _) => OnRecentSearchSelected();

        _cboSavedQueries = new ComboBox();
        _cboSavedQueries.SelectedIndexChanged += (_, _) => OnSavedQuerySelected();

        var configureButton = new Button { Text = "Configure Contexts" };
        configureButton.Click += (_, _) =>
        {
            var dlg = new AzureContextConfigDialog(_settings, _azureService);
            if (dlg.ShowModal(this))
            {
                SettingsService.Save(_settings);
            }
        };

        _gvwResults = new GridView { DataStore = _results, AllowMultipleSelection = true };
        var colName = new GridColumn
        {
            HeaderText = "Name",
            DataCell = new TextBoxCell { Binding = Binding.Property<Resource, string>(r => r.ResourceName) },
            CellToolTipBinding = Binding.Property<Resource, string>(r => r.ResourceName),
            Width = 200,
            Sortable = true
        };
        _columnPropertyMap[colName] = nameof(Resource.ResourceName);
        _gvwResults.Columns.Add(colName);
        var colType = new GridColumn
        {
            HeaderText = "Type",
            DataCell = new TextBoxCell { Binding = Binding.Property<Resource, string>(r => r.ResourceType) },
            CellToolTipBinding = Binding.Property<Resource, string>(r => r.ResourceType),
            Width = 180,
            Sortable = true
        };
        _columnPropertyMap[colType] = nameof(Resource.ResourceType);
        _gvwResults.Columns.Add(colType);
        var colRg = new GridColumn
        {
            HeaderText = "Resource Group",
            DataCell = new TextBoxCell { Binding = Binding.Property<Resource, string>(r => r.ResourceGroup) },
            CellToolTipBinding = Binding.Property<Resource, string>(r => r.ResourceGroup),
            Width = 180,
            Sortable = true
        };
        _columnPropertyMap[colRg] = nameof(Resource.ResourceGroup);
        _gvwResults.Columns.Add(colRg);
        var colSub = new GridColumn
        {
            HeaderText = "Subscription",
            DataCell = new TextBoxCell { Binding = Binding.Property<Resource, string>(r => r.SubscriptionName) },
            CellToolTipBinding = Binding.Property<Resource, string>(r => r.SubscriptionName),
            Width = 180,
            Sortable = true
        };
        _columnPropertyMap[colSub] = nameof(Resource.SubscriptionName);
        _gvwResults.Columns.Add(colSub);
        var colTags = new GridColumn
        {
            HeaderText = "Tags",
            DataCell = new TextBoxCell { Binding = Binding.Property<Resource, string>(r => r.CombinedTagsFormatted) },
            CellToolTipBinding = Binding.Property<Resource, string>(r => r.CombinedTagsFormatted),
            Width = 250,
            Sortable = true
        };
        _columnPropertyMap[colTags] = nameof(Resource.CombinedTags);
        _gvwResults.Columns.Add(colTags);
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
            Width = 150
        };
        var valueCol = new GridColumn
        {
            HeaderText = "Value",
            Editable = true,
            DataCell = new TextBoxCell { Binding = Binding.Property<TagEntry, string>(t => t.Value) },
            CellToolTipBinding = Binding.Property<TagEntry, string>(t => t.Value),
            Width = 250
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

        _cboTagTemplates = new ComboBox();
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

        _cboQuickFilter1Column = new ComboBox { Width = 150 };
        _cboQuickFilter2Column = new ComboBox { Width = 150 };
        var propertyNames = typeof(Resource).GetProperties().Select(p => p.Name).ToList();
        _cboQuickFilter1Column.DataStore = new List<string>(new[] { string.Empty }.Concat(propertyNames));
        _cboQuickFilter2Column.DataStore = new List<string>(new[] { string.Empty }.Concat(propertyNames));
        _cboQuickFilter1Column.SelectedIndexChanged += (_, _) => FilterResults();
        _cboQuickFilter2Column.SelectedIndexChanged += (_, _) => FilterResults();

        _txtQuickFilter1Text = new TextBox { Width = 180 };
        _txtQuickFilter2Text = new TextBox { Width = 180 };
        _txtQuickFilter1Text.TextChanged += (_, _) => FilterResults();
        _txtQuickFilter2Text.TextChanged += (_, _) => FilterResults();

        _quickFilterContextMenu = CreateQuickFilterContextMenu(_txtQuickFilter1Text);
        _txtQuickFilter1Text.MouseUp += (s, e) => { if (e.Buttons == MouseButtons.Alternate) _quickFilterContextMenu.Show(_txtQuickFilter1Text); };
        var menu2 = CreateQuickFilterContextMenu(_txtQuickFilter2Text);
        _txtQuickFilter2Text.MouseUp += (s, e) => { if (e.Buttons == MouseButtons.Alternate) menu2.Show(_txtQuickFilter2Text); };

        _lblResultsCount = new Label();

        _lnkRegExDocs = new LinkButton { Text = ".NET RegEx" };
        _lnkRegExDocs.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference",
            UseShellExecute = true
        });

        _lnkResourceGraphDocs = new LinkButton { Text = "Resource Graph" };
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

        _lnkEditSettings = new LinkButton { Text = "Edit Settings" };
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

        _lnkResetDefaults = new LinkButton { Text = "Reset Defaults" };
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
                _btnSaveQuery,
                _btnCopyQuery,
                _btnClearQuery,
                configureButton,
                null
            }
        };

        var layout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = 10 };
        layout.AddRow(new TableRow(_cboRecentSearches, _cboSavedQueries));
        layout.AddRow(new Label { Text = "Query:" });
        layout.AddRow(_txtSearchQuery);
        layout.AddRow(topRow);
        layout.AddRow(new TableRow(_cboQuickFilter1Column, _txtQuickFilter1Text, _cboQuickFilter2Column, _txtQuickFilter2Text));
        var resultsPanel = new DynamicLayout { DefaultSpacing = new Size(5, 5) };
        resultsPanel.AddRow(new TableRow(_gvwResults) { ScaleHeight = true });
        resultsPanel.AddRow(_lblResultsCount);

        var tagsPanel = new DynamicLayout { DefaultSpacing = new Size(5, 5) };
        tagsPanel.AddRow(new TableRow(_gvwTags) { ScaleHeight = true });
        tagsPanel.AddRow(new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items = { _cboTagTemplates, _btnEditTemplates, null }
        });
        tagsPanel.AddRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { _btnApplyTags, null } });

        _splitter = new Splitter
        {
            Orientation = Orientation.Vertical,
            FixedPanel = SplitterFixedPanel.None,
            Panel1 = resultsPanel,
            Panel2 = tagsPanel,
            Position = _settings.SplitterPosition
        };
        layout.AddRow(new Label { Text = "Results / Tags:" });
        layout.AddRow(new TableRow(_splitter) { ScaleHeight = true });

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
                null,
                _lblVersion
            }
        };
        layout.AddRow(linksRow);

        LoadRecentSearches();
        LoadSavedSearches();
        ReloadTagTemplates();

        _txtSearchQuery.Text = _settings.LastSearchQuery;
        _txtQuickFilter1Text.Text = _settings.LastQuickFilter1Text;
        _txtQuickFilter2Text.Text = _settings.LastQuickFilter2Text;

        Content = layout;

        Closed += (_, _) => SaveSettings();
    }

    private async Task EnsureSignedInAsync()
    {
        if (_azureService == null)
            _azureService = new AzureService(_settings);
        await _azureService.SignInAsync();
    }

    private string BuildQuery()
    {
        if (string.IsNullOrWhiteSpace(_txtSearchQuery.Text))
            return BaseQuery;

        var filter = $"| where ResourceName contains '{_txtSearchQuery.Text.Replace("'", "''")}' or ResourceGroup contains '{_txtSearchQuery.Text.Replace("'", "''")}' or SubscriptionName contains '{_txtSearchQuery.Text.Replace("'", "''")}'";
        return BaseQuery + "\n" + filter;
    }

    private async Task SearchAsync()
    {
        _btnSearch.Enabled = false;
        try
        {
            await EnsureSignedInAsync();
            var query = BuildQuery();
            var items = (await _azureService.QueryResourcesAsync(query)).ToList();
            _allResults = items;
            FilterResults();
            SaveRecentSearch(_txtSearchQuery.Text);
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Search failed");
            MessageBox.Show(this, "Search failed", MessageBoxButtons.OK, MessageBoxType.Error);
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
        return user.Mail ?? user.UserPrincipalName ?? Environment.UserName;
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

    private static object GetPropertyValue(Resource resource, string propertyName)
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
        IEnumerable<Resource> filtered = _allResults;

        if (_cboQuickFilter1Column.SelectedIndex > 0 && !string.IsNullOrWhiteSpace(_txtQuickFilter1Text.Text))
        {
            try
            {
                var regex = new Regex(_txtQuickFilter1Text.Text, RegexOptions.IgnoreCase);
                var column = _cboQuickFilter1Column.SelectedValue.ToString();
                filtered = filtered.Where(r => regex.IsMatch(GetPropertyValue(r, column)?.ToString() ?? string.Empty));
            }
            catch
            {
                // ignore invalid regex
            }
        }

        if (_cboQuickFilter2Column.SelectedIndex > 0 && !string.IsNullOrWhiteSpace(_txtQuickFilter2Text.Text))
        {
            try
            {
                var regex = new Regex(_txtQuickFilter2Text.Text, RegexOptions.IgnoreCase);
                var column = _cboQuickFilter2Column.SelectedValue.ToString();
                filtered = filtered.Where(r => regex.IsMatch(GetPropertyValue(r, column)?.ToString() ?? string.Empty));
            }
            catch
            {
            }
        }

        _results.Clear();
        foreach (var r in filtered)
            _results.Add(r);

        UpdateResultsCountLabel();
    }

    private void UpdateResultsCountLabel()
    {
        _lblResultsCount.Text = $"Items: {_results.Count}";
    }

    private void LoadRecentSearches()
    {
        var items = new List<string> { "Recent Searches" };
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

    private void AddToFilterQuery(bool exclude)
    {
        if (_resultsContextRow < 0 || _resultsContextColumn == null)
            return;
        if (_resultsContextRow >= _results.Count)
            return;

        var item = _results[_resultsContextRow];
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
        if (_resultsContextRow >= _results.Count)
            return;

        var item = _results[_resultsContextRow];
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
                var local = _results.FirstOrDefault(r => r.Id == up.Id);
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

    private void SaveSettings()
    {
        _settings.WindowSize = new Settings.WinSize(ClientSize.Width, ClientSize.Height);
        _settings.WindowLocation = new Settings.WinLocation(Location.X, Location.Y);
        _settings.LastSearchQuery = _txtSearchQuery.Text;
        _settings.LastQuickFilter1Text = _txtQuickFilter1Text.Text;
        _settings.LastQuickFilter2Text = _txtQuickFilter2Text.Text;
        _settings.SplitterPosition = _splitter.Position;
        SettingsService.Save(_settings);
    }
}
