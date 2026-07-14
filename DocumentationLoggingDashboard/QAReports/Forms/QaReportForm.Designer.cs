using DocumentationLoggingDashboard.QAReports.Forms.Controls;

namespace DocumentationLoggingDashboard.QAReports.Forms;

partial class QaReportForm
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel mainLayoutPanel;
    private Label privacyReminderLabel;
    private TabControl reportTabControl;
    private TabPage reportDetailsTabPage;
    private TabPage rawFileTabPage;
    private TabPage databaseTabPage;
    private TabPage statisticsReadinessTabPage;
    private TabPage findingsTabPage;
    private Panel reportDetailsScrollPanel;
    private TableLayoutPanel reportDetailsLayoutPanel;
    private GroupBox hotelGroupBox;
    private TableLayoutPanel hotelLayoutPanel;
    private QaHotelSelectorControl hotelSelectorControl;
    private TableLayoutPanel selectedHotelLayoutPanel;
    private Label selectedHotelIdLabel;
    private TextBox selectedHotelIdTextBox;
    private Label selectedPmsLabel;
    private TextBox selectedPmsTextBox;
    private Button manageHotelsPmsButton;
    private GroupBox reportInformationGroupBox;
    private TableLayoutPanel reportInformationLayoutPanel;
    private Label fileMonthLabel;
    private DateTimePicker fileMonthPicker;
    private Label qaDateLabel;
    private DateTimePicker qaDatePicker;
    private Label createdByLabel;
    private TextBox createdByTextBox;
    private Label originalFileNameLabel;
    private TextBox originalFileNameTextBox;
    private Label generalNotesLabel;
    private TextBox generalNotesTextBox;
    private GroupBox fileCharacteristicsGroupBox;
    private TableLayoutPanel fileCharacteristicsLayoutPanel;
    private GroupBox nameColumnModeGroupBox;
    private FlowLayoutPanel nameColumnModeFlowLayoutPanel;
    private RadioButton separateNameColumnsRadioButton;
    private RadioButton fullNameColumnRadioButton;
    private GroupBox currencyColumnGroupBox;
    private FlowLayoutPanel currencyColumnFlowLayoutPanel;
    private RadioButton currencyColumnFoundRadioButton;
    private RadioButton noCurrencyColumnRadioButton;
    private GroupBox monetaryScenarioGroupBox;
    private FlowLayoutPanel monetaryScenarioFlowLayoutPanel;
    private RadioButton oneMonetaryColumnRadioButton;
    private RadioButton twoMonetaryColumnsRadioButton;
    private RadioButton moreThanTwoMonetaryColumnsRadioButton;
    private Label moreThanTwoMonetaryColumnsNoteLabel;
    private GroupBox confirmationCandidatesGroupBox;
    private FlowLayoutPanel confirmationCandidatesFlowLayoutPanel;
    private RadioButton multipleConfirmationCandidatesRadioButton;
    private RadioButton noMultipleConfirmationCandidatesRadioButton;
    private GroupBox customScriptSupportGroupBox;
    private FlowLayoutPanel customScriptSupportFlowLayoutPanel;
    private RadioButton customScriptAvailableRadioButton;
    private RadioButton noCustomScriptAvailableRadioButton;
    private GroupBox rejectedRecordsGroupBox;
    private FlowLayoutPanel rejectedRecordsFlowLayoutPanel;
    private RadioButton rejectedRecordsExistRadioButton;
    private RadioButton noRejectedRecordsRadioButton;
    private FlowLayoutPanel rawChecklistFlowLayoutPanel;
    private FlowLayoutPanel databaseChecklistFlowLayoutPanel;
    private QaStatisticsControl statisticsControl;
    private TableLayoutPanel findingsLayoutPanel;
    private GroupBox warningsGroupBox;
    private FlowLayoutPanel warningsFlowLayoutPanel;
    private Label noWarningsLabel;
    private GroupBox failedChecksGroupBox;
    private FlowLayoutPanel failedChecksFlowLayoutPanel;
    private Label noFailedChecksLabel;
    private FlowLayoutPanel actionFlowLayoutPanel;
    private Button closeButton;
    private Button checkReportReadinessButton;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        mainLayoutPanel = new TableLayoutPanel();
        privacyReminderLabel = new Label();
        reportTabControl = new TabControl();
        reportDetailsTabPage = new TabPage();
        reportDetailsScrollPanel = new Panel();
        reportDetailsLayoutPanel = new TableLayoutPanel();
        hotelGroupBox = new GroupBox();
        hotelLayoutPanel = new TableLayoutPanel();
        hotelSelectorControl = new QaHotelSelectorControl();
        selectedHotelLayoutPanel = new TableLayoutPanel();
        selectedHotelIdLabel = new Label();
        selectedHotelIdTextBox = new TextBox();
        selectedPmsLabel = new Label();
        selectedPmsTextBox = new TextBox();
        manageHotelsPmsButton = new Button();
        reportInformationGroupBox = new GroupBox();
        reportInformationLayoutPanel = new TableLayoutPanel();
        fileMonthLabel = new Label();
        fileMonthPicker = new DateTimePicker();
        qaDateLabel = new Label();
        qaDatePicker = new DateTimePicker();
        createdByLabel = new Label();
        createdByTextBox = new TextBox();
        originalFileNameLabel = new Label();
        originalFileNameTextBox = new TextBox();
        generalNotesLabel = new Label();
        generalNotesTextBox = new TextBox();
        fileCharacteristicsGroupBox = new GroupBox();
        fileCharacteristicsLayoutPanel = new TableLayoutPanel();
        nameColumnModeGroupBox = new GroupBox();
        nameColumnModeFlowLayoutPanel = new FlowLayoutPanel();
        separateNameColumnsRadioButton = new RadioButton();
        fullNameColumnRadioButton = new RadioButton();
        currencyColumnGroupBox = new GroupBox();
        currencyColumnFlowLayoutPanel = new FlowLayoutPanel();
        currencyColumnFoundRadioButton = new RadioButton();
        noCurrencyColumnRadioButton = new RadioButton();
        monetaryScenarioGroupBox = new GroupBox();
        monetaryScenarioFlowLayoutPanel = new FlowLayoutPanel();
        oneMonetaryColumnRadioButton = new RadioButton();
        twoMonetaryColumnsRadioButton = new RadioButton();
        moreThanTwoMonetaryColumnsRadioButton = new RadioButton();
        moreThanTwoMonetaryColumnsNoteLabel = new Label();
        confirmationCandidatesGroupBox = new GroupBox();
        confirmationCandidatesFlowLayoutPanel = new FlowLayoutPanel();
        multipleConfirmationCandidatesRadioButton = new RadioButton();
        noMultipleConfirmationCandidatesRadioButton = new RadioButton();
        customScriptSupportGroupBox = new GroupBox();
        customScriptSupportFlowLayoutPanel = new FlowLayoutPanel();
        customScriptAvailableRadioButton = new RadioButton();
        noCustomScriptAvailableRadioButton = new RadioButton();
        rejectedRecordsGroupBox = new GroupBox();
        rejectedRecordsFlowLayoutPanel = new FlowLayoutPanel();
        rejectedRecordsExistRadioButton = new RadioButton();
        noRejectedRecordsRadioButton = new RadioButton();
        rawFileTabPage = new TabPage();
        rawChecklistFlowLayoutPanel = new FlowLayoutPanel();
        databaseTabPage = new TabPage();
        databaseChecklistFlowLayoutPanel = new FlowLayoutPanel();
        statisticsReadinessTabPage = new TabPage();
        statisticsControl = new QaStatisticsControl();
        findingsTabPage = new TabPage();
        findingsLayoutPanel = new TableLayoutPanel();
        warningsGroupBox = new GroupBox();
        warningsFlowLayoutPanel = new FlowLayoutPanel();
        noWarningsLabel = new Label();
        failedChecksGroupBox = new GroupBox();
        failedChecksFlowLayoutPanel = new FlowLayoutPanel();
        noFailedChecksLabel = new Label();
        actionFlowLayoutPanel = new FlowLayoutPanel();
        closeButton = new Button();
        checkReportReadinessButton = new Button();
        mainLayoutPanel.SuspendLayout();
        reportTabControl.SuspendLayout();
        reportDetailsTabPage.SuspendLayout();
        reportDetailsScrollPanel.SuspendLayout();
        reportDetailsLayoutPanel.SuspendLayout();
        hotelGroupBox.SuspendLayout();
        hotelLayoutPanel.SuspendLayout();
        selectedHotelLayoutPanel.SuspendLayout();
        reportInformationGroupBox.SuspendLayout();
        reportInformationLayoutPanel.SuspendLayout();
        fileCharacteristicsGroupBox.SuspendLayout();
        fileCharacteristicsLayoutPanel.SuspendLayout();
        nameColumnModeGroupBox.SuspendLayout();
        nameColumnModeFlowLayoutPanel.SuspendLayout();
        currencyColumnGroupBox.SuspendLayout();
        currencyColumnFlowLayoutPanel.SuspendLayout();
        monetaryScenarioGroupBox.SuspendLayout();
        monetaryScenarioFlowLayoutPanel.SuspendLayout();
        confirmationCandidatesGroupBox.SuspendLayout();
        confirmationCandidatesFlowLayoutPanel.SuspendLayout();
        customScriptSupportGroupBox.SuspendLayout();
        customScriptSupportFlowLayoutPanel.SuspendLayout();
        rejectedRecordsGroupBox.SuspendLayout();
        rejectedRecordsFlowLayoutPanel.SuspendLayout();
        rawFileTabPage.SuspendLayout();
        databaseTabPage.SuspendLayout();
        statisticsReadinessTabPage.SuspendLayout();
        findingsTabPage.SuspendLayout();
        findingsLayoutPanel.SuspendLayout();
        warningsGroupBox.SuspendLayout();
        warningsFlowLayoutPanel.SuspendLayout();
        failedChecksGroupBox.SuspendLayout();
        failedChecksFlowLayoutPanel.SuspendLayout();
        actionFlowLayoutPanel.SuspendLayout();
        SuspendLayout();
        //
        // mainLayoutPanel
        //
        mainLayoutPanel.ColumnCount = 1;
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.Controls.Add(privacyReminderLabel, 0, 0);
        mainLayoutPanel.Controls.Add(reportTabControl, 0, 1);
        mainLayoutPanel.Controls.Add(actionFlowLayoutPanel, 0, 2);
        mainLayoutPanel.Dock = DockStyle.Fill;
        mainLayoutPanel.Location = new Point(0, 0);
        mainLayoutPanel.Name = "mainLayoutPanel";
        mainLayoutPanel.Padding = new Padding(12);
        mainLayoutPanel.RowCount = 3;
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainLayoutPanel.Size = new Size(1060, 740);
        mainLayoutPanel.TabIndex = 0;
        //
        // privacyReminderLabel
        //
        privacyReminderLabel.Dock = DockStyle.Fill;
        privacyReminderLabel.ForeColor = SystemColors.ControlDarkDark;
        privacyReminderLabel.Location = new Point(15, 12);
        privacyReminderLabel.Name = "privacyReminderLabel";
        privacyReminderLabel.Padding = new Padding(6);
        privacyReminderLabel.Size = new Size(1030, 72);
        privacyReminderLabel.TabIndex = 0;
        privacyReminderLabel.Text = "Do not enter guest names, guest email addresses, reservation-level personal information, payment information, credentials, or full hotel-file contents. Use field names, counts, percentages, script names, and summarized conditions.";
        privacyReminderLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // reportTabControl
        //
        reportTabControl.Controls.Add(reportDetailsTabPage);
        reportTabControl.Controls.Add(rawFileTabPage);
        reportTabControl.Controls.Add(databaseTabPage);
        reportTabControl.Controls.Add(statisticsReadinessTabPage);
        reportTabControl.Controls.Add(findingsTabPage);
        reportTabControl.Dock = DockStyle.Fill;
        reportTabControl.Location = new Point(15, 87);
        reportTabControl.Name = "reportTabControl";
        reportTabControl.SelectedIndex = 0;
        reportTabControl.Size = new Size(1030, 590);
        reportTabControl.TabIndex = 1;
        //
        // reportDetailsTabPage
        //
        reportDetailsTabPage.Controls.Add(reportDetailsScrollPanel);
        reportDetailsTabPage.Location = new Point(4, 24);
        reportDetailsTabPage.Name = "reportDetailsTabPage";
        reportDetailsTabPage.Padding = new Padding(8);
        reportDetailsTabPage.Size = new Size(1022, 562);
        reportDetailsTabPage.TabIndex = 0;
        reportDetailsTabPage.Text = "Report Details";
        reportDetailsTabPage.UseVisualStyleBackColor = true;
        //
        // reportDetailsScrollPanel
        //
        reportDetailsScrollPanel.AutoScroll = true;
        reportDetailsScrollPanel.Controls.Add(reportDetailsLayoutPanel);
        reportDetailsScrollPanel.Dock = DockStyle.Fill;
        reportDetailsScrollPanel.Location = new Point(8, 8);
        reportDetailsScrollPanel.Name = "reportDetailsScrollPanel";
        reportDetailsScrollPanel.Padding = new Padding(0, 0, 8, 0);
        reportDetailsScrollPanel.Size = new Size(1006, 546);
        reportDetailsScrollPanel.TabIndex = 0;
        //
        // reportDetailsLayoutPanel
        //
        reportDetailsLayoutPanel.AutoSize = true;
        reportDetailsLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        reportDetailsLayoutPanel.ColumnCount = 1;
        reportDetailsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        reportDetailsLayoutPanel.Controls.Add(hotelGroupBox, 0, 0);
        reportDetailsLayoutPanel.Controls.Add(reportInformationGroupBox, 0, 1);
        reportDetailsLayoutPanel.Controls.Add(fileCharacteristicsGroupBox, 0, 2);
        reportDetailsLayoutPanel.Dock = DockStyle.Top;
        reportDetailsLayoutPanel.Location = new Point(0, 0);
        reportDetailsLayoutPanel.Name = "reportDetailsLayoutPanel";
        reportDetailsLayoutPanel.RowCount = 3;
        reportDetailsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 235F));
        reportDetailsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
        reportDetailsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        reportDetailsLayoutPanel.Size = new Size(998, 925);
        reportDetailsLayoutPanel.TabIndex = 0;
        //
        // hotelGroupBox
        //
        hotelGroupBox.Controls.Add(hotelLayoutPanel);
        hotelGroupBox.Dock = DockStyle.Fill;
        hotelGroupBox.Location = new Point(3, 3);
        hotelGroupBox.Name = "hotelGroupBox";
        hotelGroupBox.Padding = new Padding(10);
        hotelGroupBox.Size = new Size(992, 229);
        hotelGroupBox.TabIndex = 0;
        hotelGroupBox.TabStop = false;
        hotelGroupBox.Text = "Hotel";
        //
        // hotelLayoutPanel
        //
        hotelLayoutPanel.ColumnCount = 2;
        hotelLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        hotelLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        hotelLayoutPanel.Controls.Add(hotelSelectorControl, 0, 0);
        hotelLayoutPanel.Controls.Add(selectedHotelLayoutPanel, 1, 0);
        hotelLayoutPanel.Dock = DockStyle.Fill;
        hotelLayoutPanel.Location = new Point(10, 26);
        hotelLayoutPanel.Name = "hotelLayoutPanel";
        hotelLayoutPanel.RowCount = 1;
        hotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        hotelLayoutPanel.Size = new Size(972, 193);
        hotelLayoutPanel.TabIndex = 0;
        //
        // hotelSelectorControl
        //
        hotelSelectorControl.Dock = DockStyle.Fill;
        hotelSelectorControl.Location = new Point(3, 3);
        hotelSelectorControl.Name = "hotelSelectorControl";
        hotelSelectorControl.Size = new Size(557, 187);
        hotelSelectorControl.TabIndex = 0;
        //
        // selectedHotelLayoutPanel
        //
        selectedHotelLayoutPanel.ColumnCount = 2;
        selectedHotelLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        selectedHotelLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        selectedHotelLayoutPanel.Controls.Add(selectedHotelIdLabel, 0, 0);
        selectedHotelLayoutPanel.Controls.Add(selectedHotelIdTextBox, 1, 0);
        selectedHotelLayoutPanel.Controls.Add(selectedPmsLabel, 0, 1);
        selectedHotelLayoutPanel.Controls.Add(selectedPmsTextBox, 1, 1);
        selectedHotelLayoutPanel.Controls.Add(manageHotelsPmsButton, 1, 2);
        selectedHotelLayoutPanel.Dock = DockStyle.Fill;
        selectedHotelLayoutPanel.Location = new Point(566, 3);
        selectedHotelLayoutPanel.Name = "selectedHotelLayoutPanel";
        selectedHotelLayoutPanel.RowCount = 4;
        selectedHotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        selectedHotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        selectedHotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        selectedHotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        selectedHotelLayoutPanel.Size = new Size(403, 187);
        selectedHotelLayoutPanel.TabIndex = 1;
        //
        // selectedHotelIdLabel
        //
        selectedHotelIdLabel.AutoSize = true;
        selectedHotelIdLabel.Dock = DockStyle.Fill;
        selectedHotelIdLabel.Location = new Point(3, 0);
        selectedHotelIdLabel.Name = "selectedHotelIdLabel";
        selectedHotelIdLabel.Size = new Size(86, 38);
        selectedHotelIdLabel.TabIndex = 0;
        selectedHotelIdLabel.Text = "Hotel ID";
        selectedHotelIdLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // selectedHotelIdTextBox
        //
        selectedHotelIdTextBox.AccessibleName = "Selected hotel ID";
        selectedHotelIdTextBox.Dock = DockStyle.Fill;
        selectedHotelIdTextBox.Location = new Point(95, 7);
        selectedHotelIdTextBox.Margin = new Padding(3, 7, 3, 3);
        selectedHotelIdTextBox.Name = "selectedHotelIdTextBox";
        selectedHotelIdTextBox.ReadOnly = true;
        selectedHotelIdTextBox.Size = new Size(305, 23);
        selectedHotelIdTextBox.TabIndex = 1;
        //
        // selectedPmsLabel
        //
        selectedPmsLabel.AutoSize = true;
        selectedPmsLabel.Dock = DockStyle.Fill;
        selectedPmsLabel.Location = new Point(3, 38);
        selectedPmsLabel.Name = "selectedPmsLabel";
        selectedPmsLabel.Size = new Size(86, 38);
        selectedPmsLabel.TabIndex = 2;
        selectedPmsLabel.Text = "PMS";
        selectedPmsLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // selectedPmsTextBox
        //
        selectedPmsTextBox.AccessibleName = "Selected hotel's canonical PMS";
        selectedPmsTextBox.Dock = DockStyle.Fill;
        selectedPmsTextBox.Location = new Point(95, 45);
        selectedPmsTextBox.Margin = new Padding(3, 7, 3, 3);
        selectedPmsTextBox.Name = "selectedPmsTextBox";
        selectedPmsTextBox.ReadOnly = true;
        selectedPmsTextBox.Size = new Size(305, 23);
        selectedPmsTextBox.TabIndex = 3;
        //
        // manageHotelsPmsButton
        //
        manageHotelsPmsButton.Anchor = AnchorStyles.Left;
        manageHotelsPmsButton.Location = new Point(95, 83);
        manageHotelsPmsButton.Name = "manageHotelsPmsButton";
        manageHotelsPmsButton.Size = new Size(164, 30);
        manageHotelsPmsButton.TabIndex = 4;
        manageHotelsPmsButton.Text = "Manage Hotels / PMS";
        manageHotelsPmsButton.UseVisualStyleBackColor = true;
        //
        // reportInformationGroupBox
        //
        reportInformationGroupBox.Controls.Add(reportInformationLayoutPanel);
        reportInformationGroupBox.Dock = DockStyle.Fill;
        reportInformationGroupBox.Location = new Point(3, 238);
        reportInformationGroupBox.Name = "reportInformationGroupBox";
        reportInformationGroupBox.Padding = new Padding(10);
        reportInformationGroupBox.Size = new Size(992, 244);
        reportInformationGroupBox.TabIndex = 1;
        reportInformationGroupBox.TabStop = false;
        reportInformationGroupBox.Text = "Report Information";
        //
        // reportInformationLayoutPanel
        //
        reportInformationLayoutPanel.ColumnCount = 2;
        reportInformationLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        reportInformationLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        reportInformationLayoutPanel.Controls.Add(fileMonthLabel, 0, 0);
        reportInformationLayoutPanel.Controls.Add(fileMonthPicker, 1, 0);
        reportInformationLayoutPanel.Controls.Add(qaDateLabel, 0, 1);
        reportInformationLayoutPanel.Controls.Add(qaDatePicker, 1, 1);
        reportInformationLayoutPanel.Controls.Add(createdByLabel, 0, 2);
        reportInformationLayoutPanel.Controls.Add(createdByTextBox, 1, 2);
        reportInformationLayoutPanel.Controls.Add(originalFileNameLabel, 0, 3);
        reportInformationLayoutPanel.Controls.Add(originalFileNameTextBox, 1, 3);
        reportInformationLayoutPanel.Controls.Add(generalNotesLabel, 0, 4);
        reportInformationLayoutPanel.Controls.Add(generalNotesTextBox, 1, 4);
        reportInformationLayoutPanel.Dock = DockStyle.Fill;
        reportInformationLayoutPanel.Location = new Point(10, 26);
        reportInformationLayoutPanel.Name = "reportInformationLayoutPanel";
        reportInformationLayoutPanel.RowCount = 5;
        reportInformationLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        reportInformationLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        reportInformationLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        reportInformationLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        reportInformationLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        reportInformationLayoutPanel.Size = new Size(972, 208);
        reportInformationLayoutPanel.TabIndex = 0;
        //
        // fileMonthLabel
        //
        fileMonthLabel.AutoSize = true;
        fileMonthLabel.Dock = DockStyle.Fill;
        fileMonthLabel.Location = new Point(3, 0);
        fileMonthLabel.Name = "fileMonthLabel";
        fileMonthLabel.Size = new Size(144, 36);
        fileMonthLabel.TabIndex = 0;
        fileMonthLabel.Text = "File Month";
        fileMonthLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // fileMonthPicker
        //
        fileMonthPicker.AccessibleName = "File month";
        fileMonthPicker.CustomFormat = "yyyy-MM";
        fileMonthPicker.Format = DateTimePickerFormat.Custom;
        fileMonthPicker.Location = new Point(153, 6);
        fileMonthPicker.Margin = new Padding(3, 6, 3, 3);
        fileMonthPicker.Name = "fileMonthPicker";
        fileMonthPicker.ShowUpDown = true;
        fileMonthPicker.Size = new Size(140, 23);
        fileMonthPicker.TabIndex = 1;
        //
        // qaDateLabel
        //
        qaDateLabel.AutoSize = true;
        qaDateLabel.Dock = DockStyle.Fill;
        qaDateLabel.Location = new Point(3, 36);
        qaDateLabel.Name = "qaDateLabel";
        qaDateLabel.Size = new Size(144, 36);
        qaDateLabel.TabIndex = 2;
        qaDateLabel.Text = "QA Date";
        qaDateLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // qaDatePicker
        //
        qaDatePicker.AccessibleName = "QA date";
        qaDatePicker.Format = DateTimePickerFormat.Short;
        qaDatePicker.Location = new Point(153, 42);
        qaDatePicker.Margin = new Padding(3, 6, 3, 3);
        qaDatePicker.Name = "qaDatePicker";
        qaDatePicker.Size = new Size(140, 23);
        qaDatePicker.TabIndex = 3;
        //
        // createdByLabel
        //
        createdByLabel.AutoSize = true;
        createdByLabel.Dock = DockStyle.Fill;
        createdByLabel.Location = new Point(3, 72);
        createdByLabel.Name = "createdByLabel";
        createdByLabel.Size = new Size(144, 36);
        createdByLabel.TabIndex = 4;
        createdByLabel.Text = "Created By (optional)";
        createdByLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // createdByTextBox
        //
        createdByTextBox.AccessibleName = "Created by, optional";
        createdByTextBox.Dock = DockStyle.Fill;
        createdByTextBox.Location = new Point(153, 78);
        createdByTextBox.Margin = new Padding(3, 6, 3, 3);
        createdByTextBox.Name = "createdByTextBox";
        createdByTextBox.Size = new Size(816, 23);
        createdByTextBox.TabIndex = 5;
        //
        // originalFileNameLabel
        //
        originalFileNameLabel.AutoSize = true;
        originalFileNameLabel.Dock = DockStyle.Fill;
        originalFileNameLabel.Location = new Point(3, 108);
        originalFileNameLabel.Name = "originalFileNameLabel";
        originalFileNameLabel.Size = new Size(144, 36);
        originalFileNameLabel.TabIndex = 6;
        originalFileNameLabel.Text = "Original Filename (optional)";
        originalFileNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // originalFileNameTextBox
        //
        originalFileNameTextBox.AccessibleName = "Original filename or label, optional";
        originalFileNameTextBox.Dock = DockStyle.Fill;
        originalFileNameTextBox.Location = new Point(153, 114);
        originalFileNameTextBox.Margin = new Padding(3, 6, 3, 3);
        originalFileNameTextBox.Name = "originalFileNameTextBox";
        originalFileNameTextBox.Size = new Size(816, 23);
        originalFileNameTextBox.TabIndex = 7;
        //
        // generalNotesLabel
        //
        generalNotesLabel.AutoSize = true;
        generalNotesLabel.Dock = DockStyle.Fill;
        generalNotesLabel.Location = new Point(3, 144);
        generalNotesLabel.Name = "generalNotesLabel";
        generalNotesLabel.Size = new Size(144, 64);
        generalNotesLabel.TabIndex = 8;
        generalNotesLabel.Text = "General Notes (optional)";
        generalNotesLabel.TextAlign = ContentAlignment.TopLeft;
        //
        // generalNotesTextBox
        //
        generalNotesTextBox.AccessibleName = "General QA notes, optional";
        generalNotesTextBox.Dock = DockStyle.Fill;
        generalNotesTextBox.Location = new Point(153, 147);
        generalNotesTextBox.Multiline = true;
        generalNotesTextBox.Name = "generalNotesTextBox";
        generalNotesTextBox.ScrollBars = ScrollBars.Vertical;
        generalNotesTextBox.Size = new Size(816, 58);
        generalNotesTextBox.TabIndex = 9;
        //
        // fileCharacteristicsGroupBox
        //
        fileCharacteristicsGroupBox.AutoSize = true;
        fileCharacteristicsGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        fileCharacteristicsGroupBox.Controls.Add(fileCharacteristicsLayoutPanel);
        fileCharacteristicsGroupBox.Dock = DockStyle.Top;
        fileCharacteristicsGroupBox.Location = new Point(3, 488);
        fileCharacteristicsGroupBox.Name = "fileCharacteristicsGroupBox";
        fileCharacteristicsGroupBox.Padding = new Padding(10);
        fileCharacteristicsGroupBox.Size = new Size(992, 434);
        fileCharacteristicsGroupBox.TabIndex = 2;
        fileCharacteristicsGroupBox.TabStop = false;
        fileCharacteristicsGroupBox.Text = "File Characteristics";
        //
        // fileCharacteristicsLayoutPanel
        //
        fileCharacteristicsLayoutPanel.AutoSize = true;
        fileCharacteristicsLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        fileCharacteristicsLayoutPanel.ColumnCount = 2;
        fileCharacteristicsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        fileCharacteristicsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        fileCharacteristicsLayoutPanel.Controls.Add(nameColumnModeGroupBox, 0, 0);
        fileCharacteristicsLayoutPanel.Controls.Add(currencyColumnGroupBox, 1, 0);
        fileCharacteristicsLayoutPanel.Controls.Add(monetaryScenarioGroupBox, 0, 1);
        fileCharacteristicsLayoutPanel.Controls.Add(confirmationCandidatesGroupBox, 1, 1);
        fileCharacteristicsLayoutPanel.Controls.Add(customScriptSupportGroupBox, 0, 2);
        fileCharacteristicsLayoutPanel.Controls.Add(rejectedRecordsGroupBox, 1, 2);
        fileCharacteristicsLayoutPanel.Dock = DockStyle.Top;
        fileCharacteristicsLayoutPanel.Location = new Point(10, 26);
        fileCharacteristicsLayoutPanel.Name = "fileCharacteristicsLayoutPanel";
        fileCharacteristicsLayoutPanel.RowCount = 3;
        fileCharacteristicsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fileCharacteristicsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fileCharacteristicsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fileCharacteristicsLayoutPanel.Size = new Size(972, 398);
        fileCharacteristicsLayoutPanel.TabIndex = 0;
        //
        // nameColumnModeGroupBox
        //
        nameColumnModeGroupBox.AutoSize = true;
        nameColumnModeGroupBox.Controls.Add(nameColumnModeFlowLayoutPanel);
        nameColumnModeGroupBox.Dock = DockStyle.Fill;
        nameColumnModeGroupBox.Location = new Point(3, 3);
        nameColumnModeGroupBox.MinimumSize = new Size(0, 100);
        nameColumnModeGroupBox.Name = "nameColumnModeGroupBox";
        nameColumnModeGroupBox.Padding = new Padding(8);
        nameColumnModeGroupBox.Size = new Size(480, 100);
        nameColumnModeGroupBox.TabIndex = 0;
        nameColumnModeGroupBox.TabStop = false;
        nameColumnModeGroupBox.Text = "Name-column mode";
        //
        // nameColumnModeFlowLayoutPanel
        //
        nameColumnModeFlowLayoutPanel.AutoSize = true;
        nameColumnModeFlowLayoutPanel.Controls.Add(separateNameColumnsRadioButton);
        nameColumnModeFlowLayoutPanel.Controls.Add(fullNameColumnRadioButton);
        nameColumnModeFlowLayoutPanel.Dock = DockStyle.Fill;
        nameColumnModeFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        nameColumnModeFlowLayoutPanel.Location = new Point(8, 24);
        nameColumnModeFlowLayoutPanel.Name = "nameColumnModeFlowLayoutPanel";
        nameColumnModeFlowLayoutPanel.Size = new Size(464, 68);
        nameColumnModeFlowLayoutPanel.TabIndex = 0;
        nameColumnModeFlowLayoutPanel.WrapContents = false;
        //
        // separateNameColumnsRadioButton
        //
        separateNameColumnsRadioButton.AutoSize = true;
        separateNameColumnsRadioButton.Checked = true;
        separateNameColumnsRadioButton.Location = new Point(3, 3);
        separateNameColumnsRadioButton.Name = "separateNameColumnsRadioButton";
        separateNameColumnsRadioButton.Size = new Size(213, 19);
        separateNameColumnsRadioButton.TabIndex = 0;
        separateNameColumnsRadioButton.TabStop = true;
        separateNameColumnsRadioButton.Text = "Separate First Name and Last Name";
        separateNameColumnsRadioButton.UseVisualStyleBackColor = true;
        //
        // fullNameColumnRadioButton
        //
        fullNameColumnRadioButton.AutoSize = true;
        fullNameColumnRadioButton.Location = new Point(3, 28);
        fullNameColumnRadioButton.Name = "fullNameColumnRadioButton";
        fullNameColumnRadioButton.Size = new Size(79, 19);
        fullNameColumnRadioButton.TabIndex = 1;
        fullNameColumnRadioButton.Text = "Full Name";
        fullNameColumnRadioButton.UseVisualStyleBackColor = true;
        //
        // currencyColumnGroupBox
        //
        currencyColumnGroupBox.AutoSize = true;
        currencyColumnGroupBox.Controls.Add(currencyColumnFlowLayoutPanel);
        currencyColumnGroupBox.Dock = DockStyle.Fill;
        currencyColumnGroupBox.Location = new Point(489, 3);
        currencyColumnGroupBox.MinimumSize = new Size(0, 100);
        currencyColumnGroupBox.Name = "currencyColumnGroupBox";
        currencyColumnGroupBox.Padding = new Padding(8);
        currencyColumnGroupBox.Size = new Size(480, 100);
        currencyColumnGroupBox.TabIndex = 1;
        currencyColumnGroupBox.TabStop = false;
        currencyColumnGroupBox.Text = "Currency column";
        //
        // currencyColumnFlowLayoutPanel
        //
        currencyColumnFlowLayoutPanel.AutoSize = true;
        currencyColumnFlowLayoutPanel.Controls.Add(currencyColumnFoundRadioButton);
        currencyColumnFlowLayoutPanel.Controls.Add(noCurrencyColumnRadioButton);
        currencyColumnFlowLayoutPanel.Dock = DockStyle.Fill;
        currencyColumnFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        currencyColumnFlowLayoutPanel.Location = new Point(8, 24);
        currencyColumnFlowLayoutPanel.Name = "currencyColumnFlowLayoutPanel";
        currencyColumnFlowLayoutPanel.Size = new Size(464, 68);
        currencyColumnFlowLayoutPanel.TabIndex = 0;
        currencyColumnFlowLayoutPanel.WrapContents = false;
        //
        // currencyColumnFoundRadioButton
        //
        currencyColumnFoundRadioButton.AutoSize = true;
        currencyColumnFoundRadioButton.Location = new Point(3, 3);
        currencyColumnFoundRadioButton.Name = "currencyColumnFoundRadioButton";
        currencyColumnFoundRadioButton.Size = new Size(147, 19);
        currencyColumnFoundRadioButton.TabIndex = 0;
        currencyColumnFoundRadioButton.Text = "Currency column found";
        currencyColumnFoundRadioButton.UseVisualStyleBackColor = true;
        //
        // noCurrencyColumnRadioButton
        //
        noCurrencyColumnRadioButton.AutoSize = true;
        noCurrencyColumnRadioButton.Checked = true;
        noCurrencyColumnRadioButton.Location = new Point(3, 28);
        noCurrencyColumnRadioButton.Name = "noCurrencyColumnRadioButton";
        noCurrencyColumnRadioButton.Size = new Size(164, 19);
        noCurrencyColumnRadioButton.TabIndex = 1;
        noCurrencyColumnRadioButton.TabStop = true;
        noCurrencyColumnRadioButton.Text = "No currency column found";
        noCurrencyColumnRadioButton.UseVisualStyleBackColor = true;
        //
        // monetaryScenarioGroupBox
        //
        monetaryScenarioGroupBox.AutoSize = true;
        monetaryScenarioGroupBox.Controls.Add(monetaryScenarioFlowLayoutPanel);
        monetaryScenarioGroupBox.Dock = DockStyle.Fill;
        monetaryScenarioGroupBox.Location = new Point(3, 109);
        monetaryScenarioGroupBox.MinimumSize = new Size(0, 180);
        monetaryScenarioGroupBox.Name = "monetaryScenarioGroupBox";
        monetaryScenarioGroupBox.Padding = new Padding(8);
        monetaryScenarioGroupBox.Size = new Size(480, 180);
        monetaryScenarioGroupBox.TabIndex = 2;
        monetaryScenarioGroupBox.TabStop = false;
        monetaryScenarioGroupBox.Text = "Monetary-value scenario";
        //
        // monetaryScenarioFlowLayoutPanel
        //
        monetaryScenarioFlowLayoutPanel.AutoSize = true;
        monetaryScenarioFlowLayoutPanel.Controls.Add(oneMonetaryColumnRadioButton);
        monetaryScenarioFlowLayoutPanel.Controls.Add(twoMonetaryColumnsRadioButton);
        monetaryScenarioFlowLayoutPanel.Controls.Add(moreThanTwoMonetaryColumnsRadioButton);
        monetaryScenarioFlowLayoutPanel.Controls.Add(moreThanTwoMonetaryColumnsNoteLabel);
        monetaryScenarioFlowLayoutPanel.Dock = DockStyle.Fill;
        monetaryScenarioFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        monetaryScenarioFlowLayoutPanel.Location = new Point(8, 24);
        monetaryScenarioFlowLayoutPanel.Name = "monetaryScenarioFlowLayoutPanel";
        monetaryScenarioFlowLayoutPanel.Size = new Size(464, 148);
        monetaryScenarioFlowLayoutPanel.TabIndex = 0;
        monetaryScenarioFlowLayoutPanel.WrapContents = false;
        //
        // oneMonetaryColumnRadioButton
        //
        oneMonetaryColumnRadioButton.AutoSize = true;
        oneMonetaryColumnRadioButton.Checked = true;
        oneMonetaryColumnRadioButton.Location = new Point(3, 3);
        oneMonetaryColumnRadioButton.Name = "oneMonetaryColumnRadioButton";
        oneMonetaryColumnRadioButton.Size = new Size(172, 19);
        oneMonetaryColumnRadioButton.TabIndex = 0;
        oneMonetaryColumnRadioButton.TabStop = true;
        oneMonetaryColumnRadioButton.Text = "One monetary-value column";
        oneMonetaryColumnRadioButton.UseVisualStyleBackColor = true;
        //
        // twoMonetaryColumnsRadioButton
        //
        twoMonetaryColumnsRadioButton.AutoSize = true;
        twoMonetaryColumnsRadioButton.Location = new Point(3, 28);
        twoMonetaryColumnsRadioButton.Name = "twoMonetaryColumnsRadioButton";
        twoMonetaryColumnsRadioButton.Size = new Size(177, 19);
        twoMonetaryColumnsRadioButton.TabIndex = 1;
        twoMonetaryColumnsRadioButton.Text = "Two monetary-value columns";
        twoMonetaryColumnsRadioButton.UseVisualStyleBackColor = true;
        //
        // moreThanTwoMonetaryColumnsRadioButton
        //
        moreThanTwoMonetaryColumnsRadioButton.AutoSize = true;
        moreThanTwoMonetaryColumnsRadioButton.Location = new Point(3, 53);
        moreThanTwoMonetaryColumnsRadioButton.Name = "moreThanTwoMonetaryColumnsRadioButton";
        moreThanTwoMonetaryColumnsRadioButton.Size = new Size(225, 19);
        moreThanTwoMonetaryColumnsRadioButton.TabIndex = 2;
        moreThanTwoMonetaryColumnsRadioButton.Text = "More than two monetary-value columns";
        moreThanTwoMonetaryColumnsRadioButton.UseVisualStyleBackColor = true;
        //
        // moreThanTwoMonetaryColumnsNoteLabel
        //
        moreThanTwoMonetaryColumnsNoteLabel.AutoSize = true;
        moreThanTwoMonetaryColumnsNoteLabel.ForeColor = SystemColors.ControlDarkDark;
        moreThanTwoMonetaryColumnsNoteLabel.Location = new Point(3, 78);
        moreThanTwoMonetaryColumnsNoteLabel.MaximumSize = new Size(330, 0);
        moreThanTwoMonetaryColumnsNoteLabel.Name = "moreThanTwoMonetaryColumnsNoteLabel";
        moreThanTwoMonetaryColumnsNoteLabel.Size = new Size(321, 45);
        moreThanTwoMonetaryColumnsNoteLabel.TabIndex = 3;
        moreThanTwoMonetaryColumnsNoteLabel.Text = "A warning for this condition is recorded in Findings.";
        moreThanTwoMonetaryColumnsNoteLabel.Visible = false;
        //
        // confirmationCandidatesGroupBox
        //
        confirmationCandidatesGroupBox.AutoSize = true;
        confirmationCandidatesGroupBox.Controls.Add(confirmationCandidatesFlowLayoutPanel);
        confirmationCandidatesGroupBox.Dock = DockStyle.Fill;
        confirmationCandidatesGroupBox.Location = new Point(489, 109);
        confirmationCandidatesGroupBox.MinimumSize = new Size(0, 180);
        confirmationCandidatesGroupBox.Name = "confirmationCandidatesGroupBox";
        confirmationCandidatesGroupBox.Padding = new Padding(8);
        confirmationCandidatesGroupBox.Size = new Size(480, 180);
        confirmationCandidatesGroupBox.TabIndex = 3;
        confirmationCandidatesGroupBox.TabStop = false;
        confirmationCandidatesGroupBox.Text = "Confirmation Number candidates";
        //
        // confirmationCandidatesFlowLayoutPanel
        //
        confirmationCandidatesFlowLayoutPanel.AutoSize = true;
        confirmationCandidatesFlowLayoutPanel.Controls.Add(multipleConfirmationCandidatesRadioButton);
        confirmationCandidatesFlowLayoutPanel.Controls.Add(noMultipleConfirmationCandidatesRadioButton);
        confirmationCandidatesFlowLayoutPanel.Dock = DockStyle.Fill;
        confirmationCandidatesFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        confirmationCandidatesFlowLayoutPanel.Location = new Point(8, 24);
        confirmationCandidatesFlowLayoutPanel.Name = "confirmationCandidatesFlowLayoutPanel";
        confirmationCandidatesFlowLayoutPanel.Size = new Size(464, 148);
        confirmationCandidatesFlowLayoutPanel.TabIndex = 0;
        confirmationCandidatesFlowLayoutPanel.WrapContents = false;
        //
        // multipleConfirmationCandidatesRadioButton
        //
        multipleConfirmationCandidatesRadioButton.AutoSize = true;
        multipleConfirmationCandidatesRadioButton.Location = new Point(3, 3);
        multipleConfirmationCandidatesRadioButton.Name = "multipleConfirmationCandidatesRadioButton";
        multipleConfirmationCandidatesRadioButton.Size = new Size(180, 19);
        multipleConfirmationCandidatesRadioButton.TabIndex = 0;
        multipleConfirmationCandidatesRadioButton.Text = "Multiple candidate columns found";
        multipleConfirmationCandidatesRadioButton.UseVisualStyleBackColor = true;
        //
        // noMultipleConfirmationCandidatesRadioButton
        //
        noMultipleConfirmationCandidatesRadioButton.AutoSize = true;
        noMultipleConfirmationCandidatesRadioButton.Checked = true;
        noMultipleConfirmationCandidatesRadioButton.Location = new Point(3, 28);
        noMultipleConfirmationCandidatesRadioButton.Name = "noMultipleConfirmationCandidatesRadioButton";
        noMultipleConfirmationCandidatesRadioButton.Size = new Size(173, 19);
        noMultipleConfirmationCandidatesRadioButton.TabIndex = 1;
        noMultipleConfirmationCandidatesRadioButton.TabStop = true;
        noMultipleConfirmationCandidatesRadioButton.Text = "No multiple candidates found";
        noMultipleConfirmationCandidatesRadioButton.UseVisualStyleBackColor = true;
        //
        // customScriptSupportGroupBox
        //
        customScriptSupportGroupBox.AutoSize = true;
        customScriptSupportGroupBox.Controls.Add(customScriptSupportFlowLayoutPanel);
        customScriptSupportGroupBox.Dock = DockStyle.Fill;
        customScriptSupportGroupBox.Location = new Point(3, 295);
        customScriptSupportGroupBox.MinimumSize = new Size(0, 100);
        customScriptSupportGroupBox.Name = "customScriptSupportGroupBox";
        customScriptSupportGroupBox.Padding = new Padding(8);
        customScriptSupportGroupBox.Size = new Size(480, 100);
        customScriptSupportGroupBox.TabIndex = 4;
        customScriptSupportGroupBox.TabStop = false;
        customScriptSupportGroupBox.Text = "Custom-script support";
        //
        // customScriptSupportFlowLayoutPanel
        //
        customScriptSupportFlowLayoutPanel.AutoSize = true;
        customScriptSupportFlowLayoutPanel.Controls.Add(customScriptAvailableRadioButton);
        customScriptSupportFlowLayoutPanel.Controls.Add(noCustomScriptAvailableRadioButton);
        customScriptSupportFlowLayoutPanel.Dock = DockStyle.Fill;
        customScriptSupportFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        customScriptSupportFlowLayoutPanel.Location = new Point(8, 24);
        customScriptSupportFlowLayoutPanel.Name = "customScriptSupportFlowLayoutPanel";
        customScriptSupportFlowLayoutPanel.Size = new Size(464, 68);
        customScriptSupportFlowLayoutPanel.TabIndex = 0;
        customScriptSupportFlowLayoutPanel.WrapContents = false;
        //
        // customScriptAvailableRadioButton
        //
        customScriptAvailableRadioButton.AutoSize = true;
        customScriptAvailableRadioButton.Location = new Point(3, 3);
        customScriptAvailableRadioButton.Name = "customScriptAvailableRadioButton";
        customScriptAvailableRadioButton.Size = new Size(189, 19);
        customScriptAvailableRadioButton.TabIndex = 0;
        customScriptAvailableRadioButton.Text = "Custom-script support is available";
        customScriptAvailableRadioButton.UseVisualStyleBackColor = true;
        //
        // noCustomScriptAvailableRadioButton
        //
        noCustomScriptAvailableRadioButton.AutoSize = true;
        noCustomScriptAvailableRadioButton.Checked = true;
        noCustomScriptAvailableRadioButton.Location = new Point(3, 28);
        noCustomScriptAvailableRadioButton.Name = "noCustomScriptAvailableRadioButton";
        noCustomScriptAvailableRadioButton.Size = new Size(204, 19);
        noCustomScriptAvailableRadioButton.TabIndex = 1;
        noCustomScriptAvailableRadioButton.TabStop = true;
        noCustomScriptAvailableRadioButton.Text = "No custom-script support is available";
        noCustomScriptAvailableRadioButton.UseVisualStyleBackColor = true;
        //
        // rejectedRecordsGroupBox
        //
        rejectedRecordsGroupBox.AutoSize = true;
        rejectedRecordsGroupBox.Controls.Add(rejectedRecordsFlowLayoutPanel);
        rejectedRecordsGroupBox.Dock = DockStyle.Fill;
        rejectedRecordsGroupBox.Enabled = false;
        rejectedRecordsGroupBox.Location = new Point(489, 295);
        rejectedRecordsGroupBox.MinimumSize = new Size(0, 100);
        rejectedRecordsGroupBox.Name = "rejectedRecordsGroupBox";
        rejectedRecordsGroupBox.Padding = new Padding(8);
        rejectedRecordsGroupBox.Size = new Size(480, 100);
        rejectedRecordsGroupBox.TabIndex = 5;
        rejectedRecordsGroupBox.TabStop = false;
        rejectedRecordsGroupBox.Text = "Rejected database records (driven by Database Statistics)";
        //
        // rejectedRecordsFlowLayoutPanel
        //
        rejectedRecordsFlowLayoutPanel.AutoSize = true;
        rejectedRecordsFlowLayoutPanel.Controls.Add(rejectedRecordsExistRadioButton);
        rejectedRecordsFlowLayoutPanel.Controls.Add(noRejectedRecordsRadioButton);
        rejectedRecordsFlowLayoutPanel.Dock = DockStyle.Fill;
        rejectedRecordsFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        rejectedRecordsFlowLayoutPanel.Location = new Point(8, 24);
        rejectedRecordsFlowLayoutPanel.Name = "rejectedRecordsFlowLayoutPanel";
        rejectedRecordsFlowLayoutPanel.Size = new Size(464, 68);
        rejectedRecordsFlowLayoutPanel.TabIndex = 0;
        rejectedRecordsFlowLayoutPanel.WrapContents = false;
        //
        // rejectedRecordsExistRadioButton
        //
        rejectedRecordsExistRadioButton.AutoSize = true;
        rejectedRecordsExistRadioButton.Location = new Point(3, 3);
        rejectedRecordsExistRadioButton.Name = "rejectedRecordsExistRadioButton";
        rejectedRecordsExistRadioButton.Size = new Size(155, 19);
        rejectedRecordsExistRadioButton.TabIndex = 0;
        rejectedRecordsExistRadioButton.Text = "Rejected DB records exist";
        rejectedRecordsExistRadioButton.UseVisualStyleBackColor = true;
        //
        // noRejectedRecordsRadioButton
        //
        noRejectedRecordsRadioButton.AutoSize = true;
        noRejectedRecordsRadioButton.Checked = true;
        noRejectedRecordsRadioButton.Location = new Point(3, 28);
        noRejectedRecordsRadioButton.Name = "noRejectedRecordsRadioButton";
        noRejectedRecordsRadioButton.Size = new Size(170, 19);
        noRejectedRecordsRadioButton.TabIndex = 1;
        noRejectedRecordsRadioButton.TabStop = true;
        noRejectedRecordsRadioButton.Text = "No rejected DB records exist";
        noRejectedRecordsRadioButton.UseVisualStyleBackColor = true;
        //
        // rawFileTabPage
        //
        rawFileTabPage.Controls.Add(rawChecklistFlowLayoutPanel);
        rawFileTabPage.Location = new Point(4, 24);
        rawFileTabPage.Name = "rawFileTabPage";
        rawFileTabPage.Padding = new Padding(8);
        rawFileTabPage.Size = new Size(1022, 562);
        rawFileTabPage.TabIndex = 1;
        rawFileTabPage.Text = "Raw File QA";
        rawFileTabPage.UseVisualStyleBackColor = true;
        //
        // rawChecklistFlowLayoutPanel
        //
        rawChecklistFlowLayoutPanel.AutoScroll = true;
        rawChecklistFlowLayoutPanel.Dock = DockStyle.Fill;
        rawChecklistFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        rawChecklistFlowLayoutPanel.Location = new Point(8, 8);
        rawChecklistFlowLayoutPanel.Name = "rawChecklistFlowLayoutPanel";
        rawChecklistFlowLayoutPanel.Padding = new Padding(0, 0, 8, 0);
        rawChecklistFlowLayoutPanel.Size = new Size(1006, 546);
        rawChecklistFlowLayoutPanel.TabIndex = 0;
        rawChecklistFlowLayoutPanel.WrapContents = false;
        //
        // databaseTabPage
        //
        databaseTabPage.Controls.Add(databaseChecklistFlowLayoutPanel);
        databaseTabPage.Location = new Point(4, 24);
        databaseTabPage.Name = "databaseTabPage";
        databaseTabPage.Padding = new Padding(8);
        databaseTabPage.Size = new Size(1022, 562);
        databaseTabPage.TabIndex = 2;
        databaseTabPage.Text = "DB QA";
        databaseTabPage.UseVisualStyleBackColor = true;
        //
        // databaseChecklistFlowLayoutPanel
        //
        databaseChecklistFlowLayoutPanel.AutoScroll = true;
        databaseChecklistFlowLayoutPanel.Dock = DockStyle.Fill;
        databaseChecklistFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        databaseChecklistFlowLayoutPanel.Location = new Point(8, 8);
        databaseChecklistFlowLayoutPanel.Name = "databaseChecklistFlowLayoutPanel";
        databaseChecklistFlowLayoutPanel.Padding = new Padding(0, 0, 8, 0);
        databaseChecklistFlowLayoutPanel.Size = new Size(1006, 546);
        databaseChecklistFlowLayoutPanel.TabIndex = 0;
        databaseChecklistFlowLayoutPanel.WrapContents = false;
        //
        // statisticsReadinessTabPage
        //
        statisticsReadinessTabPage.Controls.Add(statisticsControl);
        statisticsReadinessTabPage.Location = new Point(4, 24);
        statisticsReadinessTabPage.Name = "statisticsReadinessTabPage";
        statisticsReadinessTabPage.Padding = new Padding(8);
        statisticsReadinessTabPage.Size = new Size(1022, 562);
        statisticsReadinessTabPage.TabIndex = 3;
        statisticsReadinessTabPage.Text = "Statistics && Readiness";
        statisticsReadinessTabPage.UseVisualStyleBackColor = true;
        //
        // statisticsControl
        //
        statisticsControl.Dock = DockStyle.Fill;
        statisticsControl.Location = new Point(8, 8);
        statisticsControl.Name = "statisticsControl";
        statisticsControl.Size = new Size(1006, 546);
        statisticsControl.TabIndex = 0;
        //
        // findingsTabPage
        //
        findingsTabPage.Controls.Add(findingsLayoutPanel);
        findingsTabPage.Location = new Point(4, 24);
        findingsTabPage.Name = "findingsTabPage";
        findingsTabPage.Padding = new Padding(8);
        findingsTabPage.Size = new Size(1022, 562);
        findingsTabPage.TabIndex = 4;
        findingsTabPage.Text = "Findings";
        findingsTabPage.UseVisualStyleBackColor = true;
        //
        // findingsLayoutPanel
        //
        findingsLayoutPanel.ColumnCount = 1;
        findingsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        findingsLayoutPanel.Controls.Add(warningsGroupBox, 0, 0);
        findingsLayoutPanel.Controls.Add(failedChecksGroupBox, 0, 1);
        findingsLayoutPanel.Dock = DockStyle.Fill;
        findingsLayoutPanel.Location = new Point(8, 8);
        findingsLayoutPanel.Name = "findingsLayoutPanel";
        findingsLayoutPanel.RowCount = 2;
        findingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        findingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        findingsLayoutPanel.Size = new Size(1006, 546);
        findingsLayoutPanel.TabIndex = 0;
        //
        // warningsGroupBox
        //
        warningsGroupBox.Controls.Add(warningsFlowLayoutPanel);
        warningsGroupBox.Dock = DockStyle.Fill;
        warningsGroupBox.Location = new Point(3, 3);
        warningsGroupBox.Name = "warningsGroupBox";
        warningsGroupBox.Padding = new Padding(8);
        warningsGroupBox.Size = new Size(1000, 267);
        warningsGroupBox.TabIndex = 0;
        warningsGroupBox.TabStop = false;
        warningsGroupBox.Text = "Warnings";
        //
        // warningsFlowLayoutPanel
        //
        warningsFlowLayoutPanel.AutoScroll = true;
        warningsFlowLayoutPanel.Controls.Add(noWarningsLabel);
        warningsFlowLayoutPanel.Dock = DockStyle.Fill;
        warningsFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        warningsFlowLayoutPanel.Location = new Point(8, 24);
        warningsFlowLayoutPanel.Name = "warningsFlowLayoutPanel";
        warningsFlowLayoutPanel.Padding = new Padding(0, 0, 8, 0);
        warningsFlowLayoutPanel.Size = new Size(984, 235);
        warningsFlowLayoutPanel.TabIndex = 0;
        warningsFlowLayoutPanel.WrapContents = false;
        //
        // noWarningsLabel
        //
        noWarningsLabel.AutoSize = true;
        noWarningsLabel.ForeColor = SystemColors.GrayText;
        noWarningsLabel.Location = new Point(3, 6);
        noWarningsLabel.Margin = new Padding(3, 6, 3, 3);
        noWarningsLabel.Name = "noWarningsLabel";
        noWarningsLabel.Size = new Size(77, 15);
        noWarningsLabel.TabIndex = 0;
        noWarningsLabel.Text = "No warnings";
        //
        // failedChecksGroupBox
        //
        failedChecksGroupBox.Controls.Add(failedChecksFlowLayoutPanel);
        failedChecksGroupBox.Dock = DockStyle.Fill;
        failedChecksGroupBox.Location = new Point(3, 276);
        failedChecksGroupBox.Name = "failedChecksGroupBox";
        failedChecksGroupBox.Padding = new Padding(8);
        failedChecksGroupBox.Size = new Size(1000, 267);
        failedChecksGroupBox.TabIndex = 1;
        failedChecksGroupBox.TabStop = false;
        failedChecksGroupBox.Text = "Failed Checks";
        //
        // failedChecksFlowLayoutPanel
        //
        failedChecksFlowLayoutPanel.AutoScroll = true;
        failedChecksFlowLayoutPanel.Controls.Add(noFailedChecksLabel);
        failedChecksFlowLayoutPanel.Dock = DockStyle.Fill;
        failedChecksFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        failedChecksFlowLayoutPanel.Location = new Point(8, 24);
        failedChecksFlowLayoutPanel.Name = "failedChecksFlowLayoutPanel";
        failedChecksFlowLayoutPanel.Padding = new Padding(0, 0, 8, 0);
        failedChecksFlowLayoutPanel.Size = new Size(984, 235);
        failedChecksFlowLayoutPanel.TabIndex = 0;
        failedChecksFlowLayoutPanel.WrapContents = false;
        //
        // noFailedChecksLabel
        //
        noFailedChecksLabel.AutoSize = true;
        noFailedChecksLabel.ForeColor = SystemColors.GrayText;
        noFailedChecksLabel.Location = new Point(3, 6);
        noFailedChecksLabel.Margin = new Padding(3, 6, 3, 3);
        noFailedChecksLabel.Name = "noFailedChecksLabel";
        noFailedChecksLabel.Size = new Size(96, 15);
        noFailedChecksLabel.TabIndex = 0;
        noFailedChecksLabel.Text = "No failed checks";
        //
        // actionFlowLayoutPanel
        //
        actionFlowLayoutPanel.Controls.Add(closeButton);
        actionFlowLayoutPanel.Controls.Add(checkReportReadinessButton);
        actionFlowLayoutPanel.Dock = DockStyle.Fill;
        actionFlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
        actionFlowLayoutPanel.Location = new Point(15, 683);
        actionFlowLayoutPanel.Name = "actionFlowLayoutPanel";
        actionFlowLayoutPanel.Size = new Size(1030, 42);
        actionFlowLayoutPanel.TabIndex = 2;
        actionFlowLayoutPanel.WrapContents = false;
        //
        // closeButton
        //
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Location = new Point(937, 6);
        closeButton.Margin = new Padding(3, 6, 3, 3);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(90, 30);
        closeButton.TabIndex = 0;
        closeButton.Text = "Close";
        closeButton.UseVisualStyleBackColor = true;
        //
        // checkReportReadinessButton
        //
        checkReportReadinessButton.Location = new Point(751, 6);
        checkReportReadinessButton.Margin = new Padding(3, 6, 3, 3);
        checkReportReadinessButton.Name = "checkReportReadinessButton";
        checkReportReadinessButton.Size = new Size(180, 30);
        checkReportReadinessButton.TabIndex = 1;
        checkReportReadinessButton.Text = "Check Report Readiness";
        checkReportReadinessButton.UseVisualStyleBackColor = true;
        //
        // QaReportForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = closeButton;
        ClientSize = new Size(1060, 740);
        Controls.Add(mainLayoutPanel);
        MinimumSize = new Size(880, 600);
        Name = "QaReportForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Create QA Report";
        mainLayoutPanel.ResumeLayout(false);
        reportTabControl.ResumeLayout(false);
        reportDetailsTabPage.ResumeLayout(false);
        reportDetailsScrollPanel.ResumeLayout(false);
        reportDetailsScrollPanel.PerformLayout();
        reportDetailsLayoutPanel.ResumeLayout(false);
        reportDetailsLayoutPanel.PerformLayout();
        hotelGroupBox.ResumeLayout(false);
        hotelLayoutPanel.ResumeLayout(false);
        selectedHotelLayoutPanel.ResumeLayout(false);
        selectedHotelLayoutPanel.PerformLayout();
        reportInformationGroupBox.ResumeLayout(false);
        reportInformationLayoutPanel.ResumeLayout(false);
        reportInformationLayoutPanel.PerformLayout();
        fileCharacteristicsGroupBox.ResumeLayout(false);
        fileCharacteristicsGroupBox.PerformLayout();
        fileCharacteristicsLayoutPanel.ResumeLayout(false);
        fileCharacteristicsLayoutPanel.PerformLayout();
        nameColumnModeGroupBox.ResumeLayout(false);
        nameColumnModeGroupBox.PerformLayout();
        nameColumnModeFlowLayoutPanel.ResumeLayout(false);
        nameColumnModeFlowLayoutPanel.PerformLayout();
        currencyColumnGroupBox.ResumeLayout(false);
        currencyColumnGroupBox.PerformLayout();
        currencyColumnFlowLayoutPanel.ResumeLayout(false);
        currencyColumnFlowLayoutPanel.PerformLayout();
        monetaryScenarioGroupBox.ResumeLayout(false);
        monetaryScenarioGroupBox.PerformLayout();
        monetaryScenarioFlowLayoutPanel.ResumeLayout(false);
        monetaryScenarioFlowLayoutPanel.PerformLayout();
        confirmationCandidatesGroupBox.ResumeLayout(false);
        confirmationCandidatesGroupBox.PerformLayout();
        confirmationCandidatesFlowLayoutPanel.ResumeLayout(false);
        confirmationCandidatesFlowLayoutPanel.PerformLayout();
        customScriptSupportGroupBox.ResumeLayout(false);
        customScriptSupportGroupBox.PerformLayout();
        customScriptSupportFlowLayoutPanel.ResumeLayout(false);
        customScriptSupportFlowLayoutPanel.PerformLayout();
        rejectedRecordsGroupBox.ResumeLayout(false);
        rejectedRecordsGroupBox.PerformLayout();
        rejectedRecordsFlowLayoutPanel.ResumeLayout(false);
        rejectedRecordsFlowLayoutPanel.PerformLayout();
        rawFileTabPage.ResumeLayout(false);
        databaseTabPage.ResumeLayout(false);
        statisticsReadinessTabPage.ResumeLayout(false);
        findingsTabPage.ResumeLayout(false);
        findingsLayoutPanel.ResumeLayout(false);
        warningsGroupBox.ResumeLayout(false);
        warningsFlowLayoutPanel.ResumeLayout(false);
        warningsFlowLayoutPanel.PerformLayout();
        failedChecksGroupBox.ResumeLayout(false);
        failedChecksFlowLayoutPanel.ResumeLayout(false);
        failedChecksFlowLayoutPanel.PerformLayout();
        actionFlowLayoutPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
