using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.Services;

namespace DocumentationLoggingDashboard.GeometryTests;

internal static class V1SyntheticRegressionTests
{
    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        string parentPath = Path.GetFullPath(Path.Combine(
            Path.GetTempPath(),
            "DocumentationLoggingDashboard.V1RegressionTests"));
        string rootPath = Path.Combine(
            parentPath,
            "r-" + Guid.NewGuid().ToString("N")[..16]);
        string settingsPath = Path.Combine(AppContext.BaseDirectory, "user-settings.json");
        bool settingsCreated = false;

        try
        {
            if (File.Exists(settingsPath))
            {
                throw new InvalidOperationException(
                    $"V1 regression refused to replace existing runtime settings '{settingsPath}'.");
            }

            SettingsService settingsService = new();
            settingsService.UpdateDocumentationRootFolder(rootPath);
            settingsCreated = true;

            LogTemplateService templateService = new();
            LogFileService fileService = new(settingsService, templateService);
            LogIdService idService = new(templateService, fileService);
            LogIndexService indexService = new(fileService, templateService);
            fileService.EnsureDocumentationRootFolder();

            DateTime timestamp = new(2026, 7, 20, 10, 24, 0, DateTimeKind.Local);
            V1Case[] cases =
            [
                new(
                    LogType.DebuggingLog,
                    "DEBUG",
                    "DebuggingLogs",
                    "DebuggingLog",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["hotelName"] = "Synthetic Harbour & Pine",
                        ["hotelId"] = "SYN-V1-001",
                        ["pms"] = "Synthetic PMS One",
                        ["errorShownOnTicket"] = "Synthetic error summary",
                        ["rootCause"] = "Synthetic root cause",
                        ["fixApplied"] = "Synthetic fix summary"
                    }),
                new(
                    LogType.ScriptEditingLog,
                    "EDIT",
                    "ScriptEditingLogs",
                    "ScriptEditingLog",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["scriptName"] = "Synthetic Edit Script",
                        ["reasonForEdit"] = "Synthetic edit reason",
                        ["changesMade"] = "Synthetic edit summary",
                        ["hotelAppliedTo"] = "Synthetic Hotel Group"
                    }),
                new(
                    LogType.ScriptCreationLog,
                    "CREATE",
                    "ScriptCreationLogs",
                    "ScriptCreationLog",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["scriptName"] = "Synthetic Created Script",
                        ["reasonForCreation"] = "Synthetic creation reason",
                        ["scriptPurpose"] = "Synthetic script purpose",
                        ["hotelsThatUseThisScript"] = "Synthetic Hotel A; Synthetic Hotel B"
                    })
            ];

            foreach (V1Case testCase in cases)
            {
                string expectedFirstId = $"{testCase.Prefix}-20260720-001";
                string logId = idService.GenerateNextLogId(testCase.LogType, timestamp);
                Check(logId == expectedFirstId, $"Unexpected first {testCase.LogType} ID '{logId}'.");

                LogEntry entry = new()
                {
                    LogType = testCase.LogType,
                    LogId = logId,
                    DateTime = timestamp,
                    CreatedBy = "Synthetic QA",
                    NotesFollowUp = "Synthetic aggregate-only follow-up"
                };
                foreach ((string key, string value) in testCase.Fields)
                {
                    entry.FieldValues[key] = value;
                }

                string formatted = templateService.FormatEntry(entry);
                string savedPath = fileService.SaveEntry(entry, formatted);
                string indexPath = indexService.AppendEntry(entry, savedPath);
                string expectedPath = Path.Combine(
                    rootPath,
                    testCase.FolderName,
                    $"2026-07-20_{testCase.FileSuffix}.txt");

                Check(PathsEqual(savedPath, expectedPath), $"Unexpected {testCase.LogType} path '{savedPath}'.");
                Check(File.Exists(savedPath), $"Missing {testCase.LogType} output.");
                Check(
                    File.ReadAllText(savedPath) == formatted.TrimEnd() + Environment.NewLine,
                    $"{testCase.LogType} text output changed from the V1 template.");
                Check(
                    idService.GenerateNextLogId(testCase.LogType, timestamp)
                        == $"{testCase.Prefix}-20260720-002",
                    $"{testCase.LogType} daily sequence did not advance to 002.");
                Check(
                    PathsEqual(fileService.EnsureLogFolder(testCase.LogType), Path.GetDirectoryName(expectedPath)!),
                    $"{testCase.LogType} folder action resolved an unexpected directory.");
                Check(File.Exists(indexPath), "The central V1 LogIndex was not created.");
            }

            string v1IndexPath = indexService.GetIndexFilePath();
            string[] indexLines = File.ReadAllLines(v1IndexPath);
            Check(indexLines.Length == cases.Length, $"V1 LogIndex contains {indexLines.Length} lines, expected 3.");
            foreach (V1Case testCase in cases)
            {
                string expectedId = $"{testCase.Prefix}-20260720-001";
                string line = indexLines.Single(candidate => candidate.Contains(expectedId, StringComparison.Ordinal));
                Check(line.Split(" | ", StringSplitOptions.None).Length == 7, $"Index line for {expectedId} does not have seven fields.");
            }

            Check(!Directory.Exists(Path.Combine(rootPath, "QAReports")), "V1 regression created a QAReports tree.");
            Check(
                !Directory.EnumerateFiles(rootPath, "*QAReportIndex*", SearchOption.AllDirectories).Any(),
                "V1 regression contaminated a QA report index.");
            Check(
                !Directory.Exists(Path.Combine(rootPath, "ByHotel"))
                    && !Directory.Exists(Path.Combine(rootPath, "ByPMS")),
                "Debugging Logs were routed into Hotel/PMS folders; that V2.1 feature must remain deferred.");

            output.WriteLine(
                "[PASS] V1 synthetic regression: DEBUG/EDIT/CREATE prefixes, daily sequence, filenames, exact text, three-entry LogIndex, folder resolution, and V1/V2 isolation passed.");
            output.WriteLine(
                "  Debugging Log remained only in DebuggingLogs; Hotel/PMS routing is correctly not implemented.");
            return 0;
        }
        catch (Exception exception)
        {
            output.WriteLine("[FAIL] V1 synthetic regression");
            output.WriteLine(exception.ToString());
            return 1;
        }
        finally
        {
            if (settingsCreated && File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }

            DeleteVerifiedRoot(rootPath, parentPath);
        }
    }

    private static void DeleteVerifiedRoot(string rootPath, string parentPath)
    {
        string fullRoot = Path.GetFullPath(rootPath);
        string fullParent = Path.GetFullPath(parentPath);
        string leaf = Path.GetFileName(fullRoot);
        bool verifiedLeaf = leaf.Length == 18
            && leaf.StartsWith("r-", StringComparison.Ordinal)
            && leaf[2..].All(Uri.IsHexDigit);
        bool strictlyInside = fullRoot.StartsWith(
            fullParent.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);

        if (!verifiedLeaf || !strictlyInside)
        {
            throw new InvalidOperationException("V1 regression cleanup refused an unverified path.");
        }

        if (Directory.Exists(fullRoot))
        {
            Directory.Delete(fullRoot, recursive: true);
        }

        try
        {
            Directory.Delete(fullParent, recursive: false);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed record V1Case(
        LogType LogType,
        string Prefix,
        string FolderName,
        string FileSuffix,
        IReadOnlyDictionary<string, string> Fields);
}
