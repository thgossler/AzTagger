// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

namespace AzTagger
{
    public partial class MainForm
    {
        private System.Windows.Forms.Button _btnApplyTags;

        private System.Windows.Forms.Button _btnCopyQuery;

        private System.Windows.Forms.Button _btnPerformSearch;

        private System.Windows.Forms.Button _btnRefreshSignin;

        private System.Windows.Forms.ComboBox _cboQuickFilter1Column;

        private System.Windows.Forms.ComboBox _cboQuickFilter2Column;

        private System.Windows.Forms.ComboBox _cboRecentSearches;

        private System.Windows.Forms.ComboBox _cboTagTemplates;

        private System.Windows.Forms.DataGridView _gvwResults;

        private System.Windows.Forms.DataGridView _gvwTags;

        private System.Windows.Forms.Label _lblCopyPasteHint;

        private System.Windows.Forms.Label _lblQueryMode;

        private System.Windows.Forms.Label _lblQuickFiltersLabel;

        private System.Windows.Forms.Label _lblRecentQueries;

        private System.Windows.Forms.Label _lblResultsCount;

        private System.Windows.Forms.Label _lblResultsFilteredCount;

        private System.Windows.Forms.Label _lblSearchQuery;

        private System.Windows.Forms.Label _lblSearchResults;

        private System.Windows.Forms.Label _lblTags;

        private System.Windows.Forms.Label _lblTagTemplates;

        private System.Windows.Forms.Label _lblVersion;

        private System.Windows.Forms.LinkLabel _lnkDonation;

        private System.Windows.Forms.LinkLabel _lnkEditTagTemplates;

        private System.Windows.Forms.LinkLabel _lnkGitHubLink;

        private System.Windows.Forms.LinkLabel _lnkResetQuickFilters;

        private System.Windows.Forms.FlowLayoutPanel _pnlQueryButtons;

        private System.Windows.Forms.FlowLayoutPanel _pnlQuickFilters;

        private System.Windows.Forms.ProgressBar _queryActivityIndicator;

        private System.Windows.Forms.ProgressBar _resultsActivityIndicator;

        private System.Windows.Forms.TextBox _txtQuickFilter1Text;

        private System.Windows.Forms.TextBox _txtQuickFilter2Text;

        private System.Windows.Forms.TextBox _txtSearchQuery;

        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.DataGridViewTextBoxColumn Key;

        private System.Windows.Forms.DataGridViewTextBoxColumn Value;

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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            _txtSearchQuery = new System.Windows.Forms.TextBox();
            _btnPerformSearch = new System.Windows.Forms.Button();
            _gvwResults = new System.Windows.Forms.DataGridView();
            _btnApplyTags = new System.Windows.Forms.Button();
            _gvwTags = new System.Windows.Forms.DataGridView();
            Key = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _cboTagTemplates = new System.Windows.Forms.ComboBox();
            _cboRecentSearches = new System.Windows.Forms.ComboBox();
            _lblRecentQueries = new System.Windows.Forms.Label();
            _lblSearchQuery = new System.Windows.Forms.Label();
            _lblSearchResults = new System.Windows.Forms.Label();
            _lblTags = new System.Windows.Forms.Label();
            _lblTagTemplates = new System.Windows.Forms.Label();
            _lblQueryMode = new System.Windows.Forms.Label();
            _queryActivityIndicator = new System.Windows.Forms.ProgressBar();
            _lblResultsCount = new System.Windows.Forms.Label();
            _btnCopyQuery = new System.Windows.Forms.Button();
            _pnlQueryButtons = new System.Windows.Forms.FlowLayoutPanel();
            _btnRefreshSignin = new System.Windows.Forms.Button();
            _pnlQuickFilters = new System.Windows.Forms.FlowLayoutPanel();
            _lblQuickFiltersLabel = new System.Windows.Forms.Label();
            _cboQuickFilter1Column = new System.Windows.Forms.ComboBox();
            _txtQuickFilter1Text = new System.Windows.Forms.TextBox();
            _cboQuickFilter2Column = new System.Windows.Forms.ComboBox();
            _txtQuickFilter2Text = new System.Windows.Forms.TextBox();
            _lnkResetQuickFilters = new System.Windows.Forms.LinkLabel();
            _lblResultsFilteredCount = new System.Windows.Forms.Label();
            _lnkEditTagTemplates = new System.Windows.Forms.LinkLabel();
            _lnkGitHubLink = new System.Windows.Forms.LinkLabel();
            _lblCopyPasteHint = new System.Windows.Forms.Label();
            _resultsActivityIndicator = new System.Windows.Forms.ProgressBar();
            _lnkDonation = new System.Windows.Forms.LinkLabel();
            _lblVersion = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)_gvwResults).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_gvwTags).BeginInit();
            _pnlQueryButtons.SuspendLayout();
            _pnlQuickFilters.SuspendLayout();
            SuspendLayout();
            // 
            // _txtSearchQuery
            // 
            _txtSearchQuery.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _txtSearchQuery.HideSelection = false;
            _txtSearchQuery.Location = new System.Drawing.Point(12, 79);
            _txtSearchQuery.Multiline = true;
            _txtSearchQuery.Name = "_txtSearchQuery";
            _txtSearchQuery.Size = new System.Drawing.Size(1458, 69);
            _txtSearchQuery.TabIndex = 2;
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
            _btnPerformSearch.UseVisualStyleBackColor = true;
            _btnPerformSearch.Click += Button_PerformSearch_Click;
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
            _gvwResults.Location = new System.Drawing.Point(12, 240);
            _gvwResults.Name = "_gvwResults";
            _gvwResults.ReadOnly = true;
            _gvwResults.RowHeadersWidth = 51;
            _gvwResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _gvwResults.ShowCellErrors = false;
            _gvwResults.ShowEditingIcon = false;
            _gvwResults.ShowRowErrors = false;
            _gvwResults.Size = new System.Drawing.Size(1458, 413);
            _gvwResults.StandardTab = true;
            _gvwResults.TabIndex = 4;
            // 
            // _btnApplyTags
            // 
            _btnApplyTags.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _btnApplyTags.AutoSize = true;
            _btnApplyTags.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _btnApplyTags.Location = new System.Drawing.Point(907, 806);
            _btnApplyTags.Name = "_btnApplyTags";
            _btnApplyTags.Size = new System.Drawing.Size(174, 30);
            _btnApplyTags.TabIndex = 5;
            _btnApplyTags.Text = "Apply Tags to Selection";
            _btnApplyTags.UseVisualStyleBackColor = true;
            _btnApplyTags.Click += Button_ApplyTags_Click;
            // 
            // _gvwTags
            // 
            _gvwTags.AllowUserToResizeRows = false;
            _gvwTags.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _gvwTags.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gvwTags.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Key, Value });
            _gvwTags.Location = new System.Drawing.Point(12, 697);
            _gvwTags.Name = "_gvwTags";
            _gvwTags.RowHeadersWidth = 51;
            _gvwTags.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _gvwTags.Size = new System.Drawing.Size(864, 244);
            _gvwTags.TabIndex = 6;
            // 
            // Key
            // 
            Key.HeaderText = "Key";
            Key.MaxInputLength = 255;
            Key.MinimumWidth = 6;
            Key.Name = "Key";
            Key.Width = 300;
            // 
            // Value
            // 
            Value.HeaderText = "Value";
            Value.MaxInputLength = 255;
            Value.MinimumWidth = 6;
            Value.Name = "Value";
            Value.Width = 490;
            // 
            // _cboTagTemplates
            // 
            _cboTagTemplates.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _cboTagTemplates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cboTagTemplates.FormattingEnabled = true;
            _cboTagTemplates.Location = new System.Drawing.Point(907, 720);
            _cboTagTemplates.Name = "_cboTagTemplates";
            _cboTagTemplates.Size = new System.Drawing.Size(244, 28);
            _cboTagTemplates.TabIndex = 7;
            _cboTagTemplates.SelectedIndexChanged += ComboBox_TagTemplates_SelectedIndexChanged;
            // 
            // _cboRecentSearches
            // 
            _cboRecentSearches.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cboRecentSearches.FormattingEnabled = true;
            _cboRecentSearches.Location = new System.Drawing.Point(129, 12);
            _cboRecentSearches.MaxDropDownItems = 10;
            _cboRecentSearches.Name = "_cboRecentSearches";
            _cboRecentSearches.Size = new System.Drawing.Size(1121, 28);
            _cboRecentSearches.TabIndex = 8;
            _cboRecentSearches.SelectedIndexChanged += ComboBox_RecentSearches_SelectedIndexChanged;
            // 
            // _lblRecentQueries
            // 
            _lblRecentQueries.AutoSize = true;
            _lblRecentQueries.Location = new System.Drawing.Point(12, 15);
            _lblRecentQueries.Name = "_lblRecentQueries";
            _lblRecentQueries.Size = new System.Drawing.Size(111, 20);
            _lblRecentQueries.TabIndex = 9;
            _lblRecentQueries.Text = "Recent Queries:";
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
            // _lblTags
            // 
            _lblTags.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _lblTags.AutoSize = true;
            _lblTags.Location = new System.Drawing.Point(12, 674);
            _lblTags.Name = "_lblTags";
            _lblTags.Size = new System.Drawing.Size(41, 20);
            _lblTags.TabIndex = 12;
            _lblTags.Text = "Tags:";
            // 
            // _lblTagTemplates
            // 
            _lblTagTemplates.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _lblTagTemplates.AutoSize = true;
            _lblTagTemplates.Location = new System.Drawing.Point(907, 697);
            _lblTagTemplates.Name = "_lblTagTemplates";
            _lblTagTemplates.Size = new System.Drawing.Size(107, 20);
            _lblTagTemplates.TabIndex = 13;
            _lblTagTemplates.Text = "Tag Templates:";
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
            _queryActivityIndicator.Location = new System.Drawing.Point(12, 148);
            _queryActivityIndicator.MarqueeAnimationSpeed = 20;
            _queryActivityIndicator.Name = "_queryActivityIndicator";
            _queryActivityIndicator.Size = new System.Drawing.Size(1459, 2);
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
            _btnCopyQuery.UseVisualStyleBackColor = true;
            _btnCopyQuery.Click += Button_CopyQuery_Click;
            // 
            // _pnlQueryButtons
            // 
            _pnlQueryButtons.Controls.Add(_btnPerformSearch);
            _pnlQueryButtons.Controls.Add(_btnRefreshSignin);
            _pnlQueryButtons.Controls.Add(_btnCopyQuery);
            _pnlQueryButtons.Location = new System.Drawing.Point(12, 154);
            _pnlQueryButtons.Name = "_pnlQueryButtons";
            _pnlQueryButtons.Size = new System.Drawing.Size(384, 45);
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
            _btnRefreshSignin.UseVisualStyleBackColor = true;
            _btnRefreshSignin.Click += Button_RefreshSignin_Click;
            // 
            // _pnlQuickFilters
            // 
            _pnlQuickFilters.Controls.Add(_lblQuickFiltersLabel);
            _pnlQuickFilters.Controls.Add(_cboQuickFilter1Column);
            _pnlQuickFilters.Controls.Add(_txtQuickFilter1Text);
            _pnlQuickFilters.Controls.Add(_cboQuickFilter2Column);
            _pnlQuickFilters.Controls.Add(_txtQuickFilter2Text);
            _pnlQuickFilters.Controls.Add(_lnkResetQuickFilters);
            _pnlQuickFilters.Location = new System.Drawing.Point(166, 205);
            _pnlQuickFilters.Name = "_pnlQuickFilters";
            _pnlQuickFilters.Size = new System.Drawing.Size(915, 35);
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
            // 
            // _txtQuickFilter1Text
            // 
            _txtQuickFilter1Text.Location = new System.Drawing.Point(323, 3);
            _txtQuickFilter1Text.MaxLength = 256;
            _txtQuickFilter1Text.Name = "_txtQuickFilter1Text";
            _txtQuickFilter1Text.Size = new System.Drawing.Size(115, 27);
            _txtQuickFilter1Text.TabIndex = 1;
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
            // 
            // _txtQuickFilter2Text
            // 
            _txtQuickFilter2Text.Location = new System.Drawing.Point(613, 3);
            _txtQuickFilter2Text.MaxLength = 256;
            _txtQuickFilter2Text.Name = "_txtQuickFilter2Text";
            _txtQuickFilter2Text.Size = new System.Drawing.Size(115, 27);
            _txtQuickFilter2Text.TabIndex = 4;
            // 
            // _lnkResetQuickFilters
            // 
            _lnkResetQuickFilters.Location = new System.Drawing.Point(746, 7);
            _lnkResetQuickFilters.Margin = new System.Windows.Forms.Padding(15, 7, 3, 0);
            _lnkResetQuickFilters.Name = "_lnkResetQuickFilters";
            _lnkResetQuickFilters.Size = new System.Drawing.Size(139, 25);
            _lnkResetQuickFilters.TabIndex = 5;
            _lnkResetQuickFilters.TabStop = true;
            _lnkResetQuickFilters.Text = "Reset Quick Filters";
            _lnkResetQuickFilters.VisitedLinkColor = System.Drawing.Color.Blue;
            _lnkResetQuickFilters.LinkClicked += LinkLabel_ResetQuickFilters_LinkClicked;
            // 
            // _lblResultsFilteredCount
            // 
            _lblResultsFilteredCount.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _lblResultsFilteredCount.Location = new System.Drawing.Point(998, 658);
            _lblResultsFilteredCount.Name = "_lblResultsFilteredCount";
            _lblResultsFilteredCount.Size = new System.Drawing.Size(472, 26);
            _lblResultsFilteredCount.TabIndex = 20;
            _lblResultsFilteredCount.Text = "(0 items)";
            _lblResultsFilteredCount.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // _lnkEditTagTemplates
            // 
            _lnkEditTagTemplates.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _lnkEditTagTemplates.Location = new System.Drawing.Point(1018, 751);
            _lnkEditTagTemplates.Name = "_lnkEditTagTemplates";
            _lnkEditTagTemplates.Size = new System.Drawing.Size(133, 25);
            _lnkEditTagTemplates.TabIndex = 21;
            _lnkEditTagTemplates.TabStop = true;
            _lnkEditTagTemplates.Text = "Edit Templates";
            _lnkEditTagTemplates.TextAlign = System.Drawing.ContentAlignment.TopRight;
            _lnkEditTagTemplates.VisitedLinkColor = System.Drawing.Color.Blue;
            _lnkEditTagTemplates.LinkClicked += LinkLabel_EditTagTemplates_LinkClicked;
            // 
            // _lnkGitHubLink
            // 
            _lnkGitHubLink.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _lnkGitHubLink.Location = new System.Drawing.Point(1266, 893);
            _lnkGitHubLink.Name = "_lnkGitHubLink";
            _lnkGitHubLink.Size = new System.Drawing.Size(202, 24);
            _lnkGitHubLink.TabIndex = 22;
            _lnkGitHubLink.TabStop = true;
            _lnkGitHubLink.Text = "Open Source on GitHub";
            _lnkGitHubLink.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            _lnkGitHubLink.VisitedLinkColor = System.Drawing.Color.Blue;
            _lnkGitHubLink.LinkClicked += LinkLabel_GitHubLink_LinkClicked;
            // 
            // _lblCopyPasteHint
            // 
            _lblCopyPasteHint.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblCopyPasteHint.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
            _lblCopyPasteHint.Location = new System.Drawing.Point(498, 658);
            _lblCopyPasteHint.Name = "_lblCopyPasteHint";
            _lblCopyPasteHint.Size = new System.Drawing.Size(487, 20);
            _lblCopyPasteHint.TabIndex = 23;
            _lblCopyPasteHint.Text = "(Use Ctrl+C to copy data into clipboard)";
            _lblCopyPasteHint.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // _resultsActivityIndicator
            // 
            _resultsActivityIndicator.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _resultsActivityIndicator.Location = new System.Drawing.Point(12, 652);
            _resultsActivityIndicator.MarqueeAnimationSpeed = 20;
            _resultsActivityIndicator.Name = "_resultsActivityIndicator";
            _resultsActivityIndicator.Size = new System.Drawing.Size(1459, 2);
            _resultsActivityIndicator.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            _resultsActivityIndicator.TabIndex = 24;
            _resultsActivityIndicator.Visible = false;
            // 
            // _lnkDonation
            // 
            _lnkDonation.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _lnkDonation.Location = new System.Drawing.Point(1224, 917);
            _lnkDonation.Name = "_lnkDonation";
            _lnkDonation.Size = new System.Drawing.Size(248, 24);
            _lnkDonation.TabIndex = 25;
            _lnkDonation.TabStop = true;
            _lnkDonation.Text = "Thumbs-up with a Donation 👍🏼";
            _lnkDonation.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            _lnkDonation.VisitedLinkColor = System.Drawing.Color.Blue;
            _lnkDonation.LinkClicked += LinkLabel_Donation_LinkClicked;
            // 
            // _lblVersion
            // 
            _lblVersion.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _lblVersion.AutoSize = true;
            _lblVersion.Location = new System.Drawing.Point(1130, 921);
            _lblVersion.Name = "_lblVersion";
            _lblVersion.Size = new System.Drawing.Size(91, 20);
            _lblVersion.TabIndex = 26;
            _lblVersion.Text = "Version: x.x.x";
            // 
            // MainForm
            // 
            ClientSize = new System.Drawing.Size(1482, 953);
            Controls.Add(_lblVersion);
            Controls.Add(_lnkDonation);
            Controls.Add(_resultsActivityIndicator);
            Controls.Add(_lblCopyPasteHint);
            Controls.Add(_lnkGitHubLink);
            Controls.Add(_lnkEditTagTemplates);
            Controls.Add(_lblResultsFilteredCount);
            Controls.Add(_pnlQuickFilters);
            Controls.Add(_pnlQueryButtons);
            Controls.Add(_lblResultsCount);
            Controls.Add(_queryActivityIndicator);
            Controls.Add(_lblQueryMode);
            Controls.Add(_lblTagTemplates);
            Controls.Add(_lblTags);
            Controls.Add(_lblSearchResults);
            Controls.Add(_lblSearchQuery);
            Controls.Add(_lblRecentQueries);
            Controls.Add(_cboRecentSearches);
            Controls.Add(_cboTagTemplates);
            Controls.Add(_gvwTags);
            Controls.Add(_btnApplyTags);
            Controls.Add(_gvwResults);
            Controls.Add(_txtSearchQuery);
            DoubleBuffered = true;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(1280, 930);
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "AzTagger";
            Load += Form_Load;
            ResizeEnd += Form_ResizeEnd;
            SizeChanged += Form_SizeChanged;
            ((System.ComponentModel.ISupportInitialize)_gvwResults).EndInit();
            ((System.ComponentModel.ISupportInitialize)_gvwTags).EndInit();
            _pnlQueryButtons.ResumeLayout(false);
            _pnlQueryButtons.PerformLayout();
            _pnlQuickFilters.ResumeLayout(false);
            _pnlQuickFilters.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
