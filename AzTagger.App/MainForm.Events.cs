// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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

    private void ExportResults()
    {
        var items = _paginatedResults.FilteredItems;
        if (items == null || items.Count == 0)
        {
            MessageBox.Show(this, "No results to export.", "Info", MessageBoxButtons.OK, MessageBoxType.Information);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Export Results",
            Filters =
            {
                new FileFilter("JSON Files", ".json"),
                new FileFilter("CSV Files", ".csv")
            }
        };

        if (!string.IsNullOrEmpty(_settings.LastExportFolder) && Directory.Exists(_settings.LastExportFolder))
        {
            saveDialog.Directory = new Uri(_settings.LastExportFolder);
        }
        else
        {
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (Directory.Exists(homeFolder))
            {
                saveDialog.Directory = new Uri(homeFolder);
            }
        }

        if (saveDialog.ShowDialog(this) != DialogResult.Ok)
            return;

        var filePath = saveDialog.FileName;
        _settings.LastExportFolder = Path.GetDirectoryName(filePath);
        SettingsService.Save(_settings);

        try
        {
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            if (extension == ".json")
            {
                ExportAsJson(filePath, items);
            }
            else if (extension == ".csv")
            {
                ExportAsCsv(filePath, items);
            }
            else
            {
                // Determine from the selected filter
                var filterIndex = saveDialog.CurrentFilterIndex;
                if (filterIndex == 1)
                    ExportAsCsv(filePath, items);
                else
                    ExportAsJson(filePath, items);
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Failed to export results");
            MessageBox.Show(this, $"Failed to export results: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }

    private static void ExportAsJson(string filePath, IReadOnlyList<Resource> items)
    {
        var exportData = items.Select(r => new Dictionary<string, object>
        {
            ["EntityType"] = r.EntityType ?? "",
            ["Id"] = r.Id ?? "",
            ["SubscriptionName"] = r.SubscriptionName ?? "",
            ["SubscriptionId"] = r.SubscriptionId ?? "",
            ["ResourceGroup"] = r.ResourceGroup ?? "",
            ["ResourceName"] = r.ResourceName ?? "",
            ["ResourceType"] = r.ResourceType ?? "",
            ["SubscriptionTags"] = r.SubscriptionTags ?? new Dictionary<string, string>(),
            ["ResourceGroupTags"] = r.ResourceGroupTags ?? new Dictionary<string, string>(),
            ["ResourceTags"] = r.ResourceTags ?? new Dictionary<string, string>(),
            ["CombinedTags"] = r.CombinedTags ?? new Dictionary<string, string>()
        }).ToList();

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    private static void ExportAsCsv(string filePath, IReadOnlyList<Resource> items)
    {
        var columns = new[] { "EntityType", "Id", "SubscriptionName", "SubscriptionId", "ResourceGroup", "ResourceName", "ResourceType", "SubscriptionTags", "ResourceGroupTags", "ResourceTags", "CombinedTags" };
        var tagColumns = new HashSet<string> { "SubscriptionTags", "ResourceGroupTags", "ResourceTags", "CombinedTags" };

        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(",", columns));

        // Rows
        foreach (var r in items)
        {
            var values = new List<string>();
            foreach (var col in columns)
            {
                string value;
                if (tagColumns.Contains(col))
                {
                    var tags = col switch
                    {
                        "SubscriptionTags" => r.SubscriptionTags,
                        "ResourceGroupTags" => r.ResourceGroupTags,
                        "ResourceTags" => r.ResourceTags,
                        "CombinedTags" => r.CombinedTags,
                        _ => null
                    };
                    value = tags != null && tags.Count > 0
                        ? JsonSerializer.Serialize(tags)
                        : "";
                }
                else
                {
                    value = col switch
                    {
                        "EntityType" => r.EntityType,
                        "Id" => r.Id,
                        "SubscriptionName" => r.SubscriptionName,
                        "SubscriptionId" => r.SubscriptionId,
                        "ResourceGroup" => r.ResourceGroup,
                        "ResourceName" => r.ResourceName,
                        "ResourceType" => r.ResourceType,
                        _ => ""
                    } ?? "";
                }
                values.Add(CsvEscape(value));
            }
            sb.AppendLine(string.Join(",", values));
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
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
