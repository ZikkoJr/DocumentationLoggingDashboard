namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

partial class QaFindingItemControl
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel mainLayoutPanel;
    private Label severityResolutionLabel;
    private Label titleLabel;
    private Label descriptionLabel;
    private Label relatedCheckLabel;
    private GroupBox resolutionGroupBox;
    private FlowLayoutPanel resolutionFlowLayoutPanel;
    private RadioButton activeRadioButton;
    private RadioButton handledByCustomScriptRadioButton;
    private RadioButton explainedAndAcceptedRadioButton;
    private TableLayoutPanel customScriptLayoutPanel;
    private Label customScriptNameLabel;
    private TextBox customScriptNameTextBox;
    private Label resolutionNotesLabel;
    private TextBox resolutionNotesTextBox;
    private Label explainedNotesNeededLabel;

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
        severityResolutionLabel = new Label();
        titleLabel = new Label();
        descriptionLabel = new Label();
        relatedCheckLabel = new Label();
        resolutionGroupBox = new GroupBox();
        resolutionFlowLayoutPanel = new FlowLayoutPanel();
        activeRadioButton = new RadioButton();
        handledByCustomScriptRadioButton = new RadioButton();
        explainedAndAcceptedRadioButton = new RadioButton();
        customScriptLayoutPanel = new TableLayoutPanel();
        customScriptNameLabel = new Label();
        customScriptNameTextBox = new TextBox();
        resolutionNotesLabel = new Label();
        resolutionNotesTextBox = new TextBox();
        explainedNotesNeededLabel = new Label();
        mainLayoutPanel.SuspendLayout();
        resolutionGroupBox.SuspendLayout();
        resolutionFlowLayoutPanel.SuspendLayout();
        customScriptLayoutPanel.SuspendLayout();
        SuspendLayout();
        //
        // mainLayoutPanel
        //
        mainLayoutPanel.AutoSize = true;
        mainLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        mainLayoutPanel.ColumnCount = 1;
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.Controls.Add(severityResolutionLabel, 0, 0);
        mainLayoutPanel.Controls.Add(titleLabel, 0, 1);
        mainLayoutPanel.Controls.Add(descriptionLabel, 0, 2);
        mainLayoutPanel.Controls.Add(relatedCheckLabel, 0, 3);
        mainLayoutPanel.Controls.Add(resolutionGroupBox, 0, 4);
        mainLayoutPanel.Controls.Add(customScriptLayoutPanel, 0, 5);
        mainLayoutPanel.Controls.Add(resolutionNotesLabel, 0, 6);
        mainLayoutPanel.Controls.Add(resolutionNotesTextBox, 0, 7);
        mainLayoutPanel.Controls.Add(explainedNotesNeededLabel, 0, 8);
        mainLayoutPanel.Dock = DockStyle.Top;
        mainLayoutPanel.Location = new Point(0, 0);
        mainLayoutPanel.Name = "mainLayoutPanel";
        mainLayoutPanel.Padding = new Padding(10, 8, 10, 8);
        mainLayoutPanel.RowCount = 9;
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.RowStyles.Add(new RowStyle());
        mainLayoutPanel.Size = new Size(758, 282);
        mainLayoutPanel.TabIndex = 0;
        //
        // severityResolutionLabel
        //
        severityResolutionLabel.AutoSize = true;
        severityResolutionLabel.Dock = DockStyle.Fill;
        severityResolutionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        severityResolutionLabel.ForeColor = Color.DarkGoldenrod;
        severityResolutionLabel.Location = new Point(13, 8);
        severityResolutionLabel.Margin = new Padding(3, 0, 3, 4);
        severityResolutionLabel.Name = "severityResolutionLabel";
        severityResolutionLabel.Size = new Size(732, 15);
        severityResolutionLabel.TabIndex = 0;
        severityResolutionLabel.Text = "Warning \u2014 Active";
        //
        // titleLabel
        //
        titleLabel.AutoSize = true;
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        titleLabel.Location = new Point(13, 27);
        titleLabel.Margin = new Padding(3, 0, 3, 4);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(732, 15);
        titleLabel.TabIndex = 1;
        titleLabel.Text = "Finding title";
        //
        // descriptionLabel
        //
        descriptionLabel.AutoSize = true;
        descriptionLabel.Dock = DockStyle.Fill;
        descriptionLabel.ForeColor = SystemColors.GrayText;
        descriptionLabel.Location = new Point(13, 46);
        descriptionLabel.Margin = new Padding(3, 0, 3, 4);
        descriptionLabel.Name = "descriptionLabel";
        descriptionLabel.Size = new Size(732, 15);
        descriptionLabel.TabIndex = 2;
        descriptionLabel.Text = "Finding description";
        //
        // relatedCheckLabel
        //
        relatedCheckLabel.AutoSize = true;
        relatedCheckLabel.Dock = DockStyle.Fill;
        relatedCheckLabel.ForeColor = SystemColors.GrayText;
        relatedCheckLabel.Location = new Point(13, 65);
        relatedCheckLabel.Margin = new Padding(3, 0, 3, 6);
        relatedCheckLabel.Name = "relatedCheckLabel";
        relatedCheckLabel.Size = new Size(732, 15);
        relatedCheckLabel.TabIndex = 3;
        relatedCheckLabel.Text = "Related check";
        //
        // resolutionGroupBox
        //
        resolutionGroupBox.AutoSize = true;
        resolutionGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        resolutionGroupBox.Controls.Add(resolutionFlowLayoutPanel);
        resolutionGroupBox.Dock = DockStyle.Top;
        resolutionGroupBox.Location = new Point(13, 89);
        resolutionGroupBox.Margin = new Padding(3, 3, 3, 6);
        resolutionGroupBox.Name = "resolutionGroupBox";
        resolutionGroupBox.Padding = new Padding(8, 6, 8, 6);
        resolutionGroupBox.Size = new Size(732, 50);
        resolutionGroupBox.TabIndex = 4;
        resolutionGroupBox.TabStop = false;
        resolutionGroupBox.Text = "Resolution";
        //
        // resolutionFlowLayoutPanel
        //
        resolutionFlowLayoutPanel.AutoSize = true;
        resolutionFlowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        resolutionFlowLayoutPanel.Controls.Add(activeRadioButton);
        resolutionFlowLayoutPanel.Controls.Add(handledByCustomScriptRadioButton);
        resolutionFlowLayoutPanel.Controls.Add(explainedAndAcceptedRadioButton);
        resolutionFlowLayoutPanel.Dock = DockStyle.Fill;
        resolutionFlowLayoutPanel.Location = new Point(8, 22);
        resolutionFlowLayoutPanel.Name = "resolutionFlowLayoutPanel";
        resolutionFlowLayoutPanel.Size = new Size(716, 22);
        resolutionFlowLayoutPanel.TabIndex = 0;
        resolutionFlowLayoutPanel.WrapContents = true;
        //
        // activeRadioButton
        //
        activeRadioButton.AutoSize = true;
        activeRadioButton.Checked = true;
        activeRadioButton.Location = new Point(3, 3);
        activeRadioButton.Margin = new Padding(3, 3, 14, 3);
        activeRadioButton.Name = "activeRadioButton";
        activeRadioButton.Size = new Size(58, 19);
        activeRadioButton.TabIndex = 0;
        activeRadioButton.TabStop = true;
        activeRadioButton.Text = "Active";
        activeRadioButton.UseVisualStyleBackColor = true;
        //
        // handledByCustomScriptRadioButton
        //
        handledByCustomScriptRadioButton.AutoSize = true;
        handledByCustomScriptRadioButton.Enabled = false;
        handledByCustomScriptRadioButton.Location = new Point(78, 3);
        handledByCustomScriptRadioButton.Margin = new Padding(3, 3, 14, 3);
        handledByCustomScriptRadioButton.Name = "handledByCustomScriptRadioButton";
        handledByCustomScriptRadioButton.Size = new Size(164, 19);
        handledByCustomScriptRadioButton.TabIndex = 1;
        handledByCustomScriptRadioButton.Text = "Handled by Custom Script";
        handledByCustomScriptRadioButton.UseVisualStyleBackColor = true;
        //
        // explainedAndAcceptedRadioButton
        //
        explainedAndAcceptedRadioButton.AutoSize = true;
        explainedAndAcceptedRadioButton.Location = new Point(259, 3);
        explainedAndAcceptedRadioButton.Name = "explainedAndAcceptedRadioButton";
        explainedAndAcceptedRadioButton.Size = new Size(155, 19);
        explainedAndAcceptedRadioButton.TabIndex = 2;
        explainedAndAcceptedRadioButton.Text = "Explained and Accepted";
        explainedAndAcceptedRadioButton.UseVisualStyleBackColor = true;
        //
        // customScriptLayoutPanel
        //
        customScriptLayoutPanel.AutoSize = true;
        customScriptLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        customScriptLayoutPanel.ColumnCount = 2;
        customScriptLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142F));
        customScriptLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        customScriptLayoutPanel.Controls.Add(customScriptNameLabel, 0, 0);
        customScriptLayoutPanel.Controls.Add(customScriptNameTextBox, 1, 0);
        customScriptLayoutPanel.Dock = DockStyle.Top;
        customScriptLayoutPanel.Location = new Point(13, 148);
        customScriptLayoutPanel.Margin = new Padding(3, 3, 3, 6);
        customScriptLayoutPanel.Name = "customScriptLayoutPanel";
        customScriptLayoutPanel.RowCount = 1;
        customScriptLayoutPanel.RowStyles.Add(new RowStyle());
        customScriptLayoutPanel.Size = new Size(732, 29);
        customScriptLayoutPanel.TabIndex = 5;
        customScriptLayoutPanel.Visible = false;
        //
        // customScriptNameLabel
        //
        customScriptNameLabel.AutoSize = true;
        customScriptNameLabel.Dock = DockStyle.Fill;
        customScriptNameLabel.Location = new Point(3, 3);
        customScriptNameLabel.Margin = new Padding(3, 3, 8, 3);
        customScriptNameLabel.Name = "customScriptNameLabel";
        customScriptNameLabel.Size = new Size(131, 23);
        customScriptNameLabel.TabIndex = 0;
        customScriptNameLabel.Text = "Custom Script Name";
        customScriptNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // customScriptNameTextBox
        //
        customScriptNameTextBox.AccessibleDescription = "Enter only a non-sensitive custom script name.";
        customScriptNameTextBox.Dock = DockStyle.Fill;
        customScriptNameTextBox.Enabled = false;
        customScriptNameTextBox.Location = new Point(145, 3);
        customScriptNameTextBox.Name = "customScriptNameTextBox";
        customScriptNameTextBox.Size = new Size(584, 23);
        customScriptNameTextBox.TabIndex = 1;
        //
        // resolutionNotesLabel
        //
        resolutionNotesLabel.AutoSize = true;
        resolutionNotesLabel.Dock = DockStyle.Fill;
        resolutionNotesLabel.Location = new Point(13, 186);
        resolutionNotesLabel.Margin = new Padding(3, 3, 3, 4);
        resolutionNotesLabel.Name = "resolutionNotesLabel";
        resolutionNotesLabel.Size = new Size(732, 15);
        resolutionNotesLabel.TabIndex = 6;
        resolutionNotesLabel.Text = "Resolution notes";
        //
        // resolutionNotesTextBox
        //
        resolutionNotesTextBox.AcceptsReturn = true;
        resolutionNotesTextBox.AccessibleDescription = "Enter summarized resolution details without guest-level personal information.";
        resolutionNotesTextBox.Dock = DockStyle.Fill;
        resolutionNotesTextBox.Location = new Point(13, 208);
        resolutionNotesTextBox.Margin = new Padding(3, 3, 3, 4);
        resolutionNotesTextBox.MinimumSize = new Size(0, 60);
        resolutionNotesTextBox.Multiline = true;
        resolutionNotesTextBox.Name = "resolutionNotesTextBox";
        resolutionNotesTextBox.ScrollBars = ScrollBars.Vertical;
        resolutionNotesTextBox.Size = new Size(732, 60);
        resolutionNotesTextBox.TabIndex = 7;
        resolutionNotesTextBox.WordWrap = true;
        //
        // explainedNotesNeededLabel
        //
        explainedNotesNeededLabel.AutoSize = true;
        explainedNotesNeededLabel.Dock = DockStyle.Fill;
        explainedNotesNeededLabel.ForeColor = Color.DarkGoldenrod;
        explainedNotesNeededLabel.Location = new Point(13, 272);
        explainedNotesNeededLabel.Margin = new Padding(3, 0, 3, 8);
        explainedNotesNeededLabel.Name = "explainedNotesNeededLabel";
        explainedNotesNeededLabel.Size = new Size(732, 15);
        explainedNotesNeededLabel.TabIndex = 8;
        explainedNotesNeededLabel.Text = "Add resolution notes explaining why this warning is accepted.";
        explainedNotesNeededLabel.Visible = false;
        //
        // QaFindingItemControl
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowOnly;
        BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(mainLayoutPanel);
        MinimumSize = new Size(520, 0);
        Name = "QaFindingItemControl";
        Size = new Size(760, 284);
        mainLayoutPanel.ResumeLayout(false);
        mainLayoutPanel.PerformLayout();
        resolutionGroupBox.ResumeLayout(false);
        resolutionGroupBox.PerformLayout();
        resolutionFlowLayoutPanel.ResumeLayout(false);
        resolutionFlowLayoutPanel.PerformLayout();
        customScriptLayoutPanel.ResumeLayout(false);
        customScriptLayoutPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
