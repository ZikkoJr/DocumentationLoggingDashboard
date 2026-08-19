using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

/// <summary>
/// Presents and updates one report-specific result for a stable checklist definition.
/// </summary>
public partial class QaChecklistItemControl : UserControl
{
    private QaCheckResult? boundResult;
    private string boundDisplayName = string.Empty;
    private bool hasPendingNotesEdit;
    private bool isApplicable;
    private bool isSynchronizing;
    private bool allowsPassedWarning = true;

    public QaChecklistItemControl()
    {
        InitializeComponent();

        passRadioButton.CheckedChanged += (_, _) =>
            ApplyUserStatus(QaCheckStatus.Pass, passRadioButton.Checked);
        failRadioButton.CheckedChanged += (_, _) =>
            ApplyUserStatus(QaCheckStatus.Fail, failRadioButton.Checked);
        warningFoundCheckBox.CheckedChanged += (_, _) =>
            ApplyWarningSelection();
        notesTextBox.TextChanged += (_, _) => MarkNotesEditPending();
        notesTextBox.Validated += (_, _) => CommitPendingTextEdits();
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

        bool bindingChanged = !ReferenceEquals(boundResult, result);

        if (bindingChanged && boundResult is not null)
        {
            bool sameCheckId = boundResult.CheckId.Equals(
                result.CheckId,
                StringComparison.Ordinal);
            _ = CommitPendingTextEditsCore(raiseResultChanged: false);

            if (sameCheckId)
            {
                result.Notes = boundResult.Notes;
            }
        }

        CheckId = definition.Id;
        boundDisplayName = definition.DisplayName;
        boundResult = result;
        if (bindingChanged)
        {
            hasPendingNotesEdit = false;
        }

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
    /// Presents the existing Pass/Fail states as Available/Unavailable and removes
    /// the ordinary passed-check warning choice. Aggregate availability rules remain
    /// authoritative in the finding synchronization service.
    /// </summary>
    public void UseAvailabilityPresentation()
    {
        allowsPassedWarning = false;
        passRadioButton.Text = "Available";
        failRadioButton.Text = "Unavailable";
        passRadioButton.AccessibleName = $"Available: {boundDisplayName}";
        failRadioButton.AccessibleName = $"Unavailable: {boundDisplayName}";
        warningFoundCheckBox.Visible = false;
        warningExplanationNeededLabel.Visible = false;
        SetWarningFoundCore(warningFound: false);

        if (boundResult is not null)
        {
            SynchronizeFromResult();
        }
    }

    /// <summary>
    /// Commits the final normalized note and raises at most one logical result
    /// change event.
    /// </summary>
    public bool CommitPendingTextEdits()
    {
        return CommitPendingTextEditsCore(raiseResultChanged: true);
    }

    /// <summary>
    /// Synchronizes the warning checkbox from the corresponding deterministic finding.
    /// Programmatic synchronization does not raise <see cref="ResultChanged"/>.
    /// </summary>
    public void SetWarningFound(bool warningFound)
    {
        QaCheckResult result = GetBoundResult();
        bool canSelectWarning =
            allowsPassedWarning
            && isApplicable
            && result.Status == QaCheckStatus.Pass;

        SetWarningFoundCore(warningFound && canSelectWarning);
    }

    /// <summary>
    /// Applies checklist applicability and performs the required one-way status transition.
    /// </summary>
    public void SetApplicable(bool applicable)
    {
        QaCheckResult result = GetBoundResult();
        bool pendingTextChanged =
            CommitPendingTextEditsCore(raiseResultChanged: false);
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

        if (pendingTextChanged
            || previousStatus != result.Status
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

        bool pendingTextChanged =
            CommitPendingTextEditsCore(raiseResultChanged: false);

        if (boundResult.Status == status
            && boundResult.ResultSource == QaResultSource.Manual)
        {
            if (pendingTextChanged)
            {
                OnResultChanged();
            }

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

        bool pendingTextChanged =
            CommitPendingTextEditsCore(raiseResultChanged: false);

        if (!allowsPassedWarning
            || !isApplicable
            || boundResult.Status != QaCheckStatus.Pass)
        {
            SetWarningFoundCore(warningFound: false);

            if (pendingTextChanged)
            {
                OnResultChanged();
            }

            return;
        }

        UpdateWarningExplanationState();
        OnResultChanged();
    }

    private void MarkNotesEditPending()
    {
        if (isSynchronizing || !isApplicable || boundResult is null)
        {
            return;
        }

        hasPendingNotesEdit = true;
        UpdateWarningExplanationState();
    }

    private bool CommitPendingTextEditsCore(bool raiseResultChanged)
    {
        if (isSynchronizing
            || !hasPendingNotesEdit
            || !isApplicable
            || boundResult is null)
        {
            return false;
        }

        string? notes = TrimToNull(notesTextBox.Text);
        bool changed = !string.Equals(
            boundResult.Notes,
            notes,
            StringComparison.Ordinal);

        if (changed)
        {
            boundResult.Notes = notes;
            boundResult.ResultSource = QaResultSource.Manual;
        }

        hasPendingNotesEdit = false;
        UpdateWarningExplanationState();

        if (changed && raiseResultChanged)
        {
            OnResultChanged();
        }

        return changed;
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
                hasPendingNotesEdit = false;
                if (notesTextBox.Text.Length != 0)
                {
                    notesTextBox.Clear();
                }
            }
            else if (!hasPendingNotesEdit
                && !OptionalTextMatches(notesTextBox.Text, result.Notes))
            {
                notesTextBox.Text = result.Notes ?? string.Empty;
            }

            bool canSelectWarning =
                allowsPassedWarning
                && isApplicable
                && result.Status == QaCheckStatus.Pass;

            if (!canSelectWarning)
            {
                warningFoundCheckBox.Checked = false;
            }

            warningFoundCheckBox.Enabled = canSelectWarning;
            warningFoundCheckBox.Visible = allowsPassedWarning;
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
            allowsPassedWarning
            && warningFoundCheckBox.Checked
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
