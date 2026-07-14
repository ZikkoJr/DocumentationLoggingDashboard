namespace DocumentationLoggingDashboard.QAReports.Forms;

partial class AddHotelForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel mainLayoutPanel;
    private Label instructionLabel;
    private Label hotelIdLabel;
    private TextBox hotelIdTextBox;
    private Label hotelNameLabel;
    private TextBox hotelNameTextBox;
    private Label pmsSearchLabel;
    private TextBox pmsSearchTextBox;
    private Label pmsListLabel;
    private ListBox pmsListBox;
    private Label pmsEmptyStateLabel;
    private FlowLayoutPanel buttonFlowLayoutPanel;
    private Button addButton;
    private Button cancelButton;

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
        mainLayoutPanel = new TableLayoutPanel();
        instructionLabel = new Label();
        hotelIdLabel = new Label();
        hotelIdTextBox = new TextBox();
        hotelNameLabel = new Label();
        hotelNameTextBox = new TextBox();
        pmsSearchLabel = new Label();
        pmsSearchTextBox = new TextBox();
        pmsListLabel = new Label();
        pmsListBox = new ListBox();
        pmsEmptyStateLabel = new Label();
        buttonFlowLayoutPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        addButton = new Button();
        mainLayoutPanel.SuspendLayout();
        buttonFlowLayoutPanel.SuspendLayout();
        SuspendLayout();
        //
        // mainLayoutPanel
        //
        mainLayoutPanel.ColumnCount = 2;
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.Controls.Add(instructionLabel, 0, 0);
        mainLayoutPanel.Controls.Add(hotelIdLabel, 0, 1);
        mainLayoutPanel.Controls.Add(hotelIdTextBox, 1, 1);
        mainLayoutPanel.Controls.Add(hotelNameLabel, 0, 2);
        mainLayoutPanel.Controls.Add(hotelNameTextBox, 1, 2);
        mainLayoutPanel.Controls.Add(pmsSearchLabel, 0, 3);
        mainLayoutPanel.Controls.Add(pmsSearchTextBox, 1, 3);
        mainLayoutPanel.Controls.Add(pmsListLabel, 0, 4);
        mainLayoutPanel.Controls.Add(pmsListBox, 1, 4);
        mainLayoutPanel.Controls.Add(pmsEmptyStateLabel, 1, 5);
        mainLayoutPanel.Controls.Add(buttonFlowLayoutPanel, 0, 6);
        mainLayoutPanel.Dock = DockStyle.Fill;
        mainLayoutPanel.Location = new Point(0, 0);
        mainLayoutPanel.Name = "mainLayoutPanel";
        mainLayoutPanel.Padding = new Padding(16);
        mainLayoutPanel.RowCount = 7;
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        mainLayoutPanel.Size = new Size(620, 540);
        mainLayoutPanel.TabIndex = 0;
        mainLayoutPanel.SetColumnSpan(instructionLabel, 2);
        mainLayoutPanel.SetColumnSpan(buttonFlowLayoutPanel, 2);
        //
        // instructionLabel
        //
        instructionLabel.AutoSize = true;
        instructionLabel.Dock = DockStyle.Fill;
        instructionLabel.Location = new Point(19, 19);
        instructionLabel.Margin = new Padding(3, 3, 3, 14);
        instructionLabel.Name = "instructionLabel";
        instructionLabel.Size = new Size(582, 30);
        instructionLabel.TabIndex = 0;
        instructionLabel.Text = "Enter the hotel details, then select an existing PMS system. Search text only filters the available PMS records.";
        //
        // hotelIdLabel
        //
        hotelIdLabel.AutoSize = true;
        hotelIdLabel.Dock = DockStyle.Fill;
        hotelIdLabel.Location = new Point(19, 66);
        hotelIdLabel.Margin = new Padding(3, 3, 8, 8);
        hotelIdLabel.Name = "hotelIdLabel";
        hotelIdLabel.Size = new Size(113, 23);
        hotelIdLabel.TabIndex = 1;
        hotelIdLabel.Text = "Hotel &ID:";
        hotelIdLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // hotelIdTextBox
        //
        hotelIdTextBox.Dock = DockStyle.Fill;
        hotelIdTextBox.Location = new Point(143, 66);
        hotelIdTextBox.Margin = new Padding(3, 3, 3, 8);
        hotelIdTextBox.Name = "hotelIdTextBox";
        hotelIdTextBox.Size = new Size(458, 23);
        hotelIdTextBox.TabIndex = 2;
        hotelIdTextBox.AccessibleName = "Hotel ID";
        //
        // hotelNameLabel
        //
        hotelNameLabel.AutoSize = true;
        hotelNameLabel.Dock = DockStyle.Fill;
        hotelNameLabel.Location = new Point(19, 100);
        hotelNameLabel.Margin = new Padding(3, 3, 8, 8);
        hotelNameLabel.Name = "hotelNameLabel";
        hotelNameLabel.Size = new Size(113, 23);
        hotelNameLabel.TabIndex = 3;
        hotelNameLabel.Text = "Hotel &Name:";
        hotelNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // hotelNameTextBox
        //
        hotelNameTextBox.Dock = DockStyle.Fill;
        hotelNameTextBox.Location = new Point(143, 100);
        hotelNameTextBox.Margin = new Padding(3, 3, 3, 8);
        hotelNameTextBox.Name = "hotelNameTextBox";
        hotelNameTextBox.Size = new Size(458, 23);
        hotelNameTextBox.TabIndex = 4;
        hotelNameTextBox.AccessibleName = "Hotel Name";
        //
        // pmsSearchLabel
        //
        pmsSearchLabel.AutoSize = true;
        pmsSearchLabel.Dock = DockStyle.Fill;
        pmsSearchLabel.Location = new Point(19, 134);
        pmsSearchLabel.Margin = new Padding(3, 3, 8, 8);
        pmsSearchLabel.Name = "pmsSearchLabel";
        pmsSearchLabel.Size = new Size(113, 23);
        pmsSearchLabel.TabIndex = 5;
        pmsSearchLabel.Text = "&Search PMS:";
        pmsSearchLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // pmsSearchTextBox
        //
        pmsSearchTextBox.AccessibleDescription = "Filters the existing PMS systems by PMS name.";
        pmsSearchTextBox.AccessibleName = "Search existing PMS systems";
        pmsSearchTextBox.Dock = DockStyle.Fill;
        pmsSearchTextBox.Location = new Point(143, 134);
        pmsSearchTextBox.Margin = new Padding(3, 3, 3, 8);
        pmsSearchTextBox.Name = "pmsSearchTextBox";
        pmsSearchTextBox.Size = new Size(458, 23);
        pmsSearchTextBox.TabIndex = 6;
        //
        // pmsListLabel
        //
        pmsListLabel.AutoSize = true;
        pmsListLabel.Dock = DockStyle.Fill;
        pmsListLabel.Location = new Point(19, 168);
        pmsListLabel.Margin = new Padding(3, 3, 8, 8);
        pmsListLabel.Name = "pmsListLabel";
        pmsListLabel.Size = new Size(113, 291);
        pmsListLabel.TabIndex = 7;
        pmsListLabel.Text = "&Existing PMS:";
        pmsListLabel.TextAlign = ContentAlignment.TopLeft;
        //
        // pmsListBox
        //
        pmsListBox.AccessibleDescription = "Select one existing PMS system for the new hotel.";
        pmsListBox.AccessibleName = "Existing PMS systems";
        pmsListBox.Dock = DockStyle.Fill;
        pmsListBox.FormattingEnabled = true;
        pmsListBox.IntegralHeight = false;
        pmsListBox.ItemHeight = 15;
        pmsListBox.Location = new Point(143, 168);
        pmsListBox.Margin = new Padding(3, 3, 3, 8);
        pmsListBox.Name = "pmsListBox";
        pmsListBox.Size = new Size(458, 291);
        pmsListBox.TabIndex = 8;
        //
        // pmsEmptyStateLabel
        //
        pmsEmptyStateLabel.AutoSize = true;
        pmsEmptyStateLabel.Dock = DockStyle.Fill;
        pmsEmptyStateLabel.ForeColor = SystemColors.GrayText;
        pmsEmptyStateLabel.Location = new Point(143, 467);
        pmsEmptyStateLabel.Margin = new Padding(3, 0, 3, 8);
        pmsEmptyStateLabel.Name = "pmsEmptyStateLabel";
        pmsEmptyStateLabel.Size = new Size(458, 15);
        pmsEmptyStateLabel.TabIndex = 9;
        pmsEmptyStateLabel.Text = "No PMS systems match the current search.";
        pmsEmptyStateLabel.Visible = false;
        //
        // buttonFlowLayoutPanel
        //
        buttonFlowLayoutPanel.AutoSize = false;
        buttonFlowLayoutPanel.Controls.Add(cancelButton);
        buttonFlowLayoutPanel.Controls.Add(addButton);
        buttonFlowLayoutPanel.Dock = DockStyle.Fill;
        buttonFlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonFlowLayoutPanel.Location = new Point(19, 493);
        buttonFlowLayoutPanel.Name = "buttonFlowLayoutPanel";
        buttonFlowLayoutPanel.Size = new Size(582, 31);
        buttonFlowLayoutPanel.TabIndex = 10;
        buttonFlowLayoutPanel.WrapContents = false;
        //
        // cancelButton
        //
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(479, 3);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(100, 30);
        cancelButton.TabIndex = 1;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        //
        // addButton
        //
        addButton.Enabled = false;
        addButton.Location = new Point(373, 3);
        addButton.Name = "addButton";
        addButton.Size = new Size(100, 30);
        addButton.TabIndex = 0;
        addButton.Text = "Add Hotel";
        addButton.UseVisualStyleBackColor = true;
        //
        // AddHotelForm
        //
        AcceptButton = addButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(620, 540);
        Controls.Add(mainLayoutPanel);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(560, 500);
        Name = "AddHotelForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Add Hotel";
        mainLayoutPanel.ResumeLayout(false);
        mainLayoutPanel.PerformLayout();
        buttonFlowLayoutPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
