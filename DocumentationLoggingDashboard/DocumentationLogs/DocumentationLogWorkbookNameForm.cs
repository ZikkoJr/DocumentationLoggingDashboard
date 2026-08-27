namespace DocumentationLoggingDashboard.DocumentationLogs;

/// <summary>
/// Collects a leaf filename for a new documentation-log workbook.
/// Filename validation and filesystem operations remain the caller's responsibility.
/// </summary>
public sealed class DocumentationLogWorkbookNameForm : Form
{
    private readonly TextBox fileNameTextBox;

    public DocumentationLogWorkbookNameForm(string suggestedFileName = "")
    {
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(520, 155);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Create New Log File";

        TableLayoutPanel mainLayoutPanel = new()
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 3
        };
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        Label instructionLabel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            Text = "Workbook filename (.xlsx is added when omitted):"
        };

        fileNameTextBox = new TextBox
        {
            AccessibleName = "New documentation log workbook filename",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 14),
            Text = suggestedFileName ?? string.Empty
        };

        FlowLayoutPanel buttonFlowLayoutPanel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0),
            WrapContents = false
        };

        Button cancelButton = new()
        {
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(6, 0, 0, 0),
            Size = new Size(90, 30),
            Text = "Cancel",
            UseVisualStyleBackColor = true
        };
        Button createButton = new()
        {
            DialogResult = DialogResult.OK,
            Margin = new Padding(6, 0, 0, 0),
            Size = new Size(90, 30),
            Text = "Create",
            UseVisualStyleBackColor = true
        };

        buttonFlowLayoutPanel.Controls.Add(cancelButton);
        buttonFlowLayoutPanel.Controls.Add(createButton);
        mainLayoutPanel.Controls.Add(instructionLabel, 0, 0);
        mainLayoutPanel.Controls.Add(fileNameTextBox, 0, 1);
        mainLayoutPanel.Controls.Add(buttonFlowLayoutPanel, 0, 2);
        Controls.Add(mainLayoutPanel);

        AcceptButton = createButton;
        CancelButton = cancelButton;
        Shown += (_, _) =>
        {
            fileNameTextBox.Focus();
            fileNameTextBox.SelectAll();
        };
    }

    public string RequestedFileName => fileNameTextBox.Text;
}
