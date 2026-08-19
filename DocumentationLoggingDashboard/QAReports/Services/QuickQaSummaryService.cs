using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Generates privacy-safe Quick QA summaries and enforces the explicit
/// generated/manual/stale/regenerated lifecycle.
/// </summary>
public sealed class QuickQaSummaryService
{
    public const string CleanPassSummary =
        "Raw file and Database QA passed successfully";

    public string Generate(QuickQaReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Findings.Count == 0)
        {
            return CleanPassSummary;
        }

        return string.Join(
            Environment.NewLine,
            report.Findings.Select(CreateFindingSentence));
    }

    public string ComputeFingerprint(QuickQaReport report)
    {
        return QuickQaStateFingerprint.Compute(report);
    }

    /// <summary>
    /// Refreshes a generated Summary after QA-state changes. A manually edited
    /// Summary is deliberately retained so the form can display its stale state.
    /// </summary>
    public void Synchronize(QuickQaReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!report.SummaryWasManuallyEdited)
        {
            Regenerate(report);
        }
    }

    public void MarkManuallyEdited(
        QuickQaReport report,
        string? summary)
    {
        ArgumentNullException.ThrowIfNull(report);

        report.Summary = summary ?? string.Empty;
        report.SummaryWasManuallyEdited = true;
    }

    public void Regenerate(QuickQaReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        report.Summary = Generate(report);
        report.SummaryGeneratedFromFingerprint = ComputeFingerprint(report);
        report.SummaryWasManuallyEdited = false;
    }

    public bool IsStale(QuickQaReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return string.IsNullOrWhiteSpace(
                report.SummaryGeneratedFromFingerprint)
            || !string.Equals(
                report.SummaryGeneratedFromFingerprint,
                ComputeFingerprint(report),
                StringComparison.Ordinal);
    }

    private static string CreateFindingSentence(QaFinding? finding)
    {
        if (finding is null)
        {
            throw new InvalidOperationException(
                "Quick QA findings cannot contain null entries.");
        }

        string sentence = NormalizeSentence(finding.Description);

        return finding.Resolution switch
        {
            QaFindingResolution.HandledByCustomScript =>
                AppendContext(sentence, "handled by Custom Script"),
            QaFindingResolution.ExplainedAndAccepted =>
                AppendContext(sentence, "explained and accepted"),
            _ => sentence
        };
    }

    private static string NormalizeSentence(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Every Quick QA finding requires a summary description.");
        }

        string normalized = string.Join(
            ' ',
            value.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        return normalized[^1] is '.' or '!' or '?'
            ? normalized
            : normalized + ".";
    }

    private static string AppendContext(string sentence, string context)
    {
        string stem = sentence.TrimEnd('.', '!', '?');
        return $"{stem}; {context}.";
    }
}
