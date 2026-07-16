using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Finds current-key QA report PDFs in the two canonical destination folders.
/// </summary>
public sealed class QaReportExistingFileService
{
    private readonly QaStoragePaths paths;
    private readonly QaReportFilenameParser filenameParser;

    public QaReportExistingFileService(
        QaStoragePaths paths,
        QaReportFilenameParser filenameParser)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.filenameParser = filenameParser
            ?? throw new ArgumentNullException(nameof(filenameParser));
    }

    public QaReportExistingFiles FindExistingFiles(
        QaHotelMetadata canonicalHotel,
        QaPmsMetadata canonicalPms,
        QaReportKey reportKey)
    {
        ArgumentNullException.ThrowIfNull(canonicalHotel);
        ArgumentNullException.ThrowIfNull(canonicalPms);
        ArgumentNullException.ThrowIfNull(reportKey);

        if (!string.Equals(
                canonicalHotel.PmsName?.Trim(),
                canonicalPms.PmsName?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The canonical Hotel and PMS metadata do not describe the same PMS.",
                nameof(canonicalPms));
        }

        if (!string.Equals(
                canonicalHotel.HotelId?.Trim(),
                reportKey.HotelId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The canonical Hotel metadata does not agree with the QA report key.",
                nameof(reportKey));
        }

        string hotelDirectory =
            paths.ResolveHotelDirectory(canonicalHotel.FolderName);
        string pmsDirectory =
            paths.ResolvePmsDirectory(canonicalPms.FolderName);
        return new QaReportExistingFiles(
            FindMatches(hotelDirectory, reportKey),
            FindMatches(pmsDirectory, reportKey));
    }

    private IReadOnlyList<string> FindMatches(
        string directoryPath,
        QaReportKey reportKey)
    {
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        List<string> matches = [];
        string normalizedDirectory = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(directoryPath));

        foreach (string candidatePath in Directory.EnumerateFiles(
                     normalizedDirectory,
                     "*",
                     SearchOption.TopDirectoryOnly))
        {
            string filename = Path.GetFileName(candidatePath);

            if (filename.Length > QaReportFilenameService.MaximumFilenameLength
                || !string.Equals(
                    Path.GetExtension(filename),
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!filenameParser.TryParse(
                    filename,
                    out QaReportFilenameParts? filenameParts)
                || filenameParts.ReportKey != reportKey)
            {
                continue;
            }

            string fullCandidatePath = Path.GetFullPath(candidatePath);
            string? candidateDirectory = Path.GetDirectoryName(
                fullCandidatePath);

            if (!string.Equals(
                    Path.TrimEndingDirectorySeparator(candidateDirectory ?? string.Empty),
                    normalizedDirectory,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "An enumerated QA report path escaped its canonical destination directory.");
            }

            matches.Add(fullCandidatePath);
        }

        return matches;
    }
}
