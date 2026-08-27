using System.Globalization;
using System.Text;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.Services;

namespace DocumentationLoggingDashboard.DocumentationLogs;

public sealed record DocumentationLogIndexUpdatePlan(
    byte[] UpdatedContent,
    string AppendedLine);

/// <summary>
/// Prepares, but does not independently write, the legacy-compatible
/// LogIndex.txt update for one logical Excel event.
/// </summary>
public sealed class DocumentationLogIndexService
{
    private static readonly byte[] CrLf = "\r\n"u8.ToArray();
    private readonly LogTemplateService templateService;

    public DocumentationLogIndexService(LogTemplateService? templateService = null)
    {
        this.templateService = templateService ?? new LogTemplateService();
    }

    public DocumentationLogIndexUpdatePlan PrepareAppend(
        ReadOnlyMemory<byte> existingContent,
        DocumentationLogEvent logEvent,
        string runningWorkbookPath)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        if (string.IsNullOrWhiteSpace(runningWorkbookPath))
        {
            throw new ArgumentException(
                "A Running workbook path is required.",
                nameof(runningWorkbookPath));
        }

        string line = BuildIndexLine(logEvent, runningWorkbookPath);
        byte[] lineBytes = Encoding.UTF8.GetBytes(line);
        bool needsLeadingNewline = !existingContent.IsEmpty
            && existingContent.Span[^1] is not (byte)'\r' and not (byte)'\n';
        int length = existingContent.Length
            + (needsLeadingNewline ? CrLf.Length : 0)
            + lineBytes.Length
            + CrLf.Length;
        byte[] updated = new byte[length];
        int offset = 0;
        existingContent.Span.CopyTo(updated);
        offset += existingContent.Length;

        if (needsLeadingNewline)
        {
            CrLf.CopyTo(updated, offset);
            offset += CrLf.Length;
        }

        lineBytes.CopyTo(updated, offset);
        offset += lineBytes.Length;
        CrLf.CopyTo(updated, offset);
        return new DocumentationLogIndexUpdatePlan(updated, line);
    }

    public string BuildIndexLine(
        DocumentationLogEvent logEvent,
        string runningWorkbookPath)
    {
        (string Primary, string Secondary) summary = logEvent.LogType switch
        {
            LogType.DebuggingLog => (
                $"Hotel: {logEvent.Hotels[0].HotelName} / {logEvent.Hotels[0].HotelId}",
                $"PMS: {logEvent.Hotels[0].Pms.PmsName}"),
            LogType.ScriptEditingLog or LogType.ScriptCreationLog => (
                $"Script: {logEvent.GetValue(DocumentationLogFieldKeys.ScriptName)}",
                $"Hotel IDs: {logEvent.CanonicalHotelIds}"),
            _ => ("N/A", "N/A")
        };
        string[] parts =
        [
            logEvent.Timestamp.DateTime.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            logEvent.Timestamp.DateTime.ToString(
                "h:mm tt",
                CultureInfo.InvariantCulture),
            Normalize(logEvent.LogId),
            Normalize(templateService.GetDisplayName(logEvent.LogType)),
            Normalize(summary.Primary),
            Normalize(summary.Secondary),
            Normalize(Path.GetFullPath(runningWorkbookPath))
        ];
        return string.Join(" | ", parts);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "N/A";
        }

        string normalized = string.Join(
            " / ",
            value.Split(
                    ["\r\n", "\r", "\n"],
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0));
        return normalized.Length == 0
            ? "N/A"
            : normalized.Replace('|', '/');
    }
}
