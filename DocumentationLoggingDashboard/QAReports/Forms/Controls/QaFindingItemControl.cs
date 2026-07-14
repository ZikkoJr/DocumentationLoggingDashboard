using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

/// <summary>
/// Presents and directly updates the resolution state of one report finding.
/// </summary>
public partial class QaFindingItemControl : UserControl
{
    private QaFinding? boundFinding;
    private bool customScriptSupportAvailable;
    private bool isSynchronizing;

    public QaFindingItemControl()
    {
        InitializeComponent();

        activeRadioButton.CheckedChanged += (_, _) =>
            ApplyUserResolution(
                QaFindingResolution.Active,
                activeRadioButton.Checked);
        handledByCustomScriptRadioButton.CheckedChanged += (_, _) =>
            ApplyUserResolution(
                QaFindingResolution.HandledByCustomScript,
                handledByCustomScriptRadioButton.Checked);
        explainedAndAcceptedRadioButton.CheckedChanged += (_, _) =>
            ApplyUserResolution(
                QaFindingResolution.ExplainedAndAccepted,
                explainedAndAcceptedRadioButton.Checked);
        customScriptNameTextBox.TextChanged += (_, _) =>
            ApplyCustomScriptName();
        resolutionNotesTextBox.TextChanged += (_, _) =>
            ApplyResolutionNotes();
        SizeChanged += (_, _) => UpdateWrappingWidths();
    }

    /// <summary>
    /// Gets the deterministic ID of the currently bound finding.
    /// </summary>
    public string FindingId => boundFinding?.FindingId ?? string.Empty;

    public event EventHandler? FindingChanged;

    /// <summary>
    /// Binds or refreshes this row from the actual finding owned by the report.
    /// </summary>
    public void Bind(
        QaFinding finding,
        string? relatedCheckDisplayName,
        bool isCustomScriptSupportAvailable)
    {
        ArgumentNullException.ThrowIfNull(finding);

        if (string.IsNullOrWhiteSpace(finding.FindingId))
        {
            throw new ArgumentException(
                "A finding item requires a nonblank deterministic finding ID.",
                nameof(finding));
        }

        ValidateSeverity(finding.Severity);
        ValidateResolution(finding.Resolution);

        bool bindingChanged = !ReferenceEquals(boundFinding, finding);
        boundFinding = finding;
        customScriptSupportAvailable = isCustomScriptSupportAvailable;

        NormalizeBoundFinding();
        SynchronizeFromFinding(relatedCheckDisplayName, bindingChanged);
    }

    /// <summary>
    /// Alias used by keyed findings containers that retain rows across refreshes.
    /// </summary>
    public void BindOrRefresh(
        QaFinding finding,
        string? relatedCheckDisplayName,
        bool isCustomScriptSupportAvailable)
    {
        Bind(
            finding,
            relatedCheckDisplayName,
            isCustomScriptSupportAvailable);
    }

    private void ApplyUserResolution(
        QaFindingResolution resolution,
        bool isChecked)
    {
        if (isSynchronizing || !isChecked || boundFinding is null)
        {
            return;
        }

        if (!IsResolutionAvailable(boundFinding.Severity, resolution))
        {
            SynchronizeFromFinding(
                GetRelatedCheckDisplayNameFromLabel(),
                forceTextRefresh: false);
            return;
        }

        if (boundFinding.Resolution == resolution)
        {
            return;
        }

        boundFinding.Resolution = resolution;
        boundFinding.CustomScriptName = null;
        boundFinding.ResolutionNotes = null;

        SynchronizeFromFinding(
            GetRelatedCheckDisplayNameFromLabel(),
            forceTextRefresh: false);
        OnFindingChanged();
    }

    private void ApplyCustomScriptName()
    {
        if (isSynchronizing || boundFinding is null)
        {
            return;
        }

        if (!customScriptSupportAvailable
            || boundFinding.Resolution !=
                QaFindingResolution.HandledByCustomScript)
        {
            return;
        }

        string? scriptName = TrimToNull(customScriptNameTextBox.Text);

        if (string.Equals(
                boundFinding.CustomScriptName,
                scriptName,
                StringComparison.Ordinal))
        {
            return;
        }

        boundFinding.CustomScriptName = scriptName;
        OnFindingChanged();
    }

    private void ApplyResolutionNotes()
    {
        if (isSynchronizing || boundFinding is null)
        {
            return;
        }

        string? notes = TrimToNull(resolutionNotesTextBox.Text);
        UpdateExplainedNotesPrompt();

        if (string.Equals(
                boundFinding.ResolutionNotes,
                notes,
                StringComparison.Ordinal))
        {
            return;
        }

        boundFinding.ResolutionNotes = notes;
        OnFindingChanged();
    }

    private void NormalizeBoundFinding()
    {
        QaFinding finding = GetBoundFinding();

        finding.CustomScriptName = TrimToNull(finding.CustomScriptName);
        finding.ResolutionNotes = TrimToNull(finding.ResolutionNotes);

        if (finding.Resolution == QaFindingResolution.ExplainedAndAccepted
            && finding.Severity == QaFindingSeverity.Failure)
        {
            finding.Resolution = QaFindingResolution.Active;
            finding.CustomScriptName = null;
            finding.ResolutionNotes = null;
            return;
        }

        if (finding.Resolution == QaFindingResolution.HandledByCustomScript
            && !customScriptSupportAvailable)
        {
            finding.Resolution = QaFindingResolution.Active;
            finding.CustomScriptName = null;
            finding.ResolutionNotes = null;
            return;
        }

        if (finding.Resolution != QaFindingResolution.HandledByCustomScript)
        {
            finding.CustomScriptName = null;
        }
    }

    private void SynchronizeFromFinding(
        string? relatedCheckDisplayName,
        bool forceTextRefresh)
    {
        QaFinding finding = GetBoundFinding();
        bool wasSynchronizing = isSynchronizing;
        isSynchronizing = true;

        try
        {
            string severityText = GetSeverityText(finding.Severity);
            string resolutionText = GetResolutionText(finding.Resolution);

            severityResolutionLabel.Text =
                $"{severityText} \u2014 {resolutionText}";
            severityResolutionLabel.ForeColor = finding.Severity switch
            {
                QaFindingSeverity.Warning => Color.DarkGoldenrod,
                QaFindingSeverity.Failure => Color.Firebrick,
                _ => SystemColors.ControlText
            };
            titleLabel.Text = finding.Title ?? string.Empty;
            descriptionLabel.Text = finding.Description ?? string.Empty;
            SetRelatedCheckText(
                finding.RelatedCheckId,
                relatedCheckDisplayName);

            bool isWarning =
                finding.Severity == QaFindingSeverity.Warning;
            bool isHandled =
                finding.Resolution ==
                    QaFindingResolution.HandledByCustomScript;

            activeRadioButton.Checked =
                finding.Resolution == QaFindingResolution.Active;
            handledByCustomScriptRadioButton.Checked = isHandled;
            handledByCustomScriptRadioButton.Enabled =
                customScriptSupportAvailable;
            explainedAndAcceptedRadioButton.Visible = isWarning;
            explainedAndAcceptedRadioButton.Checked =
                isWarning
                && finding.Resolution ==
                    QaFindingResolution.ExplainedAndAccepted;

            customScriptLayoutPanel.Visible =
                isHandled && customScriptSupportAvailable;
            customScriptNameTextBox.Enabled =
                isHandled && customScriptSupportAvailable;

            SetOptionalText(
                customScriptNameTextBox,
                finding.CustomScriptName,
                forceTextRefresh);
            SetOptionalText(
                resolutionNotesTextBox,
                finding.ResolutionNotes,
                forceTextRefresh);

            AccessibleName = $"{severityText} finding: {finding.Title}";
            AccessibleDescription = finding.Description ?? string.Empty;
            activeRadioButton.AccessibleName =
                $"Active resolution for {finding.Title}";
            handledByCustomScriptRadioButton.AccessibleName =
                $"Handled by custom script resolution for {finding.Title}";
            explainedAndAcceptedRadioButton.AccessibleName =
                $"Explained and accepted resolution for {finding.Title}";
            customScriptNameTextBox.AccessibleName =
                $"Custom script name for {finding.Title}";
            resolutionNotesTextBox.AccessibleName =
                $"Resolution notes for {finding.Title}";

            UpdateExplainedNotesPrompt();
            UpdateWrappingWidths();
        }
        finally
        {
            isSynchronizing = wasSynchronizing;
        }
    }

    private bool IsResolutionAvailable(
        QaFindingSeverity severity,
        QaFindingResolution resolution)
    {
        return resolution switch
        {
            QaFindingResolution.Active => true,
            QaFindingResolution.HandledByCustomScript =>
                customScriptSupportAvailable,
            QaFindingResolution.ExplainedAndAccepted =>
                severity == QaFindingSeverity.Warning,
            _ => false
        };
    }

    private void SetRelatedCheckText(
        string? relatedCheckId,
        string? relatedCheckDisplayName)
    {
        if (string.IsNullOrWhiteSpace(relatedCheckId))
        {
            relatedCheckLabel.Text = string.Empty;
            relatedCheckLabel.Visible = false;
            relatedCheckLabel.Tag = null;
            return;
        }

        string trimmedId = relatedCheckId.Trim();
        string? displayName = TrimToNull(relatedCheckDisplayName);

        relatedCheckLabel.Text = string.IsNullOrEmpty(displayName)
            || displayName.Equals(trimmedId, StringComparison.Ordinal)
                ? $"Related check: {trimmedId}"
                : $"Related check: {displayName} ({trimmedId})";
        relatedCheckLabel.Visible = true;
        relatedCheckLabel.Tag = displayName;
    }

    private string? GetRelatedCheckDisplayNameFromLabel()
    {
        return relatedCheckLabel.Tag as string;
    }

    private void UpdateExplainedNotesPrompt()
    {
        explainedNotesNeededLabel.Visible =
            boundFinding?.Severity == QaFindingSeverity.Warning
            && boundFinding.Resolution ==
                QaFindingResolution.ExplainedAndAccepted
            && string.IsNullOrWhiteSpace(resolutionNotesTextBox.Text);
    }

    private void UpdateWrappingWidths()
    {
        int availableWidth = Math.Max(
            0,
            ClientSize.Width
                - mainLayoutPanel.Padding.Horizontal
                - 8);

        titleLabel.MaximumSize = new Size(availableWidth, 0);
        descriptionLabel.MaximumSize = new Size(availableWidth, 0);
        relatedCheckLabel.MaximumSize = new Size(availableWidth, 0);
    }

    private static void SetOptionalText(
        TextBox textBox,
        string? modelValue,
        bool forceRefresh)
    {
        if (!forceRefresh
            && OptionalTextMatches(textBox.Text, modelValue))
        {
            return;
        }

        textBox.Text = modelValue ?? string.Empty;
    }

    private static bool OptionalTextMatches(
        string controlText,
        string? modelValue)
    {
        return string.Equals(
            TrimToNull(controlText),
            TrimToNull(modelValue),
            StringComparison.Ordinal);
    }

    private static string GetSeverityText(QaFindingSeverity severity)
    {
        return severity switch
        {
            QaFindingSeverity.Warning => "Warning",
            QaFindingSeverity.Failure => "Failure",
            _ => throw new ArgumentOutOfRangeException(
                nameof(severity),
                severity,
                "Unknown QA finding severity.")
        };
    }

    private static string GetResolutionText(QaFindingResolution resolution)
    {
        return resolution switch
        {
            QaFindingResolution.Active => "Active",
            QaFindingResolution.HandledByCustomScript =>
                "Handled by Custom Script",
            QaFindingResolution.ExplainedAndAccepted =>
                "Explained and Accepted",
            _ => throw new ArgumentOutOfRangeException(
                nameof(resolution),
                resolution,
                "Unknown QA finding resolution.")
        };
    }

    private static void ValidateSeverity(QaFindingSeverity severity)
    {
        _ = GetSeverityText(severity);
    }

    private static void ValidateResolution(QaFindingResolution resolution)
    {
        _ = GetResolutionText(resolution);
    }

    private QaFinding GetBoundFinding()
    {
        return boundFinding
            ?? throw new InvalidOperationException(
                "Bind must be called before synchronizing a finding item.");
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private void OnFindingChanged()
    {
        FindingChanged?.Invoke(this, EventArgs.Empty);
    }
}
