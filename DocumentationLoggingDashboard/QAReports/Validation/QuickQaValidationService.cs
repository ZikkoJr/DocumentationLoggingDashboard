using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.QAReports.Validation;

/// <summary>
/// Validates one completed Quick QA draft without importing Detailed QA
/// statistics, readiness, PDF, or overwrite rules.
/// </summary>
public sealed class QuickQaValidationService
{
    private readonly QaReportStatusService statusService = new();
    private readonly QuickQaSummaryService summaryService = new();

    public QuickQaValidationResult Validate(
        QuickQaReport report,
        IReadOnlyList<QaHotelMetadata> currentHotels,
        string? selectedSurfaceWorkbookFileName)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(currentHotels);

        List<string> errors = [];
        ValidateReportDetails(
            report,
            currentHotels,
            selectedSurfaceWorkbookFileName,
            errors);
        ValidateChecklist(report, errors);
        ValidateFindingsAndStatus(report, errors);
        ValidateSummary(report, errors);
        return new QuickQaValidationResult(errors);
    }

    private static void ValidateReportDetails(
        QuickQaReport report,
        IReadOnlyList<QaHotelMetadata> currentHotels,
        string? selectedSurfaceWorkbookFileName,
        ICollection<string> errors)
    {
        QaHotelInformation hotel = report.HotelInformation;
        string? hotelId = TrimToNull(hotel.HotelId);

        if (hotelId is null)
        {
            errors.Add("Select a Hotel from current QA metadata.");
        }
        else
        {
            QaHotelMetadata[] matches = currentHotels
                .Where(candidate => candidate is not null
                    && candidate.HotelId.Equals(
                        hotelId,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length != 1)
            {
                errors.Add("The selected Hotel no longer exists uniquely in current QA metadata.");
            }
            else
            {
                QaHotelMetadata canonical = matches[0];

                if (!string.Equals(
                        hotel.HotelName,
                        canonical.HotelName,
                        StringComparison.Ordinal)
                    || !string.Equals(
                        hotel.PmsName,
                        canonical.PmsName,
                        StringComparison.Ordinal))
                {
                    errors.Add("The selected Hotel name or PMS no longer matches current QA metadata.");
                }
            }
        }

        if (hotel.FileMonth is null
            || hotel.FileMonth.Year is < 1 or > 9999
            || hotel.FileMonth.Month is < 1 or > 12)
        {
            errors.Add("Select a valid File Month.");
        }

        report.FileId = report.FileId?.Trim() ?? string.Empty;

        if (report.FileId.Length == 0)
        {
            errors.Add("File ID is required.");
        }

        if (!IsPlainWorkbookFileName(selectedSurfaceWorkbookFileName))
        {
            errors.Add("Select a compatible Surface QA workbook.");
        }
    }

    private static void ValidateChecklist(
        QuickQaReport report,
        ICollection<string> errors)
    {
        Dictionary<string, QuickQaCheckResult> resultsById =
            new(StringComparer.Ordinal);

        foreach (QuickQaCheckResult? result in report.ChecklistResults)
        {
            if (result is null
                || string.IsNullOrWhiteSpace(result.CheckId)
                || !QuickQaChecklistCatalog.DefinitionsById.ContainsKey(
                    result.CheckId)
                || !Enum.IsDefined(result.Status)
                || !resultsById.TryAdd(result.CheckId, result))
            {
                errors.Add("Quick QA checklist results are missing, duplicated, or invalid.");
                continue;
            }
        }

        if (resultsById.Count != QuickQaChecklistCatalog.ExpectedDefinitionCount
            || report.ChecklistResults.Count
                != QuickQaChecklistCatalog.ExpectedDefinitionCount)
        {
            errors.Add("Quick QA must contain exactly the approved 21 checklist results.");
        }

        foreach (QuickQaCheckDefinition definition in
                 QuickQaChecklistCatalog.Definitions)
        {
            if (!resultsById.TryGetValue(
                    definition.Id,
                    out QuickQaCheckResult? result))
            {
                errors.Add($"Evaluate '{definition.DisplayName}'.");
                continue;
            }

            if (result.Status == QuickQaCheckStatus.NotEvaluated)
            {
                errors.Add($"Evaluate '{definition.DisplayName}'.");
            }

            if (result.Status == QuickQaCheckStatus.NotApplicable
                && !definition.AllowsNotApplicable)
            {
                errors.Add($"'{definition.DisplayName}' does not allow N/A.");
            }

            if (definition.IsStrategyAvailability
                && result.Status is not QuickQaCheckStatus.Pass
                    and not QuickQaCheckStatus.Fail)
            {
                errors.Add($"Choose Available or Unavailable for '{definition.DisplayName}'.");
            }
        }
    }

    private void ValidateFindingsAndStatus(
        QuickQaReport report,
        ICollection<string> errors)
    {
        Dictionary<string, QaFinding> actualById =
            new(StringComparer.Ordinal);

        foreach (QaFinding? finding in report.Findings)
        {
            if (finding is null
                || string.IsNullOrWhiteSpace(finding.FindingId)
                || !Enum.IsDefined(finding.Severity)
                || !Enum.IsDefined(finding.Resolution)
                || !actualById.TryAdd(finding.FindingId, finding))
            {
                errors.Add("Quick QA findings are missing, duplicated, or invalid.");
                continue;
            }

            if (finding.Resolution == QaFindingResolution.ExplainedAndAccepted)
            {
                errors.Add("Quick QA findings may only be Active or Handled by Custom Script.");
            }

            if (!report.CustomScriptAvailable
                && finding.Resolution == QaFindingResolution.HandledByCustomScript)
            {
                errors.Add("A finding cannot be Handled by Custom Script when no custom script is available.");
            }
        }

        QuickQaReport expectedReport = new()
        {
            CustomScriptAvailable = report.CustomScriptAvailable
        };
        expectedReport.ChecklistResults.Clear();
        expectedReport.ChecklistResults.AddRange(
            report.ChecklistResults
                .Where(result => result is not null)
                .Select(result => new QuickQaCheckResult
                {
                    CheckId = result.CheckId,
                    Status = result.Status
                }));

        try
        {
            _ = new QuickQaFindingSynchronizationService().Synchronize(
                expectedReport);

            if (expectedReport.Findings.Count != report.Findings.Count
                || expectedReport.Findings.Any(expected =>
                    !actualById.TryGetValue(
                        expected.FindingId,
                        out QaFinding? actual)
                    || !GeneratedFindingFieldsEqual(expected, actual)))
            {
                errors.Add("Quick QA findings do not match the current checklist state.");
            }
        }
        catch (InvalidOperationException)
        {
            errors.Add("Quick QA findings cannot be validated until all checklist results are structurally valid.");
        }

        QaReportStatus? expectedStatus = null;

        try
        {
            expectedStatus = statusService.CalculateStatus(
                report.Findings,
                hasBlockingErrors: false);
        }
        catch (ArgumentException)
        {
            errors.Add("Quick QA status cannot be calculated from invalid findings.");
        }

        if (!Enum.IsDefined(report.FinalStatus)
            || expectedStatus is null
            || report.FinalStatus != expectedStatus)
        {
            errors.Add("The final Quick QA Result does not match the current findings.");
        }
    }

    private void ValidateSummary(
        QuickQaReport report,
        ICollection<string> errors)
    {
        if (report.Findings.Count > 0
            && string.IsNullOrWhiteSpace(report.Summary))
        {
            errors.Add("A Summary is required for a Quick QA with warnings or failures.");
        }

        if (summaryService.IsStale(report))
        {
            errors.Add("The Summary is out of date. Regenerate it before saving.");
        }
    }

    private static bool GeneratedFindingFieldsEqual(
        QaFinding expected,
        QaFinding actual)
    {
        return expected.Severity == actual.Severity
            && expected.Source == actual.Source
            && string.Equals(
                expected.RelatedCheckId,
                actual.RelatedCheckId,
                StringComparison.Ordinal)
            && string.Equals(
                expected.Title,
                actual.Title,
                StringComparison.Ordinal)
            && string.Equals(
                expected.Description,
                actual.Description,
                StringComparison.Ordinal);
    }

    private static bool IsPlainWorkbookFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || Path.IsPathFullyQualified(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        return string.Equals(
                Path.GetFileName(trimmed),
                trimmed,
                StringComparison.Ordinal)
            && trimmed.IndexOfAny(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) < 0
            && Path.GetExtension(trimmed).Equals(
                ".xlsx",
                StringComparison.OrdinalIgnoreCase);
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
