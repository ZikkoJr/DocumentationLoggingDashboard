namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Describes the validated semantic fields of one locked QA report filename.
/// </summary>
public sealed class QaReportFilenameParts
{
    internal QaReportFilenameParts(
        DateOnly qaDate,
        string sanitizedHotelName,
        string sanitizedHotelId,
        string sanitizedPmsName,
        QaFileMonth fileMonth)
    {
        QaDate = qaDate;
        SanitizedHotelName = sanitizedHotelName;
        SanitizedHotelId = sanitizedHotelId;
        SanitizedPmsName = sanitizedPmsName;
        FileMonth = fileMonth;
        ReportKey = new QaReportKey(sanitizedHotelId, fileMonth);
    }

    public DateOnly QaDate { get; }

    public string SanitizedHotelName { get; }

    public string SanitizedHotelId { get; }

    public string SanitizedPmsName { get; }

    public QaFileMonth FileMonth { get; }

    public QaReportKey ReportKey { get; }
}
