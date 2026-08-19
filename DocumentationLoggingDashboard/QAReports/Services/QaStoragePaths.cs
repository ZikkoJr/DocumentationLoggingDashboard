namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Provides side-effect-free, strongly defined paths for QA report storage.
/// </summary>
public sealed class QaStoragePaths
{
    private static readonly QaFolderNameSanitizer FolderNameSanitizer = new();

    public QaStoragePaths(string documentationRootPath)
    {
        if (string.IsNullOrWhiteSpace(documentationRootPath))
        {
            throw new ArgumentException("A documentation root path is required.", nameof(documentationRootPath));
        }

        DocumentationRootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(documentationRootPath));
        QaReportsRootPath = ResolveFixedDescendant(DocumentationRootPath, "QAReports");
        ByHotelRootPath = ResolveFixedDescendant(QaReportsRootPath, "ByHotel");
        ByPmsRootPath = ResolveFixedDescendant(QaReportsRootPath, "ByPMS");
        SurfaceQaRootPath = ResolveFixedDescendant(QaReportsRootPath, "SurfaceQA");
        MetadataRootPath = ResolveFixedDescendant(QaReportsRootPath, "Metadata");
        HotelsMetadataFilePath = ResolveFixedDescendant(MetadataRootPath, "hotels.json");
        PmsSystemsMetadataFilePath = ResolveFixedDescendant(MetadataRootPath, "pms-systems.json");
        QuickQaSettingsFilePath = ResolveFixedDescendant(MetadataRootPath, "quick-qa-settings.json");
        IndexRootPath = ResolveFixedDescendant(QaReportsRootPath, "Index");
        QaReportIndexFilePath = ResolveFixedDescendant(IndexRootPath, "QAReportIndex.txt");
        MetadataBackupRootPath = ResolveFixedDescendant(MetadataRootPath, "Backups");

        EnsureDescendantOfDocumentationRoot(QaReportsRootPath);
        EnsureDescendantOfDocumentationRoot(ByHotelRootPath);
        EnsureDescendantOfDocumentationRoot(ByPmsRootPath);
        EnsureDescendantOfDocumentationRoot(SurfaceQaRootPath);
        EnsureDescendantOfDocumentationRoot(MetadataRootPath);
        EnsureDescendantOfDocumentationRoot(HotelsMetadataFilePath);
        EnsureDescendantOfDocumentationRoot(PmsSystemsMetadataFilePath);
        EnsureDescendantOfDocumentationRoot(QuickQaSettingsFilePath);
        EnsureDescendantOfDocumentationRoot(IndexRootPath);
        EnsureDescendantOfDocumentationRoot(QaReportIndexFilePath);
        EnsureDescendantOfDocumentationRoot(MetadataBackupRootPath);
    }

    public string DocumentationRootPath { get; }

    public string QaReportsRootPath { get; }

    public string ByHotelRootPath { get; }

    public string ByPmsRootPath { get; }

    public string SurfaceQaRootPath { get; }

    public string MetadataRootPath { get; }

    public string HotelsMetadataFilePath { get; }

    public string PmsSystemsMetadataFilePath { get; }

    public string QuickQaSettingsFilePath { get; }

    public string IndexRootPath { get; }

    public string QaReportIndexFilePath { get; }

    public string MetadataBackupRootPath { get; }

    public string ResolveHotelDirectory(string folderName)
    {
        return ResolveValidatedLeafDirectory(ByHotelRootPath, folderName, nameof(folderName));
    }

    public string ResolvePmsDirectory(string folderName)
    {
        return ResolveValidatedLeafDirectory(ByPmsRootPath, folderName, nameof(folderName));
    }

    public string ResolveSurfaceQaWorkbookPath(string workbookFileName)
    {
        if (!QuickQaWorkbookFilenameService.IsSafeStoredWorkbookFileName(workbookFileName))
        {
            throw new ArgumentException(
                "The Surface QA workbook filename must be a safe .xlsx leaf filename.",
                nameof(workbookFileName));
        }

        return ResolveStrictDirectChildFile(
            SurfaceQaRootPath,
            workbookFileName,
            nameof(workbookFileName));
    }

    public string ResolveQuickQaHistoryPath(string hotelFolderName)
    {
        string hotelDirectory = ResolveHotelDirectory(hotelFolderName);
        return ResolveStrictDirectChildFile(
            hotelDirectory,
            "QuickQAHistory.xlsx",
            nameof(hotelFolderName));
    }

    private static string ResolveFixedDescendant(string parentPath, string childName)
    {
        string resolvedPath = Path.GetFullPath(Path.Combine(parentPath, childName));

        if (!IsDescendant(parentPath, resolvedPath))
        {
            throw new InvalidOperationException("A fixed QA storage path escaped its assigned parent path.");
        }

        return resolvedPath;
    }

    private static string ResolveValidatedLeafDirectory(
        string parentPath,
        string folderName,
        string parameterName)
    {
        if (!FolderNameSanitizer.IsSafeStoredLeafName(folderName))
        {
            throw new ArgumentException("A safe Windows directory leaf name is required.", parameterName);
        }

        string resolvedPath = Path.GetFullPath(Path.Combine(parentPath, folderName));

        if (!IsDescendant(parentPath, resolvedPath))
        {
            throw new ArgumentException("The directory leaf must remain within its assigned QA storage root.", parameterName);
        }

        return resolvedPath;
    }

    private static string ResolveStrictDirectChildFile(
        string parentDirectory,
        string fileName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || Path.IsPathRooted(fileName)
            || fileName.IndexOf(Path.DirectorySeparatorChar) >= 0
            || fileName.IndexOf(Path.AltDirectorySeparatorChar) >= 0
            || !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The filename must be a direct child filename with no path components.",
                parameterName);
        }

        string normalizedParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parentDirectory));
        string candidatePath = Path.GetFullPath(Path.Combine(normalizedParent, fileName));
        string? candidateParent = Path.GetDirectoryName(candidatePath);

        if (candidateParent is null
            || !string.Equals(
                Path.TrimEndingDirectorySeparator(candidateParent),
                normalizedParent,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The resolved file must be a direct child of the expected directory.",
                parameterName);
        }

        return candidatePath;
    }

    private void EnsureDescendantOfDocumentationRoot(string path)
    {
        if (!IsDescendant(DocumentationRootPath, path))
        {
            throw new InvalidOperationException("A fixed QA storage path escaped the documentation root.");
        }
    }

    private static bool IsDescendant(string parentPath, string candidatePath)
    {
        string normalizedParentPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parentPath));
        string normalizedCandidatePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidatePath));
        string parentWithSeparator = normalizedParentPath.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedParentPath
            : normalizedParentPath + Path.DirectorySeparatorChar;

        return normalizedCandidatePath.StartsWith(parentWithSeparator, StringComparison.OrdinalIgnoreCase);
    }
}
