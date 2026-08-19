namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Validates and normalizes user-provided Surface QA workbook file names.
/// </summary>
public sealed class QuickQaWorkbookFilenameService
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

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
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

    private readonly QaStoragePaths paths;

    public QuickQaWorkbookFilenameService(QaStoragePaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        this.paths = paths;
    }

    /// <summary>
    /// Converts a user-entered workbook name to a safe <c>.xlsx</c> leaf name.
    /// Invalid Windows filename characters are replaced with underscores; path
    /// separators, path traversal, rooted paths, other extensions, and reserved
    /// Windows device names are rejected.
    /// </summary>
    public string CreateSafeWorkbookFileName(string requestedFileName)
    {
        if (string.IsNullOrWhiteSpace(requestedFileName))
        {
            throw new ArgumentException("A Surface QA workbook filename is required.", nameof(requestedFileName));
        }

        string trimmed = requestedFileName.Trim();
        EnsureNotAPath(trimmed, nameof(requestedFileName));

        // Windows ignores terminal spaces and dots. Remove them before deciding
        // whether the user supplied an extension so the result is deterministic.
        trimmed = trimmed.TrimEnd(' ', '.');
        if (trimmed.Length == 0 || trimmed is "." or "..")
        {
            throw new ArgumentException("The workbook filename must contain a name.", nameof(requestedFileName));
        }

        string extension = Path.GetExtension(trimmed);
        string stem;
        if (extension.Length == 0)
        {
            stem = trimmed;
        }
        else if (string.Equals(extension, WorkbookExtension, StringComparison.OrdinalIgnoreCase))
        {
            stem = trimmed[..^extension.Length];
        }
        else
        {
            throw new ArgumentException(
                $"Surface QA workbooks must use the {WorkbookExtension} extension.",
                nameof(requestedFileName));
        }

        stem = stem.Trim().TrimEnd(' ', '.');
        if (stem.Length == 0 || stem is "." or "..")
        {
            throw new ArgumentException("The workbook filename must contain a name.", nameof(requestedFileName));
        }

        string safeStem = ReplaceInvalidWindowsFileNameCharacters(stem)
            .Trim()
            .TrimEnd(' ', '.');

        if (safeStem.Length == 0 || safeStem is "." or "..")
        {
            throw new ArgumentException(
                "The workbook filename contains no usable characters.",
                nameof(requestedFileName));
        }

        EnsureNotReservedDeviceName(safeStem, nameof(requestedFileName));

        string result = safeStem + WorkbookExtension;
        if (result.Length > MaximumWorkbookFileNameLength)
        {
            throw new ArgumentException(
                $"The workbook filename cannot exceed {MaximumWorkbookFileNameLength} characters.",
                nameof(requestedFileName));
        }

        return result;
    }

    /// <summary>
    /// Validates a filename received from persisted state or workbook discovery.
    /// Unlike <see cref="CreateSafeWorkbookFileName"/>, this method does not repair
    /// invalid input. It only normalizes the extension casing.
    /// </summary>
    public string ValidateStoredWorkbookFileName(string workbookFileName)
    {
        if (!IsSafeStoredWorkbookFileName(workbookFileName))
        {
            throw new ArgumentException(
                "The stored Surface QA workbook filename is not a safe .xlsx leaf filename.",
                nameof(workbookFileName));
        }

        return workbookFileName[..^WorkbookExtension.Length] + WorkbookExtension;
    }

    /// <summary>
    /// Returns a safe direct-child path for a new workbook and fails if a file or
    /// directory already occupies that path. The caller remains responsible for
    /// using create-new semantics when it opens the file.
    /// </summary>
    public string ResolveNewWorkbookPath(string requestedFileName)
    {
        string fileName = CreateSafeWorkbookFileName(requestedFileName);
        string fullPath = paths.ResolveSurfaceQaWorkbookPath(fileName);

        if (File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            throw new IOException(
                $"A file or folder named '{fileName}' already exists in the Surface QA folder.");
        }

        return fullPath;
    }

    internal static bool IsSafeStoredWorkbookFileName(string? workbookFileName)
    {
        if (string.IsNullOrWhiteSpace(workbookFileName)
            || workbookFileName.Length > MaximumWorkbookFileNameLength
            || !string.Equals(workbookFileName, workbookFileName.Trim(), StringComparison.Ordinal)
            || workbookFileName.EndsWith(' ')
            || workbookFileName.EndsWith('.'))
        {
            return false;
        }

        try
        {
            if (Path.IsPathRooted(workbookFileName)
                || workbookFileName.IndexOfAny(DirectorySeparators) >= 0
                || !string.Equals(Path.GetFileName(workbookFileName), workbookFileName, StringComparison.Ordinal)
                || !string.Equals(Path.GetExtension(workbookFileName), WorkbookExtension, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        string stem = workbookFileName[..^WorkbookExtension.Length];
        if (stem.Length == 0 || stem is "." or ".." || IsReservedDeviceName(stem))
        {
            return false;
        }

        foreach (char character in stem)
        {
            if (IsInvalidWindowsFileNameCharacter(character))
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureNotAPath(string candidate, string parameterName)
    {
        bool isRooted;
        try
        {
            isRooted = Path.IsPathRooted(candidate);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException("The workbook filename is invalid.", parameterName, exception);
        }

        if (isRooted || candidate.IndexOfAny(DirectorySeparators) >= 0)
        {
            throw new ArgumentException(
                "Enter a filename only; folders and path separators are not allowed.",
                parameterName);
        }

        if (candidate is "." or "..")
        {
            throw new ArgumentException("Path traversal names are not allowed.", parameterName);
        }
    }

    private static string ReplaceInvalidWindowsFileNameCharacters(string value)
    {
        char[] characters = value.ToCharArray();
        bool previousWasReplacement = false;
        int destination = 0;

        foreach (char character in characters)
        {
            bool replace = IsInvalidWindowsFileNameCharacter(character);
            if (replace && previousWasReplacement)
            {
                continue;
            }

            characters[destination++] = replace ? '_' : character;
            previousWasReplacement = replace;
        }

        return new string(characters, 0, destination);
    }

    private static bool IsInvalidWindowsFileNameCharacter(char character)
    {
        return char.IsControl(character)
            || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*';
    }

    private static void EnsureNotReservedDeviceName(string stem, string parameterName)
    {
        if (IsReservedDeviceName(stem))
        {
            throw new ArgumentException(
                $"'{stem}' is a reserved Windows device name and cannot be used as a workbook filename.",
                parameterName);
        }
    }

    private static bool IsReservedDeviceName(string stem)
    {
        string deviceCandidate = stem.Split('.')[0].TrimEnd(' ', '.');
        return ReservedDeviceNames.Contains(deviceCandidate);
    }
}
