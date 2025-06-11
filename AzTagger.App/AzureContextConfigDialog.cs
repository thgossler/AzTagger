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
        ClientSize = new Size(600, 300);
        Padding = 10;
        Resizable = true;

        _contexts = new ObservableCollection<AzureContext>(settings.AzureContexts);

        _grid = new GridView
        {
            DataStore = _contexts
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
            Width = 200
        };

        _grid.Columns.Add(nameColumn);
        _grid.Columns.Add(envColumn);
        _grid.Columns.Add(tenantColumn);

        var addButton = new Button { Text = "Add" };
        addButton.Click += (_, _) => _contexts.Add(new AzureContext());

        var removeButton = new Button { Text = "Remove" };
        removeButton.Click += (_, _) =>
        {
            if (_grid.SelectedItem is AzureContext ctx)
                _contexts.Remove(ctx);
        };

        var okButton = new Button { Text = "OK" };
        okButton.Click += (_, _) =>
        {
            settings.AzureContexts = new List<AzureContext>(_contexts);
            settings.SelectedAzureContext = settings.AzureContexts.FirstOrDefault()?.Name ?? string.Empty;
            Close(true);
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Click += (_, _) => Close(false);

        DefaultButton = okButton;
        AbortButton = cancelButton;

        var layout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
        layout.AddRow(_grid);
        layout.AddSeparateRow(addButton, removeButton, null);
        layout.AddSeparateRow(null, okButton, cancelButton);
        Content = layout;
    }
}
