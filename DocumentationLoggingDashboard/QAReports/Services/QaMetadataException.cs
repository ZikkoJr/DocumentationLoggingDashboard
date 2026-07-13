using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Represents a failure involving one of the known QA metadata files.
/// </summary>
public class QaMetadataException : Exception
{
    public QaMetadataException(
        QaMetadataFileKind fileKind,
        string metadataFilePath,
        string reason,
        Exception? innerException = null,
        Exception? rollbackException = null)
        : base(BuildMessage(metadataFilePath, reason), innerException)
    {
        FileKind = fileKind;
        MetadataFilePath = metadataFilePath;
        MetadataFileName = Path.GetFileName(metadataFilePath);
        RollbackException = rollbackException;
    }

    public QaMetadataFileKind FileKind { get; }

    public string MetadataFilePath { get; }

    public string MetadataFileName { get; }

    /// <summary>
    /// Gets a secondary failure encountered while attempting to roll back a record directory.
    /// </summary>
    public Exception? RollbackException { get; }

    private static string BuildMessage(string metadataFilePath, string reason)
    {
        string fileName = Path.GetFileName(metadataFilePath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "the selected metadata file";
        }

        return $"QA metadata file '{fileName}' could not be processed: {reason}";
    }
}
