namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Describes one validated, direct-child Surface QA workbook.
/// </summary>
public sealed class QuickQaSurfaceWorkbookInfo
{
    internal QuickQaSurfaceWorkbookInfo(
        string fileName,
        string fullPath,
        string worksheetName,
        int dataRowCount,
        bool usedLegacyHeaders,
        DateTime lastWriteTimeUtc)
    {
        FileName = fileName;
        FullPath = Path.GetFullPath(fullPath);
        WorksheetName = worksheetName;
        DataRowCount = dataRowCount;
        UsedLegacyHeaders = usedLegacyHeaders;
        LastWriteTimeUtc = DateTime.SpecifyKind(
            lastWriteTimeUtc,
            DateTimeKind.Utc);
    }

    public string FileName { get; }

    public string FullPath { get; }

    public string WorksheetName { get; }

    public int DataRowCount { get; }

    public bool UsedLegacyHeaders { get; }

    public DateTime LastWriteTimeUtc { get; }
}
