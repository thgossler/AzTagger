using Eto.Forms;
using Eto.Drawing;
using AzTagger.Services;
using AzTagger.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.ResourceManager.Resources;

namespace AzTagger.App;

public class AzureContextConfigDialog : Dialog<bool>
{
    private readonly ObservableCollection<AzureContext> _contexts;
    private readonly GridView _grid;
    private readonly AzureService _azureService;
    private ContextMenu _contextMenu;
    private bool _shouldKeepMenuOpen = false;
    private Point _menuPosition;

    public AzureContextConfigDialog(Settings settings, AzureService azureService)
    {
        Title = "Azure Contexts";
        ClientSize = new Size(640, 280);
        MinimumSize = new Size(500, 260);
        Padding = 10;
        Resizable = true;

        _azureService = azureService;
        _contexts = new ObservableCollection<AzureContext>(settings.AzureContexts);

        _grid = new GridView
        {
            DataStore = _contexts,
            Size = new Size(-1, 120) // Height for exactly 3 rows
        };

        // Initialize context menu
        InitializeContextMenu();

        _grid.MouseDown += Grid_MouseDown;
        _grid.MouseUp += Grid_MouseUp;
        
        // Track when the window gets focus to reopen context menu if needed
        this.GotFocus += Dialog_GotFocus;

        var nameColumn = new GridColumn
        {
            HeaderText = "Name",
            Editable = true,
            DataCell = new TextBoxCell { Binding = Binding.Property<AzureContext, string>(x => x.Name) },
            Width = 150
        };

        var envNames = azureService.GetAzureEnvironmentNames();
        var envColumn = new GridColumn
        {
            HeaderText = "Environment",
            Editable = true,
            DataCell = new ComboBoxCell
            {
                DataStore = envNames,
                Binding = Binding.Property<AzureContext, object>(x => x.AzureEnvironmentName)
            },
            Width = 150
        };

        var tenantColumn = new GridColumn
        {
            HeaderText = "Tenant Id",
            Editable = true,
            DataCell = new TextBoxCell { Binding = Binding.Property<AzureContext, string>(x => x.TenantId) },
            Width = 290
        };

        _grid.Columns.Add(nameColumn);
        _grid.Columns.Add(envColumn);
        _grid.Columns.Add(tenantColumn);

        var addButton = new Button 
        { 
            Text = "Add",
            Size = new Size(80, 30)
        };
        addButton.Click += (_, _) => _contexts.Add(new AzureContext());

        var removeButton = new Button 
        { 
            Text = "Remove",
            Size = new Size(80, 30)
        };
        removeButton.Click += (_, _) =>
        {
            if (_grid.SelectedItem is AzureContext ctx)
                _contexts.Remove(ctx);
        };

        var okButton = new Button 
        { 
            Text = "OK",
            Size = new Size(80, 30)
        };
        okButton.Click += (_, _) =>
        {
            settings.AzureContexts = new List<AzureContext>(_contexts);
            settings.SelectedAzureContext = settings.AzureContexts.FirstOrDefault()?.Name ?? string.Empty;
            Close(true);
        };

        var cancelButton = new Button 
        { 
            Text = "Cancel",
            Size = new Size(80, 30)
        };
        cancelButton.Click += (_, _) => Close(false);

        DefaultButton = okButton;
        AbortButton = cancelButton;

        var mainLayout = new TableLayout
        {
            Padding = new Padding(10),
            Spacing = new Size(5, 5)
        };

        var buttonLayout = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items = { addButton, removeButton }
        };

        var bottomButtonLayout = new TableLayout();
        bottomButtonLayout.Rows.Add(new TableRow(
            new TableCell { ScaleWidth = true },
            new TableCell(okButton, true) { ScaleWidth = false },
            new TableCell(new Panel { Width = 10 }, true) { ScaleWidth = false },
            new TableCell(cancelButton, true) { ScaleWidth = false }
        ));

        mainLayout.Rows.Add(new TableRow(buttonLayout) { ScaleHeight = false });
        mainLayout.Rows.Add(new TableRow(_grid) { ScaleHeight = true });
        mainLayout.Rows.Add(new TableRow(bottomButtonLayout) { ScaleHeight = false });

        Content = mainLayout;
    }

    private void InitializeContextMenu()
    {
        _contextMenu = new ContextMenu();
        
        var removeItem = new ButtonMenuItem { Text = "Remove Context" };
        removeItem.Click += (_, _) => 
        {
            RemoveSelectedContext();
            _shouldKeepMenuOpen = false; // Close menu after action
        };
        
        var refreshItem = new ButtonMenuItem { Text = "Refresh tenants for the selected environment" };
        refreshItem.Click += async (_, _) => await RefreshTenantsForSelectedEnvironment();
        
        _contextMenu.Items.Add(removeItem);
        _contextMenu.Items.Add(refreshItem);
        
        // Add separator - try different approaches to ensure it's visible
        var separator = new SeparatorMenuItem();
        _contextMenu.Items.Add(separator);
        
        // Add a placeholder item initially to make separator visible, will be replaced by tenants
        var placeholderItem = new ButtonMenuItem { Text = "(Select 'Refresh tenants' to load)", Enabled = false };
        _contextMenu.Items.Add(placeholderItem);
        
        // Track when menu is closed by user action
        _contextMenu.Closed += (_, _) => 
        {
            // Only stop tracking if the menu was closed by user interaction, not programmatically
            if (!_shouldKeepMenuOpen)
                _shouldKeepMenuOpen = false;
        };
    }

    private void Grid_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Buttons == MouseButtons.Alternate)
        {
            // Prevent the grid from entering edit mode on right-click
            e.Handled = true;
        }
    }

    private void Grid_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Buttons == MouseButtons.Alternate)
        {
            // Find the row at the mouse position and select it without entering edit mode
            var hitInfo = _grid.GetCellAt(e.Location);
            if (hitInfo != null && hitInfo.Item != null)
            {
                // Select the row without entering edit mode
                _grid.UnselectAll();
                _grid.SelectRow(_contexts.IndexOf((AzureContext)hitInfo.Item));
                
                // Calculate menu position relative to the cell
                var screenLocation = _grid.PointToScreen(e.Location);
                _menuPosition = new Point((int)screenLocation.X, (int)screenLocation.Y);
                _shouldKeepMenuOpen = true;
                
                // Show context menu at screen position
                _contextMenu.Show(_menuPosition);
            }
        }
        else
        {
            // Any other click should close the menu tracking
            _shouldKeepMenuOpen = false;
        }
    }

    private void Dialog_GotFocus(object sender, System.EventArgs e)
    {
        // Reopen context menu if it should be kept open
        if (_shouldKeepMenuOpen)
        {
            Application.Instance.AsyncInvoke(() =>
            {
                if (_shouldKeepMenuOpen)
                {
                    _contextMenu.Show(_menuPosition);
                }
            });
        }
    }

    private void RemoveSelectedContext()
    {
        if (_grid.SelectedItem is AzureContext ctx)
            _contexts.Remove(ctx);
    }

    private async Task RefreshTenantsForSelectedEnvironment()
    {
        if (!(_grid.SelectedItem is AzureContext selectedContext))
            return;

        // Remove existing tenant items (after the separator) immediately
        RemoveTenantMenuItems();

        // Add loading indicator immediately
        var loadingItem = new ButtonMenuItem { Text = "Loading tenants...", Enabled = false };
        _contextMenu.Items.Add(loadingItem);

        try
        {
            // Get tenants for the selected environment - this will trigger Azure sign-in if needed
            var tenants = await _azureService.GetAvailableTenantsAsync(selectedContext.AzureEnvironmentName);

            // Remove loading indicator
            _contextMenu.Items.Remove(loadingItem);

            // Add tenant items
            foreach (var tenant in tenants)
            {
                var displayName = tenant.DisplayName ?? tenant.TenantId?.ToString() ?? "Unknown";
                var tenantId = tenant.TenantId?.ToString();
                
                var tenantItem = new ButtonMenuItem 
                { 
                    Text = $"Tenant: {displayName} ({tenantId})" 
                };
                
                tenantItem.Click += (_, _) => 
                {
                    // Update the TenantId cell value for the selected row
                    selectedContext.TenantId = tenantId;
                    
                    // Force grid refresh to show updated value
                    var selectedIndex = _contexts.IndexOf(selectedContext);
                    _grid.DataStore = null;
                    _grid.DataStore = _contexts;
                    if (selectedIndex >= 0 && selectedIndex < _contexts.Count)
                    {
                        _grid.SelectRow(selectedIndex);
                    }
                    
                    // Close menu tracking after tenant selection
                    _shouldKeepMenuOpen = false;
                };
                
                _contextMenu.Items.Add(tenantItem);
            }
        }
        catch (System.Exception ex)
        {
            // Remove loading indicator
            _contextMenu.Items.Remove(loadingItem);
            
            // Add error item
            var errorItem = new ButtonMenuItem 
            { 
                Text = $"Error: {ex.Message}", 
                Enabled = false 
            };
            _contextMenu.Items.Add(errorItem);
        }
    }

    private void RemoveTenantMenuItems()
    {
        // Remove all items after the separator (tenant items and loading/error items)
        var itemsToRemove = new List<MenuItem>();
        var separatorFound = false;
        
        foreach (var item in _contextMenu.Items)
        {
            if (separatorFound && !(item is SeparatorMenuItem))
            {
                itemsToRemove.Add(item);
            }
            else if (item is SeparatorMenuItem)
            {
                separatorFound = true;
            }
        }

        foreach (var item in itemsToRemove)
        {
            _contextMenu.Items.Remove(item);
        }
    }
}
