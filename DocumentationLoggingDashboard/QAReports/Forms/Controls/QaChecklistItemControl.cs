using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

/// <summary>
/// Presents and updates one report-specific result for a stable checklist definition.
/// </summary>
public partial class QaChecklistItemControl : UserControl
{
    private QaCheckResult? boundResult;
    private bool isApplicable;
    private bool isSynchronizing;

    public QaChecklistItemControl()
    {
        InitializeComponent();

        passRadioButton.CheckedChanged += (_, _) =>
            ApplyUserStatus(QaCheckStatus.Pass, passRadioButton.Checked);
        failRadioButton.CheckedChanged += (_, _) =>
            ApplyUserStatus(QaCheckStatus.Fail, failRadioButton.Checked);
    }

    public string CheckId { get; private set; } = string.Empty;

    public event EventHandler? ResultChanged;

    /// <summary>
    /// Binds the control to the actual result object owned by the report draft.
    /// </summary>
    public void Bind(QaCheckDefinition definition, QaCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(result);

        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            throw new ArgumentException(
                "The checklist definition must have a nonblank stable ID.",
                nameof(definition));
        }

        if (!definition.Id.Equals(result.CheckId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The checklist result ID must match the checklist definition ID.",
                nameof(result));
        }

        CheckId = definition.Id;
        boundResult = result;
        boundResult.ResultSource = QaResultSource.Manual;
        isApplicable = boundResult.Status != QaCheckStatus.NotApplicable;

        displayNameLabel.Text = definition.DisplayName;
        descriptionLabel.Text = definition.Description;
        AccessibleName = definition.DisplayName;
        AccessibleDescription = definition.Description;
        passRadioButton.AccessibleName = $"Pass: {definition.DisplayName}";
        failRadioButton.AccessibleName = $"Fail: {definition.DisplayName}";
        descriptionToolTip.SetToolTip(displayNameLabel, definition.Description);
        descriptionToolTip.SetToolTip(descriptionLabel, definition.Description);

        SynchronizeFromResult();
    }

    /// <summary>
    /// Applies checklist applicability and performs the required one-way status transition.
    /// </summary>
    public void SetApplicable(bool applicable)
    {
        QaCheckResult result = GetBoundResult();
        QaCheckStatus previousStatus = result.Status;

        result.ResultSource = QaResultSource.Manual;

        if (!applicable)
        {
            result.Status = QaCheckStatus.NotApplicable;
            result.Notes = null;
            result.EvaluatedAt = null;
        }
        else if (!isApplicable || result.Status == QaCheckStatus.NotApplicable)
        {
            result.Status = QaCheckStatus.NotEvaluated;
            result.Notes = null;
            result.EvaluatedAt = null;
        }

        isApplicable = applicable;
        SynchronizeFromResult();

        if (previousStatus != result.Status)
        {
            OnResultChanged();
        }
    }

    private void ApplyUserStatus(QaCheckStatus status, bool isChecked)
    {
        if (isSynchronizing || !isChecked || !isApplicable || boundResult is null)
        {
            return;
        }

        if (boundResult.Status == status
            && boundResult.ResultSource == QaResultSource.Manual)
        {
            return;
        }

        boundResult.Status = status;
        boundResult.ResultSource = QaResultSource.Manual;
        OnResultChanged();
    }

    private void SynchronizeFromResult()
    {
        QaCheckResult result = GetBoundResult();

        isSynchronizing = true;

        try
        {
            passRadioButton.Checked =
                isApplicable && result.Status == QaCheckStatus.Pass;
            failRadioButton.Checked =
                isApplicable && result.Status == QaCheckStatus.Fail;
            resultFlowLayoutPanel.Enabled = isApplicable;
            Enabled = isApplicable;
            Visible = isApplicable;
        }
        finally
        {
            isSynchronizing = false;
        }
    }

    private QaCheckResult GetBoundResult()
    {
        return boundResult
            ?? throw new InvalidOperationException(
                "Bind must be called before synchronizing a checklist item.");
    }

    private void OnResultChanged()
    {
        ResultChanged?.Invoke(this, EventArgs.Empty);
    }
}
