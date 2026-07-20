namespace DocumentationLoggingDashboard.QAReports.Forms;

partial class AddPmsForm
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel mainLayoutPanel;
    private Label pmsNameLabel;
    private TextBox pmsNameTextBox;
    private FlowLayoutPanel buttonFlowLayoutPanel;
    private Button addButton;
    private Button cancelButton;

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
        pmsNameLabel = new Label();
        pmsNameTextBox = new TextBox();
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
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.Controls.Add(pmsNameLabel, 0, 0);
        mainLayoutPanel.Controls.Add(pmsNameTextBox, 1, 0);
        mainLayoutPanel.Controls.Add(buttonFlowLayoutPanel, 0, 1);
        mainLayoutPanel.Dock = DockStyle.Fill;
        mainLayoutPanel.Location = new Point(0, 0);
        mainLayoutPanel.Name = "mainLayoutPanel";
        mainLayoutPanel.Padding = new Padding(16);
        mainLayoutPanel.RowCount = 2;
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        mainLayoutPanel.Size = new Size(480, 150);
        mainLayoutPanel.TabIndex = 0;
        //
        // pmsNameLabel
        //
        pmsNameLabel.AutoSize = true;
        pmsNameLabel.Dock = DockStyle.Fill;
        pmsNameLabel.Location = new Point(19, 16);
        pmsNameLabel.Name = "pmsNameLabel";
        pmsNameLabel.Size = new Size(94, 72);
        pmsNameLabel.TabIndex = 0;
        pmsNameLabel.Text = "PMS Name";
        pmsNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // pmsNameTextBox
        //
        pmsNameTextBox.AccessibleName = "PMS Name";
        pmsNameTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        pmsNameTextBox.Location = new Point(119, 40);
        pmsNameTextBox.Name = "pmsNameTextBox";
        pmsNameTextBox.Size = new Size(342, 23);
        pmsNameTextBox.TabIndex = 1;
        //
        // buttonFlowLayoutPanel
        //
        mainLayoutPanel.SetColumnSpan(buttonFlowLayoutPanel, 2);
        buttonFlowLayoutPanel.Controls.Add(cancelButton);
        buttonFlowLayoutPanel.Controls.Add(addButton);
        buttonFlowLayoutPanel.Dock = DockStyle.Fill;
        buttonFlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonFlowLayoutPanel.Location = new Point(19, 91);
        buttonFlowLayoutPanel.Name = "buttonFlowLayoutPanel";
        buttonFlowLayoutPanel.Padding = new Padding(0, 6, 0, 0);
        buttonFlowLayoutPanel.Size = new Size(442, 40);
        buttonFlowLayoutPanel.TabIndex = 2;
        buttonFlowLayoutPanel.WrapContents = false;
        //
        // cancelButton
        //
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(347, 9);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(92, 30);
        cancelButton.TabIndex = 1;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        //
        // addButton
        //
        addButton.Enabled = false;
        addButton.Location = new Point(244, 9);
        addButton.Margin = new Padding(3, 3, 8, 3);
        addButton.Name = "addButton";
        addButton.Size = new Size(92, 30);
        addButton.TabIndex = 0;
        addButton.Text = "Add";
        addButton.UseVisualStyleBackColor = true;
        //
        // AddPmsForm
        //
        AcceptButton = addButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(480, 150);
        Controls.Add(mainLayoutPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(440, 180);
        Name = "AddPmsForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Add PMS System";
        mainLayoutPanel.ResumeLayout(false);
        mainLayoutPanel.PerformLayout();
        buttonFlowLayoutPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
