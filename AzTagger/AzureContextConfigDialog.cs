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
    private ToolStripMenuItem _deleteContextMenuItem;
    private AzureService _azureService;
    private bool _initialized = false;
    private Point _contextMenuLocation;
    private volatile bool _contextMenuShallBeKeptOpen;
    private bool _isFormActive;

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

        _deleteContextMenuItem = new ToolStripMenuItem($"Remove Context");
        _deleteContextMenuItem.Click += MenuItem_RemoveAzureContextIrem_Click;
        _dataGridViewContextMenu.Items.Add(_deleteContextMenuItem);
        _dataGridViewContextMenu.Items.Add(new ToolStripSeparator());

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
        AttachClickEventHandler(this);

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
        _isFormActive = true;
        if (_contextMenuShallBeKeptOpen)
        {
            _dataGridView.Focus();
            _dataGridViewContextMenu.AutoClose = false;
            _dataGridViewContextMenu.Show(_dataGridView, _contextMenuLocation);
        }
    }

    private void Form_Deactivate(object sender, EventArgs e)
    {
        _isFormActive = false;
        _dataGridViewContextMenu.AutoClose = true;
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

    private void MenuItem_RemoveAzureContextIrem_Click(object sender, EventArgs e)
    {
        if (_dataGridView.SelectedRows.Count > 0)
        {
            var selectedRow = _dataGridView.SelectedRows[0];
            if (selectedRow != null)
            {
                if (selectedRow.IsNewRow)
                {
                    var newSelectedRowIndex = selectedRow.Index - 1;
                    _dataGridView.CancelEdit();
                    if (_dataGridView.Rows.Count > 0 && newSelectedRowIndex >= 0)
                    {
                        _dataGridView.Rows[newSelectedRowIndex].Selected = true;
                    }
                }
                else
                {
                    _tempAzureContexts.RemoveAt(selectedRow.Index);
                }
                _dataGridView.Refresh();
            }
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
        KeepContextMenuOpen(true);
        ResetTenantMenuItems("Loading tenant infos...");
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

                        var tenantMenuItem = new ToolStripMenuItem($"Tenant: {tenant.ToString()}");
                        tenantMenuItem.Click += MenuItem_SelectTenant_Click;
                        _dataGridViewContextMenu.Items.Add(tenantMenuItem);
                    }
                }
                else
                {
                    ResetTenantMenuItems("No tenants found");
                }
                KeepContextMenuOpen(false);
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

    private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex == 0)
        {
            if (string.IsNullOrEmpty(_dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString()))
            {
                _dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "Default";
            }
            else
            {
                var azureContextNames = _tempAzureContexts.Select(x => x.Name).ToList();
                var azureContextName = _dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                var i = 1;
                while (azureContextNames.Count(x => x == azureContextName) > 1)
                {
                    azureContextName = $"{_dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value} ({i})";
                    i++;
                }
                _dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = azureContextName;

                if (_dataGridView.SelectedRows.Count > 0)
                {
                    var selectedAzureContextName = GetSelectedAzureContextName();
                    UpdateSelectionLabel(selectedAzureContextName);
                }
            }
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

    private void KeepContextMenuOpen(bool keepOpen)
    {
        if (keepOpen)
        {
            _contextMenuShallBeKeptOpen = true;
            _dataGridViewContextMenu.Show(_dataGridView, _contextMenuLocation);
        }
        else
        {
            Task.Delay(3000).ContinueWith(_ =>
            {
                _contextMenuShallBeKeptOpen = false;
                if (_isFormActive)
                {
                    Invoke((MethodInvoker)delegate
                    {
                        _dataGridViewContextMenu.AutoClose = true;
                        _dataGridViewContextMenu.Show(_dataGridView, _contextMenuLocation);
                    });
                }
            });
        }
    }

    private void AttachClickEventHandler(Control control)
    {
        control.Click += HideContextMenu;
        foreach (Control childControl in control.Controls)
        {
            AttachClickEventHandler(childControl);
        }
    }

    private void HideContextMenu(object sender, EventArgs e)
    {
        if (_dataGridViewContextMenu.Visible)
        {
            _dataGridViewContextMenu.Close();
        }
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
            result = tenantDataList.Select(t => new TenantInfo { TenantId = t.TenantId.ToString(), DisplayName = t.DisplayName }).ToArray();
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
            var progressMenuItem = new ToolStripMenuItem(message);
            progressMenuItem.Enabled = false;
            _dataGridViewContextMenu.Items.Add(progressMenuItem);
        }
        _dataGridViewContextMenu.Refresh();
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
                if (_dataGridViewContextMenu.Visible)
                {
                    _dataGridViewContextMenu.Close();
                    return true;
                }
                else if (_dataGridView.IsCurrentCellInEditMode || (_dataGridView.CurrentRow != null && _dataGridView.CurrentRow.IsNewRow))
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
