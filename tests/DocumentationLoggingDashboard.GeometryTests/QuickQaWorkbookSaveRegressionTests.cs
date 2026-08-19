using System.Globalization;
using ClosedXML.Excel;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.GeometryTests;

/// <summary>
/// End-to-end synthetic coverage for Quick QA workbook schemas and the paired
/// Surface/Hotel-history transaction. Program.cs intentionally remains unmodified.
/// </summary>
internal static class QuickQaWorkbookSaveRegressionTests
{
    private static readonly DateTimeOffset FirstTimestamp =
        new(2026, 8, 18, 14, 30, 0, TimeSpan.FromHours(-4));
    private static readonly DateTimeOffset SecondTimestamp =
        new(2026, 8, 18, 15, 45, 12, TimeSpan.FromHours(-4));

    private static readonly string[] LegacySurfaceHeaders =
    [
        "Month 2026",
        "Hotel Name",
        "Hotel Id",
        "PMS",
        "file id",
        "Pass/Fail/Warning",
        "summary"
    ];

    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        (string Name, Action Body)[] tests =
        [
            ("canonical create, repeated paired append, and presentation", TestCanonicalRepeatedSave),
            ("legacy aliases append and canonicalization", TestLegacyAppend),
            ("incompatible, corrupt, and ambiguous rejection", TestInvalidWorkbookCategories),
            ("invalid report preflight leaves both destinations untouched", TestInvalidReportPreflight),
            ("Surface and Hotel-history lock categorization", TestLockCategories),
            ("staging failures and new-history commit rollback", TestStagingAndNewHistoryRollback),
            ("Surface commit failure restores both workbooks", TestSurfaceCommitRollback),
            ("History commit failure restores both workbooks", TestHistoryCommitRollback),
            ("staged validation mismatch blocks commit", TestStagedValidationMismatch),
            ("final verification mismatch restores both workbooks", TestFinalVerificationMismatch),
            ("concurrent Surface change is retained and not overwritten", TestConcurrentChange),
            ("post-backup Surface edit is detected and restored", TestPostBackupConcurrentChange)
        ];

        int failed = 0;
        output.WriteLine($"Quick QA workbook/save harness: {tests.Length} tests");

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
                ? $"[PASS] All {tests.Length} Quick QA workbook/save tests passed."
                : $"[FAIL] {failed} of {tests.Length} Quick QA workbook/save tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void TestCanonicalRepeatedSave()
    {
        using SyntheticEnvironment environment = new();
        string fileName = environment.WorkbookService.CreateNewSurfaceWorkbook(
            "Surface QA August");
        string surfacePath = environment.Paths.ResolveSurfaceQaWorkbookPath(
            fileName);
        byte[] pristine = File.ReadAllBytes(surfacePath);
        QuickQaSurfaceWorkbookInfo initial =
            environment.WorkbookService.ValidateSurfaceWorkbook(fileName);

        Check(
            fileName == "Surface QA August.xlsx"
            && initial.DataRowCount == 0
            && !initial.UsedLegacyHeaders,
            "A new canonical Surface workbook was not created with the expected identity and empty schema.");
        Check(
            !File.Exists(environment.HistoryPath),
            $"The synthetic Hotel history unexpectedly existed before its first Quick save: '{environment.HistoryPath}'.");

        QuickQaWorkbookException duplicate = ExpectWorkbookFailure(
            () => environment.WorkbookService.CreateNewSurfaceWorkbook(
                "Surface QA August.xlsx"));
        Check(
            duplicate.ErrorCategory == QuickQaWorkbookErrorCategory.AlreadyExists
            && duplicate.Role == QuickQaWorkbookRole.Surface
            && File.ReadAllBytes(surfacePath).SequenceEqual(pristine),
            "Create New Surface QA File overwrote or misclassified an existing workbook.");

        SequenceTimeProvider timeProvider = new(
            [FirstTimestamp, SecondTimestamp]);
        QuickQaSaveService saveService = environment.CreateSaveService(
            environment.RealFileOperations,
            timeProvider);

        QuickQaReport firstReport = environment.CreateReadyReport("001234");
        QuickQaSaveResult first = saveService.Save(firstReport, fileName);
        Check(
            first.Timestamp == FirstTimestamp
            && first.FileId == "001234"
            && first.FinalStatus == QaReportStatus.Pass
            && first.CleanupWarning is null,
            "The first paired save result did not preserve its exact event data.");
        Check(
            environment.WorkbookService.ValidateSurfaceWorkbook(fileName)
                .DataRowCount == 1
            && CountWorkbookDataRows(
                environment.HistoryPath,
                QuickQaWorkbookService.HistoryWorksheetName,
                columnCount: 8) == 1,
            "One guarded save invocation did not produce exactly one Surface row and one History row.");

        QuickQaReport secondReport = environment.CreateReadyReport("000777");
        QuickQaSaveResult second = saveService.Save(secondReport, fileName);
        Check(
            second.Timestamp == SecondTimestamp
            && second.FileId == "000777"
            && second.SurfaceWorkbookPath == surfacePath
            && second.HistoryPath == environment.HistoryPath,
            "The repeated paired save returned incorrect destination or event details.");
        Check(
            environment.WorkbookService.ValidateSurfaceWorkbook(fileName)
                .DataRowCount == 2
            && CountWorkbookDataRows(
                environment.HistoryPath,
                QuickQaWorkbookService.HistoryWorksheetName,
                columnCount: 8) == 2,
            "A legitimate later repeat did not add exactly one further row to each workbook.");

        using (XLWorkbook surface = new(surfacePath))
        {
            IXLWorksheet worksheet = surface.Worksheet(
                QuickQaWorkbookService.SurfaceWorksheetName);
            AssertHeaders(worksheet, QuickQaWorkbookService.SurfaceHeaders);
            AssertSurfaceRow(
                worksheet,
                rowNumber: 2,
                environment,
                "001234",
                QuickQaSummaryService.CleanPassSummary);
            AssertSurfaceRow(
                worksheet,
                rowNumber: 3,
                environment,
                "000777",
                QuickQaSummaryService.CleanPassSummary);
            AssertPresentation(
                worksheet,
                QuickQaWorkbookService.SurfaceTableName,
                columnCount: 7,
                expectedLastRow: 3,
                summaryColumn: 7);
            AssertTextCell(
                worksheet.Cell(2, 3),
                environment.Hotel.HotelId,
                "Surface Hotel ID");
            AssertTextCell(worksheet.Cell(2, 5), "001234", "first Surface File ID");
            AssertTextCell(worksheet.Cell(3, 5), "000777", "second Surface File ID");
            Check(
                worksheet.Row(1).LastCellUsed(
                    XLCellsUsedOptions.Contents)?.Address.ColumnNumber == 7,
                "The client Surface workbook unexpectedly contains a QA Timestamp column.");
        }

        using (XLWorkbook history = new(environment.HistoryPath))
        {
            Check(
                history.Worksheets.Count == 1,
                "Hotel Quick QA history contains unexpected worksheets.");
            IXLWorksheet worksheet = history.Worksheet(
                QuickQaWorkbookService.HistoryWorksheetName);
            AssertHeaders(worksheet, QuickQaWorkbookService.HistoryHeaders);
            AssertHistoryRow(
                worksheet,
                rowNumber: 2,
                environment,
                FirstTimestamp,
                "001234",
                QuickQaSummaryService.CleanPassSummary);
            AssertHistoryRow(
                worksheet,
                rowNumber: 3,
                environment,
                SecondTimestamp,
                "000777",
                QuickQaSummaryService.CleanPassSummary);
            AssertPresentation(
                worksheet,
                QuickQaWorkbookService.HistoryTableName,
                columnCount: 8,
                expectedLastRow: 3,
                summaryColumn: 8);
            AssertTextCell(worksheet.Cell(2, 1), FirstTimestamp.ToString(
                "O",
                CultureInfo.InvariantCulture), "first history timestamp");
            AssertTextCell(
                worksheet.Cell(2, 4),
                environment.Hotel.HotelId,
                "history Hotel ID");
            AssertTextCell(worksheet.Cell(2, 6), "001234", "first history File ID");
            AssertTextCell(worksheet.Cell(3, 6), "000777", "second history File ID");
        }

        Check(
            !Directory.EnumerateFiles(
                    environment.Paths.ByPmsRootPath,
                    QuickQaWorkbookService.HistoryFileName,
                    SearchOption.AllDirectories)
                .Any(),
            "Quick QA created an out-of-contract PMS history workbook.");
        AssertNoTransactionArtifacts(environment.RootPath);
    }

    private static void TestLegacyAppend()
    {
        using SyntheticEnvironment environment = new();
        const string fileName = "Legacy Surface.xlsx";
        string path = environment.Paths.ResolveSurfaceQaWorkbookPath(fileName);
        string[] legacyRow =
        [
            "2026-06",
            "Historical Hotel",
            "0099",
            "Historical PMS",
            "000111",
            "Pass with Warnings",
            "Existing legacy row remains unchanged."
        ];
        WriteWorkbook(
            path,
            [
                new WorksheetFixture(
                    "Legacy Sample",
                    LegacySurfaceHeaders,
                    [legacyRow])
            ]);
        using (XLWorkbook fixture = new(path))
        {
            IXLWorksheet legacySheet = fixture.Worksheet("Legacy Sample");
            legacySheet.Cell(2, 2).Style.Font.Bold = true;
            legacySheet.Cell(2, 2).Style.Fill.BackgroundColor =
                XLColor.LightGoldenrodYellow;
            IXLWorksheet fixtureNotes = fixture.Worksheets.Add("Client Notes");
            fixtureNotes.Cell("C4").Value =
                "Unrelated client content must survive.";
            fixtureNotes.Cell("C4").Style.Font.Italic = true;
            fixture.Save();
        }

        QuickQaSurfaceWorkbookInfo legacy =
            environment.WorkbookService.ValidateSurfaceWorkbook(fileName);
        Check(
            legacy.UsedLegacyHeaders
            && legacy.DataRowCount == 1
            && legacy.WorksheetName == "Legacy Sample",
            "The approved legacy aliases were not recognized before append.");

        QuickQaSaveService saveService = environment.CreateSaveService(
            environment.RealFileOperations,
            new SequenceTimeProvider([FirstTimestamp]));
        _ = saveService.Save(
            environment.CreateReadyReport("001234"),
            fileName);

        using XLWorkbook workbook = new(path);
        IXLWorksheet worksheet = workbook.Worksheet("Legacy Sample");
        AssertHeaders(worksheet, QuickQaWorkbookService.SurfaceHeaders);
        AssertExactRow(worksheet, 2, legacyRow, "preserved legacy Surface row");
        AssertSurfaceRow(
            worksheet,
            rowNumber: 3,
            environment,
            "001234",
            QuickQaSummaryService.CleanPassSummary);
        AssertTextCell(worksheet.Cell(2, 5), "000111", "legacy File ID");
        AssertTextCell(worksheet.Cell(2, 3), "0099", "legacy Hotel ID");
        AssertTextCell(worksheet.Cell(3, 5), "001234", "appended File ID");
        AssertPresentation(
            worksheet,
            QuickQaWorkbookService.SurfaceTableName,
            columnCount: 7,
            expectedLastRow: 3,
            summaryColumn: 7);
        Check(
            worksheet.Cell(2, 2).Style.Font.Bold
            && worksheet.Cell(2, 2).Style.Fill.BackgroundColor ==
                XLColor.LightGoldenrodYellow,
            "Legacy append did not preserve distinctive existing-cell formatting.");
        IXLWorksheet preservedNotes = workbook.Worksheet("Client Notes");
        Check(
            preservedNotes.Cell("C4").GetString() ==
                "Unrelated client content must survive."
            && preservedNotes.Cell("C4").Style.Font.Italic,
            "Legacy append changed an unrelated worksheet or its formatting.");
        Check(
            !environment.WorkbookService.ValidateSurfaceWorkbook(fileName)
                .UsedLegacyHeaders,
            "A successful legacy append did not canonicalize all seven headers.");
    }

    private static void TestInvalidWorkbookCategories()
    {
        using SyntheticEnvironment environment = new();

        const string incompatibleName = "Incompatible.xlsx";
        WriteWorkbook(
            environment.Paths.ResolveSurfaceQaWorkbookPath(incompatibleName),
            [
                new WorksheetFixture(
                    "Wrong Schema",
                    ["Wrong", "Hotel Name", "Hotel ID", "PMS", "File ID", "Result", "Summary"],
                    [])
            ]);
        AssertWorkbookCategoryAndUnchanged(
            environment,
            incompatibleName,
            QuickQaWorkbookErrorCategory.IncompatibleSchema);

        const string hybridName = "Hybrid Headers.xlsx";
        WriteWorkbook(
            environment.Paths.ResolveSurfaceQaWorkbookPath(hybridName),
            [
                new WorksheetFixture(
                    "Hybrid",
                    [
                        "Month 2026",
                        "Hotel Name",
                        "Hotel ID",
                        "PMS",
                        "File ID",
                        "Result",
                        "Summary"
                    ],
                    [])
            ]);
        AssertWorkbookCategoryAndUnchanged(
            environment,
            hybridName,
            QuickQaWorkbookErrorCategory.IncompatibleSchema);

        const string reorderedName = "Reordered Headers.xlsx";
        WriteWorkbook(
            environment.Paths.ResolveSurfaceQaWorkbookPath(reorderedName),
            [
                new WorksheetFixture(
                    "Reordered",
                    [
                        "Hotel Name",
                        "File Month",
                        "Hotel ID",
                        "PMS",
                        "File ID",
                        "Result",
                        "Summary"
                    ],
                    [])
            ]);
        AssertWorkbookCategoryAndUnchanged(
            environment,
            reorderedName,
            QuickQaWorkbookErrorCategory.IncompatibleSchema);

        const string extraHeaderName = "Extra Header.xlsx";
        WriteWorkbook(
            environment.Paths.ResolveSurfaceQaWorkbookPath(extraHeaderName),
            [
                new WorksheetFixture(
                    "Extra",
                    QuickQaWorkbookService.SurfaceHeaders
                        .Append("Unexpected")
                        .ToArray(),
                    [])
            ]);
        AssertWorkbookCategoryAndUnchanged(
            environment,
            extraHeaderName,
            QuickQaWorkbookErrorCategory.IncompatibleSchema);

        const string corruptName = "Corrupt.xlsx";
        File.WriteAllBytes(
            environment.Paths.ResolveSurfaceQaWorkbookPath(corruptName),
            [0x50, 0x4B, 0x03, 0x04, 0x01, 0x02, 0x03]);
        AssertWorkbookCategoryAndUnchanged(
            environment,
            corruptName,
            QuickQaWorkbookErrorCategory.Corrupt);

        const string ambiguousName = "Ambiguous.xlsx";
        WriteWorkbook(
            environment.Paths.ResolveSurfaceQaWorkbookPath(ambiguousName),
            [
                new WorksheetFixture(
                    "First Match",
                    QuickQaWorkbookService.SurfaceHeaders.ToArray(),
                    []),
                new WorksheetFixture(
                    "Second Match",
                    QuickQaWorkbookService.SurfaceHeaders.ToArray(),
                    [])
            ]);
        AssertWorkbookCategoryAndUnchanged(
            environment,
            ambiguousName,
            QuickQaWorkbookErrorCategory.AmbiguousSchema);
    }

    private static void TestLockCategories()
    {
        using SyntheticEnvironment environment = CreateSeededEnvironment(
            out string fileName);
        string surfacePath = environment.Paths.ResolveSurfaceQaWorkbookPath(
            fileName);
        byte[] surfaceBefore = File.ReadAllBytes(surfacePath);
        byte[] historyBefore = File.ReadAllBytes(environment.HistoryPath);

        FaultInjectingFileOperations surfaceLocked = new();
        surfaceLocked.FailureFactory = call =>
            call.Kind == FileOperationKind.ReadAllBytes
            && PathsEqual(call.Path, surfacePath)
                ? new IOException("Synthetic Surface lock.")
                : null;
        QuickQaSaveException surfaceFailure = ExpectSaveFailure(() =>
            environment.CreateSaveService(
                    surfaceLocked,
                    new SequenceTimeProvider([SecondTimestamp]))
                .Save(environment.CreateReadyReport("000002"), fileName));
        Check(
            surfaceFailure.ErrorCategory ==
                QuickQaSaveErrorCategory.SurfaceWorkbookInUse
            && surfaceFailure.Stage == QuickQaSaveStage.SurfaceLoad,
            "A locked Surface workbook was not classified at SurfaceLoad.");

        FaultInjectingFileOperations historyLocked = new();
        historyLocked.FailureFactory = call =>
            call.Kind == FileOperationKind.ReadAllBytes
            && PathsEqual(call.Path, environment.HistoryPath)
                ? new IOException("Synthetic Hotel history lock.")
                : null;
        QuickQaSaveException historyFailure = ExpectSaveFailure(() =>
            environment.CreateSaveService(
                    historyLocked,
                    new SequenceTimeProvider([SecondTimestamp]))
                .Save(environment.CreateReadyReport("000003"), fileName));
        Check(
            historyFailure.ErrorCategory ==
                QuickQaSaveErrorCategory.HotelHistoryWorkbookInUse
            && historyFailure.Stage == QuickQaSaveStage.HistoryLoad,
            "A locked Hotel history workbook was not classified at HistoryLoad.");

        Check(
            File.ReadAllBytes(surfacePath).SequenceEqual(surfaceBefore)
            && File.ReadAllBytes(environment.HistoryPath).SequenceEqual(
                historyBefore),
            "A simulated workbook lock left a one-sided update.");
        AssertNoTransactionArtifacts(environment.RootPath);
    }

    private static void TestInvalidReportPreflight()
    {
        using SyntheticEnvironment environment = new();
        string fileName = environment.WorkbookService.CreateNewSurfaceWorkbook(
            "Preflight Surface.xlsx");
        string surfacePath = environment.Paths.ResolveSurfaceQaWorkbookPath(
            fileName);
        byte[] surfaceBefore = File.ReadAllBytes(surfacePath);

        QuickQaReport handledWhileDisabled =
            environment.CreateReadyReport("000030");
        handledWhileDisabled.GetChecklistResult(
            QuickQaChecklistIds.Raw.FileFormatConsistent).Status =
                QuickQaCheckStatus.Fail;
        _ = new QuickQaFindingSynchronizationService().Synchronize(
            handledWhileDisabled);
        new QuickQaSummaryService().Regenerate(handledWhileDisabled);
        handledWhileDisabled.Findings.Single().Resolution =
            QaFindingResolution.HandledByCustomScript;

        QuickQaSaveException handledFailure = ExpectSaveFailure(() =>
            environment.CreateSaveService(
                    environment.RealFileOperations,
                    new SequenceTimeProvider([FirstTimestamp]))
                .Save(handledWhileDisabled, fileName));
        Check(
            handledFailure.ErrorCategory == QuickQaSaveErrorCategory.ReportInvalid
            && handledFailure.Stage == QuickQaSaveStage.Validation,
            "Handled-by-script while Custom Script Available is No was not rejected during preflight.");
        AssertPreflightDidNotMutate(
            environment,
            surfacePath,
            surfaceBefore,
            "handled-while-disabled preflight");

        QuickQaReport statusMismatch = environment.CreateReadyReport("000031");
        statusMismatch.FinalStatus = QaReportStatus.Fail;
        QuickQaSaveException statusFailure = ExpectSaveFailure(() =>
            environment.CreateSaveService(
                    environment.RealFileOperations,
                    new SequenceTimeProvider([SecondTimestamp]))
                .Save(statusMismatch, fileName));
        Check(
            statusFailure.ErrorCategory == QuickQaSaveErrorCategory.ReportInvalid
            && statusFailure.Stage == QuickQaSaveStage.Validation,
            "A stale FinalStatus was not rejected during preflight.");
        AssertPreflightDidNotMutate(
            environment,
            surfacePath,
            surfaceBefore,
            "FinalStatus-mismatch preflight");
    }

    private static void TestStagingAndNewHistoryRollback()
    {
        using (SyntheticEnvironment environment = new())
        {
            string fileName =
                environment.WorkbookService.CreateNewSurfaceWorkbook(
                    "Surface Staging Failure.xlsx");
            string surfacePath =
                environment.Paths.ResolveSurfaceQaWorkbookPath(fileName);
            byte[] surfaceBefore = File.ReadAllBytes(surfacePath);
            FaultInjectingFileOperations operations = new();
            operations.FailureFactory = call =>
                call.Kind == FileOperationKind.WriteNewAndFlush
                && IsStagePath(call.Path)
                && PathsEqual(
                    Path.GetDirectoryName(call.Path)!,
                    environment.Paths.SurfaceQaRootPath)
                    ? new IOException("Synthetic Surface staging failure.")
                    : null;

            QuickQaSaveException failure = ExpectSaveFailure(() =>
                environment.CreateSaveService(
                        operations,
                        new SequenceTimeProvider([FirstTimestamp]))
                    .Save(environment.CreateReadyReport("000040"), fileName));
            Check(
                failure.ErrorCategory ==
                    QuickQaSaveErrorCategory.SurfaceWorkbookInUse
                && failure.Stage == QuickQaSaveStage.SurfaceStaging
                && !failure.PreviousStateRestored
                && !failure.ManualReviewRequired,
                "A Surface staging failure was not safely classified before mutation. "
                + $"Actual: {failure.ErrorCategory}/{failure.Stage}, restored={failure.PreviousStateRestored}, manualReview={failure.ManualReviewRequired}.");
            AssertPreflightDidNotMutate(
                environment,
                surfacePath,
                surfaceBefore,
                "Surface-staging failure");
        }

        using (SyntheticEnvironment environment = CreateSeededEnvironment(
                   out string fileName))
        {
            string surfacePath =
                environment.Paths.ResolveSurfaceQaWorkbookPath(fileName);
            byte[] surfaceBefore = File.ReadAllBytes(surfacePath);
            byte[] historyBefore = File.ReadAllBytes(environment.HistoryPath);
            string historyDirectory =
                Path.GetDirectoryName(environment.HistoryPath)!;
            FaultInjectingFileOperations operations = new();
            operations.FailureFactory = call =>
                call.Kind == FileOperationKind.WriteNewAndFlush
                && IsStagePath(call.Path)
                && PathsEqual(
                    Path.GetDirectoryName(call.Path)!,
                    historyDirectory)
                    ? new IOException("Synthetic History staging failure.")
                    : null;

            QuickQaSaveException failure = ExpectSaveFailure(() =>
                environment.CreateSaveService(
                        operations,
                        new SequenceTimeProvider([SecondTimestamp]))
                    .Save(environment.CreateReadyReport("000041"), fileName));
            Check(
                failure.ErrorCategory ==
                    QuickQaSaveErrorCategory.HotelHistoryWorkbookInUse
                && failure.Stage == QuickQaSaveStage.HistoryStaging
                && !failure.PreviousStateRestored
                && !failure.ManualReviewRequired,
                "A History staging failure was not safely classified before mutation. "
                + $"Actual: {failure.ErrorCategory}/{failure.Stage}, restored={failure.PreviousStateRestored}, manualReview={failure.ManualReviewRequired}.");
            AssertRestoredPair(
                environment,
                surfacePath,
                surfaceBefore,
                historyBefore,
                "History staging failure");
        }

        using (SyntheticEnvironment environment = new())
        {
            string fileName =
                environment.WorkbookService.CreateNewSurfaceWorkbook(
                    "New History Rollback.xlsx");
            string surfacePath =
                environment.Paths.ResolveSurfaceQaWorkbookPath(fileName);
            byte[] surfaceBefore = File.ReadAllBytes(surfacePath);
            FaultInjectingFileOperations operations = new();
            operations.FailureFactory = call =>
                call.Kind == FileOperationKind.Move
                && call.DestinationPath is not null
                && PathsEqual(call.DestinationPath, environment.HistoryPath)
                && IsStagePath(call.Path)
                    ? new IOException("Synthetic new-History commit failure.")
                    : null;

            QuickQaSaveException failure = ExpectSaveFailure(() =>
                environment.CreateSaveService(
                        operations,
                        new SequenceTimeProvider([FirstTimestamp]))
                    .Save(environment.CreateReadyReport("000042"), fileName));
            Check(
                failure.ErrorCategory ==
                    QuickQaSaveErrorCategory.HotelHistoryWorkbookInUse
                && failure.Stage == QuickQaSaveStage.HistoryCommit
                && failure.PreviousStateRestored
                && !failure.ManualReviewRequired,
                "A failed first History commit did not report a successful rollback.");
            AssertPreflightDidNotMutate(
                environment,
                surfacePath,
                surfaceBefore,
                "new-History commit rollback");
        }
    }

    private static void TestSurfaceCommitRollback()
    {
        using SyntheticEnvironment environment = CreateSeededEnvironment(
            out string fileName);
        string surfacePath = environment.Paths.ResolveSurfaceQaWorkbookPath(
            fileName);
        byte[] surfaceBefore = File.ReadAllBytes(surfacePath);
        byte[] historyBefore = File.ReadAllBytes(environment.HistoryPath);
        FaultInjectingFileOperations operations = new();
        operations.FailureFactory = call =>
            call.Kind == FileOperationKind.Move
            && call.DestinationPath is not null
            && PathsEqual(call.DestinationPath, surfacePath)
            && IsStagePath(call.Path)
                ? new IOException("Synthetic Surface commit failure.")
                : null;

        QuickQaSaveException failure = ExpectSaveFailure(() =>
            environment.CreateSaveService(
                    operations,
                    new SequenceTimeProvider([SecondTimestamp]))
                .Save(environment.CreateReadyReport("000010"), fileName));
        Check(
            failure.ErrorCategory ==
                QuickQaSaveErrorCategory.SurfaceWorkbookInUse
            && failure.Stage == QuickQaSaveStage.SurfaceCommit
            && failure.PreviousStateRestored
            && !failure.ManualReviewRequired,
            "The Surface commit failure was not classified as a successful rollback.");
        AssertRestoredPair(
            environment,
            surfacePath,
            surfaceBefore,
            historyBefore,
            "Surface commit failure");
    }

    private static void TestHistoryCommitRollback()
    {
        using SyntheticEnvironment environment = CreateSeededEnvironment(
            out string fileName);
        string surfacePath = environment.Paths.ResolveSurfaceQaWorkbookPath(
            fileName);
        byte[] surfaceBefore = File.ReadAllBytes(surfacePath);
        byte[] historyBefore = File.ReadAllBytes(environment.HistoryPath);
        FaultInjectingFileOperations operations = new();
        operations.FailureFactory = call =>
            call.Kind == FileOperationKind.Move
            && call.DestinationPath is not null
            && PathsEqual(call.DestinationPath, environment.HistoryPath)
            && IsStagePath(call.Path)
                ? new IOException("Synthetic History commit failure.")
                : null;

        QuickQaSaveException failure = ExpectSaveFailure(() =>
            environment.CreateSaveService(
                    operations,
                    new SequenceTimeProvider([SecondTimestamp]))
                .Save(environment.CreateReadyReport("000020"), fileName));
        Check(
            failure.ErrorCategory ==
                QuickQaSaveErrorCategory.HotelHistoryWorkbookInUse
            && failure.Stage == QuickQaSaveStage.HistoryCommit
            && failure.PreviousStateRestored
            && !failure.ManualReviewRequired,
            "The History commit failure was not classified as a successful rollback.");
        AssertRestoredPair(
            environment,
            surfacePath,
            surfaceBefore,
            historyBefore,
            "History commit failure");
    }

    private static void TestStagedValidationMismatch()
    {
        using SyntheticEnvironment environment = new();
        string fileName = environment.WorkbookService.CreateNewSurfaceWorkbook(
            "Staged Validation Mismatch.xlsx");
        string surfacePath = environment.Paths.ResolveSurfaceQaWorkbookPath(
            fileName);
        byte[] surfaceBefore = File.ReadAllBytes(surfacePath);
        bool stagedContentCorrupted = false;
        FaultInjectingFileOperations operations = new();
        operations.BeforeCall = call =>
        {
            if (!stagedContentCorrupted
                && call.Kind == FileOperationKind.ComputeSha256
                && IsStagePath(call.Path)
                && PathsEqual(
                    Path.GetDirectoryName(call.Path)!,
                    environment.Paths.SurfaceQaRootPath))
            {
                CorruptFileWithoutChangingLength(call.Path);
                stagedContentCorrupted = true;
            }
        };

        QuickQaSaveException failure = ExpectSaveFailure(() =>
            environment.CreateSaveService(
                    operations,
                    new SequenceTimeProvider([FirstTimestamp]))
                .Save(environment.CreateReadyReport("000050"), fileName));
        Check(
            stagedContentCorrupted
            && failure.ErrorCategory ==
                QuickQaSaveErrorCategory.ContentVerificationFailure
            && failure.Stage == QuickQaSaveStage.SurfaceStaging
            && !failure.PreviousStateRestored
            && !failure.ManualReviewRequired,
            "A staged Surface hash mismatch did not block commit with the exact verification classification.");
        AssertPreflightDidNotMutate(
            environment,
            surfacePath,
            surfaceBefore,
            "staged-validation mismatch");
    }

    private static void TestFinalVerificationMismatch()
    {
        using SyntheticEnvironment environment = CreateSeededEnvironment(
            out string fileName);
        string surfacePath = environment.Paths.ResolveSurfaceQaWorkbookPath(
            fileName);
        byte[] surfaceBefore = File.ReadAllBytes(surfacePath);
        byte[] historyBefore = File.ReadAllBytes(environment.HistoryPath);
        bool bothCommitsCompleted = false;
        bool finalContentCorrupted = false;
        FaultInjectingFileOperations operations = new();
        operations.AfterCall = call =>
        {
            if (call.Kind == FileOperationKind.Move
                && call.DestinationPath is not null
                && PathsEqual(call.DestinationPath, environment.HistoryPath)
                && IsStagePath(call.Path))
            {
                bothCommitsCompleted = true;
            }
        };
        operations.BeforeCall = call =>
        {
            if (bothCommitsCompleted
                && !finalContentCorrupted
                && call.Kind == FileOperationKind.ComputeSha256
                && PathsEqual(call.Path, surfacePath))
            {
                CorruptFileWithoutChangingLength(surfacePath);
                finalContentCorrupted = true;
            }
        };

        QuickQaSaveException failure = ExpectSaveFailure(() =>
            environment.CreateSaveService(
                    operations,
                    new SequenceTimeProvider([SecondTimestamp]))
                .Save(environment.CreateReadyReport("000051"), fileName));
        Check(
            bothCommitsCompleted
            && finalContentCorrupted
            && failure.ErrorCategory ==
                QuickQaSaveErrorCategory.ContentVerificationFailure
            && failure.Stage == QuickQaSaveStage.FinalVerification
            && failure.PreviousStateRestored
            && !failure.ManualReviewRequired,
            "A final Surface hash mismatch did not report safe recovery after both commits.");
        AssertRestoredPair(
            environment,
            surfacePath,
            surfaceBefore,
            historyBefore,
            "final-verification mismatch");
    }

    private static void TestConcurrentChange()
    {
        using SyntheticEnvironment environment = new();
        string fileName = environment.WorkbookService.CreateNewSurfaceWorkbook(
            "Concurrent Surface.xlsx");
        string surfacePath = environment.Paths.ResolveSurfaceQaWorkbookPath(
            fileName);
        byte[] externalContent = CreateWorkbookBytes(
            [
                new WorksheetFixture(
                    QuickQaWorkbookService.SurfaceWorksheetName,
                    QuickQaWorkbookService.SurfaceHeaders.ToArray(),
                    [
                        [
                            "2026-07",
                            "External Hotel",
                            "EXT-001",
                            "External PMS",
                            "009900",
                            "Pass",
                            "External edit retained."
                        ]
                    ])
            ]);
        bool externalChangeApplied = false;
        FaultInjectingFileOperations operations = new();
        operations.BeforeCall = call =>
        {
            if (!externalChangeApplied
                && call.Kind == FileOperationKind.ComputeSha256
                && PathsEqual(call.Path, surfacePath))
            {
                File.WriteAllBytes(surfacePath, externalContent);
                externalChangeApplied = true;
            }
        };

        QuickQaSaveException failure = ExpectSaveFailure(() =>
            environment.CreateSaveService(
                    operations,
                    new SequenceTimeProvider([FirstTimestamp]))
                .Save(environment.CreateReadyReport("001234"), fileName));
        Check(
            externalChangeApplied
            && failure.ErrorCategory == QuickQaSaveErrorCategory.ConcurrentChange
            && failure.Stage == QuickQaSaveStage.ConcurrencyCheck
            && File.ReadAllBytes(surfacePath).SequenceEqual(externalContent)
            && !File.Exists(environment.HistoryPath),
            "A concurrent Surface edit was overwritten or misclassified.");
        AssertNoTransactionArtifacts(environment.RootPath);
    }

    private static void TestPostBackupConcurrentChange()
    {
        using SyntheticEnvironment environment = CreateSeededEnvironment(
            out string fileName);
        string surfacePath = environment.Paths.ResolveSurfaceQaWorkbookPath(
            fileName);
        byte[] surfaceBefore = File.ReadAllBytes(surfacePath);
        byte[] historyBefore = File.ReadAllBytes(environment.HistoryPath);
        byte[] externalContent = CreateEditedSurfaceVersion(
            surfaceBefore,
            "External edit made after the live workbook was renamed.");
        bool externalChangeApplied = false;
        FaultInjectingFileOperations operations = new();
        operations.AfterCall = call =>
        {
            if (!externalChangeApplied
                && call.Kind == FileOperationKind.Move
                && call.DestinationPath is not null
                && PathsEqual(call.Path, surfacePath)
                && IsRollbackPath(call.DestinationPath))
            {
                File.WriteAllBytes(call.DestinationPath, externalContent);
                externalChangeApplied = true;
            }
        };

        QuickQaSaveException failure = ExpectSaveFailure(() =>
            environment.CreateSaveService(
                    operations,
                    new SequenceTimeProvider([SecondTimestamp]))
                .Save(environment.CreateReadyReport("000052"), fileName));
        Check(
            externalChangeApplied
            && failure.ErrorCategory == QuickQaSaveErrorCategory.ConcurrentChange
            && failure.Stage == QuickQaSaveStage.Backup
            && failure.PreviousStateRestored
            && !failure.ManualReviewRequired
            && File.ReadAllBytes(surfacePath).SequenceEqual(externalContent)
            && File.ReadAllBytes(environment.HistoryPath).SequenceEqual(
                historyBefore),
            "A Surface edit in the post-backup TOCTOU window was lost, overwritten, or misclassified.");
        AssertNoTransactionArtifacts(environment.RootPath);
    }

    private static SyntheticEnvironment CreateSeededEnvironment(
        out string fileName)
    {
        SyntheticEnvironment environment = new();

        try
        {
            fileName = environment.WorkbookService.CreateNewSurfaceWorkbook(
                "Seeded Surface.xlsx");
            _ = environment.CreateSaveService(
                    environment.RealFileOperations,
                    new SequenceTimeProvider([FirstTimestamp]))
                .Save(environment.CreateReadyReport("000001"), fileName);
            return environment;
        }
        catch
        {
            environment.Dispose();
            throw;
        }
    }

    private static void AssertRestoredPair(
        SyntheticEnvironment environment,
        string surfacePath,
        byte[] surfaceBefore,
        byte[] historyBefore,
        string scenario)
    {
        Check(
            File.Exists(surfacePath)
            && File.Exists(environment.HistoryPath)
            && File.ReadAllBytes(surfacePath).SequenceEqual(surfaceBefore)
            && File.ReadAllBytes(environment.HistoryPath).SequenceEqual(
                historyBefore),
            $"{scenario} did not restore the exact prior workbook bytes.");
        AssertNoTransactionArtifacts(environment.RootPath);
    }

    private static void AssertPreflightDidNotMutate(
        SyntheticEnvironment environment,
        string surfacePath,
        byte[] surfaceBefore,
        string scenario)
    {
        Check(
            File.ReadAllBytes(surfacePath).SequenceEqual(surfaceBefore)
            && !File.Exists(environment.HistoryPath),
            $"The {scenario} changed a destination workbook.");
        AssertNoTransactionArtifacts(environment.RootPath);
    }

    private static void AssertSurfaceRow(
        IXLWorksheet worksheet,
        int rowNumber,
        SyntheticEnvironment environment,
        string fileId,
        string summary)
    {
        AssertExactRow(
            worksheet,
            rowNumber,
            [
                "2026-08",
                environment.Hotel.HotelName,
                environment.Hotel.HotelId,
                environment.Pms.PmsName,
                fileId,
                "Pass",
                summary
            ],
            $"Surface row {rowNumber}");
    }

    private static void AssertHistoryRow(
        IXLWorksheet worksheet,
        int rowNumber,
        SyntheticEnvironment environment,
        DateTimeOffset timestamp,
        string fileId,
        string summary)
    {
        AssertExactRow(
            worksheet,
            rowNumber,
            [
                timestamp.ToString("O", CultureInfo.InvariantCulture),
                "2026-08",
                environment.Hotel.HotelName,
                environment.Hotel.HotelId,
                environment.Pms.PmsName,
                fileId,
                "Pass",
                summary
            ],
            $"History row {rowNumber}");
    }

    private static void AssertHeaders(
        IXLWorksheet worksheet,
        IReadOnlyList<string> expectedHeaders)
    {
        AssertExactRow(
            worksheet,
            rowNumber: 1,
            expectedHeaders,
            $"'{worksheet.Name}' headers");
    }

    private static int CountWorkbookDataRows(
        string path,
        string worksheetName,
        int columnCount)
    {
        using XLWorkbook workbook = new(path);
        IXLWorksheet worksheet = workbook.Worksheet(worksheetName);
        return worksheet.RowsUsed(XLCellsUsedOptions.Contents)
            .Count(row => row.RowNumber() > 1
                && Enumerable.Range(1, columnCount).Any(column =>
                    !string.IsNullOrWhiteSpace(
                        row.Cell(column).GetString())));
    }

    private static void AssertExactRow(
        IXLWorksheet worksheet,
        int rowNumber,
        IReadOnlyList<string> expectedValues,
        string context)
    {
        for (int column = 1; column <= expectedValues.Count; column++)
        {
            string actual = worksheet.Cell(rowNumber, column).GetString();
            Check(
                actual == expectedValues[column - 1],
                $"{context} column {column} was '{actual}', expected '{expectedValues[column - 1]}'.");
        }
    }

    private static void AssertTextCell(
        IXLCell cell,
        string expectedValue,
        string context)
    {
        Check(
            cell.GetString() == expectedValue
            && cell.DataType == XLDataType.Text
            && cell.Style.NumberFormat.Format == "@",
            $"The {context} was not stored as exact Excel text.");
    }

    private static void AssertPresentation(
        IXLWorksheet worksheet,
        string tableName,
        int columnCount,
        int expectedLastRow,
        int summaryColumn)
    {
        IXLTable[] tables = worksheet.Tables
            .Where(table => table.Name.Equals(
                tableName,
                StringComparison.Ordinal))
            .ToArray();
        Check(
            tables.Length == 1
            && tables[0].RangeAddress.FirstAddress.RowNumber == 1
            && tables[0].RangeAddress.FirstAddress.ColumnNumber == 1
            && tables[0].RangeAddress.LastAddress.RowNumber == expectedLastRow
            && tables[0].ColumnCount() == columnCount
            && tables[0].ShowAutoFilter,
            $"'{worksheet.Name}' does not have the exact expanding Table/filter contract.");
        Check(
            worksheet.SheetView.SplitRow >= 1,
            $"'{worksheet.Name}' does not freeze the header row.");
        Check(
            worksheet.Cell(expectedLastRow, summaryColumn)
                .Style.Alignment.WrapText,
            $"'{worksheet.Name}' does not wrap the appended Summary cell.");
    }

    private static void AssertWorkbookCategoryAndUnchanged(
        SyntheticEnvironment environment,
        string fileName,
        QuickQaWorkbookErrorCategory expectedCategory)
    {
        string path = environment.Paths.ResolveSurfaceQaWorkbookPath(fileName);
        byte[] before = File.ReadAllBytes(path);
        QuickQaWorkbookException exception = ExpectWorkbookFailure(
            () => environment.WorkbookService.ValidateSurfaceWorkbook(fileName));
        Check(
            exception.ErrorCategory == expectedCategory
            && exception.Role == QuickQaWorkbookRole.Surface,
            $"'{fileName}' was {exception.ErrorCategory}, expected {expectedCategory}.");
        Check(
            File.ReadAllBytes(path).SequenceEqual(before),
            $"Validation rewrote rejected workbook '{fileName}'.");
    }

    private static QuickQaWorkbookException ExpectWorkbookFailure(Action action)
    {
        try
        {
            action();
        }
        catch (QuickQaWorkbookException exception)
        {
            return exception;
        }

        throw new InvalidOperationException(
            "Expected a QuickQaWorkbookException, but the operation succeeded.");
    }

    private static QuickQaSaveException ExpectSaveFailure(Action action)
    {
        try
        {
            action();
        }
        catch (QuickQaSaveException exception)
        {
            return exception;
        }

        throw new InvalidOperationException(
            "Expected a QuickQaSaveException, but the transaction succeeded.");
    }

    private static void AssertNoTransactionArtifacts(string rootPath)
    {
        string[] artifacts = Directory.EnumerateFiles(
                rootPath,
                "*",
                SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).Contains(
                ".quickqa-",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Check(
            artifacts.Length == 0,
            "Quick QA transaction artifacts remain: "
            + string.Join(", ", artifacts.Select(Path.GetFileName)));
    }

    private static bool IsStagePath(string path)
    {
        return Path.GetFileName(path).Contains(
            ".quickqa-",
            StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(".stage.xlsx", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRollbackPath(string path)
    {
        return Path.GetFileName(path).Contains(
            ".quickqa-",
            StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(
                ".rollback.xlsx",
                StringComparison.OrdinalIgnoreCase);
    }

    private static void CorruptFileWithoutChangingLength(string path)
    {
        byte[] content = File.ReadAllBytes(path);
        Check(
            content.Length > 32,
            $"Synthetic workbook '{path}' was too small for deterministic corruption.");
        int index = content.Length / 2;
        content[index] ^= 0x5A;
        File.WriteAllBytes(path, content);
        Check(
            new FileInfo(path).Length == content.LongLength,
            "Synthetic corruption unexpectedly changed workbook length.");
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteWorkbook(
        string path,
        IReadOnlyList<WorksheetFixture> worksheets)
    {
        byte[] content = CreateWorkbookBytes(worksheets);
        File.WriteAllBytes(path, content);
    }

    private static byte[] CreateWorkbookBytes(
        IReadOnlyList<WorksheetFixture> worksheets)
    {
        using XLWorkbook workbook = new();

        foreach (WorksheetFixture fixture in worksheets)
        {
            IXLWorksheet worksheet = workbook.Worksheets.Add(fixture.Name);
            for (int column = 1; column <= fixture.Headers.Count; column++)
            {
                worksheet.Cell(1, column).Value = fixture.Headers[column - 1];
            }

            for (int row = 0; row < fixture.Rows.Count; row++)
            {
                IReadOnlyList<string> values = fixture.Rows[row];
                for (int column = 1; column <= values.Count; column++)
                {
                    IXLCell cell = worksheet.Cell(row + 2, column);
                    if (column is 1 or 3 or 5)
                    {
                        cell.Style.NumberFormat.Format = "@";
                    }

                    cell.Value = values[column - 1];
                }
            }
        }

        using MemoryStream output = new();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static byte[] CreateEditedSurfaceVersion(
        ReadOnlyMemory<byte> source,
        string editedSummary)
    {
        using MemoryStream input = new(source.ToArray(), writable: false);
        using XLWorkbook workbook = new(input);
        IXLWorksheet worksheet = workbook.Worksheet(
            QuickQaWorkbookService.SurfaceWorksheetName);
        worksheet.Cell(2, 7).Value = editedSummary;
        using MemoryStream output = new();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private sealed record WorksheetFixture(
        string Name,
        IReadOnlyList<string> Headers,
        IReadOnlyList<IReadOnlyList<string>> Rows);

    private sealed class SyntheticEnvironment : IDisposable
    {
        private bool disposed;

        public SyntheticEnvironment()
        {
            RootPath = Path.Combine(
                Path.GetTempPath(),
                $"DocumentationLoggingDashboard.QuickWorkbook.{Guid.NewGuid():N}");
            Paths = new QaStoragePaths(RootPath);
            new QaStorageInitializer(Paths).Initialize();
            MetadataService = new QaMetadataService(
                Paths,
                new QaFolderNameSanitizer());
            Pms = MetadataService.AddPmsSystem("Synthetic Quick PMS");
            Hotel = MetadataService.AddHotel(
                "000099",
                "Synthetic Quick Hotel",
                Pms.PmsName);
            RealFileOperations = new QuickQaTransactionFileOperations();
            WorkbookService = new QuickQaWorkbookService(Paths);
            HistoryPath = Paths.ResolveQuickQaHistoryPath(Hotel.FolderName);
        }

        public string RootPath { get; }

        public QaStoragePaths Paths { get; }

        public QaMetadataService MetadataService { get; }

        public QaHotelMetadata Hotel { get; }

        public QaPmsMetadata Pms { get; }

        public IQuickQaTransactionFileOperations RealFileOperations { get; }

        public QuickQaWorkbookService WorkbookService { get; }

        public string HistoryPath { get; }

        public QuickQaReport CreateReadyReport(string fileId)
        {
            QuickQaReport report = new()
            {
                HotelInformation = new QaHotelInformation
                {
                    HotelId = Hotel.HotelId,
                    HotelName = Hotel.HotelName,
                    PmsName = Pms.PmsName,
                    FileMonth = new QaFileMonth(2026, 8)
                },
                FileId = fileId
            };

            foreach (QuickQaCheckResult result in report.ChecklistResults)
            {
                result.Status = QuickQaCheckStatus.Pass;
            }

            _ = new QuickQaFindingSynchronizationService().Synchronize(report);
            new QuickQaSummaryService().Regenerate(report);
            return report;
        }

        public QuickQaSaveService CreateSaveService(
            IQuickQaTransactionFileOperations fileOperations,
            TimeProvider timeProvider)
        {
            QuickQaWorkbookFilenameService filenameService = new(Paths);
            QuickQaWorkbookService workbookService = new(
                Paths,
                filenameService,
                fileOperations);
            return new QuickQaSaveService(
                Paths,
                MetadataService,
                filenameService,
                workbookService,
                fileOperations,
                new QuickQaFindingSynchronizationService(),
                new QuickQaSummaryService(),
                timeProvider);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }

    private sealed class SequenceTimeProvider : TimeProvider
    {
        private readonly Queue<DateTimeOffset> utcTimes;

        public SequenceTimeProvider(IEnumerable<DateTimeOffset> localTimes)
        {
            DateTimeOffset[] snapshot = localTimes.ToArray();
            if (snapshot.Length == 0)
            {
                throw new ArgumentException(
                    "At least one synthetic timestamp is required.",
                    nameof(localTimes));
            }

            TimeSpan offset = snapshot[0].Offset;
            if (snapshot.Any(value => value.Offset != offset))
            {
                throw new ArgumentException(
                    "Synthetic timestamps must use one fixed offset.",
                    nameof(localTimes));
            }

            LocalTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                "Synthetic Quick QA Offset",
                offset,
                "Synthetic Quick QA Offset",
                "Synthetic Quick QA Offset");
            utcTimes = new Queue<DateTimeOffset>(
                snapshot.Select(value => value.ToUniversalTime()));
        }

        public override TimeZoneInfo LocalTimeZone { get; }

        public override DateTimeOffset GetUtcNow()
        {
            if (utcTimes.Count == 0)
            {
                throw new InvalidOperationException(
                    "The synthetic TimeProvider was read more often than expected.");
            }

            return utcTimes.Dequeue();
        }
    }

    private enum FileOperationKind
    {
        CreateDirectory,
        FileExists,
        ReadAllBytes,
        WriteNewAndFlush,
        GetFileLength,
        ComputeSha256,
        GetLastWriteTimeUtc,
        VerifyExclusiveAccess,
        OpenExclusiveReadLease,
        Move,
        Delete
    }

    private sealed record FileOperationCall(
        FileOperationKind Kind,
        string Path,
        string? DestinationPath = null);

    private sealed class FaultInjectingFileOperations
        : IQuickQaTransactionFileOperations
    {
        private readonly IQuickQaTransactionFileOperations inner =
            new QuickQaTransactionFileOperations();

        public Action<FileOperationCall>? BeforeCall { get; set; }

        public Action<FileOperationCall>? AfterCall { get; set; }

        public Func<FileOperationCall, Exception?>? FailureFactory { get; set; }

        public void CreateDirectory(string path)
        {
            FileOperationCall call = new(
                FileOperationKind.CreateDirectory,
                path);
            Invoke(call);
            inner.CreateDirectory(path);
            Complete(call);
        }

        public bool FileExists(string path)
        {
            FileOperationCall call = new(FileOperationKind.FileExists, path);
            Invoke(call);
            bool result = inner.FileExists(path);
            Complete(call);
            return result;
        }

        public byte[] ReadAllBytes(string path)
        {
            FileOperationCall call = new(FileOperationKind.ReadAllBytes, path);
            Invoke(call);
            byte[] result = inner.ReadAllBytes(path);
            Complete(call);
            return result;
        }

        public void WriteNewAndFlush(
            string path,
            ReadOnlyMemory<byte> content)
        {
            FileOperationCall call = new(
                FileOperationKind.WriteNewAndFlush,
                path);
            Invoke(call);
            inner.WriteNewAndFlush(path, content);
            Complete(call);
        }

        public long GetFileLength(string path)
        {
            FileOperationCall call = new(
                FileOperationKind.GetFileLength,
                path);
            Invoke(call);
            long result = inner.GetFileLength(path);
            Complete(call);
            return result;
        }

        public byte[] ComputeSha256(string path)
        {
            FileOperationCall call = new(
                FileOperationKind.ComputeSha256,
                path);
            Invoke(call);
            byte[] result = inner.ComputeSha256(path);
            Complete(call);
            return result;
        }

        public DateTime GetLastWriteTimeUtc(string path)
        {
            FileOperationCall call = new(
                FileOperationKind.GetLastWriteTimeUtc,
                path);
            Invoke(call);
            DateTime result = inner.GetLastWriteTimeUtc(path);
            Complete(call);
            return result;
        }

        public void VerifyExclusiveAccess(string path)
        {
            FileOperationCall call = new(
                FileOperationKind.VerifyExclusiveAccess,
                path);
            Invoke(call);
            inner.VerifyExclusiveAccess(path);
            Complete(call);
        }

        public IQuickQaExclusiveReadLease OpenExclusiveReadLease(string path)
        {
            FileOperationCall call = new(
                FileOperationKind.OpenExclusiveReadLease,
                path);
            Invoke(call);
            IQuickQaExclusiveReadLease result =
                inner.OpenExclusiveReadLease(path);
            Complete(call);
            return result;
        }

        public void Move(string sourcePath, string destinationPath)
        {
            FileOperationCall call = new(
                FileOperationKind.Move,
                sourcePath,
                destinationPath);
            Invoke(call);
            inner.Move(sourcePath, destinationPath);
            Complete(call);
        }

        public void Delete(string path)
        {
            FileOperationCall call = new(FileOperationKind.Delete, path);
            Invoke(call);
            inner.Delete(path);
            Complete(call);
        }

        private void Invoke(FileOperationCall call)
        {
            BeforeCall?.Invoke(call);
            Exception? failure = FailureFactory?.Invoke(call);
            if (failure is not null)
            {
                throw failure;
            }
        }

        private void Complete(FileOperationCall call)
        {
            AfterCall?.Invoke(call);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
