// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Graph;
using Eto.Forms;
using AzTagger.Models;
using AzTagger.Services;

namespace AzTagger.App;

public partial class MainForm : Form
{
    private void LoadTagsForSelection()
    {
        _tags.Clear();
        
        var selectedResources = _gvwResults.SelectedItems.Cast<Resource>().ToList();
        
        if (selectedResources.Count == 0)
        {
            return;
        }

        if (selectedResources.Count == 1)
        {
            // Single selection - show all tags
            var res = selectedResources[0];
            
            if (res.CombinedTags != null)
            {
                foreach (var kvp in res.CombinedTags.OrderBy(k => k.Key))
                {
                    _tags.Add(new TagEntry { Key = kvp.Key, Value = kvp.Value });
                }
            }
        }
        else
        {
            // Multiple selection - show only common tags with same values
            var commonTags = new Dictionary<string, string>();
            
            // Start with tags from the first resource
            var firstResource = selectedResources[0];
            
            if (firstResource.CombinedTags != null)
            {
                foreach (var kvp in firstResource.CombinedTags)
                {
                    commonTags[kvp.Key] = kvp.Value;
                }
            }
            
            // Remove tags that don't exist in all other resources or have different values
            for (int i = 1; i < selectedResources.Count; i++)
            {
                var resource = selectedResources[i];
                
                if (resource.CombinedTags == null)
                {
                    commonTags.Clear();
                    break;
                }
                
                var tagsToRemove = new List<string>();
                foreach (var commonTag in commonTags)
                {
                    if (!resource.CombinedTags.TryGetValue(commonTag.Key, out var value))
                    {
                        tagsToRemove.Add(commonTag.Key);
                    }
                    else if (value != commonTag.Value)
                    {
                        tagsToRemove.Add(commonTag.Key);
                    }
                }
                
                foreach (var tagKey in tagsToRemove)
                {
                    commonTags.Remove(tagKey);
                }
            }
            
            // Add common tags to the display
            foreach (var kvp in commonTags.OrderBy(k => k.Key))
            {
                _tags.Add(new TagEntry { Key = kvp.Key, Value = kvp.Value });
            }
        }
    }

    private async Task ApplyTagsAsync()
    {
        if (!_gvwResults.SelectedItems.Cast<object>().Any())
            return;
        _applyTagsProgress.Visible = true;
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
        finally
        {
            _applyTagsProgress.Visible = false;
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

    private async Task RefreshTagsAsync()
    {
        var selected = _gvwResults.SelectedItems.Cast<Resource>().ToList();
        if (selected.Count == 0)
            return;
        _resultsRefreshProgress.Visible = true;
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
            _paginatedResults.Refresh();
            LoadTagsForSelection();
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Failed to refresh tags");
            MessageBox.Show(this, "Failed to refresh tags", MessageBoxButtons.OK, MessageBoxType.Error);
        }
        finally
        {
            _resultsRefreshProgress.Visible = false;
        }
    }

    private void AddTagToFilterQuery(bool exclude)
    {
        if (_tagsContextRow < 0 || _tagsContextRow >= _tags.Count)
        {
            return;
        }

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

    private void CopyTagContextCellValue()
    {
        if (_tagsContextRow < 0 || _tagsContextRow >= _tags.Count || _tagsContextColumn == null)
        {
            LoggingService.LogError(new InvalidOperationException(), $"CopyTagContextCellValue: Invalid context - RowIndex: {_tagsContextRow}, Tags.Count: {_tags.Count}, Column: {_tagsContextColumn != null}");
            return;
        }

        var tag = _tags[_tagsContextRow];
        var value = _tagsContextColumn.HeaderText.Contains("Key") ? tag.Key : tag.Value;
        Clipboard.Instance.Text = value ?? string.Empty;
    }
}
