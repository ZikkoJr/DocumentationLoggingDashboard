namespace DocumentationLoggingDashboard.QAReports.Definitions;

/// <summary>
/// Defines how one stable source-field concept participates in statistics entry.
/// </summary>
public sealed record QaStatisticFieldDefinition(
    string Id,
    string DisplayName,
    QaStatisticFieldApplicability Applicability,
    string? RelatedChecklistId,
    bool SupportsBlankStatistics,
    bool SupportsBrokenDataStatistics);

/// <summary>
/// Defines the file-characteristic rule that controls a statistics field.
/// </summary>
public enum QaStatisticFieldApplicability
{
    Always,
    SeparateNameColumns,
    FullNameColumn,
    StayValueAvailable
}
