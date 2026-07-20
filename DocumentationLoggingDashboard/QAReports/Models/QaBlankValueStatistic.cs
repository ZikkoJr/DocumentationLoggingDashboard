namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Captures blank-value statistics for one source field.
/// </summary>
public sealed class QaBlankValueStatistic
{
    public string FieldId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int BlankCount { get; set; }

    public int TotalApplicableRows { get; set; }

    /// <summary>
    /// Gets or sets whether the denominator follows the report's current
    /// Total Data Rows value. The initializer keeps older serialized rows that
    /// do not contain this property in the safe automatic mode.
    /// </summary>
    public bool UseAutomaticTotalApplicableRows { get; set; } = true;

    /// <summary>
    /// Gets or sets the blank percentage on a 0 through 100 scale.
    /// </summary>
    public decimal BlankPercentage { get; set; }
}
