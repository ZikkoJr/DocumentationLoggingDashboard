namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Captures multiword first-name and last-name statistics.
/// </summary>
public sealed class QaMultiwordNameStatistics
{
    public int MultiwordFirstNameCount { get; set; }

    /// <summary>
    /// Gets or sets the multiword First Name percentage on a 0 through 100 scale.
    /// </summary>
    public decimal MultiwordFirstNamePercentage { get; set; }

    public int MultiwordLastNameCount { get; set; }

    /// <summary>
    /// Gets or sets the multiword Last Name percentage on a 0 through 100 scale.
    /// </summary>
    public decimal MultiwordLastNamePercentage { get; set; }
}
