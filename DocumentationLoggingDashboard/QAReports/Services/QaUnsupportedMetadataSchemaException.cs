using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Represents a QA metadata document whose schema version is not supported.
/// </summary>
public sealed class QaUnsupportedMetadataSchemaException : QaMetadataException
{
    public QaUnsupportedMetadataSchemaException(
        QaMetadataFileKind fileKind,
        string metadataFilePath,
        string reason,
        Exception? innerException = null)
        : base(fileKind, metadataFilePath, reason, innerException)
    {
    }

    public QaUnsupportedMetadataSchemaException(
        QaMetadataFileKind fileKind,
        string metadataFilePath,
        int actualSchemaVersion,
        int expectedSchemaVersion,
        Exception? innerException = null)
        : this(
            fileKind,
            metadataFilePath,
            $"Schema version {actualSchemaVersion} is unsupported; expected version {expectedSchemaVersion}.",
            innerException)
    {
        ActualSchemaVersion = actualSchemaVersion;
        ExpectedSchemaVersion = expectedSchemaVersion;
    }

    public int? ActualSchemaVersion { get; }

    public int? ExpectedSchemaVersion { get; }
}
