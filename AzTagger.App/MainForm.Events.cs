// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Eto.Forms;
using AzTagger.Models;
using AzTagger.Services;

namespace AzTagger.App;

public partial class MainForm : Form
{
    private void ShowAboutDialog()
    {
        var aboutDialog = new AboutDialog();
        aboutDialog.ShowModal(this);
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
            Clipboard.Instance.Text = System.Text.Json.JsonSerializer.Serialize(res.CombinedTags.OrderBy(t => t.Key), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
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
}
