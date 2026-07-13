namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Captures database import and comparison statistics.
/// </summary>
public sealed class QaDatabaseStatistics
{
    public int ImportedRecordCount { get; set; }

    public int RejectedRecordCount { get; set; }

    public int RecordsWithMissingRequiredDatabaseValues { get; set; }

    /// <summary>
    /// Gets or sets raw data rows minus imported database records.
    /// </summary>
    public int RawMinusImportedRecordCountDifference { get; set; }
}
