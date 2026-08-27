using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.DocumentationLogs;

/// <summary>
/// Resolves the fixed storage layout used by legacy and Excel documentation logs.
/// Hotel and PMS history directories always come from canonical QA metadata paths.
/// </summary>
public sealed class DocumentationLogStoragePaths
{
    private readonly QaStoragePaths qaStoragePaths;

    public DocumentationLogStoragePaths(string documentationRootPath)
        : this(new QaStoragePaths(documentationRootPath))
    {
    }

    public DocumentationLogStoragePaths(QaStoragePaths qaStoragePaths)
    {
        this.qaStoragePaths = qaStoragePaths
            ?? throw new ArgumentNullException(nameof(qaStoragePaths));

        DocumentationRootPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(qaStoragePaths.DocumentationRootPath));
        IndexRootPath = ResolveFixedDescendant(DocumentationRootPath, "Index");
        LogIndexFilePath = ResolveFixedDescendant(IndexRootPath, "LogIndex.txt");
        SettingsFilePath = ResolveFixedDescendant(
            IndexRootPath,
            "documentation-log-settings.json");
        SequenceStateFilePath = ResolveFixedDescendant(
            IndexRootPath,
            "documentation-log-sequences.json");
        HotelsMetadataFilePath = Path.GetFullPath(
            qaStoragePaths.HotelsMetadataFilePath);
        PmsSystemsMetadataFilePath = Path.GetFullPath(
            qaStoragePaths.PmsSystemsMetadataFilePath);
    }

    public string DocumentationRootPath { get; }

    public string IndexRootPath { get; }

    public string LogIndexFilePath { get; }

    public string SettingsFilePath { get; }

    public string SequenceStateFilePath { get; }

    public string HotelsMetadataFilePath { get; }

    public string PmsSystemsMetadataFilePath { get; }

    public string GetLegacyLogDirectory(LogType logType)
    {
        return ResolveFixedDescendant(
            DocumentationRootPath,
            GetLegacyFolderName(logType));
    }

    public string GetRunningDirectory(LogType logType)
    {
        return ResolveFixedDescendant(GetLegacyLogDirectory(logType), "Running");
    }

    public string GetLegacyDailyLogFilePath(LogType logType, DateTime date)
    {
        string fileName = $"{date:yyyy-MM-dd}_{GetLegacyFileNameSuffix(logType)}.txt";
        return ResolveStrictDirectChildFile(
            GetLegacyLogDirectory(logType),
            fileName,
            nameof(logType));
    }

    public string ResolveRunningWorkbookPath(
        LogType logType,
        string workbookFileName)
    {
        return ResolveStrictDirectChildFile(
            GetRunningDirectory(logType),
            workbookFileName,
            nameof(workbookFileName));
    }

    public string ResolveHotelHistoryPath(
        LogType logType,
        string hotelFolderName)
    {
        string hotelDirectory = qaStoragePaths.ResolveHotelDirectory(
            hotelFolderName);
        return ResolveStrictDirectChildFile(
            hotelDirectory,
            DocumentationLogWorkbookSchema.GetHistoryWorkbookFileName(logType),
            nameof(hotelFolderName));
    }

    public string ResolvePmsHistoryPath(
        LogType logType,
        string pmsFolderName)
    {
        string pmsDirectory = qaStoragePaths.ResolvePmsDirectory(pmsFolderName);
        return ResolveStrictDirectChildFile(
            pmsDirectory,
            DocumentationLogWorkbookSchema.GetHistoryWorkbookFileName(logType),
            nameof(pmsFolderName));
    }

    public void EnsureBaseDirectories()
    {
        Directory.CreateDirectory(DocumentationRootPath);

        foreach (LogType logType in DocumentationLogWorkbookSchema.SupportedLogTypes)
        {
            Directory.CreateDirectory(GetLegacyLogDirectory(logType));
            Directory.CreateDirectory(GetRunningDirectory(logType));
        }

        Directory.CreateDirectory(IndexRootPath);
    }

    private static string GetLegacyFolderName(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => "DebuggingLogs",
            LogType.ScriptEditingLog => "ScriptEditingLogs",
            LogType.ScriptCreationLog => "ScriptCreationLogs",
            _ => throw new ArgumentOutOfRangeException(
                nameof(logType),
                logType,
                "Unsupported documentation log type.")
        };
    }

    private static string GetLegacyFileNameSuffix(LogType logType)
    {
        return logType switch
        {
            LogType.DebuggingLog => "DebuggingLog",
            LogType.ScriptEditingLog => "ScriptEditingLog",
            LogType.ScriptCreationLog => "ScriptCreationLog",
            _ => throw new ArgumentOutOfRangeException(
                nameof(logType),
                logType,
                "Unsupported documentation log type.")
        };
    }

    private static string ResolveFixedDescendant(string parentPath, string childName)
    {
        string normalizedParent = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(parentPath));
        string candidate = Path.GetFullPath(Path.Combine(normalizedParent, childName));

        if (!IsDescendant(normalizedParent, candidate))
        {
            throw new InvalidOperationException(
                "A fixed documentation-log path escaped its assigned parent path.");
        }

        return candidate;
    }

    private static string ResolveStrictDirectChildFile(
        string parentDirectory,
        string fileName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || Path.IsPathRooted(fileName)
            || fileName.IndexOf(Path.DirectorySeparatorChar) >= 0
            || fileName.IndexOf(Path.AltDirectorySeparatorChar) >= 0
            || !string.Equals(
                Path.GetFileName(fileName),
                fileName,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The filename must be a direct child leaf with no path components.",
                parameterName);
        }

        string normalizedParent = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(parentDirectory));
        string candidate = Path.GetFullPath(Path.Combine(normalizedParent, fileName));
        string? candidateParent = Path.GetDirectoryName(candidate);

        if (candidateParent is null
            || !string.Equals(
                Path.TrimEndingDirectorySeparator(candidateParent),
                normalizedParent,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The resolved file must remain a direct child of its assigned directory.",
                parameterName);
        }

        return candidate;
    }

    private static bool IsDescendant(string parentPath, string candidatePath)
    {
        string normalizedParent = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(parentPath));
        string normalizedCandidate = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(candidatePath));
        string parentPrefix = normalizedParent + Path.DirectorySeparatorChar;

        return normalizedCandidate.StartsWith(
            parentPrefix,
            StringComparison.OrdinalIgnoreCase);
    }
}
