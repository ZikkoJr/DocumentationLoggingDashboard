namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Describes the structured outcome of one Phase 9 save operation.
/// </summary>
public sealed class QaReportSaveResult
{
    internal QaReportSaveResult(
        bool success,
        bool cancelled,
        string finalFilename,
        string hotelCopyPath,
        string pmsCopyPath,
        QaReportKey reportKey,
        QaReportStatus finalStatus,
        bool overwriteOccurred,
        int hotelMatchesReplaced,
        int pmsMatchesReplaced,
        bool indexUpdated,
        int pdfByteLength,
        DateTimeOffset savedAt,
        string? cleanupWarning,
        bool manualReviewRequired)
    {
        if (success && cancelled)
        {
            throw new ArgumentException(
                "A save result cannot be both successful and cancelled.");
        }

        if (!Enum.IsDefined(finalStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(finalStatus),
                finalStatus,
                "The QA report status is invalid.");
        }

        if (hotelMatchesReplaced < 0 || pmsMatchesReplaced < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hotelMatchesReplaced),
                "Replaced-file counts cannot be negative.");
        }

        if (pdfByteLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pdfByteLength),
                "The PDF byte length cannot be negative.");
        }

        Success = success;
        Cancelled = cancelled;
        FinalFilename = RequireValue(finalFilename, nameof(finalFilename));
        HotelCopyPath = NormalizeFullPath(
            hotelCopyPath,
            nameof(hotelCopyPath));
        PmsCopyPath = NormalizeFullPath(
            pmsCopyPath,
            nameof(pmsCopyPath));
        ReportKey = reportKey
            ?? throw new ArgumentNullException(nameof(reportKey));
        FinalStatus = finalStatus;
        OverwriteOccurred = overwriteOccurred;
        HotelMatchesReplaced = hotelMatchesReplaced;
        PmsMatchesReplaced = pmsMatchesReplaced;
        IndexUpdated = indexUpdated;
        PdfByteLength = pdfByteLength;
        SavedAtUtc = savedAt.ToUniversalTime();
        CleanupWarning = string.IsNullOrWhiteSpace(cleanupWarning)
            ? null
            : cleanupWarning.Trim();
        ManualReviewRequired = manualReviewRequired;
    }

    public bool Success { get; }

    public bool Cancelled { get; }

    public string FinalFilename { get; }

    public string HotelCopyPath { get; }

    public string PmsCopyPath { get; }

    public QaReportKey ReportKey { get; }

    public QaReportStatus FinalStatus { get; }

    public bool OverwriteOccurred { get; }

    public int HotelMatchesReplaced { get; }

    public int PmsMatchesReplaced { get; }

    public int TotalMatchesReplaced =>
        HotelMatchesReplaced + PmsMatchesReplaced;

    public bool IndexUpdated { get; }

    public int PdfByteLength { get; }

    public DateTimeOffset SavedAtUtc { get; }

    public string? CleanupWarning { get; }

    public bool ManualReviewRequired { get; }

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank save-result value is required.",
                parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeFullPath(string value, string parameterName)
    {
        return Path.GetFullPath(RequireValue(value, parameterName));
    }
}
