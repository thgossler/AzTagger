using System.Windows.Forms;

namespace AzTagger;

partial class AzureContextConfigDialog
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AzureContextConfigDialog));
        _dataGridView = new DataGridView();
        _colName = new DataGridViewTextBoxColumn();
        _colAzureEnvironment = new DataGridViewTextBoxColumn();
        _colTenantId = new DataGridViewTextBoxColumn();
        _colClientAppId = new DataGridViewTextBoxColumn();
        _btnOk = new Button();
        _btnCancel = new Button();
        _label = new Label();
        _lblSelectedAzureContextName = new Label();
        _toolTip = new ToolTip(components);
        ((System.ComponentModel.ISupportInitialize)_dataGridView).BeginInit();
        SuspendLayout();
        // 
        // _dataGridView
        // 
        _dataGridView.AllowDrop = true;
        _dataGridView.AllowUserToResizeRows = false;
        _dataGridView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _dataGridView.Columns.AddRange(new DataGridViewColumn[] { _colName, _colAzureEnvironment, _colTenantId, _colClientAppId });
        _dataGridView.Location = new System.Drawing.Point(12, 183);
        _dataGridView.MultiSelect = false;
        _dataGridView.Name = "_dataGridView";
        _dataGridView.RowHeadersWidth = 51;
        _dataGridView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        _dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _dataGridView.Size = new System.Drawing.Size(840, 148);
        _dataGridView.TabIndex = 0;
        _dataGridView.CellEndEdit += DataGridView_CellEndEdit;
        _dataGridView.CellMouseDoubleClick += DataGridView_CellMouse_DoubleClick;
        _dataGridView.RowsRemoved += DataGridView_RowsRemoved;
        _dataGridView.SelectionChanged += DataGridView_SelectionChanged;
        // 
        // _colName
        // 
        _colName.DataPropertyName = "Name";
        _colName.HeaderText = "Context Name";
        _colName.MaxInputLength = 64;
        _colName.MinimumWidth = 6;
        _colName.Name = "_colName";
        _colName.SortMode = DataGridViewColumnSortMode.NotSortable;
        _colName.Width = 140;
        // 
        // _colAzureEnvironment
        // 
        _colAzureEnvironment.DataPropertyName = "AzureEnvironmentName";
        _colAzureEnvironment.HeaderText = "Azure Environment Name";
        _colAzureEnvironment.MaxInputLength = 32;
        _colAzureEnvironment.MinimumWidth = 6;
        _colAzureEnvironment.Name = "_colAzureEnvironment";
        _colAzureEnvironment.SortMode = DataGridViewColumnSortMode.NotSortable;
        _colAzureEnvironment.Width = 220;
        // 
        // _colTenantId
        // 
        _colTenantId.DataPropertyName = "TenantId";
        _colTenantId.HeaderText = "Tenant ID";
        _colTenantId.MaxInputLength = 128;
        _colTenantId.MinimumWidth = 6;
        _colTenantId.Name = "_colTenantId";
        _colTenantId.SortMode = DataGridViewColumnSortMode.NotSortable;
        _colTenantId.Width = 200;
        // 
        // _colClientAppId
        // 
        _colClientAppId.DataPropertyName = "ClientAppId";
        _colClientAppId.HeaderText = "Client App ID";
        _colClientAppId.MaxInputLength = 128;
        _colClientAppId.MinimumWidth = 6;
        _colClientAppId.Name = "_colClientAppId";
        _colClientAppId.SortMode = DataGridViewColumnSortMode.NotSortable;
        _colClientAppId.Width = 200;
        // 
        // _btnOk
        // 
        _btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOk.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _btnOk.DialogResult = DialogResult.OK;
        _btnOk.Location = new System.Drawing.Point(679, 343);
        _btnOk.Name = "_btnOk";
        _btnOk.Size = new System.Drawing.Size(81, 33);
        _btnOk.TabIndex = 1;
        _btnOk.Text = "Save";
        _btnOk.UseVisualStyleBackColor = true;
        _btnOk.Click += Button_Ok_Click;
        // 
        // _btnCancel
        // 
        _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnCancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Location = new System.Drawing.Point(770, 343);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new System.Drawing.Size(82, 33);
        _btnCancel.TabIndex = 2;
        _btnCancel.Text = "Cancel";
        _btnCancel.UseVisualStyleBackColor = true;
        _btnCancel.Click += Button_Cancel_Click;
        // 
        // _label
        // 
        _label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _label.Location = new System.Drawing.Point(12, 12);
        _label.Name = "_label";
        _label.Size = new System.Drawing.Size(840, 158);
        _label.TabIndex = 3;
        _label.Text = resources.GetString("_label.Text");
        // 
        // _lblSelectedAzureContextName
        // 
        _lblSelectedAzureContextName.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _lblSelectedAzureContextName.Location = new System.Drawing.Point(269, 343);
        _lblSelectedAzureContextName.Name = "_lblSelectedAzureContextName";
        _lblSelectedAzureContextName.Size = new System.Drawing.Size(390, 33);
        _lblSelectedAzureContextName.TabIndex = 4;
        _lblSelectedAzureContextName.Text = "Selected Azure Context: None";
        _lblSelectedAzureContextName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        // 
        // AzureContextConfigDialog
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = _btnCancel;
        ClientSize = new System.Drawing.Size(864, 387);
        Controls.Add(_lblSelectedAzureContextName);
        Controls.Add(_label);
        Controls.Add(_btnCancel);
        Controls.Add(_btnOk);
        Controls.Add(_dataGridView);
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AzureContextConfigDialog";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Environments";
        Activated += Form_Activated;
        Deactivate += Form_Deactivate;
        Load += Form_Load;
        ((System.ComponentModel.ISupportInitialize)_dataGridView).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.DataGridView _dataGridView;
    private System.Windows.Forms.Button _btnOk;
    private System.Windows.Forms.Button _btnCancel;
    private System.Windows.Forms.Label _label;
    private System.Windows.Forms.Label _lblSelectedAzureContextName;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colAzureEnvironment;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colTenantId;
    private System.Windows.Forms.DataGridViewTextBoxColumn _colClientAppId;
    private System.Windows.Forms.ToolTip _toolTip;
}