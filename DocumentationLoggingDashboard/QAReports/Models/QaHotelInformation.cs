namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Captures the hotel and PMS information stored on a QA report snapshot.
/// </summary>
public sealed class QaHotelInformation
{
    public string HotelName { get; set; } = string.Empty;

    public string HotelId { get; set; } = string.Empty;

    public string PmsName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected file month. A draft may not have one yet.
    /// </summary>
    public QaFileMonth? FileMonth { get; set; }
}
