namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Describes the verified outcome of one paired Quick QA workbook transaction.
/// </summary>
public sealed class QuickQaSaveResult
{
    internal QuickQaSaveResult(
        string surfaceWorkbookFileName,
        string surfaceWorkbookPath,
        string historyPath,
        DateTimeOffset timestamp,
        QaReportStatus finalStatus,
        string hotelName,
        string hotelId,
        string pmsName,
        string fileId,
        string summary,
        string? cleanupWarning)
    {
        SurfaceWorkbookFileName = surfaceWorkbookFileName;
        SurfaceWorkbookPath = Path.GetFullPath(surfaceWorkbookPath);
        HistoryPath = Path.GetFullPath(historyPath);
        Timestamp = timestamp;
        FinalStatus = finalStatus;
        HotelName = hotelName;
        HotelId = hotelId;
        PmsName = pmsName;
        FileId = fileId;
        Summary = summary;
        CleanupWarning = cleanupWarning;
    }

    public string SurfaceWorkbookFileName { get; }

    public string SurfaceWorkbookPath { get; }

    public string HistoryPath { get; }

    public DateTimeOffset Timestamp { get; }

    public QaReportStatus FinalStatus { get; }

    public string HotelName { get; }

    public string HotelId { get; }

    public string PmsName { get; }

    public string FileId { get; }

    public string Summary { get; }

    public string? CleanupWarning { get; }
}
