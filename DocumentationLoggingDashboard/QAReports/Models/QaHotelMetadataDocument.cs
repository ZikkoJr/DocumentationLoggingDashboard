namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Represents the schema-versioned persistent hotel metadata document.
/// </summary>
public sealed class QaHotelMetadataDocument
{
    public const int CurrentSchemaVersion = 1;

    public int? SchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets the hotel records. Null remains distinguishable during validation.
    /// </summary>
    public List<QaHotelMetadata?>? Hotels { get; set; }
}
