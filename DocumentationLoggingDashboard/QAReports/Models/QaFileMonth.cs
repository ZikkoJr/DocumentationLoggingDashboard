using System.Globalization;

namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Represents a file month using only a year and month.
/// </summary>
public sealed record QaFileMonth
{
    public QaFileMonth(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        }

        Year = year;
        Month = month;
    }

    /// <summary>
    /// Gets the four-digit calendar year for the file month.
    /// </summary>
    public int Year { get; }

    /// <summary>
    /// Gets the one-based calendar month.
    /// </summary>
    public int Month { get; }

    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Year:D4}-{Month:D2}");
    }
}
