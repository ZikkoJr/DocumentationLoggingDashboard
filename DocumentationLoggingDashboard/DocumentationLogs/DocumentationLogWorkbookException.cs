namespace DocumentationLoggingDashboard.DocumentationLogs;

public enum DocumentationLogWorkbookErrorCategory
{
    InvalidFilename,
    NotFound,
    InUseOrUnavailable,
    Corrupt,
    MissingMetadata,
    UnsupportedSchemaVersion,
    WrongLogType,
    WrongScope,
    IncompatibleSchema,
    AmbiguousSchema,
    AlreadyExists,
    ContentVerificationFailure
}

/// <summary>
/// Represents a focused documentation-workbook access, compatibility, or
/// staged-content verification failure.
/// </summary>
public sealed class DocumentationLogWorkbookException : Exception
{
    public DocumentationLogWorkbookException(
        DocumentationLogWorkbookErrorCategory errorCategory,
        string message,
        Exception? innerException = null)
        : this(
            errorCategory,
            expectedContract: null,
            workbookPath: null,
            message,
            innerException)
    {
    }

    internal DocumentationLogWorkbookException(
        DocumentationLogWorkbookErrorCategory errorCategory,
        DocumentationLogWorkbookContract? expectedContract,
        string? workbookPath,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCategory = errorCategory;
        ExpectedContract = expectedContract;
        WorkbookPath = workbookPath is null
            ? null
            : Path.GetFullPath(workbookPath);
    }

    public DocumentationLogWorkbookErrorCategory ErrorCategory { get; }

    // Alias retained for callers that prefer the shorter focused-error name.
    public DocumentationLogWorkbookErrorCategory Category => ErrorCategory;

    public DocumentationLogWorkbookContract? ExpectedContract { get; }

    public string? WorkbookPath { get; }
}
