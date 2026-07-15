using System.Globalization;
using System.Text.RegularExpressions;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Finds current-key QA report PDFs in the two canonical destination folders.
/// </summary>
public sealed class QaReportExistingFileService
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private readonly QaStoragePaths paths;
    private readonly QaReportFilenameService filenameService;

    public QaReportExistingFileService(
        QaStoragePaths paths,
        QaReportFilenameService filenameService)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.filenameService = filenameService
            ?? throw new ArgumentNullException(nameof(filenameService));
    }

    public QaReportExistingFiles FindExistingFiles(
        QaHotelMetadata canonicalHotel,
        QaPmsMetadata canonicalPms,
        QaFileMonth fileMonth)
    {
        ArgumentNullException.ThrowIfNull(canonicalHotel);
        ArgumentNullException.ThrowIfNull(canonicalPms);
        QaReportFilenameService.ValidateFileMonth(
            fileMonth,
            nameof(fileMonth));

        if (!string.Equals(
                canonicalHotel.PmsName?.Trim(),
                canonicalPms.PmsName?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The canonical Hotel and PMS metadata do not describe the same PMS.",
                nameof(canonicalPms));
        }

        string hotelDirectory =
            paths.ResolveHotelDirectory(canonicalHotel.FolderName);
        string pmsDirectory =
            paths.ResolvePmsDirectory(canonicalPms.FolderName);
        QaReportFilenameComponents components =
            filenameService.CreateSanitizedComponents(
                canonicalHotel,
                canonicalPms);
        Regex filenamePattern = CreateFilenamePattern(components, fileMonth);

        return new QaReportExistingFiles(
            FindMatches(hotelDirectory, filenamePattern),
            FindMatches(pmsDirectory, filenamePattern));
    }

    private static Regex CreateFilenamePattern(
        QaReportFilenameComponents components,
        QaFileMonth fileMonth)
    {
        string pattern = string.Concat(
            "^(?<qaDate>\\d{4}-\\d{2}-\\d{2})_",
            Regex.Escape(components.SanitizedHotelName),
            "_",
            Regex.Escape(components.SanitizedHotelId),
            "_",
            Regex.Escape(components.SanitizedPmsName),
            "_",
            Regex.Escape(fileMonth.ToString()),
            "_QAReport\\.pdf$");

        return new Regex(
            pattern,
            RegexOptions.CultureInvariant
                | RegexOptions.IgnoreCase
                | RegexOptions.ExplicitCapture,
            RegexTimeout);
    }

    private static IReadOnlyList<string> FindMatches(
        string directoryPath,
        Regex filenamePattern)
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

            Match match = filenamePattern.Match(filename);

            if (!match.Success
                || !DateOnly.TryParseExact(
                    match.Groups["qaDate"].Value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
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
