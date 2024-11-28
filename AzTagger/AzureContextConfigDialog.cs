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
    private static BindingList<string> _allTenantIds = new BindingList<string>();

    private Settings _inputSettings;
    private BindingList<AzureContext> _tempAzureContexts;
    private AzureService _azureService;
    private bool _initialized = false;

    public AzureContextConfigDialog(Settings settings)
    {
        _inputSettings = settings;
        _tempAzureContexts = new BindingList<AzureContext>(_inputSettings.AzureContexts);

        InitializeComponent();

        _dataGridView.DataSource = _tempAzureContexts;

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

        _azureService = new AzureService(_inputSettings);
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

        UpdateTenantIdColumnItems();

        _initialized = true;

        //Task.Run(GetTenantsAndUpdateUIAsync);
    }

    private void DataGridView_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
    {
        if (_dataGridView.SelectedRows.Count == 0 && _dataGridView.Rows.Count > 0)
        {
            _dataGridView.Rows[0].Selected = true;
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

    private void UpdateTenantIdColumnItems(List<string> retrievedTenants = null)
    {
        var existingTenantIds = _tempAzureContexts.Select(c => c.TenantId)
            .Where(id => !string.IsNullOrEmpty(id))
            .OrderBy(t => t)
            .ToList();

        if (retrievedTenants != null)
        {
            var list = retrievedTenants.Concat(existingTenantIds
                .Where(t1 => !retrievedTenants.Any(t2 => t2 == t1)))
                .OrderBy(t => t)
                .ToList();
            UpdateTenantInfoList(list);
        }
        else
        {
            UpdateTenantInfoList(existingTenantIds);
        }
    }

    private static void UpdateTenantInfoList(IList<string> list)
    {
        _allTenantIds.Clear();
        foreach (var item in list)
        {
            _allTenantIds.Add(item);
        }
    }

    private async Task GetTenantsAndUpdateUIAsync()
    {
        try
        {
            var tenantDataList = await _azureService.GetAvailableTenantsAsync();

            var newTenantItems = tenantDataList.Select(t => t.TenantId.ToString()).ToList();
            UpdateTenantIdColumnItems(newTenantItems);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to retrieve tenant IDs: " + ex.Message);
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
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
