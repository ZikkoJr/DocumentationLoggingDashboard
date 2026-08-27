using DocumentationLoggingDashboard.Models;

namespace DocumentationLoggingDashboard.DocumentationLogs;

public sealed record DocumentationLogFormFieldDefinition(
    string Key,
    string Label,
    bool IsRequired,
    bool IsMultiline);

public sealed record DocumentationLogFormContract(
    LogType LogType,
    string DisplayName,
    bool UsesHotelSelector,
    bool UsesManualHotelIds,
    IReadOnlyList<DocumentationLogFormFieldDefinition> Fields);

/// <summary>
/// Owns the exact visible columns, form fields, titles, filenames, and internal
/// workbook marker values for the three Excel documentation-log types.
/// </summary>
public static class DocumentationLogWorkbookSchema
{
    public const string SchemaName =
        "DocumentationLoggingDashboard.LogWorkbook";
    public const int CurrentSchemaVersion = 1;
    public const string DataWorksheetName = "Documentation Log";
    public const string MetadataWorksheetName = "__DLD_Metadata";

    private static readonly IReadOnlyList<LogType> SupportedTypes =
        Array.AsReadOnly(
        [
            LogType.DebuggingLog,
            LogType.ScriptEditingLog,
            LogType.ScriptCreationLog
        ]);

    private static readonly IReadOnlyList<string> DebuggingHeaders =
        Array.AsReadOnly(
        [
            "Date/Time",
            "Log ID",
            "Hotel Name",
            "Hotel ID",
            "PMS",
            "Error Shown On Ticket",
            "Root Cause",
            "Fix Applied",
            "Created By",
            "Notes / Follow-up"
        ]);

    private static readonly IReadOnlyList<string> ScriptEditingHeaders =
        Array.AsReadOnly(
        [
            "Date/Time",
            "Log ID",
            "Hotel IDs",
            "Script Name",
            "Reason For Edit",
            "Changes Made",
            "Created By",
            "Notes / Follow-up"
        ]);

    private static readonly IReadOnlyList<string> ScriptCreationHeaders =
        Array.AsReadOnly(
        [
            "Date/Time",
            "Log ID",
            "Hotel IDs",
            "Script Name",
            "Reason For Creation",
            "Script Purpose / What It Does",
            "Created By",
            "Notes / Follow-up"
        ]);

    private static readonly IReadOnlyList<DocumentationLogFormFieldDefinition>
        DebuggingFields = Array.AsReadOnly(
        [
            Required(
                DocumentationLogFieldKeys.ErrorShownOnTicket,
                "Error Shown On Ticket",
                isMultiline: true),
            Required(
                DocumentationLogFieldKeys.RootCause,
                "Root Cause",
                isMultiline: true),
            Required(
                DocumentationLogFieldKeys.FixApplied,
                "Fix Applied",
                isMultiline: true),
            Optional(DocumentationLogFieldKeys.CreatedBy, "Created By"),
            Optional(
                DocumentationLogFieldKeys.NotesFollowUp,
                "Notes / Follow-up",
                isMultiline: true)
        ]);

    private static readonly IReadOnlyList<DocumentationLogFormFieldDefinition>
        ScriptEditingFields = Array.AsReadOnly(
        [
            Required(
                DocumentationLogFieldKeys.HotelIds,
                "Hotel ID(s)",
                isMultiline: true),
            Required(DocumentationLogFieldKeys.ScriptName, "Script Name"),
            Required(
                DocumentationLogFieldKeys.ReasonForEdit,
                "Reason For Edit",
                isMultiline: true),
            Required(
                DocumentationLogFieldKeys.ChangesMade,
                "Changes Made",
                isMultiline: true),
            Optional(DocumentationLogFieldKeys.CreatedBy, "Created By"),
            Optional(
                DocumentationLogFieldKeys.NotesFollowUp,
                "Notes / Follow-up",
                isMultiline: true)
        ]);

    private static readonly IReadOnlyList<DocumentationLogFormFieldDefinition>
        ScriptCreationFields = Array.AsReadOnly(
        [
            Required(
                DocumentationLogFieldKeys.HotelIds,
                "Hotel ID(s)",
                isMultiline: true),
            Required(DocumentationLogFieldKeys.ScriptName, "Script Name"),
            Required(
                DocumentationLogFieldKeys.ReasonForCreation,
                "Reason For Creation",
                isMultiline: true),
            Required(
                DocumentationLogFieldKeys.ScriptPurpose,
                "Script Purpose / What It Does",
                isMultiline: true),
            Optional(DocumentationLogFieldKeys.CreatedBy, "Created By"),
            Optional(
                DocumentationLogFieldKeys.NotesFollowUp,
                "Notes / Follow-up",
                isMultiline: true)
        ]);

    public static IReadOnlyList<LogType> SupportedLogTypes => SupportedTypes;

    public static IReadOnlyList<string> GetHeaders(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => DebuggingHeaders,
            LogType.ScriptEditingLog => ScriptEditingHeaders,
            LogType.ScriptCreationLog => ScriptCreationHeaders,
            _ => throw UnsupportedLogType(logType)
        };
    }

    public static DocumentationLogFormContract GetFormContract(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => new DocumentationLogFormContract(
                logType,
                GetDisplayName(logType),
                UsesHotelSelector: true,
                UsesManualHotelIds: false,
                DebuggingFields),
            LogType.ScriptEditingLog => new DocumentationLogFormContract(
                logType,
                GetDisplayName(logType),
                UsesHotelSelector: false,
                UsesManualHotelIds: true,
                ScriptEditingFields),
            LogType.ScriptCreationLog => new DocumentationLogFormContract(
                logType,
                GetDisplayName(logType),
                UsesHotelSelector: false,
                UsesManualHotelIds: true,
                ScriptCreationFields),
            _ => throw UnsupportedLogType(logType)
        };
    }

    public static IReadOnlyList<DocumentationLogFormFieldDefinition> GetFormFields(
        LogType logType)
    {
        return GetFormContract(logType).Fields;
    }

    public static string GetDisplayName(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => "Debugging Log",
            LogType.ScriptEditingLog => "Script Editing Log",
            LogType.ScriptCreationLog => "Script Creation Log",
            _ => throw UnsupportedLogType(logType)
        };
    }

    public static string GetHistoryWorkbookFileName(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => "DebuggingLogHistory.xlsx",
            LogType.ScriptEditingLog => "ScriptEditingLogHistory.xlsx",
            LogType.ScriptCreationLog => "ScriptCreationLogHistory.xlsx",
            _ => throw UnsupportedLogType(logType)
        };
    }

    public static string GetTableName(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => "DebuggingLogTable",
            LogType.ScriptEditingLog => "ScriptEditingLogTable",
            LogType.ScriptCreationLog => "ScriptCreationLogTable",
            _ => throw UnsupportedLogType(logType)
        };
    }

    public static DocumentationLogWorkbookContract CreateRunningContract(
        LogType logType)
    {
        return new DocumentationLogWorkbookContract(
            logType,
            DocumentationLogScopeType.Running,
            string.Empty,
            $"{GetDisplayName(logType)} — Running Log");
    }

    public static DocumentationLogWorkbookContract CreateHotelHistoryContract(
        LogType logType,
        DocumentationLogHotel hotel)
    {
        ArgumentNullException.ThrowIfNull(hotel);
        return CreateHotelHistoryContract(
            logType,
            hotel.HotelName,
            hotel.HotelId);
    }

    public static DocumentationLogWorkbookContract CreateHotelHistoryContract(
        LogType logType,
        string hotelName,
        string hotelId)
    {
        string canonicalName = RequireValue(hotelName, nameof(hotelName));
        string canonicalId = RequireValue(hotelId, nameof(hotelId));

        return new DocumentationLogWorkbookContract(
            logType,
            DocumentationLogScopeType.Hotel,
            canonicalId,
            $"{GetDisplayName(logType)} — Hotel: {canonicalName} / {canonicalId}");
    }

    public static DocumentationLogWorkbookContract CreatePmsHistoryContract(
        LogType logType,
        DocumentationLogPms pms)
    {
        ArgumentNullException.ThrowIfNull(pms);
        return CreatePmsHistoryContract(logType, pms.PmsName);
    }

    public static DocumentationLogWorkbookContract CreatePmsHistoryContract(
        LogType logType,
        string pmsName)
    {
        string canonicalName = RequireValue(pmsName, nameof(pmsName));

        return new DocumentationLogWorkbookContract(
            logType,
            DocumentationLogScopeType.PMS,
            canonicalName,
            $"{GetDisplayName(logType)} — PMS: {canonicalName}");
    }

    internal static void ValidateContract(
        DocumentationLogWorkbookContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        _ = GetHeaders(contract.LogType);

        if (!Enum.IsDefined(contract.ScopeType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(contract),
                contract.ScopeType,
                "Unsupported documentation-log workbook scope.");
        }

        if (contract.ScopeType == DocumentationLogScopeType.Running)
        {
            if (contract.ScopeId.Length != 0
                || !string.Equals(
                    contract.Title,
                    CreateRunningContract(contract.LogType).Title,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A Running workbook contract requires a blank scope ID and the canonical Running title.",
                    nameof(contract));
            }

            return;
        }

        _ = RequireValue(contract.ScopeId, nameof(contract));
        _ = RequireValue(contract.Title, nameof(contract));
    }

    internal static IReadOnlyList<double> GetColumnWidths(LogType logType)
    {
        _ = GetHeaders(logType);

        return logType switch
        {
            LogType.DebuggingLog =>
                [20, 20, 30, 15, 20, 45, 45, 45, 24, 45],
            LogType.ScriptEditingLog =>
                [20, 20, 30, 30, 45, 55, 24, 45],
            LogType.ScriptCreationLog =>
                [20, 20, 30, 30, 45, 55, 24, 45],
            _ => throw UnsupportedLogType(logType)
        };
    }

    internal static IReadOnlySet<int> GetTextColumnNumbers(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => new HashSet<int> { 2, 4 },
            LogType.ScriptEditingLog or LogType.ScriptCreationLog =>
                new HashSet<int> { 2, 3 },
            _ => throw UnsupportedLogType(logType)
        };
    }

    internal static IReadOnlySet<int> GetWrappedColumnNumbers(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => new HashSet<int> { 6, 7, 8, 10 },
            LogType.ScriptEditingLog => new HashSet<int> { 3, 5, 6, 8 },
            LogType.ScriptCreationLog => new HashSet<int> { 3, 5, 6, 8 },
            _ => throw UnsupportedLogType(logType)
        };
    }

    private static DocumentationLogFormFieldDefinition Required(
        string key,
        string label,
        bool isMultiline = false)
    {
        return new DocumentationLogFormFieldDefinition(
            key,
            label,
            IsRequired: true,
            isMultiline);
    }

    private static DocumentationLogFormFieldDefinition Optional(
        string key,
        string label,
        bool isMultiline = false)
    {
        return new DocumentationLogFormFieldDefinition(
            key,
            label,
            IsRequired: false,
            isMultiline);
    }

    private static string RequireValue(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank canonical value is required.",
                parameterName);
        }

        return value.Trim();
    }

    private static ArgumentOutOfRangeException UnsupportedLogType(LogType logType)
    {
        return new ArgumentOutOfRangeException(
            nameof(logType),
            logType,
            "Unsupported documentation log type.");
    }
}
