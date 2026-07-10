using System.Globalization;
using System.Text;
using DocumentationLoggingDashboard.Models;

namespace DocumentationLoggingDashboard.Services;

/// <summary>
/// Provides centralized metadata, field templates, and plain-text formatting for documentation logs.
/// </summary>
public sealed class LogTemplateService
{
    public const string SeparatorLine = "==================================================";

    public const string CreatedByKey = "createdBy";

    public const string NotesFollowUpKey = "notesFollowUp";

    private static readonly IReadOnlyList<LogType> SupportedLogTypes =
    [
        LogType.DebuggingLog,
        LogType.ScriptEditingLog,
        LogType.ScriptCreationLog
    ];

    public IReadOnlyList<LogType> GetSupportedLogTypes()
    {
        return SupportedLogTypes;
    }

    public IReadOnlyList<LogFieldDefinition> GetFields(LogType logType)
    {
        List<LogFieldDefinition> fields = [.. GetLogTypeFields(logType)];
        fields.AddRange(GetCommonFields());

        return fields;
    }

    public IReadOnlyList<LogFieldDefinition> GetLogTypeFields(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog =>
            [
                Required("hotelName", "Hotel Name"),
                Required("hotelId", "Hotel ID"),
                Required("pms", "PMS"),
                Required("errorShownOnTicket", "Error Shown On Ticket", isMultiline: true),
                Required("rootCause", "Root Cause", isMultiline: true),
                Required("fixApplied", "Fix Applied", isMultiline: true)
            ],
            LogType.ScriptEditingLog =>
            [
                Required("scriptName", "Script Name"),
                Required("reasonForEdit", "Reason For Edit", isMultiline: true),
                Required("changesMade", "Changes Made", isMultiline: true),
                Required("hotelAppliedTo", "Hotel Applied To")
            ],
            LogType.ScriptCreationLog =>
            [
                Required("scriptName", "Script Name"),
                Required("reasonForCreation", "Reason For Creation", isMultiline: true),
                Required("scriptPurpose", "Script Purpose / What It Does", isMultiline: true),
                Required("hotelsThatUseThisScript", "Hotels That Use This Script", isMultiline: true)
            ],
            _ => []
        };
    }

    public IReadOnlyList<LogFieldDefinition> GetCommonFields()
    {
        return
        [
            Optional(CreatedByKey, "Created By"),
            Optional(NotesFollowUpKey, "Notes / Follow-up", isMultiline: true)
        ];
    }

    public string GetDisplayName(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => "Debugging Log",
            LogType.ScriptEditingLog => "Script Editing Log",
            LogType.ScriptCreationLog => "Script Creation Log",
            _ => logType.ToString()
        };
    }

    public string GetEntryHeader(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => "DEBUGGING LOG ENTRY",
            LogType.ScriptEditingLog => "SCRIPT EDITING LOG ENTRY",
            LogType.ScriptCreationLog => "SCRIPT CREATION LOG ENTRY",
            _ => $"{GetDisplayName(logType).ToUpperInvariant()} ENTRY"
        };
    }

    public string GetLogIdPrefix(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => "DEBUG",
            LogType.ScriptEditingLog => "EDIT",
            LogType.ScriptCreationLog => "CREATE",
            _ => string.Empty
        };
    }

    public string GetFolderName(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => "DebuggingLogs",
            LogType.ScriptEditingLog => "ScriptEditingLogs",
            LogType.ScriptCreationLog => "ScriptCreationLogs",
            _ => string.Empty
        };
    }

    public string GetFileNameSuffix(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => "DebuggingLog",
            LogType.ScriptEditingLog => "ScriptEditingLog",
            LogType.ScriptCreationLog => "ScriptCreationLog",
            _ => string.Empty
        };
    }

    public string GetDailyFileName(LogType logType, DateTime date)
    {
        return $"{date:yyyy-MM-dd}_{GetFileNameSuffix(logType)}.txt";
    }

    public string FormatEntry(LogEntry entry)
    {
        StringBuilder builder = new();

        builder.AppendLine(SeparatorLine);
        builder.AppendLine(GetEntryHeader(entry.LogType));
        builder.AppendLine($"Log ID: {NormalizeRequiredValue(entry.LogId)}");
        builder.AppendLine($"Date/Time: {entry.DateTime.ToString("yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"Log Type: {GetDisplayName(entry.LogType)}");
        builder.AppendLine();

        AppendField(builder, "Created By", NormalizeOptionalValue(entry.CreatedBy));

        foreach (LogFieldDefinition field in GetLogTypeFields(entry.LogType))
        {
            entry.FieldValues.TryGetValue(field.Key, out string? value);
            AppendField(builder, field.Label, NormalizeRequiredValue(value));
        }

        AppendField(builder, "Notes / Follow-up", NormalizeOptionalValue(entry.NotesFollowUp));
        builder.AppendLine(SeparatorLine);

        return builder.ToString();
    }

    private static void AppendField(StringBuilder builder, string label, string value)
    {
        builder.AppendLine($"{label}:");
        builder.AppendLine(value);
        builder.AppendLine();
    }

    private static string NormalizeRequiredValue(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "N/A" : value.Trim();
    }

    private static LogFieldDefinition Required(string key, string label, bool isMultiline = false)
    {
        return new LogFieldDefinition
        {
            Key = key,
            Label = label,
            IsRequired = true,
            IsMultiline = isMultiline
        };
    }

    private static LogFieldDefinition Optional(string key, string label, bool isMultiline = false)
    {
        return new LogFieldDefinition
        {
            Key = key,
            Label = label,
            IsRequired = false,
            IsMultiline = isMultiline
        };
    }
}
