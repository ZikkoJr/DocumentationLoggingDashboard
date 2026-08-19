using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Reconciles deterministic Quick QA findings with the fixed checklist and
/// preserves resolution state while a finding ID remains continuously present.
/// </summary>
public sealed class QuickQaFindingSynchronizationService
{
    public const string StrategyWarningFindingId =
        "QUICK:WARN:STRATEGY:SOURCE_RATE_MARKET";

    public const string StrategyFailureFindingId =
        "QUICK:FAIL:STRATEGY:SOURCE_RATE_MARKET";

    private readonly IReadOnlyList<QuickQaCheckDefinition> definitions;
    private readonly IReadOnlyDictionary<string, QuickQaCheckDefinition>
        definitionsById;
    private readonly QaReportStatusService statusService;

    public QuickQaFindingSynchronizationService()
        : this(
            QuickQaChecklistCatalog.Definitions,
            new QaReportStatusService())
    {
    }

    public QuickQaFindingSynchronizationService(
        IReadOnlyList<QuickQaCheckDefinition> definitions,
        QaReportStatusService statusService)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        this.statusService = statusService
            ?? throw new ArgumentNullException(nameof(statusService));

        this.definitions = definitions.ToArray();
        definitionsById = ValidateAndIndexDefinitions(this.definitions);
    }

    public static string WarningForCheck(string checkId)
    {
        return CreateFindingId("QUICK:WARN:CHECK:", checkId);
    }

    public static string FailureForCheck(string checkId)
    {
        return CreateFindingId("QUICK:FAIL:CHECK:", checkId);
    }

    /// <summary>
    /// Synchronizes generated findings, updates the report's final status, and
    /// returns that calculated status.
    /// </summary>
    public QaReportStatus Synchronize(QuickQaReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        Dictionary<string, QuickQaCheckResult> resultsById =
            ValidateAndIndexResults(report);
        Dictionary<string, QaFinding> existingById =
            ValidateAndIndexFindings(report.Findings);
        List<QaFinding> expected = BuildExpectedFindings(resultsById);
        List<QaFinding> synchronized = new(expected.Count);

        foreach (QaFinding generated in expected)
        {
            QaFinding finding;

            if (existingById.TryGetValue(
                    generated.FindingId,
                    out QaFinding? existing))
            {
                ApplyGeneratedFields(existing, generated);
                finding = existing;
            }
            else
            {
                finding = generated;
            }

            NormalizeResolution(finding, report.CustomScriptAvailable);
            synchronized.Add(finding);
        }

        report.Findings.Clear();
        report.Findings.AddRange(synchronized);

        report.FinalStatus = statusService.CalculateStatus(
                report.Findings,
                hasBlockingErrors: false)
            ?? throw new InvalidOperationException(
                "Quick QA status could not be calculated from synchronized findings.");

        return report.FinalStatus;
    }

    private List<QaFinding> BuildExpectedFindings(
        IReadOnlyDictionary<string, QuickQaCheckResult> resultsById)
    {
        List<QaFinding> expected = [];
        bool strategyFindingAdded = false;

        foreach (QuickQaCheckDefinition definition in definitions)
        {
            if (definition.IsStrategyAvailability)
            {
                if (!strategyFindingAdded)
                {
                    QaFinding? strategyFinding = CreateStrategyFinding(resultsById);

                    if (strategyFinding is not null)
                    {
                        expected.Add(strategyFinding);
                    }

                    strategyFindingAdded = true;
                }

                continue;
            }

            QuickQaCheckResult result = resultsById[definition.Id];

            if (result.Status == QuickQaCheckStatus.Warning)
            {
                expected.Add(CreateOrdinaryFinding(
                    definition,
                    QaFindingSeverity.Warning));
            }
            else if (result.Status == QuickQaCheckStatus.Fail)
            {
                expected.Add(CreateOrdinaryFinding(
                    definition,
                    QaFindingSeverity.Failure));
            }
        }

        return expected;
    }

    private static QaFinding? CreateStrategyFinding(
        IReadOnlyDictionary<string, QuickQaCheckResult> resultsById)
    {
        (string Id, string Name)[] strategyChecks =
        [
            (QuickQaChecklistIds.Raw.SourceColumnAvailable, "Source"),
            (QuickQaChecklistIds.Raw.RateColumnAvailable, "Rate"),
            (QuickQaChecklistIds.Raw.MarketColumnAvailable, "Market")
        ];
        string[] unavailable = strategyChecks
            .Where(check =>
                resultsById[check.Id].Status == QuickQaCheckStatus.Fail)
            .Select(check => check.Name)
            .ToArray();

        if (unavailable.Length == 0)
        {
            return null;
        }

        if (unavailable.Length == strategyChecks.Length)
        {
            const string sentence =
                "Source, Rate, and Market strategy columns are all unavailable.";

            return new QaFinding
            {
                FindingId = StrategyFailureFindingId,
                Severity = QaFindingSeverity.Failure,
                Title = "Strategy columns unavailable",
                Description = sentence,
                Resolution = QaFindingResolution.Active,
                Source = QaFindingSource.Checklist
            };
        }

        string missingNames = unavailable.Length == 1
            ? unavailable[0]
            : string.Join(" and ", unavailable);
        string columnWord = unavailable.Length == 1 ? "column is" : "columns are";
        string warningSentence =
            $"Strategy Warning: {missingNames} {columnWord} not available.";

        return new QaFinding
        {
            FindingId = StrategyWarningFindingId,
            Severity = QaFindingSeverity.Warning,
            Title = warningSentence.TrimEnd('.'),
            Description = warningSentence,
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static QaFinding CreateOrdinaryFinding(
        QuickQaCheckDefinition definition,
        QaFindingSeverity severity)
    {
        bool isWarning = severity == QaFindingSeverity.Warning;
        string description = CreateOrdinaryDescription(definition.Id, isWarning);

        return new QaFinding
        {
            FindingId = isWarning
                ? WarningForCheck(definition.Id)
                : FailureForCheck(definition.Id),
            RelatedCheckId = definition.Id,
            Severity = severity,
            Title = isWarning
                ? $"Warning: {definition.DisplayName}"
                : $"Failed check: {definition.DisplayName}",
            Description = description,
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static string CreateOrdinaryDescription(
        string checkId,
        bool isWarning)
    {
        string failureDescription = checkId switch
        {
            QuickQaChecklistIds.Raw.NamesAvailable =>
                "Names are not available to be pulled correctly.",
            QuickQaChecklistIds.Raw.ConfirmationNumberAvailable =>
                "Confirmation Number is not available.",
            QuickQaChecklistIds.Raw.ReservationDateAvailable =>
                "Reservation/Booking Date is not available.",
            QuickQaChecklistIds.Raw.ArrivalDateAvailable =>
                "Arrival Date is not available.",
            QuickQaChecklistIds.Raw.DepartureDateAvailable =>
                "Departure Date is not available.",
            QuickQaChecklistIds.Raw.MonetaryAvailable =>
                "A usable monetary column is not available.",
            QuickQaChecklistIds.Raw.FileFormatConsistent =>
                "File formatting is not consistent throughout.",
            QuickQaChecklistIds.Raw.DateFormatValid =>
                "Date values do not use valid and interpretable formats.",
            QuickQaChecklistIds.Raw.DateSequenceValid =>
                "Reservation/Booking, Arrival, and Departure dates are not sequenced correctly.",
            QuickQaChecklistIds.Raw.CurrencyConsistent =>
                "Currency values are not consistent.",
            QuickQaChecklistIds.Raw.ConfirmationCandidatesReviewed =>
                "Potential Confirmation Number fields were not reviewed sufficiently to identify the correct source.",
            QuickQaChecklistIds.Database.NamesPulledCorrectly =>
                "Names are not being pulled correctly in the database.",
            QuickQaChecklistIds.Database.MonetaryPulledCorrectly =>
                "Monetary values are not being pulled or calculated correctly in the database.",
            QuickQaChecklistIds.Database.DatesPulledCorrectly =>
                "Dates are not being pulled correctly in the database.",
            QuickQaChecklistIds.Database.ConfirmationNumberPulledCorrectly =>
                "Confirmation Number is not being pulled correctly in the database.",
            QuickQaChecklistIds.Database.RequiredValuesPresent =>
                "Required database values are not present.",
            QuickQaChecklistIds.Database.RejectedRecordsAccountedFor =>
                "Rejected records are not accounted for.",
            QuickQaChecklistIds.Database.EmailPulledCorrectly =>
                "Email is not being pulled correctly in the database.",
            _ => throw new InvalidOperationException(
                $"Quick QA checklist ID '{checkId}' has no finding description.")
        };

        return isWarning
            ? "Warning: " + char.ToLowerInvariant(failureDescription[0])
                + failureDescription[1..]
            : failureDescription;
    }

    private static void ApplyGeneratedFields(
        QaFinding target,
        QaFinding generated)
    {
        target.FindingId = generated.FindingId;
        target.RelatedCheckId = generated.RelatedCheckId;
        target.Severity = generated.Severity;
        target.Title = generated.Title;
        target.Description = generated.Description;
        target.Source = generated.Source;
    }

    private static void NormalizeResolution(
        QaFinding finding,
        bool customScriptAvailable)
    {
        finding.CustomScriptName = null;
        finding.ResolutionNotes = TrimToNull(finding.ResolutionNotes);

        if (!Enum.IsDefined(finding.Resolution)
            || (finding.Resolution == QaFindingResolution.HandledByCustomScript
                && !customScriptAvailable)
            || finding.Resolution == QaFindingResolution.ExplainedAndAccepted)
        {
            finding.Resolution = QaFindingResolution.Active;
            finding.ResolutionNotes = null;
        }
    }

    private static IReadOnlyDictionary<string, QuickQaCheckDefinition>
        ValidateAndIndexDefinitions(
            IReadOnlyList<QuickQaCheckDefinition> source)
    {
        if (source.Count == 0)
        {
            throw new ArgumentException(
                "At least one Quick QA checklist definition is required.",
                nameof(source));
        }

        Dictionary<string, QuickQaCheckDefinition> index =
            new(StringComparer.Ordinal);

        foreach (QuickQaCheckDefinition? definition in source)
        {
            if (definition is null
                || string.IsNullOrWhiteSpace(definition.Id)
                || string.IsNullOrWhiteSpace(definition.DisplayName)
                || !index.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException(
                    "Quick QA definitions must have unique, nonblank IDs and display names.",
                    nameof(source));
            }
        }

        return index;
    }

    private Dictionary<string, QuickQaCheckResult> ValidateAndIndexResults(
        QuickQaReport report)
    {
        Dictionary<string, QuickQaCheckResult> index =
            new(StringComparer.Ordinal);

        foreach (QuickQaCheckResult? result in report.ChecklistResults)
        {
            if (result is null
                || string.IsNullOrWhiteSpace(result.CheckId)
                || !definitionsById.ContainsKey(result.CheckId)
                || !Enum.IsDefined(result.Status)
                || !index.TryAdd(result.CheckId, result))
            {
                throw new InvalidOperationException(
                    "Quick QA results must contain valid, unique catalog IDs and statuses.");
            }
        }

        if (index.Count != definitions.Count
            || definitions.Any(definition => !index.ContainsKey(definition.Id)))
        {
            throw new InvalidOperationException(
                "Quick QA must contain exactly one result for every checklist definition.");
        }

        return index;
    }

    private static Dictionary<string, QaFinding> ValidateAndIndexFindings(
        IEnumerable<QaFinding> findings)
    {
        Dictionary<string, QaFinding> index = new(StringComparer.Ordinal);

        foreach (QaFinding? finding in findings)
        {
            if (finding is null
                || string.IsNullOrWhiteSpace(finding.FindingId)
                || !index.TryAdd(finding.FindingId, finding))
            {
                throw new InvalidOperationException(
                    "Quick QA findings must have unique, nonblank deterministic IDs.");
            }
        }

        return index;
    }

    private static string CreateFindingId(string prefix, string checkId)
    {
        if (string.IsNullOrWhiteSpace(checkId))
        {
            throw new ArgumentException(
                "A nonblank Quick QA checklist ID is required.",
                nameof(checkId));
        }

        return prefix + checkId.Trim();
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
