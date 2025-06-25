// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using AzTagger.Models;
using AzTagger.Services;

namespace AzTagger.App;

public partial class MainForm : Form
{
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

        var menuBar = new MenuBar
        {
            Items = { fileMenu }
        };

        menuBar.QuitItem = exitCommand;
        menuBar.AboutItem = aboutItem;

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

    private void CreateSearchControls()
    {
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
                _lblQueryMode!.Text = "--> KQL full expression (not supported)";
                _queryMode = QueryMode.KqlFull;
            }
            else if (normalizedQuery.StartsWith("|"))
            {
                _lblQueryMode!.Text = "--> KQL filter-only expression";
                _queryMode = QueryMode.KqlFilter;
            }
            else if (normalizedQuery.Length > 0)
            {
                _lblQueryMode!.Text = "--> regex (applied to SubscriptionName, ResourceGroup and ResourceName)";
                _queryMode = QueryMode.Regex;
            }
            else
            {
                _lblQueryMode!.Text = string.Empty;
                _queryMode = QueryMode.Regex;
            }
        });

        _btnSearch.Click += async (_, _) => await SearchAsync();
        _btnClearQuery.Click += (_, _) => _txtSearchQuery.Text = string.Empty;
        _btnCopyQuery.Click += (_, _) => Clipboard.Instance.Text = BuildQuery();
        _btnSaveQuery.Click += (_, _) => SaveQuery();
        _cboRecentSearches.SelectedIndexChanged += (_, _) => OnRecentSearchSelected();
        _cboSavedQueries.SelectedIndexChanged += (_, _) => OnSavedQuerySelected();
    }

    private void CreateResultsGrid()
    {
        _gvwResults.CellDoubleClick += (_, _) => OpenSelectedResourceInPortal();
        _gvwResults.SelectionChanged += (_, _) => LoadTagsForSelection();

        // Add handler for detecting double-clicks on column headers and managing sort timing
        DateTime _lastHeaderClickTime = DateTime.MinValue;
        GridColumn _lastHeaderColumn = null;
        GridColumn _pendingSortColumn = null;
        System.Threading.Timer _sortTimer = null;
        bool _doubleClickDetected = false;
        const int doubleClickTimeThreshold = 250; // milliseconds
        
        // Handle column header clicks with delayed sorting to avoid sorting on double-click
        _gvwResults.ColumnHeaderClick += (_, e) =>
        {
            // If a double-click was already detected in MouseDown, ignore this ColumnHeaderClick
            if (_doubleClickDetected)
            {
                _doubleClickDetected = false; // Reset for next time
                return;
            }
            
            var now = DateTime.Now;
            _pendingSortColumn = e.Column;
            
            // Cancel any pending sort timer
            _sortTimer?.Dispose();
            
            // Update tracking variables for this click
            _lastHeaderClickTime = now;
            _lastHeaderColumn = e.Column;
            
            // Start a timer to sort after the double-click threshold
            _sortTimer = new System.Threading.Timer(_ =>
            {
                if (_pendingSortColumn == e.Column && !_doubleClickDetected)
                {
                    Application.Instance.AsyncInvoke(() => SortResults(e.Column));
                }
                _sortTimer?.Dispose();
                _sortTimer = null;
            }, null, doubleClickTimeThreshold + 20, System.Threading.Timeout.Infinite);
        };
        
        _gvwResults.MouseDown += (s, e) => 
        {
            // Try to get the cell at the click location
            var cell = _gvwResults.GetCellAt(e.Location);
            
            // If row index is -1, it's likely a header click
            if (cell?.RowIndex == -1 && cell?.Column != null)
            {
                var now = DateTime.Now;
                // Check if it's a double click (same column, within time threshold)
                if (_lastHeaderColumn == cell.Column && 
                    (now - _lastHeaderClickTime).TotalMilliseconds < doubleClickTimeThreshold)
                {
                    // Double-click detected
                    _doubleClickDetected = true;
                    
                    // Insert column name into query
                    InsertColumnNameIntoQuery(cell.Column);
                    
                    // Cancel any pending sort operation
                    _pendingSortColumn = null;
                    _sortTimer?.Dispose();
                    _sortTimer = null;
                    
                    // Reset tracking vars to prevent triple-click detection
                    _lastHeaderClickTime = DateTime.MinValue;
                    _lastHeaderColumn = null;
                }
                // Note: For single clicks, we let ColumnHeaderClick handle the tracking variables
            }
            else if (e.Buttons == MouseButtons.Alternate && cell != null && cell.RowIndex >= 0 && cell.Column != null)
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
        };

        var resourceProps = typeof(Resource).GetProperties()
            .Where(p => p.Name != nameof(Resource.CombinedTagsFormatted));
        foreach (var prop in resourceProps)
        {
            var cell = new TextBoxCell { Binding = Binding.Delegate<Resource, string>(r => FormatPropertyForGrid(r, prop.Name)) };
            
            GridColumn col = new GridColumn
            {
                HeaderText = prop.Name,
                DataCell = cell,
                CellToolTipBinding = Binding.Delegate<Resource, string>(r => FormatPropertyForTooltip(r, prop.Name)),
                Sortable = true,
                Width = GetDpiScaledSize(150)
            };
            _columnPropertyMap[col] = prop.Name;
            _gvwResults.Columns.Add(col);
        }
    }

    private void CreateTagsGrid()
    {
        var keyCol = new GridColumn
        {
            HeaderText = "Key",
            Editable = true,
            DataCell = new TextBoxCell { Binding = Binding.Property<TagEntry, string>(t => t.Key) },
            CellToolTipBinding = Binding.Property<TagEntry, string>(t => t.Key),
            Width = GetDpiScaledSize(150)
        };
        var valueCol = new GridColumn
        {
            HeaderText = "Value",
            Editable = true,
            DataCell = new TextBoxCell { Binding = Binding.Property<TagEntry, string>(t => t.Value) },
            CellToolTipBinding = Binding.Property<TagEntry, string>(t => t.Value),
            Width = GetDpiScaledSize(250)
        };
        _gvwTags.Columns.Add(keyCol);
        _gvwTags.Columns.Add(valueCol);

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
    }

    private void CreateContextMenus()
    {
        // Tags context menu
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

        // Results context menu
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
    }

    private void CreatePaginationControls()
    {
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
    }

    private void CreateQuickFilterControls()
    {
        var propertyNames = typeof(Resource).GetProperties()
            .Where(p => p.Name != nameof(Resource.CombinedTagsFormatted))
            .Select(p => p.Name).ToList();
        _cboQuickFilter1Column.DataStore = new List<string>(new[] { Constants.QuickFilterNone, Constants.QuickFilterAll }.Concat(propertyNames));
        _cboQuickFilter1Column.SelectedValue = Constants.QuickFilterNone;
        _cboQuickFilter2Column.DataStore = new List<string>(new[] { Constants.QuickFilterNone, Constants.QuickFilterAll }.Concat(propertyNames));
        _cboQuickFilter2Column.SelectedValue = Constants.QuickFilterNone;
        _cboQuickFilter1Column.SelectedIndexChanged += (_, _) => FilterResults();
        _cboQuickFilter2Column.SelectedIndexChanged += (_, _) => FilterResults();

        _txtQuickFilter1Text.TextChanged += (_, _) => ScheduleDelayedFilter(1);
        _txtQuickFilter2Text.TextChanged += (_, _) => ScheduleDelayedFilter(2);
        
        // Add button click handlers for exclude functionality
        _btnQuickFilter1Exclude.Click += (_, _) => ToggleIncludeExcludeFilter(_txtQuickFilter1Text);
        _btnQuickFilter2Exclude.Click += (_, _) => ToggleIncludeExcludeFilter(_txtQuickFilter2Text);
    }

    private void ToggleIncludeExcludeFilter(TextBox textBox)
    {
        var currentText = textBox.Text?.Trim();
        if (string.IsNullOrEmpty(currentText))
        {
            MessageBox.Show(this, "No text to convert to exclude pattern.", "Info", MessageBoxButtons.OK, MessageBoxType.Information);
            return;
        }

        // Check if it's already an exclusion pattern and extract the original text
        if (currentText.StartsWith("^(?!.*") && currentText.EndsWith(").*$"))
        {
            // Extract the escaped text from the pattern: ^(?!.*escaped_text).*$
            var startIndex = "^(?!.*".Length;
            var endIndex = currentText.Length - ").*$".Length;
            if (endIndex > startIndex)
            {
                var extractedText = currentText.Substring(startIndex, endIndex - startIndex);
                // Unescape the text to get back to the original
                var originalText = Regex.Unescape(extractedText);
                textBox.Text = originalText;
            }
            return;
        }

        // Escape special regex characters in the text
        var escapedText = Regex.Escape(currentText);
        
        // Create negative lookahead pattern to exclude the text
        var excludePattern = $"^(?!.*{escapedText}).*$";
        
        textBox.Text = excludePattern;
    }

    private void CreateTagTemplateControls()
    {
        _cboTagTemplates.SelectedIndexChanged += async (_, _) => await OnTagTemplateSelectedAsync();

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

        _btnApplyTags.Click += async (_, _) => await ApplyTagsAsync();
    }

    private void CreateLinkButtons()
    {
        _lnkRegExDocs.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference",
            UseShellExecute = true
        });

        _lnkResourceGraphDocs.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://learn.microsoft.com/en-us/azure/governance/resource-graph/concepts/query-language",
            UseShellExecute = true
        });

        _lnkGitHub.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/thgossler/AzTagger",
            UseShellExecute = true
        });

        _lnkDonation.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.paypal.com/donate/?hosted_button_id=JVG7PFJ8DMW7J",
            UseShellExecute = true
        });

        _lnkEditSettings.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = SettingsService.SettingsFilePath,
            UseShellExecute = true
        });

        _lnkShowErrorLog.Click += (_, _) =>
        {
            var file = LoggingService.GetLatestLogFile();
            if (!string.IsNullOrEmpty(file))
                Process.Start(new ProcessStartInfo { FileName = file, UseShellExecute = true });
        };

        _lnkResetDefaults.Click += (_, _) =>
        {
            _settings.ResetToWindowDefaults();
            ClientSize = new Size(_settings.WindowSize.Width, _settings.WindowSize.Height);
            Location = new Point(_settings.WindowLocation.X, _settings.WindowLocation.Y);
            _splitter!.Position = _settings.SplitterPosition;
            Invalidate();
        };
    }

    private void SetupIconAndWindowProperties()
    {
        // Set application icon
        try
        {
            Icon icon = null;
            
            if (Platform.IsWpf)
            {
                // Try embedded resource first
                try
                {
                    icon = Icon.FromResource("Resources.icon.ico");
                }
                catch
                {
                    // Fallback to file system
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var iconPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "images", "icon.ico"));
                    if (File.Exists(iconPath))
                    {
                        using var stream = new FileStream(iconPath, FileMode.Open, FileAccess.Read);
                        icon = new Icon(stream);
                    }
                }
            }
            else if (Platform.IsMac)
            {
                // Try embedded resource first for Mac
                try
                {
                    icon = Icon.FromResource("Resources.icon.icns");
                }
                catch
                {
                    // Fallback to PNG for Mac
                    try
                    {
                        icon = Icon.FromResource("Resources.icon.png");
                    }
                    catch
                    {
                        // Fallback to copied file
                        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
                        if (File.Exists(iconPath))
                        {
                            icon = new Icon(iconPath);
                        }
                    }
                }
            }
            else
            {
                // GTK/Linux - use PNG
                try
                {
                    icon = Icon.FromResource("Resources.icon.png");
                }
                catch
                {
                    // Fallback to copied file
                    var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
                    if (File.Exists(iconPath))
                    {
                        icon = new Icon(iconPath);
                    }
                    else
                    {
                        // Final fallback: try to find the icon in the images directory
                        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        var fallbackPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "images", "icon.png"));
                        if (File.Exists(fallbackPath))
                        {
                            icon = new Icon(fallbackPath);
                        }
                    }
                }
            }
            
            if (icon != null)
            {
                Icon = icon;
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the application
            System.Diagnostics.Debug.WriteLine($"Failed to load application icon: {ex.Message}");
        }
        
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
    }

    private void SetupFormEvents()
    {
        Closing += (_, e) => 
        {
            if (!_isClosing)
            {
                _isClosing = true;
                SaveSettings();
                LoggingService.CloseAndFlush();
            }
        };
        
        Closed += (_, _) => 
        {
            if (!_isClosing)
            {
                _isClosing = true;
                LoggingService.CloseAndFlush();
            }
            
            // On macOS, explicitly quit the application when the main form closes
            // This ensures the application doesn't stay running in the dock
            if (Application.Instance.Platform.ID.StartsWith("Mac", StringComparison.OrdinalIgnoreCase))
            {
                Application.Instance.Quit();
            }
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
                UpdateSortIndicators();
            });

            // Ensure the search query input always has focus on program start
            _txtSearchQuery.Focus();
        };
        
        SizeChanged += (_, _) => 
        {
            ScheduleDelayedResize();
        };
    }

    private int GetDpiScaledSize(int baseWidth)
    {
        try
        {
            var scale = Screen.LogicalPixelSize;
            return (int)(baseWidth * scale);
        }
        catch
        {
            // Fallback to base width if screen information is not available
            return baseWidth;
        }
    }

    private void UpdateTitle()
    {
        var baseName = "AzTagger";
        if (!string.IsNullOrEmpty(_settings.SelectedAzureContext))
        {
            Title = $"{baseName} - {_settings.SelectedAzureContext}";
        }
        else
        {
            Title = baseName;
        }
    }

    private void ExitApplication()
    {
        if (!_isClosing)
        {
            _isClosing = true;
            SaveSettings();
            LoggingService.CloseAndFlush();
        }
        
        // Close the window first
        Close();
        
        // On macOS, ensure the application quits completely
        if (Application.Instance.Platform.ID == "Mac")
        {
            Application.Instance.Quit();
        }
    }
}
