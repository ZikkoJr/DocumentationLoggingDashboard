namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Captures broken-data statistics for populated values in one source field.
/// </summary>
public sealed class QaBrokenDataStatistic
{
    public string FieldId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int BrokenValueCount { get; set; }

    /// <summary>
    /// Gets or sets the number of applicable nonblank values used as the denominator.
    /// </summary>
    public int TotalApplicableNonblankValues { get; set; }

    /// <summary>
    /// Gets or sets the broken-data percentage on a 0 through 100 scale.
    /// </summary>
    public decimal BrokenDataPercentage { get; set; }

    public string? Explanation { get; set; }
}
