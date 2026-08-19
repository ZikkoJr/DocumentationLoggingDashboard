using System.Text.Json;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Loads and atomically persists the filename-only Quick QA workbook preference.
/// </summary>
public sealed class QuickQaPreferencesService
{
    public const int CurrentSchemaVersion = 1;

    private const int MaximumSettingsFileBytes = 16 * 1024;
    private static readonly object SyncRoot = new();

    private readonly QaStoragePaths paths;
    private readonly QuickQaWorkbookFilenameService filenameService;

    public QuickQaPreferencesService(
        QaStoragePaths paths,
        QuickQaWorkbookFilenameService filenameService)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(filenameService);

        this.paths = paths;
        this.filenameService = filenameService;
    }

    /// <summary>
    /// Returns the remembered workbook leaf filename, or <see langword="null"/>
    /// when no settings file exists or the preference has been cleared.
    /// The remembered workbook is not required to still exist.
    /// </summary>
    public string? LoadLastUsedWorkbookFileName()
    {
        lock (SyncRoot)
        {
            if (!File.Exists(paths.QuickQaSettingsFilePath))
            {
                return null;
            }

            try
            {
                byte[] utf8Json = ReadSettingsSnapshot();
                return ParseSettings(utf8Json);
            }
            catch (QuickQaPreferencesException)
            {
                throw;
            }
            catch (JsonException exception)
            {
                throw CreateInvalidSettingsException("The settings file is not valid JSON.", exception);
            }
            catch (IOException exception)
            {
                throw CreateReadException(exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                throw CreateReadException(exception);
            }
        }
    }

    /// <summary>
    /// Atomically records an existing direct-child Surface QA workbook by leaf
    /// filename. Absolute paths and unsafe filenames are rejected.
    /// </summary>
    public void SaveLastUsedWorkbookFileName(string workbookFileName)
    {
        string canonicalFileName = filenameService.ValidateStoredWorkbookFileName(workbookFileName);
        string workbookPath = paths.ResolveSurfaceQaWorkbookPath(canonicalFileName);

        try
        {
            QuickQaPathSafety.EnsureNoReparsePoints(
                paths.QaReportsRootPath,
                workbookPath);
            // Excluding FileShare.Delete prevents a discovered workbook from
            // being renamed or removed during the atomic preference write.
            using FileStream workbookLease = new(
                workbookPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 1,
                FileOptions.RandomAccess);
            WriteSettings(canonicalFileName);
        }
        catch (QuickQaPreferencesException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or InvalidOperationException
                or NotSupportedException
                or System.Security.SecurityException)
        {
            throw CreateWorkbookUnavailableException(
                canonicalFileName,
                exception);
        }
    }

    /// <summary>
    /// Atomically clears the remembered filename while retaining a valid,
    /// versioned settings document.
    /// </summary>
    public void ClearLastUsedWorkbookFileName()
    {
        WriteSettings(lastUsedWorkbookFileName: null);
    }

    private byte[] ReadSettingsSnapshot()
    {
        using FileStream stream = new(
            paths.QuickQaSettingsFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.SequentialScan);

        if (stream.Length > MaximumSettingsFileBytes)
        {
            throw CreateInvalidSettingsException(
                $"The settings file exceeds the {MaximumSettingsFileBytes}-byte size limit.");
        }

        byte[] content = new byte[checked((int)stream.Length)];
        stream.ReadExactly(content);
        return content;
    }

    private string? ParseSettings(byte[] utf8Json)
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
            throw CreateInvalidSettingsException("The settings root must be a JSON object.");
        }

        bool foundSchemaVersion = false;
        bool foundWorkbookFileName = false;
        int schemaVersion = 0;
        string? workbookFileName = null;
        HashSet<string> propertyNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (!propertyNames.Add(property.Name))
            {
                throw CreateInvalidSettingsException(
                    $"The settings property '{property.Name}' is duplicated.");
            }

            if (string.Equals(property.Name, "schemaVersion", StringComparison.OrdinalIgnoreCase))
            {
                foundSchemaVersion = true;
                if (property.Value.ValueKind != JsonValueKind.Number
                    || !property.Value.TryGetInt32(out schemaVersion))
                {
                    throw CreateInvalidSettingsException("schemaVersion must be an integer.");
                }

                continue;
            }

            if (string.Equals(
                property.Name,
                "lastUsedWorkbookFileName",
                StringComparison.OrdinalIgnoreCase))
            {
                foundWorkbookFileName = true;
                if (property.Value.ValueKind == JsonValueKind.Null)
                {
                    workbookFileName = null;
                }
                else if (property.Value.ValueKind == JsonValueKind.String)
                {
                    workbookFileName = property.Value.GetString();
                }
                else
                {
                    throw CreateInvalidSettingsException(
                        "lastUsedWorkbookFileName must be a string or null.");
                }

                continue;
            }

            throw CreateInvalidSettingsException(
                $"The settings property '{property.Name}' is not supported by this schema.");
        }

        if (!foundSchemaVersion)
        {
            throw CreateInvalidSettingsException("The settings file is missing schemaVersion.");
        }

        if (schemaVersion != CurrentSchemaVersion)
        {
            throw CreateInvalidSettingsException(
                $"Settings schema version {schemaVersion} is unsupported; expected {CurrentSchemaVersion}.");
        }

        if (!foundWorkbookFileName)
        {
            throw CreateInvalidSettingsException(
                "The settings file is missing lastUsedWorkbookFileName.");
        }

        if (workbookFileName is null)
        {
            return null;
        }

        try
        {
            return filenameService.ValidateStoredWorkbookFileName(workbookFileName);
        }
        catch (ArgumentException exception)
        {
            throw CreateInvalidSettingsException(
                "lastUsedWorkbookFileName is not a safe .xlsx leaf filename.",
                exception);
        }
    }

    private void WriteSettings(string? lastUsedWorkbookFileName)
    {
        byte[] content = QaMetadataJson.SerializeToUtf8Bytes(
            new QuickQaSettingsDocument
            {
                SchemaVersion = CurrentSchemaVersion,
                LastUsedWorkbookFileName = lastUsedWorkbookFileName
            });

        lock (SyncRoot)
        {
            try
            {
                Directory.CreateDirectory(paths.MetadataRootPath);

                if (File.Exists(paths.QuickQaSettingsFilePath))
                {
                    QaAtomicFileWriter.Replace(paths.QuickQaSettingsFilePath, content);
                }
                else
                {
                    QaAtomicFileWriter.WriteNew(paths.QuickQaSettingsFilePath, content);
                }
            }
            catch (IOException exception)
            {
                throw CreateWriteException(exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                throw CreateWriteException(exception);
            }
        }
    }

    private QuickQaPreferencesException CreateInvalidSettingsException(
        string reason,
        Exception? innerException = null)
    {
        return new QuickQaPreferencesException(
            paths.QuickQaSettingsFilePath,
            reason,
            innerException);
    }

    private QuickQaPreferencesException CreateReadException(Exception innerException)
    {
        return new QuickQaPreferencesException(
            paths.QuickQaSettingsFilePath,
            "The Quick QA settings file could not be read.",
            innerException);
    }

    private QuickQaPreferencesException CreateWriteException(Exception innerException)
    {
        return new QuickQaPreferencesException(
            paths.QuickQaSettingsFilePath,
            "The Quick QA settings file could not be written atomically.",
            innerException);
    }

    private QuickQaPreferencesException CreateWorkbookUnavailableException(
        string workbookFileName,
        Exception innerException)
    {
        return new QuickQaPreferencesException(
            paths.QuickQaSettingsFilePath,
            $"The selected Surface QA workbook '{workbookFileName}' no longer exists or is unavailable, so the last-used preference was not saved.",
            innerException);
    }

    private sealed class QuickQaSettingsDocument
    {
        public required int SchemaVersion { get; init; }

        public string? LastUsedWorkbookFileName { get; init; }
    }
}

/// <summary>
/// Describes an unreadable, unsupported, or unwritable Quick QA preferences file.
/// </summary>
public sealed class QuickQaPreferencesException : Exception
{
    public QuickQaPreferencesException(
        string settingsFilePath,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        SettingsFilePath = settingsFilePath;
    }

    public string SettingsFilePath { get; }
}
