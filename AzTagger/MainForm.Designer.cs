// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

namespace AzTagger
{
    public partial class MainForm
    {

        private System.Windows.Forms.Button _btnCopyQuery;

        private System.Windows.Forms.Button _btnPerformSearch;

        private System.Windows.Forms.Button _btnRefreshSignin;

        private System.Windows.Forms.ComboBox _cboQuickFilter1Column;

        private System.Windows.Forms.ComboBox _cboQuickFilter2Column;

        private System.Windows.Forms.ComboBox _cboRecentSearches;

        private System.Windows.Forms.Label _lblQueryMode;

        private System.Windows.Forms.Label _lblQuickFiltersLabel;

        private System.Windows.Forms.Label _lblResultsCount;

        private System.Windows.Forms.Label _lblSearchQuery;

        private System.Windows.Forms.Label _lblSearchResults;

        private System.Windows.Forms.LinkLabel _lnkResetQuickFilters;

        private System.Windows.Forms.FlowLayoutPanel _pnlQueryButtons;

        private System.Windows.Forms.FlowLayoutPanel _pnlQuickFilters;

        private System.Windows.Forms.ProgressBar _queryActivityIndicator;

        private System.Windows.Forms.TextBox _txtQuickFilter1Text;

        private System.Windows.Forms.TextBox _txtQuickFilter2Text;

        private System.Windows.Forms.TextBox _txtSearchQuery;

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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            _txtSearchQuery = new System.Windows.Forms.TextBox();
            _btnPerformSearch = new System.Windows.Forms.Button();
            _cboRecentSearches = new System.Windows.Forms.ComboBox();
            _lblSearchQuery = new System.Windows.Forms.Label();
            _lblSearchResults = new System.Windows.Forms.Label();
            _lblQueryMode = new System.Windows.Forms.Label();
            _queryActivityIndicator = new System.Windows.Forms.ProgressBar();
            _lblResultsCount = new System.Windows.Forms.Label();
            _btnCopyQuery = new System.Windows.Forms.Button();
            _pnlQueryButtons = new System.Windows.Forms.FlowLayoutPanel();
            _btnRefreshSignin = new System.Windows.Forms.Button();
            _btnSaveQuery = new System.Windows.Forms.Button();
            _cboSavedQueries = new System.Windows.Forms.ComboBox();
            _pnlQuickFilters = new System.Windows.Forms.FlowLayoutPanel();
            _lblQuickFiltersLabel = new System.Windows.Forms.Label();
            _cboQuickFilter1Column = new System.Windows.Forms.ComboBox();
            _txtQuickFilter1Text = new System.Windows.Forms.TextBox();
            _cboQuickFilter2Column = new System.Windows.Forms.ComboBox();
            _txtQuickFilter2Text = new System.Windows.Forms.TextBox();
            _lnkResetQuickFilters = new System.Windows.Forms.LinkLabel();
            _lnkDotNetRegExDocs = new System.Windows.Forms.LinkLabel();
            _toolTip = new System.Windows.Forms.ToolTip(components);
            _btnClearSearchQuery = new System.Windows.Forms.Button();
            _lblCopyPasteHint = new System.Windows.Forms.Label();
            _lblResultsFilteredCount = new System.Windows.Forms.Label();
            _cboTagTemplates = new System.Windows.Forms.ComboBox();
            _gvwTags = new System.Windows.Forms.DataGridView();
            Key = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _lnkResourceGraphDocs = new System.Windows.Forms.LinkLabel();
            _splitContainer = new System.Windows.Forms.SplitContainer();
            _resultsActivityIndicator = new System.Windows.Forms.ProgressBar();
            _gvwResults = new System.Windows.Forms.DataGridView();
            _lnkResetToDefaults = new System.Windows.Forms.LinkLabel();
            _lblVersion = new System.Windows.Forms.Label();
            _lnkDonation = new System.Windows.Forms.LinkLabel();
            _lnkGitHubLink = new System.Windows.Forms.LinkLabel();
            _lnkEditTagTemplates = new System.Windows.Forms.LinkLabel();
            _lblTags = new System.Windows.Forms.Label();
            _btnApplyTags = new System.Windows.Forms.Button();
            errorProvider1 = new System.Windows.Forms.ErrorProvider(components);
            errorProvider2 = new System.Windows.Forms.ErrorProvider(components);
            _cboAzureContext = new System.Windows.Forms.ComboBox();
            _lblAzureContext = new System.Windows.Forms.Label();
            _pnlQueryButtons.SuspendLayout();
            _pnlQuickFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_gvwTags).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
            _splitContainer.Panel1.SuspendLayout();
            _splitContainer.Panel2.SuspendLayout();
            _splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_gvwResults).BeginInit();
            ((System.ComponentModel.ISupportInitialize)errorProvider1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)errorProvider2).BeginInit();
            SuspendLayout();
            // 
            // _txtSearchQuery
            // 
            _txtSearchQuery.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _txtSearchQuery.HideSelection = false;
            _txtSearchQuery.Location = new System.Drawing.Point(44, 79);
            _txtSearchQuery.Multiline = true;
            _txtSearchQuery.Name = "_txtSearchQuery";
            _txtSearchQuery.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            _txtSearchQuery.Size = new System.Drawing.Size(1426, 69);
            _txtSearchQuery.TabIndex = 2;
            _toolTip.SetToolTip(_txtSearchQuery, resources.GetString("_txtSearchQuery.ToolTip"));
            _txtSearchQuery.TextChanged += TextBox_SearchQuery_TextChanged;
            _txtSearchQuery.KeyPress += TextBox_SearchQuery_KeyPress;
            _txtSearchQuery.MouseDoubleClick += TextBox_SearchQuery_MouseDoubleClick;
            // 
            // _btnPerformSearch
            // 
            _btnPerformSearch.AutoSize = true;
            _btnPerformSearch.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _btnPerformSearch.Location = new System.Drawing.Point(3, 3);
            _btnPerformSearch.Name = "_btnPerformSearch";
            _btnPerformSearch.Size = new System.Drawing.Size(119, 30);
            _btnPerformSearch.TabIndex = 3;
            _btnPerformSearch.Text = "Perform Search";
            _toolTip.SetToolTip(_btnPerformSearch, "Search the Azure Resource Graph using your query to find matching resources, resource groups, and subscriptions.");
            _btnPerformSearch.UseVisualStyleBackColor = true;
            _btnPerformSearch.Click += Button_PerformSearch_Click;
            // 
            // _cboRecentSearches
            // 
            _cboRecentSearches.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _cboRecentSearches.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cboRecentSearches.FormattingEnabled = true;
            _cboRecentSearches.Location = new System.Drawing.Point(15, 12);
            _cboRecentSearches.MaxDropDownItems = 10;
            _cboRecentSearches.Name = "_cboRecentSearches";
            _cboRecentSearches.Size = new System.Drawing.Size(1098, 28);
            _cboRecentSearches.TabIndex = 8;
            _cboRecentSearches.SelectedIndexChanged += ComboBox_RecentSearches_SelectedIndexChanged;
            // 
            // _lblSearchQuery
            // 
            _lblSearchQuery.AutoSize = true;
            _lblSearchQuery.Location = new System.Drawing.Point(12, 56);
            _lblSearchQuery.Name = "_lblSearchQuery";
            _lblSearchQuery.Size = new System.Drawing.Size(99, 20);
            _lblSearchQuery.TabIndex = 10;
            _lblSearchQuery.Text = "Search Query:";
            // 
            // _lblSearchResults
            // 
            _lblSearchResults.AutoSize = true;
            _lblSearchResults.Location = new System.Drawing.Point(12, 210);
            _lblSearchResults.Name = "_lblSearchResults";
            _lblSearchResults.Size = new System.Drawing.Size(106, 20);
            _lblSearchResults.TabIndex = 11;
            _lblSearchResults.Text = "Search Results:";
            // 
            // _lblQueryMode
            // 
            _lblQueryMode.AutoSize = true;
            _lblQueryMode.Location = new System.Drawing.Point(138, 56);
            _lblQueryMode.Name = "_lblQueryMode";
            _lblQueryMode.Size = new System.Drawing.Size(583, 20);
            _lblQueryMode.TabIndex = 14;
            _lblQueryMode.Text = "(regular expression, applied to SubscriptionName, ResourceGroup and ResourceName)";
            // 
            // _queryActivityIndicator
            // 
            _queryActivityIndicator.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _queryActivityIndicator.Location = new System.Drawing.Point(45, 148);
            _queryActivityIndicator.MarqueeAnimationSpeed = 20;
            _queryActivityIndicator.Name = "_queryActivityIndicator";
            _queryActivityIndicator.Size = new System.Drawing.Size(1423, 2);
            _queryActivityIndicator.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            _queryActivityIndicator.TabIndex = 15;
            _queryActivityIndicator.Visible = false;
            // 
            // _lblResultsCount
            // 
            _lblResultsCount.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _lblResultsCount.Location = new System.Drawing.Point(855, 151);
            _lblResultsCount.Name = "_lblResultsCount";
            _lblResultsCount.Size = new System.Drawing.Size(616, 24);
            _lblResultsCount.TabIndex = 16;
            _lblResultsCount.Text = "(0 items)";
            _lblResultsCount.TextAlign = System.Drawing.ContentAlignment.TopRight;
            _toolTip.SetToolTip(_lblResultsCount, "These values represent all results returned by the KQL query without applying any quick filters.");
            // 
            // _btnCopyQuery
            // 
            _btnCopyQuery.AutoSize = true;
            _btnCopyQuery.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _btnCopyQuery.Location = new System.Drawing.Point(253, 3);
            _btnCopyQuery.Name = "_btnCopyQuery";
            _btnCopyQuery.Size = new System.Drawing.Size(96, 30);
            _btnCopyQuery.TabIndex = 17;
            _btnCopyQuery.Text = "Copy Query";
            _toolTip.SetToolTip(_btnCopyQuery, "Copy the complete KQL query to your clipboard for use in Azure Resource Graph Explorer.");
            _btnCopyQuery.UseVisualStyleBackColor = true;
            _btnCopyQuery.Click += Button_CopyQuery_Click;
            // 
            // _pnlQueryButtons
            // 
            _pnlQueryButtons.Controls.Add(_btnPerformSearch);
            _pnlQueryButtons.Controls.Add(_btnRefreshSignin);
            _pnlQueryButtons.Controls.Add(_btnCopyQuery);
            _pnlQueryButtons.Controls.Add(_btnSaveQuery);
            _pnlQueryButtons.Controls.Add(_cboSavedQueries);
            _pnlQueryButtons.Location = new System.Drawing.Point(12, 154);
            _pnlQueryButtons.Name = "_pnlQueryButtons";
            _pnlQueryButtons.Size = new System.Drawing.Size(761, 45);
            _pnlQueryButtons.TabIndex = 18;
            // 
            // _btnRefreshSignin
            // 
            _btnRefreshSignin.AutoSize = true;
            _btnRefreshSignin.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _btnRefreshSignin.Location = new System.Drawing.Point(128, 3);
            _btnRefreshSignin.Name = "_btnRefreshSignin";
            _btnRefreshSignin.Size = new System.Drawing.Size(119, 30);
            _btnRefreshSignin.TabIndex = 18;
            _btnRefreshSignin.Text = "Refresh Sign-in";
            _toolTip.SetToolTip(_btnRefreshSignin, "Refresh your sign-in if your Azure role-based permissions have been updated, such as when requesting elevated RBAC permissions through Privileged Identity Management.");
            _btnRefreshSignin.UseVisualStyleBackColor = true;
            _btnRefreshSignin.Click += Button_RefreshSignin_Click;
            // 
            // _btnSaveQuery
            // 
            _btnSaveQuery.AutoSize = true;
            _btnSaveQuery.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _btnSaveQuery.Location = new System.Drawing.Point(355, 3);
            _btnSaveQuery.Name = "_btnSaveQuery";
            _btnSaveQuery.Size = new System.Drawing.Size(120, 30);
            _btnSaveQuery.TabIndex = 19;
            _btnSaveQuery.Text = "Save Query as...";
            _toolTip.SetToolTip(_btnSaveQuery, "Save the current query under a custom name.");
            _btnSaveQuery.UseVisualStyleBackColor = true;
            _btnSaveQuery.Click += Button_SaveQuery_Click;
            // 
            // _cboSavedQueries
            // 
            _cboSavedQueries.CausesValidation = false;
            _cboSavedQueries.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cboSavedQueries.FormattingEnabled = true;
            _cboSavedQueries.Location = new System.Drawing.Point(481, 4);
            _cboSavedQueries.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            _cboSavedQueries.MaxDropDownItems = 20;
            _cboSavedQueries.Name = "_cboSavedQueries";
            _cboSavedQueries.Size = new System.Drawing.Size(237, 28);
            _cboSavedQueries.TabIndex = 20;
            _cboSavedQueries.SelectedIndexChanged += ComboBox_SavedQueries_SelectedIndexChanged;
            // 
            // _pnlQuickFilters
            // 
            _pnlQuickFilters.Controls.Add(_lblQuickFiltersLabel);
            _pnlQuickFilters.Controls.Add(_cboQuickFilter1Column);
            _pnlQuickFilters.Controls.Add(_txtQuickFilter1Text);
            _pnlQuickFilters.Controls.Add(_cboQuickFilter2Column);
            _pnlQuickFilters.Controls.Add(_txtQuickFilter2Text);
            _pnlQuickFilters.Controls.Add(_lnkResetQuickFilters);
            _pnlQuickFilters.Controls.Add(_lnkDotNetRegExDocs);
            _pnlQuickFilters.Location = new System.Drawing.Point(166, 205);
            _pnlQuickFilters.Name = "_pnlQuickFilters";
            _pnlQuickFilters.Size = new System.Drawing.Size(1247, 35);
            _pnlQuickFilters.TabIndex = 19;
            // 
            // _lblQuickFiltersLabel
            // 
            _lblQuickFiltersLabel.Location = new System.Drawing.Point(3, 0);
            _lblQuickFiltersLabel.Name = "_lblQuickFiltersLabel";
            _lblQuickFiltersLabel.Size = new System.Drawing.Size(150, 30);
            _lblQuickFiltersLabel.TabIndex = 0;
            _lblQuickFiltersLabel.Text = "Quick Filters (regex)";
            _lblQuickFiltersLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _cboQuickFilter1Column
            // 
            _cboQuickFilter1Column.CausesValidation = false;
            _cboQuickFilter1Column.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cboQuickFilter1Column.FormattingEnabled = true;
            _cboQuickFilter1Column.Location = new System.Drawing.Point(166, 3);
            _cboQuickFilter1Column.Margin = new System.Windows.Forms.Padding(10, 3, 3, 3);
            _cboQuickFilter1Column.MaxDropDownItems = 15;
            _cboQuickFilter1Column.Name = "_cboQuickFilter1Column";
            _cboQuickFilter1Column.Size = new System.Drawing.Size(151, 28);
            _cboQuickFilter1Column.TabIndex = 2;
            _toolTip.SetToolTip(_cboQuickFilter1Column, "Column to apply quick filter 1 to");
            // 
            // _txtQuickFilter1Text
            // 
            _txtQuickFilter1Text.Location = new System.Drawing.Point(323, 3);
            _txtQuickFilter1Text.MaxLength = 256;
            _txtQuickFilter1Text.Name = "_txtQuickFilter1Text";
            _txtQuickFilter1Text.Size = new System.Drawing.Size(115, 27);
            _txtQuickFilter1Text.TabIndex = 1;
            _toolTip.SetToolTip(_txtQuickFilter1Text, "Quick filter 1 regular expression");
            // 
            // _cboQuickFilter2Column
            // 
            _cboQuickFilter2Column.CausesValidation = false;
            _cboQuickFilter2Column.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cboQuickFilter2Column.FormattingEnabled = true;
            _cboQuickFilter2Column.Location = new System.Drawing.Point(456, 3);
            _cboQuickFilter2Column.Margin = new System.Windows.Forms.Padding(15, 3, 3, 3);
            _cboQuickFilter2Column.MaxDropDownItems = 15;
            _cboQuickFilter2Column.Name = "_cboQuickFilter2Column";
            _cboQuickFilter2Column.Size = new System.Drawing.Size(151, 28);
            _cboQuickFilter2Column.TabIndex = 3;
            _toolTip.SetToolTip(_cboQuickFilter2Column, "Column to apply quick filter 2 to");
            // 
            // _txtQuickFilter2Text
            // 
            _txtQuickFilter2Text.Location = new System.Drawing.Point(613, 3);
            _txtQuickFilter2Text.MaxLength = 256;
            _txtQuickFilter2Text.Name = "_txtQuickFilter2Text";
            _txtQuickFilter2Text.Size = new System.Drawing.Size(115, 27);
            _txtQuickFilter2Text.TabIndex = 4;
            _toolTip.SetToolTip(_txtQuickFilter2Text, "Quick filter 2 regular expression");
            // 
            // _lnkResetQuickFilters
            // 
            _lnkResetQuickFilters.AutoSize = true;
            _lnkResetQuickFilters.Location = new System.Drawing.Point(746, 7);
            _lnkResetQuickFilters.Margin = new System.Windows.Forms.Padding(15, 7, 3, 0);
            _lnkResetQuickFilters.Name = "_lnkResetQuickFilters";
            _lnkResetQuickFilters.Size = new System.Drawing.Size(127, 20);
            _lnkResetQuickFilters.TabIndex = 5;
            _lnkResetQuickFilters.TabStop = true;
            _lnkResetQuickFilters.Text = "Clear Quick Filters";
            _lnkResetQuickFilters.VisitedLinkColor = System.Drawing.Color.Blue;
            _lnkResetQuickFilters.LinkClicked += LinkLabel_ResetQuickFilters_LinkClicked;
            // 
            // _lnkDotNetRegExDocs
            // 
            _lnkDotNetRegExDocs.AutoSize = true;
            _lnkDotNetRegExDocs.Location = new System.Drawing.Point(891, 7);
            _lnkDotNetRegExDocs.Margin = new System.Windows.Forms.Padding(15, 7, 3, 0);
            _lnkDotNetRegExDocs.Name = "_lnkDotNetRegExDocs";
            _lnkDotNetRegExDocs.Size = new System.Drawing.Size(205, 20);
            _lnkDotNetRegExDocs.TabIndex = 6;
            _lnkDotNetRegExDocs.TabStop = true;
            _lnkDotNetRegExDocs.Text = ".NET Regular Expression Docs";
            _lnkDotNetRegExDocs.VisitedLinkColor = System.Drawing.Color.Blue;
            _lnkDotNetRegExDocs.LinkClicked += LinkLabel_DotNetRegExDocs_LinkClicked;
            // 
            // _toolTip
            // 
            _toolTip.AutomaticDelay = 400;
            _toolTip.AutoPopDelay = 10000;
            _toolTip.InitialDelay = 400;
            _toolTip.ReshowDelay = 80;
            // 
            // _btnClearSearchQuery
            // 
            _btnClearSearchQuery.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _btnClearSearchQuery.Location = new System.Drawing.Point(15, 79);
            _btnClearSearchQuery.Name = "_btnClearSearchQuery";
            _btnClearSearchQuery.Size = new System.Drawing.Size(27, 71);
            _btnClearSearchQuery.TabIndex = 28;
            _btnClearSearchQuery.Text = "X";
            _toolTip.SetToolTip(_btnClearSearchQuery, "Clear Search Query");
            _btnClearSearchQuery.UseVisualStyleBackColor = true;
            _btnClearSearchQuery.Click += Button_ClearSearchQuery_Click;
            // 
            // _lblCopyPasteHint
            // 
            _lblCopyPasteHint.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblCopyPasteHint.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
            _lblCopyPasteHint.Location = new System.Drawing.Point(498, 394);
            _lblCopyPasteHint.Name = "_lblCopyPasteHint";
            _lblCopyPasteHint.Size = new System.Drawing.Size(487, 20);
            _lblCopyPasteHint.TabIndex = 27;
            _lblCopyPasteHint.Text = "(Use Ctrl+C to copy data into clipboard)";
            _lblCopyPasteHint.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            _toolTip.SetToolTip(_lblCopyPasteHint, "Copies the selected rows, including column headers, as tab-separated plain text (TSV) that can be directly pasted into an Excel spreadsheet.");
            // 
            // _lblResultsFilteredCount
            // 
            _lblResultsFilteredCount.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _lblResultsFilteredCount.Location = new System.Drawing.Point(998, 394);
            _lblResultsFilteredCount.Name = "_lblResultsFilteredCount";
            _lblResultsFilteredCount.Size = new System.Drawing.Size(472, 26);
            _lblResultsFilteredCount.TabIndex = 26;
            _lblResultsFilteredCount.Text = "(0 items)";
            _lblResultsFilteredCount.TextAlign = System.Drawing.ContentAlignment.TopRight;
            _toolTip.SetToolTip(_lblResultsFilteredCount, "These values represent only the items after the quick filters have been applied.");
            // 
            // _cboTagTemplates
            // 
            _cboTagTemplates.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _cboTagTemplates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cboTagTemplates.FormattingEnabled = true;
            _cboTagTemplates.Location = new System.Drawing.Point(906, 32);
            _cboTagTemplates.Name = "_cboTagTemplates";
            _cboTagTemplates.Size = new System.Drawing.Size(335, 28);
            _cboTagTemplates.TabIndex = 29;
            _toolTip.SetToolTip(_cboTagTemplates, "When you select a template, its tags will be added to your current list, and any existing tags will be updated.");
            // 
            // _gvwTags
            // 
            _gvwTags.AllowUserToResizeRows = false;
            _gvwTags.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _gvwTags.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gvwTags.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Key, Value });
            _gvwTags.Location = new System.Drawing.Point(11, 32);
            _gvwTags.Name = "_gvwTags";
            _gvwTags.RowHeadersWidth = 51;
            _gvwTags.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _gvwTags.Size = new System.Drawing.Size(864, 234);
            _gvwTags.TabIndex = 28;
            _toolTip.SetToolTip(_gvwTags, "When multiple rows are selected, only the tags common to all selected resources are displayed below. Only these shared tags can be updated; other tags will remain unchanged.");
            // 
            // Key
            // 
            Key.HeaderText = "Key";
            Key.MaxInputLength = 255;
            Key.MinimumWidth = 6;
            Key.Name = "Key";
            Key.Width = 250;
            // 
            // Value
            // 
            Value.HeaderText = "Value";
            Value.MaxInputLength = 255;
            Value.MinimumWidth = 6;
            Value.Name = "Value";
            Value.Width = 540;
            // 
            // _lnkResourceGraphDocs
            // 
            _lnkResourceGraphDocs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _lnkResourceGraphDocs.AutoSize = true;
            _lnkResourceGraphDocs.Location = new System.Drawing.Point(1171, 56);
            _lnkResourceGraphDocs.Name = "_lnkResourceGraphDocs";
            _lnkResourceGraphDocs.Size = new System.Drawing.Size(297, 20);
            _lnkResourceGraphDocs.TabIndex = 27;
            _lnkResourceGraphDocs.TabStop = true;
            _lnkResourceGraphDocs.Text = "Azure Resource Graph query language docs";
            _lnkResourceGraphDocs.TextAlign = System.Drawing.ContentAlignment.TopRight;
            _lnkResourceGraphDocs.LinkClicked += LinkLabel_ResourceGraphDocs_LinkClicked;
            // 
            // _splitContainer
            // 
            _splitContainer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _splitContainer.Location = new System.Drawing.Point(1, 240);
            _splitContainer.Name = "_splitContainer";
            _splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _splitContainer.Panel1
            // 
            _splitContainer.Panel1.Controls.Add(_resultsActivityIndicator);
            _splitContainer.Panel1.Controls.Add(_lblCopyPasteHint);
            _splitContainer.Panel1.Controls.Add(_lblResultsFilteredCount);
            _splitContainer.Panel1.Controls.Add(_gvwResults);
            _splitContainer.Panel1MinSize = 200;
            // 
            // _splitContainer.Panel2
            // 
            _splitContainer.Panel2.Controls.Add(_lnkResetToDefaults);
            _splitContainer.Panel2.Controls.Add(_lblVersion);
            _splitContainer.Panel2.Controls.Add(_lnkDonation);
            _splitContainer.Panel2.Controls.Add(_lnkGitHubLink);
            _splitContainer.Panel2.Controls.Add(_lnkEditTagTemplates);
            _splitContainer.Panel2.Controls.Add(_lblTags);
            _splitContainer.Panel2.Controls.Add(_cboTagTemplates);
            _splitContainer.Panel2.Controls.Add(_gvwTags);
            _splitContainer.Panel2.Controls.Add(_btnApplyTags);
            _splitContainer.Panel2MinSize = 200;
            _splitContainer.Size = new System.Drawing.Size(1482, 713);
            _splitContainer.SplitterDistance = 425;
            _splitContainer.SplitterWidth = 10;
            _splitContainer.TabIndex = 29;
            // 
            // _resultsActivityIndicator
            // 
            _resultsActivityIndicator.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _resultsActivityIndicator.Location = new System.Drawing.Point(12, 389);
            _resultsActivityIndicator.MarqueeAnimationSpeed = 20;
            _resultsActivityIndicator.Name = "_resultsActivityIndicator";
            _resultsActivityIndicator.Size = new System.Drawing.Size(1459, 2);
            _resultsActivityIndicator.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            _resultsActivityIndicator.TabIndex = 28;
            _resultsActivityIndicator.Visible = false;
            // 
            // _gvwResults
            // 
            _gvwResults.AllowUserToAddRows = false;
            _gvwResults.AllowUserToDeleteRows = false;
            _gvwResults.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(224, 224, 224);
            _gvwResults.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            _gvwResults.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _gvwResults.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            _gvwResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gvwResults.Location = new System.Drawing.Point(12, 5);
            _gvwResults.Name = "_gvwResults";
            _gvwResults.ReadOnly = true;
            _gvwResults.RowHeadersWidth = 51;
            _gvwResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _gvwResults.ShowCellErrors = false;
            _gvwResults.ShowEditingIcon = false;
            _gvwResults.ShowRowErrors = false;
            _gvwResults.Size = new System.Drawing.Size(1458, 385);
            _gvwResults.StandardTab = true;
            _gvwResults.TabIndex = 25;
            // 
            // _lnkResetToDefaults
            // 
            _lnkResetToDefaults.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _lnkResetToDefaults.Location = new System.Drawing.Point(1270, 198);
            _lnkResetToDefaults.Name = "_lnkResetToDefaults";
            _lnkResetToDefaults.Size = new System.Drawing.Size(201, 23);
            _lnkResetToDefaults.TabIndex = 35;
            _lnkResetToDefaults.TabStop = true;
            _lnkResetToDefaults.Text = "Reset Window to Defaults";
            _lnkResetToDefaults.TextAlign = System.Drawing.ContentAlignment.TopRight;
            _lnkResetToDefaults.LinkClicked += LinkLabel_ResetToDefaults_LinkClicked;
            // 
            // _lblVersion
            // 
            _lblVersion.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _lblVersion.AutoSize = true;
            _lblVersion.Location = new System.Drawing.Point(1130, 246);
            _lblVersion.Name = "_lblVersion";
            _lblVersion.Size = new System.Drawing.Size(91, 20);
            _lblVersion.TabIndex = 34;
            _lblVersion.Text = "Version: x.x.x";
            // 
            // _lnkDonation
            // 
            _lnkDonation.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _lnkDonation.Location = new System.Drawing.Point(1224, 242);
            _lnkDonation.Name = "_lnkDonation";
            _lnkDonation.Size = new System.Drawing.Size(248, 24);
            _lnkDonation.TabIndex = 33;
            _lnkDonation.TabStop = true;
            _lnkDonation.Text = "👍 Thumbs-up with a Donation";
            _lnkDonation.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            _lnkDonation.VisitedLinkColor = System.Drawing.Color.Blue;
            // 
            // _lnkGitHubLink
            // 
            _lnkGitHubLink.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _lnkGitHubLink.Location = new System.Drawing.Point(1270, 218);
            _lnkGitHubLink.Name = "_lnkGitHubLink";
            _lnkGitHubLink.Size = new System.Drawing.Size(202, 24);
            _lnkGitHubLink.TabIndex = 32;
            _lnkGitHubLink.TabStop = true;
            _lnkGitHubLink.Text = "Open Source on GitHub";
            _lnkGitHubLink.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            _lnkGitHubLink.VisitedLinkColor = System.Drawing.Color.Blue;
            // 
            // _lnkEditTagTemplates
            // 
            _lnkEditTagTemplates.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _lnkEditTagTemplates.Location = new System.Drawing.Point(1108, 63);
            _lnkEditTagTemplates.Name = "_lnkEditTagTemplates";
            _lnkEditTagTemplates.Size = new System.Drawing.Size(133, 25);
            _lnkEditTagTemplates.TabIndex = 31;
            _lnkEditTagTemplates.TabStop = true;
            _lnkEditTagTemplates.Text = "Edit Templates";
            _lnkEditTagTemplates.TextAlign = System.Drawing.ContentAlignment.TopRight;
            _lnkEditTagTemplates.VisitedLinkColor = System.Drawing.Color.Blue;
            // 
            // _lblTags
            // 
            _lblTags.AutoSize = true;
            _lblTags.Location = new System.Drawing.Point(11, 9);
            _lblTags.Name = "_lblTags";
            _lblTags.Size = new System.Drawing.Size(41, 20);
            _lblTags.TabIndex = 30;
            _lblTags.Text = "Tags:";
            // 
            // _btnApplyTags
            // 
            _btnApplyTags.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _btnApplyTags.AutoSize = true;
            _btnApplyTags.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _btnApplyTags.Location = new System.Drawing.Point(906, 141);
            _btnApplyTags.Name = "_btnApplyTags";
            _btnApplyTags.Size = new System.Drawing.Size(174, 30);
            _btnApplyTags.TabIndex = 27;
            _btnApplyTags.Text = "Apply Tags to Selection";
            _btnApplyTags.UseVisualStyleBackColor = true;
            // 
            // errorProvider1
            // 
            errorProvider1.ContainerControl = this;
            // 
            // errorProvider2
            // 
            errorProvider2.ContainerControl = this;
            // 
            // _cboAzureContext
            // 
            _cboAzureContext.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _cboAzureContext.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cboAzureContext.FormattingEnabled = true;
            _cboAzureContext.Location = new System.Drawing.Point(1270, 12);
            _cboAzureContext.Name = "_cboAzureContext";
            _cboAzureContext.Size = new System.Drawing.Size(198, 28);
            _cboAzureContext.TabIndex = 30;
            _cboAzureContext.SelectedValueChanged += ComboBox_AzureContext_SelectedValueChanged;
            // 
            // _lblAzureContext
            // 
            _lblAzureContext.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _lblAzureContext.AutoSize = true;
            _lblAzureContext.Location = new System.Drawing.Point(1162, 16);
            _lblAzureContext.Name = "_lblAzureContext";
            _lblAzureContext.Size = new System.Drawing.Size(105, 20);
            _lblAzureContext.TabIndex = 31;
            _lblAzureContext.Text = "Azure Context:";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(1482, 953);
            Controls.Add(_lblAzureContext);
            Controls.Add(_cboAzureContext);
            Controls.Add(_btnClearSearchQuery);
            Controls.Add(_lnkResourceGraphDocs);
            Controls.Add(_pnlQuickFilters);
            Controls.Add(_pnlQueryButtons);
            Controls.Add(_lblResultsCount);
            Controls.Add(_queryActivityIndicator);
            Controls.Add(_lblQueryMode);
            Controls.Add(_lblSearchResults);
            Controls.Add(_lblSearchQuery);
            Controls.Add(_cboRecentSearches);
            Controls.Add(_txtSearchQuery);
            Controls.Add(_splitContainer);
            DoubleBuffered = true;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(1290, 930);
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "AzTagger";
            Load += Form_Load;
            ResizeEnd += Form_ResizeEnd;
            SizeChanged += Form_SizeChanged;
            _pnlQueryButtons.ResumeLayout(false);
            _pnlQueryButtons.PerformLayout();
            _pnlQuickFilters.ResumeLayout(false);
            _pnlQuickFilters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_gvwTags).EndInit();
            _splitContainer.Panel1.ResumeLayout(false);
            _splitContainer.Panel2.ResumeLayout(false);
            _splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
            _splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_gvwResults).EndInit();
            ((System.ComponentModel.ISupportInitialize)errorProvider1).EndInit();
            ((System.ComponentModel.ISupportInitialize)errorProvider2).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.ToolTip _toolTip;
        private System.Windows.Forms.LinkLabel _lnkDotNetRegExDocs;
        private System.Windows.Forms.LinkLabel _lnkResourceGraphDocs;
        private System.Windows.Forms.Button _btnSaveQuery;
        private System.Windows.Forms.ComboBox _cboSavedQueries;
        private System.Windows.Forms.Button _btnClearSearchQuery;
        private System.Windows.Forms.SplitContainer _splitContainer;
        private System.Windows.Forms.ProgressBar _resultsActivityIndicator;
        private System.Windows.Forms.Label _lblCopyPasteHint;
        private System.Windows.Forms.Label _lblResultsFilteredCount;
        private System.Windows.Forms.DataGridView _gvwResults;
        private System.Windows.Forms.Label _lblVersion;
        private System.Windows.Forms.LinkLabel _lnkDonation;
        private System.Windows.Forms.LinkLabel _lnkGitHubLink;
        private System.Windows.Forms.LinkLabel _lnkEditTagTemplates;
        private System.Windows.Forms.Label _lblTags;
        private System.Windows.Forms.ComboBox _cboTagTemplates;
        private System.Windows.Forms.DataGridView _gvwTags;
        private System.Windows.Forms.Button _btnApplyTags;
        private System.Windows.Forms.DataGridViewTextBoxColumn Key;
        private System.Windows.Forms.DataGridViewTextBoxColumn Value;
        private System.Windows.Forms.LinkLabel _lnkResetToDefaults;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.ErrorProvider errorProvider2;
        private System.Windows.Forms.ComboBox _cboAzureContext;
        private System.Windows.Forms.Label _lblAzureContext;
    }
}
