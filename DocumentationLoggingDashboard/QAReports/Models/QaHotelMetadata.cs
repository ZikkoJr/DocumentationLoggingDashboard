namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Represents one persistent hotel metadata record.
/// </summary>
public sealed class QaHotelMetadata
{
    public string HotelId { get; set; } = string.Empty;

    public string HotelName { get; set; } = string.Empty;

    public string PmsName { get; set; } = string.Empty;

    public string FolderName { get; set; } = string.Empty;
}
