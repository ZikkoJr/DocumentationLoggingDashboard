using DocumentationLoggingDashboard.QAReports.Definitions;

namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Represents one mutable Quick QA draft without Detailed QA statistics,
/// file characteristics, PDF fields, or report identity behavior.
/// </summary>
public sealed class QuickQaReport
{
    public QuickQaReport()
    {
        ChecklistResults.AddRange(
            QuickQaChecklistCatalog.Definitions.Select(
                definition => new QuickQaCheckResult
                {
                    CheckId = definition.Id,
                    Status = QuickQaCheckStatus.NotEvaluated
                }));
    }

    public QaHotelInformation HotelInformation { get; set; } = new();

    public string FileId { get; set; } = string.Empty;

    public List<QuickQaCheckResult> ChecklistResults { get; } = [];

    public bool CustomScriptAvailable { get; set; }

    public List<QaFinding> Findings { get; } = [];

    public QaReportStatus FinalStatus { get; set; } = QaReportStatus.Pass;

    public string Summary { get; set; } = string.Empty;

    public string? SummaryGeneratedFromFingerprint { get; set; }

    public bool SummaryWasManuallyEdited { get; set; }

    public QuickQaCheckResult GetChecklistResult(string checkId)
    {
        if (string.IsNullOrWhiteSpace(checkId))
        {
            throw new ArgumentException(
                "A Quick QA checklist ID is required.",
                nameof(checkId));
        }

        return ChecklistResults.Single(result => result.CheckId.Equals(
            checkId,
            StringComparison.Ordinal));
    }
}
