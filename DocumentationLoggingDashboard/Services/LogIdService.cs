using System.Globalization;
using System.Text.RegularExpressions;
using DocumentationLoggingDashboard.Models;

namespace DocumentationLoggingDashboard.Services;

/// <summary>
/// Generates sequential log IDs for daily log files.
/// </summary>
public sealed class LogIdService
{
    private readonly LogTemplateService logTemplateService;
    private readonly LogFileService logFileService;

    public LogIdService(LogTemplateService logTemplateService, LogFileService logFileService)
    {
        this.logTemplateService = logTemplateService;
        this.logFileService = logFileService;
    }

    public string GenerateNextLogId(LogType logType, DateTime dateTime)
    {
        string prefix = logTemplateService.GetLogIdPrefix(logType);
        string datePart = dateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        int nextSequence = GetNextSequenceNumber(logType, prefix, datePart, dateTime);

        return $"{prefix}-{datePart}-{nextSequence:000}";
    }

    private int GetNextSequenceNumber(LogType logType, string prefix, string datePart, DateTime dateTime)
    {
        string filePath = logFileService.GetDailyLogFilePath(logType, dateTime);

        if (!File.Exists(filePath))
        {
            return 1;
        }

        string idPattern = $@"\b{Regex.Escape(prefix)}-{datePart}-(\d{{3}})\b";
        int highestSequence = 0;

        foreach (Match match in Regex.Matches(File.ReadAllText(filePath), idPattern))
        {
            if (int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int sequence))
            {
                highestSequence = Math.Max(highestSequence, sequence);
            }
        }

        return highestSequence + 1;
    }
}
