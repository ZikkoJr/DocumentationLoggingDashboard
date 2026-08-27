namespace DocumentationLoggingDashboard.DocumentationLogs;

public enum DocumentationLogSaveErrorCategory
{
    Validation,
    MetadataMismatch,
    InvalidDestination,
    RunningWorkbookUnavailable,
    HistoryWorkbookUnavailable,
    WorkbookInvalid,
    IndexUnavailable,
    SequenceStateInvalid,
    ConcurrentChange,
    ContentVerificationFailure,
    PermissionDenied,
    TransactionFailure,
    RollbackFailure
}
public enum DocumentationLogSaveStage
{
    Validation,
    MetadataResolution,
    RunningLoad,
    HistoryLoad,
    IndexPreparation,
    SequencePreparation,
    WorkbookStaging,
    IndexStaging,
    SequenceStaging,
    ConcurrencyCheck,
    Backup,
    Commit,
    FinalVerification,
    Rollback,
    Cleanup
}

/// <summary>
/// Operator-safe failure information for the full documentation-log logical
/// transaction. Raw exception details remain available for diagnostics but are
/// not intended for direct display.
/// </summary>
public sealed class DocumentationLogSaveException : Exception
{
    public DocumentationLogSaveException(
        DocumentationLogSaveErrorCategory errorCategory,
        DocumentationLogSaveStage stage,
        string message,
        Exception? innerException = null,
        string? affectedPath = null,
        bool previousStateRestored = false,
        bool manualReviewRequired = false,
        IReadOnlyList<string>? manualReviewLocations = null)
        : base(message, innerException)
    {
        ErrorCategory = errorCategory;
        Stage = stage;
        AffectedPath = string.IsNullOrWhiteSpace(affectedPath)
            ? null
            : Path.GetFullPath(affectedPath);
        PreviousStateRestored = previousStateRestored;
        ManualReviewRequired = manualReviewRequired;
        ManualReviewLocations = Array.AsReadOnly(
            (manualReviewLocations ?? Array.Empty<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    public DocumentationLogSaveErrorCategory ErrorCategory { get; }

    public DocumentationLogSaveStage Stage { get; }

    public string? AffectedPath { get; }

    public bool PreviousStateRestored { get; }

    public bool ManualReviewRequired { get; }

    public IReadOnlyList<string> ManualReviewLocations { get; }
}
