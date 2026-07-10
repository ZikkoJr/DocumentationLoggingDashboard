using System.Text;
using DocumentationLoggingDashboard.Models;

namespace DocumentationLoggingDashboard.Services;

/// <summary>
/// Resolves documentation log paths and saves completed entries to daily text files.
/// </summary>
public sealed class LogFileService
{
    private const string IndexFolderName = "Index";

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly SettingsService settingsService;
    private readonly LogTemplateService logTemplateService;

    public LogFileService(SettingsService settingsService, LogTemplateService logTemplateService)
    {
        this.settingsService = settingsService;
        this.logTemplateService = logTemplateService;
    }

    public string GetDocumentationRootFolder()
    {
        return settingsService.GetDocumentationRootFolder();
    }

    public string EnsureDocumentationRootFolder()
    {
        return EnsureDocumentationFolderStructure();
    }

    public string EnsureDocumentationFolderStructure()
    {
        string rootFolder = GetDocumentationRootFolder();
        EnsureDocumentationFolderStructure(rootFolder);

        return rootFolder;
    }

    public void EnsureDocumentationFolderStructure(string rootFolder)
    {
        if (string.IsNullOrWhiteSpace(rootFolder))
        {
            throw new ArgumentException("A documentation root folder is required.", nameof(rootFolder));
        }

        Directory.CreateDirectory(rootFolder);

        foreach (LogType logType in logTemplateService.GetSupportedLogTypes())
        {
            Directory.CreateDirectory(GetLogFolderPath(rootFolder, logType));
        }

        Directory.CreateDirectory(GetIndexFolderPath(rootFolder));
    }

    public string GetLogFolderPath(LogType logType)
    {
        return GetLogFolderPath(GetDocumentationRootFolder(), logType);
    }

    public string EnsureLogFolder(LogType logType)
    {
        EnsureDocumentationFolderStructure();

        string logFolder = GetLogFolderPath(logType);

        return logFolder;
    }

    public string GetIndexFolderPath()
    {
        return GetIndexFolderPath(GetDocumentationRootFolder());
    }

    public string GetDailyLogFilePath(LogType logType, DateTime date)
    {
        return Path.Combine(GetLogFolderPath(logType), logTemplateService.GetDailyFileName(logType, date));
    }

    public string SaveEntry(LogEntry entry, string formattedEntry)
    {
        EnsureDocumentationFolderStructure();

        string filePath = GetDailyLogFilePath(entry.LogType, entry.DateTime);
        bool fileAlreadyHasEntries = File.Exists(filePath) && new FileInfo(filePath).Length > 0;

        using StreamWriter writer = new(filePath, append: true, Utf8NoBom);

        if (fileAlreadyHasEntries)
        {
            writer.WriteLine();
        }

        writer.Write(formattedEntry.TrimEnd());
        writer.WriteLine();

        return filePath;
    }

    private string GetLogFolderPath(string rootFolder, LogType logType)
    {
        return Path.Combine(rootFolder, logTemplateService.GetFolderName(logType));
    }

    private static string GetIndexFolderPath(string rootFolder)
    {
        return Path.Combine(rootFolder, IndexFolderName);
    }
}
