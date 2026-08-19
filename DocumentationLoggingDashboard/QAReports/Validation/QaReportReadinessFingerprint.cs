using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Validation;

/// <summary>
/// Produces deterministic evidence for the mutable report state that was validated.
/// ReportStatus is deliberately excluded because the form assigns it after validation.
/// </summary>
internal static class QaReportReadinessFingerprint
{
    private const string FingerprintVersion = "QA-REPORT-READINESS-V2";

    public static string Compute(QaReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        StringBuilder value = new();
        AppendString(value, "fingerprintVersion", FingerprintVersion);
        AppendInt(value, "schemaVersion", report.SchemaVersion);
        AppendString(value, "reportId", report.ReportId);
        AppendString(value, "fileId", report.FileId);

        QaHotelInformation? hotel = report.HotelInformation;
        AppendPresence(value, "hotelInformation", hotel is not null);
        if (hotel is not null)
        {
            AppendString(value, "hotelName", hotel.HotelName);
            AppendString(value, "hotelId", hotel.HotelId);
            AppendString(value, "pmsName", hotel.PmsName);
            AppendPresence(value, "fileMonth", hotel.FileMonth is not null);
            if (hotel.FileMonth is not null)
            {
                AppendInt(value, "fileMonthYear", hotel.FileMonth.Year);
                AppendInt(value, "fileMonthMonth", hotel.FileMonth.Month);
            }
        }

        AppendString(
            value,
            "qaDate",
            report.QaDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendString(value, "createdBy", report.CreatedBy);
        AppendString(value, "originalFileName", report.OriginalFileName);
        AppendString(value, "generalNotes", report.GeneralNotes);

        QaFileCharacteristics? characteristics = report.FileCharacteristics;
        AppendPresence(value, "fileCharacteristics", characteristics is not null);
        if (characteristics is not null)
        {
            AppendEnum(value, "nameColumnMode", characteristics.NameColumnMode);
            AppendBool(value, "hasCurrencyColumn", characteristics.HasCurrencyColumn);
            AppendEnum(
                value,
                "monetaryColumnScenario",
                characteristics.MonetaryColumnScenario);
            AppendBool(
                value,
                "hasMultipleConfirmationCandidates",
                characteristics.HasMultipleConfirmationNumberCandidateColumns);
            AppendBool(
                value,
                "customScriptSupportAvailable",
                characteristics.IsCustomScriptSupportAvailable);
            AppendBool(
                value,
                "hasRejectedDatabaseRecords",
                characteristics.HasRejectedDatabaseRecords);
        }

        AppendCatalog(value, "checklistCatalog", QaChecklistCatalog.Definitions.Select(
            definition => definition.Id));
        AppendInt(value, "checklistResultCount", report.ChecklistResults.Count);
        foreach (QaCheckResult? result in OrderChecklistResults(report.ChecklistResults))
        {
            AppendPresence(value, "checklistResult", result is not null);
            if (result is null)
            {
                continue;
            }

            AppendString(value, "checkId", result.CheckId);
            AppendEnum(value, "checkStatus", result.Status);
            AppendString(value, "checkNotes", result.Notes);
            AppendEnum(value, "resultSource", result.ResultSource);
            AppendString(
                value,
                "evaluatedAt",
                result.EvaluatedAt?.ToString("O", CultureInfo.InvariantCulture));
        }

        AppendStatistics(value, report.Statistics);

        AppendInt(value, "findingCount", report.Findings.Count);
        foreach (QaFinding? finding in report.Findings
                     .Select((item, index) => new { Item = item, Index = index })
                     .OrderBy(
                         entry => entry.Item?.FindingId,
                         StringComparer.Ordinal)
                     .ThenBy(entry => entry.Index)
                     .Select(entry => entry.Item))
        {
            AppendPresence(value, "finding", finding is not null);
            if (finding is null)
            {
                continue;
            }

            AppendString(value, "findingId", finding.FindingId);
            AppendString(value, "relatedCheckId", finding.RelatedCheckId);
            AppendEnum(value, "findingSeverity", finding.Severity);
            AppendString(value, "findingTitle", finding.Title);
            AppendString(value, "findingDescription", finding.Description);
            AppendEnum(value, "findingResolution", finding.Resolution);
            AppendString(value, "resolutionNotes", finding.ResolutionNotes);
            AppendString(value, "customScriptName", finding.CustomScriptName);
            AppendEnum(value, "findingSource", finding.Source);
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value.ToString()));
        return Convert.ToHexString(digest);
    }

    private static IEnumerable<QaCheckResult?> OrderChecklistResults(
        IReadOnlyList<QaCheckResult> results)
    {
        Dictionary<string, int> catalogOrder = QaChecklistCatalog.Definitions
            .Select((definition, index) => new { definition.Id, Index = index })
            .ToDictionary(entry => entry.Id, entry => entry.Index, StringComparer.Ordinal);

        return results
            .Select((item, index) => new { Item = item, Index = index })
            .OrderBy(
                entry => entry.Item is not null
                    && catalogOrder.TryGetValue(entry.Item.CheckId, out int order)
                        ? order
                        : int.MaxValue)
            .ThenBy(entry => entry.Item?.CheckId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Index)
            .Select(entry => entry.Item);
    }

    private static void AppendStatistics(
        StringBuilder value,
        QaStatistics? statistics)
    {
        AppendPresence(value, "statistics", statistics is not null);
        if (statistics is null)
        {
            return;
        }

        QaFileInformationStatistics? file = statistics.FileInformation;
        AppendPresence(value, "fileInformation", file is not null);
        if (file is not null)
        {
            AppendInt(value, "totalDataRows", file.TotalDataRows);
            AppendBool(value, "headersPresent", file.HeadersArePresent);
            AppendEnum(value, "usefulHeaders", file.UsefulHeaders);
            AppendInt(value, "dataStartRow", file.DataStartRow);
        }

        AppendCatalog(value, "statisticCatalog", QaStatisticFieldCatalog.Definitions.Select(
            definition => definition.Id));
        AppendInt(value, "blankStatisticCount", statistics.BlankValues.Count);
        foreach (QaBlankValueStatistic? statistic in OrderStatistics(
                     statistics.BlankValues,
                     item => item?.FieldId))
        {
            AppendPresence(value, "blankStatistic", statistic is not null);
            if (statistic is null)
            {
                continue;
            }

            AppendString(value, "blankFieldId", statistic.FieldId);
            AppendString(value, "blankDisplayName", statistic.DisplayName);
            AppendInt(value, "blankCount", statistic.BlankCount);
            AppendInt(value, "blankRows", statistic.TotalApplicableRows);
            AppendBool(
                value,
                "blankRowsAutomatic",
                statistic.UseAutomaticTotalApplicableRows);
            AppendDecimal(value, "blankPercentage", statistic.BlankPercentage);
        }

        AppendInt(value, "brokenStatisticCount", statistics.BrokenData.Count);
        foreach (QaBrokenDataStatistic? statistic in OrderStatistics(
                     statistics.BrokenData,
                     item => item?.FieldId))
        {
            AppendPresence(value, "brokenStatistic", statistic is not null);
            if (statistic is null)
            {
                continue;
            }

            AppendString(value, "brokenFieldId", statistic.FieldId);
            AppendString(value, "brokenDisplayName", statistic.DisplayName);
            AppendInt(value, "brokenCount", statistic.BrokenValueCount);
            AppendInt(
                value,
                "brokenNonblankRows",
                statistic.TotalApplicableNonblankValues);
            AppendBool(
                value,
                "brokenNonblankRowsAutomatic",
                statistic.UseAutomaticTotalApplicableNonblankValues);
            AppendDecimal(
                value,
                "brokenPercentage",
                statistic.BrokenDataPercentage);
            AppendString(value, "brokenExplanation", statistic.Explanation);
        }

        QaMultiwordNameStatistics? names = statistics.MultiwordNames;
        AppendPresence(value, "multiwordNames", names is not null);
        if (names is not null)
        {
            AppendInt(value, "multiwordFirstCount", names.MultiwordFirstNameCount);
            AppendDecimal(
                value,
                "multiwordFirstPercentage",
                names.MultiwordFirstNamePercentage);
            AppendInt(value, "multiwordLastCount", names.MultiwordLastNameCount);
            AppendDecimal(
                value,
                "multiwordLastPercentage",
                names.MultiwordLastNamePercentage);
        }

        QaFileMonthStatistics? month = statistics.FileMonth;
        AppendPresence(value, "fileMonthStatistics", month is not null);
        if (month is not null)
        {
            AppendInt(value, "arrivalWithinCount", month.ArrivalDatesWithinFileMonth);
            AppendInt(value, "arrivalOutsideCount", month.ArrivalDatesOutsideFileMonth);
            AppendDecimal(value, "arrivalWithinPercentage", month.PercentageWithinFileMonth);
            AppendDecimal(value, "arrivalOutsidePercentage", month.PercentageOutsideFileMonth);
            AppendInt(value, "validArrivalCount", month.ValidArrivalDateCount);
        }

        AppendUnusualMonetary(
            value,
            "unusualAverageRate",
            statistics.UnusualAverageRateValues);
        AppendUnusualMonetary(
            value,
            "unusualStayValue",
            statistics.UnusualStayValues);

        QaHighStayValueStatistics? high = statistics.HighStayValues;
        AppendPresence(value, "highStayValues", high is not null);
        if (high is not null)
        {
            AppendInt(
                value,
                "highStayValueCount",
                high.StayValuesAboveTenThousandCount);
            AppendDecimal(
                value,
                "highStayValuePercentage",
                high.StayValuesAboveTenThousandPercentage);
            AppendBool(value, "highStayValuesExpected", high.AreHighValuesExpected);
            AppendString(value, "highStayValueExplanation", high.Explanation);
        }

        QaDatabaseStatistics? database = statistics.Database;
        AppendPresence(value, "databaseStatistics", database is not null);
        if (database is not null)
        {
            AppendInt(value, "importedRecordCount", database.ImportedRecordCount);
            AppendInt(value, "rejectedRecordCount", database.RejectedRecordCount);
            AppendInt(
                value,
                "missingRequiredDbValueCount",
                database.RecordsWithMissingRequiredDatabaseValues);
            AppendInt(
                value,
                "rawMinusImportedDifference",
                database.RawMinusImportedRecordCountDifference);
        }
    }

    private static IEnumerable<T?> OrderStatistics<T>(
        IReadOnlyList<T> statistics,
        Func<T?, string?> idSelector)
        where T : class
    {
        Dictionary<string, int> catalogOrder = QaStatisticFieldCatalog.Definitions
            .Select((definition, index) => new { definition.Id, Index = index })
            .ToDictionary(entry => entry.Id, entry => entry.Index, StringComparer.Ordinal);

        return statistics
            .Select((item, index) => new { Item = item, Index = index })
            .OrderBy(entry =>
            {
                string? id = idSelector(entry.Item);
                return id is not null
                    && catalogOrder.TryGetValue(id, out int order)
                        ? order
                        : int.MaxValue;
            })
            .ThenBy(entry => idSelector(entry.Item), StringComparer.Ordinal)
            .ThenBy(entry => entry.Index)
            .Select(entry => entry.Item);
    }

    private static void AppendUnusualMonetary(
        StringBuilder value,
        string name,
        QaUnusualMonetaryValueStatistics? statistic)
    {
        AppendPresence(value, name, statistic is not null);
        if (statistic is null)
        {
            return;
        }

        AppendBool(value, name + "Found", statistic.HasUnusualValues);
        AppendInt(value, name + "Count", statistic.UnusualValueCount);
        AppendDecimal(value, name + "Percentage", statistic.UnusualValuePercentage);
        AppendString(value, name + "Explanation", statistic.Explanation);
    }

    private static void AppendCatalog(
        StringBuilder value,
        string name,
        IEnumerable<string> ids)
    {
        string[] snapshot = ids.ToArray();
        AppendInt(value, name + "Count", snapshot.Length);
        foreach (string id in snapshot)
        {
            AppendString(value, name + "Id", id);
        }
    }

    private static void AppendPresence(
        StringBuilder value,
        string name,
        bool isPresent)
    {
        AppendBool(value, name + "Present", isPresent);
    }

    private static void AppendBool(StringBuilder value, string name, bool item)
    {
        AppendScalar(value, name, item ? "1" : "0");
    }

    private static void AppendInt(StringBuilder value, string name, int item)
    {
        AppendScalar(value, name, item.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendDecimal(
        StringBuilder value,
        string name,
        decimal item)
    {
        AppendScalar(value, name, item.ToString("G29", CultureInfo.InvariantCulture));
    }

    private static void AppendEnum<T>(StringBuilder value, string name, T item)
        where T : struct, Enum
    {
        AppendScalar(
            value,
            name,
            Convert.ToInt64(item, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendString(
        StringBuilder value,
        string name,
        string? item)
    {
        AppendScalar(value, name, item);
    }

    private static void AppendScalar(
        StringBuilder value,
        string name,
        string? item)
    {
        value.Append(name.Length.ToString(CultureInfo.InvariantCulture));
        value.Append(':');
        value.Append(name);
        value.Append('=');

        if (item is null)
        {
            value.Append("-1:");
        }
        else
        {
            value.Append(item.Length.ToString(CultureInfo.InvariantCulture));
            value.Append(':');
            value.Append(item);
        }

        value.Append(';');
    }
}
