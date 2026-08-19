using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Forms;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Performs read-only completion and consistency validation on a synchronized report.
/// </summary>
public sealed class QaReportValidationService
{
    public const string MissingCreatedByWarning =
        "This QA report does not have an assigned QA person.";

    public const string DefaultEffectiveCreatedBy = "InnoVarxi QA Team";

    private readonly IReadOnlyList<QaCheckDefinition> checklistDefinitions;
    private readonly IReadOnlyDictionary<string, QaCheckDefinition>
        checklistDefinitionsById;
    private readonly QaReportStatusService statusService;

    public QaReportValidationService()
        : this(QaChecklistCatalog.Definitions, new QaReportStatusService())
    {
    }

    public QaReportValidationService(
        IReadOnlyList<QaCheckDefinition> checklistDefinitions,
        QaReportStatusService statusService)
    {
        ArgumentNullException.ThrowIfNull(checklistDefinitions);
        ArgumentNullException.ThrowIfNull(statusService);

        this.checklistDefinitions = checklistDefinitions.ToArray();
        checklistDefinitionsById = IndexDefinitions(this.checklistDefinitions);
        this.statusService = statusService;
    }

    public QaReportValidationResult Validate(
        QaReport report,
        IReadOnlyList<QaHotelMetadata> canonicalHotels)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(canonicalHotels);

        List<string> errors = [];
        List<string> workflowWarnings = [];

        string? createdBy = TrimToNull(report.CreatedBy);
        string effectiveCreatedBy;

        if (createdBy is null)
        {
            workflowWarnings.Add(MissingCreatedByWarning);
            effectiveCreatedBy = DefaultEffectiveCreatedBy;
        }
        else
        {
            effectiveCreatedBy = createdBy;
        }

        ValidateReportDetails(report, canonicalHotels, errors);

        QaFileCharacteristics? characteristics = report.FileCharacteristics;
        Dictionary<string, QaCheckResult> resultsById =
            ValidateChecklistResults(report, characteristics, errors);

        StatisticsSnapshot statistics = ValidateStatistics(
            report,
            characteristics,
            resultsById,
            errors);
        Dictionary<string, QaFinding> findingsById = ValidateFindings(
            report,
            characteristics,
            resultsById,
            statistics,
            errors);

        ValidateZeroRowContext(report, resultsById, findingsById, errors);

        ValidateChecklistExplanations(
            resultsById,
            findingsById,
            errors);

        int warningCount = findingsById.Values.Count(
            finding => finding.Severity == QaFindingSeverity.Warning);
        int failureCount = findingsById.Values.Count(
            finding => finding.Severity == QaFindingSeverity.Failure);
        int handledCount = findingsById.Values.Count(
            finding => finding.Resolution != QaFindingResolution.Active);

        QaReportStatus? calculatedStatus = statusService.CalculateStatus(
            findingsById.Values,
            hasBlockingErrors: errors.Count != 0);

        string? validatedReportFingerprint = errors.Count == 0
            ? QaReportReadinessFingerprint.Compute(report)
            : null;

        return new QaReportValidationResult(
            errors,
            workflowWarnings,
            calculatedStatus,
            effectiveCreatedBy,
            warningCount,
            failureCount,
            handledCount,
            validatedReportFingerprint);
    }

    private static void ValidateZeroRowContext(
        QaReport report,
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        IReadOnlyDictionary<string, QaFinding> findingsById,
        ICollection<string> errors)
    {
        if (report.Statistics?.FileInformation?.TotalDataRows != 0)
        {
            return;
        }

        bool hasFailedChecklistResult = resultsById.Values.Any(
            result => result.Status == QaCheckStatus.Fail);
        bool hasCurrentFailureFinding = findingsById.Values.Any(
            finding => finding.Severity == QaFindingSeverity.Failure);

        if (!hasFailedChecklistResult && !hasCurrentFailureFinding)
        {
            errors.Add(
                "Zero Total Data Rows must be coherently documented by a failed checklist item or current Failure finding.");
        }
    }

    private void ValidateReportDetails(
        QaReport report,
        IReadOnlyList<QaHotelMetadata> canonicalHotels,
        ICollection<string> errors)
    {
        if (report.SchemaVersion != QaReport.CurrentSchemaVersion)
        {
            errors.Add(
                $"Detailed QA report schema version {report.SchemaVersion} is unsupported; expected version {QaReport.CurrentSchemaVersion}.");
        }

        if (TrimToNull(report.FileId) is null)
        {
            errors.Add("File ID is required.");
        }

        QaHotelInformation? hotelInformation = report.HotelInformation;

        if (hotelInformation is null)
        {
            errors.Add("Hotel information is required.");
        }
        else
        {
            string? hotelId = TrimToNull(hotelInformation.HotelId);
            string? hotelName = TrimToNull(hotelInformation.HotelName);
            string? pmsName = TrimToNull(hotelInformation.PmsName);

            if (hotelId is null)
            {
                errors.Add("A canonical Hotel ID must be selected.");
            }

            if (hotelName is null)
            {
                errors.Add("A canonical Hotel Name must be selected.");
            }

            if (pmsName is null)
            {
                errors.Add("A canonical PMS must be selected.");
            }

            if (hotelId is not null)
            {
                QaHotelMetadata[] matches = canonicalHotels
                    .Where(hotel => hotel is not null)
                    .Where(hotel => string.Equals(
                        TrimToNull(hotel.HotelId),
                        hotelId,
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (matches.Length != 1)
                {
                    errors.Add(
                        "The selected Hotel ID does not identify exactly one current canonical hotel.");
                }
                else
                {
                    QaHotelMetadata canonical = matches[0];

                    if (!string.Equals(
                            hotelId,
                            TrimToNull(canonical.HotelId),
                            StringComparison.Ordinal)
                        || !string.Equals(
                            hotelName,
                            TrimToNull(canonical.HotelName),
                            StringComparison.Ordinal)
                        || !string.Equals(
                            pmsName,
                            TrimToNull(canonical.PmsName),
                            StringComparison.Ordinal))
                    {
                        errors.Add(
                            "The selected Hotel Name or PMS does not match current canonical metadata.");
                    }
                }
            }

            if (hotelInformation.FileMonth is null)
            {
                errors.Add("File Month is required.");
            }
            else if (hotelInformation.FileMonth.Year is < 1 or > 9999
                || hotelInformation.FileMonth.Month is < 1 or > 12)
            {
                errors.Add("File Month must contain a valid calendar year and month.");
            }
        }

        if (canonicalHotels.Any(hotel => hotel is null))
        {
            errors.Add("The canonical hotel snapshot contains a null record.");
        }

        if (report.QaDate == default)
        {
            errors.Add("QA Date is required.");
        }

        QaFileCharacteristics? characteristics = report.FileCharacteristics;

        if (characteristics is null)
        {
            errors.Add("File characteristics are required.");
        }
        else
        {
            if (!Enum.IsDefined(typeof(QaNameColumnMode), characteristics.NameColumnMode))
            {
                errors.Add("Name-column mode has an unknown value.");
            }

            if (!Enum.IsDefined(
                    typeof(QaMonetaryColumnScenario),
                    characteristics.MonetaryColumnScenario))
            {
                errors.Add("Monetary-column scenario has an unknown value.");
            }
        }

        QaStatistics? statistics = report.Statistics;

        if (statistics is null)
        {
            errors.Add("Statistics are required.");
            return;
        }

        QaFileInformationStatistics? fileInformation = statistics.FileInformation;

        if (fileInformation is null)
        {
            errors.Add("File Information statistics are required.");
            return;
        }

        if (fileInformation.TotalDataRows < 0)
        {
            errors.Add("Total Data Rows cannot be negative.");
        }

        if (fileInformation.TotalDataRows > 0
            && fileInformation.DataStartRow < 1)
        {
            errors.Add("Data Start Row must be at least 1 when data rows exist.");
        }

        if (fileInformation.DataStartRow < 0)
        {
            errors.Add("Data Start Row cannot be negative.");
        }

        if (!Enum.IsDefined(
                typeof(QaUsefulHeadersResult),
                fileInformation.UsefulHeaders))
        {
            errors.Add("Useful Headers has an unknown value.");
        }
    }

    private Dictionary<string, QaCheckResult> ValidateChecklistResults(
        QaReport report,
        QaFileCharacteristics? characteristics,
        ICollection<string> errors)
    {
        Dictionary<string, QaCheckResult> resultsById =
            new(StringComparer.Ordinal);

        foreach (QaCheckResult? result in report.ChecklistResults)
        {
            if (result is null)
            {
                errors.Add("The checklist result collection contains a null entry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(result.CheckId))
            {
                errors.Add("Every checklist result requires a stable checklist ID.");
                continue;
            }

            if (!checklistDefinitionsById.ContainsKey(result.CheckId))
            {
                errors.Add($"Checklist result '{result.CheckId}' is not in the approved catalog.");
                continue;
            }

            if (!resultsById.TryAdd(result.CheckId, result))
            {
                errors.Add($"Checklist result '{result.CheckId}' appears more than once.");
                continue;
            }

            if (!Enum.IsDefined(typeof(QaCheckStatus), result.Status))
            {
                errors.Add($"Checklist result '{result.CheckId}' has an unknown status.");
            }

            if (!Enum.IsDefined(typeof(QaResultSource), result.ResultSource))
            {
                errors.Add($"Checklist result '{result.CheckId}' has an unknown result source.");
            }
        }

        foreach (QaCheckDefinition definition in checklistDefinitions)
        {
            if (!resultsById.TryGetValue(
                    definition.Id,
                    out QaCheckResult? result))
            {
                errors.Add($"Checklist result '{definition.Id}' is missing.");
                continue;
            }

            if (characteristics is null
                || !Enum.IsDefined(
                    typeof(QaNameColumnMode),
                    characteristics.NameColumnMode)
                || !Enum.IsDefined(
                    typeof(QaMonetaryColumnScenario),
                    characteristics.MonetaryColumnScenario))
            {
                continue;
            }

            bool applicable;

            try
            {
                applicable = QaChecklistApplicabilityEvaluator.IsApplicable(
                    definition,
                    characteristics);
            }
            catch (ArgumentOutOfRangeException)
            {
                errors.Add(
                    $"Checklist definition '{definition.Id}' has an unknown applicability value.");
                continue;
            }

            if (applicable
                && result.Status is not QaCheckStatus.Pass
                    and not QaCheckStatus.Fail)
            {
                errors.Add(
                    result.Status == QaCheckStatus.NotEvaluated
                        ? $"Applicable checklist item '{definition.DisplayName}' has not been evaluated."
                        : $"Applicable checklist item '{definition.DisplayName}' cannot be Not Applicable.");
            }
            else if (!applicable
                && result.Status != QaCheckStatus.NotApplicable)
            {
                errors.Add(
                    $"Non-applicable checklist item '{definition.DisplayName}' must be Not Applicable.");
            }
        }

        if (resultsById.Count != checklistDefinitions.Count)
        {
            errors.Add(
                "The report must contain exactly one checklist result for every approved definition.");
        }

        return resultsById;
    }

    private StatisticsSnapshot ValidateStatistics(
        QaReport report,
        QaFileCharacteristics? characteristics,
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        ICollection<string> errors)
    {
        StatisticsSnapshot snapshot = new();
        QaStatistics? statistics = report.Statistics;

        if (statistics is null || characteristics is null)
        {
            return snapshot;
        }

        int totalDataRows = statistics.FileInformation?.TotalDataRows ?? 0;
        snapshot.BlankById = IndexBlankStatistics(
            statistics.BlankValues,
            errors);
        snapshot.BrokenById = IndexBrokenStatistics(
            statistics.BrokenData,
            errors);

        foreach (QaStatisticFieldDefinition definition
                 in QaStatisticFieldCatalog.Definitions)
        {
            bool applicable = IsStatisticApplicable(
                definition,
                characteristics,
                errors);

            ValidateBlankStatistic(
                definition,
                applicable,
                totalDataRows,
                snapshot.BlankById,
                resultsById,
                errors);
            ValidateBrokenStatistic(
                definition,
                applicable,
                snapshot.BlankById,
                snapshot.BrokenById,
                resultsById,
                errors);
        }

        ValidateMultiwordNames(
            statistics,
            characteristics,
            snapshot.BlankById,
            errors);
        ValidateFileMonthStatistics(
            statistics,
            snapshot.BlankById,
            errors);
        ValidateMonetaryStatistics(
            statistics,
            characteristics,
            snapshot.BlankById,
            errors);
        ValidateDatabaseStatistics(
            statistics,
            characteristics,
            resultsById,
            errors);

        snapshot.Statistics = statistics;
        return snapshot;
    }

    private static Dictionary<string, QaBlankValueStatistic>
        IndexBlankStatistics(
            IEnumerable<QaBlankValueStatistic> statistics,
            ICollection<string> errors)
    {
        Dictionary<string, QaBlankValueStatistic> byId =
            new(StringComparer.Ordinal);

        foreach (QaBlankValueStatistic? statistic in statistics)
        {
            if (statistic is null)
            {
                errors.Add("Blank-value statistics contain a null entry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(statistic.FieldId))
            {
                errors.Add("Every blank-value statistic requires a stable field ID.");
                continue;
            }

            try
            {
                _ = QaStatisticFieldCatalog.GetRequired(statistic.FieldId);
            }
            catch (ArgumentException)
            {
                errors.Add(
                    $"Blank-value statistic '{statistic.FieldId}' is not in the approved field catalog.");
                continue;
            }

            if (!byId.TryAdd(statistic.FieldId, statistic))
            {
                errors.Add(
                    $"Blank-value statistic '{statistic.FieldId}' appears more than once.");
            }
        }

        return byId;
    }

    private static Dictionary<string, QaBrokenDataStatistic>
        IndexBrokenStatistics(
            IEnumerable<QaBrokenDataStatistic> statistics,
            ICollection<string> errors)
    {
        Dictionary<string, QaBrokenDataStatistic> byId =
            new(StringComparer.Ordinal);

        foreach (QaBrokenDataStatistic? statistic in statistics)
        {
            if (statistic is null)
            {
                errors.Add("Broken-data statistics contain a null entry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(statistic.FieldId))
            {
                errors.Add("Every broken-data statistic requires a stable field ID.");
                continue;
            }

            try
            {
                _ = QaStatisticFieldCatalog.GetRequired(statistic.FieldId);
            }
            catch (ArgumentException)
            {
                errors.Add(
                    $"Broken-data statistic '{statistic.FieldId}' is not in the approved field catalog.");
                continue;
            }

            if (!byId.TryAdd(statistic.FieldId, statistic))
            {
                errors.Add(
                    $"Broken-data statistic '{statistic.FieldId}' appears more than once.");
            }
        }

        return byId;
    }

    private static void ValidateBlankStatistic(
        QaStatisticFieldDefinition definition,
        bool applicable,
        int totalDataRows,
        IReadOnlyDictionary<string, QaBlankValueStatistic> blankById,
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        ICollection<string> errors)
    {
        bool exists = blankById.TryGetValue(
            definition.Id,
            out QaBlankValueStatistic? statistic);

        if (!definition.SupportsBlankStatistics)
        {
            if (exists)
            {
                errors.Add(
                    $"Field '{definition.DisplayName}' does not support blank-value statistics.");
            }

            return;
        }

        if (applicable && !exists)
        {
            errors.Add(
                $"Blank-value statistics for '{definition.DisplayName}' are required.");
            return;
        }

        if (!applicable)
        {
            if (exists)
            {
                errors.Add(
                    $"Non-applicable blank-value statistics for '{definition.DisplayName}' must be removed.");
            }

            return;
        }

        if (!string.Equals(
                statistic!.DisplayName,
                definition.DisplayName,
                StringComparison.Ordinal))
        {
            errors.Add(
                $"Blank-value statistics for '{definition.Id}' have a stale display name.");
        }

        if (statistic.BlankCount < 0)
        {
            errors.Add($"Blank count for '{definition.DisplayName}' cannot be negative.");
        }

        if (statistic.TotalApplicableRows < 0)
        {
            errors.Add(
                $"Applicable-row count for '{definition.DisplayName}' cannot be negative.");
        }

        if (statistic.UseAutomaticTotalApplicableRows
            && statistic.TotalApplicableRows != totalDataRows)
        {
            errors.Add(
                $"Automatic applicable-row count for '{definition.DisplayName}' must equal Total Data Rows.");
        }

        bool missingFieldIsDocumented = IsMissingFieldDocumented(
            definition.Id,
            resultsById);

        if (totalDataRows > 0
            && statistic.TotalApplicableRows == 0
            && !missingFieldIsDocumented)
        {
            errors.Add(
                $"Applicable-row count for '{definition.DisplayName}' is required when data rows exist.");
        }

        if (statistic.BlankCount > statistic.TotalApplicableRows)
        {
            errors.Add(
                $"Blank count for '{definition.DisplayName}' cannot exceed its applicable-row count.");
        }

        if (totalDataRows >= 0
            && statistic.TotalApplicableRows > totalDataRows)
        {
            errors.Add(
                $"Applicable-row count for '{definition.DisplayName}' cannot exceed Total Data Rows.");
        }

        decimal expected = QaStatisticsCalculationService.CalculatePercentage(
            statistic.BlankCount,
            statistic.TotalApplicableRows);

        if (statistic.BlankPercentage != expected)
        {
            errors.Add(
                $"Blank percentage for '{definition.DisplayName}' is not synchronized with its counts.");
        }
    }

    private static void ValidateBrokenStatistic(
        QaStatisticFieldDefinition definition,
        bool applicable,
        IReadOnlyDictionary<string, QaBlankValueStatistic> blankById,
        IReadOnlyDictionary<string, QaBrokenDataStatistic> brokenById,
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        ICollection<string> errors)
    {
        bool exists = brokenById.TryGetValue(
            definition.Id,
            out QaBrokenDataStatistic? statistic);

        if (!definition.SupportsBrokenDataStatistics)
        {
            if (exists)
            {
                errors.Add(
                    $"Field '{definition.DisplayName}' does not support broken-data statistics.");
            }

            return;
        }

        if (applicable && !exists)
        {
            errors.Add(
                $"Broken-data statistics for '{definition.DisplayName}' are required.");
            return;
        }

        if (!applicable)
        {
            if (exists)
            {
                errors.Add(
                    $"Non-applicable broken-data statistics for '{definition.DisplayName}' must be removed.");
            }

            return;
        }

        if (!string.Equals(
                statistic!.DisplayName,
                definition.DisplayName,
                StringComparison.Ordinal))
        {
            errors.Add(
                $"Broken-data statistics for '{definition.Id}' have a stale display name.");
        }

        if (statistic.BrokenValueCount < 0)
        {
            errors.Add(
                $"Broken-value count for '{definition.DisplayName}' cannot be negative.");
        }

        if (statistic.TotalApplicableNonblankValues < 0)
        {
            errors.Add(
                $"Nonblank denominator for '{definition.DisplayName}' cannot be negative.");
        }

        if (statistic.BrokenValueCount
            > statistic.TotalApplicableNonblankValues)
        {
            errors.Add(
                $"Broken-value count for '{definition.DisplayName}' cannot exceed its nonblank denominator.");
        }

        if (!blankById.TryGetValue(
                definition.Id,
                out QaBlankValueStatistic? blankStatistic))
        {
            errors.Add(
                $"Broken-data statistics for '{definition.DisplayName}' require matching blank-value statistics.");
        }
        else
        {
            long derived = (long)blankStatistic.TotalApplicableRows
                - blankStatistic.BlankCount;
            bool missingFieldIsDocumented = IsMissingFieldDocumented(
                definition.Id,
                resultsById);

            if (derived < 0)
            {
                errors.Add(
                    $"Derived nonblank denominator for '{definition.DisplayName}' cannot be negative.");
            }
            else if (derived > 0
                && statistic.TotalApplicableNonblankValues == 0
                && !missingFieldIsDocumented)
            {
                errors.Add(
                    $"Nonblank denominator for '{definition.DisplayName}' is required when its blank-derived nonblank population is positive.");
            }
            else if (statistic.UseAutomaticTotalApplicableNonblankValues
                && statistic.TotalApplicableNonblankValues != derived)
            {
                errors.Add(
                    $"Automatic broken-data denominator for '{definition.DisplayName}' must equal its blank-derived nonblank count.");
            }
            else if (!statistic.UseAutomaticTotalApplicableNonblankValues
                && statistic.TotalApplicableNonblankValues > derived)
            {
                errors.Add(
                    $"Manual broken-data denominator for '{definition.DisplayName}' cannot exceed its blank-derived nonblank population.");
            }
        }

        decimal expected = QaStatisticsCalculationService.CalculatePercentage(
            statistic.BrokenValueCount,
            statistic.TotalApplicableNonblankValues);

        if (statistic.BrokenDataPercentage != expected)
        {
            errors.Add(
                $"Broken-data percentage for '{definition.DisplayName}' is not synchronized with its counts.");
        }

        if (QaStatisticsCalculationService.ClassifyThreshold(
                statistic.BrokenValueCount,
                statistic.TotalApplicableNonblankValues)
                == QaFindingSeverity.Failure
            && definition.RelatedChecklistId is not null
            && (!resultsById.TryGetValue(
                    definition.RelatedChecklistId,
                    out QaCheckResult? relatedResult)
                || relatedResult.Status != QaCheckStatus.Fail))
        {
            errors.Add(
                $"Broken data above 50% for '{definition.DisplayName}' requires checklist item '{definition.RelatedChecklistId}' to be Fail.");
        }
    }

    private static void ValidateMultiwordNames(
        QaStatistics statistics,
        QaFileCharacteristics characteristics,
        IReadOnlyDictionary<string, QaBlankValueStatistic> blankById,
        ICollection<string> errors)
    {
        QaMultiwordNameStatistics? multiword = statistics.MultiwordNames;

        if (multiword is null)
        {
            errors.Add("Multiword-name statistics are required.");
            return;
        }

        if (characteristics.NameColumnMode == QaNameColumnMode.FullName)
        {
            if (multiword.MultiwordFirstNameCount != 0
                || multiword.MultiwordFirstNamePercentage != 0m
                || multiword.MultiwordLastNameCount != 0
                || multiword.MultiwordLastNamePercentage != 0m)
            {
                errors.Add(
                    "Separate-name multiword statistics must be cleared in Full Name mode.");
            }

            return;
        }

        ValidateDerivedStatistic(
            "Multiword First Name",
            multiword.MultiwordFirstNameCount,
            multiword.MultiwordFirstNamePercentage,
            QaStatisticFieldIds.FirstName,
            blankById,
            errors);
        ValidateDerivedStatistic(
            "Multiword Last Name",
            multiword.MultiwordLastNameCount,
            multiword.MultiwordLastNamePercentage,
            QaStatisticFieldIds.LastName,
            blankById,
            errors);
    }

    private static void ValidateFileMonthStatistics(
        QaStatistics statistics,
        IReadOnlyDictionary<string, QaBlankValueStatistic> blankById,
        ICollection<string> errors)
    {
        QaFileMonthStatistics? fileMonth = statistics.FileMonth;

        if (fileMonth is null)
        {
            errors.Add("File Month statistics are required.");
            return;
        }

        if (fileMonth.ValidArrivalDateCount < 0
            || fileMonth.ArrivalDatesWithinFileMonth < 0
            || fileMonth.ArrivalDatesOutsideFileMonth < 0)
        {
            errors.Add("File Month statistic counts cannot be negative.");
        }

        if (fileMonth.ValidArrivalDateCount >= 0
            && fileMonth.ArrivalDatesWithinFileMonth >
                fileMonth.ValidArrivalDateCount)
        {
            errors.Add(
                "Arrival Dates within File Month cannot exceed Valid Arrival Date Count.");
        }

        if (fileMonth.ValidArrivalDateCount >= 0
            && fileMonth.ArrivalDatesOutsideFileMonth >
                fileMonth.ValidArrivalDateCount)
        {
            errors.Add(
                "Arrival Dates outside File Month cannot exceed Valid Arrival Date Count.");
        }

        long categorized = (long)fileMonth.ArrivalDatesWithinFileMonth
            + fileMonth.ArrivalDatesOutsideFileMonth;

        if (categorized != fileMonth.ValidArrivalDateCount)
        {
            errors.Add(
                "Arrival Dates within and outside File Month must sum to Valid Arrival Date Count.");
        }

        decimal expectedWithin = QaStatisticsCalculationService.CalculatePercentage(
            fileMonth.ArrivalDatesWithinFileMonth,
            fileMonth.ValidArrivalDateCount);
        decimal expectedOutside = QaStatisticsCalculationService.CalculatePercentage(
            fileMonth.ArrivalDatesOutsideFileMonth,
            fileMonth.ValidArrivalDateCount);

        if (fileMonth.PercentageWithinFileMonth != expectedWithin
            || fileMonth.PercentageOutsideFileMonth != expectedOutside)
        {
            errors.Add("File Month percentages are not synchronized with their counts.");
        }

        if (blankById.TryGetValue(
                QaStatisticFieldIds.ArrivalDate,
                out QaBlankValueStatistic? arrivalBlank))
        {
            long nonblank = (long)arrivalBlank.TotalApplicableRows
                - arrivalBlank.BlankCount;

            if (fileMonth.ValidArrivalDateCount > nonblank)
            {
                errors.Add(
                    "Valid Arrival Date Count cannot exceed the derived nonblank Arrival Date count.");
            }
        }
    }

    private static void ValidateMonetaryStatistics(
        QaStatistics statistics,
        QaFileCharacteristics characteristics,
        IReadOnlyDictionary<string, QaBlankValueStatistic> blankById,
        ICollection<string> errors)
    {
        ValidateUnusualMonetary(
            "Average Rate",
            statistics.UnusualAverageRateValues,
            QaStatisticFieldIds.AverageRate,
            blankById,
            errors);

        bool stayValueApplies = IsStatisticApplicable(
            QaStatisticFieldCatalog.GetRequired(QaStatisticFieldIds.StayValue),
            characteristics,
            errors);

        if (!stayValueApplies)
        {
            QaUnusualMonetaryValueStatistics? unusualStay =
                statistics.UnusualStayValues;
            QaHighStayValueStatistics? highStay = statistics.HighStayValues;

            if (unusualStay is null
                || highStay is null)
            {
                errors.Add("Stay Value statistics objects are required.");
                return;
            }

            if (unusualStay.HasUnusualValues
                || unusualStay.UnusualValueCount != 0
                || unusualStay.UnusualValuePercentage != 0m
                || TrimToNull(unusualStay.Explanation) is not null
                || highStay.StayValuesAboveTenThousandCount != 0
                || highStay.StayValuesAboveTenThousandPercentage != 0m
                || highStay.AreHighValuesExpected
                || TrimToNull(highStay.Explanation) is not null)
            {
                errors.Add(
                    "Stay Value statistics must be cleared when Stay Value is not applicable.");
            }

            return;
        }

        ValidateUnusualMonetary(
            "Stay Value",
            statistics.UnusualStayValues,
            QaStatisticFieldIds.StayValue,
            blankById,
            errors);

        QaHighStayValueStatistics? highValues = statistics.HighStayValues;

        if (highValues is null)
        {
            errors.Add("High Stay Value statistics are required.");
            return;
        }

        ValidateDerivedStatistic(
            "Stay Values strictly above 10,000",
            highValues.StayValuesAboveTenThousandCount,
            highValues.StayValuesAboveTenThousandPercentage,
            QaStatisticFieldIds.StayValue,
            blankById,
            errors);

        if (highValues.AreHighValuesExpected
            && highValues.StayValuesAboveTenThousandCount == 0)
        {
            errors.Add(
                "High Stay Values cannot be marked expected when their count is zero.");
        }

        if (highValues.StayValuesAboveTenThousandCount > 0
            && !highValues.AreHighValuesExpected
            && TrimToNull(highValues.Explanation) is null)
        {
            errors.Add(
                "Unexpected Stay Values strictly above 10,000 require an explanation.");
        }
    }

    private static void ValidateUnusualMonetary(
        string displayName,
        QaUnusualMonetaryValueStatistics? statistic,
        string fieldId,
        IReadOnlyDictionary<string, QaBlankValueStatistic> blankById,
        ICollection<string> errors)
    {
        if (statistic is null)
        {
            errors.Add($"Unusual {displayName} statistics are required.");
            return;
        }

        if (!statistic.HasUnusualValues)
        {
            if (statistic.UnusualValueCount != 0
                || statistic.UnusualValuePercentage != 0m
                || TrimToNull(statistic.Explanation) is not null)
            {
                errors.Add(
                    $"Unusual {displayName} values marked No must have cleared count, percentage, and explanation.");
            }

            return;
        }

        if (statistic.UnusualValueCount <= 0)
        {
            errors.Add(
                $"Unusual {displayName} values marked Yes require a positive count.");
        }

        if (TrimToNull(statistic.Explanation) is null)
        {
            errors.Add($"Unusual {displayName} values require an explanation.");
        }

        ValidateDerivedStatistic(
            $"Unusual {displayName}",
            statistic.UnusualValueCount,
            statistic.UnusualValuePercentage,
            fieldId,
            blankById,
            errors);
    }

    private static void ValidateDerivedStatistic(
        string displayName,
        int numerator,
        decimal percentage,
        string blankFieldId,
        IReadOnlyDictionary<string, QaBlankValueStatistic> blankById,
        ICollection<string> errors)
    {
        if (numerator < 0)
        {
            errors.Add($"{displayName} count cannot be negative.");
        }

        if (!blankById.TryGetValue(
                blankFieldId,
                out QaBlankValueStatistic? blankStatistic))
        {
            errors.Add(
                $"{displayName} requires matching blank-value statistics for its denominator.");
            return;
        }

        long denominator = (long)blankStatistic.TotalApplicableRows
            - blankStatistic.BlankCount;

        if (denominator < 0)
        {
            errors.Add($"{displayName} has a negative derived nonblank denominator.");
            return;
        }

        if (denominator > int.MaxValue)
        {
            errors.Add($"{displayName} derived denominator is outside the supported range.");
            return;
        }

        if (numerator > denominator)
        {
            errors.Add($"{displayName} count cannot exceed its derived denominator.");
        }

        decimal expected = QaStatisticsCalculationService.CalculatePercentage(
            numerator,
            (int)denominator);

        if (percentage != expected)
        {
            errors.Add($"{displayName} percentage is not synchronized with its counts.");
        }
    }

    private static void ValidateDatabaseStatistics(
        QaStatistics statistics,
        QaFileCharacteristics characteristics,
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        ICollection<string> errors)
    {
        QaDatabaseStatistics? database = statistics.Database;
        QaFileInformationStatistics? fileInformation = statistics.FileInformation;

        if (database is null || fileInformation is null)
        {
            errors.Add("Database and File Information statistics are required.");
            return;
        }

        if (database.ImportedRecordCount < 0
            || database.RejectedRecordCount < 0
            || database.RecordsWithMissingRequiredDatabaseValues < 0)
        {
            errors.Add("Database statistic counts cannot be negative.");
        }

        if (fileInformation.TotalDataRows >= 0
            && database.RejectedRecordCount > fileInformation.TotalDataRows)
        {
            errors.Add("Rejected Record Count cannot exceed Total Data Rows.");
        }

        if (database.ImportedRecordCount >= 0
            && database.RecordsWithMissingRequiredDatabaseValues
                > database.ImportedRecordCount)
        {
            errors.Add(
                "Records With Missing Required DB Values cannot exceed Imported Record Count.");
        }

        try
        {
            int expectedDifference =
                QaStatisticsCalculationService.CalculateRawMinusImportedDifference(
                    fileInformation.TotalDataRows,
                    database.ImportedRecordCount);

            if (database.RawMinusImportedRecordCountDifference
                != expectedDifference)
            {
                errors.Add(
                    "Raw Minus Imported Record Count Difference is not synchronized with its counts.");
            }
        }
        catch (OverflowException)
        {
            errors.Add("Raw Minus Imported Record Count Difference is outside the supported range.");
        }

        bool shouldHaveRejectedRecords = database.RejectedRecordCount > 0;

        if (characteristics.HasRejectedDatabaseRecords
            != shouldHaveRejectedRecords)
        {
            errors.Add(
                "Rejected Record Count and the rejected-record file characteristic disagree.");
        }

        if (resultsById.TryGetValue(
                QaChecklistIds.Database.RejectedRecordsAccountedFor,
                out QaCheckResult? rejectedResult))
        {
            if (shouldHaveRejectedRecords)
            {
                if (rejectedResult.Status is not QaCheckStatus.Pass
                    and not QaCheckStatus.Fail)
                {
                    errors.Add(
                        "Rejected records require their checklist item to be Pass or Fail.");
                }

                if (rejectedResult.Status == QaCheckStatus.Pass
                    && TrimToNull(rejectedResult.Notes) is null)
                {
                    errors.Add(
                        "Accounted-for rejected records require a non-sensitive checklist explanation.");
                }
            }
            else if (rejectedResult.Status != QaCheckStatus.NotApplicable)
            {
                errors.Add(
                    "The rejected-record checklist item must be Not Applicable when no records were rejected.");
            }
        }

        if (resultsById.TryGetValue(
                QaChecklistIds.Database.RequiredValuesPresent,
                out QaCheckResult? requiredValuesResult))
        {
            if (database.RecordsWithMissingRequiredDatabaseValues > 0
                && requiredValuesResult.Status != QaCheckStatus.Fail)
            {
                errors.Add(
                    "Missing required database values require the Required DB Values checklist item to be Fail.");
            }
            else if (database.RecordsWithMissingRequiredDatabaseValues == 0
                && requiredValuesResult.Status == QaCheckStatus.Fail
                && TrimToNull(requiredValuesResult.Notes) is null)
            {
                errors.Add(
                    "A Required DB Values failure with zero missing values requires meaningful notes.");
            }
        }
    }

    private Dictionary<string, QaFinding> ValidateFindings(
        QaReport report,
        QaFileCharacteristics? characteristics,
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        StatisticsSnapshot statistics,
        ICollection<string> errors)
    {
        Dictionary<string, QaFinding> findingsById =
            new(StringComparer.Ordinal);

        foreach (QaFinding? finding in report.Findings)
        {
            if (finding is null)
            {
                errors.Add("The findings collection contains a null entry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(finding.FindingId))
            {
                errors.Add("Every finding requires a deterministic ID.");
                continue;
            }

            if (!findingsById.TryAdd(finding.FindingId, finding))
            {
                errors.Add($"Finding '{finding.FindingId}' appears more than once.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(finding.Title))
            {
                errors.Add($"Finding '{finding.FindingId}' requires a title.");
            }

            if (string.IsNullOrWhiteSpace(finding.Description))
            {
                errors.Add($"Finding '{finding.FindingId}' requires a description.");
            }

            if (!Enum.IsDefined(typeof(QaFindingSeverity), finding.Severity))
            {
                errors.Add($"Finding '{finding.FindingId}' has an unknown severity.");
            }

            if (!Enum.IsDefined(typeof(QaFindingResolution), finding.Resolution))
            {
                errors.Add($"Finding '{finding.FindingId}' has an unknown resolution.");
            }

            if (!Enum.IsDefined(typeof(QaFindingSource), finding.Source))
            {
                errors.Add($"Finding '{finding.FindingId}' has an unknown source.");
            }

            string? relatedCheckId = TrimToNull(finding.RelatedCheckId);

            if (relatedCheckId is not null
                && !checklistDefinitionsById.ContainsKey(relatedCheckId))
            {
                errors.Add(
                    $"Finding '{finding.FindingId}' references unknown checklist item '{relatedCheckId}'.");
            }

            if (finding.Resolution ==
                    QaFindingResolution.HandledByCustomScript
                && characteristics?.IsCustomScriptSupportAvailable != true)
            {
                errors.Add(
                    $"Finding '{finding.FindingId}' cannot use custom-script resolution when support is unavailable.");
            }

            if (finding.Resolution !=
                    QaFindingResolution.HandledByCustomScript
                && TrimToNull(finding.CustomScriptName) is not null)
            {
                errors.Add(
                    $"Finding '{finding.FindingId}' retains a custom script name outside custom-script resolution.");
            }

            if (finding.Severity == QaFindingSeverity.Failure
                && finding.Resolution ==
                    QaFindingResolution.ExplainedAndAccepted)
            {
                errors.Add(
                    $"Failure finding '{finding.FindingId}' cannot be Explained and Accepted.");
            }

            if (finding.Severity == QaFindingSeverity.Warning
                && finding.Resolution ==
                    QaFindingResolution.ExplainedAndAccepted
                && TrimToNull(finding.ResolutionNotes) is null)
            {
                errors.Add(
                    $"Explained and Accepted warning '{finding.FindingId}' requires resolution notes.");
            }

            if (finding.FindingId.StartsWith("FAIL:", StringComparison.Ordinal)
                && finding.Severity != QaFindingSeverity.Failure)
            {
                errors.Add(
                    $"Failure finding '{finding.FindingId}' must retain Failure severity.");
            }

            if (finding.FindingId.StartsWith("WARN:", StringComparison.Ordinal)
                && finding.Severity != QaFindingSeverity.Warning)
            {
                errors.Add(
                    $"Warning finding '{finding.FindingId}' must retain Warning severity.");
            }
        }

        Dictionary<string, ExpectedFinding> expected = BuildExpectedFindings(
            characteristics,
            resultsById,
            statistics,
            findingsById,
            errors);

        foreach ((string id, ExpectedFinding expectedFinding) in expected)
        {
            if (!findingsById.TryGetValue(id, out QaFinding? finding))
            {
                errors.Add($"Synchronized finding '{id}' is missing.");
                continue;
            }

            if (finding.Severity != expectedFinding.Severity)
            {
                errors.Add($"Finding '{id}' has an unexpected severity.");
            }

            if (finding.Source != expectedFinding.Source)
            {
                errors.Add($"Finding '{id}' has an unexpected source.");
            }
        }

        foreach (string managedId in CreateManagedFindingIds())
        {
            if (findingsById.ContainsKey(managedId)
                && !expected.ContainsKey(managedId))
            {
                errors.Add(
                    $"Managed finding '{managedId}' remains after its source condition ended.");
            }
        }

        ValidateFindingContext(
            findingsById,
            errors);

        return findingsById;
    }

    private Dictionary<string, ExpectedFinding> BuildExpectedFindings(
        QaFileCharacteristics? characteristics,
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        StatisticsSnapshot statistics,
        IReadOnlyDictionary<string, QaFinding> findingsById,
        ICollection<string> errors)
    {
        Dictionary<string, ExpectedFinding> expected =
            new(StringComparer.Ordinal);
        HashSet<string> thresholdCanonicalChecklistFailureIds =
            GetBrokenThresholdFailureRelatedCheckIds(
                characteristics,
                statistics,
                errors);

        foreach (QaCheckDefinition definition in checklistDefinitions)
        {
            if (!resultsById.TryGetValue(definition.Id, out QaCheckResult? result))
            {
                continue;
            }

            if (QaDetailedAvailabilityRules
                .SuppressesOrdinaryChecklistFinding(definition.Id))
            {
                continue;
            }

            if (result.Status == QaCheckStatus.Fail)
            {
                if (!ShouldSuppressChecklistFailureForThreshold(
                        result,
                        thresholdCanonicalChecklistFailureIds))
                {
                    AddExpected(
                        expected,
                        QaFindingIds.FailureForCheck(definition.Id),
                        QaFindingSeverity.Failure,
                        QaFindingSource.Checklist,
                        errors);
                }
            }

            string warningId = QaFindingIds.WarningForCheck(definition.Id);

            if (findingsById.ContainsKey(warningId))
            {
                bool applicable = characteristics is not null
                    && TryIsChecklistApplicable(definition, characteristics);
                bool suppressedRejectedWarning = definition.Id.Equals(
                    QaChecklistIds.Database.RejectedRecordsAccountedFor,
                    StringComparison.Ordinal)
                    && statistics.Statistics?.Database?.RejectedRecordCount > 0;

                if (result.Status == QaCheckStatus.Pass
                    && applicable
                    && !suppressedRejectedWarning)
                {
                    AddExpected(
                        expected,
                        warningId,
                        QaFindingSeverity.Warning,
                        QaFindingSource.Checklist,
                        errors);
                }
            }
        }

        foreach (QaFinding availabilityFinding
                 in QaDetailedAvailabilityRules.CreateExpectedFindings(resultsById))
        {
            AddExpected(
                expected,
                availabilityFinding.FindingId,
                availabilityFinding.Severity,
                availabilityFinding.Source,
                errors);
        }

        if (characteristics?.NameColumnMode == QaNameColumnMode.FullName)
        {
            AddExpected(
                expected,
                QaFindingIds.FullNameCharacteristicWarning,
                QaFindingSeverity.Warning,
                QaFindingSource.Checklist,
                errors);
        }

        if (characteristics?.MonetaryColumnScenario ==
            QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns)
        {
            AddExpected(
                expected,
                QaFindingIds.MoreThanTwoMonetaryColumnsCharacteristicWarning,
                QaFindingSeverity.Warning,
                QaFindingSource.Checklist,
                errors);
        }

        if (characteristics?.HasMultipleConfirmationNumberCandidateColumns == true)
        {
            AddExpected(
                expected,
                QaFindingIds.MultipleConfirmationCandidatesCharacteristicWarning,
                QaFindingSeverity.Warning,
                QaFindingSource.Checklist,
                errors);
        }

        foreach ((string fieldId, QaBlankValueStatistic statistic)
                 in statistics.BlankById)
        {
            QaStatisticFieldDefinition definition =
                QaStatisticFieldCatalog.GetRequired(fieldId);
            bool applicable = characteristics is not null
                && IsStatisticApplicable(definition, characteristics, errors);
            QaFindingSeverity? severity =
                QaStatisticsCalculationService.ClassifyThreshold(
                    statistic.BlankCount,
                    statistic.TotalApplicableRows);

            if (applicable && severity is not null)
            {
                AddExpected(
                    expected,
                    severity == QaFindingSeverity.Failure
                        ? QaFindingIds.FailureForBlankStatistic(fieldId)
                        : QaFindingIds.WarningForBlankStatistic(fieldId),
                    severity.Value,
                    QaFindingSource.Statistic,
                    errors);
            }
        }

        foreach ((string fieldId, QaBrokenDataStatistic statistic)
                 in statistics.BrokenById)
        {
            QaStatisticFieldDefinition definition =
                QaStatisticFieldCatalog.GetRequired(fieldId);
            bool applicable = characteristics is not null
                && IsStatisticApplicable(definition, characteristics, errors);
            QaFindingSeverity? severity =
                QaStatisticsCalculationService.ClassifyThreshold(
                    statistic.BrokenValueCount,
                    statistic.TotalApplicableNonblankValues);

            if (applicable && severity is not null)
            {
                AddExpected(
                    expected,
                    severity == QaFindingSeverity.Failure
                        ? QaFindingIds.FailureForBrokenStatistic(fieldId)
                        : QaFindingIds.WarningForBrokenStatistic(fieldId),
                    severity.Value,
                    QaFindingSource.Statistic,
                    errors);
            }
        }

        QaStatistics? reportStatistics = statistics.Statistics;
        bool stayValueApplies = characteristics is not null
            && IsStatisticApplicable(
                QaStatisticFieldCatalog.GetRequired(
                    QaStatisticFieldIds.StayValue),
                characteristics,
                errors);

        if (reportStatistics is not null)
        {
            if (QaStatisticsCalculationService
                .ExceedsArrivalOutsideFileMonthFailureThreshold(
                    reportStatistics.FileMonth))
            {
                AddExpected(
                    expected,
                    QaFindingIds.ArrivalOutsideFileMonthStatisticFailure,
                    QaFindingSeverity.Failure,
                    QaFindingSource.Statistic,
                    errors);
            }

            if (reportStatistics.UnusualAverageRateValues?.HasUnusualValues == true
                && reportStatistics.UnusualAverageRateValues.UnusualValueCount > 0)
            {
                AddExpected(
                    expected,
                    QaFindingIds.UnusualAverageRateStatisticWarning,
                    QaFindingSeverity.Warning,
                    QaFindingSource.Statistic,
                    errors);
            }

            if (stayValueApplies
                && reportStatistics.UnusualStayValues?.HasUnusualValues == true
                && reportStatistics.UnusualStayValues.UnusualValueCount > 0)
            {
                AddExpected(
                    expected,
                    QaFindingIds.UnusualStayValueStatisticWarning,
                    QaFindingSeverity.Warning,
                    QaFindingSource.Statistic,
                    errors);
            }

            if (stayValueApplies
                && reportStatistics.HighStayValues is not null
                && reportStatistics.HighStayValues
                    .StayValuesAboveTenThousandCount > 0
                && !reportStatistics.HighStayValues.AreHighValuesExpected)
            {
                AddExpected(
                    expected,
                    QaFindingIds.HighStayValueStatisticWarning,
                    QaFindingSeverity.Warning,
                    QaFindingSource.Statistic,
                    errors);
            }

            if (reportStatistics.Database is not null)
            {
                int signedDifference = reportStatistics.Database
                    .RawMinusImportedRecordCountDifference;

                if (reportStatistics.FileInformation is not null)
                {
                    try
                    {
                        signedDifference = QaStatisticsCalculationService
                            .CalculateRawMinusImportedDifference(
                                reportStatistics.FileInformation.TotalDataRows,
                                reportStatistics.Database.ImportedRecordCount);
                    }
                    catch (OverflowException)
                    {
                        // The numeric validation reports this separately.
                    }
                }

                long absoluteDifference =
                    QaStatisticsCalculationService.GetAbsoluteDifference(
                        signedDifference);

                if (absoluteDifference >= 10)
                {
                    AddExpected(
                        expected,
                        QaFindingIds.RowDifferenceStatisticWarning,
                        QaFindingSeverity.Warning,
                        QaFindingSource.DatabaseComparison,
                        errors);
                }

                if (reportStatistics.Database.RejectedRecordCount > 0
                    && resultsById.TryGetValue(
                        QaChecklistIds.Database.RejectedRecordsAccountedFor,
                        out QaCheckResult? rejectedResult)
                    && rejectedResult.Status == QaCheckStatus.Pass)
                {
                    AddExpected(
                        expected,
                        QaFindingIds.RejectedRecordsStatisticWarning,
                        QaFindingSeverity.Warning,
                        QaFindingSource.DatabaseComparison,
                        errors);
                }
            }
        }

        return expected;
    }

    private static bool ShouldSuppressChecklistFailureForThreshold(
        QaCheckResult result,
        ISet<string> thresholdCanonicalChecklistFailureIds)
    {
        return thresholdCanonicalChecklistFailureIds.Contains(result.CheckId)
            && TrimToNull(result.Notes) is null;
    }

    private static HashSet<string> GetBrokenThresholdFailureRelatedCheckIds(
        QaFileCharacteristics? characteristics,
        StatisticsSnapshot statistics,
        ICollection<string> errors)
    {
        HashSet<string> checkIds = new(StringComparer.Ordinal);

        if (characteristics is null)
        {
            return checkIds;
        }

        foreach ((string fieldId, QaBrokenDataStatistic statistic)
                 in statistics.BrokenById)
        {
            QaStatisticFieldDefinition definition =
                QaStatisticFieldCatalog.GetRequired(fieldId);

            if (string.IsNullOrWhiteSpace(definition.RelatedChecklistId)
                || !IsStatisticApplicable(
                    definition,
                    characteristics,
                    errors)
                || QaStatisticsCalculationService.ClassifyThreshold(
                    statistic.BrokenValueCount,
                    statistic.TotalApplicableNonblankValues)
                    != QaFindingSeverity.Failure)
            {
                continue;
            }

            checkIds.Add(definition.RelatedChecklistId);
        }

        return checkIds;
    }

    private static void ValidateFindingContext(
        IReadOnlyDictionary<string, QaFinding> findingsById,
        ICollection<string> errors)
    {
        if (findingsById.TryGetValue(
                QaFindingIds.RowDifferenceStatisticWarning,
                out QaFinding? rowDifference)
            && TrimToNull(rowDifference.ResolutionNotes) is null)
        {
            errors.Add("The row-difference warning requires a non-sensitive explanation.");
        }
    }

    private void ValidateChecklistExplanations(
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        IReadOnlyDictionary<string, QaFinding> findingsById,
        ICollection<string> errors)
    {
        foreach (QaCheckDefinition definition in checklistDefinitions)
        {
            if (!resultsById.TryGetValue(definition.Id, out QaCheckResult? result))
            {
                continue;
            }

            if (result.Status == QaCheckStatus.Fail
                && !QaDetailedAvailabilityRules
                    .SuppressesOrdinaryChecklistFinding(definition.Id)
                && TrimToNull(result.Notes) is null)
            {
                string failureId = QaFindingIds.FailureForCheck(definition.Id);
                bool hasContextualGeneratedDescription =
                    findingsById.TryGetValue(failureId, out QaFinding? finding)
                    && TrimToNull(finding.Description) is string description
                    && !description.Equals(
                        definition.Description,
                        StringComparison.Ordinal);
                hasContextualGeneratedDescription |= findingsById.Values.Any(
                    candidate => candidate.Severity == QaFindingSeverity.Failure
                        && candidate.Source == QaFindingSource.Statistic
                        && string.Equals(
                            candidate.RelatedCheckId,
                            definition.Id,
                            StringComparison.Ordinal)
                        && candidate.FindingId.StartsWith(
                            "FAIL:STAT:BROKEN:",
                            StringComparison.Ordinal));

                if (!hasContextualGeneratedDescription)
                {
                    errors.Add(
                        $"Failed checklist item '{definition.DisplayName}' requires meaningful notes.");
                }
            }

            string warningId = QaFindingIds.WarningForCheck(definition.Id);

            if (result.Status == QaCheckStatus.Pass
                && findingsById.ContainsKey(warningId)
                && TrimToNull(result.Notes) is null)
            {
                errors.Add(
                    $"Passed-check warning for '{definition.DisplayName}' requires an explanation.");
            }
        }
    }

    private HashSet<string> CreateManagedFindingIds()
    {
        HashSet<string> ids = new(StringComparer.Ordinal)
        {
            QaFindingIds.FullNameCharacteristicWarning,
            QaFindingIds.MoreThanTwoMonetaryColumnsCharacteristicWarning,
            QaFindingIds.MultipleConfirmationCandidatesCharacteristicWarning,
            QaFindingIds.UnusualAverageRateStatisticWarning,
            QaFindingIds.UnusualStayValueStatisticWarning,
            QaFindingIds.HighStayValueStatisticWarning,
            QaFindingIds.RowDifferenceStatisticWarning,
            QaFindingIds.RejectedRecordsStatisticWarning,
            QaFindingIds.ArrivalOutsideFileMonthStatisticFailure,
            QaFindingIds.StrategySourceRateMarketWarning,
            QaFindingIds.StrategySourceRateMarketFailure
        };

        foreach (QaCheckDefinition definition in checklistDefinitions)
        {
            ids.Add(QaFindingIds.FailureForCheck(definition.Id));
            ids.Add(QaFindingIds.WarningForCheck(definition.Id));
        }

        foreach (QaStatisticFieldDefinition definition
                 in QaStatisticFieldCatalog.Definitions)
        {
            ids.Add(QaFindingIds.WarningForBlankStatistic(definition.Id));
            ids.Add(QaFindingIds.FailureForBlankStatistic(definition.Id));
            ids.Add(QaFindingIds.WarningForBrokenStatistic(definition.Id));
            ids.Add(QaFindingIds.FailureForBrokenStatistic(definition.Id));
        }

        return ids;
    }

    private static bool IsStatisticApplicable(
        QaStatisticFieldDefinition definition,
        QaFileCharacteristics characteristics,
        ICollection<string> errors)
    {
        try
        {
            return QaStatisticFieldCatalog.IsApplicable(
                definition,
                characteristics);
        }
        catch (ArgumentOutOfRangeException)
        {
            errors.Add(
                $"Statistics field '{definition.Id}' has an unknown applicability value.");
            return false;
        }
    }

    private static bool IsMissingFieldDocumented(
        string fieldId,
        IReadOnlyDictionary<string, QaCheckResult> resultsById)
    {
        if (fieldId == QaStatisticFieldIds.SourceRateMarket)
        {
            string[] strategyCheckIds =
            [
                QaChecklistIds.Raw.StrategySourceColumnAvailable,
                QaChecklistIds.Raw.StrategyRateColumnAvailable,
                QaChecklistIds.Raw.StrategyMarketColumnAvailable
            ];

            return strategyCheckIds.All(checkId =>
                resultsById.TryGetValue(checkId, out QaCheckResult? result)
                && result.Status == QaCheckStatus.Fail);
        }

        string presenceCheckId = fieldId switch
        {
            QaStatisticFieldIds.FirstName or
            QaStatisticFieldIds.LastName or
            QaStatisticFieldIds.FullName =>
                QaChecklistIds.Raw.RequiredNameFieldPresent,
            QaStatisticFieldIds.ConfirmationNumber =>
                QaChecklistIds.Raw.RequiredConfirmationPresent,
            QaStatisticFieldIds.Email =>
                QaChecklistIds.Raw.EmailColumnAvailable,
            QaStatisticFieldIds.ReservationDate =>
                QaChecklistIds.Raw.RequiredReservationDatePresent,
            QaStatisticFieldIds.ArrivalDate =>
                QaChecklistIds.Raw.RequiredArrivalDatePresent,
            QaStatisticFieldIds.DepartureDate =>
                QaChecklistIds.Raw.RequiredDepartureDatePresent,
            QaStatisticFieldIds.AverageRate or
            QaStatisticFieldIds.StayValue =>
                QaChecklistIds.Raw.RequiredMonetaryValuePresent,
            _ => throw new ArgumentOutOfRangeException(
                nameof(fieldId),
                fieldId,
                "Unknown statistics field ID.")
        };

        return resultsById.TryGetValue(
                presenceCheckId,
                out QaCheckResult? presenceResult)
            && presenceResult.Status == QaCheckStatus.Fail;
    }

    private static bool TryIsChecklistApplicable(
        QaCheckDefinition definition,
        QaFileCharacteristics characteristics)
    {
        try
        {
            return QaChecklistApplicabilityEvaluator.IsApplicable(
                definition,
                characteristics);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static void AddExpected(
        IDictionary<string, ExpectedFinding> expected,
        string id,
        QaFindingSeverity severity,
        QaFindingSource source,
        ICollection<string> errors)
    {
        if (!expected.TryAdd(id, new ExpectedFinding(severity, source)))
        {
            errors.Add(
                $"Finding rules produced duplicate deterministic ID '{id}'.");
        }
    }

    private static IReadOnlyDictionary<string, QaCheckDefinition>
        IndexDefinitions(IReadOnlyList<QaCheckDefinition> definitions)
    {
        if (definitions.Count == 0)
        {
            throw new ArgumentException(
                "At least one checklist definition is required.",
                nameof(definitions));
        }

        Dictionary<string, QaCheckDefinition> byId =
            new(StringComparer.Ordinal);

        foreach (QaCheckDefinition? definition in definitions)
        {
            if (definition is null
                || string.IsNullOrWhiteSpace(definition.Id)
                || string.IsNullOrWhiteSpace(definition.DisplayName)
                || string.IsNullOrWhiteSpace(definition.Description))
            {
                throw new ArgumentException(
                    "Every checklist definition requires an ID, display name, and description.",
                    nameof(definitions));
            }

            if (!byId.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException(
                    $"Checklist definition ID '{definition.Id}' appears more than once.",
                    nameof(definitions));
            }
        }

        return byId;
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed class StatisticsSnapshot
    {
        public QaStatistics? Statistics { get; set; }

        public IReadOnlyDictionary<string, QaBlankValueStatistic> BlankById
            { get; set; } =
                new Dictionary<string, QaBlankValueStatistic>(
                    StringComparer.Ordinal);

        public IReadOnlyDictionary<string, QaBrokenDataStatistic> BrokenById
            { get; set; } =
                new Dictionary<string, QaBrokenDataStatistic>(
                    StringComparer.Ordinal);
    }

    private sealed record ExpectedFinding(
        QaFindingSeverity Severity,
        QaFindingSource Source);
}
