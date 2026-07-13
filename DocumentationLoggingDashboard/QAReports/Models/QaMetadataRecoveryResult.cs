namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Describes the files involved in one explicit QA metadata recovery operation.
/// </summary>
public sealed record QaMetadataRecoveryResult(
    QaMetadataFileKind FileKind,
    string OriginalMetadataPath,
    string BackupPath);
