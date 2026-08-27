using System.Text;
using ClosedXML.Excel;
using DocumentationLoggingDashboard.DocumentationLogs;
using DocumentationLoggingDashboard.Models;

namespace DocumentationLoggingDashboard.GeometryTests;

/// <summary>
/// Protects the V1 transition boundary: historical daily TXT/index bytes remain
/// authoritative and untouched while new DEBUG/EDIT/CREATE events use the Excel
/// workflow and retain the established prefixes, folders, and index format.
/// </summary>
internal static class V1SyntheticRegressionTests
{
    private static readonly DateTimeOffset TestTimestamp =
        new(2026, 7, 20, 10, 24, 0, TimeSpan.FromHours(-4));

    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        try
        {
            using DocumentationLogSyntheticEnvironment environment = new();
            byte[] detailedQaIndexBefore = File.ReadAllBytes(
                environment.QaPaths.QaReportIndexFilePath);
            Dictionary<LogType, byte[]> legacyBytes = SeedLegacyTxt(environment);
            byte[] historicalIndex = SeedHistoricalIndex(environment);
            Dictionary<LogType, DocumentationLogSaveResult> results = [];

            foreach ((LogType type, string prefix, string stem) in Cases())
            {
                string running = environment.CreateRunning(type, stem);
                DocumentationLogSaveResult result = environment
                    .CreateSaveService(TestTimestamp)
                    .Save(CreateRequest(environment, type, running));
                results[type] = result;

                Check(
                    result.Event.LogId == $"{prefix}-20260720-005",
                    $"{type} did not continue above its V1 TXT/index sequence.");
                Check(
                    result.RunningWorkbookPath.EndsWith(
                        Path.Combine("Running", running),
                        StringComparison.OrdinalIgnoreCase),
                    $"{type} did not save to its legacy folder's Running child.");
                Check(
                    DocumentationLogSyntheticEnvironment.CountWorkbookRows(
                        result.RunningWorkbookPath) == 1,
                    $"{type} Running workbook did not receive exactly one row.");
                CheckEveryCopyContainsOneIdenticalEvent(result);
            }

            AssertLegacyTxtPreserved(environment, legacyBytes);
            AssertIndexCompatibility(environment, historicalIndex, results);
            AssertQaIsolation(environment, detailedQaIndexBefore);

            foreach ((LogType type, string prefix, _) in Cases())
            {
                string next = environment.CreateSaveService(TestTimestamp)
                    .CreatePreview(CreateRequest(
                        environment,
                        type,
                        results[type].RunningWorkbookFileName))
                    .LogId;
                Check(
                    next == $"{prefix}-20260720-006",
                    $"{type} durable sequence did not advance once per logical event.");
            }

            output.WriteLine(
                "[PASS] V1 compatibility regression: DEBUG/EDIT/CREATE prefixes, legacy folders, byte-exact historical TXT/index prefixes, Excel Running/history routing, one index row per event, and QA isolation passed.");
            output.WriteLine(
                "  Historical daily TXT remained read-only; all new synthetic entries used transactional Excel workbooks.");
            return 0;
        }
        catch (Exception exception)
        {
            output.WriteLine("[FAIL] V1 compatibility regression");
            output.WriteLine(exception);
            return 1;
        }
    }

    private static Dictionary<LogType, byte[]> SeedLegacyTxt(
        DocumentationLogSyntheticEnvironment environment)
    {
        Dictionary<LogType, byte[]> bytes = [];

        foreach ((LogType type, string prefix, _) in Cases())
        {
            byte[] content = Encoding.UTF8.GetBytes(
                $"Historical V1 record\r\nLog ID: {prefix}-20260720-003\r\n");
            string path = environment.Paths.GetLegacyDailyLogFilePath(
                type,
                TestTimestamp.DateTime);
            File.WriteAllBytes(path, content);
            bytes[type] = content;
        }

        return bytes;
    }

    private static byte[] SeedHistoricalIndex(
        DocumentationLogSyntheticEnvironment environment)
    {
        string[] lines = Cases()
            .Select(testCase =>
            {
                string legacyPath = environment.Paths.GetLegacyDailyLogFilePath(
                    testCase.Type,
                    TestTimestamp.DateTime);
                return $"2026-07-20 | 9:00 AM | {testCase.Prefix}-20260720-004 | {DocumentationLogWorkbookSchema.GetDisplayName(testCase.Type)} | Historical item | Historical association | {legacyPath}";
            })
            .ToArray();
        byte[] content = Encoding.UTF8.GetBytes(
            string.Join("\r\n", lines) + "\r\n");
        File.WriteAllBytes(environment.Paths.LogIndexFilePath, content);
        return content;
    }

    private static DocumentationLogSaveRequest CreateRequest(
        DocumentationLogSyntheticEnvironment environment,
        LogType type,
        string runningWorkbookFileName)
    {
        return type switch
        {
            LogType.DebuggingLog => environment.CreateDebuggingRequest(
                runningWorkbookFileName,
                createdBy: string.Empty,
                notes: string.Empty),
            LogType.ScriptEditingLog or LogType.ScriptCreationLog =>
                environment.CreateScriptRequest(
                    type,
                    runningWorkbookFileName,
                    "1953; 2093; 3001",
                    createdBy: string.Empty,
                    notes: string.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private static void CheckEveryCopyContainsOneIdenticalEvent(
        DocumentationLogSaveResult result)
    {
        string[] paths = result.HotelHistoryPaths
            .Concat(result.PmsHistoryPaths)
            .Prepend(result.RunningWorkbookPath)
            .ToArray();
        string[] signatures = paths.Select(ReadOnlyRowSignature).ToArray();

        Check(
            signatures.All(signature => signature == signatures[0]),
            $"{result.Event.LogType} copies did not contain one identical event row.");
        Check(
            paths.All(path =>
                DocumentationLogSyntheticEnvironment.CountWorkbookRows(path) == 1),
            $"{result.Event.LogType} routed a duplicate or missing workbook row.");

        int expectedHotelCopies = result.Event.LogType == LogType.DebuggingLog
            ? 1
            : 3;
        int expectedPmsCopies = result.Event.LogType == LogType.DebuggingLog
            ? 1
            : 2;
        Check(
            result.HotelHistoryPaths.Count == expectedHotelCopies
                && result.PmsHistoryPaths.Count == expectedPmsCopies,
            $"{result.Event.LogType} Hotel/PMS fan-out or PMS deduplication changed.");
    }

    private static string ReadOnlyRowSignature(string path)
    {
        using XLWorkbook workbook = new(path);
        IXLWorksheet worksheet = workbook.Worksheet(
            DocumentationLogWorkbookSchema.DataWorksheetName);
        int columns = worksheet.Tables.Single().ColumnCount();
        string timestamp = worksheet.Cell(4, 1).GetDateTime().ToString("O");
        return string.Join(
            "\u001f",
            new[] { timestamp }.Concat(
                Enumerable.Range(2, columns - 1)
                    .Select(column => worksheet.Cell(4, column).GetString())));
    }

    private static void AssertLegacyTxtPreserved(
        DocumentationLogSyntheticEnvironment environment,
        IReadOnlyDictionary<LogType, byte[]> legacyBytes)
    {
        foreach ((LogType type, byte[] expected) in legacyBytes)
        {
            string path = environment.Paths.GetLegacyDailyLogFilePath(
                type,
                TestTimestamp.DateTime);
            Check(
                File.ReadAllBytes(path).SequenceEqual(expected),
                $"Historical {type} TXT bytes changed.");
            Check(
                Directory.EnumerateFiles(
                    environment.Paths.GetLegacyLogDirectory(type),
                    "*.txt",
                    SearchOption.TopDirectoryOnly).Count() == 1,
                $"A new daily {type} TXT file was created.");
        }
    }

    private static void AssertIndexCompatibility(
        DocumentationLogSyntheticEnvironment environment,
        byte[] historicalIndex,
        IReadOnlyDictionary<LogType, DocumentationLogSaveResult> results)
    {
        byte[] finalBytes = File.ReadAllBytes(environment.Paths.LogIndexFilePath);
        Check(
            finalBytes.AsSpan(0, historicalIndex.Length).SequenceEqual(
                historicalIndex),
            "Historical LogIndex bytes were rewritten.");

        string[] lines = File.ReadAllLines(environment.Paths.LogIndexFilePath);
        Check(lines.Length == 6, $"LogIndex contains {lines.Length} lines, expected 6.");

        foreach (DocumentationLogSaveResult result in results.Values)
        {
            string line = lines.Single(candidate => candidate.Contains(
                result.Event.LogId,
                StringComparison.Ordinal));
            Check(
                line.Split(" | ", StringSplitOptions.None).Length == 7,
                $"Index line for {result.Event.LogId} does not have seven fields.");
            Check(
                line.Contains(result.RunningWorkbookPath, StringComparison.Ordinal)
                    && line.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase),
                $"Index line for {result.Event.LogId} does not point to its Running workbook.");
        }
    }

    private static void AssertQaIsolation(
        DocumentationLogSyntheticEnvironment environment,
        byte[] detailedQaIndexBefore)
    {
        Check(
            File.ReadAllBytes(environment.QaPaths.QaReportIndexFilePath)
                .SequenceEqual(detailedQaIndexBefore),
            "Documentation saves contaminated the Detailed QA index.");
        Check(
            !File.Exists(environment.QaPaths.QuickQaSettingsFilePath),
            "Documentation saves contaminated Quick QA preferences.");
        Check(
            !Directory.EnumerateFiles(
                    environment.QaPaths.QaReportsRootPath,
                    "*.pdf",
                    SearchOption.AllDirectories)
                .Any(),
            "Documentation saves created a Detailed QA PDF.");
        Check(
            !Directory.EnumerateFiles(
                    environment.QaPaths.QaReportsRootPath,
                    "QuickQAHistory.xlsx",
                    SearchOption.AllDirectories)
                .Any(),
            "Documentation saves created a Quick QA history workbook.");
    }

    private static IEnumerable<(LogType Type, string Prefix, string Stem)> Cases()
    {
        yield return (LogType.DebuggingLog, "DEBUG", "V1 Debug Running");
        yield return (LogType.ScriptEditingLog, "EDIT", "V1 Edit Running");
        yield return (LogType.ScriptCreationLog, "CREATE", "V1 Create Running");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
