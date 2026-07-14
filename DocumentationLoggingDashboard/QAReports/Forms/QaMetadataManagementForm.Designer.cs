namespace DocumentationLoggingDashboard.QAReports.Forms;

partial class QaMetadataManagementForm
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel mainLayoutPanel;
    private Label privacyReminderLabel;
    private TableLayoutPanel metadataLayoutPanel;
    private GroupBox pmsGroupBox;
    private TableLayoutPanel pmsLayoutPanel;
    private Label pmsSearchLabel;
    private TextBox pmsSearchTextBox;
    private Label pmsEmptyStateLabel;
    private ListBox pmsListBox;
    private GroupBox selectedPmsGroupBox;
    private TableLayoutPanel selectedPmsLayoutPanel;
    private Label selectedPmsNameLabel;
    private TextBox selectedPmsNameTextBox;
    private Label selectedPmsFolderNameLabel;
    private TextBox selectedPmsFolderNameTextBox;
    private Button addPmsButton;
    private GroupBox hotelGroupBox;
    private TableLayoutPanel hotelLayoutPanel;
    private Label hotelSearchLabel;
    private TextBox hotelSearchTextBox;
    private Label hotelEmptyStateLabel;
    private ListBox hotelListBox;
    private GroupBox selectedHotelGroupBox;
    private TableLayoutPanel selectedHotelLayoutPanel;
    private Label selectedHotelIdLabel;
    private TextBox selectedHotelIdTextBox;
    private Label selectedHotelNameLabel;
    private TextBox selectedHotelNameTextBox;
    private Label selectedHotelPmsLabel;
    private TextBox selectedHotelPmsTextBox;
    private Label selectedHotelFolderNameLabel;
    private TextBox selectedHotelFolderNameTextBox;
    private Button addHotelButton;
    private FlowLayoutPanel actionFlowLayoutPanel;
    private Button closeButton;
    private Button refreshButton;

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
        metadataLayoutPanel = new TableLayoutPanel();
        pmsGroupBox = new GroupBox();
        pmsLayoutPanel = new TableLayoutPanel();
        pmsSearchLabel = new Label();
        pmsSearchTextBox = new TextBox();
        pmsEmptyStateLabel = new Label();
        pmsListBox = new ListBox();
        selectedPmsGroupBox = new GroupBox();
        selectedPmsLayoutPanel = new TableLayoutPanel();
        selectedPmsNameLabel = new Label();
        selectedPmsNameTextBox = new TextBox();
        selectedPmsFolderNameLabel = new Label();
        selectedPmsFolderNameTextBox = new TextBox();
        addPmsButton = new Button();
        hotelGroupBox = new GroupBox();
        hotelLayoutPanel = new TableLayoutPanel();
        hotelSearchLabel = new Label();
        hotelSearchTextBox = new TextBox();
        hotelEmptyStateLabel = new Label();
        hotelListBox = new ListBox();
        selectedHotelGroupBox = new GroupBox();
        selectedHotelLayoutPanel = new TableLayoutPanel();
        selectedHotelIdLabel = new Label();
        selectedHotelIdTextBox = new TextBox();
        selectedHotelNameLabel = new Label();
        selectedHotelNameTextBox = new TextBox();
        selectedHotelPmsLabel = new Label();
        selectedHotelPmsTextBox = new TextBox();
        selectedHotelFolderNameLabel = new Label();
        selectedHotelFolderNameTextBox = new TextBox();
        addHotelButton = new Button();
        actionFlowLayoutPanel = new FlowLayoutPanel();
        closeButton = new Button();
        refreshButton = new Button();
        mainLayoutPanel.SuspendLayout();
        metadataLayoutPanel.SuspendLayout();
        pmsGroupBox.SuspendLayout();
        pmsLayoutPanel.SuspendLayout();
        selectedPmsGroupBox.SuspendLayout();
        selectedPmsLayoutPanel.SuspendLayout();
        hotelGroupBox.SuspendLayout();
        hotelLayoutPanel.SuspendLayout();
        selectedHotelGroupBox.SuspendLayout();
        selectedHotelLayoutPanel.SuspendLayout();
        actionFlowLayoutPanel.SuspendLayout();
        SuspendLayout();
        //
        // mainLayoutPanel
        //
        mainLayoutPanel.ColumnCount = 1;
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.Controls.Add(privacyReminderLabel, 0, 0);
        mainLayoutPanel.Controls.Add(metadataLayoutPanel, 0, 1);
        mainLayoutPanel.Controls.Add(actionFlowLayoutPanel, 0, 2);
        mainLayoutPanel.Dock = DockStyle.Fill;
        mainLayoutPanel.Location = new Point(0, 0);
        mainLayoutPanel.Name = "mainLayoutPanel";
        mainLayoutPanel.Padding = new Padding(12);
        mainLayoutPanel.RowCount = 3;
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        mainLayoutPanel.Size = new Size(900, 620);
        mainLayoutPanel.TabIndex = 0;
        //
        // privacyReminderLabel
        //
        privacyReminderLabel.Dock = DockStyle.Fill;
        privacyReminderLabel.ForeColor = SystemColors.ControlDarkDark;
        privacyReminderLabel.Location = new Point(15, 12);
        privacyReminderLabel.Name = "privacyReminderLabel";
        privacyReminderLabel.Padding = new Padding(4);
        privacyReminderLabel.Size = new Size(870, 72);
        privacyReminderLabel.TabIndex = 0;
        privacyReminderLabel.Text = "QA metadata may contain only Hotel ID, Hotel Name, PMS Name, and safe generated folder names. Do not enter guest names, guest emails, reservation details, payment data, credentials, raw guest records, or full hotel files.";
        privacyReminderLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // metadataLayoutPanel
        //
        metadataLayoutPanel.ColumnCount = 2;
        metadataLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        metadataLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        metadataLayoutPanel.Controls.Add(pmsGroupBox, 0, 0);
        metadataLayoutPanel.Controls.Add(hotelGroupBox, 1, 0);
        metadataLayoutPanel.Dock = DockStyle.Fill;
        metadataLayoutPanel.Location = new Point(15, 87);
        metadataLayoutPanel.Name = "metadataLayoutPanel";
        metadataLayoutPanel.RowCount = 1;
        metadataLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        metadataLayoutPanel.Size = new Size(870, 470);
        metadataLayoutPanel.TabIndex = 1;
        //
        // pmsGroupBox
        //
        pmsGroupBox.Controls.Add(pmsLayoutPanel);
        pmsGroupBox.Dock = DockStyle.Fill;
        pmsGroupBox.Location = new Point(3, 3);
        pmsGroupBox.Name = "pmsGroupBox";
        pmsGroupBox.Padding = new Padding(10);
        pmsGroupBox.Size = new Size(429, 464);
        pmsGroupBox.TabIndex = 0;
        pmsGroupBox.TabStop = false;
        pmsGroupBox.Text = "PMS Systems";
        //
        // pmsLayoutPanel
        //
        pmsLayoutPanel.ColumnCount = 1;
        pmsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pmsLayoutPanel.Controls.Add(pmsSearchLabel, 0, 0);
        pmsLayoutPanel.Controls.Add(pmsSearchTextBox, 0, 1);
        pmsLayoutPanel.Controls.Add(pmsEmptyStateLabel, 0, 2);
        pmsLayoutPanel.Controls.Add(pmsListBox, 0, 3);
        pmsLayoutPanel.Controls.Add(selectedPmsGroupBox, 0, 4);
        pmsLayoutPanel.Controls.Add(addPmsButton, 0, 5);
        pmsLayoutPanel.Dock = DockStyle.Fill;
        pmsLayoutPanel.Location = new Point(10, 26);
        pmsLayoutPanel.Name = "pmsLayoutPanel";
        pmsLayoutPanel.RowCount = 6;
        pmsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        pmsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        pmsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        pmsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pmsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        pmsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        pmsLayoutPanel.Size = new Size(409, 428);
        pmsLayoutPanel.TabIndex = 0;
        //
        // pmsSearchLabel
        //
        pmsSearchLabel.AutoSize = true;
        pmsSearchLabel.Dock = DockStyle.Fill;
        pmsSearchLabel.Location = new Point(3, 0);
        pmsSearchLabel.Name = "pmsSearchLabel";
        pmsSearchLabel.Size = new Size(403, 24);
        pmsSearchLabel.TabIndex = 0;
        pmsSearchLabel.Text = "Search PMS Name";
        pmsSearchLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // pmsSearchTextBox
        //
        pmsSearchTextBox.AccessibleName = "Search PMS systems";
        pmsSearchTextBox.Dock = DockStyle.Fill;
        pmsSearchTextBox.Location = new Point(3, 27);
        pmsSearchTextBox.Name = "pmsSearchTextBox";
        pmsSearchTextBox.Size = new Size(403, 23);
        pmsSearchTextBox.TabIndex = 1;
        //
        // pmsEmptyStateLabel
        //
        pmsEmptyStateLabel.Dock = DockStyle.Fill;
        pmsEmptyStateLabel.ForeColor = SystemColors.ControlDarkDark;
        pmsEmptyStateLabel.Location = new Point(3, 58);
        pmsEmptyStateLabel.Name = "pmsEmptyStateLabel";
        pmsEmptyStateLabel.Size = new Size(403, 36);
        pmsEmptyStateLabel.TabIndex = 2;
        pmsEmptyStateLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // pmsListBox
        //
        pmsListBox.AccessibleName = "PMS systems";
        pmsListBox.Dock = DockStyle.Fill;
        pmsListBox.FormattingEnabled = true;
        pmsListBox.IntegralHeight = false;
        pmsListBox.Location = new Point(3, 97);
        pmsListBox.Name = "pmsListBox";
        pmsListBox.Size = new Size(403, 184);
        pmsListBox.TabIndex = 3;
        //
        // selectedPmsGroupBox
        //
        selectedPmsGroupBox.Controls.Add(selectedPmsLayoutPanel);
        selectedPmsGroupBox.Dock = DockStyle.Fill;
        selectedPmsGroupBox.Location = new Point(3, 287);
        selectedPmsGroupBox.Name = "selectedPmsGroupBox";
        selectedPmsGroupBox.Padding = new Padding(6);
        selectedPmsGroupBox.Size = new Size(403, 98);
        selectedPmsGroupBox.TabIndex = 4;
        selectedPmsGroupBox.TabStop = false;
        selectedPmsGroupBox.Text = "Selected PMS";
        //
        // selectedPmsLayoutPanel
        //
        selectedPmsLayoutPanel.ColumnCount = 2;
        selectedPmsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        selectedPmsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        selectedPmsLayoutPanel.Controls.Add(selectedPmsNameLabel, 0, 0);
        selectedPmsLayoutPanel.Controls.Add(selectedPmsNameTextBox, 1, 0);
        selectedPmsLayoutPanel.Controls.Add(selectedPmsFolderNameLabel, 0, 1);
        selectedPmsLayoutPanel.Controls.Add(selectedPmsFolderNameTextBox, 1, 1);
        selectedPmsLayoutPanel.Dock = DockStyle.Fill;
        selectedPmsLayoutPanel.Location = new Point(6, 22);
        selectedPmsLayoutPanel.Name = "selectedPmsLayoutPanel";
        selectedPmsLayoutPanel.RowCount = 2;
        selectedPmsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        selectedPmsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        selectedPmsLayoutPanel.Size = new Size(391, 70);
        selectedPmsLayoutPanel.TabIndex = 0;
        //
        // selectedPmsNameLabel
        //
        selectedPmsNameLabel.AutoSize = true;
        selectedPmsNameLabel.Dock = DockStyle.Fill;
        selectedPmsNameLabel.Location = new Point(3, 0);
        selectedPmsNameLabel.Name = "selectedPmsNameLabel";
        selectedPmsNameLabel.Size = new Size(86, 35);
        selectedPmsNameLabel.TabIndex = 0;
        selectedPmsNameLabel.Text = "PMS Name";
        selectedPmsNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // selectedPmsNameTextBox
        //
        selectedPmsNameTextBox.Dock = DockStyle.Fill;
        selectedPmsNameTextBox.AccessibleName = "Selected PMS name";
        selectedPmsNameTextBox.Margin = new Padding(3, 0, 3, 0);
        selectedPmsNameTextBox.Location = new Point(95, 6);
        selectedPmsNameTextBox.Name = "selectedPmsNameTextBox";
        selectedPmsNameTextBox.ReadOnly = true;
        selectedPmsNameTextBox.Size = new Size(293, 23);
        selectedPmsNameTextBox.TabIndex = 1;
        //
        // selectedPmsFolderNameLabel
        //
        selectedPmsFolderNameLabel.AutoSize = true;
        selectedPmsFolderNameLabel.Dock = DockStyle.Fill;
        selectedPmsFolderNameLabel.Location = new Point(3, 35);
        selectedPmsFolderNameLabel.Name = "selectedPmsFolderNameLabel";
        selectedPmsFolderNameLabel.Size = new Size(86, 35);
        selectedPmsFolderNameLabel.TabIndex = 2;
        selectedPmsFolderNameLabel.Text = "Folder Name";
        selectedPmsFolderNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // selectedPmsFolderNameTextBox
        //
        selectedPmsFolderNameTextBox.Dock = DockStyle.Fill;
        selectedPmsFolderNameTextBox.AccessibleName = "Selected PMS folder name";
        selectedPmsFolderNameTextBox.Margin = new Padding(3, 0, 3, 0);
        selectedPmsFolderNameTextBox.Location = new Point(95, 41);
        selectedPmsFolderNameTextBox.Name = "selectedPmsFolderNameTextBox";
        selectedPmsFolderNameTextBox.ReadOnly = true;
        selectedPmsFolderNameTextBox.Size = new Size(293, 23);
        selectedPmsFolderNameTextBox.TabIndex = 3;
        //
        // addPmsButton
        //
        addPmsButton.Anchor = AnchorStyles.Left;
        addPmsButton.Location = new Point(3, 393);
        addPmsButton.Name = "addPmsButton";
        addPmsButton.Size = new Size(112, 30);
        addPmsButton.TabIndex = 5;
        addPmsButton.Text = "Add PMS";
        addPmsButton.UseVisualStyleBackColor = true;
        //
        // hotelGroupBox
        //
        hotelGroupBox.Controls.Add(hotelLayoutPanel);
        hotelGroupBox.Dock = DockStyle.Fill;
        hotelGroupBox.Location = new Point(438, 3);
        hotelGroupBox.Name = "hotelGroupBox";
        hotelGroupBox.Padding = new Padding(10);
        hotelGroupBox.Size = new Size(429, 464);
        hotelGroupBox.TabIndex = 1;
        hotelGroupBox.TabStop = false;
        hotelGroupBox.Text = "Hotels";
        //
        // hotelLayoutPanel
        //
        hotelLayoutPanel.ColumnCount = 1;
        hotelLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        hotelLayoutPanel.Controls.Add(hotelSearchLabel, 0, 0);
        hotelLayoutPanel.Controls.Add(hotelSearchTextBox, 0, 1);
        hotelLayoutPanel.Controls.Add(hotelEmptyStateLabel, 0, 2);
        hotelLayoutPanel.Controls.Add(hotelListBox, 0, 3);
        hotelLayoutPanel.Controls.Add(selectedHotelGroupBox, 0, 4);
        hotelLayoutPanel.Controls.Add(addHotelButton, 0, 5);
        hotelLayoutPanel.Dock = DockStyle.Fill;
        hotelLayoutPanel.Location = new Point(10, 26);
        hotelLayoutPanel.Name = "hotelLayoutPanel";
        hotelLayoutPanel.RowCount = 6;
        hotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        hotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        hotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        hotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        hotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
        hotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        hotelLayoutPanel.Size = new Size(409, 428);
        hotelLayoutPanel.TabIndex = 0;
        //
        // hotelSearchLabel
        //
        hotelSearchLabel.AutoSize = true;
        hotelSearchLabel.Dock = DockStyle.Fill;
        hotelSearchLabel.Location = new Point(3, 0);
        hotelSearchLabel.Name = "hotelSearchLabel";
        hotelSearchLabel.Size = new Size(403, 24);
        hotelSearchLabel.TabIndex = 0;
        hotelSearchLabel.Text = "Search Hotel ID or Hotel Name";
        hotelSearchLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // hotelSearchTextBox
        //
        hotelSearchTextBox.AccessibleName = "Search hotels";
        hotelSearchTextBox.Dock = DockStyle.Fill;
        hotelSearchTextBox.Location = new Point(3, 27);
        hotelSearchTextBox.Name = "hotelSearchTextBox";
        hotelSearchTextBox.Size = new Size(403, 23);
        hotelSearchTextBox.TabIndex = 1;
        //
        // hotelEmptyStateLabel
        //
        hotelEmptyStateLabel.Dock = DockStyle.Fill;
        hotelEmptyStateLabel.ForeColor = SystemColors.ControlDarkDark;
        hotelEmptyStateLabel.Location = new Point(3, 58);
        hotelEmptyStateLabel.Name = "hotelEmptyStateLabel";
        hotelEmptyStateLabel.Size = new Size(403, 36);
        hotelEmptyStateLabel.TabIndex = 2;
        hotelEmptyStateLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // hotelListBox
        //
        hotelListBox.AccessibleName = "Hotels";
        hotelListBox.Dock = DockStyle.Fill;
        hotelListBox.FormattingEnabled = true;
        hotelListBox.IntegralHeight = false;
        hotelListBox.Location = new Point(3, 97);
        hotelListBox.Name = "hotelListBox";
        hotelListBox.Size = new Size(403, 132);
        hotelListBox.TabIndex = 3;
        //
        // selectedHotelGroupBox
        //
        selectedHotelGroupBox.Controls.Add(selectedHotelLayoutPanel);
        selectedHotelGroupBox.Dock = DockStyle.Fill;
        selectedHotelGroupBox.Location = new Point(3, 235);
        selectedHotelGroupBox.Name = "selectedHotelGroupBox";
        selectedHotelGroupBox.Padding = new Padding(6);
        selectedHotelGroupBox.Size = new Size(403, 150);
        selectedHotelGroupBox.TabIndex = 4;
        selectedHotelGroupBox.TabStop = false;
        selectedHotelGroupBox.Text = "Selected Hotel";
        //
        // selectedHotelLayoutPanel
        //
        selectedHotelLayoutPanel.ColumnCount = 2;
        selectedHotelLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        selectedHotelLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        selectedHotelLayoutPanel.Controls.Add(selectedHotelIdLabel, 0, 0);
        selectedHotelLayoutPanel.Controls.Add(selectedHotelIdTextBox, 1, 0);
        selectedHotelLayoutPanel.Controls.Add(selectedHotelNameLabel, 0, 1);
        selectedHotelLayoutPanel.Controls.Add(selectedHotelNameTextBox, 1, 1);
        selectedHotelLayoutPanel.Controls.Add(selectedHotelPmsLabel, 0, 2);
        selectedHotelLayoutPanel.Controls.Add(selectedHotelPmsTextBox, 1, 2);
        selectedHotelLayoutPanel.Controls.Add(selectedHotelFolderNameLabel, 0, 3);
        selectedHotelLayoutPanel.Controls.Add(selectedHotelFolderNameTextBox, 1, 3);
        selectedHotelLayoutPanel.Dock = DockStyle.Fill;
        selectedHotelLayoutPanel.Location = new Point(6, 22);
        selectedHotelLayoutPanel.Name = "selectedHotelLayoutPanel";
        selectedHotelLayoutPanel.RowCount = 4;
        selectedHotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        selectedHotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        selectedHotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        selectedHotelLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        selectedHotelLayoutPanel.Size = new Size(391, 122);
        selectedHotelLayoutPanel.TabIndex = 0;
        //
        // selectedHotelIdLabel
        //
        selectedHotelIdLabel.AutoSize = true;
        selectedHotelIdLabel.Dock = DockStyle.Fill;
        selectedHotelIdLabel.Location = new Point(3, 0);
        selectedHotelIdLabel.Name = "selectedHotelIdLabel";
        selectedHotelIdLabel.Size = new Size(86, 30);
        selectedHotelIdLabel.TabIndex = 0;
        selectedHotelIdLabel.Text = "Hotel ID";
        selectedHotelIdLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // selectedHotelIdTextBox
        //
        selectedHotelIdTextBox.Dock = DockStyle.Fill;
        selectedHotelIdTextBox.AccessibleName = "Selected hotel ID";
        selectedHotelIdTextBox.Margin = new Padding(3, 0, 3, 0);
        selectedHotelIdTextBox.Location = new Point(95, 3);
        selectedHotelIdTextBox.Name = "selectedHotelIdTextBox";
        selectedHotelIdTextBox.ReadOnly = true;
        selectedHotelIdTextBox.Size = new Size(293, 23);
        selectedHotelIdTextBox.TabIndex = 1;
        //
        // selectedHotelNameLabel
        //
        selectedHotelNameLabel.AutoSize = true;
        selectedHotelNameLabel.Dock = DockStyle.Fill;
        selectedHotelNameLabel.Location = new Point(3, 30);
        selectedHotelNameLabel.Name = "selectedHotelNameLabel";
        selectedHotelNameLabel.Size = new Size(86, 30);
        selectedHotelNameLabel.TabIndex = 2;
        selectedHotelNameLabel.Text = "Hotel Name";
        selectedHotelNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // selectedHotelNameTextBox
        //
        selectedHotelNameTextBox.Dock = DockStyle.Fill;
        selectedHotelNameTextBox.AccessibleName = "Selected hotel name";
        selectedHotelNameTextBox.Margin = new Padding(3, 0, 3, 0);
        selectedHotelNameTextBox.Location = new Point(95, 33);
        selectedHotelNameTextBox.Name = "selectedHotelNameTextBox";
        selectedHotelNameTextBox.ReadOnly = true;
        selectedHotelNameTextBox.Size = new Size(293, 23);
        selectedHotelNameTextBox.TabIndex = 3;
        //
        // selectedHotelPmsLabel
        //
        selectedHotelPmsLabel.AutoSize = true;
        selectedHotelPmsLabel.Dock = DockStyle.Fill;
        selectedHotelPmsLabel.Location = new Point(3, 60);
        selectedHotelPmsLabel.Name = "selectedHotelPmsLabel";
        selectedHotelPmsLabel.Size = new Size(86, 30);
        selectedHotelPmsLabel.TabIndex = 4;
        selectedHotelPmsLabel.Text = "Saved PMS";
        selectedHotelPmsLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // selectedHotelPmsTextBox
        //
        selectedHotelPmsTextBox.Dock = DockStyle.Fill;
        selectedHotelPmsTextBox.AccessibleName = "Selected hotel's saved PMS";
        selectedHotelPmsTextBox.Margin = new Padding(3, 0, 3, 0);
        selectedHotelPmsTextBox.Location = new Point(95, 63);
        selectedHotelPmsTextBox.Name = "selectedHotelPmsTextBox";
        selectedHotelPmsTextBox.ReadOnly = true;
        selectedHotelPmsTextBox.Size = new Size(293, 23);
        selectedHotelPmsTextBox.TabIndex = 5;
        //
        // selectedHotelFolderNameLabel
        //
        selectedHotelFolderNameLabel.AutoSize = true;
        selectedHotelFolderNameLabel.Dock = DockStyle.Fill;
        selectedHotelFolderNameLabel.Location = new Point(3, 90);
        selectedHotelFolderNameLabel.Name = "selectedHotelFolderNameLabel";
        selectedHotelFolderNameLabel.Size = new Size(86, 32);
        selectedHotelFolderNameLabel.TabIndex = 6;
        selectedHotelFolderNameLabel.Text = "Folder Name";
        selectedHotelFolderNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // selectedHotelFolderNameTextBox
        //
        selectedHotelFolderNameTextBox.Dock = DockStyle.Fill;
        selectedHotelFolderNameTextBox.AccessibleName = "Selected hotel folder name";
        selectedHotelFolderNameTextBox.Margin = new Padding(3, 0, 3, 0);
        selectedHotelFolderNameTextBox.Location = new Point(95, 93);
        selectedHotelFolderNameTextBox.Name = "selectedHotelFolderNameTextBox";
        selectedHotelFolderNameTextBox.ReadOnly = true;
        selectedHotelFolderNameTextBox.Size = new Size(293, 23);
        selectedHotelFolderNameTextBox.TabIndex = 7;
        //
        // addHotelButton
        //
        addHotelButton.Anchor = AnchorStyles.Left;
        addHotelButton.Location = new Point(3, 393);
        addHotelButton.Name = "addHotelButton";
        addHotelButton.Size = new Size(112, 30);
        addHotelButton.TabIndex = 5;
        addHotelButton.Text = "Add Hotel";
        addHotelButton.UseVisualStyleBackColor = true;
        //
        // actionFlowLayoutPanel
        //
        actionFlowLayoutPanel.Controls.Add(closeButton);
        actionFlowLayoutPanel.Controls.Add(refreshButton);
        actionFlowLayoutPanel.Dock = DockStyle.Fill;
        actionFlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
        actionFlowLayoutPanel.Location = new Point(15, 563);
        actionFlowLayoutPanel.Name = "actionFlowLayoutPanel";
        actionFlowLayoutPanel.Size = new Size(870, 42);
        actionFlowLayoutPanel.TabIndex = 2;
        actionFlowLayoutPanel.WrapContents = false;
        //
        // closeButton
        //
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Location = new Point(777, 6);
        closeButton.Margin = new Padding(3, 6, 3, 3);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(90, 30);
        closeButton.TabIndex = 1;
        closeButton.Text = "Close";
        closeButton.UseVisualStyleBackColor = true;
        //
        // refreshButton
        //
        refreshButton.Location = new Point(681, 6);
        refreshButton.Margin = new Padding(3, 6, 3, 3);
        refreshButton.Name = "refreshButton";
        refreshButton.Size = new Size(90, 30);
        refreshButton.TabIndex = 0;
        refreshButton.Text = "Refresh";
        refreshButton.UseVisualStyleBackColor = true;
        //
        // QaMetadataManagementForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = closeButton;
        ClientSize = new Size(900, 620);
        Controls.Add(mainLayoutPanel);
        MinimumSize = new Size(760, 520);
        Name = "QaMetadataManagementForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "QA Hotel and PMS Management";
        mainLayoutPanel.ResumeLayout(false);
        metadataLayoutPanel.ResumeLayout(false);
        pmsGroupBox.ResumeLayout(false);
        pmsLayoutPanel.ResumeLayout(false);
        pmsLayoutPanel.PerformLayout();
        selectedPmsGroupBox.ResumeLayout(false);
        selectedPmsLayoutPanel.ResumeLayout(false);
        selectedPmsLayoutPanel.PerformLayout();
        hotelGroupBox.ResumeLayout(false);
        hotelLayoutPanel.ResumeLayout(false);
        hotelLayoutPanel.PerformLayout();
        selectedHotelGroupBox.ResumeLayout(false);
        selectedHotelLayoutPanel.ResumeLayout(false);
        selectedHotelLayoutPanel.PerformLayout();
        actionFlowLayoutPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
