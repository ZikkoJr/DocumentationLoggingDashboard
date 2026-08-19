using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

/// <summary>
/// Exposes the only Quick QA finding resolution: an independent custom-script
/// handling choice for this specific warning or failure.
/// </summary>
public sealed class QuickQaFindingItemControl : UserControl
{
    private readonly Label titleLabel = new();
    private readonly Label descriptionLabel = new();
    private readonly CheckBox handledByCustomScriptCheckBox = new();
    private QaFinding? boundFinding;
    private bool isSynchronizing;

    public QuickQaFindingItemControl()
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
            ColumnCount = 1,
            Dock = DockStyle.Top
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        titleLabel.AutoSize = true;
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.Font = new Font(Font, FontStyle.Bold);
        titleLabel.Margin = new Padding(3, 0, 3, 4);

        descriptionLabel.AutoSize = true;
        descriptionLabel.Dock = DockStyle.Fill;
        descriptionLabel.ForeColor = SystemColors.GrayText;
        descriptionLabel.MaximumSize = new Size(900, 0);
        descriptionLabel.Margin = new Padding(3, 0, 3, 6);

        handledByCustomScriptCheckBox.AutoSize = true;
        handledByCustomScriptCheckBox.Text = "Handled by Custom Script";
        handledByCustomScriptCheckBox.CheckedChanged += (_, _) =>
            ApplyResolution();

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(descriptionLabel, 0, 1);
        layout.Controls.Add(handledByCustomScriptCheckBox, 0, 2);
        Controls.Add(layout);
    }

    public string FindingId => boundFinding?.FindingId ?? string.Empty;

    public event EventHandler? ResolutionChanged;

    public void Bind(QaFinding finding, bool customScriptAvailable)
    {
        ArgumentNullException.ThrowIfNull(finding);
        boundFinding = finding;
        titleLabel.Text = $"{FormatSeverity(finding.Severity)}: {finding.Title}";
        descriptionLabel.Text = finding.Description;
        AccessibleName = titleLabel.Text;
        RefreshFromFinding(customScriptAvailable);
    }

    public void RefreshFromFinding(bool customScriptAvailable)
    {
        QaFinding finding = boundFinding
            ?? throw new InvalidOperationException(
                "Bind must be called before refreshing a Quick QA finding row.");

        bool previous = isSynchronizing;
        isSynchronizing = true;

        try
        {
            if (!customScriptAvailable
                && finding.Resolution == QaFindingResolution.HandledByCustomScript)
            {
                finding.Resolution = QaFindingResolution.Active;
                finding.CustomScriptName = null;
            }

            handledByCustomScriptCheckBox.Enabled = customScriptAvailable;
            handledByCustomScriptCheckBox.Checked = customScriptAvailable
                && finding.Resolution == QaFindingResolution.HandledByCustomScript;
        }
        finally
        {
            isSynchronizing = previous;
        }
    }

    private void ApplyResolution()
    {
        if (isSynchronizing || boundFinding is null)
        {
            return;
        }

        boundFinding.Resolution = handledByCustomScriptCheckBox.Checked
            ? QaFindingResolution.HandledByCustomScript
            : QaFindingResolution.Active;
        boundFinding.CustomScriptName = null;
        ResolutionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string FormatSeverity(QaFindingSeverity severity) =>
        severity == QaFindingSeverity.Failure ? "Failure" : "Warning";
}
