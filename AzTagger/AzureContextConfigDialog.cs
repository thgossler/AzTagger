// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using AzTagger.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AzTagger;

public partial class AzureContextConfigDialog : Form
{
    private Settings _inputSettings;
    private List<AzureContext> _tempAzureContexts;
    private bool _initialized = false;

    public AzureContextConfigDialog(Settings settings)
    {
        _inputSettings = settings;
        _tempAzureContexts = new List<AzureContext>(_inputSettings.AzureContexts);

        InitializeComponent();

        _dataGridView.AutoGenerateColumns = false;
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
    }

    private void Form_Load(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_inputSettings.SelectedAzureContext))
        {
            var selectedAzureContext = _tempAzureContexts.Find(x => x.Name == _inputSettings.SelectedAzureContext);
            if (selectedAzureContext != null)
            {
                var index = _tempAzureContexts.IndexOf(selectedAzureContext);
                _dataGridView.Rows[index].Selected = true;
                UpdateSelectionLabel(selectedAzureContext.Name);
            }
        }

        _initialized = true;
    }

    private string GetSelectedAzureContextName()
    {
        string selectedAzureContextName = string.Empty;
        try
        {
            selectedAzureContextName = _dataGridView.SelectedRows.Count > 0 ? _dataGridView.SelectedRows[0].Cells[0].Value.ToString() : "None";
        }
        catch (Exception) { }
        return selectedAzureContextName;
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

    private void UpdateSelectionLabel(string selectedAzureContextName)
    {
        _lblSelectedAzureContextName.Text = $"Selected Azure Context: {selectedAzureContextName}";
    }

    private void DataGridView_CellMouse_DoubleClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        Button_Ok_Click(sender, e);
    }

    private void Button_Ok_Click(object sender, EventArgs e)
    {
        _inputSettings.AzureContexts = _tempAzureContexts;
        var selectedAzureContextName = GetSelectedAzureContextName();
        _inputSettings.SelectAzureContext(selectedAzureContextName);
        _inputSettings.SanitizeAzureContexts();
        Close();
    }

    private void Button_Cancel_Click(object sender, EventArgs e)
    {
        Close();
    }
}
