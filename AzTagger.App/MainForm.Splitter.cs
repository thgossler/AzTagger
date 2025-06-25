// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using Eto.Forms;
using AzTagger.Models;
using AzTagger.Services;

namespace AzTagger.App;

public partial class MainForm : Form
{
    private void CalculateInitialTagsPanelHeight()
    {
        var availableHeight = GetAvailableHeightForSplitter();
        if (availableHeight > 0)
        {
            if (_settings.SplitterPosition > 0 && _settings.SplitterPosition < availableHeight)
            {
                var tagsPanelHeight = availableHeight - _settings.SplitterPosition;
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, tagsPanelHeight);
            }
            else
            {
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, (int)(availableHeight / 3.0));
            }
        }
        else
        {
            _fixedTagsPanelHeight = GetDpiScaledSize(200);
        }
    }

    private void UpdateSplitterPosition()
    {
        var availableHeight = GetAvailableHeightForSplitter();
        if (availableHeight <= 0) return;

        var maxAllowedTagsHeight = (int)(availableHeight * 0.7);
        _fixedTagsPanelHeight = Math.Min(_fixedTagsPanelHeight, maxAllowedTagsHeight);
        _fixedTagsPanelHeight = Math.Max(_fixedTagsPanelHeight, MinTagsPanelHeight);

        var desiredPosition = availableHeight - _fixedTagsPanelHeight;
        
        var minPosition = MinResultsPanelHeight;
        var maxPosition = availableHeight - MinTagsPanelHeight;
        
        var newPosition = Math.Max(minPosition, Math.Min(maxPosition, desiredPosition));
        
        if (Math.Abs(_splitter.Position - newPosition) > 5)
        {
            _isProgrammaticSplitterUpdate = true;
            _splitter.Position = newPosition;
            _isProgrammaticSplitterUpdate = false;
            
            // Force a complete visual refresh of the splitter
            ForceRefreshSplitter();
        }
        
        var actualTagsPanelHeight = availableHeight - _splitter.Position;
        if (actualTagsPanelHeight != _fixedTagsPanelHeight)
        {
            _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, actualTagsPanelHeight);
        }
    }

    private void ScheduleDelayedSplitterConstraint()
    {
        _splitterTimer?.Dispose();
        
        _splitterTimer = new System.Threading.Timer(
            _ => Application.Instance.Invoke(EnforceSplitterMinimumHeights),
            null,
            TimeSpan.FromMilliseconds(300),
            TimeSpan.FromMilliseconds(-1) 
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
            
            if (currentPosition < minPosition || currentPosition > maxPosition)
            {
                var correctedPosition = Math.Max(minPosition, Math.Min(maxPosition, currentPosition));
                
                _isProgrammaticSplitterUpdate = true;
                _splitter.Position = correctedPosition;
                _isProgrammaticSplitterUpdate = false;
                
                // Force a complete visual refresh of the splitter
                ForceRefreshSplitter();
                
                var tagsPanelHeight = availableHeight - correctedPosition;
                _fixedTagsPanelHeight = Math.Max(MinTagsPanelHeight, tagsPanelHeight);
            }
        }
    }

    private int GetActualSplitterHeight()
    {
        if (_splitter != null && _splitter.Height > 0)
        {
            return _splitter.Height;
        }
        
        int baseUIHeight = 320;
        int nonSplitterUIHeight = GetDpiScaledSize(baseUIHeight);
        var estimatedHeight = Math.Max(300, ClientSize.Height - nonSplitterUIHeight);
        
        return estimatedHeight;
    }

    private int GetAvailableHeightForSplitter()
    {
        return GetActualSplitterHeight();
    }

    private void ForceRefreshSplitter()
    {
        try
        {
            // Multiple approaches to ensure the splitter visual is updated
            
            //  1. Invalidate the splitter and its panels
            _splitter.Invalidate();
            _splitter.Panel1?.Invalidate();
            _splitter.Panel2?.Invalidate();
            
            // 2. Suspend and resume layout to force a complete redraw
            _splitter.SuspendLayout();
            _splitter.ResumeLayout();
            
            // 3. Invalidate the main content
            Content?.Invalidate();
            
            // 4. Force a refresh of the entire form if needed
            Invalidate();
            
            // 5. For some platforms, a slight delay and second invalidation helps
            Application.Instance.AsyncInvoke(() =>
            {
                _splitter.Invalidate();
                Content?.Invalidate();
            });
        }
        catch (Exception ex)
        {
            // Log but don't fail on refresh issues
            LoggingService.LogError(ex, "Failed to refresh splitter visual");
        }
    }

    private void ScheduleDelayedResize()
    {
        _resizeTimer?.Dispose();
        
        _resizeTimer = new System.Threading.Timer(
            _ => Application.Instance.Invoke(() => 
            {
                ResizeResultsGridColumns();
                ResizeTagsGridColumns();
                UpdateSplitterPosition();
            }),
            null,
            TimeSpan.FromMilliseconds(300),
            TimeSpan.FromMilliseconds(-1)
        );
    }
}
