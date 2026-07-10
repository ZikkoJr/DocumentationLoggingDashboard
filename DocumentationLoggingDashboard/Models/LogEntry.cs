namespace DocumentationLoggingDashboard.Models;

/// <summary>
/// Represents a completed or previewed documentation log entry.
/// </summary>
public sealed class LogEntry
{
    public LogType LogType { get; set; }

    public string LogId { get; set; } = string.Empty;

    public DateTime DateTime { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public Dictionary<string, string> FieldValues { get; } = new(StringComparer.Ordinal);

    public string NotesFollowUp { get; set; } = string.Empty;
}
