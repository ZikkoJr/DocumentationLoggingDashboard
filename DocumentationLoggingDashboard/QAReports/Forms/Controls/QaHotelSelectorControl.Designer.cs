namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

partial class QaHotelSelectorControl
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel mainLayoutPanel;
    private Label searchLabel;
    private TextBox searchTextBox;
    private Label hotelListLabel;
    private ListBox hotelListBox;
    private Label emptyStateLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private void InitializeComponent()
    {
        mainLayoutPanel = new TableLayoutPanel();
        searchLabel = new Label();
        searchTextBox = new TextBox();
        hotelListLabel = new Label();
        hotelListBox = new ListBox();
        emptyStateLabel = new Label();
        mainLayoutPanel.SuspendLayout();
        SuspendLayout();
        //
        // mainLayoutPanel
        //
        mainLayoutPanel.ColumnCount = 2;
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.Controls.Add(searchLabel, 0, 0);
        mainLayoutPanel.Controls.Add(searchTextBox, 1, 0);
        mainLayoutPanel.Controls.Add(hotelListLabel, 0, 1);
        mainLayoutPanel.Controls.Add(hotelListBox, 1, 1);
        mainLayoutPanel.Controls.Add(emptyStateLabel, 1, 2);
        mainLayoutPanel.Dock = DockStyle.Fill;
        mainLayoutPanel.Location = new Point(0, 0);
        mainLayoutPanel.Name = "mainLayoutPanel";
        mainLayoutPanel.RowCount = 3;
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.Size = new Size(520, 250);
        mainLayoutPanel.TabIndex = 0;
        //
        // searchLabel
        //
        searchLabel.AutoSize = true;
        searchLabel.Dock = DockStyle.Fill;
        searchLabel.Location = new Point(3, 3);
        searchLabel.Margin = new Padding(3, 3, 8, 8);
        searchLabel.Name = "searchLabel";
        searchLabel.Size = new Size(101, 23);
        searchLabel.TabIndex = 0;
        searchLabel.Text = "&Search Hotels:";
        searchLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // searchTextBox
        //
        searchTextBox.AccessibleDescription = "Filters existing hotels by Hotel ID or Hotel Name. Typed text does not select or create a hotel.";
        searchTextBox.AccessibleName = "Search existing hotels";
        searchTextBox.Dock = DockStyle.Fill;
        searchTextBox.Location = new Point(115, 3);
        searchTextBox.Margin = new Padding(3, 3, 3, 8);
        searchTextBox.Name = "searchTextBox";
        searchTextBox.Size = new Size(402, 23);
        searchTextBox.TabIndex = 1;
        //
        // hotelListLabel
        //
        hotelListLabel.AutoSize = true;
        hotelListLabel.Dock = DockStyle.Fill;
        hotelListLabel.Location = new Point(3, 37);
        hotelListLabel.Margin = new Padding(3, 3, 8, 8);
        hotelListLabel.Name = "hotelListLabel";
        hotelListLabel.Size = new Size(101, 187);
        hotelListLabel.TabIndex = 2;
        hotelListLabel.Text = "&Existing Hotels:";
        hotelListLabel.TextAlign = ContentAlignment.TopLeft;
        //
        // hotelListBox
        //
        hotelListBox.AccessibleDescription = "Select one existing hotel metadata record for the QA report.";
        hotelListBox.AccessibleName = "Existing QA hotels";
        hotelListBox.Dock = DockStyle.Fill;
        hotelListBox.FormattingEnabled = true;
        hotelListBox.IntegralHeight = false;
        hotelListBox.ItemHeight = 15;
        hotelListBox.Location = new Point(115, 37);
        hotelListBox.Margin = new Padding(3, 3, 3, 8);
        hotelListBox.Name = "hotelListBox";
        hotelListBox.SelectionMode = SelectionMode.One;
        hotelListBox.Size = new Size(402, 187);
        hotelListBox.TabIndex = 3;
        //
        // emptyStateLabel
        //
        emptyStateLabel.AutoSize = true;
        emptyStateLabel.Dock = DockStyle.Fill;
        emptyStateLabel.ForeColor = SystemColors.GrayText;
        emptyStateLabel.Location = new Point(115, 232);
        emptyStateLabel.Margin = new Padding(3, 0, 3, 3);
        emptyStateLabel.Name = "emptyStateLabel";
        emptyStateLabel.Size = new Size(402, 15);
        emptyStateLabel.TabIndex = 4;
        emptyStateLabel.Text = "No hotels have been added.";
        //
        // QaHotelSelectorControl
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(mainLayoutPanel);
        MinimumSize = new Size(320, 180);
        Name = "QaHotelSelectorControl";
        Size = new Size(520, 250);
        mainLayoutPanel.ResumeLayout(false);
        mainLayoutPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
}
