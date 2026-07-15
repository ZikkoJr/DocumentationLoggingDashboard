using System.Globalization;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Creates the locked Phase 9 QA report filename from canonical metadata.
/// </summary>
public sealed class QaReportFilenameService
{
    public const int MaximumFilenameLength = 240;

    // The save service applies this policy after resolving the authoritative
    // destination directories through QaStoragePaths.
    internal const int MaximumSupportedFullPathLength = 259;

    private readonly QaFolderNameSanitizer folderNameSanitizer;

    public QaReportFilenameService(QaFolderNameSanitizer folderNameSanitizer)
    {
        this.folderNameSanitizer = folderNameSanitizer
            ?? throw new ArgumentNullException(nameof(folderNameSanitizer));
    }

    public string CreateFilename(
        QaReport report,
        QaHotelMetadata canonicalHotel,
        QaPmsMetadata canonicalPms)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.QaDate == default)
        {
            throw new ArgumentException(
                "A valid QA Date is required to create the QA report filename.",
                nameof(report));
        }

        QaFileMonth fileMonth = report.HotelInformation?.FileMonth
            ?? throw new ArgumentException(
                "A valid File Month is required to create the QA report filename.",
                nameof(report));
        ValidateFileMonth(fileMonth, nameof(report));

        QaReportFilenameComponents components =
            CreateSanitizedComponents(canonicalHotel, canonicalPms);

        string filename = string.Create(
            CultureInfo.InvariantCulture,
            $"{report.QaDate:yyyy-MM-dd}_{components.SanitizedHotelName}_{components.SanitizedHotelId}_{components.SanitizedPmsName}_{fileMonth}_QAReport.pdf");

        EnsureStrictLeafFilename(filename);
        return filename;
    }

    internal QaReportFilenameComponents CreateSanitizedComponents(
        QaHotelMetadata canonicalHotel,
        QaPmsMetadata canonicalPms)
    {
        ArgumentNullException.ThrowIfNull(canonicalHotel);
        ArgumentNullException.ThrowIfNull(canonicalPms);

        string sanitizedHotelName =
            folderNameSanitizer.SanitizeLeafName(canonicalHotel.HotelName);
        string sanitizedHotelId =
            folderNameSanitizer.SanitizeLeafName(canonicalHotel.HotelId);
        string sanitizedPmsName =
            folderNameSanitizer.SanitizeLeafName(canonicalPms.PmsName);

        EnsureSafeComponent(sanitizedHotelName, nameof(canonicalHotel));
        EnsureSafeComponent(sanitizedHotelId, nameof(canonicalHotel));
        EnsureSafeComponent(sanitizedPmsName, nameof(canonicalPms));

        return new QaReportFilenameComponents(
            sanitizedHotelName,
            sanitizedHotelId,
            sanitizedPmsName);
    }

    internal void EnsureStrictLeafFilename(string filename)
    {
        if (!folderNameSanitizer.IsSafeGeneratedFileName(
                filename,
                MaximumFilenameLength)
            || !filename.EndsWith(".pdf", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The generated QA report filename is not a safe Windows filename within the {MaximumFilenameLength}-character policy.");
        }

        string leafName;

        try
        {
            leafName = Path.GetFileName(filename);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                "The generated QA report filename is invalid.",
                exception);
        }

        if (!string.Equals(leafName, filename, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The generated QA report filename must be one path-free leaf name.");
        }
    }

    internal static void ValidateFileMonth(
        QaFileMonth fileMonth,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(fileMonth, parameterName);

        if (fileMonth.Year is < 1 or > 9999
            || fileMonth.Month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "A valid File Month is required.");
        }
    }

    private void EnsureSafeComponent(string component, string parameterName)
    {
        if (!folderNameSanitizer.IsSafeStoredLeafName(component))
        {
            throw new ArgumentException(
                "Canonical metadata did not produce a safe filename component.",
                parameterName);
        }
    }

}

internal sealed record QaReportFilenameComponents(
    string SanitizedHotelName,
    string SanitizedHotelId,
    string SanitizedPmsName);
