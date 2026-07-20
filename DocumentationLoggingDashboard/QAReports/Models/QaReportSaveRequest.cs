using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Carries one freshly validated report and its single generation timestamp
/// into Phase 9 preparation.
/// </summary>
public sealed class QaReportSaveRequest
{
    public QaReportSaveRequest(
        QaReport report,
        QaReportValidationResult validationResult,
        DateTimeOffset generatedAt)
    {
        Report = report ?? throw new ArgumentNullException(nameof(report));
        ValidationResult = validationResult
            ?? throw new ArgumentNullException(nameof(validationResult));
        GeneratedAt = generatedAt;
    }

    public QaReport Report { get; }

    public QaReportValidationResult ValidationResult { get; }

    public DateTimeOffset GeneratedAt { get; }
}
