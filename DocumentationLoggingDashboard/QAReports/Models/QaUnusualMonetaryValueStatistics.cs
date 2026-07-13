namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Captures manually identified unusual monetary values for one monetary field.
/// </summary>
public sealed class QaUnusualMonetaryValueStatistics
{
    public bool HasUnusualValues { get; set; }

    public int UnusualValueCount { get; set; }

    /// <summary>
    /// Gets or sets the unusual-value percentage on a 0 through 100 scale.
    /// </summary>
    public decimal UnusualValuePercentage { get; set; }

    public string? Explanation { get; set; }
}
