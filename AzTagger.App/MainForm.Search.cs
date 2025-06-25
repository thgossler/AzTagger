// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Eto.Forms;
using AzTagger.Models;
using AzTagger.Services;

namespace AzTagger.App;

public partial class MainForm : Form
{
    private enum QueryMode
    {
        KqlFilter,
        Regex,
        KqlFull
    }
    
    private QueryMode _queryMode = QueryMode.Regex;

    private async Task EnsureSignedInAsync()
    {
        if (_azureService == null)
            _azureService = new AzureService(_settings);
        await _azureService.SignInAsync();
    }

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
        _searchProgress.Visible = true;
        try
        {
            await EnsureSignedInAsync();
            var query = BuildQuery();
            var items = (await _azureService.QueryResourcesAsync(query)).ToList();
            _allResults = items;
            _paginatedResults.SetAllItems(_allResults);
            UpdatePaginationControls();
            UpdateSortIndicators();
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
                        UpdateTitle(); // Update title when Azure context changes
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
            _searchProgress.Visible = false;
        }
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

    private void LoadRecentSearches()
    {
        var items = new System.Collections.Generic.List<string> { "Recent Queries" };
        items.AddRange(_settings.RecentSearches);
        _cboRecentSearches.DataStore = items;
        _cboRecentSearches.SelectedIndex = 0;
    }

    private void LoadSavedSearches()
    {
        var items = new System.Collections.Generic.List<string> { "Saved Queries" };
        items.AddRange(_settings.SavedSearches.Select(s => s.Name));
        _cboSavedQueries.DataStore = items;
        _cboSavedQueries.SelectedIndex = 0;
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
}
