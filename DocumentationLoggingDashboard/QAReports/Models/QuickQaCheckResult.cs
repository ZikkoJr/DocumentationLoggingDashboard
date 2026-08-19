namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Stores the mutable result for one fixed Quick QA checklist definition.
/// </summary>
public sealed class QuickQaCheckResult
{
    public string CheckId { get; set; } = string.Empty;

    public QuickQaCheckStatus Status { get; set; } =
        QuickQaCheckStatus.NotEvaluated;
}
