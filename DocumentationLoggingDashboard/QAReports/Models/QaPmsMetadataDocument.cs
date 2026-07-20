namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Represents the schema-versioned persistent PMS metadata document.
/// </summary>
public sealed class QaPmsMetadataDocument
{
    public const int CurrentSchemaVersion = 1;

    public int? SchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets the PMS records. Null remains distinguishable during validation.
    /// </summary>
    public List<QaPmsMetadata?>? PmsSystems { get; set; }
}
