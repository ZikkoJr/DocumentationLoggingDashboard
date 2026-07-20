namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Captures basic source-file row and header statistics.
/// </summary>
public sealed class QaFileInformationStatistics
{
    public int TotalDataRows { get; set; }

    public bool HeadersArePresent { get; set; }

    public QaUsefulHeadersResult UsefulHeaders { get; set; } = QaUsefulHeadersResult.Yes;

    public int DataStartRow { get; set; }
}
