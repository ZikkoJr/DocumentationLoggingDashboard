namespace DocumentationLoggingDashboard.Models;

/// <summary>
/// Describes one field that appears on a documentation log form.
/// </summary>
public sealed class LogFieldDefinition
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public bool IsRequired { get; init; }

    public bool IsMultiline { get; init; }
}
