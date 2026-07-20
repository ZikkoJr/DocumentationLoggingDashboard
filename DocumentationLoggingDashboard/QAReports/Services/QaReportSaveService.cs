using System.Security.Cryptography;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Prepares and commits the Hotel PDF, PMS PDF, and QA report index as one
/// logical transaction without accepting caller-selected destinations.
/// </summary>
public sealed class QaReportSaveService
{
    private static readonly object SaveLock = new();

    private readonly QaStoragePaths paths;
    private readonly QaMetadataService metadataService;
    private readonly QaFolderNameSanitizer folderNameSanitizer;
    private readonly QaReportFilenameService filenameService;
    private readonly QaReportExistingFileService existingFileService;
    private readonly QaReportIndexService indexService;
    private readonly IQaReportTransactionFileOperations fileOperations;

    public QaReportSaveService(
        QaStoragePaths paths,
        QaMetadataService metadataService,
        QaFolderNameSanitizer folderNameSanitizer)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.metadataService = metadataService
            ?? throw new ArgumentNullException(nameof(metadataService));
        this.folderNameSanitizer = folderNameSanitizer
            ?? throw new ArgumentNullException(nameof(folderNameSanitizer));
        filenameService = new QaReportFilenameService(
            this.folderNameSanitizer);
        QaReportFilenameParser filenameParser = new(
            this.folderNameSanitizer);
        existingFileService = new QaReportExistingFileService(
            this.paths,
            filenameParser);
        indexService = new QaReportIndexService(this.paths);
        fileOperations = new QaReportTransactionFileOperations();
    }

    internal QaReportSaveService(
        QaStoragePaths paths,
        QaMetadataService metadataService,
        QaFolderNameSanitizer folderNameSanitizer,
        QaReportFilenameService filenameService,
        QaReportExistingFileService existingFileService,
        QaReportIndexService indexService,
        IQaReportTransactionFileOperations fileOperations)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.metadataService = metadataService
            ?? throw new ArgumentNullException(nameof(metadataService));
        this.folderNameSanitizer = folderNameSanitizer
            ?? throw new ArgumentNullException(nameof(folderNameSanitizer));
        this.filenameService = filenameService
            ?? throw new ArgumentNullException(nameof(filenameService));
        this.existingFileService = existingFileService
            ?? throw new ArgumentNullException(nameof(existingFileService));
        this.indexService = indexService
            ?? throw new ArgumentNullException(nameof(indexService));
        this.fileOperations = fileOperations
            ?? throw new ArgumentNullException(nameof(fileOperations));
    }

    /// <summary>
    /// Rereads metadata and the index, resolves canonical destinations, and
    /// finds existing filesystem reports without writing any file or directory.
    /// </summary>
    public QaReportSavePreparation Prepare(QaReportSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            return PrepareCore(request);
        }
        catch (QaReportSaveException)
        {
            throw;
        }
        catch (QaReportIndexException exception)
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.IndexFailure,
                QaReportSaveStage.Preparation,
                "The QA report index could not be prepared.",
                exception,
                previousStateRestored: false,
                manualReviewRequired: false,
                manualReviewLocations: []);
        }
        catch (QaMetadataException exception)
        {
            throw CreatePreparationFailure(
                QaReportSaveErrorCategory.MetadataMismatch,
                "Current QA metadata could not authorize this save.",
                exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw CreatePreparationFailure(
                QaReportSaveErrorCategory.PermissionDenied,
                "A configured QA report location could not be inspected.",
                exception);
        }
        catch (IOException exception) when (IsFileInUse(exception))
        {
            throw CreatePreparationFailure(
                QaReportSaveErrorCategory.FileInUse,
                "A configured QA report file is currently in use.",
                exception);
        }
        catch (Exception exception) when (
            exception is IOException
                or ArgumentException
                or NotSupportedException
                or InvalidOperationException
                or System.Security.SecurityException)
        {
            throw CreatePreparationFailure(
                QaReportSaveErrorCategory.InvalidDestination,
                "The configured QA report destination is invalid or inaccessible.",
                exception);
        }
    }

    /// <summary>
    /// Stages and verifies the exact same PDF payload in both canonical
    /// destinations, then commits both copies and the complete index.
    /// </summary>
    public QaReportSaveResult Save(
        QaReportSavePreparation preparation,
        ReadOnlyMemory<byte> pdfBytes,
        bool overwriteConfirmed)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        ValidatePdfPayload(pdfBytes);

        lock (SaveLock)
        {
            QaReportSavePreparation currentPreparation = Prepare(
                preparation.Request);
            EnsurePreparationStillCurrent(
                preparation,
                currentPreparation);

            if (currentPreparation.ExistingFiles.HasMatches
                && !overwriteConfirmed)
            {
                throw new QaReportSaveException(
                    QaReportSaveErrorCategory.OverwriteRequired,
                    QaReportSaveStage.Preparation,
                    "Existing QA report files require explicit overwrite confirmation.");
            }

            return CommitTransaction(currentPreparation, pdfBytes);
        }
    }

    private QaReportSavePreparation PrepareCore(QaReportSaveRequest request)
    {
        ValidateFreshReadiness(request);

        QaReport report = request.Report;
        QaHotelInformation hotelInformation = report.HotelInformation
            ?? throw MetadataMismatch(
                "The report no longer contains Hotel information.");
        QaFileMonth fileMonth = hotelInformation.FileMonth
            ?? throw MetadataMismatch(
                "The report no longer contains a File Month.");

        string reportHotelId = RequireTrimmedMetadataValue(
            hotelInformation.HotelId,
            "Hotel ID");
        string reportHotelName = RequireTrimmedMetadataValue(
            hotelInformation.HotelName,
            "Hotel Name");
        string reportPmsName = RequireTrimmedMetadataValue(
            hotelInformation.PmsName,
            "PMS");

        IReadOnlyList<QaHotelMetadata> currentHotels =
            metadataService.LoadHotels();
        IReadOnlyList<QaPmsMetadata> currentPmsSystems =
            metadataService.LoadPmsSystems();

        QaHotelMetadata[] hotelMatches = currentHotels
            .Where(hotel => hotel is not null)
            .Where(hotel => string.Equals(
                hotel.HotelId?.Trim(),
                reportHotelId,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (hotelMatches.Length != 1)
        {
            throw MetadataMismatch(
                "The report Hotel ID no longer identifies one canonical Hotel.");
        }

        QaHotelMetadata canonicalHotel = hotelMatches[0];
        string canonicalHotelId = RequireTrimmedMetadataValue(
            canonicalHotel.HotelId,
            "canonical Hotel ID");
        string canonicalHotelName = RequireTrimmedMetadataValue(
            canonicalHotel.HotelName,
            "canonical Hotel Name");
        string canonicalHotelPmsName = RequireTrimmedMetadataValue(
            canonicalHotel.PmsName,
            "canonical Hotel PMS");

        if (!string.Equals(
                reportHotelId,
                canonicalHotelId,
                StringComparison.Ordinal)
            || !string.Equals(
                reportHotelName,
                canonicalHotelName,
                StringComparison.Ordinal)
            || !string.Equals(
                reportPmsName,
                canonicalHotelPmsName,
                StringComparison.Ordinal))
        {
            throw MetadataMismatch(
                "The report Hotel Name or PMS no longer matches canonical metadata.");
        }

        QaPmsMetadata[] pmsMatches = currentPmsSystems
            .Where(pms => pms is not null)
            .Where(pms => string.Equals(
                pms.PmsName?.Trim(),
                canonicalHotelPmsName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (pmsMatches.Length != 1)
        {
            throw MetadataMismatch(
                "The report PMS no longer identifies one canonical PMS.");
        }

        QaPmsMetadata canonicalPms = pmsMatches[0];

        if (!folderNameSanitizer.IsSafeStoredLeafName(
                canonicalHotel.FolderName)
            || !folderNameSanitizer.IsSafeStoredLeafName(
                canonicalPms.FolderName))
        {
            throw MetadataMismatch(
                "Canonical Hotel or PMS storage metadata is no longer safe.");
        }

        string filename;

        try
        {
            filename = filenameService.CreateFilename(
                report,
                canonicalHotel,
                canonicalPms);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or NotSupportedException)
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.FilenameFailure,
                QaReportSaveStage.Preparation,
                "A safe QA report filename could not be created.",
                exception,
                previousStateRestored: false,
                manualReviewRequired: false,
                manualReviewLocations: []);
        }

        string hotelDirectory = paths.ResolveHotelDirectory(
            canonicalHotel.FolderName);
        string pmsDirectory = paths.ResolvePmsDirectory(
            canonicalPms.FolderName);
        string hotelFinalPath = Path.GetFullPath(
            Path.Combine(hotelDirectory, filename));
        string pmsFinalPath = Path.GetFullPath(
            Path.Combine(pmsDirectory, filename));

        ValidateApprovedPath(
            paths.ByHotelRootPath,
            hotelFinalPath,
            "Hotel PDF destination");
        ValidateApprovedPath(
            paths.ByPmsRootPath,
            pmsFinalPath,
            "PMS PDF destination");
        ValidateApprovedPath(
            paths.IndexRootPath,
            paths.QaReportIndexFilePath,
            "QA report index destination");

        QaReportKey reportKey = new(canonicalHotelId, fileMonth);
        QaReportExistingFiles existingFiles =
            existingFileService.FindExistingFiles(
                canonicalHotel,
                canonicalPms,
                reportKey);

        ValidateExistingMatches(
            existingFiles.HotelFilePaths,
            hotelDirectory,
            paths.ByHotelRootPath,
            "Hotel");
        ValidateExistingMatches(
            existingFiles.PmsFilePaths,
            pmsDirectory,
            paths.ByPmsRootPath,
            "PMS");
        EnsureExactTargetIsRecognized(
            hotelFinalPath,
            existingFiles.HotelFilePaths,
            "Hotel");
        EnsureExactTargetIsRecognized(
            pmsFinalPath,
            existingFiles.PmsFilePaths,
            "PMS");

        string relativeHotelPath = CreateRelativeQaPath(hotelFinalPath);
        string relativePmsPath = CreateRelativeQaPath(pmsFinalPath);
        QaReportStatus finalStatus = request.ValidationResult.CalculatedStatus!.Value;
        QaReportIndexEntry indexEntry = new(
            reportKey,
            report.QaDate,
            canonicalHotelName,
            canonicalHotelId,
            RequireTrimmedMetadataValue(canonicalPms.PmsName, "canonical PMS Name"),
            fileMonth,
            finalStatus,
            request.ValidationResult.EffectiveCreatedBy,
            filename,
            relativeHotelPath,
            relativePmsPath,
            request.GeneratedAt);
        QaReportIndexUpdatePlan indexUpdate =
            indexService.PrepareUpdate(indexEntry);

        return new QaReportSavePreparation(
            request,
            canonicalHotel,
            canonicalPms,
            hotelDirectory,
            pmsDirectory,
            hotelFinalPath,
            pmsFinalPath,
            indexEntry,
            indexUpdate.SourceContent,
            indexUpdate.UpdatedContent,
            existingFiles);
    }

    private void ValidateFreshReadiness(QaReportSaveRequest request)
    {
        QaReportValidationResult validationResult = request.ValidationResult;
        QaReport report = request.Report;

        if (!validationResult.IsReady
            || validationResult.BlockingErrors.Count != 0
            || validationResult.CalculatedStatus is null)
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.ReportNotReady,
                QaReportSaveStage.Preparation,
                "The QA report is not ready for saving.");
        }

        if (report.ReportStatus != validationResult.CalculatedStatus
            || string.IsNullOrWhiteSpace(
                validationResult.ValidatedReportFingerprint))
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.StaleReadiness,
                QaReportSaveStage.Preparation,
                "The QA report readiness result is stale.");
        }

        string currentFingerprint;

        try
        {
            currentFingerprint = QaReportReadinessFingerprint.Compute(report);
        }
        catch (Exception exception)
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.StaleReadiness,
                QaReportSaveStage.Preparation,
                "The current QA report state could not be matched to readiness evidence.",
                exception,
                previousStateRestored: false,
                manualReviewRequired: false,
                manualReviewLocations: []);
        }

        if (!string.Equals(
                currentFingerprint,
                validationResult.ValidatedReportFingerprint,
                StringComparison.Ordinal))
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.StaleReadiness,
                QaReportSaveStage.Preparation,
                "The QA report changed after its readiness result was created.");
        }
    }

    private QaReportSaveResult CommitTransaction(
        QaReportSavePreparation preparation,
        ReadOnlyMemory<byte> pdfBytes)
    {
        TransactionState state = CreateTransactionState(preparation);
        QaReportSaveStage stage = QaReportSaveStage.HotelStaging;

        try
        {
            fileOperations.CreateDirectory(preparation.HotelDirectoryPath);
            stage = QaReportSaveStage.PmsStaging;
            fileOperations.CreateDirectory(preparation.PmsDirectoryPath);
            stage = QaReportSaveStage.IndexStaging;
            fileOperations.CreateDirectory(paths.IndexRootPath);

            EnsureIndexUnchanged(preparation.SourceIndexContent);

            byte[] sourcePdfHash = SHA256.HashData(pdfBytes.Span);
            byte[] sourceIndexHash = SHA256.HashData(
                preparation.UpdatedIndexContent.Span);

            stage = QaReportSaveStage.HotelStaging;
            fileOperations.WriteNewAndFlush(
                state.HotelTemporaryPath,
                pdfBytes);
            state.HotelTemporaryCreated = true;
            VerifyStagedContent(
                state.HotelTemporaryPath,
                pdfBytes.Length,
                sourcePdfHash,
                "Hotel PDF");

            stage = QaReportSaveStage.PmsStaging;
            fileOperations.WriteNewAndFlush(
                state.PmsTemporaryPath,
                pdfBytes);
            state.PmsTemporaryCreated = true;
            VerifyStagedContent(
                state.PmsTemporaryPath,
                pdfBytes.Length,
                sourcePdfHash,
                "PMS PDF");

            byte[] hotelStagedHash = fileOperations.ComputeSha256(
                state.HotelTemporaryPath);
            byte[] pmsStagedHash = fileOperations.ComputeSha256(
                state.PmsTemporaryPath);
            if (!CryptographicOperations.FixedTimeEquals(
                    hotelStagedHash,
                    pmsStagedHash))
            {
                throw ContentVerificationFailure(
                    "The staged Hotel and PMS PDF copies are not identical.");
            }

            stage = QaReportSaveStage.IndexStaging;
            fileOperations.WriteNewAndFlush(
                state.IndexTemporaryPath,
                preparation.UpdatedIndexContent);
            state.IndexTemporaryCreated = true;
            VerifyStagedContent(
                state.IndexTemporaryPath,
                preparation.UpdatedIndexContent.Length,
                sourceIndexHash,
                "QA report index");

            EnsureIndexUnchanged(preparation.SourceIndexContent);
            EnsureExistingMatchesUnchanged(preparation);

            stage = QaReportSaveStage.ExistingReportBackup;
            MoveExistingReportsToBackups(state.HotelBackups);
            MoveExistingReportsToBackups(state.PmsBackups);

            stage = QaReportSaveStage.IndexBackup;
            EnsureIndexUnchanged(preparation.SourceIndexContent);
            fileOperations.Move(
                paths.QaReportIndexFilePath,
                state.IndexBackupPath);
            state.IndexBackedUp = true;

            stage = QaReportSaveStage.HotelCommit;
            fileOperations.Move(
                state.HotelTemporaryPath,
                preparation.HotelFinalPath);
            state.HotelTemporaryCreated = false;
            state.HotelCommitted = true;

            stage = QaReportSaveStage.PmsCommit;
            fileOperations.Move(
                state.PmsTemporaryPath,
                preparation.PmsFinalPath);
            state.PmsTemporaryCreated = false;
            state.PmsCommitted = true;

            stage = QaReportSaveStage.IndexCommit;
            fileOperations.Move(
                state.IndexTemporaryPath,
                paths.QaReportIndexFilePath);
            state.IndexTemporaryCreated = false;
            state.IndexCommitted = true;

            string? cleanupWarning = CleanupAfterSuccess(state);

            return new QaReportSaveResult(
                success: true,
                cancelled: false,
                preparation.FinalFilename,
                preparation.HotelFinalPath,
                preparation.PmsFinalPath,
                preparation.ReportKey,
                preparation.FinalStatus,
                overwriteOccurred: preparation.ExistingFiles.HasMatches,
                preparation.ExistingFiles.HotelFilePaths.Count,
                preparation.ExistingFiles.PmsFilePaths.Count,
                indexUpdated: true,
                pdfBytes.Length,
                preparation.GeneratedAt,
                cleanupWarning,
                manualReviewRequired: false);
        }
        catch (Exception exception)
        {
            throw RollBackAndCreateFailure(
                preparation,
                state,
                stage,
                exception);
        }
    }

    private TransactionState CreateTransactionState(
        QaReportSavePreparation preparation)
    {
        string hotelTemporaryPath = CreateTransactionPath(
            preparation.HotelDirectoryPath,
            paths.ByHotelRootPath,
            ".qa-tmp");
        string pmsTemporaryPath = CreateTransactionPath(
            preparation.PmsDirectoryPath,
            paths.ByPmsRootPath,
            ".qa-tmp");
        string indexTemporaryPath = CreateTransactionPath(
            paths.IndexRootPath,
            paths.IndexRootPath,
            ".qa-tmp");
        string indexBackupPath = CreateTransactionPath(
            paths.IndexRootPath,
            paths.IndexRootPath,
            ".qa-rollback");

        List<BackupRecord> hotelBackups = CreateBackupRecords(
            preparation.ExistingFiles.HotelFilePaths,
            preparation.HotelDirectoryPath,
            paths.ByHotelRootPath);
        List<BackupRecord> pmsBackups = CreateBackupRecords(
            preparation.ExistingFiles.PmsFilePaths,
            preparation.PmsDirectoryPath,
            paths.ByPmsRootPath);

        return new TransactionState(
            hotelTemporaryPath,
            pmsTemporaryPath,
            indexTemporaryPath,
            indexBackupPath,
            hotelBackups,
            pmsBackups);
    }

    private List<BackupRecord> CreateBackupRecords(
        IReadOnlyList<string> originalPaths,
        string expectedDirectory,
        string expectedRoot)
    {
        List<BackupRecord> backups = [];

        foreach (string originalPath in originalPaths)
        {
            ValidateApprovedPath(expectedRoot, originalPath, "existing QA report");

            if (!string.Equals(
                    Path.GetDirectoryName(originalPath),
                    expectedDirectory,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new QaReportSaveException(
                    QaReportSaveErrorCategory.InvalidDestination,
                    QaReportSaveStage.Preparation,
                    "An existing QA report is outside its canonical leaf directory.");
            }

            backups.Add(new BackupRecord(
                originalPath,
                CreateTransactionPath(
                    expectedDirectory,
                    expectedRoot,
                    ".qa-rollback")));
        }

        return backups;
    }

    private void MoveExistingReportsToBackups(
        IEnumerable<BackupRecord> backups)
    {
        foreach (BackupRecord backup in backups)
        {
            fileOperations.Move(
                backup.OriginalPath,
                backup.BackupPath);
            backup.Moved = true;
        }
    }

    private void VerifyStagedContent(
        string stagedPath,
        int expectedLength,
        ReadOnlySpan<byte> expectedHash,
        string artifactName)
    {
        if (fileOperations.GetFileLength(stagedPath) != expectedLength)
        {
            throw ContentVerificationFailure(
                $"The staged {artifactName} length does not match its source payload.");
        }

        byte[] actualHash = fileOperations.ComputeSha256(stagedPath);
        if (!CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash))
        {
            throw ContentVerificationFailure(
                $"The staged {artifactName} content does not match its source payload.");
        }
    }

    private void EnsureIndexUnchanged(ReadOnlyMemory<byte> expectedContent)
    {
        if (!fileOperations.FileExists(paths.QaReportIndexFilePath)
            || fileOperations.GetFileLength(paths.QaReportIndexFilePath)
                != expectedContent.Length)
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.ConcurrentChange,
                QaReportSaveStage.IndexBackup,
                "The QA report index changed during save preparation.");
        }

        byte[] expectedHash = SHA256.HashData(expectedContent.Span);
        byte[] currentHash = fileOperations.ComputeSha256(
            paths.QaReportIndexFilePath);

        if (!CryptographicOperations.FixedTimeEquals(
                expectedHash,
                currentHash))
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.ConcurrentChange,
                QaReportSaveStage.IndexBackup,
                "The QA report index changed during save preparation.");
        }
    }

    private string? CleanupAfterSuccess(TransactionState state)
    {
        List<string> leftovers = [];

        foreach (BackupRecord backup in state.HotelBackups
                     .Concat(state.PmsBackups))
        {
            TryCleanupFile(backup.BackupPath, leftovers);
        }

        TryCleanupFile(state.IndexBackupPath, leftovers);

        if (leftovers.Count == 0)
        {
            return null;
        }

        string[] filenames = leftovers
            .Select(path => Path.GetFileName(path) ?? path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string displayed = string.Join(", ", filenames.Take(5));

        if (filenames.Length > 5)
        {
            displayed += $", and {filenames.Length - 5} more";
        }

        return "The report was saved, but transaction cleanup could not remove: "
            + displayed;
    }

    private void TryCleanupFile(
        string path,
        ICollection<string> leftovers)
    {
        try
        {
            // File.Delete is intentionally a no-op for a genuinely missing file;
            // calling it directly preserves access/IO failures as warnings.
            fileOperations.Delete(path);
        }
        catch (Exception)
        {
            leftovers.Add(path);
        }
    }

    private QaReportSaveException RollBackAndCreateFailure(
        QaReportSavePreparation preparation,
        TransactionState state,
        QaReportSaveStage failedStage,
        Exception originalFailure)
    {
        List<Exception> rollbackFailures = [];

        TryRollback(
            () => DeleteIfExists(
                state.IndexCommitted
                    ? paths.QaReportIndexFilePath
                    : null),
            rollbackFailures);
        TryRollback(
            () => DeleteIfExists(
                state.PmsCommitted
                    ? preparation.PmsFinalPath
                    : null),
            rollbackFailures);
        TryRollback(
            () => DeleteIfExists(
                state.HotelCommitted
                    ? preparation.HotelFinalPath
                    : null),
            rollbackFailures);

        if (state.IndexBackedUp)
        {
            TryRollback(
                () => RestoreBackup(
                    state.IndexBackupPath,
                    paths.QaReportIndexFilePath),
                rollbackFailures);
        }

        foreach (BackupRecord backup in state.HotelBackups
                     .Concat(state.PmsBackups)
                     .Where(backup => backup.Moved)
                     .Reverse())
        {
            TryRollback(
                () => RestoreBackup(
                    backup.BackupPath,
                    backup.OriginalPath),
                rollbackFailures);
        }

        if (state.HotelTemporaryCreated)
        {
            TryRollback(
                () => DeleteIfExists(state.HotelTemporaryPath),
                rollbackFailures);
        }

        if (state.PmsTemporaryCreated)
        {
            TryRollback(
                () => DeleteIfExists(state.PmsTemporaryPath),
                rollbackFailures);
        }

        if (state.IndexTemporaryCreated)
        {
            TryRollback(
                () => DeleteIfExists(state.IndexTemporaryPath),
                rollbackFailures);
        }

        if (originalFailure is QaReportOwnedTemporaryCleanupException
            ownedTemporaryFailure)
        {
            // The low-level writer certifies ownership only after CreateNew
            // succeeds; a retry here cannot delete another transaction's file.
            TryRollback(
                () => DeleteIfExists(
                    ownedTemporaryFailure.TemporaryPath),
                rollbackFailures);
        }

        bool manualReviewRequired = rollbackFailures.Count != 0;
        bool priorPdfStateChanged = state.HotelBackups.Any(backup => backup.Moved)
            || state.PmsBackups.Any(backup => backup.Moved);
        bool previousStateRestored = priorPdfStateChanged
            && !manualReviewRequired;
        QaReportSaveErrorCategory category = manualReviewRequired
            ? QaReportSaveErrorCategory.RollbackFailure
            : ClassifyFailure(originalFailure, failedStage);
        Exception retainedFailure = manualReviewRequired
            ? new AggregateException(
                "The QA report save and one or more rollback actions failed.",
                new[] { originalFailure }.Concat(rollbackFailures))
            : originalFailure;

        return new QaReportSaveException(
            category,
            manualReviewRequired
                ? QaReportSaveStage.Rollback
                : failedStage,
            manualReviewRequired
                ? "The QA report save failed and rollback was incomplete."
                : "The QA report paired transaction did not complete.",
            retainedFailure,
            previousStateRestored,
            manualReviewRequired,
            manualReviewRequired
                ? new[]
                {
                    preparation.HotelDirectoryPath,
                    preparation.PmsDirectoryPath,
                    paths.IndexRootPath
                }
                : []);
    }

    private void RestoreBackup(
        string backupPath,
        string originalPath)
    {
        // Move with overwrite disabled fails safely if an unexpected destination
        // appeared; it also preserves missing/inaccessible backup failures.
        fileOperations.Move(backupPath, originalPath);
    }

    private void DeleteIfExists(string? path)
    {
        if (path is not null)
        {
            // File.Delete is a no-op only when the path is genuinely absent.
            fileOperations.Delete(path);
        }
    }

    private void EnsureExistingMatchesUnchanged(
        QaReportSavePreparation preparation)
    {
        QaReportExistingFiles currentMatches =
            existingFileService.FindExistingFiles(
                preparation.CanonicalHotel,
                preparation.CanonicalPms,
                preparation.ReportKey);

        if (!PathsEqual(
                preparation.ExistingFiles.HotelFilePaths,
                currentMatches.HotelFilePaths)
            || !PathsEqual(
                preparation.ExistingFiles.PmsFilePaths,
                currentMatches.PmsFilePaths))
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.ConcurrentChange,
                QaReportSaveStage.ExistingReportBackup,
                "Existing QA report files changed while the transaction was being staged.");
        }

        EnsureExactTargetIsRecognized(
            preparation.HotelFinalPath,
            currentMatches.HotelFilePaths,
            "Hotel");
        EnsureExactTargetIsRecognized(
            preparation.PmsFinalPath,
            currentMatches.PmsFilePaths,
            "PMS");
    }

    private static void TryRollback(
        Action rollbackAction,
        ICollection<Exception> rollbackFailures)
    {
        try
        {
            rollbackAction();
        }
        catch (Exception exception)
        {
            rollbackFailures.Add(exception);
        }
    }

    private static QaReportSaveErrorCategory ClassifyFailure(
        Exception exception,
        QaReportSaveStage stage)
    {
        if (exception is QaReportSaveException saveException)
        {
            return saveException.ErrorCategory;
        }

        if (exception is UnauthorizedAccessException
            or System.Security.SecurityException)
        {
            return QaReportSaveErrorCategory.PermissionDenied;
        }

        if (exception is IOException ioException && IsFileInUse(ioException))
        {
            return QaReportSaveErrorCategory.FileInUse;
        }

        if (stage is QaReportSaveStage.IndexStaging
            or QaReportSaveStage.IndexBackup
            or QaReportSaveStage.IndexCommit)
        {
            return QaReportSaveErrorCategory.IndexFailure;
        }

        return QaReportSaveErrorCategory.TransactionFailure;
    }

    private static void EnsurePreparationStillCurrent(
        QaReportSavePreparation supplied,
        QaReportSavePreparation current)
    {
        bool sameAuthority = supplied.ReportKey == current.ReportKey
            && string.Equals(
                supplied.FinalFilename,
                current.FinalFilename,
                StringComparison.Ordinal)
            && string.Equals(
                supplied.HotelCopyPath,
                current.HotelCopyPath,
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                supplied.PmsCopyPath,
                current.PmsCopyPath,
                StringComparison.OrdinalIgnoreCase)
            && supplied.FinalStatus == current.FinalStatus
            && supplied.GeneratedAt == current.GeneratedAt
            && MetadataEquals(
                supplied.CanonicalHotel,
                current.CanonicalHotel)
            && MetadataEquals(
                supplied.CanonicalPms,
                current.CanonicalPms);

        bool sameMatches = PathsEqual(
                supplied.ExistingFiles.HotelFilePaths,
                current.ExistingFiles.HotelFilePaths)
            && PathsEqual(
                supplied.ExistingFiles.PmsFilePaths,
                current.ExistingFiles.PmsFilePaths);

        if (!sameAuthority || !sameMatches)
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.ConcurrentChange,
                QaReportSaveStage.Preparation,
                "QA metadata or existing report files changed after preparation.");
        }
    }

    private static bool MetadataEquals(
        QaHotelMetadata left,
        QaHotelMetadata right)
    {
        return string.Equals(left.HotelId, right.HotelId, StringComparison.Ordinal)
            && string.Equals(left.HotelName, right.HotelName, StringComparison.Ordinal)
            && string.Equals(left.PmsName, right.PmsName, StringComparison.Ordinal)
            && string.Equals(left.FolderName, right.FolderName, StringComparison.Ordinal);
    }

    private static bool MetadataEquals(
        QaPmsMetadata left,
        QaPmsMetadata right)
    {
        return string.Equals(left.PmsName, right.PmsName, StringComparison.Ordinal)
            && string.Equals(left.FolderName, right.FolderName, StringComparison.Ordinal);
    }

    private static bool PathsEqual(
        IReadOnlyList<string> left,
        IReadOnlyList<string> right)
    {
        return left.Count == right.Count
            && left.Zip(right).All(pair => string.Equals(
                pair.First,
                pair.Second,
                StringComparison.OrdinalIgnoreCase));
    }

    private string CreateRelativeQaPath(string fullPath)
    {
        string relativePath = Path.GetRelativePath(
                paths.QaReportsRootPath,
                fullPath)
            .Replace('\\', '/');

        if (Path.IsPathRooted(relativePath)
            || relativePath.StartsWith("../", StringComparison.Ordinal)
            || relativePath is "." or ".."
            || relativePath.Split('/').Any(
                segment => segment is "" or "." or ".."))
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.InvalidDestination,
                QaReportSaveStage.Preparation,
                "A QA report destination could not be represented as a safe relative path.");
        }

        return relativePath;
    }

    private static void ValidateExistingMatches(
        IReadOnlyList<string> matches,
        string expectedDirectory,
        string expectedRoot,
        string locationName)
    {
        foreach (string match in matches)
        {
            ValidateApprovedPath(
                expectedRoot,
                match,
                $"existing {locationName} QA report");

            if (!string.Equals(
                    Path.GetDirectoryName(match),
                    expectedDirectory,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    Path.GetExtension(match),
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new QaReportSaveException(
                    QaReportSaveErrorCategory.InvalidDestination,
                    QaReportSaveStage.Preparation,
                    $"An existing {locationName} QA report is outside its canonical location.");
            }
        }
    }

    private static void EnsureExactTargetIsRecognized(
        string finalPath,
        IReadOnlyList<string> matches,
        string locationName)
    {
        bool targetExists = File.Exists(finalPath) || Directory.Exists(finalPath);
        bool targetRecognized = matches.Any(match => string.Equals(
            match,
            finalPath,
            StringComparison.OrdinalIgnoreCase));

        if (targetExists && !targetRecognized)
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.InvalidDestination,
                QaReportSaveStage.Preparation,
                $"The exact {locationName} target exists but is not a recognized QA report for this key.");
        }
    }

    private static void ValidateApprovedPath(
        string expectedRoot,
        string candidatePath,
        string description)
    {
        string normalizedRoot = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(expectedRoot));
        string normalizedCandidate = Path.GetFullPath(candidatePath);
        string rootWithSeparator = normalizedRoot.EndsWith(
                Path.DirectorySeparatorChar)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;

        if (!normalizedCandidate.StartsWith(
                rootWithSeparator,
                StringComparison.OrdinalIgnoreCase)
            || normalizedCandidate.Length
                > QaReportFilenameService.MaximumSupportedFullPathLength)
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.InvalidDestination,
                QaReportSaveStage.Preparation,
                $"The {description} is outside the supported canonical path policy.");
        }
    }

    private static string CreateTransactionPath(
        string directoryPath,
        string expectedRoot,
        string suffix)
    {
        string transactionPath = Path.GetFullPath(Path.Combine(
            directoryPath,
            $".qa-{Guid.NewGuid():N}{suffix}"));
        ValidateApprovedPath(
            expectedRoot,
            transactionPath,
            "QA report transaction path");

        if (transactionPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new QaReportSaveException(
                QaReportSaveErrorCategory.InvalidDestination,
                QaReportSaveStage.Preparation,
                "A QA report transaction path cannot use the PDF extension.");
        }

        return transactionPath;
    }

    private static void ValidatePdfPayload(ReadOnlyMemory<byte> pdfBytes)
    {
        ReadOnlySpan<byte> payload = pdfBytes.Span;

        if (payload.Length < 16
            || !payload[..5].SequenceEqual("%PDF-"u8)
            || payload.LastIndexOf("%%EOF"u8) < 0)
        {
            throw ContentVerificationFailure(
                "The supplied PDF payload is empty or structurally invalid.");
        }
    }

    private static QaReportSaveException ContentVerificationFailure(
        string message)
    {
        return new QaReportSaveException(
            QaReportSaveErrorCategory.ContentVerificationFailure,
            QaReportSaveStage.Preparation,
            message);
    }

    private static QaReportSaveException MetadataMismatch(string message)
    {
        return new QaReportSaveException(
            QaReportSaveErrorCategory.MetadataMismatch,
            QaReportSaveStage.Preparation,
            message);
    }

    private static string RequireTrimmedMetadataValue(
        string? value,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw MetadataMismatch(
                $"Current {fieldName} metadata is unavailable.");
        }

        return value.Trim();
    }

    private static QaReportSaveException CreatePreparationFailure(
        QaReportSaveErrorCategory category,
        string message,
        Exception innerException)
    {
        return new QaReportSaveException(
            category,
            QaReportSaveStage.Preparation,
            message,
            innerException,
            previousStateRestored: false,
            manualReviewRequired: false,
            manualReviewLocations: []);
    }

    private static bool IsFileInUse(IOException exception)
    {
        int nativeErrorCode = exception.HResult & 0xFFFF;
        return nativeErrorCode is 32 or 33;
    }

    private sealed class BackupRecord
    {
        public BackupRecord(string originalPath, string backupPath)
        {
            OriginalPath = originalPath;
            BackupPath = backupPath;
        }

        public string OriginalPath { get; }

        public string BackupPath { get; }

        public bool Moved { get; set; }
    }

    private sealed class TransactionState
    {
        public TransactionState(
            string hotelTemporaryPath,
            string pmsTemporaryPath,
            string indexTemporaryPath,
            string indexBackupPath,
            List<BackupRecord> hotelBackups,
            List<BackupRecord> pmsBackups)
        {
            HotelTemporaryPath = hotelTemporaryPath;
            PmsTemporaryPath = pmsTemporaryPath;
            IndexTemporaryPath = indexTemporaryPath;
            IndexBackupPath = indexBackupPath;
            HotelBackups = hotelBackups;
            PmsBackups = pmsBackups;
        }

        public string HotelTemporaryPath { get; }

        public string PmsTemporaryPath { get; }

        public string IndexTemporaryPath { get; }

        public string IndexBackupPath { get; }

        public List<BackupRecord> HotelBackups { get; }

        public List<BackupRecord> PmsBackups { get; }

        public bool HotelTemporaryCreated { get; set; }

        public bool PmsTemporaryCreated { get; set; }

        public bool IndexTemporaryCreated { get; set; }

        public bool IndexBackedUp { get; set; }

        public bool HotelCommitted { get; set; }

        public bool PmsCommitted { get; set; }

        public bool IndexCommitted { get; set; }
    }
}
