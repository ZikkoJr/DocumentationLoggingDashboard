namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Represents the top-level Phase 2 QA report contract.
/// </summary>
public sealed class QaReport
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string ReportId { get; set; } = string.Empty;

    public QaHotelInformation HotelInformation { get; set; } = new();

    /// <summary>
    /// Gets or sets the date on which the QA review is performed.
    /// </summary>
    public DateOnly QaDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public string? CreatedBy { get; set; }

    public string? OriginalFileName { get; set; }

    public QaFileCharacteristics FileCharacteristics { get; set; } = new();

    public List<QaCheckResult> ChecklistResults { get; } = [];

    public QaStatistics Statistics { get; set; } = new();

    public List<QaFinding> Findings { get; } = [];

    public QaReportStatus? ReportStatus { get; set; }

    public string? GeneralNotes { get; set; }
}
