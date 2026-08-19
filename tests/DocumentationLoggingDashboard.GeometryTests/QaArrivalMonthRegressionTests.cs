using System.Security.Cryptography;
using System.Text;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Forms;
using DocumentationLoggingDashboard.QAReports.Forms.Controls;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.GeometryTests;

internal static class QaArrivalMonthRegressionTests
{
    private const string RetiredCheckId =
        "RAW.DATES.ARRIVAL_WITHIN_FILE_MONTH";
    private const string RetiredDisplayName =
        "Arrival Dates are within the selected File Month";
    private const string FindingId =
        "STAT:FAIL:ARRIVAL_OUTSIDE_FILE_MONTH";

    private static readonly QaStatisticsCalculationService CalculationService = new();
    private static readonly QaFindingSynchronizationService FindingService = new();
    private static readonly QaReportValidationService ValidationService = new();
    private static readonly QaPdfGenerationService PdfService = new();

    public static int RunAll(
        TextWriter output,
        string? preservedEvidenceParentDirectory)
    {
        ArgumentNullException.ThrowIfNull(output);

        (string Name, Action Body)[] tests =
        [
            ("active catalog and exact threshold boundaries", TestCatalogAndThresholds),
            ("zero denominator and invalid totals", TestInvalidStatistics),
            ("finding lifecycle, resolution, and status", TestFindingLifecycle),
            (
                "form, PDF, paired-save, and index evidence",
                () => TestPdfSaveAndIndexEvidence(
                    output,
                    preservedEvidenceParentDirectory))
        ];

        int failed = 0;
        output.WriteLine($"Arrival Month correction harness: {tests.Length} tests");

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
                ? $"[PASS] All {tests.Length} Arrival Month correction tests passed."
                : $"[FAIL] {failed} of {tests.Length} Arrival Month correction tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void TestCatalogAndThresholds()
    {
        IReadOnlyList<QaCheckDefinition> definitions =
            QaChecklistCatalog.Definitions;
        QaCheckDefinition[] rawDefinitions = definitions
            .Where(definition =>
                definition.Section == QaChecklistSection.RawFile)
            .ToArray();
        QaCheckDefinition[] databaseDefinitions = definitions
            .Where(definition =>
                definition.Section == QaChecklistSection.Database)
            .ToArray();

        Check(
            definitions.Count == QaChecklistCatalog.ExpectedDefinitionCount,
            $"Expected {QaChecklistCatalog.ExpectedDefinitionCount} active checklist definitions.");
        Check(rawDefinitions.Length == 22, "Expected 22 active Raw File definitions.");
        Check(databaseDefinitions.Length == 7, "Expected 7 active Database definitions.");
        Check(
            definitions.Select(definition => definition.Id)
                .Distinct(StringComparer.Ordinal)
                .Count() == definitions.Count,
            "The active checklist catalog contains a duplicate ID.");
        Check(
            !definitions.Any(definition =>
                definition.Id.Equals(RetiredCheckId, StringComparison.Ordinal)),
            "The retired Arrival Month ID remains in the active catalog.");
        Check(
            QaChecklistIds.Raw.ArrivalWithinFileMonth == RetiredCheckId,
            "The retired compatibility constant changed unexpectedly.");
        Check(
            definitions.Single(definition =>
                    definition.Id == QaChecklistIds.Raw.MonetarySpotCheckValid)
                .Applicability == QaCheckApplicability.TwoMonetaryColumns,
            "Monetary spot-check applicability changed unexpectedly.");

        (int Valid, int Inside, int Outside, decimal Percentage, bool Failure)[] cases =
        [
            (100, 100, 0, 0m, false),
            (100, 80, 20, 20m, false),
            (100, 70, 30, 30m, false),
            (100, 69, 31, 31m, true),
            (10, 7, 3, 30m, false),
            (13, 9, 4, 30.77m, true)
        ];

        foreach ((int valid, int inside, int outside, decimal percentage, bool failure)
                 in cases)
        {
            QaReport report = CreateCompleteReport(
                new QaFileMonth(2026, 6),
                valid,
                inside,
                outside);
            QaFinding[] arrivalFindings = report.Findings
                .Where(finding => finding.FindingId == FindingId)
                .ToArray();

            Check(
                report.Statistics.FileMonth.PercentageOutsideFileMonth == percentage,
                $"{outside}/{valid}: displayed percentage was " +
                $"{report.Statistics.FileMonth.PercentageOutsideFileMonth}, expected {percentage}.");
            Check(
                arrivalFindings.Length == (failure ? 1 : 0),
                $"{outside}/{valid}: expected {(failure ? "one" : "no")} threshold Failure.");
            Check(
                !report.ChecklistResults.Any(result =>
                    result.CheckId == RetiredCheckId),
                $"{outside}/{valid}: a new report contains the retired checklist result.");
            Check(
                !report.Findings.Any(finding =>
                    finding.FindingId ==
                    QaFindingIds.FailureForCheck(RetiredCheckId)),
                $"{outside}/{valid}: a retired checklist Failure was generated.");

            QaReportValidationResult validation = Validate(report);
            Check(
                validation.IsReady,
                $"{outside}/{valid}: report was not ready: " +
                string.Join(" | ", validation.BlockingErrors));
            Check(
                validation.CalculatedStatus ==
                    (failure ? QaReportStatus.Fail : QaReportStatus.Pass),
                $"{outside}/{valid}: unexpected calculated status " +
                $"{validation.CalculatedStatus}.");

            if (!failure)
            {
                Check(
                    report.Findings.Count == 0,
                    $"{outside}/{valid}: an automatic Warning or Failure was generated.");
                continue;
            }

            QaFinding finding = arrivalFindings.Single();
            Check(
                finding.Severity == QaFindingSeverity.Failure
                && finding.Resolution == QaFindingResolution.Active
                && finding.Source == QaFindingSource.Statistic
                && finding.RelatedCheckId is null,
                $"{outside}/{valid}: finding contract is incorrect.");
            Check(
                finding.Title ==
                "More than 30% of Arrival Dates are outside the selected File Month",
                $"{outside}/{valid}: finding title is incorrect.");
            Check(
                finding.Description.Contains(
                    "Selected File Month: 2026-06",
                    StringComparison.Ordinal)
                && finding.Description.Contains(
                    $"Valid Arrival Dates: {valid}",
                    StringComparison.Ordinal)
                && finding.Description.Contains(
                    $"inside File Month: {inside}",
                    StringComparison.Ordinal)
                && finding.Description.Contains(
                    $"outside File Month: {outside}",
                    StringComparison.Ordinal),
                $"{outside}/{valid}: finding description is missing required context.");

            if (valid == 13)
            {
                Check(
                    finding.Description.Contains(
                        "outside percentage: 30.769230",
                        StringComparison.Ordinal),
                    "The fractional boundary description lost sufficient precision.");
            }
        }
    }

    private static void TestInvalidStatistics()
    {
        QaReport zero = CreateCompleteReport(
            new QaFileMonth(2026, 7),
            valid: 0,
            inside: 0,
            outside: 0);
        Check(
            zero.Statistics.FileMonth.PercentageWithinFileMonth == 0m
            && zero.Statistics.FileMonth.PercentageOutsideFileMonth == 0m,
            "Zero denominator did not preserve the Phase 7 zero-percentage convention.");
        Check(!HasArrivalFailure(zero), "Zero denominator created a threshold Failure.");
        QaReportValidationResult zeroValidation = Validate(zero);
        Check(
            zeroValidation.IsReady
            && zeroValidation.CalculatedStatus == QaReportStatus.Pass,
            "A valid zero-denominator report did not remain ready and Pass.");

        (string Name, int Valid, int Inside, int Outside)[] invalidCases =
        [
            ("positive outside with zero valid", 0, 0, 1),
            ("positive inside with zero valid", 0, 1, 0),
            ("outside greater than valid", 100, 0, 101),
            ("inside greater than valid", 100, 101, 0),
            ("category sum mismatch", 100, 70, 20),
            ("negative valid", -1, 0, 0),
            ("negative inside", 100, -1, 101),
            ("negative outside", 100, 101, -1)
        ];

        foreach ((string name, int valid, int inside, int outside) in invalidCases)
        {
            QaReport report = CreateCompleteReport(
                new QaFileMonth(2026, 8),
                valid,
                inside,
                outside);
            Check(
                !HasArrivalFailure(report),
                $"{name}: invalid totals created an artificial QA finding.");

            QaReportValidationResult validation = Validate(report);
            Check(!validation.IsReady, $"{name}: invalid totals were marked ready.");
            Check(
                validation.BlockingErrors.Count > 0
                && validation.CalculatedStatus is null,
                $"{name}: invalid totals did not block readiness/status.");
        }

        QaReport legacy = CreateCompleteReport(
            new QaFileMonth(2026, 9),
            valid: 100,
            inside: 100,
            outside: 0);
        legacy.ChecklistResults.Add(new QaCheckResult
        {
            CheckId = RetiredCheckId,
            Status = QaCheckStatus.Fail,
            ResultSource = QaResultSource.Manual
        });
        QaReportValidationResult legacyValidation = Validate(legacy);
        Check(
            !legacyValidation.IsReady
            && legacyValidation.BlockingErrors.Any(error =>
                error.Contains(
                    "not in the approved catalog",
                    StringComparison.OrdinalIgnoreCase)),
            "The non-reload compatibility boundary did not reject a retired result.");

        bool pdfRejected = false;
        try
        {
            legacy.ReportStatus = QaReportStatus.Pass;
            _ = PdfService.GeneratePdf(
                legacy,
                legacyValidation,
                new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero));
        }
        catch (QaPdfGenerationException)
        {
            pdfRejected = true;
        }

        Check(pdfRejected, "PDF generation accepted a report with a retired result.");
    }

    private static void TestFindingLifecycle()
    {
        QaReport report = CreateCompleteReport(
            new QaFileMonth(2026, 10),
            valid: 100,
            inside: 69,
            outside: 31,
            customScriptSupport: true);
        QaFinding first = ArrivalFailure(report);
        Check(
            first.Resolution == QaFindingResolution.Active,
            "A newly created threshold Failure was not Active.");
        Check(
            Validate(report).CalculatedStatus == QaReportStatus.Fail,
            "An Active threshold Failure did not calculate Fail.");

        first.Resolution = QaFindingResolution.HandledByCustomScript;
        first.CustomScriptName = "arrival-month-fix.csx";
        first.ResolutionNotes =
            "Synthetic aggregate correction; no source rows retained.";
        report.GeneralNotes =
            "Synthetic unrelated report edit while the threshold remains active.";
        Synchronize(report);
        QaFinding handled = ArrivalFailure(report);
        Check(
            ReferenceEquals(first, handled)
            && handled.Severity == QaFindingSeverity.Failure
            && handled.Resolution == QaFindingResolution.HandledByCustomScript
            && handled.CustomScriptName == "arrival-month-fix.csx",
            "Continuous validity did not preserve the handled Failure state.");
        Check(
            Validate(report).CalculatedStatus == QaReportStatus.PassWithWarnings,
            "A handled Failure did not follow the PassWithWarnings rule.");

        report.Statistics.FileMonth.ArrivalDatesWithinFileMonth = 65;
        report.Statistics.FileMonth.ArrivalDatesOutsideFileMonth = 35;
        report.HotelInformation.FileMonth = new QaFileMonth(2026, 11);
        Synchronize(report);
        QaFinding updated = ArrivalFailure(report);
        Check(
            ReferenceEquals(handled, updated)
            && updated.Resolution == QaFindingResolution.HandledByCustomScript,
            "An above-threshold edit did not preserve finding identity/resolution.");
        Check(
            updated.Description.Contains(
                "Selected File Month: 2026-11",
                StringComparison.Ordinal)
            && updated.Description.Contains(
                "outside File Month: 35",
                StringComparison.Ordinal),
            "A continuously valid finding did not refresh contextual values.");
        Check(
            report.Findings.Count(finding => finding.FindingId == FindingId) == 1,
            "A continuously valid threshold condition duplicated the finding.");

        report.Statistics.FileMonth.ArrivalDatesWithinFileMonth = 70;
        report.Statistics.FileMonth.ArrivalDatesOutsideFileMonth = 30;
        Synchronize(report);
        Check(!HasArrivalFailure(report), "Exactly 30% did not remove the finding.");
        Check(
            Validate(report).CalculatedStatus == QaReportStatus.Pass,
            "31% to 30% did not allow Pass on an otherwise clean report.");

        report.Statistics.FileMonth.ArrivalDatesWithinFileMonth = 69;
        report.Statistics.FileMonth.ArrivalDatesOutsideFileMonth = 31;
        Synchronize(report);
        QaFinding recurrence = ArrivalFailure(report);
        Check(
            !ReferenceEquals(updated, recurrence)
            && recurrence.Resolution == QaFindingResolution.Active
            && recurrence.CustomScriptName is null
            && recurrence.ResolutionNotes is null,
            "Above-below-above restored obsolete handled state.");
        Check(
            Validate(report).CalculatedStatus == QaReportStatus.Fail,
            "30% to 31% did not restore Fail without checklist interaction.");
    }

    private static void TestPdfSaveAndIndexEvidence(
        TextWriter output,
        string? preservedEvidenceParentDirectory)
    {
        bool preserve = !string.IsNullOrWhiteSpace(
            preservedEvidenceParentDirectory);
        string parent;

        if (preserve)
        {
            if (!Path.IsPathFullyQualified(
                    preservedEvidenceParentDirectory!))
            {
                throw new ArgumentException(
                    "The preserved evidence parent must be an absolute path outside the repository.",
                    nameof(preservedEvidenceParentDirectory));
            }

            parent = Path.GetFullPath(
                preservedEvidenceParentDirectory!);
            string? repositoryRoot = FindRepositoryRoot();

            if (repositoryRoot is null
                || IsWithinOrEqual(parent, repositoryRoot))
            {
                throw new ArgumentException(
                    "The preserved evidence parent must be outside the repository.",
                    nameof(preservedEvidenceParentDirectory));
            }
        }
        else
        {
            parent = Path.GetTempPath();
        }

        string root = Path.Combine(
            parent,
            $"arr-{Guid.NewGuid():N}"[..16]);
        Directory.CreateDirectory(root);

        List<string> manifestLines =
        [
            "Arrival Month correction synthetic evidence",
            $"Root: {root}"
        ];

        try
        {
            QaStoragePaths paths = new(root);
            new QaStorageInitializer(paths).Initialize();
            QaFolderNameSanitizer sanitizer = new();
            QaMetadataService metadataService = new(paths, sanitizer);
            QaPmsMetadata pms = metadataService.AddPmsSystem("Synthetic PMS");
            QaHotelMetadata hotel = metadataService.AddHotel(
                "SYN-ARR-001",
                "Synthetic Arrival Hotel",
                pms.PmsName);

            using (QaReportForm form = new(
                       metadataService,
                       metadataService.LoadPmsSystems(),
                       metadataService.LoadHotels(),
                       paths))
            {
                QaChecklistItemControl[] controls =
                    Descendants(form)
                        .OfType<QaChecklistItemControl>()
                        .ToArray();
                Check(
                    controls.Length == QaChecklistCatalog.Definitions.Count,
                    $"Form created {controls.Length} checklist rows, expected " +
                    $"{QaChecklistCatalog.Definitions.Count}.");
                Check(
                    controls.Select(control => control.CheckId)
                        .ToHashSet(StringComparer.Ordinal)
                        .SetEquals(QaChecklistCatalog.Definitions.Select(
                            definition => definition.Id)),
                    "Form checklist rows do not exactly match the active catalog.");
                Check(
                    !controls.Any(control =>
                        control.CheckId == RetiredCheckId),
                    "The form created a hidden or active retired checklist row.");
                Check(
                    !Descendants(form).Any(control =>
                        control.Text.Contains(
                            RetiredDisplayName,
                            StringComparison.Ordinal)),
                    "The retired checklist display text remains in the form.");
                Check(
                    form.CurrentReport.ChecklistResults.Count ==
                        QaChecklistCatalog.ExpectedDefinitionCount
                    && !form.CurrentReport.ChecklistResults.Any(result =>
                        result.CheckId == RetiredCheckId),
                    "A new form report did not create exactly the active results.");
            }

            QaReportSaveService saveService = new(
                paths,
                metadataService,
                sanitizer);
            QaReportIndexService indexService = new(paths);
            EvidenceScenario[] scenarios =
            [
                new("Arrival outside 20 percent", new QaFileMonth(2026, 1), 80, 20, false, QaReportStatus.Pass),
                new("Arrival outside exactly 30 percent", new QaFileMonth(2026, 2), 70, 30, false, QaReportStatus.Pass),
                new("Arrival outside 31 percent active", new QaFileMonth(2026, 3), 69, 31, false, QaReportStatus.Fail),
                new("Arrival outside 31 percent handled", new QaFileMonth(2026, 4), 69, 31, true, QaReportStatus.PassWithWarnings)
            ];

            for (int index = 0; index < scenarios.Length; index++)
            {
                EvidenceScenario scenario = scenarios[index];
                QaReport report = CreateCompleteReport(
                    scenario.FileMonth,
                    valid: 100,
                    scenario.Inside,
                    scenario.Outside,
                    customScriptSupport: scenario.Handled);
                report.HotelInformation.HotelId = hotel.HotelId;
                report.HotelInformation.HotelName = hotel.HotelName;
                report.HotelInformation.PmsName = hotel.PmsName;

                if (scenario.Handled)
                {
                    QaFinding failure = ArrivalFailure(report);
                    failure.Resolution =
                        QaFindingResolution.HandledByCustomScript;
                    failure.CustomScriptName = "arrival-month-fix.csx";
                    failure.ResolutionNotes =
                        "Synthetic aggregate arrival-month correction.";
                    Synchronize(report);
                }

                QaReportValidationResult validation = ValidationService.Validate(
                    report,
                    metadataService.LoadHotels());
                Check(
                    validation.IsReady
                    && validation.CalculatedStatus == scenario.ExpectedStatus,
                    $"{scenario.Name}: readiness/status mismatch: " +
                    string.Join(" | ", validation.BlockingErrors));
                report.ReportStatus = validation.CalculatedStatus;

                DateTimeOffset generatedAt = new(
                    2026,
                    7,
                    23,
                    13,
                    index,
                    0,
                    TimeSpan.Zero);
                byte[] pdfBytes = PdfService.GeneratePdf(
                    report,
                    validation,
                    generatedAt);
                Check(pdfBytes.Length > 1_000, $"{scenario.Name}: PDF payload is unexpectedly small.");

                QaReportSavePreparation preparation = saveService.Prepare(
                    new QaReportSaveRequest(
                        report,
                        validation,
                        generatedAt));
                Check(
                    !preparation.RequiresOverwriteConfirmation
                    && preparation.FinalStatus == scenario.ExpectedStatus,
                    $"{scenario.Name}: fresh save preparation is incorrect.");
                QaReportSaveResult saved = saveService.Save(
                    preparation,
                    pdfBytes,
                    overwriteConfirmed: false);
                Check(
                    saved.Success
                    && saved.FinalStatus == scenario.ExpectedStatus,
                    $"{scenario.Name}: paired save did not preserve final status.");

                byte[] hotelBytes = File.ReadAllBytes(saved.HotelCopyPath);
                byte[] pmsBytes = File.ReadAllBytes(saved.PmsCopyPath);
                string hotelHash = Sha256(hotelBytes);
                string pmsHash = Sha256(pmsBytes);
                Check(
                    hotelBytes.AsSpan().SequenceEqual(pdfBytes)
                    && pmsBytes.AsSpan().SequenceEqual(pdfBytes)
                    && hotelHash == pmsHash,
                    $"{scenario.Name}: Hotel/PMS PDFs are not byte-identical.");

                QaReportIndexEntry[] entries = indexService.LoadEntries()
                    .Where(entry => entry.ReportKey == saved.ReportKey)
                    .ToArray();
                Check(
                    entries.Length == 1
                    && entries[0].Status == scenario.ExpectedStatus,
                    $"{scenario.Name}: QA index status/count is incorrect.");

                string line =
                    $"{scenario.Name} | status={scenario.ExpectedStatus} | " +
                    $"hotel={saved.HotelCopyPath} | pms={saved.PmsCopyPath} | " +
                    $"bytes={pdfBytes.Length} | SHA-256={hotelHash}";
                manifestLines.Add(line);
                output.WriteLine($"[EVIDENCE] {line}");
            }

            IReadOnlyList<QaReportIndexEntry> finalEntries =
                indexService.LoadEntries();
            Check(
                finalEntries.Count == scenarios.Length,
                $"Final QA index contains {finalEntries.Count} entries, expected {scenarios.Length}.");
            Check(
                Directory.EnumerateFiles(
                        paths.ByHotelRootPath,
                        "*.pdf",
                        SearchOption.AllDirectories)
                    .Count() == scenarios.Length
                && Directory.EnumerateFiles(
                        paths.ByPmsRootPath,
                        "*.pdf",
                        SearchOption.AllDirectories)
                    .Count() == scenarios.Length,
                "The paired destinations do not contain exactly four representative PDFs each.");
            Check(
                !Directory.EnumerateFiles(
                        paths.DocumentationRootPath,
                        "*",
                        SearchOption.AllDirectories)
                    .Any(IsTransactionArtifact),
                "Transaction, temporary, rollback, or backup artifacts remain.");

            string manifestPath = Path.Combine(
                root,
                "arrival-month-evidence-manifest.txt");
            File.WriteAllLines(
                manifestPath,
                manifestLines,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            output.WriteLine($"[EVIDENCE] Manifest: {manifestPath}");
            output.WriteLine($"[EVIDENCE] Root: {root}");
        }
        finally
        {
            if (!preserve && Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static QaReport CreateCompleteReport(
        QaFileMonth fileMonth,
        int valid,
        int inside,
        int outside,
        bool customScriptSupport = false)
    {
        QaReport report = new()
        {
            ReportId = $"synthetic-arrival-{Guid.NewGuid():N}",
            FileId = "001234",
            HotelInformation = new QaHotelInformation
            {
                HotelId = "SYN-ARR-001",
                HotelName = "Synthetic Arrival Hotel",
                PmsName = "Synthetic PMS",
                FileMonth = fileMonth
            },
            QaDate = new DateOnly(2026, 7, 23),
            CreatedBy = "Synthetic QA",
            OriginalFileName = "synthetic-arrival-aggregate.csv",
            FileCharacteristics = new QaFileCharacteristics
            {
                NameColumnMode =
                    QaNameColumnMode.SeparateFirstAndLastName,
                HasCurrencyColumn = false,
                MonetaryColumnScenario =
                    QaMonetaryColumnScenario.OneMonetaryColumn,
                HasMultipleConfirmationNumberCandidateColumns = false,
                IsCustomScriptSupportAvailable = customScriptSupport,
                HasRejectedDatabaseRecords = false
            },
            Statistics = new QaStatistics
            {
                FileInformation = new QaFileInformationStatistics
                {
                    TotalDataRows = 100,
                    HeadersArePresent = true,
                    UsefulHeaders = QaUsefulHeadersResult.Yes,
                    DataStartRow = 2
                },
                FileMonth = new QaFileMonthStatistics
                {
                    ValidArrivalDateCount = valid,
                    ArrivalDatesWithinFileMonth = inside,
                    ArrivalDatesOutsideFileMonth = outside
                },
                Database = new QaDatabaseStatistics
                {
                    ImportedRecordCount = 100,
                    RejectedRecordCount = 0,
                    RecordsWithMissingRequiredDatabaseValues = 0
                }
            },
            GeneralNotes =
                "Synthetic aggregate counts only; no guest-level data."
        };

        foreach (QaCheckDefinition definition
                 in QaChecklistCatalog.Definitions)
        {
            report.ChecklistResults.Add(new QaCheckResult
            {
                CheckId = definition.Id,
                Status = IsApplicable(
                    definition,
                    report.FileCharacteristics)
                        ? QaCheckStatus.Pass
                        : QaCheckStatus.NotApplicable,
                ResultSource = QaResultSource.Manual
            });
        }

        Synchronize(report);
        return report;
    }

    private static void Synchronize(QaReport report)
    {
        CalculationService.Synchronize(report);
        FindingService.SynchronizeFindings(report);
    }

    private static QaReportValidationResult Validate(QaReport report)
    {
        return ValidationService.Validate(
            report,
            [
                new QaHotelMetadata
                {
                    HotelId = "SYN-ARR-001",
                    HotelName = "Synthetic Arrival Hotel",
                    PmsName = "Synthetic PMS",
                    FolderName =
                        "Synthetic Arrival Hotel - SYN-ARR-001"
                }
            ]);
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
                characteristics.NameColumnMode ==
                QaNameColumnMode.FullName,
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
            _ => false
        };
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;

            foreach (Control descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static bool HasArrivalFailure(QaReport report)
    {
        return report.Findings.Any(finding =>
            finding.FindingId == FindingId);
    }

    private static QaFinding ArrivalFailure(QaReport report)
    {
        return report.Findings.Single(finding =>
            finding.FindingId == FindingId);
    }

    private static bool IsTransactionArtifact(string path)
    {
        string name = Path.GetFileName(path);
        return name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".qa-tmp", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(
                ".qa-rollback",
                StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return Path.TrimEndingDirectorySeparator(
                    Path.GetFullPath(directory.FullName));
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bool IsWithinOrEqual(string candidate, string parent)
    {
        string normalizedCandidate = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(candidate));
        string normalizedParent = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(parent));

        if (normalizedCandidate.Equals(
                normalizedParent,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalizedCandidate.StartsWith(
            normalizedParent + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string Sha256(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new ArrivalMonthAssertionException(message);
        }
    }

    private sealed record EvidenceScenario(
        string Name,
        QaFileMonth FileMonth,
        int Inside,
        int Outside,
        bool Handled,
        QaReportStatus ExpectedStatus);

    private sealed class ArrivalMonthAssertionException(string message)
        : Exception(message);
}
