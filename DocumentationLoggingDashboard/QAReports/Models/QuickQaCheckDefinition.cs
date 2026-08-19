namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Describes one immutable item in the fixed Quick QA checklist.
/// </summary>
public sealed class QuickQaCheckDefinition
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public QaChecklistSection Section { get; init; }

    public bool AllowsNotApplicable { get; init; }

    /// <summary>
    /// Gets whether the UI should present Available/Unavailable rather than
    /// ordinary Quick QA status choices. Unavailable is stored as Fail and is
    /// interpreted by the aggregate Strategy finding service.
    /// </summary>
    public bool IsStrategyAvailability { get; init; }
}
