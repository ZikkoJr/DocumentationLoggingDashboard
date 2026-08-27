namespace DocumentationLoggingDashboard;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel mainLayoutPanel;
    private Label titleLabel;
    private Label privacyReminderLabel;
    private TableLayoutPanel documentationFolderLayoutPanel;
    private Label documentationRootFolderLabel;
    private Label documentationRootFolderPathLabel;
    private Button changeLogsFolderButton;
    private Button resetDefaultFolderButton;
    private TableLayoutPanel logTypeLayoutPanel;
    private Label logTypeLabel;
    private ComboBox logTypeComboBox;
    private FlowLayoutPanel qaActionFlowLayoutPanel;
    private Button detailedQaReportButton;
    private Button quickQaButton;
    private Button manageQaHotelsPmsButton;
    private TableLayoutPanel runningWorkbookLayoutPanel;
    private Label runningWorkbookLabel;
    private ComboBox runningWorkbookComboBox;
    private Button createNewLogFileButton;
    private SplitContainer contentSplitContainer;
    private Panel fieldsScrollPanel;
    private TableLayoutPanel fieldsTableLayoutPanel;
    private Label previewLabel;
    private TextBox previewTextBox;
    private FlowLayoutPanel buttonFlowLayoutPanel;
    private Button previewEntryButton;
    private Button submitEntryButton;
    private Button clearFormButton;
    private Button openSelectedLogWorkbookButton;
    private Button openLogsFolderButton;
    private Button openLogIndexButton;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        mainLayoutPanel = new TableLayoutPanel();
        titleLabel = new Label();
        privacyReminderLabel = new Label();
        documentationFolderLayoutPanel = new TableLayoutPanel();
        documentationRootFolderLabel = new Label();
        documentationRootFolderPathLabel = new Label();
        changeLogsFolderButton = new Button();
        resetDefaultFolderButton = new Button();
        logTypeLayoutPanel = new TableLayoutPanel();
        logTypeLabel = new Label();
        logTypeComboBox = new ComboBox();
        qaActionFlowLayoutPanel = new FlowLayoutPanel();
        detailedQaReportButton = new Button();
        quickQaButton = new Button();
        manageQaHotelsPmsButton = new Button();
        runningWorkbookLayoutPanel = new TableLayoutPanel();
        runningWorkbookLabel = new Label();
        runningWorkbookComboBox = new ComboBox();
        createNewLogFileButton = new Button();
        contentSplitContainer = new SplitContainer();
        fieldsScrollPanel = new Panel();
        fieldsTableLayoutPanel = new TableLayoutPanel();
        previewLabel = new Label();
        previewTextBox = new TextBox();
        buttonFlowLayoutPanel = new FlowLayoutPanel();
        previewEntryButton = new Button();
        submitEntryButton = new Button();
        clearFormButton = new Button();
        openSelectedLogWorkbookButton = new Button();
        openLogsFolderButton = new Button();
        openLogIndexButton = new Button();
        mainLayoutPanel.SuspendLayout();
        documentationFolderLayoutPanel.SuspendLayout();
        logTypeLayoutPanel.SuspendLayout();
        qaActionFlowLayoutPanel.SuspendLayout();
        runningWorkbookLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)contentSplitContainer).BeginInit();
        contentSplitContainer.Panel1.SuspendLayout();
        contentSplitContainer.Panel2.SuspendLayout();
        contentSplitContainer.SuspendLayout();
        fieldsScrollPanel.SuspendLayout();
        buttonFlowLayoutPanel.SuspendLayout();
        SuspendLayout();
        // 
        // mainLayoutPanel
        // 
        mainLayoutPanel.ColumnCount = 1;
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.Controls.Add(titleLabel, 0, 0);
        mainLayoutPanel.Controls.Add(privacyReminderLabel, 0, 1);
        mainLayoutPanel.Controls.Add(documentationFolderLayoutPanel, 0, 2);
        mainLayoutPanel.Controls.Add(logTypeLayoutPanel, 0, 3);
        mainLayoutPanel.Controls.Add(runningWorkbookLayoutPanel, 0, 4);
        mainLayoutPanel.Controls.Add(contentSplitContainer, 0, 5);
        mainLayoutPanel.Controls.Add(buttonFlowLayoutPanel, 0, 6);
        mainLayoutPanel.Dock = DockStyle.Fill;
        mainLayoutPanel.Location = new Point(0, 0);
        mainLayoutPanel.Name = "mainLayoutPanel";
        mainLayoutPanel.Padding = new Padding(16);
        mainLayoutPanel.RowCount = 7;
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        mainLayoutPanel.Size = new Size(1100, 720);
        mainLayoutPanel.TabIndex = 0;
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        titleLabel.Location = new Point(19, 16);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(1062, 48);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Documentation Logging Dashboard";
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // privacyReminderLabel
        // 
        privacyReminderLabel.Dock = DockStyle.Fill;
        privacyReminderLabel.ForeColor = SystemColors.ControlDarkDark;
        privacyReminderLabel.Location = new Point(19, 64);
        privacyReminderLabel.Name = "privacyReminderLabel";
        privacyReminderLabel.Size = new Size(1062, 54);
        privacyReminderLabel.TabIndex = 1;
        privacyReminderLabel.Text = "Reminder: Do not enter guest names, emails, payment data, credentials, or full hotel files into logs. Use ticket IDs, hotel IDs, script names, and summarized issues instead.";
        privacyReminderLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // documentationFolderLayoutPanel
        // 
        documentationFolderLayoutPanel.ColumnCount = 4;
        documentationFolderLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 178F));
        documentationFolderLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        documentationFolderLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        documentationFolderLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        documentationFolderLayoutPanel.Controls.Add(documentationRootFolderLabel, 0, 0);
        documentationFolderLayoutPanel.Controls.Add(documentationRootFolderPathLabel, 1, 0);
        documentationFolderLayoutPanel.Controls.Add(changeLogsFolderButton, 2, 0);
        documentationFolderLayoutPanel.Controls.Add(resetDefaultFolderButton, 3, 0);
        documentationFolderLayoutPanel.Dock = DockStyle.Fill;
        documentationFolderLayoutPanel.Location = new Point(19, 121);
        documentationFolderLayoutPanel.Name = "documentationFolderLayoutPanel";
        documentationFolderLayoutPanel.RowCount = 1;
        documentationFolderLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        documentationFolderLayoutPanel.Size = new Size(1062, 42);
        documentationFolderLayoutPanel.TabIndex = 2;
        // 
        // documentationRootFolderLabel
        // 
        documentationRootFolderLabel.AutoSize = true;
        documentationRootFolderLabel.Dock = DockStyle.Fill;
        documentationRootFolderLabel.Location = new Point(3, 0);
        documentationRootFolderLabel.Name = "documentationRootFolderLabel";
        documentationRootFolderLabel.Size = new Size(172, 42);
        documentationRootFolderLabel.TabIndex = 0;
        documentationRootFolderLabel.Text = "Documentation Root Folder:";
        documentationRootFolderLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // documentationRootFolderPathLabel
        // 
        documentationRootFolderPathLabel.AutoEllipsis = true;
        documentationRootFolderPathLabel.BorderStyle = BorderStyle.FixedSingle;
        documentationRootFolderPathLabel.Dock = DockStyle.Fill;
        documentationRootFolderPathLabel.Location = new Point(181, 7);
        documentationRootFolderPathLabel.Margin = new Padding(3, 7, 8, 7);
        documentationRootFolderPathLabel.Name = "documentationRootFolderPathLabel";
        documentationRootFolderPathLabel.Padding = new Padding(6, 0, 6, 0);
        documentationRootFolderPathLabel.Size = new Size(553, 28);
        documentationRootFolderPathLabel.TabIndex = 1;
        documentationRootFolderPathLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // changeLogsFolderButton
        // 
        changeLogsFolderButton.Location = new Point(745, 6);
        changeLogsFolderButton.Margin = new Padding(3, 6, 8, 3);
        changeLogsFolderButton.Name = "changeLogsFolderButton";
        changeLogsFolderButton.Size = new Size(136, 30);
        changeLogsFolderButton.TabIndex = 2;
        changeLogsFolderButton.Text = "Change Logs Folder";
        changeLogsFolderButton.UseVisualStyleBackColor = true;
        // 
        // resetDefaultFolderButton
        // 
        resetDefaultFolderButton.Location = new Point(895, 6);
        resetDefaultFolderButton.Margin = new Padding(3, 6, 3, 3);
        resetDefaultFolderButton.Name = "resetDefaultFolderButton";
        resetDefaultFolderButton.Size = new Size(158, 30);
        resetDefaultFolderButton.TabIndex = 3;
        resetDefaultFolderButton.Text = "Reset to Default Folder";
        resetDefaultFolderButton.UseVisualStyleBackColor = true;
        // 
        // logTypeLayoutPanel
        // 
        logTypeLayoutPanel.ColumnCount = 3;
        logTypeLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        logTypeLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
        logTypeLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        logTypeLayoutPanel.Controls.Add(logTypeLabel, 0, 0);
        logTypeLayoutPanel.Controls.Add(logTypeComboBox, 1, 0);
        logTypeLayoutPanel.Controls.Add(qaActionFlowLayoutPanel, 2, 0);
        logTypeLayoutPanel.Dock = DockStyle.Fill;
        logTypeLayoutPanel.Location = new Point(19, 169);
        logTypeLayoutPanel.Name = "logTypeLayoutPanel";
        logTypeLayoutPanel.RowCount = 1;
        logTypeLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        logTypeLayoutPanel.Size = new Size(1062, 38);
        logTypeLayoutPanel.TabIndex = 2;
        // 
        // logTypeLabel
        // 
        logTypeLabel.AutoSize = true;
        logTypeLabel.Dock = DockStyle.Fill;
        logTypeLabel.Location = new Point(3, 0);
        logTypeLabel.Name = "logTypeLabel";
        logTypeLabel.Size = new Size(86, 38);
        logTypeLabel.TabIndex = 0;
        logTypeLabel.Text = "Log Type";
        logTypeLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // logTypeComboBox
        // 
        logTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        logTypeComboBox.FormattingEnabled = true;
        logTypeComboBox.Location = new Point(95, 8);
        logTypeComboBox.Margin = new Padding(3, 8, 3, 3);
        logTypeComboBox.Name = "logTypeComboBox";
        logTypeComboBox.Size = new Size(240, 23);
        logTypeComboBox.TabIndex = 1;
        //
        // qaActionFlowLayoutPanel
        //
        qaActionFlowLayoutPanel.Controls.Add(manageQaHotelsPmsButton);
        qaActionFlowLayoutPanel.Controls.Add(quickQaButton);
        qaActionFlowLayoutPanel.Controls.Add(detailedQaReportButton);
        qaActionFlowLayoutPanel.Dock = DockStyle.Fill;
        qaActionFlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
        qaActionFlowLayoutPanel.Location = new Point(352, 0);
        qaActionFlowLayoutPanel.Margin = new Padding(0);
        qaActionFlowLayoutPanel.Name = "qaActionFlowLayoutPanel";
        qaActionFlowLayoutPanel.Size = new Size(710, 38);
        qaActionFlowLayoutPanel.TabIndex = 2;
        qaActionFlowLayoutPanel.WrapContents = false;
        //
        // detailedQaReportButton
        //
        detailedQaReportButton.Location = new Point(335, 4);
        detailedQaReportButton.Margin = new Padding(3, 4, 3, 3);
        detailedQaReportButton.Name = "detailedQaReportButton";
        detailedQaReportButton.Size = new Size(160, 30);
        detailedQaReportButton.TabIndex = 1;
        detailedQaReportButton.Text = "Detailed QA Report";
        detailedQaReportButton.UseVisualStyleBackColor = true;
        //
        // quickQaButton
        //
        quickQaButton.Location = new Point(501, 4);
        quickQaButton.Margin = new Padding(3, 4, 3, 3);
        quickQaButton.Name = "quickQaButton";
        quickQaButton.Size = new Size(100, 30);
        quickQaButton.TabIndex = 0;
        quickQaButton.Text = "Quick QA";
        quickQaButton.UseVisualStyleBackColor = true;
        //
        // manageQaHotelsPmsButton
        //
        manageQaHotelsPmsButton.Location = new Point(607, 4);
        manageQaHotelsPmsButton.Margin = new Padding(3, 4, 3, 3);
        manageQaHotelsPmsButton.Name = "manageQaHotelsPmsButton";
        manageQaHotelsPmsButton.Size = new Size(180, 30);
        manageQaHotelsPmsButton.TabIndex = 2;
        manageQaHotelsPmsButton.Text = "Manage QA Hotels / PMS";
        manageQaHotelsPmsButton.UseVisualStyleBackColor = true;
        //
        // runningWorkbookLayoutPanel
        //
        runningWorkbookLayoutPanel.ColumnCount = 3;
        runningWorkbookLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 178F));
        runningWorkbookLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        runningWorkbookLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        runningWorkbookLayoutPanel.Controls.Add(runningWorkbookLabel, 0, 0);
        runningWorkbookLayoutPanel.Controls.Add(runningWorkbookComboBox, 1, 0);
        runningWorkbookLayoutPanel.Controls.Add(createNewLogFileButton, 2, 0);
        runningWorkbookLayoutPanel.Dock = DockStyle.Fill;
        runningWorkbookLayoutPanel.Location = new Point(19, 213);
        runningWorkbookLayoutPanel.Name = "runningWorkbookLayoutPanel";
        runningWorkbookLayoutPanel.RowCount = 1;
        runningWorkbookLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        runningWorkbookLayoutPanel.Size = new Size(1062, 42);
        runningWorkbookLayoutPanel.TabIndex = 3;
        //
        // runningWorkbookLabel
        //
        runningWorkbookLabel.AutoSize = true;
        runningWorkbookLabel.Dock = DockStyle.Fill;
        runningWorkbookLabel.Location = new Point(3, 0);
        runningWorkbookLabel.Name = "runningWorkbookLabel";
        runningWorkbookLabel.Size = new Size(172, 42);
        runningWorkbookLabel.TabIndex = 0;
        runningWorkbookLabel.Text = "Running Log Workbook:";
        runningWorkbookLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // runningWorkbookComboBox
        //
        runningWorkbookComboBox.AccessibleName = "Running documentation log workbook";
        runningWorkbookComboBox.Dock = DockStyle.Fill;
        runningWorkbookComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        runningWorkbookComboBox.FormattingEnabled = true;
        runningWorkbookComboBox.Location = new Point(181, 9);
        runningWorkbookComboBox.Margin = new Padding(3, 9, 8, 3);
        runningWorkbookComboBox.Name = "runningWorkbookComboBox";
        runningWorkbookComboBox.Size = new Size(695, 23);
        runningWorkbookComboBox.TabIndex = 1;
        //
        // createNewLogFileButton
        //
        createNewLogFileButton.Location = new Point(887, 6);
        createNewLogFileButton.Margin = new Padding(3, 6, 3, 3);
        createNewLogFileButton.Name = "createNewLogFileButton";
        createNewLogFileButton.Size = new Size(170, 30);
        createNewLogFileButton.TabIndex = 2;
        createNewLogFileButton.Text = "Create New Log File";
        createNewLogFileButton.UseVisualStyleBackColor = true;
        // 
        // contentSplitContainer
        // 
        contentSplitContainer.Dock = DockStyle.Fill;
        contentSplitContainer.Location = new Point(19, 261);
        contentSplitContainer.Name = "contentSplitContainer";
        // 
        // contentSplitContainer.Panel1
        // 
        contentSplitContainer.Panel1.Controls.Add(fieldsScrollPanel);
        contentSplitContainer.Panel1MinSize = 400;
        // 
        // contentSplitContainer.Panel2
        // 
        contentSplitContainer.Panel2.Controls.Add(previewTextBox);
        contentSplitContainer.Panel2.Controls.Add(previewLabel);
        contentSplitContainer.Panel2MinSize = 300;
        contentSplitContainer.Size = new Size(1062, 388);
        contentSplitContainer.SplitterDistance = 512;
        contentSplitContainer.TabIndex = 2;
        // 
        // fieldsScrollPanel
        // 
        fieldsScrollPanel.AutoScroll = true;
        fieldsScrollPanel.Controls.Add(fieldsTableLayoutPanel);
        fieldsScrollPanel.Dock = DockStyle.Fill;
        fieldsScrollPanel.Location = new Point(0, 0);
        fieldsScrollPanel.Name = "fieldsScrollPanel";
        fieldsScrollPanel.Padding = new Padding(0, 0, 12, 0);
        fieldsScrollPanel.Size = new Size(512, 388);
        fieldsScrollPanel.TabIndex = 0;
        // 
        // fieldsTableLayoutPanel
        // 
        fieldsTableLayoutPanel.AutoSize = true;
        fieldsTableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        fieldsTableLayoutPanel.ColumnCount = 2;
        fieldsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        fieldsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        fieldsTableLayoutPanel.Dock = DockStyle.Top;
        fieldsTableLayoutPanel.Location = new Point(0, 0);
        fieldsTableLayoutPanel.Name = "fieldsTableLayoutPanel";
        fieldsTableLayoutPanel.RowCount = 1;
        fieldsTableLayoutPanel.RowStyles.Add(new RowStyle());
        fieldsTableLayoutPanel.Size = new Size(500, 0);
        fieldsTableLayoutPanel.TabIndex = 0;
        // 
        // previewLabel
        // 
        previewLabel.AutoSize = true;
        previewLabel.Dock = DockStyle.Top;
        previewLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        previewLabel.Location = new Point(0, 0);
        previewLabel.Name = "previewLabel";
        previewLabel.Padding = new Padding(0, 0, 0, 8);
        previewLabel.Size = new Size(96, 27);
        previewLabel.TabIndex = 0;
        previewLabel.Text = "Entry Preview";
        // 
        // previewTextBox
        // 
        previewTextBox.Dock = DockStyle.Fill;
        previewTextBox.Location = new Point(0, 27);
        previewTextBox.Multiline = true;
        previewTextBox.Name = "previewTextBox";
        previewTextBox.ReadOnly = true;
        previewTextBox.ScrollBars = ScrollBars.Vertical;
        previewTextBox.Size = new Size(546, 361);
        previewTextBox.TabIndex = 1;
        // 
        // buttonFlowLayoutPanel
        // 
        buttonFlowLayoutPanel.Controls.Add(previewEntryButton);
        buttonFlowLayoutPanel.Controls.Add(submitEntryButton);
        buttonFlowLayoutPanel.Controls.Add(clearFormButton);
        buttonFlowLayoutPanel.Controls.Add(openSelectedLogWorkbookButton);
        buttonFlowLayoutPanel.Controls.Add(openLogsFolderButton);
        buttonFlowLayoutPanel.Controls.Add(openLogIndexButton);
        buttonFlowLayoutPanel.Dock = DockStyle.Fill;
        buttonFlowLayoutPanel.Location = new Point(19, 655);
        buttonFlowLayoutPanel.Name = "buttonFlowLayoutPanel";
        buttonFlowLayoutPanel.Size = new Size(1062, 46);
        buttonFlowLayoutPanel.TabIndex = 3;
        buttonFlowLayoutPanel.WrapContents = false;
        // 
        // previewEntryButton
        // 
        previewEntryButton.Location = new Point(3, 8);
        previewEntryButton.Margin = new Padding(3, 8, 8, 3);
        previewEntryButton.Name = "previewEntryButton";
        previewEntryButton.Size = new Size(110, 30);
        previewEntryButton.TabIndex = 0;
        previewEntryButton.Text = "Preview Entry";
        previewEntryButton.UseVisualStyleBackColor = true;
        // 
        // submitEntryButton
        // 
        submitEntryButton.Location = new Point(124, 8);
        submitEntryButton.Margin = new Padding(3, 8, 8, 3);
        submitEntryButton.Name = "submitEntryButton";
        submitEntryButton.Size = new Size(110, 30);
        submitEntryButton.TabIndex = 1;
        submitEntryButton.Text = "Submit Entry";
        submitEntryButton.UseVisualStyleBackColor = true;
        // 
        // clearFormButton
        // 
        clearFormButton.Location = new Point(245, 8);
        clearFormButton.Margin = new Padding(3, 8, 8, 3);
        clearFormButton.Name = "clearFormButton";
        clearFormButton.Size = new Size(95, 30);
        clearFormButton.TabIndex = 2;
        clearFormButton.Text = "Clear Form";
        clearFormButton.UseVisualStyleBackColor = true;
        // 
        // openSelectedLogWorkbookButton
        // 
        openSelectedLogWorkbookButton.Location = new Point(351, 8);
        openSelectedLogWorkbookButton.Margin = new Padding(3, 8, 8, 3);
        openSelectedLogWorkbookButton.Name = "openSelectedLogWorkbookButton";
        openSelectedLogWorkbookButton.Size = new Size(190, 30);
        openSelectedLogWorkbookButton.TabIndex = 3;
        openSelectedLogWorkbookButton.Text = "Open Selected Log Workbook";
        openSelectedLogWorkbookButton.UseVisualStyleBackColor = true;
        // 
        // openLogsFolderButton
        // 
        openLogsFolderButton.Location = new Point(552, 8);
        openLogsFolderButton.Margin = new Padding(3, 8, 8, 3);
        openLogsFolderButton.Name = "openLogsFolderButton";
        openLogsFolderButton.Size = new Size(125, 30);
        openLogsFolderButton.TabIndex = 4;
        openLogsFolderButton.Text = "Open Logs Folder";
        openLogsFolderButton.UseVisualStyleBackColor = true;
        // 
        // openLogIndexButton
        // 
        openLogIndexButton.Location = new Point(688, 8);
        openLogIndexButton.Margin = new Padding(3, 8, 8, 3);
        openLogIndexButton.Name = "openLogIndexButton";
        openLogIndexButton.Size = new Size(125, 30);
        openLogIndexButton.TabIndex = 5;
        openLogIndexButton.Text = "Open Log Index";
        openLogIndexButton.UseVisualStyleBackColor = true;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 720);
        Controls.Add(mainLayoutPanel);
        MinimumSize = new Size(900, 600);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Documentation Logging Dashboard";
        mainLayoutPanel.ResumeLayout(false);
        mainLayoutPanel.PerformLayout();
        documentationFolderLayoutPanel.ResumeLayout(false);
        documentationFolderLayoutPanel.PerformLayout();
        logTypeLayoutPanel.ResumeLayout(false);
        logTypeLayoutPanel.PerformLayout();
        qaActionFlowLayoutPanel.ResumeLayout(false);
        runningWorkbookLayoutPanel.ResumeLayout(false);
        runningWorkbookLayoutPanel.PerformLayout();
        contentSplitContainer.Panel1.ResumeLayout(false);
        contentSplitContainer.Panel2.ResumeLayout(false);
        contentSplitContainer.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)contentSplitContainer).EndInit();
        contentSplitContainer.ResumeLayout(false);
        fieldsScrollPanel.ResumeLayout(false);
        fieldsScrollPanel.PerformLayout();
        buttonFlowLayoutPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
