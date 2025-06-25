// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using AzTagger.Models;
using AzTagger.Services;

namespace AzTagger.App;

public partial class MainForm : Form
{
    private void ResizeResultsGridColumns()
    {
        if (_gvwResults.Columns.Count == 0)
            return;
        
        int tolerance = GetDpiScaledSize(20);
        int actualGridWidth = _gvwResults.Width - tolerance;
        
        if (actualGridWidth <= GetDpiScaledSize(100))
            return;
        
        int availableWidth = actualGridWidth;
        
        int colCount = _gvwResults.Columns.Count;

        var columnWidths = new Dictionary<string, int>
        {
            ["EntityType"] = GetDpiScaledSize(170),
            ["Id"] = GetDpiScaledSize(120),
            ["SubscriptionName"] = GetDpiScaledSize(200),
            ["ResourceGroup"] = GetDpiScaledSize(200),
            ["ResourceName"] = GetDpiScaledSize(200),
            ["ResourceType"] = GetDpiScaledSize(240),
            ["SubscriptionId"] = GetDpiScaledSize(180),
            ["SubscriptionTags"] = GetDpiScaledSize(200),
            ["ResourceGroupTags"] = GetDpiScaledSize(200),
            ["ResourceTags"] = GetDpiScaledSize(200),
            ["CombinedTags"] = GetDpiScaledSize(200)
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
                totalPreferredWidth += GetDpiScaledSize(150);
            }
        }
        
        double scaleFactor = (double)availableWidth / totalPreferredWidth;
        
        for (int i = 0; i < colCount; i++)
        {
            var column = _gvwResults.Columns[i];
            int preferredWidth = GetDpiScaledSize(150);
            
            if (_columnPropertyMap.TryGetValue(column, out var propertyName) && 
                columnWidths.TryGetValue(propertyName, out var configuredWidth))
            {
                preferredWidth = configuredWidth;
            }
            
            int scaledWidth = (int)(preferredWidth * scaleFactor);
            int finalWidth = Math.Max(GetDpiScaledSize(40), scaledWidth);
            column.Width = finalWidth;
        }
        
        int totalMinimumWidth = colCount * GetDpiScaledSize(40);
        if (totalMinimumWidth > availableWidth && availableWidth > GetDpiScaledSize(100))
        {
            int evenDistributedWidth = availableWidth / colCount;
            for (int i = 0; i < colCount; i++)
            {
                _gvwResults.Columns[i].Width = Math.Max(GetDpiScaledSize(25), evenDistributedWidth);
            }
        }
    }

    private void ResizeTagsGridColumns()
    {
        if (_gvwTags.Columns.Count == 0)
            return;
        
        int tolerance = GetDpiScaledSize(20);
        int actualGridWidth = _gvwTags.Width - tolerance;
        
        if (actualGridWidth <= GetDpiScaledSize(80))
            return;
        
        int availableWidth = actualGridWidth;
        
        int keyColWidth = availableWidth / 3;
        int valueColWidth = availableWidth - keyColWidth;
        
        int minKeyWidth = GetDpiScaledSize(50);
        int minValueWidth = GetDpiScaledSize(80);
        
        keyColWidth = Math.Max(keyColWidth, minKeyWidth);
        valueColWidth = Math.Max(valueColWidth, minValueWidth);
        
        if (minKeyWidth + minValueWidth > availableWidth && availableWidth > GetDpiScaledSize(60))
        {
            keyColWidth = availableWidth / 3;
            valueColWidth = availableWidth - keyColWidth;
            keyColWidth = Math.Max(GetDpiScaledSize(20), keyColWidth);
            valueColWidth = Math.Max(GetDpiScaledSize(30), valueColWidth);
        }
        
        if (_gvwTags.Columns.Count >= 2)
        {
            _gvwTags.Columns[0].Width = keyColWidth;
            _gvwTags.Columns[1].Width = valueColWidth;
        }
    }

    private void UpdateSortIndicators()
    {
        foreach (var column in _gvwResults.Columns)
        {
            if (_columnPropertyMap.TryGetValue(column, out var propertyName))
            {
                // Remove any existing indicators first
                string headerText = propertyName;
                
                // If this is the sorted column, add the appropriate indicator
                if (propertyName == _sortColumn)
                {
                    string indicator = _sortAscending ? " ▲" : " ▼";
                    column.HeaderText = headerText + indicator;
                }
                else
                {
                    column.HeaderText = headerText;
                }
            }
        }
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
        UpdateSortIndicators();
        FilterResults();
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

    private static object GetPropertyValue(Resource resource, string propertyName)
    {
        var prop = typeof(Resource).GetProperty(propertyName);
        var value = prop?.GetValue(resource);
        if (value is IDictionary<string, string> tags)
        {
            return System.Text.Json.JsonSerializer.Serialize(tags.OrderBy(t => t.Key));
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

    private void InsertColumnNameIntoQuery(GridColumn column)
    {
        if (!_columnPropertyMap.TryGetValue(column, out var propertyName))
            return;
            
        string textToInsert;
        
        // Special handling for Tag columns
        if (propertyName.EndsWith("Tags"))
        {
            textToInsert = $"{propertyName}[''] =~ ''";
        }
        else
        {
            textToInsert = propertyName;
        }
        
        // Insert the text at the current cursor position
        int caretPosition = _txtSearchQuery.CaretIndex;
        string currentText = _txtSearchQuery.Text ?? string.Empty;
        
        string newText = currentText.Length == 0 
            ? textToInsert 
            : currentText.Insert(caretPosition, textToInsert);
            
        _txtSearchQuery.Text = newText;
        
        // Set the caret position after the inserted text
        _txtSearchQuery.CaretIndex = caretPosition + textToInsert.Length;
        
        // Set focus to the query text field
        _txtSearchQuery.Focus();
    }
}
