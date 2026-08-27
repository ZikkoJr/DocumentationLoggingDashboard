namespace DocumentationLoggingDashboard.DocumentationLogs;

public sealed record DocumentationLogWorkbookInfo(
    string FileName,
    string FullPath,
    DocumentationLogWorkbookContract Contract,
    string WorksheetName,
    int DataRowCount,
    DateTime LastWriteTimeUtc);

public sealed record DocumentationLogWorkbookBuildResult(
    byte[] Content,
    int AppendedRowNumber,
    int PreviousDataRowCount);
