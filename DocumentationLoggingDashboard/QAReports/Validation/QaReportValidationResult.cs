using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Validation;

/// <summary>
/// Describes completion readiness without replacing any report-domain model.
/// </summary>
public sealed class QaReportValidationResult
{
    public QaReportValidationResult(
        IReadOnlyList<string> blockingErrors,
        IReadOnlyList<string> workflowWarnings,
        QaReportStatus? calculatedStatus,
        string effectiveCreatedBy,
        int warningFindingCount,
        int failureFindingCount,
        int handledFindingCount)
        : this(
            blockingErrors,
            workflowWarnings,
            calculatedStatus,
            effectiveCreatedBy,
            warningFindingCount,
            failureFindingCount,
            handledFindingCount,
            validatedReportFingerprint: null)
    {
    }

    public QaReportValidationResult(
        IReadOnlyList<string> blockingErrors,
        IReadOnlyList<string> workflowWarnings,
        QaReportStatus? calculatedStatus,
        string effectiveCreatedBy,
        int warningFindingCount,
        int failureFindingCount,
        int handledFindingCount,
        string? validatedReportFingerprint)
    {
        ArgumentNullException.ThrowIfNull(blockingErrors);
        ArgumentNullException.ThrowIfNull(workflowWarnings);

        if (string.IsNullOrWhiteSpace(effectiveCreatedBy))
        {
            throw new ArgumentException(
                "An effective Created By value is required.",
                nameof(effectiveCreatedBy));
        }

        if (warningFindingCount < 0
            || failureFindingCount < 0
            || handledFindingCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(warningFindingCount),
                "Finding summary counts cannot be negative.");
        }

        BlockingErrors = Array.AsReadOnly(blockingErrors.ToArray());
        WorkflowWarnings = Array.AsReadOnly(workflowWarnings.ToArray());
        IsReady = BlockingErrors.Count == 0;

        if (IsReady && calculatedStatus is null)
        {
            throw new ArgumentException(
                "A ready QA report requires a calculated status.",
                nameof(calculatedStatus));
        }

        CalculatedStatus = IsReady ? calculatedStatus : null;
        EffectiveCreatedBy = effectiveCreatedBy;
        WarningFindingCount = warningFindingCount;
        FailureFindingCount = failureFindingCount;
        HandledFindingCount = handledFindingCount;
        ValidatedReportFingerprint = string.IsNullOrWhiteSpace(
                validatedReportFingerprint)
            ? null
            : validatedReportFingerprint;
    }

    public bool IsReady { get; }

    public IReadOnlyList<string> BlockingErrors { get; }

    public IReadOnlyList<string> WorkflowWarnings { get; }

    public QaReportStatus? CalculatedStatus { get; }

    public string EffectiveCreatedBy { get; }

    public int WarningFindingCount { get; }

    public int FailureFindingCount { get; }

    public int HandledFindingCount { get; }

    /// <summary>
    /// Gets the deterministic fingerprint of the ready report state validated by
    /// <c>QaReportValidationService</c>. Older callers that use the original
    /// constructor intentionally receive no readiness evidence.
    /// </summary>
    public string? ValidatedReportFingerprint { get; }
}
