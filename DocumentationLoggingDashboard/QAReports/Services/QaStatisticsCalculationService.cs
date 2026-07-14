using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Applies statistics-field applicability and recalculates derived report values.
/// </summary>
public sealed class QaStatisticsCalculationService
{
    /// <summary>
    /// Reconciles the report's statistics rows in catalog order and recalculates all
    /// values that are derived from counts. Existing applicable row objects are reused.
    /// </summary>
    public void Synchronize(QaReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        QaStatistics statistics = report.Statistics
            ?? throw new InvalidOperationException(
                "The QA report must have a statistics object.");
        QaFileCharacteristics characteristics = report.FileCharacteristics
            ?? throw new InvalidOperationException(
                "The QA report must have file characteristics before statistics can be synchronized.");

        Dictionary<string, QaBlankValueStatistic> blankById =
            IndexBlankStatistics(statistics.BlankValues);
        Dictionary<string, QaBrokenDataStatistic> brokenById =
            IndexBrokenStatistics(statistics.BrokenData);

        List<QaBlankValueStatistic> synchronizedBlank = [];

        foreach (QaStatisticFieldDefinition definition
                 in QaStatisticFieldCatalog.Definitions)
        {
            if (!definition.SupportsBlankStatistics
                || !QaStatisticFieldCatalog.IsApplicable(
                    definition,
                    characteristics))
            {
                continue;
            }

            if (!blankById.TryGetValue(
                    definition.Id,
                    out QaBlankValueStatistic? statistic))
            {
                statistic = new QaBlankValueStatistic();
            }

            statistic.FieldId = definition.Id;
            statistic.DisplayName = definition.DisplayName;
            statistic.BlankPercentage = CalculatePercentage(
                statistic.BlankCount,
                statistic.TotalApplicableRows);
            synchronizedBlank.Add(statistic);
        }

        statistics.BlankValues.Clear();
        statistics.BlankValues.AddRange(synchronizedBlank);

        blankById = synchronizedBlank.ToDictionary(
            statistic => statistic.FieldId,
            StringComparer.Ordinal);

        List<QaBrokenDataStatistic> synchronizedBroken = [];

        foreach (QaStatisticFieldDefinition definition
                 in QaStatisticFieldCatalog.Definitions)
        {
            if (!definition.SupportsBrokenDataStatistics
                || !QaStatisticFieldCatalog.IsApplicable(
                    definition,
                    characteristics))
            {
                continue;
            }

            if (!brokenById.TryGetValue(
                    definition.Id,
                    out QaBrokenDataStatistic? statistic))
            {
                statistic = new QaBrokenDataStatistic();
            }

            QaBlankValueStatistic blankStatistic = blankById[definition.Id];
            int derivedNonblankCount =
                CalculateDerivedNonblankCount(blankStatistic);

            statistic.FieldId = definition.Id;
            statistic.DisplayName = definition.DisplayName;
            statistic.TotalApplicableNonblankValues =
                Math.Max(0, derivedNonblankCount);
            statistic.BrokenDataPercentage = CalculatePercentage(
                statistic.BrokenValueCount,
                statistic.TotalApplicableNonblankValues);
            statistic.Explanation = TrimToNull(statistic.Explanation);
            synchronizedBroken.Add(statistic);
        }

        statistics.BrokenData.Clear();
        statistics.BrokenData.AddRange(synchronizedBroken);

        RecalculateMultiwordNames(statistics, blankById, characteristics);
        RecalculateFileMonth(statistics.FileMonth);
        RecalculateMonetaryStatistics(statistics, blankById, characteristics);

        QaDatabaseStatistics database = statistics.Database
            ?? throw new InvalidOperationException(
                "The QA report must have database statistics.");
        QaFileInformationStatistics fileInformation = statistics.FileInformation
            ?? throw new InvalidOperationException(
                "The QA report must have File Information statistics.");

        database.RawMinusImportedRecordCountDifference =
            CalculateRawMinusImportedDifference(
                fileInformation.TotalDataRows,
                database.ImportedRecordCount);
    }

    public static decimal CalculatePercentage(int numerator, int denominator)
    {
        return denominator <= 0
            ? 0m
            : decimal.Round(numerator * 100m / denominator, 2);
    }

    /// <summary>
    /// Returns the signed populated-value count so callers can detect inconsistent
    /// blank statistics instead of silently converting them into valid input.
    /// </summary>
    public static int CalculateDerivedNonblankCount(
        QaBlankValueStatistic statistic)
    {
        ArgumentNullException.ThrowIfNull(statistic);

        return checked(
            statistic.TotalApplicableRows - statistic.BlankCount);
    }

    public static int CalculateRawMinusImportedDifference(
        int totalDataRows,
        int importedRecordCount)
    {
        long difference = (long)totalDataRows - importedRecordCount;

        if (difference is < int.MinValue or > int.MaxValue)
        {
            throw new OverflowException(
                "The raw-minus-imported record difference is outside the supported Int32 range.");
        }

        return (int)difference;
    }

    public static long GetAbsoluteDifference(int signedDifference)
    {
        return Math.Abs((long)signedDifference);
    }

    private static Dictionary<string, QaBlankValueStatistic>
        IndexBlankStatistics(IEnumerable<QaBlankValueStatistic> statistics)
    {
        Dictionary<string, QaBlankValueStatistic> byId =
            new(StringComparer.Ordinal);

        foreach (QaBlankValueStatistic? statistic in statistics)
        {
            if (statistic is null)
            {
                throw new InvalidOperationException(
                    "Blank-value statistics cannot contain null entries.");
            }

            QaStatisticFieldDefinition definition =
                GetDefinitionForRow(statistic.FieldId, "blank-value");

            if (!definition.SupportsBlankStatistics)
            {
                throw new InvalidOperationException(
                    $"Statistics field '{definition.Id}' does not support blank-value statistics.");
            }

            if (!byId.TryAdd(definition.Id, statistic))
            {
                throw new InvalidOperationException(
                    $"The report contains duplicate blank-value statistics ID '{definition.Id}'.");
            }
        }

        return byId;
    }

    private static Dictionary<string, QaBrokenDataStatistic>
        IndexBrokenStatistics(IEnumerable<QaBrokenDataStatistic> statistics)
    {
        Dictionary<string, QaBrokenDataStatistic> byId =
            new(StringComparer.Ordinal);

        foreach (QaBrokenDataStatistic? statistic in statistics)
        {
            if (statistic is null)
            {
                throw new InvalidOperationException(
                    "Broken-data statistics cannot contain null entries.");
            }

            QaStatisticFieldDefinition definition =
                GetDefinitionForRow(statistic.FieldId, "broken-data");

            if (!definition.SupportsBrokenDataStatistics)
            {
                throw new InvalidOperationException(
                    $"Statistics field '{definition.Id}' does not support broken-data statistics.");
            }

            if (!byId.TryAdd(definition.Id, statistic))
            {
                throw new InvalidOperationException(
                    $"The report contains duplicate broken-data statistics ID '{definition.Id}'.");
            }
        }

        return byId;
    }

    private static QaStatisticFieldDefinition GetDefinitionForRow(
        string fieldId,
        string statisticKind)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            throw new InvalidOperationException(
                $"Every {statisticKind} statistic requires a nonblank stable field ID.");
        }

        try
        {
            return QaStatisticFieldCatalog.GetRequired(fieldId);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"The report contains unknown {statisticKind} statistics ID '{fieldId}'.",
                exception);
        }
    }

    private static void RecalculateMultiwordNames(
        QaStatistics statistics,
        IReadOnlyDictionary<string, QaBlankValueStatistic> blankById,
        QaFileCharacteristics characteristics)
    {
        QaMultiwordNameStatistics multiword = statistics.MultiwordNames
            ?? throw new InvalidOperationException(
                "The QA report must have multiword-name statistics.");

        if (characteristics.NameColumnMode !=
            QaNameColumnMode.SeparateFirstAndLastName)
        {
            multiword.MultiwordFirstNameCount = 0;
            multiword.MultiwordFirstNamePercentage = 0m;
            multiword.MultiwordLastNameCount = 0;
            multiword.MultiwordLastNamePercentage = 0m;
            return;
        }

        int firstNameDenominator = CalculateDerivedNonblankCount(
            blankById[QaStatisticFieldIds.FirstName]);
        int lastNameDenominator = CalculateDerivedNonblankCount(
            blankById[QaStatisticFieldIds.LastName]);

        multiword.MultiwordFirstNamePercentage = CalculatePercentage(
            multiword.MultiwordFirstNameCount,
            firstNameDenominator);
        multiword.MultiwordLastNamePercentage = CalculatePercentage(
            multiword.MultiwordLastNameCount,
            lastNameDenominator);
    }

    private static void RecalculateFileMonth(
        QaFileMonthStatistics fileMonth)
    {
        ArgumentNullException.ThrowIfNull(fileMonth);

        fileMonth.PercentageWithinFileMonth = CalculatePercentage(
            fileMonth.ArrivalDatesWithinFileMonth,
            fileMonth.ValidArrivalDateCount);
        fileMonth.PercentageOutsideFileMonth = CalculatePercentage(
            fileMonth.ArrivalDatesOutsideFileMonth,
            fileMonth.ValidArrivalDateCount);
    }

    private static void RecalculateMonetaryStatistics(
        QaStatistics statistics,
        IReadOnlyDictionary<string, QaBlankValueStatistic> blankById,
        QaFileCharacteristics characteristics)
    {
        QaUnusualMonetaryValueStatistics averageRate =
            statistics.UnusualAverageRateValues
            ?? throw new InvalidOperationException(
                "The QA report must have unusual Average Rate statistics.");
        int averageRateDenominator = CalculateDerivedNonblankCount(
            blankById[QaStatisticFieldIds.AverageRate]);
        RecalculateUnusualMonetary(averageRate, averageRateDenominator);

        bool stayValueApplies = QaStatisticFieldCatalog.IsApplicable(
            QaStatisticFieldCatalog.GetRequired(QaStatisticFieldIds.StayValue),
            characteristics);
        QaUnusualMonetaryValueStatistics stayValue =
            statistics.UnusualStayValues
            ?? throw new InvalidOperationException(
                "The QA report must have unusual Stay Value statistics.");
        QaHighStayValueStatistics highStay = statistics.HighStayValues
            ?? throw new InvalidOperationException(
                "The QA report must have high Stay Value statistics.");

        if (!stayValueApplies)
        {
            ResetUnusualMonetary(stayValue);
            highStay.StayValuesAboveTenThousandCount = 0;
            highStay.StayValuesAboveTenThousandPercentage = 0m;
            highStay.AreHighValuesExpected = false;
            highStay.Explanation = null;
            return;
        }

        int stayValueDenominator = CalculateDerivedNonblankCount(
            blankById[QaStatisticFieldIds.StayValue]);
        RecalculateUnusualMonetary(stayValue, stayValueDenominator);
        highStay.StayValuesAboveTenThousandPercentage = CalculatePercentage(
            highStay.StayValuesAboveTenThousandCount,
            stayValueDenominator);
        highStay.Explanation = TrimToNull(highStay.Explanation);
    }

    private static void RecalculateUnusualMonetary(
        QaUnusualMonetaryValueStatistics statistic,
        int denominator)
    {
        if (!statistic.HasUnusualValues)
        {
            ResetUnusualMonetary(statistic);
            return;
        }

        statistic.UnusualValuePercentage = CalculatePercentage(
            statistic.UnusualValueCount,
            denominator);
        statistic.Explanation = TrimToNull(statistic.Explanation);
    }

    private static void ResetUnusualMonetary(
        QaUnusualMonetaryValueStatistics statistic)
    {
        statistic.HasUnusualValues = false;
        statistic.UnusualValueCount = 0;
        statistic.UnusualValuePercentage = 0m;
        statistic.Explanation = null;
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
