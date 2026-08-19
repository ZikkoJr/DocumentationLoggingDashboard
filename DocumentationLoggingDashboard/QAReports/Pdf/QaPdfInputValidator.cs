using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.QAReports.Pdf;

internal static class QaPdfInputValidator
{
    public static void Validate(
        QaReport report,
        QaReportValidationResult validationResult)
    {
        if (!validationResult.IsReady
            || validationResult.BlockingErrors.Count != 0
            || validationResult.CalculatedStatus is null)
        {
            throw new QaPdfGenerationException(
                "The QA report is not ready for PDF generation.");
        }

        if (report.ReportStatus is null)
        {
            throw new QaPdfGenerationException(
                "The QA report status is stale or unset.");
        }

        if (report.ReportStatus != validationResult.CalculatedStatus)
        {
            throw new QaPdfGenerationException(
                "The QA report status does not match the supplied readiness result.");
        }

        if (string.IsNullOrWhiteSpace(validationResult.EffectiveCreatedBy))
        {
            throw new QaPdfGenerationException(
                "The effective Created By value is unavailable.");
        }

        if (report.SchemaVersion != QaReport.CurrentSchemaVersion)
        {
            throw new QaPdfGenerationException(
                "The QA report schema version is not current.");
        }

        if (string.IsNullOrWhiteSpace(report.FileId))
        {
            throw new QaPdfGenerationException(
                "A File ID is required for PDF generation.");
        }

        if (string.IsNullOrWhiteSpace(
                validationResult.ValidatedReportFingerprint))
        {
            throw new QaPdfGenerationException(
                "The supplied readiness result does not contain validated-report evidence.");
        }

        ValidateEnums(report);
        ValidateChecklist(report);
        ValidateFindings(report);
        ValidateStatistics(report);

        string currentFingerprint = QaReportReadinessFingerprint.Compute(report);
        if (!string.Equals(
                currentFingerprint,
                validationResult.ValidatedReportFingerprint,
                StringComparison.Ordinal))
        {
            throw new QaPdfGenerationException(
                "The supplied readiness result no longer matches the report.");
        }
    }

    private static void ValidateEnums(QaReport report)
    {
        QaFileCharacteristics characteristics = report.FileCharacteristics
            ?? throw new QaPdfGenerationException(
                "The QA report has no file-characteristics data.");

        if (!Enum.IsDefined(
                typeof(QaNameColumnMode),
                characteristics.NameColumnMode)
            || !Enum.IsDefined(
                typeof(QaMonetaryColumnScenario),
                characteristics.MonetaryColumnScenario))
        {
            throw new QaPdfGenerationException(
                "The QA report contains an invalid file-characteristics value.");
        }
    }

    private static void ValidateChecklist(QaReport report)
    {
        Dictionary<string, QaCheckResult> resultsById =
            new(StringComparer.Ordinal);

        foreach (QaCheckResult? result in report.ChecklistResults)
        {
            if (result is null || string.IsNullOrWhiteSpace(result.CheckId))
            {
                throw new QaPdfGenerationException(
                    "Every checklist result requires a nonblank stable ID.");
            }

            if (!resultsById.TryAdd(result.CheckId, result))
            {
                throw new QaPdfGenerationException(
                    $"Checklist result '{result.CheckId}' is duplicated.");
            }

            if (!QaChecklistCatalog.Definitions.Any(
                    definition => string.Equals(
                        definition.Id,
                        result.CheckId,
                        StringComparison.Ordinal)))
            {
                throw new QaPdfGenerationException(
                    $"Checklist result '{result.CheckId}' is not in the approved catalog.");
            }

            if (!Enum.IsDefined(typeof(QaCheckStatus), result.Status)
                || !Enum.IsDefined(typeof(QaResultSource), result.ResultSource))
            {
                throw new QaPdfGenerationException(
                    $"Checklist result '{result.CheckId}' contains an invalid enum value.");
            }

            if (result.Status == QaCheckStatus.NotEvaluated)
            {
                throw new QaPdfGenerationException(
                    $"Checklist result '{result.CheckId}' is Not Evaluated.");
            }
        }

        QaFileCharacteristics characteristics = report.FileCharacteristics!;

        foreach (QaCheckDefinition definition in QaChecklistCatalog.Definitions)
        {
            if (!resultsById.TryGetValue(definition.Id, out QaCheckResult? result))
            {
                throw new QaPdfGenerationException(
                    $"Checklist definition '{definition.Id}' has no result.");
            }

            bool applies = IsApplicable(definition, characteristics);

            if (applies
                && result.Status is not QaCheckStatus.Pass
                    and not QaCheckStatus.Fail)
            {
                throw new QaPdfGenerationException(
                    $"Checklist result '{definition.Id}' must be Pass or Fail.");
            }

            if (!applies && result.Status != QaCheckStatus.NotApplicable)
            {
                throw new QaPdfGenerationException(
                    $"Checklist result '{definition.Id}' must be Not Applicable.");
            }
        }

        if (resultsById.Count != QaChecklistCatalog.Definitions.Count)
        {
            throw new QaPdfGenerationException(
                "The QA report does not contain exactly one result for every checklist definition.");
        }
    }

    private static bool IsApplicable(
        QaCheckDefinition definition,
        QaFileCharacteristics characteristics)
    {
        return definition.Applicability switch
        {
            QaCheckApplicability.Always => true,
            QaCheckApplicability.SeparateNameColumns =>
                characteristics.NameColumnMode ==
                    QaNameColumnMode.SeparateFirstAndLastName,
            QaCheckApplicability.FullNameColumn =>
                characteristics.NameColumnMode == QaNameColumnMode.FullName,
            QaCheckApplicability.CurrencyColumnPresent =>
                characteristics.HasCurrencyColumn,
            QaCheckApplicability.OneMonetaryColumn =>
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.OneMonetaryColumn,
            QaCheckApplicability.TwoMonetaryColumns =>
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.TwoMonetaryColumns,
            QaCheckApplicability.MoreThanTwoMonetaryColumns =>
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns,
            QaCheckApplicability.RejectedRecordsPresent =>
                characteristics.HasRejectedDatabaseRecords,
            _ => throw new QaPdfGenerationException(
                $"Checklist definition '{definition.Id}' has invalid applicability metadata.")
        };
    }

    private static void ValidateFindings(QaReport report)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (QaFinding? finding in report.Findings)
        {
            if (finding is null || string.IsNullOrWhiteSpace(finding.FindingId))
            {
                throw new QaPdfGenerationException(
                    "Every finding requires a nonblank deterministic ID.");
            }

            if (!ids.Add(finding.FindingId))
            {
                throw new QaPdfGenerationException(
                    $"Finding '{finding.FindingId}' is duplicated.");
            }

            if (!Enum.IsDefined(typeof(QaFindingSeverity), finding.Severity)
                || !Enum.IsDefined(typeof(QaFindingResolution), finding.Resolution)
                || !Enum.IsDefined(typeof(QaFindingSource), finding.Source))
            {
                throw new QaPdfGenerationException(
                    $"Finding '{finding.FindingId}' contains an invalid enum value.");
            }
        }
    }

    private static void ValidateStatistics(QaReport report)
    {
        QaStatistics statistics = report.Statistics
            ?? throw new QaPdfGenerationException(
                "Required QA statistics are unavailable.");

        _ = statistics.FileInformation
            ?? throw new QaPdfGenerationException(
                "File Information statistics are unavailable.");
        _ = statistics.MultiwordNames
            ?? throw new QaPdfGenerationException(
                "Name statistics are unavailable.");
        _ = statistics.FileMonth
            ?? throw new QaPdfGenerationException(
                "File Month statistics are unavailable.");
        _ = statistics.UnusualAverageRateValues
            ?? throw new QaPdfGenerationException(
                "Average Rate statistics are unavailable.");
        _ = statistics.UnusualStayValues
            ?? throw new QaPdfGenerationException(
                "Stay Value statistics are unavailable.");
        _ = statistics.HighStayValues
            ?? throw new QaPdfGenerationException(
                "High Stay Value statistics are unavailable.");
        _ = statistics.Database
            ?? throw new QaPdfGenerationException(
                "Database statistics are unavailable.");

        ValidateStatisticRows(
            statistics.BlankValues,
            statistic => statistic?.FieldId,
            "blank-value");
        ValidateStatisticRows(
            statistics.BrokenData,
            statistic => statistic?.FieldId,
            "broken-data");

        HashSet<string> applicableIds = QaStatisticFieldCatalog.Definitions
            .Where(definition => QaStatisticFieldCatalog.IsApplicable(
                definition,
                report.FileCharacteristics!))
            .Select(definition => definition.Id)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> blankIds = statistics.BlankValues
            .Select(statistic => statistic.FieldId)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> brokenIds = statistics.BrokenData
            .Select(statistic => statistic.FieldId)
            .ToHashSet(StringComparer.Ordinal);

        if (!blankIds.SetEquals(applicableIds)
            || !brokenIds.SetEquals(applicableIds))
        {
            throw new QaPdfGenerationException(
                "Required statistics are structurally inconsistent with the approved field catalog.");
        }
    }

    private static void ValidateStatisticRows<T>(
        IEnumerable<T> statistics,
        Func<T?, string?> idSelector,
        string kind)
        where T : class
    {
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (T? statistic in statistics)
        {
            string? id = idSelector(statistic);
            if (statistic is null || string.IsNullOrWhiteSpace(id))
            {
                throw new QaPdfGenerationException(
                    $"Every {kind} statistic requires a nonblank stable field ID.");
            }

            try
            {
                _ = QaStatisticFieldCatalog.GetRequired(id);
            }
            catch (ArgumentException exception)
            {
                throw new QaPdfGenerationException(
                    $"The {kind} statistic '{id}' is not in the approved field catalog.",
                    exception);
            }

            if (!ids.Add(id))
            {
                throw new QaPdfGenerationException(
                    $"The {kind} statistic '{id}' is duplicated.");
            }
        }
    }
}
