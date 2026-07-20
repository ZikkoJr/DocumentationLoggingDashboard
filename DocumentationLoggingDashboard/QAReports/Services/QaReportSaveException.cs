namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Identifies the operator-actionable category of a QA report save failure.
/// </summary>
public enum QaReportSaveErrorCategory
{
    ReportNotReady,
    StaleReadiness,
    MetadataMismatch,
    FilenameFailure,
    InvalidDestination,
    IndexFailure,
    OverwriteRequired,
    ConcurrentChange,
    ContentVerificationFailure,
    PermissionDenied,
    FileInUse,
    TransactionFailure,
    RollbackFailure
}

/// <summary>
/// Identifies the transaction stage active when a QA report save failed.
/// </summary>
public enum QaReportSaveStage
{
    Preparation,
    HotelStaging,
    PmsStaging,
    IndexStaging,
    ExistingReportBackup,
    IndexBackup,
    HotelCommit,
    PmsCommit,
    IndexCommit,
    Rollback,
    Cleanup
}

/// <summary>
/// Represents a focused failure in QA report preparation or paired saving.
/// </summary>
public sealed class QaReportSaveException : Exception
{
    public QaReportSaveException(
        QaReportSaveErrorCategory errorCategory,
        QaReportSaveStage stage,
        string message)
        : this(
            errorCategory,
            stage,
            message,
            innerException: null,
            previousStateRestored: false,
            manualReviewRequired: false,
            manualReviewLocations: [])
    {
    }

    public QaReportSaveException(
        QaReportSaveErrorCategory errorCategory,
        QaReportSaveStage stage,
        string message,
        Exception? innerException,
        bool previousStateRestored,
        bool manualReviewRequired,
        IReadOnlyList<string> manualReviewLocations)
        : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(manualReviewLocations);

        ErrorCategory = errorCategory;
        Stage = stage;
        PreviousStateRestored = previousStateRestored;
        ManualReviewRequired = manualReviewRequired;
        ManualReviewLocations = Array.AsReadOnly(
            manualReviewLocations
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    public QaReportSaveErrorCategory ErrorCategory { get; }

    public QaReportSaveStage Stage { get; }

    public bool PreviousStateRestored { get; }

    public bool ManualReviewRequired { get; }

    public IReadOnlyList<string> ManualReviewLocations { get; }
}
