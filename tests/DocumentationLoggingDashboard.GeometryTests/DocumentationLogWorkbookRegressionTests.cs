using ClosedXML.Excel;
using DocumentationLoggingDashboard.DocumentationLogs;
using DocumentationLoggingDashboard.Models;
using System.Text.Json;

namespace DocumentationLoggingDashboard.GeometryTests;

internal static class DocumentationLogWorkbookRegressionTests
{
    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        (string Name, Action Body)[] tests =
        [
            ("exact schemas, metadata, presentation, and typed appended row", TestExactSchemasAndAppend),
            ("Excel built-in Text format survives validation and append", TestBuiltInTextFormat),
            ("type, scope, marker, version, header, and corrupt rejection", TestCompatibilityRejections),
            ("strict filename safety, create-new semantics, and discovery", TestFilenameAndDiscovery),
            ("three independent atomic Running workbook preferences", TestPreferences)
        ];
        return Run(tests, output);
    }

    private static void TestExactSchemasAndAppend()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        Dictionary<LogType, string> files = new()
        {
            [LogType.DebuggingLog] = environment.CreateRunning(LogType.DebuggingLog, "Exact Debug"),
            [LogType.ScriptEditingLog] = environment.CreateRunning(LogType.ScriptEditingLog, "Exact Edit"),
            [LogType.ScriptCreationLog] = environment.CreateRunning(LogType.ScriptCreationLog, "Exact Create")
        };

        foreach ((LogType logType, string fileName) in files)
        {
            DocumentationLogWorkbookInfo info = environment.WorkbookService
                .ValidateRunningWorkbook(logType, fileName);
            DocumentationLogTestAssert.Equal(
                0,
                info.DataRowCount,
                $"A new {logType} Running workbook was not empty.");
            AssertWorkbookContract(
                info.FullPath,
                DocumentationLogWorkbookSchema.CreateRunningContract(logType),
                expectedRows: 0);
        }

        foreach (LogType logType in DocumentationLogWorkbookSchema.SupportedLogTypes)
        {
            DocumentationLogSaveRequest request = logType == LogType.DebuggingLog
                ? environment.CreateDebuggingRequest(
                    files[logType],
                    createdBy: string.Empty,
                    notes: string.Empty)
                : environment.CreateScriptRequest(
                    logType,
                    files[logType],
                    "0012; 1953; 2093",
                    createdBy: string.Empty,
                    notes: string.Empty);
            DocumentationLogSaveResult saved = environment.CreateSaveService()
                .Save(request);
            AssertWorkbookContract(
                saved.RunningWorkbookPath,
                DocumentationLogWorkbookSchema.CreateRunningContract(logType),
                expectedRows: 1);

            foreach (string hotelHistoryPath in saved.HotelHistoryPaths)
            {
                DocumentationLogHotel hotel = saved.Event.Hotels.Single(candidate =>
                    hotelHistoryPath.Equals(
                        environment.Paths.ResolveHotelHistoryPath(
                            logType,
                            candidate.FolderName),
                        StringComparison.OrdinalIgnoreCase));
                AssertWorkbookContract(
                    hotelHistoryPath,
                    DocumentationLogWorkbookSchema.CreateHotelHistoryContract(
                        logType,
                        hotel),
                    expectedRows: 1);
            }

            foreach (string pmsHistoryPath in saved.PmsHistoryPaths)
            {
                DocumentationLogPms pms = saved.Event.Hotels
                    .Select(hotel => hotel.Pms)
                    .DistinctBy(item => item.FolderName, StringComparer.OrdinalIgnoreCase)
                    .Single(candidate => pmsHistoryPath.Equals(
                        environment.Paths.ResolvePmsHistoryPath(
                            logType,
                            candidate.FolderName),
                        StringComparison.OrdinalIgnoreCase));
                AssertWorkbookContract(
                    pmsHistoryPath,
                    DocumentationLogWorkbookSchema.CreatePmsHistoryContract(
                        logType,
                        pms),
                    expectedRows: 1);
            }

            using XLWorkbook workbook = new(saved.RunningWorkbookPath);
            IXLWorksheet worksheet = workbook.Worksheet(
                DocumentationLogWorkbookSchema.DataWorksheetName);
            DocumentationLogTestAssert.Equal(
                XLDataType.DateTime,
                worksheet.Cell(4, 1).DataType,
                $"{logType} Date/Time was not stored as a real Excel DateTime.");
            AssertTextCell(worksheet.Cell(4, 2), saved.Event.LogId, "Log ID");

            int optionalStartColumn;
            if (logType == LogType.DebuggingLog)
            {
                AssertTextCell(worksheet.Cell(4, 4), "1953", "Hotel ID");
                optionalStartColumn = 9;
            }
            else
            {
                AssertTextCell(
                    worksheet.Cell(4, 3),
                    "0012; 1953; 2093",
                    "Hotel IDs");
                optionalStartColumn = 7;
            }

            DocumentationLogTestAssert.Equal(
                "N/A",
                worksheet.Cell(4, optionalStartColumn).GetString(),
                $"{logType} blank Created By did not preserve N/A.");
            DocumentationLogTestAssert.Equal(
                "N/A",
                worksheet.Cell(4, optionalStartColumn + 1).GetString(),
                $"{logType} blank Notes did not preserve N/A.");
        }
    }

    private static void TestBuiltInTextFormat()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        foreach (LogType type in DocumentationLogWorkbookSchema.SupportedLogTypes)
        {
            string fileName = environment.CreateRunning(type, $"Excel Text {type}");
            DocumentationLogSaveRequest request = type == LogType.DebuggingLog
                ? environment.CreateDebuggingRequest(fileName, environment.HotelLeadingZero)
                : environment.CreateScriptRequest(type, fileName, "0012; 1953");
            DocumentationLogSaveResult first = environment.CreateSaveService().Save(request);
            int hotelColumn = type == LogType.DebuggingLog ? 4 : 3;
            int[] idColumns = [2, hotelColumn];
            string[] paths = [first.RunningWorkbookPath, .. first.HotelHistoryPaths, .. first.PmsHistoryPaths];

            foreach (string path in paths)
            {
                byte[] excelSaved = Mutate(File.ReadAllBytes(path), workbook =>
                {
                    IXLWorksheet sheet = workbook.Worksheet(DocumentationLogWorkbookSchema.DataWorksheetName);
                    foreach (int column in idColumns)
                    {
                        // Excel saves Text using built-in format 49, not a custom "@" format.
                        sheet.Cell(4, column).Style.NumberFormat.NumberFormatId = 49;
                    }
                });
                File.WriteAllBytes(path, excelSaved);
            }

            environment.WorkbookService.ValidateRunningWorkbook(type, fileName);
            DocumentationLogTestAssert.True(
                environment.WorkbookService.GetCompatibleRunningWorkbookFileNames(type).Contains(fileName),
                "Discovery omitted a workbook saved with Excel's built-in Text format.");
            environment.CreateSaveService().Save(request);

            foreach (string path in paths)
            {
                using XLWorkbook workbook = new(path);
                IXLWorksheet sheet = workbook.Worksheet(DocumentationLogWorkbookSchema.DataWorksheetName);
                DocumentationLogTestAssert.Equal(first.Event.LogId, sheet.Cell(4, 2).GetString(),
                    "Appending changed a historical Log ID.");
                DocumentationLogTestAssert.True(sheet.Cell(4, hotelColumn).GetString().Contains("0012"),
                    "Appending lost a historical Hotel ID's leading zeroes.");
                DocumentationLogTestAssert.Equal(5, sheet.Tables.Single().RangeAddress.LastAddress.RowNumber,
                    "The save did not append exactly one row to every workbook.");
            }

            byte[] valid = File.ReadAllBytes(first.RunningWorkbookPath);
            DocumentationLogWorkbookContract contract = DocumentationLogWorkbookSchema.CreateRunningContract(type);
            foreach (int column in idColumns)
            {
                AssertRejected(environment, Mutate(valid, workbook =>
                    workbook.Worksheet(DocumentationLogWorkbookSchema.DataWorksheetName)
                        .Cell(4, column).Value = 12), contract, first.Event,
                    DocumentationLogWorkbookErrorCategory.IncompatibleSchema);
                AssertRejected(environment, Mutate(valid, workbook =>
                    workbook.Worksheet(DocumentationLogWorkbookSchema.DataWorksheetName)
                        .Cell(4, column).Style.NumberFormat.NumberFormatId = 0), contract, first.Event,
                    DocumentationLogWorkbookErrorCategory.IncompatibleSchema);
            }
        }
    }

    private static void TestCompatibilityRejections()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        foreach (LogType type in DocumentationLogWorkbookSchema.SupportedLogTypes)
        {
            string fileName = environment.CreateRunning(
                type,
                $"Reject {type}");
            string path = environment.Paths.ResolveRunningWorkbookPath(
                type,
                fileName);
            byte[] original = File.ReadAllBytes(path);
            DocumentationLogSaveRequest request = type == LogType.DebuggingLog
                ? environment.CreateDebuggingRequest(fileName)
                : environment.CreateScriptRequest(type, fileName, "1953; 3001");
            DocumentationLogEvent logEvent = environment.CreateSaveService()
                .CreatePreview(request);
            DocumentationLogWorkbookContract contract =
                DocumentationLogWorkbookSchema.CreateRunningContract(type);
            LogType wrongType = type == LogType.DebuggingLog
                ? LogType.ScriptEditingLog
                : LogType.DebuggingLog;

            AssertRejected(
                environment,
                Mutate(original, workbook =>
                    workbook.Worksheet(DocumentationLogWorkbookSchema.MetadataWorksheetName)
                        .Delete()),
                contract,
                logEvent,
                DocumentationLogWorkbookErrorCategory.MissingMetadata);
            AssertRejected(
                environment,
                Mutate(original, workbook =>
                    workbook.Worksheet(DocumentationLogWorkbookSchema.MetadataWorksheetName)
                        .Cell(2, 2).Value = 99),
                contract,
                logEvent,
                DocumentationLogWorkbookErrorCategory.UnsupportedSchemaVersion);
            AssertRejected(
                environment,
                Mutate(original, workbook =>
                    workbook.Worksheet(DocumentationLogWorkbookSchema.MetadataWorksheetName)
                        .Cell(3, 2).Value = wrongType.ToString()),
                contract,
                logEvent,
                DocumentationLogWorkbookErrorCategory.WrongLogType);
            AssertRejected(
                environment,
                Mutate(original, workbook =>
                {
                    IXLWorksheet metadata = workbook.Worksheet(
                        DocumentationLogWorkbookSchema.MetadataWorksheetName);
                    metadata.Cell(4, 2).Value =
                        DocumentationLogScopeType.Hotel.ToString();
                    metadata.Cell(5, 2).Value = "1953";
                }),
                contract,
                logEvent,
                DocumentationLogWorkbookErrorCategory.WrongScope);
            AssertRejected(
                environment,
                Mutate(original, workbook =>
                    workbook.Worksheet(DocumentationLogWorkbookSchema.DataWorksheetName)
                        .Cell(3, 3).Clear(XLClearOptions.Contents)),
                contract,
                logEvent,
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema);
            AssertRejected(
                environment,
                Mutate(original, workbook =>
                    workbook.Worksheet(DocumentationLogWorkbookSchema.DataWorksheetName)
                        .Cell(3, 3).Value = "Changed Header"),
                contract,
                logEvent,
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema);
            AssertRejected(
                environment,
                Mutate(original, workbook =>
                    workbook.Worksheet(DocumentationLogWorkbookSchema.DataWorksheetName)
                        .Name = "Changed Data Sheet"),
                contract,
                logEvent,
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema);
            AssertRejected(
                environment,
                [1, 2, 3, 4, 5],
                contract,
                logEvent,
                DocumentationLogWorkbookErrorCategory.Corrupt);

            DocumentationLogTestAssert.True(
                File.ReadAllBytes(path).SequenceEqual(original),
                $"Rejected {type} compatibility checks altered the original Running workbook.");
        }

        AssertIncompatibleHistoryIsNotReplaced(environment);
    }

    private static void AssertIncompatibleHistoryIsNotReplaced(
        DocumentationLogSyntheticEnvironment environment)
    {
        string running = environment.CreateRunning(
            LogType.ScriptCreationLog,
            "History Rejection");
        DocumentationLogSaveRequest request = environment.CreateScriptRequest(
            LogType.ScriptCreationLog,
            running,
            "1953");
        DocumentationLogEvent logEvent = environment.CreateSaveService()
            .CreatePreview(request);
        DocumentationLogHotel hotel = logEvent.Hotels.Single();
        DocumentationLogWorkbookContract contract =
            DocumentationLogWorkbookSchema.CreateHotelHistoryContract(
                LogType.ScriptCreationLog,
                hotel);
        byte[] validHistory = environment.WorkbookService.BuildAppend(
            sourceContent: null,
            contract,
            logEvent).Content;
        byte[] incompatibleHistory = Mutate(validHistory, workbook =>
            workbook.Worksheet(DocumentationLogWorkbookSchema.DataWorksheetName)
                .Cell(3, 6).Value = "Changed Header");
        string historyPath = environment.Paths.ResolveHotelHistoryPath(
            LogType.ScriptCreationLog,
            hotel.FolderName);
        Directory.CreateDirectory(Path.GetDirectoryName(historyPath)!);
        File.WriteAllBytes(historyPath, incompatibleHistory);

        DocumentationLogSaveException exception =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => environment.CreateSaveService().Save(request),
                "An incompatible existing Hotel history was silently replaced.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveErrorCategory.WorkbookInvalid,
            exception.ErrorCategory,
            "An incompatible history used the wrong save failure category.");
        DocumentationLogTestAssert.True(
            File.ReadAllBytes(historyPath).SequenceEqual(incompatibleHistory),
            "An incompatible existing history was repaired, reset, or overwritten.");
        DocumentationLogTestAssert.True(
            !File.Exists(environment.Paths.LogIndexFilePath)
            && !File.Exists(environment.Paths.SequenceStateFilePath),
            "Rejecting an incompatible history committed index or sequence state.");
    }

    private static void TestFilenameAndDiscovery()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        DocumentationLogWorkbookFilenameService filenames = new(environment.Paths);
        DocumentationLogTestAssert.Equal(
            "Safe Workbook.xlsx",
            filenames.CreateSafeWorkbookFileName("Safe Workbook"),
            "The .xlsx extension was not appended deterministically.");

        foreach (string invalid in new[]
                 {
                     "../escape.xlsx",
                     "C:\\escape.xlsx",
                     "CON.xlsx",
                     "bad?.xlsx",
                     "wrong.xls",
                     " trailing.xlsx"
                 })
        {
            _ = DocumentationLogTestAssert.Throws<ArgumentException>(
                () => filenames.CreateSafeWorkbookFileName(invalid),
                $"Unsafe filename '{invalid}' was accepted.");
        }

        string created = environment.CreateRunning(
            LogType.ScriptEditingLog,
            "Discover Me");
        DocumentationLogWorkbookException duplicate =
            DocumentationLogTestAssert.Throws<DocumentationLogWorkbookException>(
                () => environment.CreateRunning(
                    LogType.ScriptEditingLog,
                    "Discover Me.xlsx"),
                "Create New silently overwrote an existing workbook.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogWorkbookErrorCategory.AlreadyExists,
            duplicate.ErrorCategory,
            "Duplicate create used the wrong category.");
        DocumentationLogTestAssert.True(
            environment.WorkbookService
                .GetCompatibleRunningWorkbookFileNames(LogType.ScriptEditingLog)
                .Contains(created, StringComparer.OrdinalIgnoreCase),
            "A compatible direct-child workbook was not discovered.");

        string runningDirectory = environment.Paths.GetRunningDirectory(
            LogType.ScriptEditingLog);
        File.WriteAllBytes(
            Path.Combine(runningDirectory, "Corrupt.xlsx"),
            [1, 2, 3, 4]);
        string wrongTypeSource = environment.Paths.ResolveRunningWorkbookPath(
            LogType.DebuggingLog,
            environment.CreateRunning(LogType.DebuggingLog, "Wrong Type Source"));
        File.Copy(
            wrongTypeSource,
            Path.Combine(runningDirectory, "Wrong Type.xlsx"));
        string lockedPath = environment.Paths.ResolveRunningWorkbookPath(
            LogType.ScriptEditingLog,
            created);
        using (FileStream locked = new(
                   lockedPath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.None))
        {
            DocumentationLogTestAssert.Equal(
                0,
                environment.WorkbookService
                    .GetCompatibleRunningWorkbookFileNames(
                        LogType.ScriptEditingLog)
                    .Count,
                "Discovery exposed a locked, corrupt, or wrong-type Running workbook.");
        }
    }

    private static void TestPreferences()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string debugging = environment.CreateRunning(LogType.DebuggingLog, "Remember Debug");
        string editing = environment.CreateRunning(LogType.ScriptEditingLog, "Remember Edit");
        string creation = environment.CreateRunning(LogType.ScriptCreationLog, "Remember Create");
        environment.PreferencesService.SaveLastUsedWorkbookFileName(
            LogType.DebuggingLog,
            debugging);
        environment.PreferencesService.SaveLastUsedWorkbookFileName(
            LogType.ScriptEditingLog,
            editing);
        environment.PreferencesService.SaveLastUsedWorkbookFileName(
            LogType.ScriptCreationLog,
            creation);

        DocumentationLogWorkbookPreferencesService restarted = new(environment.Paths);
        DocumentationLogTestAssert.Equal(
            debugging,
            restarted.LoadLastUsedWorkbookFileName(LogType.DebuggingLog),
            "Debugging preference was not retained independently.");
        DocumentationLogTestAssert.Equal(
            editing,
            restarted.LoadLastUsedWorkbookFileName(LogType.ScriptEditingLog),
            "Editing preference was overwritten by another log type.");
        DocumentationLogTestAssert.Equal(
            creation,
            restarted.LoadLastUsedWorkbookFileName(LogType.ScriptCreationLog),
            "Creation preference was overwritten by another log type.");

        using (JsonDocument settings = JsonDocument.Parse(
                   File.ReadAllBytes(environment.Paths.SettingsFilePath)))
        {
            DocumentationLogTestAssert.Equal(
                DocumentationLogWorkbookPreferencesService.CurrentSchemaVersion,
                settings.RootElement.GetProperty("schemaVersion").GetInt32(),
                "Documentation preferences were not stored with the versioned schema.");
            DocumentationLogTestAssert.Equal(
                debugging,
                settings.RootElement
                    .GetProperty("debuggingLogWorkbookFileName")
                    .GetString(),
                "The Debugging preference was not persisted as a leaf filename.");
        }
        DocumentationLogTestAssert.True(
            !Directory.EnumerateFiles(
                    environment.Paths.IndexRootPath,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .Any(path => Path.GetFileName(path).Contains(
                    ".tmp",
                    StringComparison.OrdinalIgnoreCase)),
            "An atomic preference write left a temporary file behind.");

        File.Delete(environment.Paths.ResolveRunningWorkbookPath(
            LogType.ScriptEditingLog,
            editing));
        DocumentationLogTestAssert.Equal(
            editing,
            restarted.LoadLastUsedWorkbookFileName(LogType.ScriptEditingLog),
            "A missing remembered workbook crashed or silently cleared its filename-only preference.");
    }

    private static void AssertWorkbookContract(
        string path,
        DocumentationLogWorkbookContract contract,
        int expectedRows)
    {
        using XLWorkbook workbook = new(path);
        DocumentationLogTestAssert.Equal(
            2,
            workbook.Worksheets.Count,
            "The workbook contains an unexpected sheet structure.");
        IXLWorksheet worksheet = workbook.Worksheet(
            DocumentationLogWorkbookSchema.DataWorksheetName);
        DocumentationLogTestAssert.Equal(
            contract.Title,
            worksheet.Cell(1, 1).GetString(),
            "The workbook title changed.");
        DocumentationLogTestAssert.True(
            worksheet.Row(2).CellsUsed(XLCellsUsedOptions.Contents).Any() == false,
            "Row 2 is not blank.");
        IReadOnlyList<string> headers = DocumentationLogWorkbookSchema.GetHeaders(
            contract.LogType);
        DocumentationLogTestAssert.True(
            headers.SequenceEqual(
                Enumerable.Range(1, headers.Count)
                    .Select(column => worksheet.Cell(3, column).GetString()),
                StringComparer.Ordinal),
            "The exact ordered workbook headers changed.");
        DocumentationLogTestAssert.True(
            !headers.Contains("Log Type", StringComparer.Ordinal),
            "Log Type leaked into the Excel table columns.");
        IXLTable table = worksheet.Tables.Single();
        DocumentationLogTestAssert.True(
            table.RangeAddress.FirstAddress.RowNumber == 3
            && table.ShowHeaderRow
            && table.ShowAutoFilter
            && !table.ShowTotalsRow
            && worksheet.SheetView.SplitRow == 3,
            "The Table/filter/frozen-row presentation contract changed.");
        IXLWorksheet metadata = workbook.Worksheet(
            DocumentationLogWorkbookSchema.MetadataWorksheetName);
        DocumentationLogTestAssert.True(
            metadata.Visibility != XLWorksheetVisibility.Visible,
            "The internal metadata sheet is visible.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogWorkbookSchema.SchemaName,
            metadata.Cell(1, 2).GetString(),
            "The schema marker changed.");
        DocumentationLogTestAssert.Equal(
            contract.LogType.ToString(),
            metadata.Cell(3, 2).GetString(),
            "The metadata log type changed.");
        DocumentationLogTestAssert.Equal(
            contract.ScopeType.ToString(),
            metadata.Cell(4, 2).GetString(),
            "The metadata scope type changed.");
        DocumentationLogTestAssert.Equal(
            contract.ScopeId,
            metadata.Cell(5, 2).GetString(),
            "The metadata scope ID changed.");
        DocumentationLogTestAssert.Equal(
            expectedRows,
            DocumentationLogSyntheticEnvironment.CountWorkbookRows(path),
            "The workbook row count changed.");
    }

    private static void AssertRejected(
        DocumentationLogSyntheticEnvironment environment,
        byte[] content,
        DocumentationLogWorkbookContract contract,
        DocumentationLogEvent logEvent,
        DocumentationLogWorkbookErrorCategory expectedCategory)
    {
        DocumentationLogWorkbookException exception =
            DocumentationLogTestAssert.Throws<DocumentationLogWorkbookException>(
                () => environment.WorkbookService.BuildAppend(
                    content,
                    contract,
                    logEvent),
                $"Malformed workbook category {expectedCategory} was accepted.");
        DocumentationLogTestAssert.Equal(
            expectedCategory,
            exception.ErrorCategory,
            "Malformed workbook used the wrong failure category.");
    }

    private static byte[] Mutate(
        byte[] source,
        Action<XLWorkbook> mutation)
    {
        using MemoryStream input = new(source, writable: false);
        using XLWorkbook workbook = new(input);
        mutation(workbook);
        using MemoryStream output = new();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static void AssertTextCell(IXLCell cell, string expected, string label)
    {
        DocumentationLogTestAssert.True(
            cell.DataType == XLDataType.Text
            && cell.Style.NumberFormat.Format == "@"
            && cell.GetString() == expected,
            $"{label} was not stored explicitly as exact text.");
    }

    private static int Run(
        IEnumerable<(string Name, Action Body)> tests,
        TextWriter output)
    {
        (string Name, Action Body)[] cases = tests.ToArray();
        int failed = 0;
        output.WriteLine($"Documentation-log workbook harness: {cases.Length} tests");
        foreach ((string name, Action body) in cases)
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
                output.WriteLine(exception);
            }
        }

        output.WriteLine(failed == 0
            ? $"[PASS] All {cases.Length} documentation-log workbook tests passed."
            : $"[FAIL] {failed} of {cases.Length} documentation-log workbook tests failed.");
        return failed == 0 ? 0 : 1;
    }
}
