namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Represents a failure while explicitly initializing the QA storage structure.
/// </summary>
public sealed class QaStorageInitializationException : Exception
{
    public QaStorageInitializationException(
        string storagePath,
        string reason,
        Exception? innerException = null)
        : base($"QA storage at '{storagePath}' could not be initialized: {reason}", innerException)
    {
        StoragePath = storagePath;
    }

    public string StoragePath { get; }
}
