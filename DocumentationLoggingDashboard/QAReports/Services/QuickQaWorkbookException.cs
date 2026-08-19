namespace DocumentationLoggingDashboard.QAReports.Services;

public enum QuickQaWorkbookErrorCategory
{
    InvalidFilename,
    NotFound,
    InUseOrUnavailable,
    Corrupt,
    IncompatibleSchema,
    AmbiguousSchema,
    AlreadyExists,
    ContentVerificationFailure
}

public enum QuickQaWorkbookRole
{
    Surface,
    HotelHistory
}

/// <summary>
/// Represents a focused workbook compatibility or access failure.
/// </summary>
public sealed class QuickQaWorkbookException : Exception
{
    public QuickQaWorkbookException(
        QuickQaWorkbookErrorCategory errorCategory,
        QuickQaWorkbookRole role,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCategory = errorCategory;
        Role = role;
    }

    public QuickQaWorkbookErrorCategory ErrorCategory { get; }

    public QuickQaWorkbookRole Role { get; }
}
