namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Represents one persistent PMS metadata record.
/// </summary>
public sealed class QaPmsMetadata
{
    public string PmsName { get; set; } = string.Empty;

    public string FolderName { get; set; } = string.Empty;
}
