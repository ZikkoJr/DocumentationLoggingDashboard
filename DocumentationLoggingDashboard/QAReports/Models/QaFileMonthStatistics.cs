namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Captures arrival-date distribution relative to the selected file month.
/// </summary>
public sealed class QaFileMonthStatistics
{
    public int ArrivalDatesWithinFileMonth { get; set; }

    public int ArrivalDatesOutsideFileMonth { get; set; }

    /// <summary>
    /// Gets or sets the within-file-month percentage on a 0 through 100 scale.
    /// </summary>
    public decimal PercentageWithinFileMonth { get; set; }

    /// <summary>
    /// Gets or sets the outside-file-month percentage on a 0 through 100 scale.
    /// </summary>
    public decimal PercentageOutsideFileMonth { get; set; }

    public int ValidArrivalDateCount { get; set; }
}
