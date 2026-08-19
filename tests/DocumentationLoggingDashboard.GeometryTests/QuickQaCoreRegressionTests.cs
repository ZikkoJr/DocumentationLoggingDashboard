using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.GeometryTests;

/// <summary>
/// Focused semantic coverage for the fixed Quick QA catalog, findings, status,
/// Summary lifecycle, and pre-save validation. Program.cs invokes
/// <see cref="RunAll"/> with the other semantic suites.
/// </summary>
internal static class QuickQaCoreRegressionTests
{
    private const string SurfaceWorkbookFileName = "Surface QA.xlsx";

    private static readonly QuickQaFindingSynchronizationService FindingService =
        new();
    private static readonly QuickQaSummaryService SummaryService = new();
    private static readonly QuickQaValidationService ValidationService = new();

    private static readonly string[] StrategyCheckIds =
    [
        QuickQaChecklistIds.Raw.SourceColumnAvailable,
        QuickQaChecklistIds.Raw.RateColumnAvailable,
        QuickQaChecklistIds.Raw.MarketColumnAvailable
    ];

    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        (string Name, Action Body)[] tests =
        [
            ("exact 21-check Quick catalog", TestCatalogContract),
            ("Quick strategy eight-combination matrix", TestStrategyMatrix),
            ("ordinary findings and custom-script status", TestFindingsAndStatus),
            ("generated and manually edited Summary lifecycle", TestSummaryLifecycle),
            ("Quick pre-save validation", TestValidation)
        ];

        int failed = 0;
        output.WriteLine($"Quick QA core harness: {tests.Length} tests");

        foreach ((string name, Action body) in tests)
        {
            output.WriteLine($"[RUN ] {name}");

            try
            {
                body();
                output.WriteLine($"[PASS] {name}");
            }
            catch (Exception exception)
            {
                failed++;
                output.WriteLine($"[FAIL] {name}");
                output.WriteLine(exception.ToString());
            }
        }

        output.WriteLine(
            failed == 0
                ? $"[PASS] All {tests.Length} Quick QA core tests passed."
                : $"[FAIL] {failed} of {tests.Length} Quick QA core tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void TestCatalogContract()
    {
        string[] expectedIds =
        [
            "QUICK.RAW.NAMES.AVAILABLE",
            "QUICK.RAW.CONFIRMATION_NUMBER.AVAILABLE",
            "QUICK.RAW.RESERVATION_DATE.AVAILABLE",
            "QUICK.RAW.ARRIVAL_DATE.AVAILABLE",
            "QUICK.RAW.DEPARTURE_DATE.AVAILABLE",
            "QUICK.RAW.MONETARY.AVAILABLE",
            "QUICK.RAW.FILE.FORMAT_CONSISTENT",
            "QUICK.RAW.DATES.FORMAT_VALID",
            "QUICK.RAW.DATES.SEQUENCE_VALID",
            "QUICK.RAW.CURRENCY.CONSISTENT",
            "QUICK.RAW.STRATEGY.SOURCE_COLUMN_AVAILABLE",
            "QUICK.RAW.STRATEGY.RATE_COLUMN_AVAILABLE",
            "QUICK.RAW.STRATEGY.MARKET_COLUMN_AVAILABLE",
            "QUICK.RAW.CONFIRMATION.CANDIDATES_REVIEWED",
            "QUICK.DB.NAMES.PULLED_CORRECTLY",
            "QUICK.DB.MONETARY.PULLED_CORRECTLY",
            "QUICK.DB.DATES.PULLED_CORRECTLY",
            "QUICK.DB.CONFIRMATION_NUMBER.PULLED_CORRECTLY",
            "QUICK.DB.REQUIRED_VALUES.PRESENT",
            "QUICK.DB.REJECTED_RECORDS.ACCOUNTED_FOR",
            "QUICK.DB.EMAIL.PULLED_CORRECTLY"
        ];
        IReadOnlyList<QuickQaCheckDefinition> definitions =
            QuickQaChecklistCatalog.Definitions;
        string[] actualIds = definitions
            .Select(definition => definition.Id)
            .ToArray();

        Check(
            definitions.Count == QuickQaChecklistCatalog.ExpectedDefinitionCount
            && definitions.Count == 21,
            "The Quick catalog does not contain exactly 21 checks.");
        Check(
            actualIds.SequenceEqual(expectedIds, StringComparer.Ordinal),
            "The Quick catalog IDs or their order differ from the approved contract.");
        Check(
            actualIds.Distinct(StringComparer.Ordinal).Count() == actualIds.Length,
            "The Quick catalog contains a duplicate stable ID.");
        Check(
            definitions.Count(definition =>
                definition.Section == QaChecklistSection.RawFile) == 14
            && definitions.Count(definition =>
                definition.Section == QaChecklistSection.Database) == 7,
            "The Quick catalog does not contain 14 Raw and 7 Database checks.");

        string[] expectedNotApplicableIds =
        [
            QuickQaChecklistIds.Raw.CurrencyConsistent,
            QuickQaChecklistIds.Raw.ConfirmationCandidatesReviewed,
            QuickQaChecklistIds.Database.RejectedRecordsAccountedFor,
            QuickQaChecklistIds.Database.EmailPulledCorrectly
        ];
        string[] actualNotApplicableIds = definitions
            .Where(definition => definition.AllowsNotApplicable)
            .Select(definition => definition.Id)
            .ToArray();
        Check(
            actualNotApplicableIds.SequenceEqual(
                expectedNotApplicableIds,
                StringComparer.Ordinal),
            "Quick QA does not allow N/A on exactly the four approved checks.");
        Check(
            definitions
                .Where(definition => definition.IsStrategyAvailability)
                .Select(definition => definition.Id)
                .SequenceEqual(StrategyCheckIds, StringComparer.Ordinal),
            "Quick QA does not expose exactly the three Strategy availability rows.");

        HashSet<string> detailedIds = QaChecklistCatalog.Definitions
            .Select(definition => definition.Id)
            .ToHashSet(StringComparer.Ordinal);
        Check(
            actualIds.All(id => id.StartsWith("QUICK.", StringComparison.Ordinal))
            && !actualIds.Any(detailedIds.Contains)
            && !actualIds.Any(id => id.Contains(
                "ARRIVAL_WITHIN_FILE_MONTH",
                StringComparison.Ordinal)),
            "The Quick catalog contains a Detailed-only or non-Quick stable ID.");

        string[] detailedOnlyReportProperties =
        [
            "Statistics",
            "FileCharacteristics",
            "QaDate",
            "CreatedBy",
            "OriginalFileName"
        ];
        Check(
            detailedOnlyReportProperties.All(propertyName =>
                typeof(QuickQaReport).GetProperty(propertyName) is null),
            "QuickQaReport contains a Detailed-only report concept.");
        Check(
            !definitions.Any(definition =>
                definition.DisplayName.Contains(
                    "Arrival Dates are within",
                    StringComparison.OrdinalIgnoreCase)
                || definition.DisplayName.Contains(
                    "First Name",
                    StringComparison.OrdinalIgnoreCase)
                || definition.DisplayName.Contains(
                    "Last Name",
                    StringComparison.OrdinalIgnoreCase)
                || definition.DisplayName.Contains(
                    "Full Name",
                    StringComparison.OrdinalIgnoreCase)
                || definition.DisplayName.Contains(
                    "spot-check",
                    StringComparison.OrdinalIgnoreCase)),
            "The Quick checklist contains a Detailed-only checklist concept.");
    }

    private static void TestStrategyMatrix()
    {
        for (int mask = 0; mask < 8; mask++)
        {
            QuickQaReport report = CreateCompletedReport();
            List<string> unavailableCategories = [];

            for (int index = 0; index < StrategyCheckIds.Length; index++)
            {
                bool unavailable = (mask & (1 << index)) != 0;
                SetStatus(
                    report,
                    StrategyCheckIds[index],
                    unavailable
                        ? QuickQaCheckStatus.Fail
                        : QuickQaCheckStatus.Pass);

                if (unavailable)
                {
                    unavailableCategories.Add(index switch
                    {
                        0 => "Source",
                        1 => "Rate",
                        2 => "Market",
                        _ => throw new InvalidOperationException()
                    });
                }
            }

            QaReportStatus status = FindingService.Synchronize(report);
            int unavailableCount = unavailableCategories.Count;
            QaFinding[] strategyFindings = report.Findings
                .Where(finding =>
                    finding.FindingId ==
                        QuickQaFindingSynchronizationService.StrategyWarningFindingId
                    || finding.FindingId ==
                        QuickQaFindingSynchronizationService.StrategyFailureFindingId)
                .ToArray();

            Check(
                report.Findings.Count == (unavailableCount == 0 ? 0 : 1)
                && strategyFindings.Length == report.Findings.Count,
                $"Strategy mask {mask}: expected zero or one aggregate finding only.");
            Check(
                !report.Findings.Any(finding => StrategyCheckIds.Any(checkId =>
                    finding.FindingId ==
                        QuickQaFindingSynchronizationService.WarningForCheck(checkId)
                    || finding.FindingId ==
                        QuickQaFindingSynchronizationService.FailureForCheck(checkId))),
                $"Strategy mask {mask}: an individual Strategy finding was generated.");

            if (unavailableCount == 0)
            {
                Check(
                    status == QaReportStatus.Pass,
                    "All Strategy columns available did not result in Pass.");
            }
            else if (unavailableCount is 1 or 2)
            {
                string expectedSentence = CreateExpectedStrategyWarning(
                    unavailableCategories);
                Check(
                    strategyFindings[0].FindingId ==
                        "QUICK:WARN:STRATEGY:SOURCE_RATE_MARKET"
                    && strategyFindings[0].Severity == QaFindingSeverity.Warning
                    && strategyFindings[0].Description == expectedSentence
                    && status == QaReportStatus.PassWithWarnings,
                    $"Strategy mask {mask}: the aggregate Strategy Warning is incorrect.");
            }
            else
            {
                Check(
                    strategyFindings[0].FindingId ==
                        "QUICK:FAIL:STRATEGY:SOURCE_RATE_MARKET"
                    && strategyFindings[0].Severity == QaFindingSeverity.Failure
                    && strategyFindings[0].Description ==
                        "Source, Rate, and Market strategy columns are all unavailable."
                    && status == QaReportStatus.Fail,
                    "All Strategy columns unavailable did not produce the one aggregate Failure.");
            }
        }
    }

    private static void TestFindingsAndStatus()
    {
        QuickQaReport warning = CreateCompletedReport();
        SetStatus(
            warning,
            QuickQaChecklistIds.Raw.NamesAvailable,
            QuickQaCheckStatus.Warning);
        QaReportStatus warningStatus = FindingService.Synchronize(warning);
        Check(
            warning.Findings.Count == 1
            && warning.Findings[0].FindingId ==
                QuickQaFindingSynchronizationService.WarningForCheck(
                    QuickQaChecklistIds.Raw.NamesAvailable)
            && warning.Findings[0].Severity == QaFindingSeverity.Warning
            && warningStatus == QaReportStatus.PassWithWarnings,
            "An ordinary Quick Warning did not produce one Warning and Pass with Warnings.");

        QuickQaReport failure = CreateCompletedReport();
        SetStatus(
            failure,
            QuickQaChecklistIds.Database.NamesPulledCorrectly,
            QuickQaCheckStatus.Fail);
        QaReportStatus failureStatus = FindingService.Synchronize(failure);
        Check(
            failure.Findings.Count == 1
            && failure.Findings[0].FindingId ==
                QuickQaFindingSynchronizationService.FailureForCheck(
                    QuickQaChecklistIds.Database.NamesPulledCorrectly)
            && failure.Findings[0].Severity == QaFindingSeverity.Failure
            && failureStatus == QaReportStatus.Fail,
            "An ordinary Quick Failure did not produce one active Failure and Fail.");

        failure.Findings[0].Resolution =
            QaFindingResolution.HandledByCustomScript;
        failure.CustomScriptAvailable = false;
        _ = FindingService.Synchronize(failure);
        Check(
            failure.Findings[0].Resolution == QaFindingResolution.Active
            && failure.FinalStatus == QaReportStatus.Fail,
            "Custom Script unavailable did not clear an invalid Handled resolution.");

        QuickQaReport independentlyHandled = CreateCompletedReport();
        independentlyHandled.CustomScriptAvailable = true;
        SetStatus(
            independentlyHandled,
            QuickQaChecklistIds.Raw.NamesAvailable,
            QuickQaCheckStatus.Fail);
        SetStatus(
            independentlyHandled,
            QuickQaChecklistIds.Database.ConfirmationNumberPulledCorrectly,
            QuickQaCheckStatus.Fail);
        _ = FindingService.Synchronize(independentlyHandled);
        Check(
            independentlyHandled.Findings.Count == 2
            && independentlyHandled.Findings.All(finding =>
                finding.Severity == QaFindingSeverity.Failure),
            "The two independent failed checks did not create two Failure findings.");

        foreach (QaFinding finding in independentlyHandled.Findings)
        {
            finding.Resolution = QaFindingResolution.HandledByCustomScript;
        }

        _ = FindingService.Synchronize(independentlyHandled);
        Check(
            independentlyHandled.Findings.All(finding =>
                finding.Resolution ==
                    QaFindingResolution.HandledByCustomScript)
            && independentlyHandled.FinalStatus ==
                QaReportStatus.PassWithWarnings,
            "Independently handled Failures were not preserved as Pass with Warnings.");

        string firstFindingId = independentlyHandled.Findings[0].FindingId;
        string secondFindingId = independentlyHandled.Findings[1].FindingId;
        independentlyHandled.Findings[0].Resolution = QaFindingResolution.Active;
        _ = FindingService.Synchronize(independentlyHandled);
        Check(
            independentlyHandled.Findings.Single(finding =>
                finding.FindingId == firstFindingId).Resolution ==
                    QaFindingResolution.Active
            && independentlyHandled.Findings.Single(finding =>
                finding.FindingId == secondFindingId).Resolution ==
                    QaFindingResolution.HandledByCustomScript
            && independentlyHandled.FinalStatus == QaReportStatus.Fail,
            "One active Failure did not take precedence over an independently handled Failure.");
    }

    private static void TestSummaryLifecycle()
    {
        QuickQaReport clean = CreateCompletedReport();
        SummaryService.Regenerate(clean);
        Check(
            clean.Summary ==
                "Raw file and Database QA passed successfully"
            && clean.Summary == QuickQaSummaryService.CleanPassSummary
            && !SummaryService.IsStale(clean),
            "A clean Quick QA did not produce the exact current clean Summary.");

        QuickQaReport findingsReport = CreateCompletedReport();
        findingsReport.CustomScriptAvailable = true;
        SetStatus(
            findingsReport,
            QuickQaChecklistIds.Raw.NamesAvailable,
            QuickQaCheckStatus.Warning);
        SetStatus(
            findingsReport,
            QuickQaChecklistIds.Database.ConfirmationNumberPulledCorrectly,
            QuickQaCheckStatus.Fail);
        _ = FindingService.Synchronize(findingsReport);
        findingsReport.Findings.Single(finding =>
            finding.Severity == QaFindingSeverity.Failure).Resolution =
                QaFindingResolution.HandledByCustomScript;
        _ = FindingService.Synchronize(findingsReport);
        SummaryService.Regenerate(findingsReport);

        string[] summaryLines = findingsReport.Summary.Split(
            Environment.NewLine,
            StringSplitOptions.None);
        Check(
            summaryLines.Length == findingsReport.Findings.Count
            && summaryLines.All(line =>
                !string.IsNullOrWhiteSpace(line)
                && line.EndsWith(".", StringComparison.Ordinal)),
            "The generated Summary does not contain one sentence per finding.");
        Check(
            summaryLines[0] ==
                "Warning: names are not available to be pulled correctly."
            && summaryLines[1] ==
                "Confirmation Number is not being pulled correctly in the database; handled by Custom Script.",
            "The generated Summary or handled-by-Custom-Script context is incorrect.");

        QuickQaReport lifecycle = CreateCompletedReport();
        SummaryService.Regenerate(lifecycle);
        SummaryService.MarkManuallyEdited(
            lifecycle,
            "Current manually edited clean summary.");
        Check(
            lifecycle.SummaryWasManuallyEdited
            && !SummaryService.IsStale(lifecycle),
            "A manual edit without a QA-state change was not current.");

        SetStatus(
            lifecycle,
            QuickQaChecklistIds.Raw.FileFormatConsistent,
            QuickQaCheckStatus.Warning);
        _ = FindingService.Synchronize(lifecycle);
        SummaryService.Synchronize(lifecycle);
        Check(
            lifecycle.Summary == "Current manually edited clean summary."
            && lifecycle.SummaryWasManuallyEdited
            && SummaryService.IsStale(lifecycle),
            "A QA change did not retain and mark the manually edited Summary stale.");

        SummaryService.Regenerate(lifecycle);
        Check(
            lifecycle.Summary ==
                "Warning: file formatting is not consistent throughout."
            && !lifecycle.SummaryWasManuallyEdited
            && !SummaryService.IsStale(lifecycle),
            "Regenerate Summary did not restore generated current state.");

        SummaryService.MarkManuallyEdited(
            lifecycle,
            "Second current manual summary.");
        Check(
            lifecycle.Summary == "Second current manual summary."
            && lifecycle.SummaryWasManuallyEdited
            && !SummaryService.IsStale(lifecycle),
            "The regenerated Summary could not be edited again while remaining current.");
    }

    private static void TestValidation()
    {
        QuickQaReport valid = CreateReadyReport();
        QuickQaValidationResult validResult = Validate(valid);
        Check(
            validResult.IsValid
            && valid.FileId == "001234",
            $"A valid leading-zero File ID was rejected: {JoinErrors(validResult)}");

        QuickQaReport trimmed = CreateReadyReport();
        trimmed.FileId = "  001234  ";
        QuickQaValidationResult trimmedResult = Validate(trimmed);
        Check(
            trimmedResult.IsValid
            && trimmed.FileId == "001234",
            $"File ID trimming changed leading zeroes or invalidated the report: {JoinErrors(trimmedResult)}");

        foreach (string invalidFileId in new[] { string.Empty, "   " })
        {
            QuickQaReport blank = CreateReadyReport();
            blank.FileId = invalidFileId;
            AssertInvalid(
                Validate(blank),
                "File ID is required",
                "blank or whitespace-only File ID");
        }

        QuickQaReport invalidFileMonth = CreateReadyReport();
        invalidFileMonth.HotelInformation.FileMonth = new QaFileMonth(0, 8);
        AssertInvalid(
            Validate(invalidFileMonth),
            "Select a valid File Month",
            "out-of-range File Month year");

        QuickQaReport approvedNotApplicable = CreateReadyReport();
        foreach (QuickQaCheckDefinition definition in
                 QuickQaChecklistCatalog.Definitions.Where(definition =>
                     definition.AllowsNotApplicable))
        {
            SetStatus(
                approvedNotApplicable,
                definition.Id,
                QuickQaCheckStatus.NotApplicable);
        }

        SynchronizeSummary(approvedNotApplicable);
        QuickQaValidationResult approvedNotApplicableResult =
            Validate(approvedNotApplicable);
        Check(
            approvedNotApplicableResult.IsValid,
            $"An approved N/A state was rejected: {JoinErrors(approvedNotApplicableResult)}");

        QuickQaReport invalidNotApplicable = CreateReadyReport();
        SetStatus(
            invalidNotApplicable,
            QuickQaChecklistIds.Raw.NamesAvailable,
            QuickQaCheckStatus.NotApplicable);
        SynchronizeSummary(invalidNotApplicable);
        AssertInvalid(
            Validate(invalidNotApplicable),
            "does not allow N/A",
            "N/A on an ordinary required check");

        QuickQaReport notEvaluated = CreateReadyReport();
        SetStatus(
            notEvaluated,
            QuickQaChecklistIds.Database.RequiredValuesPresent,
            QuickQaCheckStatus.NotEvaluated);
        SynchronizeSummary(notEvaluated);
        AssertInvalid(
            Validate(notEvaluated),
            "Evaluate 'Required database values are present'",
            "NotEvaluated required check");

        QuickQaReport staleSummary = CreateReadyReport();
        SummaryService.MarkManuallyEdited(
            staleSummary,
            "Current manual summary before another QA change.");
        SetStatus(
            staleSummary,
            QuickQaChecklistIds.Database.DatesPulledCorrectly,
            QuickQaCheckStatus.Warning);
        _ = FindingService.Synchronize(staleSummary);
        AssertInvalid(
            Validate(staleSummary),
            "Summary is out of date",
            "stale manually edited Summary");

        QuickQaReport metadataMismatch = CreateReadyReport();
        metadataMismatch.HotelInformation.PmsName = "Changed PMS";
        AssertInvalid(
            Validate(metadataMismatch),
            "Hotel name or PMS no longer matches",
            "canonical metadata mismatch");
    }

    private static QuickQaReport CreateCompletedReport()
    {
        QuickQaReport report = new();

        foreach (QuickQaCheckResult result in report.ChecklistResults)
        {
            result.Status = QuickQaCheckStatus.Pass;
        }

        _ = FindingService.Synchronize(report);
        return report;
    }

    private static QuickQaReport CreateReadyReport()
    {
        QuickQaReport report = CreateCompletedReport();
        QaHotelMetadata hotel = CanonicalHotel();
        report.HotelInformation = new QaHotelInformation
        {
            HotelId = hotel.HotelId,
            HotelName = hotel.HotelName,
            PmsName = hotel.PmsName,
            FileMonth = new QaFileMonth(2026, 8)
        };
        report.FileId = "001234";
        SummaryService.Regenerate(report);
        return report;
    }

    private static void SynchronizeSummary(QuickQaReport report)
    {
        _ = FindingService.Synchronize(report);
        SummaryService.Regenerate(report);
    }

    private static QuickQaValidationResult Validate(QuickQaReport report)
    {
        return ValidationService.Validate(
            report,
            [CanonicalHotel()],
            SurfaceWorkbookFileName);
    }

    private static QaHotelMetadata CanonicalHotel()
    {
        return new QaHotelMetadata
        {
            HotelId = "QUICK-001",
            HotelName = "Synthetic Quick Hotel",
            PmsName = "Synthetic Quick PMS",
            FolderName = "Synthetic Quick Hotel - QUICK-001"
        };
    }

    private static void SetStatus(
        QuickQaReport report,
        string checkId,
        QuickQaCheckStatus status)
    {
        report.GetChecklistResult(checkId).Status = status;
    }

    private static string CreateExpectedStrategyWarning(
        IReadOnlyList<string> unavailableCategories)
    {
        return unavailableCategories.Count switch
        {
            1 =>
                $"Strategy Warning: {unavailableCategories[0]} column is not available.",
            2 =>
                $"Strategy Warning: {unavailableCategories[0]} and {unavailableCategories[1]} columns are not available.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(unavailableCategories))
        };
    }

    private static void AssertInvalid(
        QuickQaValidationResult result,
        string expectedErrorFragment,
        string scenario)
    {
        Check(
            !result.IsValid
            && result.Errors.Any(error => error.Contains(
                expectedErrorFragment,
                StringComparison.OrdinalIgnoreCase)),
            $"Validation did not reject {scenario} with '{expectedErrorFragment}': {JoinErrors(result)}");
    }

    private static string JoinErrors(QuickQaValidationResult result)
    {
        return result.Errors.Count == 0
            ? "<none>"
            : string.Join(" | ", result.Errors);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
