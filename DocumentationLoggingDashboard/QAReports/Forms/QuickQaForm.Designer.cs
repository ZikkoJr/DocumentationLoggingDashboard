using DocumentationLoggingDashboard.QAReports.Forms.Controls;

#nullable disable

namespace DocumentationLoggingDashboard.QAReports.Forms;

partial class QuickQaForm
{
    private System.ComponentModel.IContainer components = null;
    private Panel contentScrollPanel;
    private TableLayoutPanel contentLayoutPanel;
    private Label titleLabel;
    private GroupBox reportDetailsGroupBox;
    private TableLayoutPanel reportDetailsLayoutPanel;
    private QaHotelSelectorControl hotelSelectorControl;
    private Label hotelNameLabel;
    private TextBox hotelNameTextBox;
    private Label hotelIdLabel;
    private TextBox hotelIdTextBox;
    private Label pmsLabel;
    private TextBox pmsTextBox;
    private Label fileMonthLabel;
    private DateTimePicker fileMonthPicker;
    private Label fileIdLabel;
    private TextBox fileIdTextBox;
    private Label surfaceWorkbookLabel;
    private ComboBox surfaceWorkbookComboBox;
    private Button createSurfaceWorkbookButton;
    private Label workbookStatusLabel;
    private GroupBox checklistGroupBox;
    private FlowLayoutPanel checklistFlowLayoutPanel;
    private GroupBox customScriptGroupBox;
    private FlowLayoutPanel customScriptFlowLayoutPanel;
    private RadioButton customScriptAvailableRadioButton;
    private RadioButton noCustomScriptAvailableRadioButton;
    private GroupBox findingsGroupBox;
    private FlowLayoutPanel findingsFlowLayoutPanel;
    private Label noFindingsLabel;
    private GroupBox resultGroupBox;
    private Label resultValueLabel;
    private GroupBox summaryGroupBox;
    private TableLayoutPanel summaryLayoutPanel;
    private Label privacyReminderLabel;
    private TextBox summaryTextBox;
    private Label summaryStaleLabel;
    private Button regenerateSummaryButton;
    private FlowLayoutPanel actionFlowLayoutPanel;
    private Button saveQuickQaButton;
    private Button closeButton;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        contentScrollPanel = new Panel();
        contentLayoutPanel = new TableLayoutPanel();
        titleLabel = new Label();
        reportDetailsGroupBox = new GroupBox();
        reportDetailsLayoutPanel = new TableLayoutPanel();
        hotelSelectorControl = new QaHotelSelectorControl();
        hotelNameLabel = new Label();
        hotelNameTextBox = new TextBox();
        hotelIdLabel = new Label();
        hotelIdTextBox = new TextBox();
        pmsLabel = new Label();
        pmsTextBox = new TextBox();
        fileMonthLabel = new Label();
        fileMonthPicker = new DateTimePicker();
        fileIdLabel = new Label();
        fileIdTextBox = new TextBox();
        surfaceWorkbookLabel = new Label();
        surfaceWorkbookComboBox = new ComboBox();
        createSurfaceWorkbookButton = new Button();
        workbookStatusLabel = new Label();
        checklistGroupBox = new GroupBox();
        checklistFlowLayoutPanel = new FlowLayoutPanel();
        customScriptGroupBox = new GroupBox();
        customScriptFlowLayoutPanel = new FlowLayoutPanel();
        customScriptAvailableRadioButton = new RadioButton();
        noCustomScriptAvailableRadioButton = new RadioButton();
        findingsGroupBox = new GroupBox();
        findingsFlowLayoutPanel = new FlowLayoutPanel();
        noFindingsLabel = new Label();
        resultGroupBox = new GroupBox();
        resultValueLabel = new Label();
        summaryGroupBox = new GroupBox();
        summaryLayoutPanel = new TableLayoutPanel();
        privacyReminderLabel = new Label();
        summaryTextBox = new TextBox();
        summaryStaleLabel = new Label();
        regenerateSummaryButton = new Button();
        actionFlowLayoutPanel = new FlowLayoutPanel();
        saveQuickQaButton = new Button();
        closeButton = new Button();
        contentScrollPanel.SuspendLayout();
        contentLayoutPanel.SuspendLayout();
        reportDetailsGroupBox.SuspendLayout();
        reportDetailsLayoutPanel.SuspendLayout();
        checklistGroupBox.SuspendLayout();
        customScriptGroupBox.SuspendLayout();
        customScriptFlowLayoutPanel.SuspendLayout();
        findingsGroupBox.SuspendLayout();
        findingsFlowLayoutPanel.SuspendLayout();
        resultGroupBox.SuspendLayout();
        summaryGroupBox.SuspendLayout();
        summaryLayoutPanel.SuspendLayout();
        actionFlowLayoutPanel.SuspendLayout();
        SuspendLayout();

        contentScrollPanel.AutoScroll = true;
        contentScrollPanel.Controls.Add(contentLayoutPanel);
        contentScrollPanel.Dock = DockStyle.Fill;
        contentScrollPanel.Padding = new Padding(16);
        contentScrollPanel.TabIndex = 0;

        contentLayoutPanel.AutoSize = true;
        contentLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        contentLayoutPanel.ColumnCount = 1;
        contentLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        contentLayoutPanel.Controls.Add(titleLabel, 0, 0);
        contentLayoutPanel.Controls.Add(reportDetailsGroupBox, 0, 1);
        contentLayoutPanel.Controls.Add(checklistGroupBox, 0, 2);
        contentLayoutPanel.Controls.Add(customScriptGroupBox, 0, 3);
        contentLayoutPanel.Controls.Add(findingsGroupBox, 0, 4);
        contentLayoutPanel.Controls.Add(resultGroupBox, 0, 5);
        contentLayoutPanel.Controls.Add(summaryGroupBox, 0, 6);
        contentLayoutPanel.Controls.Add(actionFlowLayoutPanel, 0, 7);
        contentLayoutPanel.Dock = DockStyle.Top;
        contentLayoutPanel.RowCount = 8;
        contentLayoutPanel.TabIndex = 0;

        titleLabel.AutoSize = true;
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        titleLabel.Margin = new Padding(0, 0, 0, 12);
        titleLabel.Text = "Quick QA";

        reportDetailsGroupBox.AutoSize = true;
        reportDetailsGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        reportDetailsGroupBox.Controls.Add(reportDetailsLayoutPanel);
        reportDetailsGroupBox.Dock = DockStyle.Top;
        reportDetailsGroupBox.Margin = new Padding(0, 0, 0, 12);
        reportDetailsGroupBox.Padding = new Padding(10);
        reportDetailsGroupBox.Text = "Report Details";

        reportDetailsLayoutPanel.AutoSize = true;
        reportDetailsLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        reportDetailsLayoutPanel.ColumnCount = 4;
        reportDetailsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        reportDetailsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        reportDetailsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        reportDetailsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        reportDetailsLayoutPanel.Controls.Add(hotelSelectorControl, 0, 0);
        reportDetailsLayoutPanel.Controls.Add(hotelNameLabel, 0, 1);
        reportDetailsLayoutPanel.Controls.Add(hotelNameTextBox, 1, 1);
        reportDetailsLayoutPanel.Controls.Add(hotelIdLabel, 2, 1);
        reportDetailsLayoutPanel.Controls.Add(hotelIdTextBox, 3, 1);
        reportDetailsLayoutPanel.Controls.Add(pmsLabel, 0, 2);
        reportDetailsLayoutPanel.Controls.Add(pmsTextBox, 1, 2);
        reportDetailsLayoutPanel.Controls.Add(fileMonthLabel, 2, 2);
        reportDetailsLayoutPanel.Controls.Add(fileMonthPicker, 3, 2);
        reportDetailsLayoutPanel.Controls.Add(fileIdLabel, 0, 3);
        reportDetailsLayoutPanel.Controls.Add(fileIdTextBox, 1, 3);
        reportDetailsLayoutPanel.Controls.Add(surfaceWorkbookLabel, 0, 4);
        reportDetailsLayoutPanel.Controls.Add(surfaceWorkbookComboBox, 1, 4);
        reportDetailsLayoutPanel.Controls.Add(createSurfaceWorkbookButton, 3, 4);
        reportDetailsLayoutPanel.Controls.Add(workbookStatusLabel, 1, 5);
        reportDetailsLayoutPanel.Dock = DockStyle.Top;
        reportDetailsLayoutPanel.RowCount = 6;
        reportDetailsLayoutPanel.SetColumnSpan(hotelSelectorControl, 4);
        reportDetailsLayoutPanel.SetColumnSpan(fileIdTextBox, 3);
        reportDetailsLayoutPanel.SetColumnSpan(surfaceWorkbookComboBox, 2);
        reportDetailsLayoutPanel.SetColumnSpan(workbookStatusLabel, 3);

        hotelSelectorControl.Dock = DockStyle.Fill;
        hotelSelectorControl.Margin = new Padding(3, 3, 3, 10);
        hotelSelectorControl.MinimumSize = new Size(640, 210);
        hotelSelectorControl.Size = new Size(900, 230);

        ConfigureDetailsLabel(hotelNameLabel, "Hotel Name");
        ConfigureReadOnlyTextBox(hotelNameTextBox, "Canonical hotel name");
        ConfigureDetailsLabel(hotelIdLabel, "Hotel ID");
        ConfigureReadOnlyTextBox(hotelIdTextBox, "Canonical Hotel ID");
        ConfigureDetailsLabel(pmsLabel, "PMS");
        ConfigureReadOnlyTextBox(pmsTextBox, "Canonical PMS");
        ConfigureDetailsLabel(fileMonthLabel, "File Month *");
        ConfigureDetailsLabel(fileIdLabel, "File ID *");
        ConfigureDetailsLabel(surfaceWorkbookLabel, "Surface Workbook *");

        fileMonthPicker.CustomFormat = "yyyy-MM";
        fileMonthPicker.Format = DateTimePickerFormat.Custom;
        fileMonthPicker.ShowUpDown = true;
        fileMonthPicker.Dock = DockStyle.Fill;
        fileMonthPicker.Margin = new Padding(3, 3, 3, 8);

        fileIdTextBox.Dock = DockStyle.Fill;
        fileIdTextBox.AccessibleName = "File ID";
        fileIdTextBox.Margin = new Padding(3, 3, 3, 8);

        surfaceWorkbookComboBox.Dock = DockStyle.Fill;
        surfaceWorkbookComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        surfaceWorkbookComboBox.AccessibleName = "Surface QA workbook";
        surfaceWorkbookComboBox.Margin = new Padding(3, 3, 8, 3);

        createSurfaceWorkbookButton.AutoSize = true;
        createSurfaceWorkbookButton.Dock = DockStyle.Top;
        createSurfaceWorkbookButton.Margin = new Padding(3);
        createSurfaceWorkbookButton.Text = "Create New Surface QA File";
        createSurfaceWorkbookButton.UseVisualStyleBackColor = true;

        workbookStatusLabel.AutoSize = true;
        workbookStatusLabel.Dock = DockStyle.Fill;
        workbookStatusLabel.ForeColor = SystemColors.GrayText;
        workbookStatusLabel.Margin = new Padding(3, 4, 3, 4);
        workbookStatusLabel.Text = "Select or create a compatible workbook in the SurfaceQA folder.";

        checklistGroupBox.AutoSize = true;
        checklistGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        checklistGroupBox.Controls.Add(checklistFlowLayoutPanel);
        checklistGroupBox.Dock = DockStyle.Top;
        checklistGroupBox.Margin = new Padding(0, 0, 0, 12);
        checklistGroupBox.Padding = new Padding(10);
        checklistGroupBox.Text = "Checklist";

        checklistFlowLayoutPanel.AutoSize = true;
        checklistFlowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        checklistFlowLayoutPanel.Dock = DockStyle.Top;
        checklistFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        checklistFlowLayoutPanel.WrapContents = false;
        checklistFlowLayoutPanel.SizeChanged += (_, _) =>
            ResizeStackedChildren(checklistFlowLayoutPanel);

        customScriptGroupBox.AutoSize = true;
        customScriptGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        customScriptGroupBox.Controls.Add(customScriptFlowLayoutPanel);
        customScriptGroupBox.Dock = DockStyle.Top;
        customScriptGroupBox.Margin = new Padding(0, 0, 0, 12);
        customScriptGroupBox.Padding = new Padding(10);
        customScriptGroupBox.Text = "Custom Script";

        customScriptFlowLayoutPanel.AutoSize = true;
        customScriptFlowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        customScriptFlowLayoutPanel.Controls.Add(customScriptAvailableRadioButton);
        customScriptFlowLayoutPanel.Controls.Add(noCustomScriptAvailableRadioButton);
        customScriptFlowLayoutPanel.Dock = DockStyle.Top;
        customScriptFlowLayoutPanel.WrapContents = false;

        customScriptAvailableRadioButton.AutoSize = true;
        customScriptAvailableRadioButton.Margin = new Padding(3, 3, 18, 3);
        customScriptAvailableRadioButton.Text = "Custom Script Available: Yes";
        customScriptAvailableRadioButton.UseVisualStyleBackColor = true;
        noCustomScriptAvailableRadioButton.AutoSize = true;
        noCustomScriptAvailableRadioButton.Checked = true;
        noCustomScriptAvailableRadioButton.Text = "No";
        noCustomScriptAvailableRadioButton.UseVisualStyleBackColor = true;

        findingsGroupBox.AutoSize = true;
        findingsGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        findingsGroupBox.Controls.Add(findingsFlowLayoutPanel);
        findingsGroupBox.Dock = DockStyle.Top;
        findingsGroupBox.Margin = new Padding(0, 0, 0, 12);
        findingsGroupBox.Padding = new Padding(10);
        findingsGroupBox.Text = "Warnings and Failures";

        findingsFlowLayoutPanel.AutoSize = true;
        findingsFlowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        findingsFlowLayoutPanel.Controls.Add(noFindingsLabel);
        findingsFlowLayoutPanel.Dock = DockStyle.Top;
        findingsFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        findingsFlowLayoutPanel.WrapContents = false;
        findingsFlowLayoutPanel.SizeChanged += (_, _) =>
            ResizeStackedChildren(findingsFlowLayoutPanel);

        noFindingsLabel.AutoSize = true;
        noFindingsLabel.Margin = new Padding(3, 6, 3, 6);
        noFindingsLabel.Text = "No warnings or failures.";

        resultGroupBox.AutoSize = true;
        resultGroupBox.Controls.Add(resultValueLabel);
        resultGroupBox.Dock = DockStyle.Top;
        resultGroupBox.Margin = new Padding(0, 0, 0, 12);
        resultGroupBox.Padding = new Padding(10);
        resultGroupBox.Text = "Result";

        resultValueLabel.AutoSize = true;
        resultValueLabel.Dock = DockStyle.Fill;
        resultValueLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        resultValueLabel.Margin = new Padding(3, 5, 3, 5);
        resultValueLabel.Text = "Pass";

        summaryGroupBox.AutoSize = true;
        summaryGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        summaryGroupBox.Controls.Add(summaryLayoutPanel);
        summaryGroupBox.Dock = DockStyle.Top;
        summaryGroupBox.Margin = new Padding(0, 0, 0, 12);
        summaryGroupBox.Padding = new Padding(10);
        summaryGroupBox.Text = "Summary";

        summaryLayoutPanel.AutoSize = true;
        summaryLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        summaryLayoutPanel.ColumnCount = 2;
        summaryLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        summaryLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        summaryLayoutPanel.Controls.Add(privacyReminderLabel, 0, 0);
        summaryLayoutPanel.Controls.Add(summaryTextBox, 0, 1);
        summaryLayoutPanel.Controls.Add(summaryStaleLabel, 0, 2);
        summaryLayoutPanel.Controls.Add(regenerateSummaryButton, 1, 2);
        summaryLayoutPanel.Dock = DockStyle.Top;
        summaryLayoutPanel.RowCount = 3;
        summaryLayoutPanel.SetColumnSpan(privacyReminderLabel, 2);
        summaryLayoutPanel.SetColumnSpan(summaryTextBox, 2);

        privacyReminderLabel.AutoSize = true;
        privacyReminderLabel.Dock = DockStyle.Fill;
        privacyReminderLabel.ForeColor = SystemColors.ControlDarkDark;
        privacyReminderLabel.Margin = new Padding(3, 0, 3, 8);
        privacyReminderLabel.Text = "Do not enter guest names, guest emails, payment information, credentials, confirmation-level PII, or copied raw rows. Describe the QA issue at field/check level.";

        summaryTextBox.AcceptsReturn = true;
        summaryTextBox.Dock = DockStyle.Fill;
        summaryTextBox.MinimumSize = new Size(0, 110);
        summaryTextBox.Multiline = true;
        summaryTextBox.ScrollBars = ScrollBars.Vertical;
        summaryTextBox.WordWrap = true;

        summaryStaleLabel.AutoSize = true;
        summaryStaleLabel.Dock = DockStyle.Fill;
        summaryStaleLabel.ForeColor = Color.DarkRed;
        summaryStaleLabel.Margin = new Padding(3, 8, 8, 3);
        summaryStaleLabel.Text = "Summary is out of date. Regenerate it before saving.";
        summaryStaleLabel.Visible = false;

        regenerateSummaryButton.AutoSize = true;
        regenerateSummaryButton.Margin = new Padding(3, 6, 3, 3);
        regenerateSummaryButton.Text = "Regenerate Summary";
        regenerateSummaryButton.UseVisualStyleBackColor = true;

        actionFlowLayoutPanel.AutoSize = true;
        actionFlowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        actionFlowLayoutPanel.Controls.Add(saveQuickQaButton);
        actionFlowLayoutPanel.Controls.Add(closeButton);
        actionFlowLayoutPanel.Dock = DockStyle.Top;
        actionFlowLayoutPanel.Margin = new Padding(0, 0, 0, 16);
        actionFlowLayoutPanel.WrapContents = false;

        saveQuickQaButton.AutoSize = true;
        saveQuickQaButton.Margin = new Padding(3, 3, 10, 3);
        saveQuickQaButton.Padding = new Padding(12, 2, 12, 2);
        saveQuickQaButton.Text = "Save Quick QA";
        saveQuickQaButton.UseVisualStyleBackColor = true;
        closeButton.AutoSize = true;
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Padding = new Padding(12, 2, 12, 2);
        closeButton.Text = "Close";
        closeButton.UseVisualStyleBackColor = true;

        AcceptButton = saveQuickQaButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = closeButton;
        ClientSize = new Size(1040, 820);
        Controls.Add(contentScrollPanel);
        MinimumSize = new Size(820, 620);
        Name = "QuickQaForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Quick QA";
        contentScrollPanel.ResumeLayout(false);
        contentScrollPanel.PerformLayout();
        contentLayoutPanel.ResumeLayout(false);
        contentLayoutPanel.PerformLayout();
        reportDetailsGroupBox.ResumeLayout(false);
        reportDetailsGroupBox.PerformLayout();
        reportDetailsLayoutPanel.ResumeLayout(false);
        reportDetailsLayoutPanel.PerformLayout();
        checklistGroupBox.ResumeLayout(false);
        checklistGroupBox.PerformLayout();
        customScriptGroupBox.ResumeLayout(false);
        customScriptGroupBox.PerformLayout();
        customScriptFlowLayoutPanel.ResumeLayout(false);
        customScriptFlowLayoutPanel.PerformLayout();
        findingsGroupBox.ResumeLayout(false);
        findingsGroupBox.PerformLayout();
        findingsFlowLayoutPanel.ResumeLayout(false);
        findingsFlowLayoutPanel.PerformLayout();
        resultGroupBox.ResumeLayout(false);
        resultGroupBox.PerformLayout();
        summaryGroupBox.ResumeLayout(false);
        summaryGroupBox.PerformLayout();
        summaryLayoutPanel.ResumeLayout(false);
        summaryLayoutPanel.PerformLayout();
        actionFlowLayoutPanel.ResumeLayout(false);
        actionFlowLayoutPanel.PerformLayout();
        ResumeLayout(false);
    }

    private static void ConfigureDetailsLabel(Label label, string text)
    {
        label.AutoSize = true;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(3, 6, 8, 8);
        label.Text = text;
        label.TextAlign = ContentAlignment.TopLeft;
    }

    private static void ConfigureReadOnlyTextBox(
        TextBox textBox,
        string accessibleName)
    {
        textBox.AccessibleName = accessibleName;
        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(3, 3, 3, 8);
        textBox.ReadOnly = true;
        textBox.TabStop = false;
    }
}
