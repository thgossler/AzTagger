namespace AzTagger
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

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
            this.azureEnvironmentComboBox = new System.Windows.Forms.ComboBox();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.searchButton = new System.Windows.Forms.Button();
            this.resultsDataGridView = new System.Windows.Forms.DataGridView();
            this.applyTagsButton = new System.Windows.Forms.Button();
            this.tagsDataGridView = new System.Windows.Forms.DataGridView();
            this.tagTemplatesComboBox = new System.Windows.Forms.ComboBox();
            this.recentSearchesComboBox = new System.Windows.Forms.ComboBox();
            this.findItemsWithoutTagsButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.resultsDataGridView)).BeginInit();
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
            // azureEnvironmentComboBox
            // 
            this.azureEnvironmentComboBox.FormattingEnabled = true;
            this.azureEnvironmentComboBox.Location = new System.Drawing.Point(218, 12);
            this.azureEnvironmentComboBox.Name = "azureEnvironmentComboBox";
            this.azureEnvironmentComboBox.Size = new System.Drawing.Size(200, 21);
            this.azureEnvironmentComboBox.TabIndex = 1;
            // 
            // searchTextBox
            // 
            this.searchTextBox.Location = new System.Drawing.Point(12, 39);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(406, 20);
            this.searchTextBox.TabIndex = 2;
            // 
            // searchButton
            // 
            this.searchButton.Location = new System.Drawing.Point(424, 37);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(75, 23);
            this.searchButton.TabIndex = 3;
            this.searchButton.Text = "Search";
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.searchButton_Click);
            // 
            // resultsDataGridView
            // 
            this.resultsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.resultsDataGridView.Location = new System.Drawing.Point(12, 65);
            this.resultsDataGridView.Name = "resultsDataGridView";
            this.resultsDataGridView.Size = new System.Drawing.Size(760, 300);
            this.resultsDataGridView.TabIndex = 4;
            // 
            // applyTagsButton
            // 
            this.applyTagsButton.Location = new System.Drawing.Point(697, 371);
            this.applyTagsButton.Name = "applyTagsButton";
            this.applyTagsButton.Size = new System.Drawing.Size(75, 23);
            this.applyTagsButton.TabIndex = 5;
            this.applyTagsButton.Text = "Apply Tags";
            this.applyTagsButton.UseVisualStyleBackColor = true;
            this.applyTagsButton.Click += new System.EventHandler(this.applyTagsButton_Click);
            // 
            // tagsDataGridView
            // 
            this.tagsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.tagsDataGridView.Location = new System.Drawing.Point(12, 400);
            this.tagsDataGridView.Name = "tagsDataGridView";
            this.tagsDataGridView.Size = new System.Drawing.Size(760, 150);
            this.tagsDataGridView.TabIndex = 6;
            // 
            // tagTemplatesComboBox
            // 
            this.tagTemplatesComboBox.FormattingEnabled = true;
            this.tagTemplatesComboBox.Location = new System.Drawing.Point(12, 371);
            this.tagTemplatesComboBox.Name = "tagTemplatesComboBox";
            this.tagTemplatesComboBox.Size = new System.Drawing.Size(200, 21);
            this.tagTemplatesComboBox.TabIndex = 7;
            // 
            // recentSearchesComboBox
            // 
            this.recentSearchesComboBox.FormattingEnabled = true;
            this.recentSearchesComboBox.Location = new System.Drawing.Point(505, 37);
            this.recentSearchesComboBox.Name = "recentSearchesComboBox";
            this.recentSearchesComboBox.Size = new System.Drawing.Size(200, 21);
            this.recentSearchesComboBox.TabIndex = 8;
            this.recentSearchesComboBox.SelectedIndex = -1;
            this.recentSearchesComboBox.SelectedIndexChanged += new System.EventHandler(this.recentSearchesComboBox_SelectedIndexChanged);
            // 
            // findItemsWithoutTagsButton
            // 
            this.findItemsWithoutTagsButton.Location = new System.Drawing.Point(218, 371);
            this.findItemsWithoutTagsButton.Name = "findItemsWithoutTagsButton";
            this.findItemsWithoutTagsButton.Size = new System.Drawing.Size(200, 23);
            this.findItemsWithoutTagsButton.TabIndex = 9;
            this.findItemsWithoutTagsButton.Text = "Find items without any of these tags";
            this.findItemsWithoutTagsButton.UseVisualStyleBackColor = true;
            this.findItemsWithoutTagsButton.Click += new System.EventHandler(this.findItemsWithoutTagsButton_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.findItemsWithoutTagsButton);
            this.Controls.Add(this.recentSearchesComboBox);
            this.Controls.Add(this.tagTemplatesComboBox);
            this.Controls.Add(this.tagsDataGridView);
            this.Controls.Add(this.applyTagsButton);
            this.Controls.Add(this.resultsDataGridView);
            this.Controls.Add(this.searchButton);
            this.Controls.Add(this.searchTextBox);
            this.Controls.Add(this.azureEnvironmentComboBox);
            this.Controls.Add(this.tenantComboBox);
            this.Name = "MainForm";
            this.Text = "AzTagger";
            ((System.ComponentModel.ISupportInitialize)(this.resultsDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tagsDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.ComboBox tenantComboBox;
        private System.Windows.Forms.ComboBox azureEnvironmentComboBox;
        private System.Windows.Forms.TextBox searchTextBox;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.DataGridView resultsDataGridView;
        private System.Windows.Forms.Button applyTagsButton;
        private System.Windows.Forms.DataGridView tagsDataGridView;
        private System.Windows.Forms.ComboBox tagTemplatesComboBox;
        private System.Windows.Forms.ComboBox recentSearchesComboBox;
        private System.Windows.Forms.Button findItemsWithoutTagsButton;
    }
}
