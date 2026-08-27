using ClosedXML.Excel;
using DocumentationLoggingDashboard.DocumentationLogs;
using DocumentationLoggingDashboard.Models;

namespace DocumentationLoggingDashboard.GeometryTests;

internal static class DocumentationLogSaveTransactionRegressionTests
{
    private static readonly DateTimeOffset TestTimestamp =
        new(2026, 8, 26, 14, 30, 0, TimeSpan.FromHours(-4));

    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        (string Name, Action Body)[] tests =
        [
            ("successful fan-out writes one event to deduplicated Hotel/PMS destinations", TestSuccessfulFanOutAndDeduplication),
            ("locked existing Hotel history blocks the complete fan-out", TestExistingHotelHistoryLock),
            ("locked existing PMS history blocks the complete fan-out", TestExistingPmsHistoryLock),
            ("failure among multiple Hotel-history stages leaves no partial files", TestHotelHistoryStagingFailureLeavesNoPartialFiles),
            ("index staging failure leaves no partial files or transaction residue", TestIndexStagingFailureLeavesNoPartialFiles),
            ("sequence-state staging failure leaves no partial files or transaction residue", TestSequenceStagingFailureLeavesNoPartialFiles),
            ("post-backup failure restores every existing artifact byte-for-byte", TestBackupFailureRestoresEveryArtifact),
            ("post-commit failure restores workbooks, index, and sequence state byte-for-byte", TestCommitFailureRestoresEveryArtifact),
            ("final verification failure rolls back the fully committed replacement set", TestFinalVerificationFailureRestoresEveryArtifact),
            ("concurrent destination mutation is detected and never overwritten", TestConcurrentMutationIsPreserved),
            ("metadata mutation during staging blocks stale routing and is preserved", TestMetadataMutationBlocksStaleRouting),
            ("new-destination collision is detected and the external file is preserved", TestNewDestinationCollisionIsPreserved),
            ("an exclusively locked Running workbook blocks the save before fan-out", TestExclusiveRunningWorkbookLock),
            ("forced rollback failure reports manual-review paths", TestForcedRollbackFailureReportsManualReview)
        ];
        return Run(tests, output);
    }

    private static void TestSuccessfulFanOutAndDeduplication()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.ScriptEditingLog,
            "Fan Out");
        DocumentationLogSaveResult result = environment.CreateSaveService(TestTimestamp)
            .Save(environment.CreateScriptRequest(
                LogType.ScriptEditingLog,
                running,
                "1953, 2093; 1953\r\n3001; 3001"));

        DocumentationLogTestAssert.True(
            result.Event.Hotels.Select(hotel => hotel.HotelId).SequenceEqual(
                ["1953", "2093", "3001"],
                StringComparer.Ordinal),
            "The logical event did not retain one canonical Hotel destination in first-seen order.");
        DocumentationLogTestAssert.Equal(
            3,
            result.HotelHistoryPaths.Count,
            "Duplicate Hotel IDs created duplicate Hotel history destinations.");
        DocumentationLogTestAssert.Equal(
            2,
            result.PmsHistoryPaths.Count,
            "Hotels sharing a PMS created duplicate PMS history destinations.");
        DocumentationLogTestAssert.Equal(
            result.HotelHistoryPaths.Count,
            result.HotelHistoryPaths.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            "The save result contains duplicate Hotel history paths.");
        DocumentationLogTestAssert.Equal(
            result.PmsHistoryPaths.Count,
            result.PmsHistoryPaths.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            "The save result contains duplicate PMS history paths.");

        string[] workbooks =
        [
            result.RunningWorkbookPath,
            .. result.HotelHistoryPaths,
            .. result.PmsHistoryPaths
        ];
        DocumentationLogTestAssert.Equal(
            6,
            workbooks.Length,
            "The successful logical event used the wrong workbook fan-out count.");

        foreach (string workbookPath in workbooks)
        {
            DocumentationLogTestAssert.True(
                File.Exists(workbookPath),
                $"A required fan-out workbook was not created: {workbookPath}");
            DocumentationLogTestAssert.Equal(
                1,
                DocumentationLogSyntheticEnvironment.CountWorkbookRows(workbookPath),
                $"A fan-out workbook did not receive exactly one row: {workbookPath}");

            using XLWorkbook workbook = new(workbookPath);
            IXLWorksheet worksheet = workbook.Worksheet(
                DocumentationLogWorkbookSchema.DataWorksheetName);
            DocumentationLogTestAssert.Equal(
                result.Event.LogId,
                worksheet.Cell(4, 2).GetString(),
                $"A fan-out workbook received a different Log ID: {workbookPath}");
            DocumentationLogTestAssert.Equal(
                "1953; 2093; 3001",
                worksheet.Cell(4, 3).GetString(),
                $"A fan-out workbook received noncanonical Hotel IDs: {workbookPath}");
        }

        DocumentationLogTestAssert.Equal(
            1,
            File.ReadAllLines(result.IndexPath).Length,
            "One logical event wrote more than one index line.");
        DocumentationLogTestAssert.True(
            File.Exists(result.SequenceStatePath),
            "A successful logical event did not commit durable sequence state.");
        AssertNoTransactionResidue(environment.RootPath);
    }

    private static void TestExistingHotelHistoryLock()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        SeededTransaction seeded = CreateSeededTransaction(environment);
        IReadOnlyDictionary<string, byte[]> before = CaptureFiles(
            seeded.AllArtifactPaths);
        string lockedPath = seeded.SeedResult.HotelHistoryPaths[1];
        DocumentationLogSaveException exception;

        using (FileStream lockStream = new(
                   lockedPath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.None))
        {
            exception = DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => environment.CreateSaveService(TestTimestamp).Save(seeded.Request),
                "An exclusively locked Hotel-history workbook did not block the complete save.");
        }

        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveErrorCategory.HistoryWorkbookUnavailable,
            exception.ErrorCategory,
            "A locked Hotel-history workbook used the wrong failure category.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveStage.HistoryLoad,
            exception.Stage,
            "A locked Hotel-history workbook was reported at the wrong stage.");
        DocumentationLogTestAssert.Equal(
            Path.GetFullPath(lockedPath),
            exception.AffectedPath,
            "The locked Hotel-history failure identified the wrong path.");
        AssertFilesEqual(before);
        AssertNoTransactionResidue(environment.RootPath);
    }

    private static void TestExistingPmsHistoryLock()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        SeededTransaction seeded = CreateSeededTransaction(environment);
        IReadOnlyDictionary<string, byte[]> before = CaptureFiles(
            seeded.AllArtifactPaths);
        string lockedPath = seeded.SeedResult.PmsHistoryPaths[1];
        DocumentationLogSaveException exception;

        using (FileStream lockStream = new(
                   lockedPath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.None))
        {
            exception = DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => environment.CreateSaveService(TestTimestamp).Save(seeded.Request),
                "An exclusively locked PMS-history workbook did not block the complete save.");
        }

        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveErrorCategory.HistoryWorkbookUnavailable,
            exception.ErrorCategory,
            "A locked PMS-history workbook used the wrong failure category.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveStage.HistoryLoad,
            exception.Stage,
            "A locked PMS-history workbook was reported at the wrong stage.");
        DocumentationLogTestAssert.Equal(
            Path.GetFullPath(lockedPath),
            exception.AffectedPath,
            "The locked PMS-history failure identified the wrong path.");
        AssertFilesEqual(before);
        AssertNoTransactionResidue(environment.RootPath);
    }

    private static void TestHotelHistoryStagingFailureLeavesNoPartialFiles()
    {
        AssertFreshStagingFailure(
            failAfterWriteNumber: 3,
            DocumentationLogSaveStage.WorkbookStaging,
            "Hotel-history staging");
    }

    private static void TestIndexStagingFailureLeavesNoPartialFiles()
    {
        AssertFreshStagingFailure(
            failAfterWriteNumber: 7,
            DocumentationLogSaveStage.IndexStaging,
            "index staging");
    }

    private static void TestSequenceStagingFailureLeavesNoPartialFiles()
    {
        AssertFreshStagingFailure(
            failAfterWriteNumber: 8,
            DocumentationLogSaveStage.SequenceStaging,
            "sequence-state staging");
    }

    private static void AssertFreshStagingFailure(
        int failAfterWriteNumber,
        DocumentationLogSaveStage expectedStage,
        string label)
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.ScriptEditingLog,
            $"{label} Failure");
        string runningPath = environment.Paths.ResolveRunningWorkbookPath(
            LogType.ScriptEditingLog,
            running);
        byte[] runningBefore = File.ReadAllBytes(runningPath);
        string[] newDestinations = GetNewFanOutDestinations(
            environment,
            LogType.ScriptEditingLog);
        FaultInjectingFileOperations operations = new()
        {
            FailAfterWriteNumber = failAfterWriteNumber
        };
        DocumentationLogSaveService service = environment.CreateSaveService(
            TestTimestamp,
            operations);

        DocumentationLogSaveException exception =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => service.Save(environment.CreateScriptRequest(
                    LogType.ScriptEditingLog,
                    running,
                    "1953; 2093; 3001")),
                $"An injected {label} fault did not fail the transaction.");

        DocumentationLogTestAssert.Equal(
            expectedStage,
            exception.Stage,
            $"The {label} fault was reported at the wrong transaction stage.");
        DocumentationLogTestAssert.True(
            !exception.ManualReviewRequired,
            $"A fully cleaned {label} fault incorrectly requested manual review.");
        DocumentationLogTestAssert.True(
            File.ReadAllBytes(runningPath).SequenceEqual(runningBefore),
            $"The {label} fault changed the selected Running workbook.");
        AssertFilesAbsent(newDestinations);
        AssertNoTransactionResidue(environment.RootPath);
    }

    private static void TestBackupFailureRestoresEveryArtifact()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        SeededTransaction seeded = CreateSeededTransaction(environment);
        IReadOnlyDictionary<string, byte[]> before = CaptureFiles(
            seeded.AllArtifactPaths);
        FaultInjectingFileOperations operations = new()
        {
            FailAfterBackupMoveNumber = 7
        };
        DocumentationLogSaveService service = environment.CreateSaveService(
            TestTimestamp,
            operations);

        DocumentationLogSaveException exception =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => service.Save(seeded.Request),
                "A fault after an index backup move did not fail the transaction.");

        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveStage.Backup,
            exception.Stage,
            "A post-backup fault was reported at the wrong transaction stage.");
        DocumentationLogTestAssert.True(
            exception.PreviousStateRestored && !exception.ManualReviewRequired,
            "A recoverable backup fault did not report a complete automatic restore.");
        AssertFilesEqual(before);
        AssertNoTransactionResidue(environment.RootPath);

        DocumentationLogSaveResult retry = environment.CreateSaveService(TestTimestamp)
            .Save(seeded.Request);
        DocumentationLogTestAssert.Equal(
            "EDIT-20260826-002",
            retry.Event.LogId,
            "A rolled-back backup fault consumed durable sequence state.");
    }

    private static void TestCommitFailureRestoresEveryArtifact()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        SeededTransaction seeded = CreateSeededTransaction(environment);
        IReadOnlyDictionary<string, byte[]> before = CaptureFiles(
            seeded.AllArtifactPaths);
        FaultInjectingFileOperations operations = new()
        {
            FailAfterCommitMoveNumber = 8
        };
        DocumentationLogSaveService service = environment.CreateSaveService(
            TestTimestamp,
            operations);

        DocumentationLogSaveException exception =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => service.Save(seeded.Request),
                "A fault after the sequence-state commit move did not fail the transaction.");

        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveStage.Commit,
            exception.Stage,
            "A post-commit fault was reported at the wrong transaction stage.");
        DocumentationLogTestAssert.True(
            exception.PreviousStateRestored && !exception.ManualReviewRequired,
            "A recoverable post-commit fault did not report a complete automatic restore.");
        AssertFilesEqual(before);
        AssertNoTransactionResidue(environment.RootPath);

        DocumentationLogSaveResult retry = environment.CreateSaveService(TestTimestamp)
            .Save(seeded.Request);
        DocumentationLogTestAssert.Equal(
            "EDIT-20260826-002",
            retry.Event.LogId,
            "A rolled-back commit fault consumed durable sequence state.");
        DocumentationLogTestAssert.Equal(
            2,
            File.ReadAllLines(environment.Paths.LogIndexFilePath).Length,
            "A rolled-back commit fault left a duplicate index line behind.");
    }

    private static void TestFinalVerificationFailureRestoresEveryArtifact()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        SeededTransaction seeded = CreateSeededTransaction(environment);
        IReadOnlyDictionary<string, byte[]> before = CaptureFiles(
            seeded.AllArtifactPaths);
        FaultInjectingFileOperations operations = new()
        {
            FailHashAfterCommitPath = seeded.SeedResult.RunningWorkbookPath
        };
        DocumentationLogSaveService service = environment.CreateSaveService(
            TestTimestamp,
            operations);

        DocumentationLogSaveException exception =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => service.Save(seeded.Request),
                "A final-verification hash fault did not fail the transaction.");

        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveStage.FinalVerification,
            exception.Stage,
            "A final-verification fault was reported at the wrong transaction stage.");
        DocumentationLogTestAssert.True(
            exception.PreviousStateRestored && !exception.ManualReviewRequired,
            "A recoverable final-verification fault did not report a complete automatic restore.");
        AssertFilesEqual(before);
        AssertNoTransactionResidue(environment.RootPath);
    }

    private static void TestConcurrentMutationIsPreserved()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.ScriptEditingLog,
            "Concurrent Mutation");
        string runningPath = environment.Paths.ResolveRunningWorkbookPath(
            LogType.ScriptEditingLog,
            running);
        byte[] original = File.ReadAllBytes(runningPath);
        byte[] externallyChanged = [.. original, 0x20];
        FaultInjectingFileOperations operations = new()
        {
            InvokeAfterWriteNumber = 8,
            AfterWriteAction = () => File.WriteAllBytes(
                runningPath,
                externallyChanged)
        };
        DocumentationLogSaveService service = environment.CreateSaveService(
            TestTimestamp,
            operations);

        DocumentationLogSaveException exception =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => service.Save(environment.CreateScriptRequest(
                    LogType.ScriptEditingLog,
                    running,
                    "1953; 2093; 3001")),
                "A destination mutation during staging was not detected.");

        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveErrorCategory.ConcurrentChange,
            exception.ErrorCategory,
            "A staged destination mutation used the wrong error category.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveStage.ConcurrencyCheck,
            exception.Stage,
            "A staged destination mutation was detected at the wrong stage.");
        DocumentationLogTestAssert.True(
            File.ReadAllBytes(runningPath).SequenceEqual(externallyChanged),
            "Rollback overwrote a concurrent external destination change.");
        AssertFilesAbsent(GetNewFanOutDestinations(
            environment,
            LogType.ScriptEditingLog));
        AssertNoTransactionResidue(environment.RootPath);
    }

    private static void TestNewDestinationCollisionIsPreserved()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.ScriptEditingLog,
            "New Destination Collision");
        string runningPath = environment.Paths.ResolveRunningWorkbookPath(
            LogType.ScriptEditingLog,
            running);
        byte[] runningBefore = File.ReadAllBytes(runningPath);
        string[] newDestinations = GetNewFanOutDestinations(
            environment,
            LogType.ScriptEditingLog);
        string collisionPath = newDestinations[0];
        byte[] collisionBytes = "external destination bytes"u8.ToArray();
        FaultInjectingFileOperations operations = new()
        {
            InvokeAfterWriteNumber = 8,
            AfterWriteAction = () => File.WriteAllBytes(
                collisionPath,
                collisionBytes)
        };
        DocumentationLogSaveService service = environment.CreateSaveService(
            TestTimestamp,
            operations);

        DocumentationLogSaveException exception =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => service.Save(environment.CreateScriptRequest(
                    LogType.ScriptEditingLog,
                    running,
                    "1953; 2093; 3001")),
                "An external file created at a new destination was not detected.");

        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveErrorCategory.ConcurrentChange,
            exception.ErrorCategory,
            "A new-destination collision used the wrong error category.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveStage.ConcurrencyCheck,
            exception.Stage,
            "A new-destination collision was reported at the wrong stage.");
        DocumentationLogTestAssert.Equal(
            Path.GetFullPath(collisionPath),
            exception.AffectedPath,
            "A new-destination collision identified the wrong path.");
        DocumentationLogTestAssert.True(
            File.ReadAllBytes(collisionPath).SequenceEqual(collisionBytes),
            "Rollback removed or replaced the externally created collision file.");
        DocumentationLogTestAssert.True(
            File.ReadAllBytes(runningPath).SequenceEqual(runningBefore),
            "A new-destination collision changed the Running workbook.");
        AssertFilesAbsent(newDestinations.Skip(1));
        AssertNoTransactionResidue(environment.RootPath);
    }

    private static void TestMetadataMutationBlocksStaleRouting()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.ScriptEditingLog,
            "Metadata Mutation");
        string runningPath = environment.Paths.ResolveRunningWorkbookPath(
            LogType.ScriptEditingLog,
            running);
        byte[] runningBefore = File.ReadAllBytes(runningPath);
        string metadataPath = environment.QaPaths.HotelsMetadataFilePath;
        byte[] metadataBefore = File.ReadAllBytes(metadataPath);
        byte[] externallyChangedMetadata = [.. metadataBefore, (byte)' ', (byte)'\r', (byte)'\n'];
        FaultInjectingFileOperations operations = new()
        {
            InvokeAfterWriteNumber = 8,
            AfterWriteAction = () => File.WriteAllBytes(
                metadataPath,
                externallyChangedMetadata)
        };

        DocumentationLogSaveException exception =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => environment.CreateSaveService(TestTimestamp, operations)
                    .Save(environment.CreateScriptRequest(
                        LogType.ScriptEditingLog,
                        running,
                        "1953; 2093; 3001")),
                "A metadata mutation during staging did not block stale routing.");

        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveErrorCategory.MetadataMismatch,
            exception.ErrorCategory,
            "A concurrent metadata edit used the wrong failure category.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveStage.ConcurrencyCheck,
            exception.Stage,
            "A concurrent metadata edit was detected at the wrong stage.");
        DocumentationLogTestAssert.True(
            File.ReadAllBytes(metadataPath).SequenceEqual(
                externallyChangedMetadata),
            "Rollback overwrote the concurrent metadata edit.");
        DocumentationLogTestAssert.True(
            File.ReadAllBytes(runningPath).SequenceEqual(runningBefore),
            "A metadata conflict changed the selected Running workbook.");
        AssertFilesAbsent(GetNewFanOutDestinations(
            environment,
            LogType.ScriptEditingLog));
        AssertNoTransactionResidue(environment.RootPath);
    }

    private static void TestExclusiveRunningWorkbookLock()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.DebuggingLog,
            "Locked Running");
        string runningPath = environment.Paths.ResolveRunningWorkbookPath(
            LogType.DebuggingLog,
            running);
        byte[] before = File.ReadAllBytes(runningPath);
        DocumentationLogSaveService service = environment.CreateSaveService(
            TestTimestamp);

        DocumentationLogSaveException exception;
        using (FileStream lockStream = new(
                   runningPath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.None))
        {
            exception = DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => service.Save(environment.CreateDebuggingRequest(running)),
                "An exclusively locked Running workbook did not block the save.");
        }

        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveErrorCategory.RunningWorkbookUnavailable,
            exception.ErrorCategory,
            "A locked Running workbook used the wrong failure category.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveStage.RunningLoad,
            exception.Stage,
            "A locked Running workbook was reported at the wrong stage.");
        DocumentationLogTestAssert.True(
            File.ReadAllBytes(runningPath).SequenceEqual(before),
            "A failed locked-workbook save changed the Running workbook.");
        AssertFilesAbsent(
        [
            environment.Paths.ResolveHotelHistoryPath(
                LogType.DebuggingLog,
                environment.Hotel1953.FolderName),
            environment.Paths.ResolvePmsHistoryPath(
                LogType.DebuggingLog,
                environment.Mews.FolderName),
            environment.Paths.LogIndexFilePath,
            environment.Paths.SequenceStateFilePath
        ]);
        AssertNoTransactionResidue(environment.RootPath);
    }

    private static void TestForcedRollbackFailureReportsManualReview()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        SeededTransaction seeded = CreateSeededTransaction(environment);
        IReadOnlyDictionary<string, byte[]> before = CaptureFiles(
            seeded.AllArtifactPaths);
        string runningPath = seeded.SeedResult.RunningWorkbookPath;
        FaultInjectingFileOperations operations = new()
        {
            FailAfterCommitMoveNumber = 1,
            FailDeletePath = runningPath
        };
        DocumentationLogSaveService service = environment.CreateSaveService(
            TestTimestamp,
            operations);

        DocumentationLogSaveException exception =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => service.Save(seeded.Request),
                "A deliberately blocked rollback did not fail the transaction.");

        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveErrorCategory.RollbackFailure,
            exception.ErrorCategory,
            "An incomplete rollback did not use RollbackFailure.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveStage.Rollback,
            exception.Stage,
            "An incomplete rollback was reported at the wrong stage.");
        DocumentationLogTestAssert.True(
            !exception.PreviousStateRestored
            && exception.ManualReviewRequired,
            "An incomplete rollback did not require manual review.");
        DocumentationLogTestAssert.True(
            exception.ManualReviewLocations.Contains(
                runningPath,
                StringComparer.OrdinalIgnoreCase)
            && exception.ManualReviewLocations.Contains(
                environment.Paths.LogIndexFilePath,
                StringComparer.OrdinalIgnoreCase)
            && exception.ManualReviewLocations.Contains(
                environment.Paths.SequenceStateFilePath,
                StringComparer.OrdinalIgnoreCase)
            && exception.ManualReviewLocations.Any(path => IsPrivatePath(
                path,
                ".rollback")),
            "An incomplete rollback omitted required destination/private review paths.");
        DocumentationLogTestAssert.True(
            File.Exists(runningPath)
            && !File.ReadAllBytes(runningPath).SequenceEqual(before[runningPath]),
            "The forced rollback failure unexpectedly reported manual review after fully restoring the Running workbook.");
        DocumentationLogTestAssert.True(
            exception.ManualReviewLocations
                .Where(path => IsPrivatePath(path, ".rollback"))
                .Any(File.Exists),
            "The forced rollback failure did not retain the recoverable original backup it reported.");

        foreach (string path in seeded.AllArtifactPaths.Where(path =>
                     !string.Equals(
                         path,
                         runningPath,
                         StringComparison.OrdinalIgnoreCase)))
        {
            DocumentationLogTestAssert.True(
                File.ReadAllBytes(path).SequenceEqual(before[path]),
                $"Forced rollback failure damaged an unrelated artifact: {path}");
        }
    }

    private static SeededTransaction CreateSeededTransaction(
        DocumentationLogSyntheticEnvironment environment)
    {
        string running = environment.CreateRunning(
            LogType.ScriptEditingLog,
            "Seeded Transaction");
        DocumentationLogSaveRequest request = environment.CreateScriptRequest(
            LogType.ScriptEditingLog,
            running,
            "1953; 2093; 3001");
        DocumentationLogSaveResult result = environment.CreateSaveService(TestTimestamp)
            .Save(request);
        string[] paths =
        [
            result.RunningWorkbookPath,
            .. result.HotelHistoryPaths,
            .. result.PmsHistoryPaths,
            result.IndexPath,
            result.SequenceStatePath
        ];
        DocumentationLogTestAssert.Equal(
            8,
            paths.Length,
            "The seeded transaction does not contain the expected eight artifacts.");
        return new SeededTransaction(request, result, paths);
    }

    private static string[] GetNewFanOutDestinations(
        DocumentationLogSyntheticEnvironment environment,
        LogType logType)
    {
        return
        [
            environment.Paths.ResolveHotelHistoryPath(
                logType,
                environment.Hotel1953.FolderName),
            environment.Paths.ResolveHotelHistoryPath(
                logType,
                environment.Hotel2093.FolderName),
            environment.Paths.ResolveHotelHistoryPath(
                logType,
                environment.Hotel3001.FolderName),
            environment.Paths.ResolvePmsHistoryPath(
                logType,
                environment.Mews.FolderName),
            environment.Paths.ResolvePmsHistoryPath(
                logType,
                environment.Opera.FolderName),
            environment.Paths.LogIndexFilePath,
            environment.Paths.SequenceStateFilePath
        ];
    }

    private static IReadOnlyDictionary<string, byte[]> CaptureFiles(
        IEnumerable<string> paths)
    {
        return paths.ToDictionary(
            Path.GetFullPath,
            File.ReadAllBytes,
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AssertFilesEqual(
        IReadOnlyDictionary<string, byte[]> expected)
    {
        foreach ((string path, byte[] content) in expected)
        {
            DocumentationLogTestAssert.True(
                File.Exists(path),
                $"Rollback did not restore an existing artifact: {path}");
            DocumentationLogTestAssert.True(
                File.ReadAllBytes(path).SequenceEqual(content),
                $"Rollback did not restore exact original bytes: {path}");
        }
    }

    private static void AssertFilesAbsent(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            DocumentationLogTestAssert.True(
                !File.Exists(path),
                $"A failed transaction left a new destination behind: {path}");
        }
    }

    private static void AssertNoTransactionResidue(string rootPath)
    {
        string[] residues = Directory.EnumerateFiles(
                rootPath,
                "*",
                SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).Contains(
                ".documentationlog-",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        DocumentationLogTestAssert.True(
            residues.Length == 0,
            "The transaction left private stage/rollback files behind: "
                + string.Join(", ", residues));
    }

    private static bool IsPrivatePath(string path, string marker)
    {
        return Path.GetFileName(path).Contains(
            marker,
            StringComparison.OrdinalIgnoreCase);
    }

    private static int Run(
        IEnumerable<(string Name, Action Body)> tests,
        TextWriter output)
    {
        (string Name, Action Body)[] cases = tests.ToArray();
        int failed = 0;
        output.WriteLine($"Documentation-log transaction harness: {cases.Length} tests");
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
            ? $"[PASS] All {cases.Length} documentation-log transaction tests passed."
            : $"[FAIL] {failed} of {cases.Length} documentation-log transaction tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private sealed record SeededTransaction(
        DocumentationLogSaveRequest Request,
        DocumentationLogSaveResult SeedResult,
        IReadOnlyList<string> AllArtifactPaths);

    private sealed class FaultInjectingFileOperations
        : IDocumentationLogTransactionFileOperations
    {
        private readonly DocumentationLogTransactionFileOperations inner = new();
        private readonly HashSet<string> committedPaths =
            new(StringComparer.OrdinalIgnoreCase);
        private int writeCount;
        private int backupMoveCount;
        private int commitMoveCount;
        private bool writeFailureInjected;
        private bool backupFailureInjected;
        private bool commitFailureInjected;
        private bool hashFailureInjected;
        private bool writeActionInvoked;
        private bool deleteFailureInjected;

        public int? FailAfterWriteNumber { get; init; }

        public int? FailAfterBackupMoveNumber { get; init; }

        public int? FailAfterCommitMoveNumber { get; init; }

        public int? InvokeAfterWriteNumber { get; init; }

        public Action? AfterWriteAction { get; init; }

        public string? FailHashAfterCommitPath { get; init; }

        public string? FailDeletePath { get; init; }

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
            inner.WriteNewAndFlush(path, content);
            writeCount++;

            if (!writeActionInvoked
                && InvokeAfterWriteNumber == writeCount)
            {
                writeActionInvoked = true;
                AfterWriteAction?.Invoke();
            }

            if (!writeFailureInjected
                && FailAfterWriteNumber == writeCount)
            {
                writeFailureInjected = true;
                throw new IOException(
                    $"Injected fault after staging write {writeCount}.");
            }
        }

        public long GetFileLength(string path)
        {
            return inner.GetFileLength(path);
        }

        public byte[] ComputeSha256(string path)
        {
            string? configuredPath = FailHashAfterCommitPath;
            if (!hashFailureInjected
                && configuredPath is not null
                && string.Equals(
                    Path.GetFullPath(path),
                    Path.GetFullPath(configuredPath),
                    StringComparison.OrdinalIgnoreCase)
                && committedPaths.Contains(Path.GetFullPath(path)))
            {
                hashFailureInjected = true;
                throw new IOException(
                    "Injected fault during final destination verification.");
            }

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
            bool isBackup = IsPrivatePath(destinationPath, ".rollback");
            bool isCommit = IsPrivatePath(sourcePath, ".stage");
            inner.Move(sourcePath, destinationPath);

            if (isBackup)
            {
                backupMoveCount++;
                if (!backupFailureInjected
                    && FailAfterBackupMoveNumber == backupMoveCount)
                {
                    backupFailureInjected = true;
                    throw new IOException(
                        $"Injected fault after backup move {backupMoveCount}.");
                }
            }

            if (isCommit)
            {
                commitMoveCount++;
                committedPaths.Add(Path.GetFullPath(destinationPath));
                if (!commitFailureInjected
                    && FailAfterCommitMoveNumber == commitMoveCount)
                {
                    commitFailureInjected = true;
                    throw new IOException(
                        $"Injected fault after commit move {commitMoveCount}.");
                }
            }
        }

        public void Delete(string path)
        {
            string? configuredPath = FailDeletePath;
            if (!deleteFailureInjected
                && configuredPath is not null
                && string.Equals(
                    Path.GetFullPath(path),
                    Path.GetFullPath(configuredPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                deleteFailureInjected = true;
                throw new IOException(
                    "Injected fault while removing a partially committed destination during rollback.");
            }

            inner.Delete(path);
        }
    }
}
