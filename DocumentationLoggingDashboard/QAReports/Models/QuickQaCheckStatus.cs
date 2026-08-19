namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Defines the direct result states available to a Quick QA checklist row.
/// </summary>
public enum QuickQaCheckStatus
{
    NotEvaluated,
    Pass,
    Warning,
    Fail,
    NotApplicable
}
