using System.Text;
using DocumentationLoggingDashboard.DocumentationLogs;
using DocumentationLoggingDashboard.Models;

namespace DocumentationLoggingDashboard.GeometryTests;

internal static class DocumentationLogSequenceIndexRegressionTests
{
    private static readonly DateTimeOffset TestTimestamp =
        new(2026, 8, 26, 14, 30, 0, TimeSpan.FromHours(-4));

    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        (string Name, Action Body)[] tests =
        [
            ("legacy TXT, index, state, type, and next-day maximum reconciliation", TestSequenceReconciliation),
            ("ambiguous or unsupported sequence JSON is rejected without fallback", TestStrictSequenceStateParsing),
            ("Running workbook switch and service restart never reset IDs", TestWorkbookSwitchAndRestart),
            ("overlapping service submissions serialize to distinct durable IDs", TestOverlappingSubmissions),
            ("preview and failed save do not reserve or consume a sequence", TestPreviewAndFailureDoNotConsume),
            ("mixed legacy history stays byte-exact and receives one Excel index line", TestMixedHistoryCompatibility)
        ];
        return Run(tests, output);
    }

    private static void TestStrictSequenceStateParsing()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        DocumentationLogSequenceService service = new(environment.Paths);
        string marker = DocumentationLogSequenceService.SchemaName;
        string[] invalidDocuments =
        [
            $"{{\"schemaName\":\"{marker}\",\"schemaVersion\":1,\"sequences\":{{\"DebuggingLog|20260826\":9,\"DebuggingLog|20260826\":1}}}}",
            $"{{\"schemaName\":\"{marker}\",\"schemaVersion\":1,\"sequences\":{{}},\"unknown\":true}}",
            $"{{\"schemaName\":\"{marker}\",\"schemaName\":\"{marker}\",\"schemaVersion\":1,\"sequences\":{{}}}}",
            $"{{\"schemaName\":\"{marker}\",\"schemaVersion\":1,\"sequences\":{{\"UnknownLog|20260826\":4}}}}"
        ];

        foreach (string json in invalidDocuments)
        {
            DocumentationLogSaveException exception =
                DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                    () => service.PrepareNext(
                        LogType.DebuggingLog,
                        TestTimestamp,
                        stateExists: true,
                        Encoding.UTF8.GetBytes(json),
                        ReadOnlyMemory<byte>.Empty,
                        ReadOnlyMemory<byte>.Empty),
                    "Ambiguous or unsupported sequence JSON was accepted.");
            DocumentationLogTestAssert.Equal(
                DocumentationLogSaveErrorCategory.SequenceStateInvalid,
                exception.ErrorCategory,
                "Strict sequence JSON rejection used the wrong category.");
        }
    }

    private static void TestSequenceReconciliation()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        DocumentationLogSequenceService service = new(environment.Paths);
        byte[] legacy = Encoding.UTF8.GetBytes(
            "Log ID: DEBUG-20260826-005\r\n"
            + "Log ID: DEBUG-20260826-002\r\n"
            + "Log ID: DEBUG-20260825-999\r\n"
            + "Log ID: EDIT-20260826-999");
        byte[] index = Encoding.UTF8.GetBytes(
            "2026-08-26 | 1:00 PM | DEBUG-20260826-007 | Debugging Log | x | y | old.txt\r\n"
            + "2026-08-27 | 1:00 PM | DEBUG-20260827-999 | Debugging Log | x | y | future.txt\r\n"
            + "2026-08-26 | 1:00 PM | CREATE-20260826-999 | Script Creation Log | x | y | other.txt\r\n");
        DocumentationLogSequencePlan debug = service.PrepareNext(
            LogType.DebuggingLog,
            TestTimestamp,
            stateExists: false,
            stateContent: ReadOnlyMemory<byte>.Empty,
            index,
            legacy);
        DocumentationLogTestAssert.Equal(
            "DEBUG-20260826-008",
            debug.LogId,
            "The highest legacy/index sequence was not reconciled.");

        DocumentationLogSequencePlan debugSecond = service.PrepareNext(
            LogType.DebuggingLog,
            TestTimestamp,
            stateExists: true,
            debug.UpdatedStateContent,
            index,
            legacy);
        DocumentationLogTestAssert.Equal(
            "DEBUG-20260826-009",
            debugSecond.LogId,
            "Durable state did not advance independently of workbooks.");

        DocumentationLogSequencePlan editFirst = service.PrepareNext(
            LogType.ScriptEditingLog,
            TestTimestamp,
            stateExists: true,
            debugSecond.UpdatedStateContent,
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty);
        DocumentationLogTestAssert.Equal(
            "EDIT-20260826-001",
            editFirst.LogId,
            "The Edit counter was not independent from Debugging.");

        DocumentationLogSequencePlan createFirst = service.PrepareNext(
            LogType.ScriptCreationLog,
            TestTimestamp,
            stateExists: true,
            editFirst.UpdatedStateContent,
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty);
        DocumentationLogTestAssert.Equal(
            "CREATE-20260826-001",
            createFirst.LogId,
            "The Create counter was not independent from Debugging and Edit.");

        DocumentationLogSequencePlan editSecond = service.PrepareNext(
            LogType.ScriptEditingLog,
            TestTimestamp,
            stateExists: true,
            createFirst.UpdatedStateContent,
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty);
        DocumentationLogTestAssert.Equal(
            "EDIT-20260826-002",
            editSecond.LogId,
            "The durable Edit counter was lost after a Create sequence update.");

        DocumentationLogSequencePlan nextDay = service.PrepareNext(
            LogType.DebuggingLog,
            TestTimestamp.AddDays(1),
            stateExists: true,
            editSecond.UpdatedStateContent,
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty);
        DocumentationLogTestAssert.Equal(
            "DEBUG-20260827-001",
            nextDay.LogId,
            "The next calendar day did not start a date-scoped sequence.");
    }

    private static void TestWorkbookSwitchAndRestart()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string firstWorkbook = environment.CreateRunning(
            LogType.DebuggingLog,
            "Switch First");
        string secondWorkbook = environment.CreateRunning(
            LogType.DebuggingLog,
            "Switch Second");
        DocumentationLogSaveResult first = environment.CreateSaveService(TestTimestamp)
            .Save(environment.CreateDebuggingRequest(firstWorkbook));
        DocumentationLogSaveResult second = environment.CreateSaveService(TestTimestamp)
            .Save(environment.CreateDebuggingRequest(secondWorkbook));

        DocumentationLogTestAssert.Equal(
            "DEBUG-20260826-001",
            first.Event.LogId,
            "The first event did not receive sequence 001.");
        DocumentationLogTestAssert.Equal(
            "DEBUG-20260826-002",
            second.Event.LogId,
            "Switching Running workbook or restarting the service reset the ID.");
        DocumentationLogTestAssert.Equal(
            1,
            DocumentationLogSyntheticEnvironment.CountWorkbookRows(
                first.RunningWorkbookPath),
            "The first Running workbook received the wrong row count.");
        DocumentationLogTestAssert.Equal(
            1,
            DocumentationLogSyntheticEnvironment.CountWorkbookRows(
                second.RunningWorkbookPath),
            "The second Running workbook received the wrong row count.");
        DocumentationLogTestAssert.Equal(
            2,
            DocumentationLogSyntheticEnvironment.CountWorkbookRows(
                second.HotelHistoryPaths.Single()),
            "History copies consumed IDs or failed to append both logical events.");
        DocumentationLogTestAssert.Equal(
            2,
            File.ReadAllLines(environment.Paths.LogIndexFilePath).Length,
            "Two logical events did not create exactly two index entries.");
    }

    private static void TestPreviewAndFailureDoNotConsume()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.ScriptEditingLog,
            "Preview Sequence");
        DocumentationLogSaveService service = environment.CreateSaveService(TestTimestamp);
        DocumentationLogSaveRequest valid = environment.CreateScriptRequest(
            LogType.ScriptEditingLog,
            running,
            "1953;2093");
        DocumentationLogEvent previewOne = service.CreatePreview(valid);
        DocumentationLogEvent previewTwo = service.CreatePreview(valid);
        DocumentationLogTestAssert.Equal(
            previewOne.LogId,
            previewTwo.LogId,
            "Preview permanently reserved a sequence.");

        _ = DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
            () => service.Save(environment.CreateScriptRequest(
                LogType.ScriptEditingLog,
                running,
                "1953;UNKNOWN")),
            "An unresolved Hotel did not block the complete save.");
        DocumentationLogTestAssert.True(
            !File.Exists(environment.Paths.SequenceStateFilePath)
            && !File.Exists(environment.Paths.LogIndexFilePath),
            "A failed preflight consumed sequence or index state.");

        DocumentationLogSaveService stagedFailureService =
            environment.CreateSaveService(
                TestTimestamp,
                new FailFirstStageWriteOperations());
        _ = DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
            () => stagedFailureService.Save(valid),
            "An injected staging failure did not abort the complete save.");
        DocumentationLogTestAssert.True(
            !File.Exists(environment.Paths.SequenceStateFilePath)
            && !File.Exists(environment.Paths.LogIndexFilePath),
            "A failed staged transaction consumed sequence or index state.");

        DocumentationLogSaveResult saved = service.Save(valid);
        DocumentationLogTestAssert.Equal(
            "EDIT-20260826-001",
            saved.Event.LogId,
            "Preview or failed save consumed the first real sequence.");
    }

    private static void TestOverlappingSubmissions()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.ScriptCreationLog,
            "Overlapping Saves");
        DocumentationLogSaveRequest request = environment.CreateScriptRequest(
            LogType.ScriptCreationLog,
            running,
            "1953; 2093; 3001");
        DocumentationLogSaveService firstService =
            environment.CreateSaveService(TestTimestamp);
        DocumentationLogSaveService secondService =
            environment.CreateSaveService(TestTimestamp);
        using ManualResetEventSlim start = new(initialState: false);
        Task<DocumentationLogSaveResult> first = Task.Run(() =>
        {
            start.Wait();
            return firstService.Save(request);
        });
        Task<DocumentationLogSaveResult> second = Task.Run(() =>
        {
            start.Wait();
            return secondService.Save(request);
        });
        start.Set();
        Task.WaitAll(first, second);

        string[] ids = [first.Result.Event.LogId, second.Result.Event.LogId];
        Array.Sort(ids, StringComparer.Ordinal);
        DocumentationLogTestAssert.True(
            ids.SequenceEqual(
                ["CREATE-20260826-001", "CREATE-20260826-002"],
                StringComparer.Ordinal),
            "Overlapping submissions committed a duplicate or skipped ID.");
        DocumentationLogTestAssert.Equal(
            2,
            DocumentationLogSyntheticEnvironment.CountWorkbookRows(
                first.Result.RunningWorkbookPath),
            "Overlapping submissions did not append exactly two Running rows.");
        DocumentationLogTestAssert.True(
            first.Result.HotelHistoryPaths.All(path =>
                DocumentationLogSyntheticEnvironment.CountWorkbookRows(path) == 2)
            && first.Result.PmsHistoryPaths.All(path =>
                DocumentationLogSyntheticEnvironment.CountWorkbookRows(path) == 2),
            "Overlapping submissions duplicated or lost a Hotel/PMS history copy.");
        DocumentationLogTestAssert.Equal(
            2,
            File.ReadAllLines(environment.Paths.LogIndexFilePath).Length,
            "Overlapping submissions created the wrong number of index entries.");
    }

    private static void TestMixedHistoryCompatibility()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.ScriptCreationLog,
            "Mixed History");
        string legacyPath = environment.Paths.GetLegacyDailyLogFilePath(
            LogType.ScriptCreationLog,
            TestTimestamp.DateTime);
        byte[] legacyBytes = Encoding.UTF8.GetBytes(
            "Legacy TXT bytes\r\nLog ID: CREATE-20260826-004\r\n");
        File.WriteAllBytes(legacyPath, legacyBytes);
        byte[] oldIndexBytes = Encoding.UTF8.GetBytes(
            "2026-08-26 | 9:00 AM | CREATE-20260826-006 | Script Creation Log | Script: Old | Hotels That Use This Script: 1953 | C:\\legacy.txt");
        File.WriteAllBytes(environment.Paths.LogIndexFilePath, oldIndexBytes);

        DocumentationLogSaveResult result = environment.CreateSaveService(TestTimestamp)
            .Save(environment.CreateScriptRequest(
                LogType.ScriptCreationLog,
                running,
                "1953; 2093; 3001"));
        byte[] finalIndex = File.ReadAllBytes(environment.Paths.LogIndexFilePath);
        string[] indexLines = File.ReadAllLines(environment.Paths.LogIndexFilePath);
        string[] appendedFields = indexLines[1].Split(
            " | ",
            StringSplitOptions.None);
        DocumentationLogTestAssert.Equal(
            "CREATE-20260826-007",
            result.Event.LogId,
            "The new Excel event did not continue above legacy TXT/index IDs.");
        DocumentationLogTestAssert.True(
            File.ReadAllBytes(legacyPath).SequenceEqual(legacyBytes),
            "The legacy TXT file was changed or migrated.");
        DocumentationLogTestAssert.True(
            finalIndex.AsSpan(0, oldIndexBytes.Length).SequenceEqual(oldIndexBytes)
            && Encoding.UTF8.GetString(finalIndex).Contains(
                "Hotel IDs: 1953; 2093; 3001",
                StringComparison.Ordinal)
            && Encoding.UTF8.GetString(finalIndex).Contains(
                result.RunningWorkbookPath,
                StringComparison.Ordinal)
            && Encoding.UTF8.GetString(finalIndex).Contains(".xlsx", StringComparison.OrdinalIgnoreCase),
            "The historical index prefix was rewritten or the new Excel summary/path is wrong.");
        DocumentationLogTestAssert.Equal(
            2,
            indexLines.Length,
            "One logical multi-destination event created more than one index line.");
        DocumentationLogTestAssert.Equal(
            7,
            appendedFields.Length,
            "The appended Excel index record did not retain the seven-field legacy shape.");
        DocumentationLogTestAssert.Equal(
            "2026-08-26",
            appendedFields[0],
            "The appended index date was wrong.");
        DocumentationLogTestAssert.Equal(
            "CREATE-20260826-007",
            appendedFields[2],
            "The appended index Log ID was wrong.");
        DocumentationLogTestAssert.Equal(
            "Script Creation Log",
            appendedFields[3],
            "The appended index type was wrong.");
        DocumentationLogTestAssert.Equal(
            "Script: SyntheticScript.csx",
            appendedFields[4],
            "The appended index script summary was wrong.");
        DocumentationLogTestAssert.Equal(
            "Hotel IDs: 1953; 2093; 3001",
            appendedFields[5],
            "The appended index Hotel summary was wrong.");
        DocumentationLogTestAssert.Equal(
            Path.GetFullPath(result.RunningWorkbookPath),
            appendedFields[6],
            "The appended index did not point at the selected Running workbook.");
    }

    private static int Run(
        IEnumerable<(string Name, Action Body)> tests,
        TextWriter output)
    {
        (string Name, Action Body)[] cases = tests.ToArray();
        int failed = 0;
        output.WriteLine($"Documentation-log sequence/index harness: {cases.Length} tests");
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
            ? $"[PASS] All {cases.Length} documentation-log sequence/index tests passed."
            : $"[FAIL] {failed} of {cases.Length} documentation-log sequence/index tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private sealed class FailFirstStageWriteOperations
        : IDocumentationLogTransactionFileOperations
    {
        private readonly DocumentationLogTransactionFileOperations inner = new();
        private bool hasFailed;

        public void CreateDirectory(string path)
        {
            inner.CreateDirectory(path);
        }

        public bool FileExists(string path)
        {
            return inner.FileExists(path);
        }

        public byte[] ReadAllBytes(string path)
        {
            return inner.ReadAllBytes(path);
        }

        public void WriteNewAndFlush(
            string path,
            ReadOnlyMemory<byte> content)
        {
            if (!hasFailed)
            {
                hasFailed = true;
                throw new IOException("Injected first-stage write failure.");
            }

            inner.WriteNewAndFlush(path, content);
        }

        public long GetFileLength(string path)
        {
            return inner.GetFileLength(path);
        }

        public byte[] ComputeSha256(string path)
        {
            return inner.ComputeSha256(path);
        }

        public void VerifyExclusiveAccess(string path)
        {
            inner.VerifyExclusiveAccess(path);
        }

        public IDocumentationLogExclusiveReadLease OpenExclusiveReadLease(
            string path)
        {
            return inner.OpenExclusiveReadLease(path);
        }

        public void Move(string sourcePath, string destinationPath)
        {
            inner.Move(sourcePath, destinationPath);
        }

        public void Delete(string path)
        {
            inner.Delete(path);
        }
    }
}
