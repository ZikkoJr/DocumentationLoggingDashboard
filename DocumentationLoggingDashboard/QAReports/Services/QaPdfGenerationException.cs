namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Represents an actionable failure while producing an in-memory QA PDF payload.
/// </summary>
public sealed class QaPdfGenerationException : Exception
{
    public QaPdfGenerationException(string message)
        : base(message)
    {
    }

    public QaPdfGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
