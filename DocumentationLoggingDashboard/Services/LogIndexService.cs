using System.Globalization;
using System.Text;
using DocumentationLoggingDashboard.Models;

namespace DocumentationLoggingDashboard.Services;

/// <summary>
/// Maintains the plain-text index for generated documentation logs.
/// </summary>
public sealed class LogIndexService
{
    private const string IndexFileName = "LogIndex.txt";

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly LogFileService logFileService;
    private readonly LogTemplateService logTemplateService;

    public LogIndexService(LogFileService logFileService, LogTemplateService logTemplateService)
    {
        this.logFileService = logFileService;
        this.logTemplateService = logTemplateService;
    }

    public string GetIndexFilePath()
    {
        return Path.Combine(logFileService.GetIndexFolderPath(), IndexFileName);
    }

    public string AppendEntry(LogEntry entry, string savedFilePath)
    {
        string indexFilePath = EnsureIndexFile();
        string indexLine = BuildIndexLine(entry, savedFilePath);

        using StreamWriter writer = new(indexFilePath, append: true, Utf8NoBom);
        writer.WriteLine(indexLine);

        return indexFilePath;
    }

    private string EnsureIndexFile()
    {
        logFileService.EnsureDocumentationFolderStructure();

        return GetIndexFilePath();
    }

    private string BuildIndexLine(LogEntry entry, string savedFilePath)
    {
        SummaryItems summaryItems = GetSummaryItems(entry);

        string[] parts =
        [
            entry.DateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            entry.DateTime.ToString("h:mm tt", CultureInfo.InvariantCulture),
            NormalizeIndexValue(entry.LogId),
            NormalizeIndexValue(logTemplateService.GetDisplayName(entry.LogType)),
            summaryItems.PrimaryItem,
            summaryItems.SecondaryItem,
            NormalizeIndexValue(savedFilePath)
        ];

        return string.Join(" | ", parts);
    }

    private SummaryItems GetSummaryItems(LogEntry entry)
    {
        return entry.LogType switch
        {
            LogType.DebuggingLog => new SummaryItems(
                $"Hotel: {GetFieldValue(entry, "hotelName")} / {GetFieldValue(entry, "hotelId")}",
                $"PMS: {GetFieldValue(entry, "pms")}"),
            LogType.ScriptEditingLog => new SummaryItems(
                $"Script: {GetFieldValue(entry, "scriptName")}",
                $"Hotel Applied To: {GetFieldValue(entry, "hotelAppliedTo")}"),
            LogType.ScriptCreationLog => new SummaryItems(
                $"Script: {GetFieldValue(entry, "scriptName")}",
                $"Hotels That Use This Script: {GetFieldValue(entry, "hotelsThatUseThisScript")}"),
            _ => new SummaryItems("N/A", "N/A")
        };
    }

    private static string GetFieldValue(LogEntry entry, string fieldKey)
    {
        entry.FieldValues.TryGetValue(fieldKey, out string? value);

        return NormalizeIndexValue(value);
    }

    private static string NormalizeIndexValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "N/A";
        }

        string normalizedValue = string.Join(
            " / ",
            value
                .Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0));

        return string.IsNullOrWhiteSpace(normalizedValue)
            ? "N/A"
            : normalizedValue.Replace("|", "/", StringComparison.Ordinal);
    }

    private sealed record SummaryItems(string PrimaryItem, string SecondaryItem);
}
