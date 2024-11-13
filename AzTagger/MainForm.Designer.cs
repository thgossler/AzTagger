namespace AzTagger
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ComboBox tenantComboBox;
        private System.Windows.Forms.ComboBox environmentComboBox;
        private System.Windows.Forms.TextBox searchTextBox;
        private System.Windows.Forms.CheckBox searchAsYouTypeCheckBox;
        private System.Windows.Forms.CheckBox searchAsKQLRegexCheckBox;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.Button editQueryButton;
        private System.Windows.Forms.DataGridView mainDataGridView;
        private System.Windows.Forms.DataGridView tagsDataGridView;
        private System.Windows.Forms.Button applyTagsButton;
        private System.Windows.Forms.ComboBox tagTemplatesComboBox;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tenantComboBox = new System.Windows.Forms.ComboBox();
            this.environmentComboBox = new System.Windows.Forms.ComboBox();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.searchAsYouTypeCheckBox = new System.Windows.Forms.CheckBox();
            this.searchAsKQLRegexCheckBox = new System.Windows.Forms.CheckBox();
            this.searchButton = new System.Windows.Forms.Button();
            this.editQueryButton = new System.Windows.Forms.Button();
            this.mainDataGridView = new System.Windows.Forms.DataGridView();
            this.tagsDataGridView = new System.Windows.Forms.DataGridView();
            this.applyTagsButton = new System.Windows.Forms.Button();
            this.tagTemplatesComboBox = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.mainDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tagsDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // tenantComboBox
            // 
            this.tenantComboBox.FormattingEnabled = true;
            this.tenantComboBox.Location = new System.Drawing.Point(12, 12);
            this.tenantComboBox.Name = "tenantComboBox";
            this.tenantComboBox.Size = new System.Drawing.Size(200, 21);
            this.tenantComboBox.TabIndex = 0;
            // 
            // environmentComboBox
            // 
            this.environmentComboBox.FormattingEnabled = true;
            this.environmentComboBox.Location = new System.Drawing.Point(218, 12);
            this.environmentComboBox.Name = "environmentComboBox";
            this.environmentComboBox.Size = new System.Drawing.Size(200, 21);
            this.environmentComboBox.TabIndex = 1;
            // 
            // searchTextBox
            // 
            this.searchTextBox.Location = new System.Drawing.Point(12, 39);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(406, 20);
            this.searchTextBox.TabIndex = 2;
            this.searchTextBox.TextChanged += new System.EventHandler(this.searchTextBox_TextChanged);
            // 
            // searchAsYouTypeCheckBox
            // 
            this.searchAsYouTypeCheckBox.AutoSize = true;
            this.searchAsYouTypeCheckBox.Location = new System.Drawing.Point(424, 41);
            this.searchAsYouTypeCheckBox.Name = "searchAsYouTypeCheckBox";
            this.searchAsYouTypeCheckBox.Size = new System.Drawing.Size(115, 17);
            this.searchAsYouTypeCheckBox.TabIndex = 3;
            this.searchAsYouTypeCheckBox.Text = "Search as you type";
            this.searchAsYouTypeCheckBox.UseVisualStyleBackColor = true;
            // 
            // searchAsKQLRegexCheckBox
            // 
            this.searchAsKQLRegexCheckBox.AutoSize = true;
            this.searchAsKQLRegexCheckBox.Location = new System.Drawing.Point(545, 41);
            this.searchAsKQLRegexCheckBox.Name = "searchAsKQLRegexCheckBox";
            this.searchAsKQLRegexCheckBox.Size = new System.Drawing.Size(160, 17);
            this.searchAsKQLRegexCheckBox.TabIndex = 4;
            this.searchAsKQLRegexCheckBox.Text = "Search text is a KQL regex";
            this.searchAsKQLRegexCheckBox.UseVisualStyleBackColor = true;
            // 
            // searchButton
            // 
            this.searchButton.Location = new System.Drawing.Point(711, 37);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(75, 23);
            this.searchButton.TabIndex = 5;
            this.searchButton.Text = "Search";
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.searchButton_Click);
            // 
            // editQueryButton
            // 
            this.editQueryButton.Location = new System.Drawing.Point(792, 37);
            this.editQueryButton.Name = "editQueryButton";
            this.editQueryButton.Size = new System.Drawing.Size(75, 23);
            this.editQueryButton.TabIndex = 6;
            this.editQueryButton.Text = "Edit Query";
            this.editQueryButton.UseVisualStyleBackColor = true;
            this.editQueryButton.Click += new System.EventHandler(this.editQueryButton_Click);
            // 
            // mainDataGridView
            // 
            this.mainDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.mainDataGridView.Location = new System.Drawing.Point(12, 65);
            this.mainDataGridView.Name = "mainDataGridView";
            this.mainDataGridView.Size = new System.Drawing.Size(855, 300);
            this.mainDataGridView.TabIndex = 7;
            this.mainDataGridView.SelectionChanged += new System.EventHandler(this.mainDataGridView_SelectionChanged);
            // 
            // tagsDataGridView
            // 
            this.tagsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.tagsDataGridView.Location = new System.Drawing.Point(12, 371);
            this.tagsDataGridView.Name = "tagsDataGridView";
            this.tagsDataGridView.Size = new System.Drawing.Size(855, 150);
            this.tagsDataGridView.TabIndex = 8;
            this.tagsDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.tagsDataGridView_CellValueChanged);
            this.tagsDataGridView.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.tagsDataGridView_UserDeletingRow);
            this.tagsDataGridView.UserAddedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.tagsDataGridView_UserAddedRow);
            // 
            // applyTagsButton
            // 
            this.applyTagsButton.Location = new System.Drawing.Point(792, 527);
            this.applyTagsButton.Name = "applyTagsButton";
            this.applyTagsButton.Size = new System.Drawing.Size(75, 23);
            this.applyTagsButton.TabIndex = 9;
            this.applyTagsButton.Text = "Apply Tags";
            this.applyTagsButton.UseVisualStyleBackColor = true;
            this.applyTagsButton.Click += new System.EventHandler(this.applyTagsButton_Click);
            // 
            // tagTemplatesComboBox
            // 
            this.tagTemplatesComboBox.FormattingEnabled = true;
            this.tagTemplatesComboBox.Location = new System.Drawing.Point(12, 527);
            this.tagTemplatesComboBox.Name = "tagTemplatesComboBox";
            this.tagTemplatesComboBox.Size = new System.Drawing.Size(200, 21);
            this.tagTemplatesComboBox.TabIndex = 10;
            this.tagTemplatesComboBox.SelectedIndexChanged += new System.EventHandler(this.tagTemplatesComboBox_SelectedIndexChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(879, 562);
            this.Controls.Add(this.tagTemplatesComboBox);
            this.Controls.Add(this.applyTagsButton);
            this.Controls.Add(this.tagsDataGridView);
            this.Controls.Add(this.mainDataGridView);
            this.Controls.Add(this.editQueryButton);
            this.Controls.Add(this.searchButton);
            this.Controls.Add(this.searchAsKQLRegexCheckBox);
            this.Controls.Add(this.searchAsYouTypeCheckBox);
            this.Controls.Add(this.searchTextBox);
            this.Controls.Add(this.environmentComboBox);
            this.Controls.Add(this.tenantComboBox);
            this.Name = "MainForm";
            this.Text = "AzTagger";
            ((System.ComponentModel.ISupportInitialize)(this.mainDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tagsDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
