using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.QAReports.Models;
using System.Collections.ObjectModel;

namespace DocumentationLoggingDashboard.DocumentationLogs;

public enum DocumentationLogScopeType
{
    Running,
    Hotel,
    PMS
}

public static class DocumentationLogFieldKeys
{
    public const string HotelIds = "hotelIds";
    public const string ErrorShownOnTicket = "errorShownOnTicket";
    public const string RootCause = "rootCause";
    public const string FixApplied = "fixApplied";
    public const string ScriptName = "scriptName";
    public const string ReasonForEdit = "reasonForEdit";
    public const string ChangesMade = "changesMade";
    public const string ReasonForCreation = "reasonForCreation";
    public const string ScriptPurpose = "scriptPurpose";
    public const string CreatedBy = "createdBy";
    public const string NotesFollowUp = "notesFollowUp";
}

public sealed record DocumentationLogPms(
    string PmsName,
    string FolderName);

public sealed record DocumentationLogHotel(
    string HotelId,
    string HotelName,
    DocumentationLogPms Pms,
    string FolderName);

public sealed record DocumentationLogWorkbookContract(
    LogType LogType,
    DocumentationLogScopeType ScopeType,
    string ScopeId,
    string Title);

/// <summary>
/// One immutable logical documentation event. The same instance is serialized
/// to the selected Running workbook, every Hotel/PMS history workbook, and the
/// legacy-compatible index line.
/// </summary>
public sealed class DocumentationLogEvent
{
    private readonly IReadOnlyDictionary<string, string> fieldValues;

    public DocumentationLogEvent(
        LogType logType,
        string logId,
        DateTimeOffset timestamp,
        IEnumerable<DocumentationLogHotel> hotels,
        IReadOnlyDictionary<string, string> fieldValues)
    {
        LogType = logType;
        LogId = Require(logId, nameof(logId));
        Timestamp = timestamp;
        Hotels = Array.AsReadOnly(
            (hotels ?? throw new ArgumentNullException(nameof(hotels)))
                .ToArray());
        this.fieldValues = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(
                fieldValues ?? throw new ArgumentNullException(nameof(fieldValues)),
                StringComparer.Ordinal));
    }

    public LogType LogType { get; }

    public string LogId { get; }

    public DateTimeOffset Timestamp { get; }

    public IReadOnlyList<DocumentationLogHotel> Hotels { get; }

    public IReadOnlyDictionary<string, string> FieldValues => fieldValues;

    public string CanonicalHotelIds => string.Join(
        "; ",
        Hotels.Select(hotel => hotel.HotelId));

    public string GetValue(string key)
    {
        return fieldValues.TryGetValue(key, out string? value)
            ? value
            : string.Empty;
    }

    private static string Require(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A nonempty value is required.", parameterName);
        }

        return value.Trim();
    }
}

public sealed class DocumentationLogSaveRequest
{
    private readonly IReadOnlyDictionary<string, string> fieldValues;

    public DocumentationLogSaveRequest(
        LogType logType,
        string runningWorkbookFileName,
        IReadOnlyDictionary<string, string> fieldValues,
        QaHotelMetadata? selectedHotel = null,
        QaPmsMetadata? selectedPms = null,
        string? hotelIdsInput = null)
    {
        LogType = logType;
        RunningWorkbookFileName = string.IsNullOrWhiteSpace(runningWorkbookFileName)
            ? throw new ArgumentException(
                "A selected Running workbook filename is required.",
                nameof(runningWorkbookFileName))
            : runningWorkbookFileName;
        this.fieldValues = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(
                fieldValues ?? throw new ArgumentNullException(nameof(fieldValues)),
                StringComparer.Ordinal));
        SelectedHotel = selectedHotel is null
            ? null
            : new QaHotelMetadata
            {
                HotelId = selectedHotel.HotelId,
                HotelName = selectedHotel.HotelName,
                PmsName = selectedHotel.PmsName,
                FolderName = selectedHotel.FolderName
            };
        SelectedPms = selectedPms is null
            ? null
            : new QaPmsMetadata
            {
                PmsName = selectedPms.PmsName,
                FolderName = selectedPms.FolderName
            };
        HotelIdsInput = hotelIdsInput ?? string.Empty;
    }

    public LogType LogType { get; }

    public string RunningWorkbookFileName { get; }

    public IReadOnlyDictionary<string, string> FieldValues => fieldValues;

    public QaHotelMetadata? SelectedHotel { get; }

    public QaPmsMetadata? SelectedPms { get; }

    public string HotelIdsInput { get; }
}

public sealed class DocumentationLogSaveResult
{
    internal DocumentationLogSaveResult(
        DocumentationLogEvent logEvent,
        string runningWorkbookFileName,
        string runningWorkbookPath,
        IReadOnlyList<string> hotelHistoryPaths,
        IReadOnlyList<string> pmsHistoryPaths,
        string indexPath,
        string sequenceStatePath,
        string? cleanupWarning)
    {
        Event = logEvent;
        RunningWorkbookFileName = runningWorkbookFileName;
        RunningWorkbookPath = Path.GetFullPath(runningWorkbookPath);
        HotelHistoryPaths = Array.AsReadOnly(
            hotelHistoryPaths.Select(Path.GetFullPath).ToArray());
        PmsHistoryPaths = Array.AsReadOnly(
            pmsHistoryPaths.Select(Path.GetFullPath).ToArray());
        IndexPath = Path.GetFullPath(indexPath);
        SequenceStatePath = Path.GetFullPath(sequenceStatePath);
        CleanupWarning = cleanupWarning;
    }

    public DocumentationLogEvent Event { get; }

    public string RunningWorkbookFileName { get; }

    public string RunningWorkbookPath { get; }

    public IReadOnlyList<string> HotelHistoryPaths { get; }

    public IReadOnlyList<string> PmsHistoryPaths { get; }

    public string IndexPath { get; }

    public string SequenceStatePath { get; }

    public string? CleanupWarning { get; }
}

public sealed record DocumentationLogResolvedDraft(
    LogType LogType,
    IReadOnlyList<DocumentationLogHotel> Hotels,
    IReadOnlyList<DocumentationLogPms> UniquePmsSystems,
    IReadOnlyDictionary<string, string> FieldValues);
