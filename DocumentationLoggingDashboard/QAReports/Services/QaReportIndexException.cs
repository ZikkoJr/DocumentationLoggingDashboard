namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Identifies the focused failure category for the dedicated QA report index.
/// </summary>
public enum QaReportIndexErrorCategory
{
    Missing,
    ReadFailure,
    MalformedContent,
    SerializationFailure
}

/// <summary>
/// Represents a refusal to read, parse, or prepare the dedicated QA report index.
/// </summary>
public sealed class QaReportIndexException : Exception
{
    public QaReportIndexException(
        QaReportIndexErrorCategory category,
        string indexFilePath,
        string reason,
        Exception? innerException = null)
        : base(BuildMessage(indexFilePath, reason), innerException)
    {
        Category = category;
        IndexFilePath = indexFilePath;
        IndexFileName = Path.GetFileName(indexFilePath);
    }

    public QaReportIndexErrorCategory Category { get; }

    public string IndexFilePath { get; }

    public string IndexFileName { get; }

    private static string BuildMessage(string indexFilePath, string reason)
    {
        string fileName = Path.GetFileName(indexFilePath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "the QA report index";
        }

        return $"QA report index '{fileName}' could not be processed: {reason}";
    }
}
