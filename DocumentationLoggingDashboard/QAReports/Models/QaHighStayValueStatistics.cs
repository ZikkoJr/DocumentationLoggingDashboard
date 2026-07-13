namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Captures Stay Value statistics for values strictly above 10,000.
/// </summary>
public sealed class QaHighStayValueStatistics
{
    public int StayValuesAboveTenThousandCount { get; set; }

    /// <summary>
    /// Gets or sets the above-10,000 percentage on a 0 through 100 scale.
    /// </summary>
    public decimal StayValuesAboveTenThousandPercentage { get; set; }

    public bool AreHighValuesExpected { get; set; }

    public string? Explanation { get; set; }
}
