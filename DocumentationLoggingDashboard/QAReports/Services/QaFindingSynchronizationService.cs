using System.Globalization;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Forms;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Reconciles generated QA findings with the current in-memory report draft.
/// </summary>
public sealed class QaFindingSynchronizationService
{
    private readonly IReadOnlyList<QaCheckDefinition> checklistDefinitions;
    private readonly Dictionary<string, QaCheckDefinition> checklistDefinitionsById;
    private readonly IReadOnlyList<QaStatisticFieldDefinition> statisticFieldDefinitions;
    private readonly Dictionary<string, QaStatisticFieldDefinition>
        statisticFieldDefinitionsById;
    private readonly HashSet<string> managedFindingIds;

    public QaFindingSynchronizationService()
        : this(
            QaChecklistCatalog.Definitions,
            QaStatisticFieldCatalog.Definitions)
    {
    }

    public QaFindingSynchronizationService(
        IReadOnlyList<QaCheckDefinition> checklistDefinitions)
        : this(checklistDefinitions, QaStatisticFieldCatalog.Definitions)
    {
    }

    public QaFindingSynchronizationService(
        IReadOnlyList<QaCheckDefinition> checklistDefinitions,
        IReadOnlyList<QaStatisticFieldDefinition> statisticFieldDefinitions)
    {
        ArgumentNullException.ThrowIfNull(checklistDefinitions);
        ArgumentNullException.ThrowIfNull(statisticFieldDefinitions);

        this.checklistDefinitions = checklistDefinitions.ToArray();
        checklistDefinitionsById = ValidateAndIndexDefinitions(
            this.checklistDefinitions);
        this.statisticFieldDefinitions = statisticFieldDefinitions.ToArray();
        statisticFieldDefinitionsById = ValidateAndIndexStatisticDefinitions(
            this.statisticFieldDefinitions,
            checklistDefinitionsById);
        managedFindingIds = CreateManagedFindingIds(
            this.checklistDefinitions,
            this.statisticFieldDefinitions);
    }

    /// <summary>
    /// Adds or removes the deterministic warning that represents one checklist row's
    /// Warning Found selection, then reconciles all generated findings.
    /// </summary>
    public void SetChecklistWarningSelected(
        QaReport report,
        string checkId,
        bool selected)
    {
        ArgumentNullException.ThrowIfNull(report);

        string findingId = QaFindingIds.WarningForCheck(checkId);

        if (!checklistDefinitionsById.TryGetValue(
                checkId,
                out QaCheckDefinition? definition))
        {
            throw new ArgumentException(
                $"Checklist ID '{checkId}' does not exist in the finding service's definition snapshot.",
                nameof(checkId));
        }

        Dictionary<string, QaCheckResult> resultsById =
            ValidateAndIndexResults(report);
        Dictionary<string, QaFinding> existingById =
            ValidateAndIndexFindings(report.Findings);

        QaFileCharacteristics characteristics = GetFileCharacteristics(report);
        QaCheckResult result = resultsById[checkId];
        bool canSelectWarning = selected
            && result.Status == QaCheckStatus.Pass
            && !QaDetailedAvailabilityRules
                .SuppressesOrdinaryChecklistFinding(checkId)
            && QaChecklistApplicabilityEvaluator.IsApplicable(
                definition,
                characteristics);

        if (!canSelectWarning)
        {
            if (existingById.TryGetValue(findingId, out QaFinding? existing))
            {
                report.Findings.Remove(existing);
            }
        }
        else if (!existingById.ContainsKey(findingId))
        {
            report.Findings.Add(CreateChecklistWarning(definition, result));
        }

        SynchronizeFindings(report);
    }

    /// <summary>
    /// Synchronizes generated failures and warnings while preserving valid state on
    /// finding objects whose deterministic IDs remain continuously expected.
    /// </summary>
    public void SynchronizeFindings(QaReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        QaFileCharacteristics characteristics = GetFileCharacteristics(report);
        Dictionary<string, QaCheckResult> resultsById =
            ValidateAndIndexResults(report);
        Dictionary<string, QaFinding> existingById =
            ValidateAndIndexFindings(report.Findings);

        Dictionary<string, QaFinding> expectedGeneratedById =
            BuildExpectedGeneratedFindings(
                report,
                resultsById,
                existingById,
                characteristics);

        List<QaFinding> synchronizedFindings = [];

        foreach (QaFinding expected in expectedGeneratedById.Values)
        {
            QaFinding synchronized;

            if (existingById.TryGetValue(expected.FindingId, out QaFinding? existing))
            {
                ApplyGeneratedFields(existing, expected);
                synchronized = existing;
            }
            else
            {
                synchronized = expected;
            }

            NormalizeResolution(
                synchronized,
                characteristics.IsCustomScriptSupportAvailable);
            synchronizedFindings.Add(synchronized);
        }

        foreach (QaFinding existing in report.Findings)
        {
            if (managedFindingIds.Contains(existing.FindingId))
            {
                continue;
            }

            NormalizeResolution(
                existing,
                characteristics.IsCustomScriptSupportAvailable);
            synchronizedFindings.Add(existing);
        }

        synchronizedFindings.Sort(
            (left, right) => StringComparer.Ordinal.Compare(
                left.FindingId,
                right.FindingId));

        report.Findings.Clear();
        report.Findings.AddRange(synchronizedFindings);

        ValidateAndIndexFindings(report.Findings);
    }

    private Dictionary<string, QaFinding> BuildExpectedGeneratedFindings(
        QaReport report,
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        IReadOnlyDictionary<string, QaFinding> existingById,
        QaFileCharacteristics characteristics)
    {
        Dictionary<string, QaFinding> expectedById =
            new(StringComparer.Ordinal);
        QaStatistics statistics = GetStatistics(report);
        bool rejectedRecordsStatisticWarningExpected =
            statistics.Database.RejectedRecordCount > 0
            && resultsById.TryGetValue(
                QaChecklistIds.Database.RejectedRecordsAccountedFor,
                out QaCheckResult? rejectedRecordsResult)
            && rejectedRecordsResult.Status == QaCheckStatus.Pass;
        HashSet<string> thresholdCanonicalChecklistFailureIds =
            GetBrokenThresholdFailureRelatedCheckIds(
                statistics,
                characteristics);

        foreach (QaCheckDefinition definition in checklistDefinitions)
        {
            QaCheckResult result = resultsById[definition.Id];

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
                        expectedById,
                        CreateChecklistFailure(definition, result));
                }

                continue;
            }

            string warningId = QaFindingIds.WarningForCheck(definition.Id);

            if (result.Status == QaCheckStatus.Pass
                && existingById.ContainsKey(warningId)
                && (!definition.Id.Equals(
                        QaChecklistIds.Database.RejectedRecordsAccountedFor,
                        StringComparison.Ordinal)
                    || !rejectedRecordsStatisticWarningExpected)
                && QaChecklistApplicabilityEvaluator.IsApplicable(
                    definition,
                    characteristics))
            {
                AddExpected(
                    expectedById,
                    CreateChecklistWarning(definition, result));
            }
        }

        foreach (QaFinding availabilityFinding
                 in QaDetailedAvailabilityRules.CreateExpectedFindings(resultsById))
        {
            AddExpected(expectedById, availabilityFinding);
        }

        if (characteristics.NameColumnMode == QaNameColumnMode.FullName)
        {
            AddExpected(expectedById, CreateFullNameWarning());
        }

        if (characteristics.MonetaryColumnScenario ==
            QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns)
        {
            AddExpected(
                expectedById,
                CreateMoreThanTwoMonetaryColumnsWarning());
        }

        if (characteristics.HasMultipleConfirmationNumberCandidateColumns)
        {
            AddExpected(
                expectedById,
                CreateMultipleConfirmationCandidatesWarning());
        }

        AddExpectedStatisticFindings(
            expectedById,
            statistics,
            report.HotelInformation?.FileMonth,
            resultsById,
            existingById,
            characteristics,
            rejectedRecordsStatisticWarningExpected);

        return expectedById;
    }

    private static bool ShouldSuppressChecklistFailureForThreshold(
        QaCheckResult result,
        ISet<string> thresholdCanonicalChecklistFailureIds)
    {
        return thresholdCanonicalChecklistFailureIds.Contains(result.CheckId)
            && TrimToNull(result.Notes) is null;
    }

    private static QaFinding CreateChecklistFailure(
        QaCheckDefinition definition,
        QaCheckResult result)
    {
        return new QaFinding
        {
            FindingId = QaFindingIds.FailureForCheck(result.CheckId),
            RelatedCheckId = result.CheckId,
            Severity = QaFindingSeverity.Failure,
            Title = $"Failed check: {definition.DisplayName}",
            Description = TrimToNull(result.Notes) ?? definition.Description,
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static QaFinding CreateChecklistWarning(
        QaCheckDefinition definition,
        QaCheckResult result)
    {
        return new QaFinding
        {
            FindingId = QaFindingIds.WarningForCheck(result.CheckId),
            RelatedCheckId = result.CheckId,
            Severity = QaFindingSeverity.Warning,
            Title = $"Warning for passed check: {definition.DisplayName}",
            Description = TrimToNull(result.Notes) ?? string.Empty,
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static QaFinding CreateFullNameWarning()
    {
        return new QaFinding
        {
            FindingId = QaFindingIds.FullNameCharacteristicWarning,
            RelatedCheckId = QaChecklistIds.Raw.RequiredNameFieldPresent,
            Severity = QaFindingSeverity.Warning,
            Title = "Full Name field is used",
            Description = "The file uses one Full Name field rather than separate First Name and Last Name fields.",
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static QaFinding CreateMoreThanTwoMonetaryColumnsWarning()
    {
        return new QaFinding
        {
            FindingId =
                QaFindingIds.MoreThanTwoMonetaryColumnsCharacteristicWarning,
            RelatedCheckId = QaChecklistIds.Raw.RequiredMonetaryValuePresent,
            Severity = QaFindingSeverity.Warning,
            Title = "More than two monetary-value columns were found",
            Description = "More than two candidate monetary-value columns require special review to identify the correct Average Rate and Stay Value fields.",
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static QaFinding CreateMultipleConfirmationCandidatesWarning()
    {
        return new QaFinding
        {
            FindingId =
                QaFindingIds.MultipleConfirmationCandidatesCharacteristicWarning,
            RelatedCheckId = QaChecklistIds.Raw.ConfirmationCandidatesReviewed,
            Severity = QaFindingSeverity.Warning,
            Title = "Multiple Confirmation Number candidates were found",
            Description = "Multiple candidate fields were found, and the correct Confirmation Number field must be confirmed.",
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private void AddExpectedStatisticFindings(
        IDictionary<string, QaFinding> expectedById,
        QaStatistics statistics,
        QaFileMonth? selectedFileMonth,
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        IReadOnlyDictionary<string, QaFinding> existingById,
        QaFileCharacteristics characteristics,
        bool rejectedRecordsStatisticWarningExpected)
    {
        if (QaStatisticsCalculationService
            .ExceedsArrivalOutsideFileMonthFailureThreshold(
                statistics.FileMonth))
        {
            AddExpected(
                expectedById,
                CreateArrivalOutsideFileMonthFailure(
                    selectedFileMonth,
                    statistics.FileMonth));
        }

        AddExpectedBlankStatisticFindings(
            expectedById,
            statistics,
            characteristics);
        AddExpectedBrokenStatisticFindings(
            expectedById,
            statistics,
            characteristics);

        if (IsStatisticFieldApplicable(
                QaStatisticFieldIds.AverageRate,
                characteristics)
            && statistics.UnusualAverageRateValues.HasUnusualValues
            && statistics.UnusualAverageRateValues.UnusualValueCount > 0)
        {
            AddExpected(
                expectedById,
                CreateUnusualMonetaryWarning(
                    QaFindingIds.UnusualAverageRateStatisticWarning,
                    "Average Rate",
                    QaStatisticFieldIds.AverageRate,
                    statistics.UnusualAverageRateValues,
                    statistics));
        }

        if (IsStatisticFieldApplicable(
                QaStatisticFieldIds.StayValue,
                characteristics))
        {
            if (statistics.UnusualStayValues.HasUnusualValues
                && statistics.UnusualStayValues.UnusualValueCount > 0)
            {
                AddExpected(
                    expectedById,
                    CreateUnusualMonetaryWarning(
                        QaFindingIds.UnusualStayValueStatisticWarning,
                        "Stay Value",
                        QaStatisticFieldIds.StayValue,
                        statistics.UnusualStayValues,
                        statistics));
            }

            if (statistics.HighStayValues.StayValuesAboveTenThousandCount > 0
                && !statistics.HighStayValues.AreHighValuesExpected)
            {
                AddExpected(
                    expectedById,
                    CreateHighStayValueWarning(statistics));
            }
        }

        AddExpectedDatabaseStatisticWarnings(
            expectedById,
            statistics,
            resultsById,
            existingById,
            rejectedRecordsStatisticWarningExpected);
    }

    private void AddExpectedBlankStatisticFindings(
        IDictionary<string, QaFinding> expectedById,
        QaStatistics statistics,
        QaFileCharacteristics characteristics)
    {
        foreach (QaBlankValueStatistic? statistic in statistics.BlankValues)
        {
            if (statistic is null
                || string.IsNullOrWhiteSpace(statistic.FieldId)
                || !statisticFieldDefinitionsById.TryGetValue(
                    statistic.FieldId,
                    out QaStatisticFieldDefinition? definition)
                || !definition.SupportsBlankStatistics
                || !QaStatisticFieldCatalog.IsApplicable(
                    definition,
                    characteristics))
            {
                continue;
            }

            decimal percentage =
                QaStatisticsCalculationService.CalculatePercentage(
                    statistic.BlankCount,
                    statistic.TotalApplicableRows);
            QaFindingSeverity? severity =
                QaStatisticsCalculationService.ClassifyThreshold(
                    statistic.BlankCount,
                    statistic.TotalApplicableRows);

            if (severity is not null)
            {
                AddExpected(
                    expectedById,
                    CreateBlankStatisticFinding(
                        definition,
                        statistic,
                        percentage,
                        severity.Value));
            }
        }
    }

    private void AddExpectedBrokenStatisticFindings(
        IDictionary<string, QaFinding> expectedById,
        QaStatistics statistics,
        QaFileCharacteristics characteristics)
    {
        foreach (QaBrokenDataStatistic? statistic in statistics.BrokenData)
        {
            if (statistic is null
                || string.IsNullOrWhiteSpace(statistic.FieldId)
                || !statisticFieldDefinitionsById.TryGetValue(
                    statistic.FieldId,
                    out QaStatisticFieldDefinition? definition)
                || !definition.SupportsBrokenDataStatistics
                || !QaStatisticFieldCatalog.IsApplicable(
                    definition,
                    characteristics))
            {
                continue;
            }

            decimal percentage =
                QaStatisticsCalculationService.CalculatePercentage(
                    statistic.BrokenValueCount,
                    statistic.TotalApplicableNonblankValues);
            QaFindingSeverity? severity =
                QaStatisticsCalculationService.ClassifyThreshold(
                    statistic.BrokenValueCount,
                    statistic.TotalApplicableNonblankValues);

            if (severity is not null)
            {
                AddExpected(
                    expectedById,
                    CreateBrokenStatisticFinding(
                        definition,
                        statistic,
                        percentage,
                        severity.Value));
            }
        }
    }

    private HashSet<string> GetBrokenThresholdFailureRelatedCheckIds(
        QaStatistics statistics,
        QaFileCharacteristics characteristics)
    {
        HashSet<string> checkIds = new(StringComparer.Ordinal);

        foreach (QaBrokenDataStatistic? statistic in statistics.BrokenData)
        {
            if (statistic is null
                || string.IsNullOrWhiteSpace(statistic.FieldId)
                || !statisticFieldDefinitionsById.TryGetValue(
                    statistic.FieldId,
                    out QaStatisticFieldDefinition? definition)
                || string.IsNullOrWhiteSpace(definition.RelatedChecklistId)
                || !definition.SupportsBrokenDataStatistics
                || !QaStatisticFieldCatalog.IsApplicable(
                    definition,
                    characteristics)
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

    private void AddExpectedDatabaseStatisticWarnings(
        IDictionary<string, QaFinding> expectedById,
        QaStatistics statistics,
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        IReadOnlyDictionary<string, QaFinding> existingById,
        bool rejectedRecordsStatisticWarningExpected)
    {
        int signedDifference =
            QaStatisticsCalculationService.CalculateRawMinusImportedDifference(
                statistics.FileInformation.TotalDataRows,
                statistics.Database.ImportedRecordCount);
        long absoluteDifference =
            QaStatisticsCalculationService.GetAbsoluteDifference(signedDifference);

        if (absoluteDifference >= 10L)
        {
            string? explanation = existingById.TryGetValue(
                    QaFindingIds.RowDifferenceStatisticWarning,
                    out QaFinding? existing)
                ? TrimToNull(existing.ResolutionNotes)
                : null;
            AddExpected(
                expectedById,
                CreateRowDifferenceWarning(
                    statistics.FileInformation.TotalDataRows,
                    statistics.Database.ImportedRecordCount,
                    signedDifference,
                    absoluteDifference,
                    explanation));
        }

        if (rejectedRecordsStatisticWarningExpected
            && resultsById.TryGetValue(
                QaChecklistIds.Database.RejectedRecordsAccountedFor,
                out QaCheckResult? result))
        {
            AddExpected(
                expectedById,
                CreateRejectedRecordsWarning(
                    statistics.Database.RejectedRecordCount,
                    result));
        }
    }

    private static QaFinding CreateBlankStatisticFinding(
        QaStatisticFieldDefinition definition,
        QaBlankValueStatistic statistic,
        decimal calculatedPercentage,
        QaFindingSeverity severity)
    {
        bool isFailure = severity == QaFindingSeverity.Failure;

        return new QaFinding
        {
            FindingId = isFailure
                ? QaFindingIds.FailureForBlankStatistic(definition.Id)
                : QaFindingIds.WarningForBlankStatistic(definition.Id),
            Severity = severity,
            Title = isFailure
                ? $"High blank-value percentage: {definition.DisplayName}"
                : $"Blank values found: {definition.DisplayName}",
            Description =
                $"{definition.DisplayName} has {statistic.BlankCount} blank values out of {statistic.TotalApplicableRows} applicable rows ({FormatPercentage(calculatedPercentage)}%).",
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Statistic
        };
    }

    private static QaFinding CreateBrokenStatisticFinding(
        QaStatisticFieldDefinition definition,
        QaBrokenDataStatistic statistic,
        decimal calculatedPercentage,
        QaFindingSeverity severity)
    {
        bool isFailure = severity == QaFindingSeverity.Failure;
        string description =
            $"{definition.DisplayName} has {statistic.BrokenValueCount} broken populated values out of {statistic.TotalApplicableNonblankValues} applicable nonblank values ({FormatPercentage(calculatedPercentage)}%).";

        return new QaFinding
        {
            FindingId = isFailure
                ? QaFindingIds.FailureForBrokenStatistic(definition.Id)
                : QaFindingIds.WarningForBrokenStatistic(definition.Id),
            RelatedCheckId = definition.RelatedChecklistId,
            Severity = severity,
            Title = isFailure
                ? $"High broken-data percentage: {definition.DisplayName}"
                : $"Broken data found: {definition.DisplayName}",
            Description = AppendExplanation(description, statistic.Explanation),
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Statistic
        };
    }

    private static QaFinding CreateArrivalOutsideFileMonthFailure(
        QaFileMonth? selectedFileMonth,
        QaFileMonthStatistics statistics)
    {
        decimal exactOutsidePercentage =
            statistics.ArrivalDatesOutsideFileMonth
            * 100m
            / statistics.ValidArrivalDateCount;
        string fileMonth = selectedFileMonth?.ToString() ?? "Not selected";
        string percentage = exactOutsidePercentage.ToString(
            "0.############################",
            CultureInfo.InvariantCulture);

        return new QaFinding
        {
            FindingId =
                QaFindingIds.ArrivalOutsideFileMonthStatisticFailure,
            RelatedCheckId = null,
            Severity = QaFindingSeverity.Failure,
            Title =
                "More than 30% of Arrival Dates are outside the selected File Month",
            Description =
                $"Selected File Month: {fileMonth}. Valid Arrival Dates: {statistics.ValidArrivalDateCount}; inside File Month: {statistics.ArrivalDatesWithinFileMonth}; outside File Month: {statistics.ArrivalDatesOutsideFileMonth}; outside percentage: {percentage}%.",
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Statistic
        };
    }

    private static QaFinding CreateUnusualMonetaryWarning(
        string findingId,
        string displayName,
        string fieldId,
        QaUnusualMonetaryValueStatistics statistic,
        QaStatistics statistics)
    {
        int denominator = GetDerivedNonblankCount(statistics, fieldId);
        decimal percentage =
            QaStatisticsCalculationService.CalculatePercentage(
                statistic.UnusualValueCount,
                denominator);
        string description =
            $"{displayName} has {statistic.UnusualValueCount} manually identified unusual values out of {denominator} applicable nonblank values ({FormatPercentage(percentage)}%).";

        return new QaFinding
        {
            FindingId = findingId,
            Severity = QaFindingSeverity.Warning,
            Title = $"Unusual {displayName} values were found",
            Description = AppendExplanation(description, statistic.Explanation),
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Statistic
        };
    }

    private static QaFinding CreateHighStayValueWarning(QaStatistics statistics)
    {
        QaHighStayValueStatistics statistic = statistics.HighStayValues;
        int denominator = GetDerivedNonblankCount(
            statistics,
            QaStatisticFieldIds.StayValue);
        decimal percentage =
            QaStatisticsCalculationService.CalculatePercentage(
                statistic.StayValuesAboveTenThousandCount,
                denominator);
        string description =
            $"Stay Value has {statistic.StayValuesAboveTenThousandCount} values strictly above 10,000 out of {denominator} applicable nonblank values ({FormatPercentage(percentage)}%), and those high values were not expected.";

        return new QaFinding
        {
            FindingId = QaFindingIds.HighStayValueStatisticWarning,
            Severity = QaFindingSeverity.Warning,
            Title = "Unexpected Stay Values strictly above 10,000",
            Description = AppendExplanation(description, statistic.Explanation),
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Statistic
        };
    }

    private static QaFinding CreateRowDifferenceWarning(
        int totalDataRows,
        int importedRecordCount,
        int signedDifference,
        long absoluteDifference,
        string? explanation)
    {
        string description =
            $"Total Data Rows: {totalDataRows}; Imported Record Count: {importedRecordCount}; signed raw-minus-imported difference: {signedDifference}; absolute difference: {absoluteDifference}.";

        return new QaFinding
        {
            FindingId = QaFindingIds.RowDifferenceStatisticWarning,
            Severity = QaFindingSeverity.Warning,
            Title = "Raw and imported record counts differ by 10 or more",
            Description = AppendExplanation(description, explanation),
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.DatabaseComparison
        };
    }

    private static QaFinding CreateRejectedRecordsWarning(
        int rejectedRecordCount,
        QaCheckResult result)
    {
        string description =
            $"{rejectedRecordCount} database records were rejected, reviewed, and accounted for with a passing checklist result.";

        return new QaFinding
        {
            FindingId = QaFindingIds.RejectedRecordsStatisticWarning,
            RelatedCheckId =
                QaChecklistIds.Database.RejectedRecordsAccountedFor,
            Severity = QaFindingSeverity.Warning,
            Title = "Rejected database records were reviewed",
            Description = AppendExplanation(description, result.Notes),
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.DatabaseComparison
        };
    }

    private bool IsStatisticFieldApplicable(
        string fieldId,
        QaFileCharacteristics characteristics)
    {
        return statisticFieldDefinitionsById.TryGetValue(
                fieldId,
                out QaStatisticFieldDefinition? definition)
            && QaStatisticFieldCatalog.IsApplicable(
                definition,
                characteristics);
    }

    private static int GetDerivedNonblankCount(
        QaStatistics statistics,
        string fieldId)
    {
        QaBlankValueStatistic? blankStatistic = statistics.BlankValues
            .FirstOrDefault(
                statistic => statistic is not null
                    && string.Equals(
                        statistic.FieldId,
                        fieldId,
                        StringComparison.Ordinal));

        return blankStatistic is null
            ? 0
            : QaStatisticsCalculationService.CalculateDerivedNonblankCount(
                blankStatistic);
    }

    private static string AppendExplanation(
        string description,
        string? explanation)
    {
        string? trimmedExplanation = TrimToNull(explanation);
        return trimmedExplanation is null
            ? description
            : $"{description} Explanation: {trimmedExplanation}";
    }

    private static string FormatPercentage(decimal percentage)
    {
        return percentage.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static void AddExpected(
        IDictionary<string, QaFinding> expectedById,
        QaFinding finding)
    {
        if (!expectedById.TryAdd(finding.FindingId, finding))
        {
            throw new InvalidOperationException(
                $"Finding generation produced duplicate ID '{finding.FindingId}'.");
        }
    }

    private static void ApplyGeneratedFields(
        QaFinding target,
        QaFinding expected)
    {
        target.FindingId = expected.FindingId;
        target.RelatedCheckId = expected.RelatedCheckId;
        target.Severity = expected.Severity;
        target.Title = expected.Title;
        target.Description = expected.Description;
        target.Source = expected.Source;
    }

    private static void NormalizeResolution(
        QaFinding finding,
        bool isCustomScriptSupportAvailable)
    {
        finding.ResolutionNotes = TrimToNull(finding.ResolutionNotes);
        finding.CustomScriptName = TrimToNull(finding.CustomScriptName);

        if (finding.Resolution == QaFindingResolution.HandledByCustomScript
            && !isCustomScriptSupportAvailable)
        {
            finding.Resolution = QaFindingResolution.Active;
            finding.CustomScriptName = null;
            finding.ResolutionNotes = null;
            return;
        }

        if (finding.Resolution == QaFindingResolution.ExplainedAndAccepted)
        {
            finding.CustomScriptName = null;

            if (finding.Severity == QaFindingSeverity.Warning)
            {
                return;
            }

            finding.Resolution = QaFindingResolution.Active;
            finding.ResolutionNotes = null;
            return;
        }

        if (finding.Resolution == QaFindingResolution.HandledByCustomScript)
        {
            return;
        }

        if (finding.Resolution != QaFindingResolution.Active)
        {
            finding.Resolution = QaFindingResolution.Active;
        }

        finding.CustomScriptName = null;
    }

    private static Dictionary<string, QaCheckDefinition>
        ValidateAndIndexDefinitions(
            IReadOnlyList<QaCheckDefinition> definitions)
    {
        if (definitions.Count == 0)
        {
            throw new ArgumentException(
                "At least one checklist definition is required.",
                nameof(definitions));
        }

        Dictionary<string, QaCheckDefinition> definitionsById =
            new(StringComparer.Ordinal);

        foreach (QaCheckDefinition? definition in definitions)
        {
            if (definition is null)
            {
                throw new ArgumentException(
                    "The checklist definition snapshot cannot contain null entries.",
                    nameof(definitions));
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new ArgumentException(
                    "Every checklist definition must have a nonblank stable ID.",
                    nameof(definitions));
            }

            if (string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                throw new ArgumentException(
                    $"Checklist definition '{definition.Id}' must have a nonblank display name.",
                    nameof(definitions));
            }

            if (string.IsNullOrWhiteSpace(definition.Description))
            {
                throw new ArgumentException(
                    $"Checklist definition '{definition.Id}' must have a nonblank description.",
                    nameof(definitions));
            }

            if (!definitionsById.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException(
                    $"The checklist definition snapshot contains duplicate stable ID '{definition.Id}'.",
                    nameof(definitions));
            }
        }

        return definitionsById;
    }

    private static Dictionary<string, QaStatisticFieldDefinition>
        ValidateAndIndexStatisticDefinitions(
            IReadOnlyList<QaStatisticFieldDefinition> definitions,
            IReadOnlyDictionary<string, QaCheckDefinition>
                checklistDefinitionsById)
    {
        Dictionary<string, QaStatisticFieldDefinition> definitionsById =
            new(StringComparer.Ordinal);

        foreach (QaStatisticFieldDefinition? definition in definitions)
        {
            if (definition is null)
            {
                throw new ArgumentException(
                    "The statistic field definition snapshot cannot contain null entries.",
                    nameof(definitions));
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new ArgumentException(
                    "Every statistic field definition must have a nonblank stable ID.",
                    nameof(definitions));
            }

            if (string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                throw new ArgumentException(
                    $"Statistic field definition '{definition.Id}' must have a nonblank display name.",
                    nameof(definitions));
            }

            if (!Enum.IsDefined(
                    typeof(QaStatisticFieldApplicability),
                    definition.Applicability))
            {
                throw new ArgumentException(
                    $"Statistic field definition '{definition.Id}' has an unknown applicability value.",
                    nameof(definitions));
            }

            if (definition.RelatedChecklistId is not null
                && string.IsNullOrWhiteSpace(definition.RelatedChecklistId))
            {
                throw new ArgumentException(
                    $"Statistic field definition '{definition.Id}' has an invalid related checklist ID.",
                    nameof(definitions));
            }

            if (definition.RelatedChecklistId is not null
                && !checklistDefinitionsById.ContainsKey(
                    definition.RelatedChecklistId))
            {
                throw new ArgumentException(
                    $"Statistic field definition '{definition.Id}' references unknown checklist ID '{definition.RelatedChecklistId}'.",
                    nameof(definitions));
            }

            if (!definitionsById.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException(
                    $"The statistic field definition snapshot contains duplicate stable ID '{definition.Id}'.",
                    nameof(definitions));
            }
        }

        return definitionsById;
    }

    private static HashSet<string> CreateManagedFindingIds(
        IEnumerable<QaCheckDefinition> checklistDefinitions,
        IEnumerable<QaStatisticFieldDefinition> statisticFieldDefinitions)
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
            if (!ids.Add(QaFindingIds.FailureForCheck(definition.Id))
                || !ids.Add(QaFindingIds.WarningForCheck(definition.Id)))
            {
                throw new ArgumentException(
                    $"Checklist ID '{definition.Id}' collides with a deterministic finding ID.",
                    nameof(checklistDefinitions));
            }
        }

        foreach (QaStatisticFieldDefinition definition in statisticFieldDefinitions)
        {
            if (definition.SupportsBlankStatistics
                && (!ids.Add(
                        QaFindingIds.WarningForBlankStatistic(definition.Id))
                    || !ids.Add(
                        QaFindingIds.FailureForBlankStatistic(definition.Id))))
            {
                throw new ArgumentException(
                    $"Statistic field ID '{definition.Id}' collides with a deterministic blank-statistic finding ID.",
                    nameof(statisticFieldDefinitions));
            }

            if (definition.SupportsBrokenDataStatistics
                && (!ids.Add(
                        QaFindingIds.WarningForBrokenStatistic(definition.Id))
                    || !ids.Add(
                        QaFindingIds.FailureForBrokenStatistic(definition.Id))))
            {
                throw new ArgumentException(
                    $"Statistic field ID '{definition.Id}' collides with a deterministic broken-data finding ID.",
                    nameof(statisticFieldDefinitions));
            }
        }

        return ids;
    }

    private Dictionary<string, QaCheckResult> ValidateAndIndexResults(
        QaReport report)
    {
        Dictionary<string, QaCheckResult> resultsById =
            new(StringComparer.Ordinal);

        foreach (QaCheckResult? result in report.ChecklistResults)
        {
            if (result is null)
            {
                throw new InvalidOperationException(
                    "The QA report checklist result collection cannot contain null entries.");
            }

            if (string.IsNullOrWhiteSpace(result.CheckId))
            {
                throw new InvalidOperationException(
                    "Every QA checklist result must have a nonblank stable ID.");
            }

            if (!checklistDefinitionsById.ContainsKey(result.CheckId))
            {
                throw new InvalidOperationException(
                    $"QA checklist result '{result.CheckId}' has no matching definition.");
            }

            if (!Enum.IsDefined(typeof(QaCheckStatus), result.Status))
            {
                throw new InvalidOperationException(
                    $"QA checklist result '{result.CheckId}' has an unknown status value.");
            }

            if (!resultsById.TryAdd(result.CheckId, result))
            {
                throw new InvalidOperationException(
                    $"The QA report contains duplicate checklist result ID '{result.CheckId}'.");
            }
        }

        if (resultsById.Count != checklistDefinitions.Count
            || checklistDefinitions.Any(
                definition => !resultsById.ContainsKey(definition.Id)))
        {
            throw new InvalidOperationException(
                "The QA report must contain exactly one checklist result for every definition in the finding service snapshot.");
        }

        return resultsById;
    }

    private static Dictionary<string, QaFinding> ValidateAndIndexFindings(
        IEnumerable<QaFinding> findings)
    {
        Dictionary<string, QaFinding> findingsById =
            new(StringComparer.Ordinal);

        foreach (QaFinding? finding in findings)
        {
            if (finding is null)
            {
                throw new InvalidOperationException(
                    "The QA report findings collection cannot contain null entries.");
            }

            if (string.IsNullOrWhiteSpace(finding.FindingId))
            {
                throw new InvalidOperationException(
                    "Every QA finding must have a nonblank deterministic ID.");
            }

            if (!findingsById.TryAdd(finding.FindingId, finding))
            {
                throw new InvalidOperationException(
                    $"The QA report contains duplicate finding ID '{finding.FindingId}'.");
            }
        }

        return findingsById;
    }

    private static QaStatistics GetStatistics(QaReport report)
    {
        QaStatistics statistics = report.Statistics
            ?? throw new InvalidOperationException(
                "The QA report must have statistics before findings can be synchronized.");

        _ = statistics.FileInformation
            ?? throw new InvalidOperationException(
                "The QA report statistics must have file information.");
        _ = statistics.UnusualAverageRateValues
            ?? throw new InvalidOperationException(
                "The QA report statistics must have unusual Average Rate values.");
        _ = statistics.UnusualStayValues
            ?? throw new InvalidOperationException(
                "The QA report statistics must have unusual Stay Value values.");
        _ = statistics.HighStayValues
            ?? throw new InvalidOperationException(
                "The QA report statistics must have high Stay Value values.");
        _ = statistics.Database
            ?? throw new InvalidOperationException(
                "The QA report statistics must have database statistics.");

        return statistics;
    }

    private static QaFileCharacteristics GetFileCharacteristics(QaReport report)
    {
        return report.FileCharacteristics
            ?? throw new InvalidOperationException(
                "The QA report must have file characteristics before findings can be synchronized.");
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
