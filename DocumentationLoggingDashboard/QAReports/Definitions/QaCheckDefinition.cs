using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Definitions;

/// <summary>
/// Describes immutable metadata for a single QA checklist item.
/// </summary>
public sealed class QaCheckDefinition
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public QaChecklistSection Section { get; init; }

    public QaCheckApplicability Applicability { get; init; }

    public bool AppearsOnGeneratedReport { get; init; }

    public bool MustBeResolvedBeforeReportGeneration { get; init; }
}
