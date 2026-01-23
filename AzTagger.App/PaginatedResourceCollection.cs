// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using AzTagger.Models;

namespace AzTagger.App;

/// <summary>
/// A paginated collection for displaying large datasets efficiently in GridView.
/// Implements sliding window approach with configurable page size.
/// </summary>
public class PaginatedResourceCollection
{
    private List<Resource> _allItems = new();
    private List<Resource> _filteredItems = new();
    private readonly ObservableCollection<Resource> _displayedItems = new();
    
    private int _currentPage = 0;
    private int _pageSize = 1000;
    private int _totalFilteredCount = 0;
    
    private Func<Resource, bool> _filter1;
    private Func<Resource, bool> _filter2;
    
    public ObservableCollection<Resource> DisplayedItems => _displayedItems;
    public IReadOnlyList<Resource> FilteredItems => _filteredItems;
    public int TotalFilteredCount => _totalFilteredCount;
    public int CurrentPage => _currentPage;
    public int PageSize => _pageSize;
    public int TotalPages => (_totalFilteredCount + _pageSize - 1) / _pageSize;
    public bool HasNextPage => _currentPage < TotalPages - 1;
    public bool HasPreviousPage => _currentPage > 0;
    
    public event EventHandler FilterChanged;

    public void SetAllItems(IEnumerable<Resource> items)
    {
        _allItems.Clear();
        _allItems.AddRange(items);
        _currentPage = 0;
        ApplyFiltersAndRefresh();
    }

    public void SetPageSize(int pageSize)
    {
        if (pageSize <= 0) throw new ArgumentException("Page size must be positive", nameof(pageSize));
        _pageSize = pageSize;
        ApplyFiltersAndRefresh();
    }

    public void SetFilters(Func<Resource, bool> filter1, Func<Resource, bool> filter2)
    {
        _filter1 = filter1;
        _filter2 = filter2;
        _currentPage = 0;
        ApplyFiltersAndRefresh();
    }

    public void GoToPage(int page)
    {
        if (page < 0 || page >= TotalPages) 
        {
            return;
        }
        
        _currentPage = page;
        RefreshDisplayedItems();
    }

    public void NextPage()
    {
        if (HasNextPage)
        {
            _currentPage++;
            RefreshDisplayedItems();
        }
    }

    public void PreviousPage()
    {
        if (HasPreviousPage)
        {
            _currentPage--;
            RefreshDisplayedItems();
        }
    }

    public void SortBy(Func<Resource, object> keySelector, bool ascending = true)
    {
        if (ascending)
        {
            _allItems = _allItems.OrderBy(keySelector).ToList();
        }
        else
        {
            _allItems = _allItems.OrderByDescending(keySelector).ToList();
        }
        ApplyFiltersAndRefresh();
    }

    private void ApplyFiltersAndRefresh()
    {
        try
        {
            _filteredItems.Clear();
            IEnumerable<Resource> filtered = _allItems;
            
            if (_filter1 != null)
            {
                filtered = filtered.Where(_filter1);
            }
            
            if (_filter2 != null)
            {
                filtered = filtered.Where(_filter2);
            }
            
            _filteredItems.AddRange(filtered);
            _totalFilteredCount = _filteredItems.Count;
            
            if (_currentPage >= TotalPages && TotalPages > 0)
            {
                _currentPage = TotalPages - 1;
            }
            
            RefreshDisplayedItems();
            FilterChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            _filteredItems.Clear();
            _totalFilteredCount = 0;
            _displayedItems.Clear();
        }
    }

    private void RefreshDisplayedItems()
    {
        try
        {
            _displayedItems.Clear();
            
            if (_totalFilteredCount == 0 || _filteredItems.Count == 0) 
            {
                return;
            }
            
            int startIndex = _currentPage * _pageSize;
            int endIndex = Math.Min(startIndex + _pageSize, _totalFilteredCount);
            
            // Additional bounds checking
            if (startIndex >= _filteredItems.Count)
            {
                return;
            }
            
            endIndex = Math.Min(endIndex, _filteredItems.Count);
            
            for (int i = startIndex; i < endIndex; i++)
            {
                if (i >= 0 && i < _filteredItems.Count)
                {
                    var resource = _filteredItems[i];
                    _displayedItems.Add(resource);
                }
                else
                {
                    break;
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            _displayedItems.Clear();
        }
        catch (Exception)
        {
            _displayedItems.Clear();
        }
    }

    public Resource GetItemAt(int globalIndex)
    {
        if (globalIndex < 0 || globalIndex >= _totalFilteredCount)
            return null;
        return _filteredItems[globalIndex];
    }

    public int GetGlobalIndexOf(Resource item)
    {
        return _filteredItems.IndexOf(item);
    }

    /// <summary>
    /// Forces a refresh of the displayed items to update the UI when item properties have changed.
    /// </summary>
    public void Refresh()
    {
        RefreshDisplayedItems();
    }
}

/// <summary>
/// Helper class to create regex-based filters for common scenarios
/// </summary>
public static class ResourceFilters
{
    private static readonly Dictionary<string, Regex> _regexCache = new();
    private static readonly object _cacheLock = new object();

    public static Func<Resource, bool> CreateRegexFilter(string columnName, string filterText)
    {
        if (string.IsNullOrWhiteSpace(columnName) ||
            columnName.Equals(Constants.QuickFilterNone, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(filterText))
            return null;

        // Handle All option - search across all properties
        if (columnName == Constants.QuickFilterAll)
        {
            try
            {
                Regex regex;
                var cacheKey = $"{Constants.QuickFilterAll}:{filterText}";
                
                lock (_cacheLock)
                {
                    if (!_regexCache.TryGetValue(cacheKey, out regex))
                    {
                        regex = new Regex(filterText, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        
                        if (_regexCache.Count > 100)
                        {
                            _regexCache.Clear();
                        }
                        
                        _regexCache[cacheKey] = regex;
                    }
                }
                
                return r => regex.IsMatch(r.SubscriptionName ?? string.Empty) ||
                           regex.IsMatch(r.SubscriptionId ?? string.Empty) ||
                           regex.IsMatch(r.ResourceGroup ?? string.Empty) ||
                           regex.IsMatch(r.ResourceName ?? string.Empty) ||
                           regex.IsMatch(r.ResourceType ?? string.Empty) ||
                           FormatTags(r.SubscriptionTags).Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                           FormatTags(r.ResourceGroupTags).Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                           FormatTags(r.ResourceTags).Contains(filterText, StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        try
        {
            Regex regex;
            var cacheKey = $"{columnName}:{filterText}";
            
            lock (_cacheLock)
            {
                if (!_regexCache.TryGetValue(cacheKey, out regex))
                {
                    regex = new Regex(filterText, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    
                    if (_regexCache.Count > 100)
                    {
                        _regexCache.Clear();
                    }
                    
                    _regexCache[cacheKey] = regex;
                }
            }
            
            return columnName switch
            {
                nameof(Resource.EntityType) => r => regex.IsMatch(r.EntityType ?? string.Empty),
                nameof(Resource.Id) => r => regex.IsMatch(r.Id ?? string.Empty),
                nameof(Resource.SubscriptionName) => r => regex.IsMatch(r.SubscriptionName ?? string.Empty),
                nameof(Resource.SubscriptionId) => r => regex.IsMatch(r.SubscriptionId ?? string.Empty),
                nameof(Resource.ResourceGroup) => r => regex.IsMatch(r.ResourceGroup ?? string.Empty),
                nameof(Resource.ResourceName) => r => regex.IsMatch(r.ResourceName ?? string.Empty),
                nameof(Resource.ResourceType) => r => regex.IsMatch(r.ResourceType ?? string.Empty),
                nameof(Resource.SubscriptionTags) => r => FormatTags(r.SubscriptionTags).Contains(filterText, StringComparison.OrdinalIgnoreCase),
                nameof(Resource.ResourceGroupTags) => r => FormatTags(r.ResourceGroupTags).Contains(filterText, StringComparison.OrdinalIgnoreCase),
                nameof(Resource.ResourceTags) => r => FormatTags(r.ResourceTags).Contains(filterText, StringComparison.OrdinalIgnoreCase),
                nameof(Resource.CombinedTags) => r => FormatTags(r.CombinedTags).Contains(filterText, StringComparison.OrdinalIgnoreCase),
                _ => null
            };
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string FormatTags(IDictionary<string, string> tags)
    {
        if (tags == null || tags.Count == 0) return string.Empty;
        return string.Join(" ", tags.SelectMany(kvp => new[] { kvp.Key, kvp.Value }));
    }
}
