using System.Text.Json;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.DocumentationLogs;

/// <summary>
/// Loads and atomically persists one independent Running-workbook preference
/// for each documentation-log type.
/// </summary>
public sealed class DocumentationLogWorkbookPreferencesService
{
    public const int CurrentSchemaVersion = 1;

    private const int MaximumSettingsFileBytes = 16 * 1024;
    private static readonly object SyncRoot = new();

    private readonly DocumentationLogStoragePaths paths;
    private readonly DocumentationLogWorkbookFilenameService filenameService;

    public DocumentationLogWorkbookPreferencesService(
        DocumentationLogStoragePaths paths)
        : this(
            paths,
            new DocumentationLogWorkbookFilenameService(paths))
    {
    }

    public DocumentationLogWorkbookPreferencesService(
        DocumentationLogStoragePaths paths,
        DocumentationLogWorkbookFilenameService filenameService)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.filenameService = filenameService
            ?? throw new ArgumentNullException(nameof(filenameService));
    }

    /// <summary>
    /// Returns the remembered safe leaf filename for <paramref name="logType"/>,
    /// or <see langword="null"/> when it has not been set. The referenced
    /// workbook is not required to still exist when preferences are loaded.
    /// </summary>
    public string? LoadLastUsedWorkbookFileName(LogType logType)
    {
        _ = DocumentationLogWorkbookSchema.GetHeaders(logType);

        lock (SyncRoot)
        {
            DocumentationLogWorkbookSettings settings = ReadSettingsOrDefault();
            return settings.Get(logType);
        }
    }

    /// <summary>
    /// Atomically records an existing direct-child Running workbook for one
    /// log type without altering either of the other two preferences.
    /// </summary>
    public void SaveLastUsedWorkbookFileName(
        LogType logType,
        string workbookFileName)
    {
        _ = DocumentationLogWorkbookSchema.GetHeaders(logType);
        string canonicalFileName = filenameService
            .ValidateStoredWorkbookFileName(workbookFileName);
        string workbookPath = paths.ResolveRunningWorkbookPath(
            logType,
            canonicalFileName);

        try
        {
            // Excluding FileShare.Delete keeps the workbook present while the
            // preference document is read, merged, and atomically replaced.
            using FileStream workbookLease = new(
                workbookPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 1,
                FileOptions.RandomAccess);

            lock (SyncRoot)
            {
                DocumentationLogWorkbookSettings current =
                    ReadSettingsOrDefault();
                WriteSettings(current.With(logType, canonicalFileName));
            }
        }
        catch (DocumentationLogWorkbookPreferencesException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw new DocumentationLogWorkbookPreferencesException(
                paths.SettingsFilePath,
                $"The selected Running workbook '{canonicalFileName}' no longer exists or is unavailable, so its preference was not saved.",
                exception);
        }
    }

    /// <summary>
    /// Atomically clears one log type's preference while preserving the other
    /// two leaves in the versioned settings document.
    /// </summary>
    public void ClearLastUsedWorkbookFileName(LogType logType)
    {
        _ = DocumentationLogWorkbookSchema.GetHeaders(logType);

        lock (SyncRoot)
        {
            DocumentationLogWorkbookSettings current = ReadSettingsOrDefault();
            WriteSettings(current.With(logType, workbookFileName: null));
        }
    }

    private DocumentationLogWorkbookSettings ReadSettingsOrDefault()
    {
        if (!File.Exists(paths.SettingsFilePath))
        {
            return DocumentationLogWorkbookSettings.Empty;
        }

        try
        {
            using FileStream stream = new(
                paths.SettingsFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.SequentialScan);

            if (stream.Length > MaximumSettingsFileBytes)
            {
                throw InvalidSettings(
                    $"The settings file exceeds the {MaximumSettingsFileBytes}-byte size limit.");
            }

            byte[] content = new byte[checked((int)stream.Length)];
            stream.ReadExactly(content);
            return ParseSettings(content);
        }
        catch (DocumentationLogWorkbookPreferencesException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw InvalidSettings(
                "The documentation-log settings file is not valid JSON.",
                exception);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw new DocumentationLogWorkbookPreferencesException(
                paths.SettingsFilePath,
                "The documentation-log settings file could not be read.",
                exception);
        }
    }

    private DocumentationLogWorkbookSettings ParseSettings(
        ReadOnlyMemory<byte> utf8Json)
    {
        using JsonDocument document = JsonDocument.Parse(
            utf8Json,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 8
            });

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw InvalidSettings("The settings root must be a JSON object.");
        }

        int? schemaVersion = null;
        bool foundDebugging = false;
        bool foundEditing = false;
        bool foundCreation = false;
        string? debugging = null;
        string? editing = null;
        string? creation = null;
        HashSet<string> propertyNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (!propertyNames.Add(property.Name))
            {
                throw InvalidSettings(
                    $"The settings property '{property.Name}' is duplicated.");
            }

            if (property.Name.Equals(
                    "schemaVersion",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (property.Value.ValueKind != JsonValueKind.Number
                    || !property.Value.TryGetInt32(out int parsedVersion))
                {
                    throw InvalidSettings("schemaVersion must be an integer.");
                }

                schemaVersion = parsedVersion;
                continue;
            }

            if (property.Name.Equals(
                    "debuggingLogWorkbookFileName",
                    StringComparison.OrdinalIgnoreCase))
            {
                foundDebugging = true;
                debugging = ParseWorkbookLeaf(property);
                continue;
            }

            if (property.Name.Equals(
                    "scriptEditingLogWorkbookFileName",
                    StringComparison.OrdinalIgnoreCase))
            {
                foundEditing = true;
                editing = ParseWorkbookLeaf(property);
                continue;
            }

            if (property.Name.Equals(
                    "scriptCreationLogWorkbookFileName",
                    StringComparison.OrdinalIgnoreCase))
            {
                foundCreation = true;
                creation = ParseWorkbookLeaf(property);
                continue;
            }

            throw InvalidSettings(
                $"The settings property '{property.Name}' is not supported by this schema.");
        }

        if (schemaVersion is null)
        {
            throw InvalidSettings("The settings file is missing schemaVersion.");
        }

        if (schemaVersion != CurrentSchemaVersion)
        {
            throw InvalidSettings(
                $"Settings schema version {schemaVersion} is unsupported; expected {CurrentSchemaVersion}.");
        }

        if (!foundDebugging || !foundEditing || !foundCreation)
        {
            throw InvalidSettings(
                "The settings file must contain one workbook filename leaf for each documentation-log type.");
        }

        return new DocumentationLogWorkbookSettings(
            ValidatePersistedLeaf(debugging),
            ValidatePersistedLeaf(editing),
            ValidatePersistedLeaf(creation));
    }

    private string? ParseWorkbookLeaf(JsonProperty property)
    {
        if (property.Value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.Value.ValueKind != JsonValueKind.String)
        {
            throw InvalidSettings(
                $"{property.Name} must be a string or null.");
        }

        return property.Value.GetString();
    }

    private string? ValidatePersistedLeaf(string? workbookFileName)
    {
        if (workbookFileName is null)
        {
            return null;
        }

        try
        {
            return filenameService.ValidateStoredWorkbookFileName(
                workbookFileName);
        }
        catch (ArgumentException exception)
        {
            throw InvalidSettings(
                "A saved workbook preference is not a safe .xlsx leaf filename.",
                exception);
        }
    }

    private void WriteSettings(DocumentationLogWorkbookSettings settings)
    {
        try
        {
            Directory.CreateDirectory(paths.IndexRootPath);
            byte[] content = QaMetadataJson.SerializeToUtf8Bytes(
                new DocumentationLogWorkbookSettingsDocument
                {
                    SchemaVersion = CurrentSchemaVersion,
                    DebuggingLogWorkbookFileName = settings.Debugging,
                    ScriptEditingLogWorkbookFileName = settings.Editing,
                    ScriptCreationLogWorkbookFileName = settings.Creation
                });

            if (File.Exists(paths.SettingsFilePath))
            {
                QaAtomicFileWriter.Replace(paths.SettingsFilePath, content);
            }
            else
            {
                QaAtomicFileWriter.WriteNew(paths.SettingsFilePath, content);
            }
        }
        catch (DocumentationLogWorkbookPreferencesException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw new DocumentationLogWorkbookPreferencesException(
                paths.SettingsFilePath,
                "The documentation-log settings file could not be written atomically.",
                exception);
        }
    }

    private DocumentationLogWorkbookPreferencesException InvalidSettings(
        string message,
        Exception? innerException = null)
    {
        return new DocumentationLogWorkbookPreferencesException(
            paths.SettingsFilePath,
            message,
            innerException);
    }

    private static bool IsFileSystemException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException;
    }

    private sealed record DocumentationLogWorkbookSettings(
        string? Debugging,
        string? Editing,
        string? Creation)
    {
        internal static DocumentationLogWorkbookSettings Empty { get; } =
            new(null, null, null);

        internal string? Get(LogType logType)
        {
            return logType switch
            {
                LogType.DebuggingLog => Debugging,
                LogType.ScriptEditingLog => Editing,
                LogType.ScriptCreationLog => Creation,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(logType),
                    logType,
                    "Unsupported documentation log type.")
            };
        }

        internal DocumentationLogWorkbookSettings With(
            LogType logType,
            string? workbookFileName)
        {
            return logType switch
            {
                LogType.DebuggingLog => this with
                {
                    Debugging = workbookFileName
                },
                LogType.ScriptEditingLog => this with
                {
                    Editing = workbookFileName
                },
                LogType.ScriptCreationLog => this with
                {
                    Creation = workbookFileName
                },
                _ => throw new ArgumentOutOfRangeException(
                    nameof(logType),
                    logType,
                    "Unsupported documentation log type.")
            };
        }
    }

    private sealed class DocumentationLogWorkbookSettingsDocument
    {
        public required int SchemaVersion { get; init; }

        public string? DebuggingLogWorkbookFileName { get; init; }

        public string? ScriptEditingLogWorkbookFileName { get; init; }

        public string? ScriptCreationLogWorkbookFileName { get; init; }
    }
}

public sealed class DocumentationLogWorkbookPreferencesException : Exception
{
    public DocumentationLogWorkbookPreferencesException(
        string settingsFilePath,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        SettingsFilePath = Path.GetFullPath(settingsFilePath);
    }

    public string SettingsFilePath { get; }
}
