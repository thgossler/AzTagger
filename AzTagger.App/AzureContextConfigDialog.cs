using Eto.Forms;
using Eto.Drawing;
using AzTagger.Services;
using AzTagger.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace AzTagger.App;

public class AzureContextConfigDialog : Dialog<bool>
{
    private readonly ObservableCollection<AzureContext> _contexts;
    private readonly GridView _grid;

    public AzureContextConfigDialog(Settings settings, AzureService azureService)
    {
        Title = "Azure Contexts";
        ClientSize = new Size(640, 280);
        MinimumSize = new Size(500, 260);
        Padding = 10;
        Resizable = true;

        _contexts = new ObservableCollection<AzureContext>(settings.AzureContexts);

        _grid = new GridView
        {
            DataStore = _contexts,
            Size = new Size(-1, 120) // Height for exactly 3 rows
        };

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

        // Create main layout using TableLayout for better control
        var mainLayout = new TableLayout
        {
            Padding = new Padding(10),
            Spacing = new Size(5, 5)
        };

        // Top section: Add/Remove buttons
        var buttonLayout = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items = { addButton, removeButton }
        };

        // Bottom section: OK/Cancel buttons anchored to bottom right
        var bottomButtonLayout = new TableLayout();
        bottomButtonLayout.Rows.Add(new TableRow(
            new TableCell { ScaleWidth = true }, // Spacer cell to push buttons to the right
            new TableCell(okButton, true) { ScaleWidth = false },
            new TableCell(new Panel { Width = 10 }, true) { ScaleWidth = false }, // Spacer between buttons
            new TableCell(cancelButton, true) { ScaleWidth = false }
        ));

        // Configure table layout
        mainLayout.Rows.Add(new TableRow(buttonLayout) { ScaleHeight = false });
        mainLayout.Rows.Add(new TableRow(_grid) { ScaleHeight = true }); // Grid expands
        mainLayout.Rows.Add(new TableRow(bottomButtonLayout) { ScaleHeight = false });

        Content = mainLayout;
    }
}
