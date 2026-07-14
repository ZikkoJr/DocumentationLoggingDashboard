using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Calculates the final report status from synchronized findings.
/// </summary>
public sealed class QaReportStatusService
{
    public QaReportStatus? CalculateStatus(
        IEnumerable<QaFinding> findings,
        bool hasBlockingErrors)
    {
        ArgumentNullException.ThrowIfNull(findings);

        if (hasBlockingErrors)
        {
            return null;
        }

        QaFinding[] snapshot = findings.ToArray();

        if (snapshot.Any(finding => finding is null))
        {
            throw new ArgumentException(
                "The findings collection cannot contain null entries.",
                nameof(findings));
        }

        if (snapshot.Any(
                finding => finding.Severity == QaFindingSeverity.Failure
                    && finding.Resolution == QaFindingResolution.Active))
        {
            return QaReportStatus.Fail;
        }

        return snapshot.Length == 0
            ? QaReportStatus.Pass
            : QaReportStatus.PassWithWarnings;
    }
}
