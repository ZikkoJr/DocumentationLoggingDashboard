using System.Globalization;
using System.Text;
using System.Text.Json;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Loads, validates, saves, and explicitly recovers persistent QA hotel and PMS metadata.
/// </summary>
public sealed class QaMetadataService
{
    private static readonly StringComparer MetadataComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly object MetadataMutationLock = new();

    private readonly QaStoragePaths paths;
    private readonly QaFolderNameSanitizer folderNameSanitizer;

    public QaMetadataService(QaStoragePaths paths, QaFolderNameSanitizer folderNameSanitizer)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.folderNameSanitizer = folderNameSanitizer
            ?? throw new ArgumentNullException(nameof(folderNameSanitizer));
    }

    /// <summary>
    /// Rereads and returns a defensive snapshot of all persisted PMS metadata.
    /// </summary>
    public IReadOnlyList<QaPmsMetadata> LoadPmsSystems()
    {
        QaPmsMetadataDocument document = LoadPmsDocument();

        return document.PmsSystems!
            .Select(record => Clone(record!))
            .ToArray();
    }

    /// <summary>
    /// Rereads and returns a defensive snapshot of all persisted hotel metadata.
    /// </summary>
    public IReadOnlyList<QaHotelMetadata> LoadHotels()
    {
        QaPmsMetadataDocument pmsDocument = LoadPmsDocument();
        QaHotelMetadataDocument hotelDocument = LoadHotelDocument(pmsDocument);

        return hotelDocument.Hotels!
            .Select(record => Clone(record!))
            .ToArray();
    }

    /// <summary>
    /// Adds a unique PMS record and ensures its category directory exists.
    /// </summary>
    public QaPmsMetadata AddPmsSystem(string pmsName)
    {
        lock (MetadataMutationLock)
        {
            return AddPmsSystemCore(pmsName);
        }
    }

    private QaPmsMetadata AddPmsSystemCore(string pmsName)
    {
        QaPmsMetadataDocument document = LoadPmsDocument();
        string normalizedPmsName = NormalizeRequiredInput(pmsName, nameof(pmsName), "A PMS name is required.");

        if (document.PmsSystems!.Any(
                record => MetadataComparer.Equals(record!.PmsName.Trim(), normalizedPmsName)))
        {
            throw new QaDuplicateMetadataException(
                QaMetadataFileKind.PmsSystems,
                paths.PmsSystemsMetadataFilePath,
                "A PMS record with the same trimmed name already exists.");
        }

        string folderName = folderNameSanitizer.SanitizeLeafName(normalizedPmsName);

        if (document.PmsSystems!.Any(
                record => MetadataComparer.Equals(record!.FolderName, folderName)))
        {
            throw new QaDuplicateMetadataException(
                QaMetadataFileKind.PmsSystems,
                paths.PmsSystemsMetadataFilePath,
                "The generated PMS folder name collides with an existing PMS folder name.");
        }

        QaPmsMetadata metadata = new()
        {
            PmsName = normalizedPmsName,
            FolderName = folderName
        };

        document.PmsSystems!.Add(metadata);
        ValidatePmsDocument(document);

        SaveWithRecordDirectory(
            QaMetadataFileKind.PmsSystems,
            paths.PmsSystemsMetadataFilePath,
            paths.ResolvePmsDirectory(folderName),
            () => WritePmsDocument(document));

        return Clone(metadata);
    }

    /// <summary>
    /// Adds a unique hotel record related to an existing PMS and ensures its directory exists.
    /// </summary>
    public QaHotelMetadata AddHotel(string hotelId, string hotelName, string pmsName)
    {
        lock (MetadataMutationLock)
        {
            return AddHotelCore(hotelId, hotelName, pmsName);
        }
    }

    private QaHotelMetadata AddHotelCore(string hotelId, string hotelName, string pmsName)
    {
        string requestedPmsName = NormalizeRequiredInput(pmsName, nameof(pmsName), "A PMS name is required.");
        QaPmsMetadataDocument pmsDocument = LoadPmsDocument();
        QaPmsMetadata? matchingPms = pmsDocument.PmsSystems!.FirstOrDefault(
            record => MetadataComparer.Equals(record!.PmsName.Trim(), requestedPmsName));

        if (matchingPms is null)
        {
            throw new ArgumentException(
                "The requested PMS does not exist in PMS metadata.",
                nameof(pmsName));
        }

        QaHotelMetadataDocument hotelDocument = LoadHotelDocument(pmsDocument);
        string normalizedHotelId = NormalizeRequiredInput(
            hotelId,
            nameof(hotelId),
            "A hotel ID is required.");
        string normalizedHotelName = NormalizeRequiredInput(
            hotelName,
            nameof(hotelName),
            "A hotel name is required.");

        if (hotelDocument.Hotels!.Any(
                record => MetadataComparer.Equals(record!.HotelId.Trim(), normalizedHotelId)))
        {
            throw new QaDuplicateMetadataException(
                QaMetadataFileKind.Hotels,
                paths.HotelsMetadataFilePath,
                "A hotel record with the same trimmed hotel ID already exists.");
        }

        string folderName = folderNameSanitizer.CreateHotelFolderName(
            normalizedHotelId,
            normalizedHotelName);

        if (hotelDocument.Hotels!.Any(
                record => MetadataComparer.Equals(record!.FolderName, folderName)))
        {
            throw new QaDuplicateMetadataException(
                QaMetadataFileKind.Hotels,
                paths.HotelsMetadataFilePath,
                "The generated hotel folder name collides with an existing hotel folder name.");
        }

        QaHotelMetadata metadata = new()
        {
            HotelId = normalizedHotelId,
            HotelName = normalizedHotelName,
            PmsName = matchingPms.PmsName.Trim(),
            FolderName = folderName
        };

        hotelDocument.Hotels!.Add(metadata);
        ValidateHotelDocument(hotelDocument, pmsDocument);

        SaveWithRecordDirectory(
            QaMetadataFileKind.Hotels,
            paths.HotelsMetadataFilePath,
            paths.ResolveHotelDirectory(folderName),
            () => WriteHotelDocument(hotelDocument, pmsDocument));

        return Clone(metadata);
    }

    /// <summary>
    /// Validates and atomically replaces the complete PMS metadata document.
    /// </summary>
    public void SavePmsSystems(QaPmsMetadataDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        lock (MetadataMutationLock)
        {
            ValidatePmsDocument(document);
            LoadHotelDocument(document);
            WriteDocument(
                document,
                QaMetadataFileKind.PmsSystems,
                paths.PmsSystemsMetadataFilePath);
        }
    }

    /// <summary>
    /// Validates current PMS relationships and atomically replaces the complete hotel document.
    /// </summary>
    public void SaveHotels(QaHotelMetadataDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        lock (MetadataMutationLock)
        {
            QaPmsMetadataDocument pmsDocument = LoadPmsDocument();
            WriteHotelDocument(document, pmsDocument);
        }
    }

    /// <summary>
    /// Explicitly backs up and resets one selected, known metadata file.
    /// </summary>
    public QaMetadataRecoveryResult RecoverMetadata(QaMetadataFileKind fileKind)
    {
        lock (MetadataMutationLock)
        {
            return RecoverMetadataCore(fileKind);
        }
    }

    private QaMetadataRecoveryResult RecoverMetadataCore(QaMetadataFileKind fileKind)
    {
        string metadataFilePath = GetMetadataFilePath(fileKind);

        if (!File.Exists(metadataFilePath))
        {
            throw new QaMetadataException(
                fileKind,
                metadataFilePath,
                "The selected metadata file is missing and cannot be backed up.");
        }

        string backupPath;

        try
        {
            Directory.CreateDirectory(paths.MetadataBackupRootPath);
            backupPath = CreateUniqueBackupPath(metadataFilePath);
            File.Copy(metadataFilePath, backupPath, overwrite: false);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw new QaMetadataException(
                fileKind,
                metadataFilePath,
                "The selected metadata file could not be backed up for explicit recovery.",
                exception);
        }

        byte[] emptyDocumentBytes = fileKind switch
        {
            QaMetadataFileKind.Hotels => QaMetadataJson.SerializeToUtf8Bytes(
                QaMetadataJson.CreateEmptyHotelsDocument()),
            QaMetadataFileKind.PmsSystems => QaMetadataJson.SerializeToUtf8Bytes(
                QaMetadataJson.CreateEmptyPmsSystemsDocument()),
            _ => throw new ArgumentOutOfRangeException(nameof(fileKind), fileKind, "Unknown metadata file kind.")
        };

        try
        {
            QaAtomicFileWriter.Replace(metadataFilePath, emptyDocumentBytes);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw new QaMetadataException(
                fileKind,
                metadataFilePath,
                "The backup was created, but the selected metadata file could not be reset.",
                exception);
        }

        return new QaMetadataRecoveryResult(fileKind, metadataFilePath, backupPath);
    }

    private QaPmsMetadataDocument LoadPmsDocument()
    {
        byte[] json = ReadMetadataFile(
            QaMetadataFileKind.PmsSystems,
            paths.PmsSystemsMetadataFilePath,
            "pmsSystems");

        QaPmsMetadataDocument? document;

        try
        {
            document = QaMetadataJson.Deserialize<QaPmsMetadataDocument>(json);
        }
        catch (JsonException exception)
        {
            throw MalformedMetadata(
                QaMetadataFileKind.PmsSystems,
                paths.PmsSystemsMetadataFilePath,
                exception);
        }
        catch (NotSupportedException exception)
        {
            throw MalformedMetadata(
                QaMetadataFileKind.PmsSystems,
                paths.PmsSystemsMetadataFilePath,
                exception);
        }

        if (document is null)
        {
            throw InvalidMetadata(
                QaMetadataFileKind.PmsSystems,
                paths.PmsSystemsMetadataFilePath,
                "The JSON root does not contain a PMS metadata document.");
        }

        ValidatePmsDocument(document);
        return document;
    }

    private QaHotelMetadataDocument LoadHotelDocument(QaPmsMetadataDocument pmsDocument)
    {
        byte[] json = ReadMetadataFile(
            QaMetadataFileKind.Hotels,
            paths.HotelsMetadataFilePath,
            "hotels");

        QaHotelMetadataDocument? document;

        try
        {
            document = QaMetadataJson.Deserialize<QaHotelMetadataDocument>(json);
        }
        catch (JsonException exception)
        {
            throw MalformedMetadata(
                QaMetadataFileKind.Hotels,
                paths.HotelsMetadataFilePath,
                exception);
        }
        catch (NotSupportedException exception)
        {
            throw MalformedMetadata(
                QaMetadataFileKind.Hotels,
                paths.HotelsMetadataFilePath,
                exception);
        }

        if (document is null)
        {
            throw InvalidMetadata(
                QaMetadataFileKind.Hotels,
                paths.HotelsMetadataFilePath,
                "The JSON root does not contain a hotel metadata document.");
        }

        ValidateHotelDocument(document, pmsDocument);
        return document;
    }

    private byte[] ReadMetadataFile(
        QaMetadataFileKind fileKind,
        string metadataFilePath,
        string collectionPropertyName)
    {
        if (!File.Exists(metadataFilePath))
        {
            throw new QaMetadataException(
                fileKind,
                metadataFilePath,
                "The metadata file is missing. Run the explicit QA storage initializer first.");
        }

        byte[] json;

        try
        {
            json = File.ReadAllBytes(metadataFilePath);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw new QaMetadataException(
                fileKind,
                metadataFilePath,
                "The metadata file could not be read.",
                exception);
        }

        if (json.Length == 0 || string.IsNullOrWhiteSpace(Encoding.UTF8.GetString(json)))
        {
            throw InvalidMetadata(
                fileKind,
                metadataFilePath,
                "The metadata file is empty or contains only whitespace.");
        }

        ValidateJsonStructure(json, fileKind, metadataFilePath, collectionPropertyName);
        return json;
    }

    private static void ValidateJsonStructure(
        byte[] json,
        QaMetadataFileKind fileKind,
        string metadataFilePath,
        string collectionPropertyName)
    {
        try
        {
            using JsonDocument jsonDocument = JsonDocument.Parse(json);
            JsonElement root = jsonDocument.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw InvalidMetadata(
                    fileKind,
                    metadataFilePath,
                    "The JSON root must be an object.");
            }

            JsonElement schemaVersion = GetRequiredProperty(
                root,
                "schemaVersion",
                fileKind,
                metadataFilePath);

            if (schemaVersion.ValueKind != JsonValueKind.Number
                || !schemaVersion.TryGetInt32(out int actualSchemaVersion))
            {
                throw InvalidMetadata(
                    fileKind,
                    metadataFilePath,
                    "The schemaVersion property must contain an integer.");
            }

            if (actualSchemaVersion != QaHotelMetadataDocument.CurrentSchemaVersion)
            {
                throw new QaUnsupportedMetadataSchemaException(
                    fileKind,
                    metadataFilePath,
                    actualSchemaVersion,
                    QaHotelMetadataDocument.CurrentSchemaVersion);
            }

            JsonElement collection = GetRequiredProperty(
                root,
                collectionPropertyName,
                fileKind,
                metadataFilePath);

            if (collection.ValueKind != JsonValueKind.Array)
            {
                throw InvalidMetadata(
                    fileKind,
                    metadataFilePath,
                    $"The {collectionPropertyName} property must contain an array.");
            }
        }
        catch (QaMetadataException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw MalformedMetadata(fileKind, metadataFilePath, exception);
        }
    }

    private static JsonElement GetRequiredProperty(
        JsonElement root,
        string propertyName,
        QaMetadataFileKind fileKind,
        string metadataFilePath)
    {
        bool found = false;
        JsonElement value = default;

        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (found)
            {
                throw InvalidMetadata(
                    fileKind,
                    metadataFilePath,
                    $"The {propertyName} property appears more than once.");
            }

            found = true;
            value = property.Value;
        }

        if (!found)
        {
            throw InvalidMetadata(
                fileKind,
                metadataFilePath,
                $"The required {propertyName} property is missing.");
        }

        return value;
    }

    private void ValidatePmsDocument(QaPmsMetadataDocument document)
    {
        ValidateSchemaVersion(
            document.SchemaVersion,
            QaPmsMetadataDocument.CurrentSchemaVersion,
            QaMetadataFileKind.PmsSystems,
            paths.PmsSystemsMetadataFilePath);

        if (document.PmsSystems is null)
        {
            throw InvalidMetadata(
                QaMetadataFileKind.PmsSystems,
                paths.PmsSystemsMetadataFilePath,
                "The pmsSystems collection is missing or null.");
        }

        HashSet<string> pmsNames = new(MetadataComparer);
        HashSet<string> folderNames = new(MetadataComparer);

        foreach (QaPmsMetadata? record in document.PmsSystems)
        {
            if (record is null)
            {
                throw InvalidMetadata(
                    QaMetadataFileKind.PmsSystems,
                    paths.PmsSystemsMetadataFilePath,
                    "The pmsSystems collection contains a null record.");
            }

            if (string.IsNullOrWhiteSpace(record.PmsName))
            {
                throw InvalidMetadata(
                    QaMetadataFileKind.PmsSystems,
                    paths.PmsSystemsMetadataFilePath,
                    "A PMS record contains a blank required PMS name.");
            }

            if (string.IsNullOrWhiteSpace(record.FolderName)
                || !folderNameSanitizer.IsSafeStoredLeafName(record.FolderName))
            {
                throw InvalidMetadata(
                    QaMetadataFileKind.PmsSystems,
                    paths.PmsSystemsMetadataFilePath,
                    "A PMS record contains an unsafe required folder name.");
            }

            if (!pmsNames.Add(record.PmsName.Trim()))
            {
                throw new QaDuplicateMetadataException(
                    QaMetadataFileKind.PmsSystems,
                    paths.PmsSystemsMetadataFilePath,
                    "The document contains duplicate PMS names after trimming and case-insensitive comparison.");
            }

            if (!folderNames.Add(record.FolderName))
            {
                throw new QaDuplicateMetadataException(
                    QaMetadataFileKind.PmsSystems,
                    paths.PmsSystemsMetadataFilePath,
                    "The document contains duplicate PMS folder names under case-insensitive comparison.");
            }
        }
    }

    private void ValidateHotelDocument(
        QaHotelMetadataDocument document,
        QaPmsMetadataDocument pmsDocument)
    {
        ValidateSchemaVersion(
            document.SchemaVersion,
            QaHotelMetadataDocument.CurrentSchemaVersion,
            QaMetadataFileKind.Hotels,
            paths.HotelsMetadataFilePath);

        if (document.Hotels is null)
        {
            throw InvalidMetadata(
                QaMetadataFileKind.Hotels,
                paths.HotelsMetadataFilePath,
                "The hotels collection is missing or null.");
        }

        HashSet<string> knownPmsNames = new(
            pmsDocument.PmsSystems!.Select(record => record!.PmsName.Trim()),
            MetadataComparer);
        HashSet<string> hotelIds = new(MetadataComparer);
        HashSet<string> folderNames = new(MetadataComparer);

        foreach (QaHotelMetadata? record in document.Hotels)
        {
            if (record is null)
            {
                throw InvalidMetadata(
                    QaMetadataFileKind.Hotels,
                    paths.HotelsMetadataFilePath,
                    "The hotels collection contains a null record.");
            }

            if (string.IsNullOrWhiteSpace(record.HotelId)
                || string.IsNullOrWhiteSpace(record.HotelName)
                || string.IsNullOrWhiteSpace(record.PmsName))
            {
                throw InvalidMetadata(
                    QaMetadataFileKind.Hotels,
                    paths.HotelsMetadataFilePath,
                    "A hotel record contains a blank required value.");
            }

            if (string.IsNullOrWhiteSpace(record.FolderName)
                || !folderNameSanitizer.IsSafeStoredLeafName(record.FolderName))
            {
                throw InvalidMetadata(
                    QaMetadataFileKind.Hotels,
                    paths.HotelsMetadataFilePath,
                    "A hotel record contains an unsafe required folder name.");
            }

            if (!hotelIds.Add(record.HotelId.Trim()))
            {
                throw new QaDuplicateMetadataException(
                    QaMetadataFileKind.Hotels,
                    paths.HotelsMetadataFilePath,
                    "The document contains duplicate hotel IDs after trimming and case-insensitive comparison.");
            }

            if (!folderNames.Add(record.FolderName))
            {
                throw new QaDuplicateMetadataException(
                    QaMetadataFileKind.Hotels,
                    paths.HotelsMetadataFilePath,
                    "The document contains duplicate hotel folder names under case-insensitive comparison.");
            }

            if (!knownPmsNames.Contains(record.PmsName.Trim()))
            {
                throw InvalidMetadata(
                    QaMetadataFileKind.Hotels,
                    paths.HotelsMetadataFilePath,
                    "A hotel record references a PMS that does not exist in PMS metadata.");
            }
        }
    }

    private static void ValidateSchemaVersion(
        int? actualSchemaVersion,
        int expectedSchemaVersion,
        QaMetadataFileKind fileKind,
        string metadataFilePath)
    {
        if (actualSchemaVersion is null)
        {
            throw InvalidMetadata(
                fileKind,
                metadataFilePath,
                "The required schema version is missing or null.");
        }

        if (actualSchemaVersion.Value != expectedSchemaVersion)
        {
            throw new QaUnsupportedMetadataSchemaException(
                fileKind,
                metadataFilePath,
                actualSchemaVersion.Value,
                expectedSchemaVersion);
        }
    }

    private void WritePmsDocument(QaPmsMetadataDocument document)
    {
        ValidatePmsDocument(document);
        WriteDocument(
            document,
            QaMetadataFileKind.PmsSystems,
            paths.PmsSystemsMetadataFilePath);
    }

    private void WriteHotelDocument(
        QaHotelMetadataDocument document,
        QaPmsMetadataDocument pmsDocument)
    {
        ValidateHotelDocument(document, pmsDocument);
        WriteDocument(document, QaMetadataFileKind.Hotels, paths.HotelsMetadataFilePath);
    }

    private static void WriteDocument<T>(
        T document,
        QaMetadataFileKind fileKind,
        string metadataFilePath)
    {
        byte[] json;

        try
        {
            json = QaMetadataJson.SerializeToUtf8Bytes(document);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new QaMetadataException(
                fileKind,
                metadataFilePath,
                "The validated metadata document could not be serialized.",
                exception);
        }

        try
        {
            QaAtomicFileWriter.Replace(metadataFilePath, json);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw new QaMetadataException(
                fileKind,
                metadataFilePath,
                "The metadata document could not be atomically replaced.",
                exception);
        }
    }

    private static void SaveWithRecordDirectory(
        QaMetadataFileKind fileKind,
        string metadataFilePath,
        string recordDirectoryPath,
        Action saveAction)
    {
        lock (MetadataMutationLock)
        {
            SaveWithRecordDirectoryCore(
                fileKind,
                metadataFilePath,
                recordDirectoryPath,
                saveAction);
        }
    }

    private static void SaveWithRecordDirectoryCore(
        QaMetadataFileKind fileKind,
        string metadataFilePath,
        string recordDirectoryPath,
        Action saveAction)
    {
        bool directoryExisted;

        try
        {
            directoryExisted = Directory.Exists(recordDirectoryPath);
            Directory.CreateDirectory(recordDirectoryPath);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw new QaMetadataException(
                fileKind,
                metadataFilePath,
                "The record directory could not be created.",
                exception);
        }

        bool createdDirectory = !directoryExisted;

        try
        {
            saveAction();
        }
        catch (Exception saveException)
        {
            if (!createdDirectory)
            {
                throw;
            }

            Exception? rollbackException = null;

            try
            {
                Directory.Delete(recordDirectoryPath, recursive: false);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                rollbackException = exception;
            }

            if (rollbackException is null)
            {
                throw;
            }

            throw new QaMetadataException(
                fileKind,
                metadataFilePath,
                "The metadata save failed, and the newly created record directory could not be rolled back.",
                saveException,
                rollbackException);
        }
    }

    private string CreateUniqueBackupPath(string metadataFilePath)
    {
        string timestamp = DateTime.UtcNow.ToString(
            "yyyyMMdd'T'HHmmssfffffff'Z'",
            CultureInfo.InvariantCulture);
        string fileName = $"{Path.GetFileName(metadataFilePath)}.{timestamp}.{Guid.NewGuid():N}.bak";

        return Path.Combine(paths.MetadataBackupRootPath, fileName);
    }

    private string GetMetadataFilePath(QaMetadataFileKind fileKind)
    {
        return fileKind switch
        {
            QaMetadataFileKind.Hotels => paths.HotelsMetadataFilePath,
            QaMetadataFileKind.PmsSystems => paths.PmsSystemsMetadataFilePath,
            _ => throw new ArgumentOutOfRangeException(nameof(fileKind), fileKind, "Unknown metadata file kind.")
        };
    }

    private static string NormalizeRequiredInput(
        string? value,
        string parameterName,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }

    private static QaPmsMetadata Clone(QaPmsMetadata record)
    {
        return new QaPmsMetadata
        {
            PmsName = record.PmsName,
            FolderName = record.FolderName
        };
    }

    private static QaHotelMetadata Clone(QaHotelMetadata record)
    {
        return new QaHotelMetadata
        {
            HotelId = record.HotelId,
            HotelName = record.HotelName,
            PmsName = record.PmsName,
            FolderName = record.FolderName
        };
    }

    private static QaMetadataException InvalidMetadata(
        QaMetadataFileKind fileKind,
        string metadataFilePath,
        string reason)
    {
        return new QaMetadataException(fileKind, metadataFilePath, reason);
    }

    private static QaMetadataException MalformedMetadata(
        QaMetadataFileKind fileKind,
        string metadataFilePath,
        Exception innerException)
    {
        return new QaMetadataException(
            fileKind,
            metadataFilePath,
            "The metadata JSON is malformed or structurally invalid.",
            innerException);
    }

    private static bool IsFileSystemException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException;
    }
}
