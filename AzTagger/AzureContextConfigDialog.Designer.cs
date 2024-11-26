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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AzureContextConfigDialog));
        _dataGridView = new System.Windows.Forms.DataGridView();
        _colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        _colAzureEnvironment = new System.Windows.Forms.DataGridViewTextBoxColumn();
        _colTenantId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        _colClientAppId = new System.Windows.Forms.DataGridViewTextBoxColumn();
        _btnOk = new System.Windows.Forms.Button();
        _btnCancel = new System.Windows.Forms.Button();
        _label = new System.Windows.Forms.Label();
        _lblSelectedAzureContextName = new System.Windows.Forms.Label();
        ((System.ComponentModel.ISupportInitialize)_dataGridView).BeginInit();
        SuspendLayout();
        // 
        // _dataGridView
        // 
        _dataGridView.AllowUserToResizeRows = false;
        _dataGridView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { _colName, _colAzureEnvironment, _colTenantId, _colClientAppId });
        _dataGridView.Location = new System.Drawing.Point(12, 183);
        _dataGridView.MultiSelect = false;
        _dataGridView.Name = "_dataGridView";
        _dataGridView.RowHeadersWidth = 51;
        _dataGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        _dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        _dataGridView.Size = new System.Drawing.Size(840, 148);
        _dataGridView.TabIndex = 0;
        _dataGridView.CellMouseDoubleClick += DataGridView_CellMouse_DoubleClick;
        _dataGridView.SelectionChanged += DataGridView_SelectionChanged;
        // 
        // _colName
        // 
        _colName.DataPropertyName = "Name";
        _colName.HeaderText = "Context Name";
        _colName.MaxInputLength = 64;
        _colName.MinimumWidth = 6;
        _colName.Name = "_colName";
        _colName.Width = 140;
        // 
        // _colAzureEnvironment
        // 
        _colAzureEnvironment.DataPropertyName = "AzureEnvironmentName";
        _colAzureEnvironment.HeaderText = "Azure Environment Name";
        _colAzureEnvironment.MaxInputLength = 32;
        _colAzureEnvironment.MinimumWidth = 6;
        _colAzureEnvironment.Name = "_colAzureEnvironment";
        _colAzureEnvironment.Width = 220;
        // 
        // _colTenantId
        // 
        _colTenantId.DataPropertyName = "TenantId";
        _colTenantId.HeaderText = "Tenant ID";
        _colTenantId.MaxInputLength = 128;
        _colTenantId.MinimumWidth = 6;
        _colTenantId.Name = "_colTenantId";
        _colTenantId.Width = 200;
        // 
        // _colClientAppId
        // 
        _colClientAppId.DataPropertyName = "ClientAppId";
        _colClientAppId.HeaderText = "Client App ID";
        _colClientAppId.MaxInputLength = 128;
        _colClientAppId.MinimumWidth = 6;
        _colClientAppId.Name = "_colClientAppId";
        _colClientAppId.Width = 200;
        // 
        // _btnOk
        // 
        _btnOk.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        _btnOk.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        _btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
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
        _btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        _btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        _btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
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
        _label.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _label.Location = new System.Drawing.Point(12, 12);
        _label.Name = "_label";
        _label.Size = new System.Drawing.Size(840, 158);
        _label.TabIndex = 3;
        _label.Text = resources.GetString("_label.Text");
        // 
        // _lblSelectedAzureContextName
        // 
        _lblSelectedAzureContextName.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
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
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        CancelButton = _btnCancel;
        ClientSize = new System.Drawing.Size(864, 387);
        Controls.Add(_lblSelectedAzureContextName);
        Controls.Add(_label);
        Controls.Add(_btnCancel);
        Controls.Add(_btnOk);
        Controls.Add(_dataGridView);
        DoubleBuffered = true;
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AzureContextConfigDialog";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Text = "Environments";
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
}