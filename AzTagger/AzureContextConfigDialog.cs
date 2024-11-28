// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using AzTagger.Models;
using AzTagger.Services;
using Microsoft.Kiota.Abstractions.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AzTagger;

public partial class AzureContextConfigDialog : Form
{
    private Settings _inputSettings;
    private BindingList<AzureContext> _tempAzureContexts;
    private List<TenantInfo> _tenantIdsForSelectedEnvironment = new List<TenantInfo>();
    private ContextMenuStrip _dataGridViewContextMenu;
    private AzureService _azureService;
    private bool _initialized = false;
    private Point _contextMenuLocation;
    private bool _contextMenuShallBeKeptOpen;

    public AzureContextConfigDialog(Settings settings)
    {
        _inputSettings = settings;
        _tempAzureContexts = new BindingList<AzureContext>(_inputSettings.AzureContexts.ToList());

        InitializeComponent();

        _dataGridView.DataSource = _tempAzureContexts;
        _dataGridView.CellToolTipTextNeeded += DataGridView_CellToolTipTextNeeded;

        _azureService = new AzureService(_inputSettings);
        var azureEnvironments = _azureService.GetAzureEnvironmentNames();

        _dataGridViewContextMenu = new ContextMenuStrip();
        foreach (var envName in azureEnvironments)
        {
            var envMenuItem = new ToolStripMenuItem($"Environment: {envName}");
            envMenuItem.Click += MenuItem_SelectEnvironment_Click;
            _dataGridViewContextMenu.Items.Add(envMenuItem);
        }
        _dataGridViewContextMenu.Items.Add(new ToolStripSeparator());

        var selectTenantItem = new ToolStripMenuItem("Refresh tenants for the selected environment");
        selectTenantItem.Click += MenuItem_RefreshTenants_Click;
        _dataGridViewContextMenu.Items.Add(selectTenantItem);
        _dataGridViewContextMenu.Items.Add(new ToolStripSeparator());

        _dataGridViewContextMenu.Opened += DataGridView_ContextMenu_Opened;
        _dataGridView.ContextMenuStrip = _dataGridViewContextMenu;

        if (Application.IsDarkModeEnabled)
        {
            _dataGridView.BackgroundColor = Color.FromArgb(30, 30, 30);
            _dataGridView.ForeColor = Color.White;
            _dataGridView.GridColor = Color.FromArgb(45, 45, 45);
            _dataGridView.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            _dataGridView.DefaultCellStyle.ForeColor = Color.White;
            _dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 153, 255);
            _dataGridView.DefaultCellStyle.SelectionForeColor = Color.Black;
            _dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            _dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dataGridView.EnableHeadersVisualStyles = false;
            _dataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            _dataGridView.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
        }
    }

    private void Form_Load(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_inputSettings.SelectedAzureContext))
        {
            var selectedAzureContext = _tempAzureContexts.AsList().Find(x => x.Name == _inputSettings.SelectedAzureContext);
            if (selectedAzureContext != null)
            {
                var index = _tempAzureContexts.IndexOf(selectedAzureContext);
                _dataGridView.Rows[index].Selected = true;
                UpdateSelectionLabel(selectedAzureContext.Name);
            }
        }

        _initialized = true;
    }

    private void Form_Activated(object sender, EventArgs e)
    {
        if (_contextMenuShallBeKeptOpen)
        {
            _dataGridViewContextMenu.Show(_dataGridView, _contextMenuLocation);
        }
    }

    private void DataGridView_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            var columnName = _dataGridView.Columns[e.ColumnIndex].DataPropertyName;
            if (columnName == nameof(AzureContext.AzureEnvironmentName) ||
                columnName == nameof(AzureContext.TenantId))
            {
                e.ToolTipText = "Use the context menu to choose environment and tenant.";
            }
        }
    }

    private void DataGridView_ContextMenu_Opened(object sender, EventArgs e)
    {
        if (!_contextMenuShallBeKeptOpen)
        {
            _contextMenuLocation = _dataGridView.PointToClient(Cursor.Position);
        }
    }

    private void MenuItem_SelectEnvironment_Click(object sender, EventArgs e)
    {
        var menuItemText = (sender as ToolStripMenuItem)?.Text;
        if (string.IsNullOrEmpty(menuItemText))
        {
            return;
        }
        var envName = menuItemText.Replace("Environment: ", string.Empty);
        var selectedRow = _dataGridView.SelectedRows[0];
        if (selectedRow != null)
        {
            selectedRow.Cells[1].Value = envName;
            selectedRow.Cells[1].Selected = true;
            _dataGridView.NotifyCurrentCellDirty(true);
            _dataGridView.Refresh();

            ResetTenantMenuItems();
        }
    }

    private void MenuItem_RefreshTenants_Click(object sender, EventArgs e)
    {
        var selectedRow = _dataGridView.SelectedRows[0];
        if (selectedRow == null)
        {
            return;
        }
        ResetTenantMenuItems("Loading tenant infos...");
        _contextMenuShallBeKeptOpen = true;
        _dataGridViewContextMenu.Show(_dataGridView, _contextMenuLocation);
        var selectedEnvironment = selectedRow.Cells[1].Value?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(selectedEnvironment))
        {
            selectedEnvironment = "AzurePublicCloud";
            selectedRow.Cells[1].Value = selectedEnvironment;
        }
        Task.Run(() => GetTenants(selectedEnvironment)).ContinueWith(task =>
        {
            var tenantInfos = task.Result;
            Invoke((MethodInvoker)delegate
            {
                if (tenantInfos.Length > 0)
                {
                    ResetTenantMenuItems();

                    foreach (var tenant in tenantInfos)
                    {
                        _tenantIdsForSelectedEnvironment.Add(tenant);

                        foreach (var tenantInfo in _tenantIdsForSelectedEnvironment)
                        {
                            var tenantMenuItem = new ToolStripMenuItem($"Tenant: {tenantInfo.ToString()}");
                            tenantMenuItem.Click += MenuItem_SelectTenant_Click;
                            _dataGridViewContextMenu.Items.Add(tenantMenuItem);
                        }
                    }
                }
                else
                {
                    ResetTenantMenuItems("No tenants found");
                }
                _dataGridViewContextMenu.Refresh();
                _contextMenuShallBeKeptOpen = false;
            });
        });
    }

    private void MenuItem_SelectTenant_Click(object sender, EventArgs e)
    {
        var menuItemText = (sender as ToolStripMenuItem)?.Text;
        if (string.IsNullOrEmpty(menuItemText))
        {
            return;
        }
        var tenantString = menuItemText.Replace("Tenant: ", string.Empty);
        var tenantId = _tenantIdsForSelectedEnvironment.Find(t => t.ToString() == tenantString)?.TenantId;
        var selectedRow = _dataGridView.SelectedRows[0];
        if (selectedRow != null)
        {
            selectedRow.Cells[2].Value = tenantId;
            selectedRow.Cells[2].Selected = true;
            _dataGridView.NotifyCurrentCellDirty(true);
            _dataGridView.Refresh();
        }
    }

    private void DataGridView_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
    {
        var deletedRowIndex = e.RowIndex;
        if (_dataGridView.SelectedRows.Count == 0 && _dataGridView.Rows.Count > 0)
        {
            _dataGridView.Rows[deletedRowIndex - 1].Selected = true;
        }
    }

    private void DataGridView_SelectionChanged(object sender, EventArgs e)
    {
        if (!_initialized)
        {
            return;
        }

        var selectedAzureContextName = GetSelectedAzureContextName();
        if (string.IsNullOrEmpty(selectedAzureContextName))
        {
            selectedAzureContextName = "None";
        }
        UpdateSelectionLabel(selectedAzureContextName);
    }

    private void DataGridView_CellMouse_DoubleClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        foreach (DataGridViewCell cell in _dataGridView.Rows[e.RowIndex].Cells)
        {
            if (cell.Value == null || string.IsNullOrEmpty(cell.Value.ToString().Trim()))
            {
                return;
            }
        }
        Button_Ok_Click(sender, e);
    }

    private void Button_Ok_Click(object sender, EventArgs e)
    {
        _inputSettings.AzureContexts = _tempAzureContexts.AsList();
        var selectedAzureContextName = GetSelectedAzureContextName();
        _inputSettings.SelectAzureContext(selectedAzureContextName);
        _inputSettings.SanitizeAzureContexts();
        Close();
    }

    private void Button_Cancel_Click(object sender, EventArgs e)
    {
        Close();
    }

    private async Task<TenantInfo[]> GetTenants(string environment)
    {
        var result = Array.Empty<TenantInfo>();
        if (string.IsNullOrEmpty(environment))
        {
            return result;
        }
        try
        {
            var tenantDataList = await _azureService.GetAvailableTenantsAsync(environment);
            return tenantDataList.Select(t => new TenantInfo { TenantId = t.TenantId.ToString(), DisplayName = t.DisplayName }).ToArray();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to retrieve tenant IDs: " + ex.Message);
        }
        return result;
    }

    private void ResetTenantMenuItems(string message = null)
    {
        _tenantIdsForSelectedEnvironment.Clear();
        var separatorIndex = _dataGridViewContextMenu.Items.IndexOf(_dataGridViewContextMenu.Items.OfType<ToolStripSeparator>().Last());
        for (var i = _dataGridViewContextMenu.Items.Count - 1; i > separatorIndex; i--)
        {
            _dataGridViewContextMenu.Items.RemoveAt(i);
        }
        if (!string.IsNullOrWhiteSpace(message))
        {
            var tenantMenuItem = new ToolStripMenuItem(message);
            tenantMenuItem.Enabled = false;
            _dataGridViewContextMenu.Items.Add(tenantMenuItem);
        }
    }

    private string GetSelectedAzureContextName()
    {
        string selectedAzureContextName = null;
        try
        {
            selectedAzureContextName = _dataGridView.SelectedRows.Count > 0 ? _dataGridView.SelectedRows[0].Cells[0].Value?.ToString() : "None";
        }
        catch (Exception) { }
        return selectedAzureContextName ?? string.Empty;
    }

    private void UpdateSelectionLabel(string selectedAzureContextName)
    {
        _lblSelectedAzureContextName.Text = $"Selected Azure Context: {selectedAzureContextName}";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        const int WM_KEYDOWN = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104;
        if ((msg.Msg == WM_KEYDOWN) || (msg.Msg == WM_SYSKEYDOWN))
        {
            if (keyData == Keys.Enter)
            {
                if (_dataGridView.IsCurrentCellInEditMode)
                {
                    _dataGridView.EndEdit();
                    return true;
                }
            }
            else if (keyData == Keys.Escape)
            {
                if (_dataGridView.IsCurrentCellInEditMode || (_dataGridView.CurrentRow != null && _dataGridView.CurrentRow.IsNewRow))
                {
                    _dataGridView.CancelEdit();
                    return true;
                }
                else
                {
                    Close();
                    return true;
                }
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private class TenantInfo
    {
        public string TenantId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public override string ToString()
        {
            return $"{DisplayName} ({TenantId})";
        }
    }
}
