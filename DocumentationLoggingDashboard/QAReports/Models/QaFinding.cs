namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Represents an auditable warning or failed finding associated with a QA report.
/// </summary>
public sealed class QaFinding
{
    public string FindingId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the related checklist ID, when the finding came from a checklist item.
    /// </summary>
    public string? RelatedCheckId { get; set; }

    public QaFindingSeverity Severity { get; set; } = QaFindingSeverity.Warning;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public QaFindingResolution Resolution { get; set; } = QaFindingResolution.Active;

    public string? ResolutionNotes { get; set; }

    public string? CustomScriptName { get; set; }

    public QaFindingSource Source { get; set; } = QaFindingSource.Manual;
}
