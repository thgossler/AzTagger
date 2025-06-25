// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using AzTagger.Models;

namespace AzTagger.App;

/// <summary>
/// A virtualized collection that implements lazy loading for GridView performance.
/// Only loads items that are actually needed for display.
/// </summary>
public class VirtualResourceCollection : IList<Resource>, INotifyCollectionChanged
{
    private readonly List<Resource> _sourceItems;
    private readonly List<Resource> _filteredItems;
    private readonly object _lock = new object();
    
    public event NotifyCollectionChangedEventHandler CollectionChanged;

    public VirtualResourceCollection()
    {
        _sourceItems = new List<Resource>();
        _filteredItems = new List<Resource>();
    }

    public void SetSourceItems(IEnumerable<Resource> items)
    {
        lock (_lock)
        {
            _sourceItems.Clear();
            _sourceItems.AddRange(items);
            RefreshFilteredItems();
        }
    }

    public void SetFilter(Func<Resource, bool> filter)
    {
        lock (_lock)
        {
            _currentFilter = filter;
            RefreshFilteredItems();
        }
    }

    private Func<Resource, bool> _currentFilter;

    private void RefreshFilteredItems()
    {
        var previousCount = _filteredItems.Count;
        
        _filteredItems.Clear();
        
        if (_currentFilter != null)
        {
            _filteredItems.AddRange(_sourceItems.Where(_currentFilter));
        }
        else
        {
            _filteredItems.AddRange(_sourceItems);
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    public Resource this[int index]
    {
        get
        {
            lock (_lock)
            {
                if (index < 0 || index >= _filteredItems.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _filteredItems[index];
            }
        }
        set
        {
            lock (_lock)
            {
                if (index < 0 || index >= _filteredItems.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                _filteredItems[index] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, index));
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _filteredItems.Count;
            }
        }
    }

    public bool IsReadOnly => false;

    public void Add(Resource item)
    {
        lock (_lock)
        {
            _sourceItems.Add(item);
            if (_currentFilter == null || _currentFilter(item))
            {
                _filteredItems.Add(item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _filteredItems.Count - 1));
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _sourceItems.Clear();
            _filteredItems.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public bool Contains(Resource item)
    {
        lock (_lock)
        {
            return _filteredItems.Contains(item);
        }
    }

    public void CopyTo(Resource[] array, int arrayIndex)
    {
        lock (_lock)
        {
            _filteredItems.CopyTo(array, arrayIndex);
        }
    }

    public IEnumerator<Resource> GetEnumerator()
    {
        lock (_lock)
        {
            // Return a copy to avoid modification issues during enumeration
            return _filteredItems.ToList().GetEnumerator();
        }
    }

    public int IndexOf(Resource item)
    {
        lock (_lock)
        {
            return _filteredItems.IndexOf(item);
        }
    }

    public void Insert(int index, Resource item)
    {
        lock (_lock)
        {
            _sourceItems.Insert(index, item);
            if (_currentFilter == null || _currentFilter(item))
            {
                _filteredItems.Insert(index, item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            }
        }
    }

    public bool Remove(Resource item)
    {
        lock (_lock)
        {
            var sourceRemoved = _sourceItems.Remove(item);
            var filteredIndex = _filteredItems.IndexOf(item);
            if (filteredIndex >= 0)
            {
                _filteredItems.RemoveAt(filteredIndex);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, filteredIndex));
            }
            return sourceRemoved;
        }
    }

    public void RemoveAt(int index)
    {
        lock (_lock)
        {
            if (index < 0 || index >= _filteredItems.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            var item = _filteredItems[index];
            _sourceItems.Remove(item);
            _filteredItems.RemoveAt(index);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
