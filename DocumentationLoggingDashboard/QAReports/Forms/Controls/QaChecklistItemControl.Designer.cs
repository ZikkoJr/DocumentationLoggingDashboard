namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

partial class QaChecklistItemControl
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel mainLayoutPanel;
    private Label displayNameLabel;
    private Label descriptionLabel;
    private FlowLayoutPanel resultFlowLayoutPanel;
    private RadioButton passRadioButton;
    private RadioButton failRadioButton;
    private ToolTip descriptionToolTip;

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
        components = new System.ComponentModel.Container();
        mainLayoutPanel = new TableLayoutPanel();
        displayNameLabel = new Label();
        descriptionLabel = new Label();
        resultFlowLayoutPanel = new FlowLayoutPanel();
        passRadioButton = new RadioButton();
        failRadioButton = new RadioButton();
        descriptionToolTip = new ToolTip(components);
        mainLayoutPanel.SuspendLayout();
        resultFlowLayoutPanel.SuspendLayout();
        SuspendLayout();
        //
        // mainLayoutPanel
        //
        mainLayoutPanel.AutoSize = true;
        mainLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        mainLayoutPanel.ColumnCount = 2;
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148F));
        mainLayoutPanel.Controls.Add(displayNameLabel, 0, 0);
        mainLayoutPanel.Controls.Add(descriptionLabel, 0, 1);
        mainLayoutPanel.Controls.Add(resultFlowLayoutPanel, 1, 0);
        mainLayoutPanel.Dock = DockStyle.Fill;
        mainLayoutPanel.Location = new Point(0, 0);
        mainLayoutPanel.Name = "mainLayoutPanel";
        mainLayoutPanel.Padding = new Padding(10, 8, 10, 8);
        mainLayoutPanel.RowCount = 2;
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.Size = new Size(760, 94);
        mainLayoutPanel.TabIndex = 0;
        mainLayoutPanel.SetRowSpan(resultFlowLayoutPanel, 2);
        //
        // displayNameLabel
        //
        displayNameLabel.AutoSize = true;
        displayNameLabel.Dock = DockStyle.Fill;
        displayNameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        displayNameLabel.Location = new Point(13, 8);
        displayNameLabel.Margin = new Padding(3, 0, 8, 4);
        displayNameLabel.Name = "displayNameLabel";
        displayNameLabel.Size = new Size(581, 15);
        displayNameLabel.TabIndex = 0;
        displayNameLabel.Text = "Checklist item";
        //
        // descriptionLabel
        //
        descriptionLabel.AutoSize = true;
        descriptionLabel.Dock = DockStyle.Fill;
        descriptionLabel.ForeColor = SystemColors.GrayText;
        descriptionLabel.Location = new Point(13, 27);
        descriptionLabel.Margin = new Padding(3, 0, 8, 0);
        descriptionLabel.Name = "descriptionLabel";
        descriptionLabel.Size = new Size(581, 59);
        descriptionLabel.TabIndex = 1;
        descriptionLabel.Text = "Checklist description";
        //
        // resultFlowLayoutPanel
        //
        resultFlowLayoutPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        resultFlowLayoutPanel.AutoSize = true;
        resultFlowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        resultFlowLayoutPanel.Controls.Add(passRadioButton);
        resultFlowLayoutPanel.Controls.Add(failRadioButton);
        resultFlowLayoutPanel.Location = new Point(610, 11);
        resultFlowLayoutPanel.Name = "resultFlowLayoutPanel";
        resultFlowLayoutPanel.Size = new Size(137, 25);
        resultFlowLayoutPanel.TabIndex = 2;
        resultFlowLayoutPanel.WrapContents = false;
        //
        // passRadioButton
        //
        passRadioButton.AutoSize = true;
        passRadioButton.Location = new Point(3, 3);
        passRadioButton.Margin = new Padding(3, 3, 10, 3);
        passRadioButton.Name = "passRadioButton";
        passRadioButton.Size = new Size(49, 19);
        passRadioButton.TabIndex = 0;
        passRadioButton.Text = "Pass";
        passRadioButton.UseVisualStyleBackColor = true;
        //
        // failRadioButton
        //
        failRadioButton.AutoSize = true;
        failRadioButton.Location = new Point(65, 3);
        failRadioButton.Name = "failRadioButton";
        failRadioButton.Size = new Size(42, 19);
        failRadioButton.TabIndex = 1;
        failRadioButton.Text = "Fail";
        failRadioButton.UseVisualStyleBackColor = true;
        //
        // QaChecklistItemControl
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(mainLayoutPanel);
        MinimumSize = new Size(520, 84);
        Name = "QaChecklistItemControl";
        Size = new Size(760, 94);
        mainLayoutPanel.ResumeLayout(false);
        mainLayoutPanel.PerformLayout();
        resultFlowLayoutPanel.ResumeLayout(false);
        resultFlowLayoutPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
