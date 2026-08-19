namespace DocumentationLoggingDashboard.QAReports.Services;

public enum QuickQaSaveErrorCategory
{
    ReportInvalid,
    SummaryStale,
    MetadataMismatch,
    InvalidDestination,
    SurfaceWorkbookInUse,
    HotelHistoryWorkbookInUse,
    SurfaceWorkbookInvalid,
    HotelHistoryWorkbookInvalid,
    ConcurrentChange,
    ContentVerificationFailure,
    PermissionDenied,
    TransactionFailure,
    RollbackFailure
}

public enum QuickQaSaveStage
{
    Validation,
    MetadataResolution,
    SurfaceLoad,
    HistoryLoad,
    SurfaceStaging,
    HistoryStaging,
    ConcurrencyCheck,
    Backup,
    SurfaceCommit,
    HistoryCommit,
    FinalVerification,
    Rollback,
    Cleanup
}

/// <summary>
/// Represents an operator-safe Quick QA save failure without exposing raw
/// workbook internals or stack traces to the UI.
/// </summary>
public sealed class QuickQaSaveException : Exception
{
    public QuickQaSaveException(
        QuickQaSaveErrorCategory errorCategory,
        QuickQaSaveStage stage,
        string message,
        Exception? innerException = null,
        bool previousStateRestored = false,
        bool manualReviewRequired = false,
        IReadOnlyList<string>? manualReviewLocations = null)
        : base(message, innerException)
    {
        ErrorCategory = errorCategory;
        Stage = stage;
        PreviousStateRestored = previousStateRestored;
        ManualReviewRequired = manualReviewRequired;
        ManualReviewLocations = Array.AsReadOnly(
            (manualReviewLocations ?? Array.Empty<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    public QuickQaSaveErrorCategory ErrorCategory { get; }

    public QuickQaSaveStage Stage { get; }

    public bool PreviousStateRestored { get; }

    public bool ManualReviewRequired { get; }

    public IReadOnlyList<string> ManualReviewLocations { get; }
}
