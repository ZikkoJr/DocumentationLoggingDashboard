namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Stores the report-specific result for one stable QA checklist definition.
/// </summary>
public sealed class QaCheckResult
{
    public string CheckId { get; set; } = string.Empty;

    public QaCheckStatus Status { get; set; } = QaCheckStatus.NotEvaluated;

    public string? Notes { get; set; }

    public QaResultSource ResultSource { get; set; } = QaResultSource.Manual;

    /// <summary>
    /// Gets or sets the optional audit timestamp for when this result was evaluated.
    /// </summary>
    public DateTimeOffset? EvaluatedAt { get; set; }
}
