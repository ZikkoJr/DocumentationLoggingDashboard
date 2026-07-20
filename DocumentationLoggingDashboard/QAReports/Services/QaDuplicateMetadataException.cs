using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Represents a duplicate metadata key, name, or folder collision.
/// </summary>
public sealed class QaDuplicateMetadataException : QaMetadataException
{
    public QaDuplicateMetadataException(
        QaMetadataFileKind fileKind,
        string metadataFilePath,
        string reason,
        Exception? innerException = null)
        : base(fileKind, metadataFilePath, reason, innerException)
    {
    }
}
