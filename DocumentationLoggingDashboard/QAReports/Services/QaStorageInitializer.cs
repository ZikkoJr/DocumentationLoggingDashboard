namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Explicitly creates the fixed QA storage structure and missing empty foundation files.
/// </summary>
public sealed class QaStorageInitializer
{
    private readonly QaStoragePaths paths;

    public QaStorageInitializer(QaStoragePaths paths)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <summary>
    /// Creates missing fixed directories and foundation files without changing existing files.
    /// </summary>
    public void Initialize()
    {
        try
        {
            Directory.CreateDirectory(paths.QaReportsRootPath);
            Directory.CreateDirectory(paths.ByHotelRootPath);
            Directory.CreateDirectory(paths.ByPmsRootPath);
            Directory.CreateDirectory(paths.SurfaceQaRootPath);
            Directory.CreateDirectory(paths.MetadataRootPath);
            Directory.CreateDirectory(paths.IndexRootPath);

            CreateMetadataFileIfMissing(
                paths.HotelsMetadataFilePath,
                QaMetadataJson.SerializeToUtf8Bytes(QaMetadataJson.CreateEmptyHotelsDocument()));

            CreateMetadataFileIfMissing(
                paths.PmsSystemsMetadataFilePath,
                QaMetadataJson.SerializeToUtf8Bytes(QaMetadataJson.CreateEmptyPmsSystemsDocument()));

            CreateEmptyIndexIfMissing();
        }
        catch (Exception exception)
        {
            throw new QaStorageInitializationException(
                paths.QaReportsRootPath,
                "One or more required directories or files could not be created.",
                exception);
        }
    }

    private static void CreateMetadataFileIfMissing(string filePath, byte[] content)
    {
        if (File.Exists(filePath))
        {
            return;
        }

        QaAtomicFileWriter.WriteNew(filePath, content);
    }

    private void CreateEmptyIndexIfMissing()
    {
        if (File.Exists(paths.QaReportIndexFilePath))
        {
            return;
        }

        using FileStream stream = new(
            paths.QaReportIndexFilePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        stream.Flush(flushToDisk: true);
    }
}
