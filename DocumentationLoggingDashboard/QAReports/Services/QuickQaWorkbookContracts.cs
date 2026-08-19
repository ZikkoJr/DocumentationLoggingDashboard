using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

internal sealed record QuickQaWorkbookRow(
    DateTimeOffset Timestamp,
    string FileMonth,
    string HotelName,
    string HotelId,
    string PmsName,
    string FileId,
    QaReportStatus FinalStatus,
    string Summary)
{
    public string ResultText => FinalStatus switch
    {
        QaReportStatus.Pass => "Pass",
        QaReportStatus.PassWithWarnings => "Pass with Warnings",
        QaReportStatus.Fail => "Fail",
        _ => throw new ArgumentOutOfRangeException(
            nameof(FinalStatus),
            FinalStatus,
            "Unknown Quick QA final status.")
    };
}

internal sealed record QuickQaWorkbookBuildResult(
    byte[] Content,
    int AppendedRowNumber,
    int PreviousDataRowCount);

internal sealed record QuickQaFileBaseline(
    string Path,
    bool Exists,
    byte[] Content,
    byte[] Sha256,
    long Length);
