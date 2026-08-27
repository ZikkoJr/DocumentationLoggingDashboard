using DocumentationLoggingDashboard.Models;

namespace DocumentationLoggingDashboard.DocumentationLogs;

/// <summary>
/// Strictly validates user-provided and persisted Running workbook leaf names.
/// Invalid characters are rejected rather than repaired.
/// </summary>
public sealed class DocumentationLogWorkbookFilenameService
{
    public const string WorkbookExtension = ".xlsx";
    public const int MaximumWorkbookFileNameLength = 180;

    private static readonly char[] DirectorySeparators =
    [
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
        '/',
        '\\'
    ];

    private static readonly HashSet<string> ReservedDeviceNames = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "CLOCK$",
        "CONIN$",
        "CONOUT$",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "COM¹",
        "COM²",
        "COM³",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9",
        "LPT¹",
        "LPT²",
        "LPT³"
    };

    private readonly DocumentationLogStoragePaths paths;

    public DocumentationLogWorkbookFilenameService(
        DocumentationLogStoragePaths paths)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public string CreateSafeWorkbookFileName(string requestedFileName)
    {
        if (string.IsNullOrWhiteSpace(requestedFileName))
        {
            throw new ArgumentException(
                "A documentation log workbook filename is required.",
                nameof(requestedFileName));
        }

        EnsureExactTrimmedValue(requestedFileName, nameof(requestedFileName));
        EnsureNotAPath(requestedFileName, nameof(requestedFileName));

        if (requestedFileName.EndsWith(' ')
            || requestedFileName.EndsWith('.'))
        {
            throw new ArgumentException(
                "A workbook filename cannot end with a space or dot.",
                nameof(requestedFileName));
        }

        string extension = Path.GetExtension(requestedFileName);
        string candidate;

        if (extension.Length == 0)
        {
            candidate = requestedFileName + WorkbookExtension;
        }
        else if (extension.Equals(
                     WorkbookExtension,
                     StringComparison.OrdinalIgnoreCase))
        {
            candidate = requestedFileName[..^extension.Length]
                + WorkbookExtension;
        }
        else
        {
            throw new ArgumentException(
                $"Documentation log workbooks must use the {WorkbookExtension} extension.",
                nameof(requestedFileName));
        }

        return ValidateStoredWorkbookFileName(candidate);
    }

    public string ValidateStoredWorkbookFileName(string workbookFileName)
    {
        if (!IsSafeStoredWorkbookFileName(workbookFileName))
        {
            throw new ArgumentException(
                "The workbook filename must be a safe .xlsx leaf filename with no folders, traversal, invalid characters, or reserved Windows name.",
                nameof(workbookFileName));
        }

        return workbookFileName[..^WorkbookExtension.Length]
            + WorkbookExtension;
    }

    public string ResolveNewWorkbookPath(
        LogType logType,
        string requestedFileName)
    {
        _ = DocumentationLogWorkbookSchema.GetHeaders(logType);
        string fileName = CreateSafeWorkbookFileName(requestedFileName);
        string fullPath = paths.ResolveRunningWorkbookPath(logType, fileName);

        if (File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            throw new IOException(
                $"A file or folder named '{fileName}' already exists in the Running log folder.");
        }

        return fullPath;
    }

    public static bool IsSafeStoredWorkbookFileName(string? workbookFileName)
    {
        if (string.IsNullOrWhiteSpace(workbookFileName)
            || workbookFileName.Length > MaximumWorkbookFileNameLength
            || !string.Equals(
                workbookFileName,
                workbookFileName.Trim(),
                StringComparison.Ordinal)
            || workbookFileName.EndsWith(' ')
            || workbookFileName.EndsWith('.')
            || workbookFileName.StartsWith(".", StringComparison.Ordinal)
            || workbookFileName.StartsWith("~$", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            if (Path.IsPathRooted(workbookFileName)
                || workbookFileName.IndexOfAny(DirectorySeparators) >= 0
                || !string.Equals(
                    Path.GetFileName(workbookFileName),
                    workbookFileName,
                    StringComparison.Ordinal)
                || !string.Equals(
                    Path.GetExtension(workbookFileName),
                    WorkbookExtension,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        string stem = workbookFileName[..^WorkbookExtension.Length];
        if (stem.Length == 0
            || stem is "." or ".."
            || IsReservedDeviceName(stem)
            || stem.Contains(".documentation-log-", StringComparison.OrdinalIgnoreCase)
            || stem.Contains(".stage.", StringComparison.OrdinalIgnoreCase)
            || stem.Contains(".rollback.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !stem.Any(IsInvalidWindowsFileNameCharacter);
    }

    private static void EnsureExactTrimmedValue(
        string value,
        string parameterName)
    {
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A workbook filename cannot begin or end with whitespace.",
                parameterName);
        }
    }

    private static void EnsureNotAPath(string value, string parameterName)
    {
        bool isRooted;

        try
        {
            isRooted = Path.IsPathRooted(value);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException(
                "The workbook filename is invalid.",
                parameterName,
                exception);
        }

        if (isRooted || value.IndexOfAny(DirectorySeparators) >= 0)
        {
            throw new ArgumentException(
                "Enter a filename only; folders and path separators are not allowed.",
                parameterName);
        }

        if (value is "." or "..")
        {
            throw new ArgumentException(
                "Path traversal names are not allowed.",
                parameterName);
        }
    }

    private static bool IsInvalidWindowsFileNameCharacter(char character)
    {
        return char.IsControl(character)
            || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|'
                or '?' or '*';
    }

    private static bool IsReservedDeviceName(string stem)
    {
        string deviceCandidate = stem.Split('.')[0].TrimEnd(' ', '.');
        return ReservedDeviceNames.Contains(deviceCandidate);
    }
}
