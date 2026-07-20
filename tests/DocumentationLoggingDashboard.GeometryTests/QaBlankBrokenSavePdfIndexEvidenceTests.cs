using System.Security.Cryptography;
using System.Text;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.GeometryTests;

internal static class QaBlankBrokenSavePdfIndexEvidenceTests
{
    private const string TemporaryParentName =
        "DocumentationLoggingDashboard.SemanticRegressionTests";
    private const string FrozenPilotPackageDirectoryName =
        "DocumentationLoggingDashboard-V2-Pilot-Package";

    private static readonly QaStatisticsCalculationService CalculationService = new();
    private static readonly QaFindingSynchronizationService FindingService = new();
    private static readonly QaReportValidationService ValidationService = new();
    private static readonly QaPdfGenerationService PdfService = new();

    private static readonly IReadOnlyList<SaveScenario> Scenarios =
    [
        new(
            "Blank Warning only",
            BlankCount: 20,
            BrokenCount: 0,
            QaReportStatus.PassWithWarnings,
            WarningCount: 1,
            FailureCount: 0,
            HandledCount: 0,
            HandleBrokenFailure: false,
            UseMappedFirstName: false),
        new(
            "Broken Warning only",
            BlankCount: 0,
            BrokenCount: 20,
            QaReportStatus.PassWithWarnings,
            WarningCount: 1,
            FailureCount: 0,
            HandledCount: 0,
            HandleBrokenFailure: false,
            UseMappedFirstName: false),
        new(
            "Blank and Broken Warnings",
            BlankCount: 20,
            BrokenCount: 10,
            QaReportStatus.PassWithWarnings,
            WarningCount: 2,
            FailureCount: 0,
            HandledCount: 0,
            HandleBrokenFailure: false,
            UseMappedFirstName: false),
        new(
            "Blank Failure",
            BlankCount: 51,
            BrokenCount: 0,
            QaReportStatus.Fail,
            WarningCount: 0,
            FailureCount: 1,
            HandledCount: 0,
            HandleBrokenFailure: false,
            UseMappedFirstName: false),
        new(
            "Broken Failure",
            BlankCount: 0,
            BrokenCount: 51,
            QaReportStatus.Fail,
            WarningCount: 0,
            FailureCount: 1,
            HandledCount: 0,
            HandleBrokenFailure: false,
            UseMappedFirstName: true),
        new(
            "Handled Broken Failure",
            BlankCount: 0,
            BrokenCount: 51,
            QaReportStatus.PassWithWarnings,
            WarningCount: 0,
            FailureCount: 1,
            HandledCount: 1,
            HandleBrokenFailure: true,
            UseMappedFirstName: true),
        new(
            "Zero Blank and Broken findings",
            BlankCount: 0,
            BrokenCount: 0,
            QaReportStatus.Pass,
            WarningCount: 0,
            FailureCount: 0,
            HandledCount: 0,
            HandleBrokenFailure: false,
            UseMappedFirstName: false)
    ];

    private static readonly IReadOnlyList<DeferredTextSaveScenario> DeferredTextScenarios =
    [
        new(
            "Pass report with General Notes",
            DeferredTextSaveKind.GeneralNotes,
            QaReportStatus.Pass,
            WarningCount: 0,
            FailureCount: 0,
            HandledCount: 0,
            ["DEFERRED-GENERAL-FINAL-Z"]),
        new(
            "Explained and Accepted Warning notes",
            DeferredTextSaveKind.ExplainedWarning,
            QaReportStatus.PassWithWarnings,
            WarningCount: 1,
            FailureCount: 0,
            HandledCount: 1,
            ["DEFERRED-EXPLAINED-FINAL-Z"]),
        new(
            "Handled Failure with custom-script text",
            DeferredTextSaveKind.HandledFailure,
            QaReportStatus.PassWithWarnings,
            WarningCount: 0,
            FailureCount: 1,
            HandledCount: 1,
            ["DeferredScriptFinalZ.csx", "DEFERRED-HANDLED-FINAL-Z"]),
        new(
            "Active Failure with resolution notes",
            DeferredTextSaveKind.ActiveFailure,
            QaReportStatus.Fail,
            WarningCount: 0,
            FailureCount: 1,
            HandledCount: 0,
            ["DEFERRED-ACTIVE-FAILURE-FINAL-Z"]),
        new(
            "Checklist Warning with long explanation",
            DeferredTextSaveKind.ChecklistWarning,
            QaReportStatus.PassWithWarnings,
            WarningCount: 1,
            FailureCount: 0,
            HandledCount: 0,
            ["DEFERRED-CHECKLIST-WARNING-FINAL-Z"])
    ];

    public static int RunAll(
        TextWriter output,
        string? preservedEvidenceParentDirectory)
    {
        ArgumentNullException.ThrowIfNull(output);

        List<string> evidenceLines = [];
        void Record(string line)
        {
            output.WriteLine(line);
            evidenceLines.Add(line);
        }

        EvidenceRoot? evidenceRoot = null;
        int result = 0;

        try
        {
            evidenceRoot = CreateEvidenceRoot(preservedEvidenceParentDirectory);
            Record("[RUN ] Complete synthetic save/PDF/index matrix");
            Record($"Evidence root: {evidenceRoot.RootPath}");
            Record(evidenceRoot.Preserve
                ? "Evidence retention: preserved external evidence mode"
                : "Evidence retention: disposable system-temporary mode");

            RunMatrix(evidenceRoot.RootPath, Record);
            Record(
                $"[PASS] All {Scenarios.Count} Blank/Broken and " +
                $"{DeferredTextScenarios.Count} deferred-text save/PDF/index cases passed.");
        }
        catch (Exception exception)
        {
            result = 1;
            Record("[FAIL] Complete synthetic save/PDF/index matrix");
            Record(exception.ToString());
        }
        finally
        {
            if (evidenceRoot is not null)
            {
                if (evidenceRoot.Preserve)
                {
                    try
                    {
                        string manifestPath = Path.Combine(
                            evidenceRoot.RootPath,
                            "evidence-manifest.txt");
                        Record($"Evidence manifest: {manifestPath}");
                        File.WriteAllLines(
                            manifestPath,
                            evidenceLines,
                            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                        output.WriteLine($"Preserved evidence root: {evidenceRoot.RootPath}");
                    }
                    catch (Exception exception)
                    {
                        result = 1;
                        output.WriteLine(
                            $"[FAIL] Preserved evidence manifest could not be written: {exception}");
                        output.WriteLine($"Preserved evidence root: {evidenceRoot.RootPath}");
                    }
                }
                else
                {
                    try
                    {
                        DeleteDisposableEvidenceRoot(evidenceRoot);
                        output.WriteLine(
                            $"Disposable evidence root removed: {evidenceRoot.RootPath}");
                    }
                    catch (Exception exception)
                    {
                        result = 1;
                        output.WriteLine(
                            $"[FAIL] Disposable evidence cleanup failed: {exception}");
                        output.WriteLine(
                            $"Unremoved disposable evidence root: {evidenceRoot.RootPath}");
                    }
                }
            }
        }

        return result;
    }

    private static void RunMatrix(string rootPath, Action<string> record)
    {
        QaStoragePaths paths = new(rootPath);
        new QaStorageInitializer(paths).Initialize();

        QaFolderNameSanitizer sanitizer = new();
        QaMetadataService metadataService = new(paths, sanitizer);
        QaPmsMetadata canonicalPms = metadataService.AddPmsSystem("Synthetic PMS");
        QaHotelMetadata canonicalHotel = metadataService.AddHotel(
            "SYN-001",
            "Synthetic Hotel",
            canonicalPms.PmsName);
        QaPmsMetadata punctuationPms = metadataService.AddPmsSystem(
            "Opera Cloud - QA");
        QaPmsMetadata spacedPms = metadataService.AddPmsSystem(
            "Mews Synthetic 3");
        _ = metadataService.AddHotel(
            "SYN 002",
            "Harbour & Pine, Toronto",
            canonicalPms.PmsName);
        _ = metadataService.AddHotel(
            "SYN-003",
            "O'Neil Suites (East)",
            punctuationPms.PmsName);
        _ = metadataService.AddHotel(
            "SYN.004",
            "Quebec QA - Centre",
            spacedPms.PmsName);

        IReadOnlyList<QaPmsMetadata> initializedPms = metadataService.LoadPmsSystems();
        IReadOnlyList<QaHotelMetadata> initializedHotels = metadataService.LoadHotels();
        Check(initializedPms.Count == 3, "Synthetic root did not initialize three PMS systems.");
        Check(initializedHotels.Count == 4, "Synthetic root did not initialize four Hotels.");
        Check(
            initializedHotels.Count(hotel => hotel.PmsName == canonicalPms.PmsName) == 2,
            "Synthetic root did not initialize multiple Hotels sharing one PMS.");
        QaReportSaveService saveService = new(paths, metadataService, sanitizer);
        QaReportIndexService indexService = new(paths);
        List<ExpectedIndexEntry> expectedEntries = [];

        for (int index = 0; index < Scenarios.Count; index++)
        {
            SaveScenario scenario = Scenarios[index];
            int caseNumber = index + 1;
            QaFileMonth fileMonth = new(2026, caseNumber);
            QaReport report = CreateCompleteReport(
                canonicalHotel,
                fileMonth,
                scenario,
                caseNumber);

            string statisticFieldId = scenario.UseMappedFirstName
                ? QaStatisticFieldIds.FirstName
                : QaStatisticFieldIds.Email;
            QaBlankValueStatistic selectedBlank = report.Statistics.BlankValues.Single(
                row => row.FieldId == statisticFieldId);
            QaBrokenDataStatistic selectedBroken = report.Statistics.BrokenData.Single(
                row => row.FieldId == statisticFieldId);
            Check(
                selectedBlank.UseAutomaticTotalApplicableRows
                && selectedBlank.TotalApplicableRows == 100,
                $"{scenario.Name}: selected Blank denominator was not the automatic Total Data Rows value.");
            Check(
                selectedBroken.UseAutomaticTotalApplicableNonblankValues
                && selectedBroken.TotalApplicableNonblankValues
                    == 100 - scenario.BlankCount,
                $"{scenario.Name}: selected Broken denominator was not the automatic nonblank value.");

            IReadOnlyList<QaHotelMetadata> canonicalHotels = metadataService.LoadHotels();
            QaReportValidationResult validationResult = ValidationService.Validate(
                report,
                canonicalHotels);
            AssertValidation(scenario, validationResult);

            report.ReportStatus = validationResult.CalculatedStatus;
            DateTimeOffset generatedAt = new(
                year: 2026,
                month: 7,
                day: 17,
                hour: 16,
                minute: caseNumber,
                second: 0,
                offset: TimeSpan.Zero);
            byte[] pdfBytes = PdfService.GeneratePdf(
                report,
                validationResult,
                generatedAt);
            AssertPdfPayload(pdfBytes, scenario.Name);

            QaReportSaveRequest request = new(
                report,
                validationResult,
                generatedAt);
            QaReportSavePreparation preparation = saveService.Prepare(request);
            Check(
                !preparation.RequiresOverwriteConfirmation,
                $"{scenario.Name}: a fresh logical report unexpectedly required overwrite confirmation.");
            Check(
                preparation.FinalStatus == scenario.ExpectedStatus,
                $"{scenario.Name}: prepared status was {preparation.FinalStatus}, expected {scenario.ExpectedStatus}.");

            QaReportSaveResult saveResult = saveService.Save(
                preparation,
                pdfBytes,
                overwriteConfirmed: false);
            AssertSaveResult(scenario, saveResult, pdfBytes.Length, generatedAt);
            AssertPathInsideRoot(saveResult.HotelCopyPath, rootPath, "Hotel PDF");
            AssertPathInsideRoot(saveResult.PmsCopyPath, rootPath, "PMS PDF");

            byte[] hotelBytes = File.ReadAllBytes(saveResult.HotelCopyPath);
            byte[] pmsBytes = File.ReadAllBytes(saveResult.PmsCopyPath);
            Check(
                hotelBytes.AsSpan().SequenceEqual(pdfBytes),
                $"{scenario.Name}: final Hotel PDF differs from the generated payload.");
            Check(
                pmsBytes.AsSpan().SequenceEqual(pdfBytes),
                $"{scenario.Name}: final PMS PDF differs from the generated payload.");
            Check(
                hotelBytes.AsSpan().SequenceEqual(pmsBytes),
                $"{scenario.Name}: final Hotel and PMS PDFs are not byte-identical.");

            string payloadHash = Sha256(pdfBytes);
            string hotelHash = Sha256(hotelBytes);
            string pmsHash = Sha256(pmsBytes);
            Check(
                payloadHash == hotelHash && payloadHash == pmsHash,
                $"{scenario.Name}: final PDF hashes do not match the generated payload.");

            IReadOnlyList<QaReportIndexEntry> currentEntries = indexService.LoadEntries();
            Check(
                currentEntries.Count == caseNumber,
                $"{scenario.Name}: index contains {currentEntries.Count} entries after case {caseNumber}, expected {caseNumber}.");
            QaReportIndexEntry indexEntry = AssertSingleIndexEntry(
                paths,
                currentEntries,
                saveResult,
                generatedAt,
                scenario);
            expectedEntries.Add(new ExpectedIndexEntry(
                saveResult.ReportKey,
                scenario.ExpectedStatus));

            record($"[CASE] {caseNumber}: {scenario.Name}");
            record(
                $"  Status: expected={scenario.ExpectedStatus}; " +
                $"validated={validationResult.CalculatedStatus}; " +
                $"saved={saveResult.FinalStatus}; indexed={indexEntry.Status}");
            record(
                $"  Findings: warnings={validationResult.WarningFindingCount}; " +
                $"failures={validationResult.FailureFindingCount}; " +
                $"handled={validationResult.HandledFindingCount}");
            record($"  Logical key: {saveResult.ReportKey}");
            record(
                $"  Hotel PDF: {saveResult.HotelCopyPath} | " +
                $"bytes={hotelBytes.Length} | SHA-256={hotelHash}");
            record(
                $"  PMS PDF: {saveResult.PmsCopyPath} | " +
                $"bytes={pmsBytes.Length} | SHA-256={pmsHash}");
            record($"  Generated payload: bytes={pdfBytes.Length} | SHA-256={payloadHash}");
        }


        RunDeferredTextMatrix(
            paths,
            metadataService,
            canonicalHotel,
            saveService,
            indexService,
            expectedEntries,
            record);

        RunBlockedSaveGuards(
            paths,
            metadataService,
            canonicalHotel,
            saveService,
            record);
        RunOverwriteMatrix(
            paths,
            metadataService,
            canonicalHotel,
            saveService,
            indexService,
            expectedEntries,
            record);

        AssertFinalEvidence(paths, indexService, expectedEntries, record);
    }

    private static void RunBlockedSaveGuards(
        QaStoragePaths paths,
        QaMetadataService metadataService,
        QaHotelMetadata canonicalHotel,
        QaReportSaveService saveService,
        Action<string> record)
    {
        SaveScenario passScenario = new(
            "Blocked-save baseline",
            BlankCount: 0,
            BrokenCount: 0,
            QaReportStatus.Pass,
            WarningCount: 0,
            FailureCount: 0,
            HandledCount: 0,
            HandleBrokenFailure: false,
            UseMappedFirstName: false);
        (string Name, Action<QaReport> Mutate, string ExpectedError)[] cases =
        [
            (
                "Numerator greater than denominator",
                report =>
                {
                    QaBlankValueStatistic row = report.Statistics.BlankValues.Single(
                        statistic => statistic.FieldId == QaStatisticFieldIds.Email);
                    row.BlankCount = row.TotalApplicableRows + 1;
                },
                "cannot exceed"),
            (
                "Positive count with zero denominator",
                report =>
                {
                    QaBlankValueStatistic row = report.Statistics.BlankValues.Single(
                        statistic => statistic.FieldId == QaStatisticFieldIds.Email);
                    row.UseAutomaticTotalApplicableRows = false;
                    row.TotalApplicableRows = 0;
                    row.BlankCount = 1;
                },
                "required when data rows exist"),
            (
                "Applicable checklist item Not Evaluated",
                report => report.ChecklistResults.First(result =>
                    result.Status == QaCheckStatus.Pass).Status = QaCheckStatus.NotEvaluated,
                "has not been evaluated"),
            (
                "Above-50 Broken value contradicts checklist Pass",
                report =>
                {
                    QaBrokenDataStatistic row = report.Statistics.BrokenData.Single(
                        statistic => statistic.FieldId == QaStatisticFieldIds.FirstName);
                    row.BrokenValueCount = 51;
                },
                "requires checklist item"),
            (
                "Missing required report details",
                report => report.HotelInformation.HotelId = string.Empty,
                "Hotel ID")
        ];

        int caseNumber = 80;
        foreach ((string name, Action<QaReport> mutate, string expectedError) in cases)
        {
            caseNumber++;
            QaReport report = CreateCompleteReport(
                canonicalHotel,
                new QaFileMonth(2028, caseNumber - 80),
                passScenario,
                caseNumber);
            mutate(report);
            CalculationService.Synchronize(report);
            FindingService.SynchronizeFindings(report);
            QaReportValidationResult validation = ValidationService.Validate(
                report,
                metadataService.LoadHotels());
            Check(!validation.IsReady, $"{name}: invalid report was marked ready.");
            Check(
                validation.BlockingErrors.Any(error => error.Contains(
                    expectedError,
                    StringComparison.OrdinalIgnoreCase)),
                $"{name}: expected blocker containing '{expectedError}' was absent: " +
                string.Join(" | ", validation.BlockingErrors));

            string snapshotBefore = SnapshotOutputTree(paths.DocumentationRootPath);
            bool pdfBlocked = false;
            try
            {
                _ = PdfService.GeneratePdf(
                    report,
                    validation,
                    new DateTimeOffset(2026, 7, 20, 18, caseNumber - 80, 0, TimeSpan.Zero));
            }
            catch (QaPdfGenerationException)
            {
                pdfBlocked = true;
            }

            Check(pdfBlocked, $"{name}: PDF generation was not blocked.");

            bool saveBlocked = false;
            try
            {
                _ = saveService.Prepare(new QaReportSaveRequest(
                    report,
                    validation,
                    new DateTimeOffset(2026, 7, 20, 18, caseNumber - 80, 0, TimeSpan.Zero)));
            }
            catch (QaReportSaveException)
            {
                saveBlocked = true;
            }

            Check(saveBlocked, $"{name}: save preparation was not blocked.");
            Check(
                SnapshotOutputTree(paths.DocumentationRootPath) == snapshotBefore,
                $"{name}: blocked report changed PDF, index, or metadata output.");
            record($"[BLOCKED] {name}: no PDF or index change");
        }
    }

    private static void RunOverwriteMatrix(
        QaStoragePaths paths,
        QaMetadataService metadataService,
        QaHotelMetadata canonicalHotel,
        QaReportSaveService saveService,
        QaReportIndexService indexService,
        List<ExpectedIndexEntry> expectedEntries,
        Action<string> record)
    {
        SaveScenario passScenario = new(
            "Overwrite Pass",
            BlankCount: 0,
            BrokenCount: 0,
            QaReportStatus.Pass,
            WarningCount: 0,
            FailureCount: 0,
            HandledCount: 0,
            HandleBrokenFailure: false,
            UseMappedFirstName: false);
        QaFileMonth logicalMonth = new(2027, 1);

        QaReport original = CreateCompleteReport(
            canonicalHotel,
            logicalMonth,
            passScenario,
            caseNumber: 91);
        (QaReportValidationResult originalValidation, byte[] originalPdf) =
            ValidateAndGenerate(original, metadataService, 19, 1);
        QaReportSaveResult originalSave = saveService.Save(
            saveService.Prepare(new QaReportSaveRequest(
                original,
                originalValidation,
                new DateTimeOffset(2026, 7, 20, 19, 1, 0, TimeSpan.Zero))),
            originalPdf,
            overwriteConfirmed: false);
        expectedEntries.Add(new ExpectedIndexEntry(originalSave.ReportKey, QaReportStatus.Pass));

        QaReport exactReplacement = CreateCompleteReport(
            canonicalHotel,
            logicalMonth,
            passScenario,
            caseNumber: 92);
        exactReplacement.QaDate = original.QaDate;
        exactReplacement.GeneralNotes = "Synthetic exact-key replacement payload.";
        (QaReportValidationResult exactValidation, byte[] exactPdf) =
            ValidateAndGenerate(exactReplacement, metadataService, 19, 2);
        string noSnapshot = SnapshotOutputTree(paths.DocumentationRootPath);
        QaReportSavePreparation exactPreparation = saveService.Prepare(
            new QaReportSaveRequest(
                exactReplacement,
                exactValidation,
                new DateTimeOffset(2026, 7, 20, 19, 2, 0, TimeSpan.Zero)));
        Check(exactPreparation.RequiresOverwriteConfirmation, "Exact-key replacement was not detected.");
        Check(
            SnapshotOutputTree(paths.DocumentationRootPath) == noSnapshot,
            "Overwrite No decision changed files or index.");
        record("[OVERWRITE] Exact Hotel ID/File Month/QA Date No: files and index unchanged");

        QaReportSaveResult exactSave = saveService.Save(
            exactPreparation,
            exactPdf,
            overwriteConfirmed: true);
        AssertOverwriteResult(exactSave, "Exact-key Yes");
        AssertOneLogicalPair(paths, indexService, exactSave.ReportKey, exactSave);
        record("[OVERWRITE] Exact Hotel ID/File Month/QA Date Yes: one logical pair and one index entry");

        QaReport dateReplacement = CreateCompleteReport(
            canonicalHotel,
            logicalMonth,
            passScenario,
            caseNumber: 93);
        dateReplacement.QaDate = original.QaDate.AddDays(1);
        dateReplacement.GeneralNotes = "Synthetic different-QA-date replacement payload.";
        (QaReportValidationResult dateValidation, byte[] datePdf) =
            ValidateAndGenerate(dateReplacement, metadataService, 19, 3);
        QaReportSavePreparation datePreparation = saveService.Prepare(
            new QaReportSaveRequest(
                dateReplacement,
                dateValidation,
                new DateTimeOffset(2026, 7, 20, 19, 3, 0, TimeSpan.Zero)));
        Check(
            datePreparation.RequiresOverwriteConfirmation,
            "Different QA Date for the same logical report was not detected.");
        QaReportSaveResult dateSave = saveService.Save(
            datePreparation,
            datePdf,
            overwriteConfirmed: true);
        AssertOverwriteResult(dateSave, "Different-QA-date Yes");
        AssertOneLogicalPair(paths, indexService, dateSave.ReportKey, dateSave);
        RecordPair(dateSave, record, "Overwrite final different-QA-date pair");

        QaReport differentKey = CreateCompleteReport(
            canonicalHotel,
            new QaFileMonth(2027, 2),
            passScenario,
            caseNumber: 94);
        (QaReportValidationResult differentValidation, byte[] differentPdf) =
            ValidateAndGenerate(differentKey, metadataService, 19, 4);
        QaReportSavePreparation differentPreparation = saveService.Prepare(
            new QaReportSaveRequest(
                differentKey,
                differentValidation,
                new DateTimeOffset(2026, 7, 20, 19, 4, 0, TimeSpan.Zero)));
        Check(
            !differentPreparation.RequiresOverwriteConfirmation,
            "A different report key produced a false overwrite detection.");
        QaReportSaveResult differentSave = saveService.Save(
            differentPreparation,
            differentPdf,
            overwriteConfirmed: false);
        expectedEntries.Add(new ExpectedIndexEntry(differentSave.ReportKey, QaReportStatus.Pass));
        RecordPair(differentSave, record, "Different report-key pair");
        record("[OVERWRITE] Different report key: no false overwrite detection");
    }

    private static (QaReportValidationResult Validation, byte[] Pdf) ValidateAndGenerate(
        QaReport report,
        QaMetadataService metadataService,
        int hour,
        int minute)
    {
        CalculationService.Synchronize(report);
        FindingService.SynchronizeFindings(report);
        QaReportValidationResult validation = ValidationService.Validate(
            report,
            metadataService.LoadHotels());
        Check(
            validation.IsReady && validation.CalculatedStatus is not null,
            "Overwrite synthetic report was not ready: " +
            string.Join(" | ", validation.BlockingErrors));
        report.ReportStatus = validation.CalculatedStatus;
        byte[] pdf = PdfService.GeneratePdf(
            report,
            validation,
            new DateTimeOffset(2026, 7, 20, hour, minute, 0, TimeSpan.Zero));
        return (validation, pdf);
    }

    private static void AssertOverwriteResult(QaReportSaveResult result, string context)
    {
        Check(result.Success && !result.Cancelled, $"{context}: replacement did not succeed.");
        Check(result.OverwriteOccurred, $"{context}: replacement was not recorded as overwrite.");
        Check(
            result.HotelMatchesReplaced == 1 && result.PmsMatchesReplaced == 1,
            $"{context}: expected one replaced file in each destination.");
        Check(
            result.CleanupWarning is null && !result.ManualReviewRequired,
            $"{context}: cleanup or manual review was required.");
    }

    private static void AssertOneLogicalPair(
        QaStoragePaths paths,
        QaReportIndexService indexService,
        QaReportKey reportKey,
        QaReportSaveResult saveResult)
    {
        Check(File.Exists(saveResult.HotelCopyPath), "Overwrite final Hotel PDF is missing.");
        Check(File.Exists(saveResult.PmsCopyPath), "Overwrite final PMS PDF is missing.");
        byte[] hotel = File.ReadAllBytes(saveResult.HotelCopyPath);
        byte[] pms = File.ReadAllBytes(saveResult.PmsCopyPath);
        Check(hotel.AsSpan().SequenceEqual(pms), "Overwrite final Hotel/PMS PDFs differ.");
        Check(
            indexService.LoadEntries().Count(entry => entry.ReportKey == reportKey) == 1,
            "Overwrite final QA index does not contain exactly one logical entry.");

        QaReportExistingFileService existing = new(
            paths,
            new QaReportFilenameParser(new QaFolderNameSanitizer()));
        Check(
            existing.FindExistingFiles(
                    new QaHotelMetadata
                    {
                        HotelId = saveResult.ReportKey.HotelId,
                        PmsName = "Synthetic overwrite PMS",
                        FolderName = Path.GetFileName(Path.GetDirectoryName(saveResult.HotelCopyPath))!
                    },
                    new QaPmsMetadata
                    {
                        PmsName = "Synthetic overwrite PMS",
                        FolderName = Path.GetFileName(Path.GetDirectoryName(saveResult.PmsCopyPath))!
                    },
                    reportKey)
                is QaReportExistingFiles files
                && files.HotelFilePaths.Count == 1
                && files.PmsFilePaths.Count == 1,
            "Overwrite final storage does not contain exactly one logical pair.");
    }

    private static void RecordPair(
        QaReportSaveResult result,
        Action<string> record,
        string label)
    {
        byte[] hotel = File.ReadAllBytes(result.HotelCopyPath);
        byte[] pms = File.ReadAllBytes(result.PmsCopyPath);
        Check(hotel.AsSpan().SequenceEqual(pms), $"{label}: Hotel/PMS PDFs differ.");
        record($"[PAIR] {label}");
        record($"  Hotel PDF: {result.HotelCopyPath} | bytes={hotel.Length} | SHA-256={Sha256(hotel)}");
        record($"  PMS PDF: {result.PmsCopyPath} | bytes={pms.Length} | SHA-256={Sha256(pms)}");
    }

    private static string SnapshotOutputTree(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            return string.Empty;
        }

        return string.Join(
            "\n",
            Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path =>
                {
                    byte[] content = File.ReadAllBytes(path);
                    return Path.GetRelativePath(rootPath, path)
                        + "|" + content.Length
                        + "|" + Sha256(content);
                }));
    }

    private static void RunDeferredTextMatrix(
        QaStoragePaths paths,
        QaMetadataService metadataService,
        QaHotelMetadata canonicalHotel,
        QaReportSaveService saveService,
        QaReportIndexService indexService,
        List<ExpectedIndexEntry> expectedEntries,
        Action<string> record)
    {
        for (int index = 0; index < DeferredTextScenarios.Count; index++)
        {
            DeferredTextSaveScenario scenario = DeferredTextScenarios[index];
            int caseNumber = Scenarios.Count + index + 1;
            QaFileMonth fileMonth = new(2026, caseNumber);
            SaveScenario baseline = new(
                scenario.Name,
                BlankCount: 0,
                BrokenCount: 0,
                scenario.ExpectedStatus,
                scenario.WarningCount,
                scenario.FailureCount,
                scenario.HandledCount,
                HandleBrokenFailure: false,
                UseMappedFirstName: false);
            QaReport report = CreateCompleteReport(
                canonicalHotel,
                fileMonth,
                baseline,
                caseNumber);

            ConfigureDeferredTextScenario(report, scenario.Kind);
            CalculationService.Synchronize(report);
            FindingService.SynchronizeFindings(report);

            QaReportValidationResult validationResult = ValidationService.Validate(
                report,
                metadataService.LoadHotels());
            Check(validationResult.IsReady, $"{scenario.Name}: report was not ready.");
            Check(
                validationResult.BlockingErrors.Count == 0,
                $"{scenario.Name}: validation returned blocking errors: " +
                string.Join(" | ", validationResult.BlockingErrors));
            Check(
                validationResult.CalculatedStatus == scenario.ExpectedStatus,
                $"{scenario.Name}: status was {validationResult.CalculatedStatus}, expected {scenario.ExpectedStatus}.");
            Check(
                validationResult.WarningFindingCount == scenario.WarningCount
                && validationResult.FailureFindingCount == scenario.FailureCount
                && validationResult.HandledFindingCount == scenario.HandledCount,
                $"{scenario.Name}: finding counts were " +
                $"{validationResult.WarningFindingCount}/" +
                $"{validationResult.FailureFindingCount}/" +
                $"{validationResult.HandledFindingCount}, expected " +
                $"{scenario.WarningCount}/{scenario.FailureCount}/{scenario.HandledCount}.");

            report.ReportStatus = validationResult.CalculatedStatus;
            DateTimeOffset generatedAt = new(
                year: 2026,
                month: 7,
                day: 17,
                hour: 17,
                minute: caseNumber,
                second: 0,
                offset: TimeSpan.Zero);
            byte[] pdfBytes = PdfService.GeneratePdf(
                report,
                validationResult,
                generatedAt);
            QaReportSaveRequest request = new(
                report,
                validationResult,
                generatedAt);
            QaReportSavePreparation preparation = saveService.Prepare(request);
            QaReportSaveResult saveResult = saveService.Save(
                preparation,
                pdfBytes,
                overwriteConfirmed: false);
            Check(saveResult.Success, $"{scenario.Name}: paired save failed.");
            Check(
                saveResult.FinalStatus == scenario.ExpectedStatus,
                $"{scenario.Name}: saved status did not match validation.");

            byte[] hotelBytes = File.ReadAllBytes(saveResult.HotelCopyPath);
            byte[] pmsBytes = File.ReadAllBytes(saveResult.PmsCopyPath);
            Check(
                hotelBytes.AsSpan().SequenceEqual(pmsBytes)
                && hotelBytes.AsSpan().SequenceEqual(pdfBytes),
                $"{scenario.Name}: paired PDFs were not byte-identical.");
            string hotelHash = Sha256(hotelBytes);
            string pmsHash = Sha256(pmsBytes);

            IReadOnlyList<QaReportIndexEntry> entries = indexService.LoadEntries();
            Check(
                entries.Count == caseNumber,
                $"{scenario.Name}: index contains {entries.Count} entries, expected {caseNumber}.");
            QaReportIndexEntry entry = entries.Single(candidate =>
                candidate.ReportKey == saveResult.ReportKey);
            Check(
                entry.Status == scenario.ExpectedStatus,
                $"{scenario.Name}: index status was {entry.Status}, expected {scenario.ExpectedStatus}.");
            expectedEntries.Add(new ExpectedIndexEntry(
                saveResult.ReportKey,
                scenario.ExpectedStatus));

            record($"[DEFERRED CASE] {caseNumber}: {scenario.Name}");
            record(
                $"  Status: validated={validationResult.CalculatedStatus}; " +
                $"saved={saveResult.FinalStatus}; indexed={entry.Status}");
            record($"  PDF markers: {string.Join(" | ", scenario.ExpectedPdfMarkers)}");
            record(
                $"  Hotel PDF: {saveResult.HotelCopyPath} | " +
                $"bytes={hotelBytes.Length} | SHA-256={hotelHash}");
            record(
                $"  PMS PDF: {saveResult.PmsCopyPath} | " +
                $"bytes={pmsBytes.Length} | SHA-256={pmsHash}");
        }
    }

    private static void ConfigureDeferredTextScenario(
        QaReport report,
        DeferredTextSaveKind kind)
    {
        QaBlankValueStatistic emailBlank = report.Statistics.BlankValues.Single(
            statistic => statistic.FieldId == QaStatisticFieldIds.Email);

        switch (kind)
        {
            case DeferredTextSaveKind.GeneralNotes:
                report.GeneralNotes = CreateLongText(
                    "General Notes final committed content. ",
                    "DEFERRED-GENERAL-FINAL-Z");
                break;

            case DeferredTextSaveKind.ExplainedWarning:
                emailBlank.BlankCount = 20;
                CalculationService.Synchronize(report);
                FindingService.SynchronizeFindings(report);
                QaFinding warning = report.Findings.Single(finding =>
                    finding.FindingId == QaFindingIds.WarningForBlankStatistic(
                        QaStatisticFieldIds.Email));
                warning.Resolution = QaFindingResolution.ExplainedAndAccepted;
                warning.ResolutionNotes = CreateLongText(
                    "Explained Warning final committed content. ",
                    "DEFERRED-EXPLAINED-FINAL-Z");
                break;

            case DeferredTextSaveKind.HandledFailure:
                report.FileCharacteristics.IsCustomScriptSupportAvailable = true;
                emailBlank.BlankCount = 51;
                CalculationService.Synchronize(report);
                FindingService.SynchronizeFindings(report);
                QaFinding handledFailure = report.Findings.Single(finding =>
                    finding.FindingId == QaFindingIds.FailureForBlankStatistic(
                        QaStatisticFieldIds.Email));
                handledFailure.Resolution = QaFindingResolution.HandledByCustomScript;
                handledFailure.CustomScriptName = "DeferredScriptFinalZ.csx";
                handledFailure.ResolutionNotes = CreateLongText(
                    "Handled Failure final committed content. ",
                    "DEFERRED-HANDLED-FINAL-Z");
                break;

            case DeferredTextSaveKind.ActiveFailure:
                emailBlank.BlankCount = 51;
                CalculationService.Synchronize(report);
                FindingService.SynchronizeFindings(report);
                QaFinding activeFailure = report.Findings.Single(finding =>
                    finding.FindingId == QaFindingIds.FailureForBlankStatistic(
                        QaStatisticFieldIds.Email));
                activeFailure.ResolutionNotes = CreateLongText(
                    "Active Failure final committed content. ",
                    "DEFERRED-ACTIVE-FAILURE-FINAL-Z");
                break;

            case DeferredTextSaveKind.ChecklistWarning:
                QaCheckResult checklistResult = report.ChecklistResults.Single(result =>
                    result.CheckId == QaChecklistIds.Raw.FileFormatConsistent);
                checklistResult.Notes = CreateLongText(
                    "Checklist Warning final committed content. ",
                    "DEFERRED-CHECKLIST-WARNING-FINAL-Z");
                FindingService.SetChecklistWarningSelected(
                    report,
                    checklistResult.CheckId,
                    selected: true);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static string CreateLongText(string prefix, string finalMarker)
    {
        StringBuilder builder = new(prefix);
        while (builder.Length < 500)
        {
            builder.Append("Synthetic non-sensitive aggregate explanation. ");
        }

        builder.Append(finalMarker);
        return builder.ToString();
    }

    private static QaReport CreateCompleteReport(
        QaHotelMetadata canonicalHotel,
        QaFileMonth fileMonth,
        SaveScenario scenario,
        int caseNumber)
    {
        QaReport report = new()
        {
            ReportId = $"synthetic-save-evidence-{caseNumber:D2}-{Guid.NewGuid():N}",
            HotelInformation = new QaHotelInformation
            {
                HotelId = canonicalHotel.HotelId,
                HotelName = canonicalHotel.HotelName,
                PmsName = canonicalHotel.PmsName,
                FileMonth = fileMonth
            },
            QaDate = new DateOnly(2026, 7, 17),
            CreatedBy = "Synthetic QA Evidence",
            OriginalFileName = $"synthetic-statistics-case-{caseNumber:D2}.csv",
            FileCharacteristics = new QaFileCharacteristics
            {
                NameColumnMode = QaNameColumnMode.SeparateFirstAndLastName,
                HasCurrencyColumn = false,
                MonetaryColumnScenario = QaMonetaryColumnScenario.OneMonetaryColumn,
                HasMultipleConfirmationNumberCandidateColumns = false,
                IsCustomScriptSupportAvailable = scenario.HandleBrokenFailure,
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
                    ValidArrivalDateCount = 100,
                    ArrivalDatesWithinFileMonth = 100,
                    ArrivalDatesOutsideFileMonth = 0
                },
                Database = new QaDatabaseStatistics
                {
                    ImportedRecordCount = 100,
                    RejectedRecordCount = 0,
                    RecordsWithMissingRequiredDatabaseValues = 0
                }
            },
            GeneralNotes = "Synthetic aggregate counts only; no guest-level data."
        };

        foreach (QaCheckDefinition definition in QaChecklistCatalog.Definitions)
        {
            report.ChecklistResults.Add(new QaCheckResult
            {
                CheckId = definition.Id,
                Status = IsChecklistApplicable(definition, report.FileCharacteristics)
                    ? QaCheckStatus.Pass
                    : QaCheckStatus.NotApplicable,
                ResultSource = QaResultSource.Manual
            });
        }

        CalculationService.Synchronize(report);
        string statisticFieldId = scenario.UseMappedFirstName
            ? QaStatisticFieldIds.FirstName
            : QaStatisticFieldIds.Email;
        QaBlankValueStatistic selectedBlank = report.Statistics.BlankValues.Single(
            row => row.FieldId == statisticFieldId);
        QaBrokenDataStatistic selectedBroken = report.Statistics.BrokenData.Single(
            row => row.FieldId == statisticFieldId);
        selectedBlank.BlankCount = scenario.BlankCount;
        selectedBroken.BrokenValueCount = scenario.BrokenCount;
        if (scenario.UseMappedFirstName)
        {
            QaCheckResult relatedCheck = report.ChecklistResults.Single(
                result => result.CheckId == QaChecklistIds.Raw.FirstNameValuesValid);
            relatedCheck.Status = QaCheckStatus.Fail;
            relatedCheck.Notes = null;
        }
        CalculationService.Synchronize(report);
        FindingService.SynchronizeFindings(report);

        if (scenario.UseMappedFirstName)
        {
            Check(
                report.Findings.Any(finding => finding.FindingId
                    == QaFindingIds.FailureForBrokenStatistic(statisticFieldId)),
                $"{scenario.Name}: canonical mapped Broken Failure is missing.");
            Check(
                !report.Findings.Any(finding => finding.FindingId
                    == QaFindingIds.FailureForCheck(QaChecklistIds.Raw.FirstNameValuesValid)),
                $"{scenario.Name}: threshold-only mapped checklist Failure was not de-duplicated.");
        }

        if (scenario.HandleBrokenFailure)
        {
            string findingId = QaFindingIds.FailureForBrokenStatistic(
                statisticFieldId);
            QaFinding failure = report.Findings.Single(
                finding => finding.FindingId == findingId);
            Check(
                failure.Severity == QaFindingSeverity.Failure
                && failure.Resolution == QaFindingResolution.Active,
                "Handled Broken Failure: expected a new Active Failure before resolution.");
            failure.Resolution = QaFindingResolution.HandledByCustomScript;
            failure.CustomScriptName = "synthetic-name-normalization.csx";
            failure.ResolutionNotes =
                "Synthetic evidence case handled by an approved non-production test script.";
            FindingService.SynchronizeFindings(report);
        }

        Check(
            report.Findings.Select(finding => finding.FindingId)
                .Distinct(StringComparer.Ordinal)
                .Count() == report.Findings.Count,
            $"{scenario.Name}: duplicate finding IDs were generated.");
        return report;
    }

    private static bool IsChecklistApplicable(
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
            _ => false
        };
    }

    private static void AssertValidation(
        SaveScenario scenario,
        QaReportValidationResult result)
    {
        Check(
            result.IsReady,
            $"{scenario.Name}: report was not ready: " +
            string.Join(" | ", result.BlockingErrors));
        Check(
            result.BlockingErrors.Count == 0,
            $"{scenario.Name}: ready result retained blocking errors.");
        Check(
            result.WorkflowWarnings.Count == 0,
            $"{scenario.Name}: unexpected workflow warnings: " +
            string.Join(" | ", result.WorkflowWarnings));
        Check(
            result.CalculatedStatus == scenario.ExpectedStatus,
            $"{scenario.Name}: calculated status was {result.CalculatedStatus}, expected {scenario.ExpectedStatus}.");
        Check(
            result.WarningFindingCount == scenario.WarningCount,
            $"{scenario.Name}: warning count was {result.WarningFindingCount}, expected {scenario.WarningCount}.");
        Check(
            result.FailureFindingCount == scenario.FailureCount,
            $"{scenario.Name}: failure count was {result.FailureFindingCount}, expected {scenario.FailureCount}.");
        Check(
            result.HandledFindingCount == scenario.HandledCount,
            $"{scenario.Name}: handled count was {result.HandledFindingCount}, expected {scenario.HandledCount}.");
        Check(
            !string.IsNullOrWhiteSpace(result.ValidatedReportFingerprint),
            $"{scenario.Name}: ready result lacks a validated-report fingerprint.");
    }

    private static void AssertPdfPayload(byte[] payload, string scenarioName)
    {
        Check(payload.Length >= 16, $"{scenarioName}: generated PDF payload is too short.");
        Check(
            payload[0] == (byte)'%'
            && payload[1] == (byte)'P'
            && payload[2] == (byte)'D'
            && payload[3] == (byte)'F'
            && payload[4] == (byte)'-',
            $"{scenarioName}: generated payload lacks a PDF header.");

        int markerWindowStart = Math.Max(0, payload.Length - 2048);
        string ending = Encoding.ASCII.GetString(
            payload,
            markerWindowStart,
            payload.Length - markerWindowStart);
        Check(
            ending.Contains("%%EOF", StringComparison.Ordinal),
            $"{scenarioName}: generated payload lacks a plausible PDF EOF marker.");
    }

    private static void AssertSaveResult(
        SaveScenario scenario,
        QaReportSaveResult result,
        int expectedPdfLength,
        DateTimeOffset generatedAt)
    {
        Check(result.Success && !result.Cancelled, $"{scenario.Name}: save did not succeed.");
        Check(
            result.FinalStatus == scenario.ExpectedStatus,
            $"{scenario.Name}: saved status was {result.FinalStatus}, expected {scenario.ExpectedStatus}.");
        Check(result.IndexUpdated, $"{scenario.Name}: index was not updated.");
        Check(!result.OverwriteOccurred, $"{scenario.Name}: a fresh report overwrote an existing report.");
        Check(
            result.HotelMatchesReplaced == 0 && result.PmsMatchesReplaced == 0,
            $"{scenario.Name}: a fresh report replaced existing paired files.");
        Check(
            result.PdfByteLength == expectedPdfLength,
            $"{scenario.Name}: save result recorded the wrong PDF length.");
        Check(
            result.SavedAtUtc == generatedAt.ToUniversalTime(),
            $"{scenario.Name}: save timestamp does not match the deterministic generated timestamp.");
        Check(
            result.CleanupWarning is null && !result.ManualReviewRequired,
            $"{scenario.Name}: save left a cleanup warning or manual-review condition.");
    }

    private static QaReportIndexEntry AssertSingleIndexEntry(
        QaStoragePaths paths,
        IReadOnlyList<QaReportIndexEntry> entries,
        QaReportSaveResult saveResult,
        DateTimeOffset generatedAt,
        SaveScenario scenario)
    {
        QaReportIndexEntry[] matches = entries
            .Where(entry => entry.ReportKey == saveResult.ReportKey)
            .ToArray();
        Check(
            matches.Length == 1,
            $"{scenario.Name}: expected one index entry for {saveResult.ReportKey}, found {matches.Length}.");

        QaReportIndexEntry entry = matches[0];
        Check(
            entry.Status == scenario.ExpectedStatus,
            $"{scenario.Name}: index status was {entry.Status}, expected {scenario.ExpectedStatus}.");
        Check(
            string.Equals(entry.Filename, saveResult.FinalFilename, StringComparison.Ordinal),
            $"{scenario.Name}: index filename differs from the saved filename.");
        Check(
            PathsEqual(
                ResolveIndexedPath(paths, entry.RelativeHotelCopyPath),
                saveResult.HotelCopyPath),
            $"{scenario.Name}: indexed Hotel path differs from the saved Hotel path.");
        Check(
            PathsEqual(
                ResolveIndexedPath(paths, entry.RelativePmsCopyPath),
                saveResult.PmsCopyPath),
            $"{scenario.Name}: indexed PMS path differs from the saved PMS path.");
        Check(
            entry.SavedAtUtc == generatedAt.ToUniversalTime(),
            $"{scenario.Name}: index timestamp differs from the save timestamp.");
        return entry;
    }

    private static void AssertFinalEvidence(
        QaStoragePaths paths,
        QaReportIndexService indexService,
        IReadOnlyList<ExpectedIndexEntry> expectedEntries,
        Action<string> record)
    {
        IReadOnlyList<QaReportIndexEntry> entries = indexService.LoadEntries();
        Check(
            entries.Count == expectedEntries.Count,
            $"Final index contains {entries.Count} entries, expected {expectedEntries.Count}.");

        foreach (ExpectedIndexEntry expected in expectedEntries)
        {
            QaReportIndexEntry[] matches = entries
                .Where(entry => entry.ReportKey == expected.ReportKey)
                .ToArray();
            Check(
                matches.Length == 1,
                $"Final index does not contain exactly one entry for {expected.ReportKey}.");
            Check(
                matches[0].Status == expected.Status,
                $"Final index status for {expected.ReportKey} is {matches[0].Status}, expected {expected.Status}.");
        }

        int hotelPdfCount = Directory.EnumerateFiles(
                paths.ByHotelRootPath,
                "*.pdf",
                SearchOption.AllDirectories)
            .Count();
        int pmsPdfCount = Directory.EnumerateFiles(
                paths.ByPmsRootPath,
                "*.pdf",
                SearchOption.AllDirectories)
            .Count();
        Check(
            hotelPdfCount == expectedEntries.Count
                && pmsPdfCount == expectedEntries.Count,
            $"Expected {expectedEntries.Count} final PDFs in each destination; " +
            $"found Hotel={hotelPdfCount}, PMS={pmsPdfCount}.");

        string[] transactionArtifacts = Directory.EnumerateFiles(
                paths.DocumentationRootPath,
                "*",
                SearchOption.AllDirectories)
            .Where(IsTransactionArtifact)
            .ToArray();
        Check(
            transactionArtifacts.Length == 0,
            "Transaction, rollback, temporary, or backup artifacts remain: " +
            string.Join(" | ", transactionArtifacts));

        byte[] indexBytes = File.ReadAllBytes(paths.QaReportIndexFilePath);
        string indexHash = Sha256(indexBytes);
        record(
            $"Final QA index: {paths.QaReportIndexFilePath} | " +
            $"entries={entries.Count} | bytes={indexBytes.Length} | SHA-256={indexHash}");
        foreach (QaReportIndexEntry entry in entries)
        {
            record($"  Index entry: {entry.ReportKey} | {entry.Status} | {entry.Filename}");
        }

        record(
            $"Final paired PDFs: Hotel={hotelPdfCount}; PMS={pmsPdfCount}; " +
            "transaction artifacts=0");
    }

    private static EvidenceRoot CreateEvidenceRoot(string? preservedParentDirectory)
    {
        bool preserve = preservedParentDirectory is not null;
        string parentPath;

        if (preserve)
        {
            if (string.IsNullOrWhiteSpace(preservedParentDirectory)
                || !Path.IsPathFullyQualified(preservedParentDirectory))
            {
                throw new ArgumentException(
                    "The preserved evidence parent must be an absolute path outside the repository.",
                    nameof(preservedParentDirectory));
            }

            parentPath = NormalizeDirectoryPath(preservedParentDirectory);
        }
        else
        {
            parentPath = NormalizeDirectoryPath(Path.Combine(
                Path.GetTempPath(),
                TemporaryParentName));
        }

        string? repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory)
            ?? FindRepositoryRoot(Environment.CurrentDirectory);
        if (repositoryRoot is not null && IsWithin(parentPath, repositoryRoot))
        {
            throw new ArgumentException(
                $"Synthetic evidence parent must be outside repository '{repositoryRoot}'.",
                nameof(preservedParentDirectory));
        }

        if (preserve && repositoryRoot is null)
        {
            throw new InvalidOperationException(
                "The repository root could not be resolved, so an external evidence path cannot be verified safely.");
        }

        string? volumeRoot = Path.GetPathRoot(parentPath);
        if (volumeRoot is not null && PathsEqual(parentPath, volumeRoot))
        {
            throw new ArgumentException(
                "A filesystem root cannot be used as the synthetic evidence parent.",
                nameof(preservedParentDirectory));
        }

        if (ContainsPathSegment(parentPath, FrozenPilotPackageDirectoryName))
        {
            throw new ArgumentException(
                "The frozen pilot package cannot be used as an evidence parent.",
                nameof(preservedParentDirectory));
        }

        if (Directory.Exists(Path.Combine(parentPath, "QAReports")))
        {
            throw new ArgumentException(
                "An initialized documentation or pilot root cannot be used as an evidence parent.",
                nameof(preservedParentDirectory));
        }

        Directory.CreateDirectory(parentPath);
        string runDirectoryName = "r-" + Guid.NewGuid().ToString("N")[..16];
        string rootPath = NormalizeDirectoryPath(Path.Combine(
            parentPath,
            runDirectoryName));
        if (!IsStrictlyWithin(rootPath, parentPath)
            || (repositoryRoot is not null && IsWithin(rootPath, repositoryRoot)))
        {
            throw new InvalidOperationException(
                "The unique synthetic evidence root did not resolve inside its approved external parent.");
        }

        Directory.CreateDirectory(rootPath);
        return new EvidenceRoot(rootPath, parentPath, preserve);
    }

    private static void DeleteDisposableEvidenceRoot(EvidenceRoot evidenceRoot)
    {
        if (evidenceRoot.Preserve)
        {
            throw new InvalidOperationException(
                "A preserved evidence root cannot be removed by disposable cleanup.");
        }

        string rootPath = NormalizeDirectoryPath(evidenceRoot.RootPath);
        string parentPath = NormalizeDirectoryPath(evidenceRoot.ParentPath);
        string leafName = Path.GetFileName(rootPath);
        bool isVerifiedRunDirectory = leafName.Length == 18
            && leafName.StartsWith("r-", StringComparison.Ordinal)
            && leafName[2..].All(Uri.IsHexDigit);
        if (!IsStrictlyWithin(rootPath, parentPath)
            || !isVerifiedRunDirectory)
        {
            throw new InvalidOperationException(
                "Disposable cleanup refused an unverified evidence path.");
        }

        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }

        try
        {
            Directory.Delete(parentPath, recursive: false);
        }
        catch (IOException)
        {
            // Another concurrent disposable run or unrelated safe content owns the parent.
        }
        catch (UnauthorizedAccessException)
        {
            // The unique child was removed; failure to remove the shared parent is harmless.
        }
    }

    private static string ResolveIndexedPath(QaStoragePaths paths, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(
            paths.QaReportsRootPath,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string Sha256(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content));

    private static bool IsTransactionArtifact(string path)
    {
        string name = Path.GetFileName(path);
        return name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".qa-tmp", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".qa-rollback", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertPathInsideRoot(
        string candidatePath,
        string rootPath,
        string description)
    {
        Check(
            IsStrictlyWithin(candidatePath, rootPath),
            $"{description} resolved outside the isolated synthetic root.");
    }

    private static string NormalizeDirectoryPath(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            NormalizeDirectoryPath(left),
            NormalizeDirectoryPath(right),
            StringComparison.OrdinalIgnoreCase);

    private static bool IsStrictlyWithin(string path, string parent) =>
        !PathsEqual(path, parent) && IsWithin(path, parent);

    private static bool IsWithin(string path, string parent)
    {
        string relative = Path.GetRelativePath(
            NormalizeDirectoryPath(parent),
            NormalizeDirectoryPath(path));
        return relative == "."
            || (relative != ".."
                && !relative.StartsWith(
                    $"..{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal)
                && !relative.StartsWith("../", StringComparison.Ordinal)
                && !Path.IsPathFullyQualified(relative));
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(startPath));
        if (!directory.Exists && directory.Parent is not null)
        {
            directory = directory.Parent;
        }

        while (directory is not null)
        {
            string gitPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bool ContainsPathSegment(string path, string segment)
    {
        DirectoryInfo? directory = new(NormalizeDirectoryPath(path));
        while (directory is not null)
        {
            if (string.Equals(
                    directory.Name,
                    segment,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            directory = directory.Parent;
        }

        return false;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new SaveEvidenceAssertionException(message);
        }
    }

    private sealed record SaveScenario(
        string Name,
        int BlankCount,
        int BrokenCount,
        QaReportStatus ExpectedStatus,
        int WarningCount,
        int FailureCount,
        int HandledCount,
        bool HandleBrokenFailure,
        bool UseMappedFirstName);

    private sealed record DeferredTextSaveScenario(
        string Name,
        DeferredTextSaveKind Kind,
        QaReportStatus ExpectedStatus,
        int WarningCount,
        int FailureCount,
        int HandledCount,
        IReadOnlyList<string> ExpectedPdfMarkers);

    private enum DeferredTextSaveKind
    {
        GeneralNotes,
        ExplainedWarning,
        HandledFailure,
        ActiveFailure,
        ChecklistWarning
    }

    private sealed record ExpectedIndexEntry(
        QaReportKey ReportKey,
        QaReportStatus Status);

    private sealed record EvidenceRoot(
        string RootPath,
        string ParentPath,
        bool Preserve);

    private sealed class SaveEvidenceAssertionException(string message)
        : Exception(message);
}
