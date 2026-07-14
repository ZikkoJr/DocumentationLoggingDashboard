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
        warningFoundCheckBox.CheckedChanged += (_, _) =>
            ApplyWarningSelection();
        notesTextBox.TextChanged += (_, _) => ApplyNotes();
    }

    public string CheckId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether the user selected a separate warning for this passed check.
    /// The report's deterministic warning finding remains the authoritative state.
    /// </summary>
    public bool WarningFound => warningFoundCheckBox.Checked;

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
        warningFoundCheckBox.AccessibleName =
            $"Warning found for passed check: {definition.DisplayName}";
        notesTextBox.AccessibleName =
            $"Check notes or warning explanation: {definition.DisplayName}";
        descriptionToolTip.SetToolTip(displayNameLabel, definition.Description);
        descriptionToolTip.SetToolTip(descriptionLabel, definition.Description);

        SetWarningFoundCore(warningFound: false);
        SynchronizeFromResult();
    }

    /// <summary>
    /// Synchronizes the warning checkbox from the corresponding deterministic finding.
    /// Programmatic synchronization does not raise <see cref="ResultChanged"/>.
    /// </summary>
    public void SetWarningFound(bool warningFound)
    {
        QaCheckResult result = GetBoundResult();
        bool canSelectWarning =
            isApplicable && result.Status == QaCheckStatus.Pass;

        SetWarningFoundCore(warningFound && canSelectWarning);
    }

    /// <summary>
    /// Applies checklist applicability and performs the required one-way status transition.
    /// </summary>
    public void SetApplicable(bool applicable)
    {
        QaCheckResult result = GetBoundResult();
        QaCheckStatus previousStatus = result.Status;
        string? previousNotes = result.Notes;
        bool previouslyWarningFound = WarningFound;

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

        if (previousStatus != result.Status
            || !string.Equals(previousNotes, result.Notes, StringComparison.Ordinal)
            || previouslyWarningFound != WarningFound)
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
        SynchronizeFromResult();
        OnResultChanged();
    }

    private void ApplyWarningSelection()
    {
        if (isSynchronizing || boundResult is null)
        {
            return;
        }

        if (!isApplicable || boundResult.Status != QaCheckStatus.Pass)
        {
            SetWarningFoundCore(warningFound: false);
            return;
        }

        UpdateWarningExplanationState();
        OnResultChanged();
    }

    private void ApplyNotes()
    {
        if (isSynchronizing || !isApplicable || boundResult is null)
        {
            return;
        }

        string? notes = TrimToNull(notesTextBox.Text);
        UpdateWarningExplanationState();

        if (string.Equals(boundResult.Notes, notes, StringComparison.Ordinal))
        {
            return;
        }

        boundResult.Notes = notes;
        boundResult.ResultSource = QaResultSource.Manual;
        OnResultChanged();
    }

    private void SynchronizeFromResult()
    {
        QaCheckResult result = GetBoundResult();
        bool wasSynchronizing = isSynchronizing;

        isSynchronizing = true;

        try
        {
            passRadioButton.Checked =
                isApplicable && result.Status == QaCheckStatus.Pass;
            failRadioButton.Checked =
                isApplicable && result.Status == QaCheckStatus.Fail;

            if (!isApplicable)
            {
                if (notesTextBox.Text.Length != 0)
                {
                    notesTextBox.Clear();
                }
            }
            else if (!OptionalTextMatches(notesTextBox.Text, result.Notes))
            {
                notesTextBox.Text = result.Notes ?? string.Empty;
            }

            bool canSelectWarning =
                isApplicable && result.Status == QaCheckStatus.Pass;

            if (!canSelectWarning)
            {
                warningFoundCheckBox.Checked = false;
            }

            warningFoundCheckBox.Enabled = canSelectWarning;
            notesTextBox.Enabled = isApplicable;
            resultFlowLayoutPanel.Enabled = isApplicable;
            Enabled = isApplicable;
            Visible = isApplicable;
            UpdateWarningExplanationState();
        }
        finally
        {
            isSynchronizing = wasSynchronizing;
        }
    }

    private void SetWarningFoundCore(bool warningFound)
    {
        bool wasSynchronizing = isSynchronizing;
        isSynchronizing = true;

        try
        {
            warningFoundCheckBox.Checked = warningFound;
            UpdateWarningExplanationState();
        }
        finally
        {
            isSynchronizing = wasSynchronizing;
        }
    }

    private void UpdateWarningExplanationState()
    {
        warningExplanationNeededLabel.Visible =
            warningFoundCheckBox.Checked
            && string.IsNullOrWhiteSpace(notesTextBox.Text);
    }

    private static bool OptionalTextMatches(string controlText, string? modelValue)
    {
        return string.Equals(
            TrimToNull(controlText),
            TrimToNull(modelValue),
            StringComparison.Ordinal);
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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
