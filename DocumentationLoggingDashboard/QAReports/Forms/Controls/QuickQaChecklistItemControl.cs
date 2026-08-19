using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

/// <summary>
/// Presents one Quick QA checklist result without importing the Detailed QA
/// warning-checkbox contract.
/// </summary>
public sealed class QuickQaChecklistItemControl : UserControl
{
    private readonly Label displayNameLabel = new();
    private readonly FlowLayoutPanel choicesPanel = new();
    private readonly RadioButton passRadioButton = new();
    private readonly RadioButton warningRadioButton = new();
    private readonly RadioButton failRadioButton = new();
    private readonly RadioButton notApplicableRadioButton = new();
    private QuickQaCheckResult? boundResult;
    private bool isSynchronizing;

    public QuickQaChecklistItemControl()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BorderStyle = BorderStyle.FixedSingle;
        Dock = DockStyle.Top;
        MinimumSize = new Size(620, 0);
        Padding = new Padding(10, 8, 10, 8);

        TableLayoutPanel layout = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Top
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        displayNameLabel.AutoSize = true;
        displayNameLabel.Dock = DockStyle.Fill;
        displayNameLabel.Font = new Font(Font, FontStyle.Bold);
        displayNameLabel.Margin = new Padding(3, 5, 12, 5);

        choicesPanel.AutoSize = true;
        choicesPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        choicesPanel.FlowDirection = FlowDirection.LeftToRight;
        choicesPanel.WrapContents = false;

        ConfigureChoice(passRadioButton, "Pass", QuickQaCheckStatus.Pass);
        ConfigureChoice(warningRadioButton, "Warning", QuickQaCheckStatus.Warning);
        ConfigureChoice(failRadioButton, "Fail", QuickQaCheckStatus.Fail);
        ConfigureChoice(
            notApplicableRadioButton,
            "N/A",
            QuickQaCheckStatus.NotApplicable);

        choicesPanel.Controls.Add(passRadioButton);
        choicesPanel.Controls.Add(warningRadioButton);
        choicesPanel.Controls.Add(failRadioButton);
        choicesPanel.Controls.Add(notApplicableRadioButton);
        layout.Controls.Add(displayNameLabel, 0, 0);
        layout.Controls.Add(choicesPanel, 1, 0);
        Controls.Add(layout);
    }

    public string CheckId { get; private set; } = string.Empty;

    public event EventHandler? ResultChanged;

    public void Bind(
        QuickQaCheckDefinition definition,
        QuickQaCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(result);

        if (!string.Equals(definition.Id, result.CheckId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Quick QA result must match the checklist definition.",
                nameof(result));
        }

        boundResult = result;
        CheckId = definition.Id;
        displayNameLabel.Text = definition.DisplayName;
        AccessibleName = definition.DisplayName;

        passRadioButton.Text = definition.IsStrategyAvailability
            ? "Available"
            : "Pass";
        failRadioButton.Text = definition.IsStrategyAvailability
            ? "Unavailable"
            : "Fail";
        warningRadioButton.Visible = !definition.IsStrategyAvailability;
        notApplicableRadioButton.Visible =
            !definition.IsStrategyAvailability && definition.AllowsNotApplicable;

        passRadioButton.AccessibleName = $"{passRadioButton.Text}: {definition.DisplayName}";
        warningRadioButton.AccessibleName = $"Warning: {definition.DisplayName}";
        failRadioButton.AccessibleName = $"{failRadioButton.Text}: {definition.DisplayName}";
        notApplicableRadioButton.AccessibleName = $"Not applicable: {definition.DisplayName}";
        RefreshFromResult();
    }

    public void RefreshFromResult()
    {
        QuickQaCheckResult result = boundResult
            ?? throw new InvalidOperationException(
                "Bind must be called before refreshing a Quick QA checklist row.");

        bool previous = isSynchronizing;
        isSynchronizing = true;

        try
        {
            passRadioButton.Checked = result.Status == QuickQaCheckStatus.Pass;
            warningRadioButton.Checked = result.Status == QuickQaCheckStatus.Warning;
            failRadioButton.Checked = result.Status == QuickQaCheckStatus.Fail;
            notApplicableRadioButton.Checked =
                result.Status == QuickQaCheckStatus.NotApplicable;
        }
        finally
        {
            isSynchronizing = previous;
        }
    }

    private void ConfigureChoice(
        RadioButton radioButton,
        string text,
        QuickQaCheckStatus status)
    {
        radioButton.AutoSize = true;
        radioButton.Margin = new Padding(3, 3, 10, 3);
        radioButton.Text = text;
        radioButton.UseVisualStyleBackColor = true;
        radioButton.CheckedChanged += (_, _) => ApplyStatus(
            status,
            radioButton.Checked);
    }

    private void ApplyStatus(QuickQaCheckStatus status, bool isChecked)
    {
        if (isSynchronizing || !isChecked || boundResult is null)
        {
            return;
        }

        if (boundResult.Status == status)
        {
            return;
        }

        boundResult.Status = status;
        ResultChanged?.Invoke(this, EventArgs.Empty);
    }
}
