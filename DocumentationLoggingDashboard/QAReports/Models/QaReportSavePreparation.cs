namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Carries an immutable, service-resolved save plan from preparation to commit.
/// </summary>
public sealed class QaReportSavePreparation
{
    private readonly byte[] sourceIndexContent;
    private readonly byte[] updatedIndexContent;

    internal QaReportSavePreparation(
        QaReportSaveRequest request,
        QaHotelMetadata canonicalHotel,
        QaPmsMetadata canonicalPms,
        string hotelDirectoryPath,
        string pmsDirectoryPath,
        string hotelFinalPath,
        string pmsFinalPath,
        QaReportIndexEntry indexEntry,
        byte[] sourceIndexContent,
        byte[] updatedIndexContent,
        QaReportExistingFiles existingFiles)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        CanonicalHotel = Clone(
            canonicalHotel ?? throw new ArgumentNullException(nameof(canonicalHotel)));
        CanonicalPms = Clone(
            canonicalPms ?? throw new ArgumentNullException(nameof(canonicalPms)));
        HotelDirectoryPath = NormalizeFullPath(
            hotelDirectoryPath,
            nameof(hotelDirectoryPath));
        PmsDirectoryPath = NormalizeFullPath(
            pmsDirectoryPath,
            nameof(pmsDirectoryPath));
        HotelFinalPath = NormalizeFullPath(
            hotelFinalPath,
            nameof(hotelFinalPath));
        PmsFinalPath = NormalizeFullPath(
            pmsFinalPath,
            nameof(pmsFinalPath));
        IndexEntry = indexEntry
            ?? throw new ArgumentNullException(nameof(indexEntry));
        this.sourceIndexContent = sourceIndexContent?.ToArray()
            ?? throw new ArgumentNullException(nameof(sourceIndexContent));
        this.updatedIndexContent = updatedIndexContent?.ToArray()
            ?? throw new ArgumentNullException(nameof(updatedIndexContent));
        ExistingFiles = existingFiles
            ?? throw new ArgumentNullException(nameof(existingFiles));

        if (!string.Equals(
                Path.GetDirectoryName(HotelFinalPath),
                HotelDirectoryPath,
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                Path.GetDirectoryName(PmsFinalPath),
                PmsDirectoryPath,
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                Path.GetFileName(HotelFinalPath),
                IndexEntry.Filename,
                StringComparison.Ordinal)
            || !string.Equals(
                Path.GetFileName(PmsFinalPath),
                IndexEntry.Filename,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The prepared final paths do not agree with their service-resolved directories and filename.");
        }
    }

    public string FinalFilename => IndexEntry.Filename;

    public QaReportKey ReportKey => IndexEntry.ReportKey;

    public QaReportStatus FinalStatus => IndexEntry.Status;

    public DateTimeOffset GeneratedAt => Request.GeneratedAt;

    public string HotelName => CanonicalHotel.HotelName;

    public string HotelId => CanonicalHotel.HotelId;

    public string PmsName => CanonicalPms.PmsName;

    public QaFileMonth FileMonth => IndexEntry.FileMonth;

    public string FileId => IndexEntry.FileId
        ?? throw new InvalidOperationException(
            "A newly prepared Detailed QA index entry requires a File ID.");

    public string HotelCopyPath => HotelFinalPath;

    public string PmsCopyPath => PmsFinalPath;

    public QaReportExistingFiles ExistingFiles { get; }

    public bool RequiresOverwriteConfirmation => ExistingFiles.HasMatches;

    internal QaReportSaveRequest Request { get; }

    internal QaHotelMetadata CanonicalHotel { get; }

    internal QaPmsMetadata CanonicalPms { get; }

    internal string HotelDirectoryPath { get; }

    internal string PmsDirectoryPath { get; }

    internal string HotelFinalPath { get; }

    internal string PmsFinalPath { get; }

    internal QaReportIndexEntry IndexEntry { get; }

    internal ReadOnlyMemory<byte> SourceIndexContent => sourceIndexContent;

    internal ReadOnlyMemory<byte> UpdatedIndexContent => updatedIndexContent;

    private static string NormalizeFullPath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "A service-resolved full path is required.",
                parameterName);
        }

        return Path.GetFullPath(path);
    }

    private static QaHotelMetadata Clone(QaHotelMetadata source)
    {
        return new QaHotelMetadata
        {
            HotelId = source.HotelId,
            HotelName = source.HotelName,
            PmsName = source.PmsName,
            FolderName = source.FolderName
        };
    }

    private static QaPmsMetadata Clone(QaPmsMetadata source)
    {
        return new QaPmsMetadata
        {
            PmsName = source.PmsName,
            FolderName = source.FolderName
        };
    }
}
