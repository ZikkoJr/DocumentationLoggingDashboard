namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Represents one deterministic, privacy-limited QA report index entry.
/// </summary>
public sealed class QaReportIndexEntry
{
    public QaReportIndexEntry(
        QaReportKey reportKey,
        DateOnly qaDate,
        string hotelName,
        string hotelId,
        string pmsName,
        QaFileMonth fileMonth,
        QaReportStatus status,
        string effectiveCreatedBy,
        string filename,
        string relativeHotelCopyPath,
        string relativePmsCopyPath,
        DateTimeOffset savedAt)
    {
        ReportKey = reportKey
            ?? throw new ArgumentNullException(nameof(reportKey));
        FileMonth = fileMonth
            ?? throw new ArgumentNullException(nameof(fileMonth));

        if (qaDate == default)
        {
            throw new ArgumentException(
                "A QA Date is required for the index entry.",
                nameof(qaDate));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "The QA report status is invalid.");
        }

        HotelName = RequireValue(hotelName, nameof(hotelName));
        HotelId = RequireValue(hotelId, nameof(hotelId));
        PmsName = RequireValue(pmsName, nameof(pmsName));
        EffectiveCreatedBy = RequireValue(
            effectiveCreatedBy,
            nameof(effectiveCreatedBy));
        Filename = RequireValue(filename, nameof(filename));
        RelativeHotelCopyPath = NormalizeRelativePath(
            relativeHotelCopyPath,
            nameof(relativeHotelCopyPath));
        RelativePmsCopyPath = NormalizeRelativePath(
            relativePmsCopyPath,
            nameof(relativePmsCopyPath));

        if (!string.Equals(
                ReportKey.HotelId,
                HotelId,
                StringComparison.OrdinalIgnoreCase)
            || ReportKey.FileMonth != FileMonth)
        {
            throw new ArgumentException(
                "The index identity must agree with its QA report key.",
                nameof(reportKey));
        }

        QaDate = qaDate;
        Status = status;
        SavedAtUtc = savedAt.ToUniversalTime();
    }

    public QaReportKey ReportKey { get; }

    public DateOnly QaDate { get; }

    public string HotelName { get; }

    public string HotelId { get; }

    public string PmsName { get; }

    public QaFileMonth FileMonth { get; }

    public QaReportStatus Status { get; }

    public string EffectiveCreatedBy { get; }

    public string Filename { get; }

    public string RelativeHotelCopyPath { get; }

    public string RelativePmsCopyPath { get; }

    public DateTimeOffset SavedAtUtc { get; }

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank index value is required.",
                parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeRelativePath(
        string value,
        string parameterName)
    {
        string relativePath = RequireValue(value, parameterName)
            .Replace('\\', '/');

        if (Path.IsPathRooted(relativePath)
            || relativePath.StartsWith("/", StringComparison.Ordinal)
            || relativePath.Split('/').Any(
                segment => segment is "." or ".." or ""))
        {
            throw new ArgumentException(
                "An index copy path must be a safe relative path.",
                parameterName);
        }

        return relativePath;
    }
}
