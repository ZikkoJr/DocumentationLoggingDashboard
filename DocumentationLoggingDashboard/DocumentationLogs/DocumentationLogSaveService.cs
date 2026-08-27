using System.Security.Cryptography;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.DocumentationLogs;

/// <summary>
/// Commits one immutable documentation-log event to every routed workbook, the
/// legacy-compatible index, and private sequence state as one guarded filesystem
/// transaction.
/// </summary>
public sealed class DocumentationLogSaveService
{
    private static readonly object SaveLock = new();

    private readonly DocumentationLogStoragePaths paths;
    private readonly DocumentationLogRoutingService routingService;
    private readonly DocumentationLogWorkbookFilenameService filenameService;
    private readonly DocumentationLogWorkbookService workbookService;
    private readonly DocumentationLogSequenceService sequenceService;
    private readonly DocumentationLogIndexService indexService;
    private readonly IDocumentationLogTransactionFileOperations fileOperations;
    private readonly TimeProvider timeProvider;

    public DocumentationLogSaveService(
        DocumentationLogStoragePaths paths,
        QaMetadataService metadataService)
        : this(paths, metadataService, TimeProvider.System)
    {
    }

    public DocumentationLogSaveService(
        DocumentationLogStoragePaths paths,
        QaMetadataService metadataService,
        TimeProvider timeProvider)
        : this(
            paths,
            new DocumentationLogRoutingService(metadataService),
            new DocumentationLogWorkbookFilenameService(paths),
            new DocumentationLogWorkbookService(paths),
            new DocumentationLogSequenceService(paths),
            new DocumentationLogIndexService(),
            new DocumentationLogTransactionFileOperations(),
            timeProvider)
    {
    }

    public DocumentationLogSaveService(
        DocumentationLogStoragePaths paths,
        DocumentationLogRoutingService routingService,
        DocumentationLogWorkbookService workbookService,
        DocumentationLogSequenceService sequenceService,
        DocumentationLogIndexService indexService,
        IDocumentationLogTransactionFileOperations fileOperations,
        TimeProvider timeProvider)
        : this(
            paths,
            routingService,
            new DocumentationLogWorkbookFilenameService(paths),
            workbookService,
            sequenceService,
            indexService,
            fileOperations,
            timeProvider)
    {
    }

    public DocumentationLogSaveService(
        DocumentationLogStoragePaths paths,
        DocumentationLogRoutingService routingService,
        DocumentationLogWorkbookFilenameService filenameService,
        DocumentationLogWorkbookService workbookService,
        DocumentationLogSequenceService sequenceService,
        DocumentationLogIndexService indexService,
        IDocumentationLogTransactionFileOperations fileOperations,
        TimeProvider timeProvider)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.routingService = routingService
            ?? throw new ArgumentNullException(nameof(routingService));
        this.filenameService = filenameService
            ?? throw new ArgumentNullException(nameof(filenameService));
        this.workbookService = workbookService
            ?? throw new ArgumentNullException(nameof(workbookService));
        this.sequenceService = sequenceService
            ?? throw new ArgumentNullException(nameof(sequenceService));
        this.indexService = indexService
            ?? throw new ArgumentNullException(nameof(indexService));
        this.fileOperations = fileOperations
            ?? throw new ArgumentNullException(nameof(fileOperations));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Resolves current metadata and computes the presently available candidate
    /// ID without reserving it or writing any file. Save always recomputes it.
    /// </summary>
    public DocumentationLogEvent CreatePreview(DocumentationLogSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (SaveLock)
        {
            ArtifactDescriptor? current = null;
            DocumentationLogSaveStage stage = DocumentationLogSaveStage.Validation;

            try
            {
                _ = ResolveRunningWorkbook(request, out _);
                stage = DocumentationLogSaveStage.MetadataResolution;
                ArtifactDescriptor hotelsMetadata = new(
                    ArtifactKind.MetadataGuard,
                    paths.HotelsMetadataFilePath);
                current = hotelsMetadata;
                FileBaseline hotelsMetadataBaseline = CaptureBaseline(
                    hotelsMetadata,
                    mustExist: true);
                ArtifactDescriptor pmsMetadata = new(
                    ArtifactKind.MetadataGuard,
                    paths.PmsSystemsMetadataFilePath);
                current = pmsMetadata;
                FileBaseline pmsMetadataBaseline = CaptureBaseline(
                    pmsMetadata,
                    mustExist: true);
                DocumentationLogResolvedDraft draft = routingService.ResolveDraft(request);
                current = hotelsMetadata;
                EnsureBaselineUnchanged(
                    hotelsMetadataBaseline,
                    requireExclusiveAccess: false);
                current = pmsMetadata;
                EnsureBaselineUnchanged(
                    pmsMetadataBaseline,
                    requireExclusiveAccess: false);
                DateTimeOffset timestamp = timeProvider.GetLocalNow();

                ArtifactDescriptor index = new(
                    ArtifactKind.Index,
                    paths.LogIndexFilePath);
                current = index;
                stage = DocumentationLogSaveStage.IndexPreparation;
                FileBaseline indexBaseline = CaptureBaseline(index, mustExist: false);

                ArtifactDescriptor sequence = new(
                    ArtifactKind.SequenceState,
                    paths.SequenceStateFilePath);
                current = sequence;
                stage = DocumentationLogSaveStage.SequencePreparation;
                FileBaseline sequenceBaseline = CaptureBaseline(sequence, mustExist: false);

                ArtifactDescriptor legacy = CreateLegacyGuard(
                    request.LogType,
                    timestamp);
                current = legacy;
                FileBaseline legacyBaseline = CaptureBaseline(legacy, mustExist: false);
                DocumentationLogSequencePlan plan = sequenceService.PrepareNext(
                    request.LogType,
                    timestamp,
                    sequenceBaseline.Exists,
                    sequenceBaseline.Content,
                    indexBaseline.Content,
                    legacyBaseline.Content);

                return CreateEvent(draft, plan.LogId, timestamp);
            }
            catch (Exception exception)
            {
                throw ClassifyFailure(exception, stage, current);
            }
        }
    }

    public DocumentationLogSaveResult Save(DocumentationLogSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (SaveLock)
        {
            return SaveCore(request);
        }
    }

    private DocumentationLogSaveResult SaveCore(
        DocumentationLogSaveRequest request)
    {
        DocumentationLogSaveStage stage = DocumentationLogSaveStage.Validation;
        ArtifactDescriptor? current = null;
        TransactionState? state = null;

        try
        {
            string runningPath = ResolveRunningWorkbook(
                request,
                out string runningFileName);

            stage = DocumentationLogSaveStage.MetadataResolution;
            ArtifactDescriptor hotelsMetadataDescriptor = new(
                ArtifactKind.MetadataGuard,
                paths.HotelsMetadataFilePath);
            current = hotelsMetadataDescriptor;
            FileBaseline hotelsMetadataBaseline = CaptureBaseline(
                hotelsMetadataDescriptor,
                mustExist: true);
            ArtifactDescriptor pmsMetadataDescriptor = new(
                ArtifactKind.MetadataGuard,
                paths.PmsSystemsMetadataFilePath);
            current = pmsMetadataDescriptor;
            FileBaseline pmsMetadataBaseline = CaptureBaseline(
                pmsMetadataDescriptor,
                mustExist: true);
            DocumentationLogResolvedDraft draft = routingService.ResolveDraft(request);
            current = hotelsMetadataDescriptor;
            EnsureBaselineUnchanged(
                hotelsMetadataBaseline,
                requireExclusiveAccess: false);
            current = pmsMetadataDescriptor;
            EnsureBaselineUnchanged(
                pmsMetadataBaseline,
                requireExclusiveAccess: false);
            DateTimeOffset timestamp = timeProvider.GetLocalNow();
            ResolvedDestinations destinations = ResolveDestinations(
                request.LogType,
                runningPath,
                draft);

            List<WorkbookPreparation> workbookPreparations = [];
            foreach (WorkbookDestination destination in destinations.Workbooks)
            {
                current = destination.Descriptor;
                stage = destination.Descriptor.Kind == ArtifactKind.RunningWorkbook
                    ? DocumentationLogSaveStage.RunningLoad
                    : DocumentationLogSaveStage.HistoryLoad;
                FileBaseline baseline = CaptureBaseline(
                    destination.Descriptor,
                    mustExist: destination.Descriptor.Kind
                        == ArtifactKind.RunningWorkbook);
                workbookPreparations.Add(new WorkbookPreparation(
                    destination,
                    baseline));
            }

            ArtifactDescriptor indexDescriptor = new(
                ArtifactKind.Index,
                paths.LogIndexFilePath);
            current = indexDescriptor;
            stage = DocumentationLogSaveStage.IndexPreparation;
            FileBaseline indexBaseline = CaptureBaseline(
                indexDescriptor,
                mustExist: false);

            ArtifactDescriptor sequenceDescriptor = new(
                ArtifactKind.SequenceState,
                paths.SequenceStateFilePath);
            current = sequenceDescriptor;
            stage = DocumentationLogSaveStage.SequencePreparation;
            FileBaseline sequenceBaseline = CaptureBaseline(
                sequenceDescriptor,
                mustExist: false);

            ArtifactDescriptor legacyDescriptor = CreateLegacyGuard(
                request.LogType,
                timestamp);
            current = legacyDescriptor;
            FileBaseline legacyBaseline = CaptureBaseline(
                legacyDescriptor,
                mustExist: false);

            DocumentationLogSequencePlan sequencePlan =
                sequenceService.PrepareNext(
                    request.LogType,
                    timestamp,
                    sequenceBaseline.Exists,
                    sequenceBaseline.Content,
                    indexBaseline.Content,
                    legacyBaseline.Content);
            DocumentationLogEvent logEvent = CreateEvent(
                draft,
                sequencePlan.LogId,
                timestamp);

            List<TransactionArtifact> artifacts = [];
            foreach (WorkbookPreparation preparation in workbookPreparations)
            {
                current = preparation.Destination.Descriptor;
                stage = current.Kind == ArtifactKind.RunningWorkbook
                    ? DocumentationLogSaveStage.RunningLoad
                    : DocumentationLogSaveStage.HistoryLoad;
                ReadOnlyMemory<byte>? source = null;
                if (preparation.Baseline.Exists)
                {
                    source = preparation.Baseline.Content;
                }

                DocumentationLogWorkbookBuildResult build =
                    workbookService.BuildAppend(
                        source,
                        preparation.Destination.Contract,
                        logEvent);
                artifacts.Add(TransactionArtifact.ForWorkbook(
                    preparation.Destination,
                    preparation.Baseline,
                    build));
            }

            current = indexDescriptor;
            stage = DocumentationLogSaveStage.IndexPreparation;
            DocumentationLogIndexUpdatePlan indexPlan = indexService.PrepareAppend(
                indexBaseline.Content,
                logEvent,
                destinations.RunningPath);
            artifacts.Add(TransactionArtifact.ForBytes(
                indexDescriptor,
                indexBaseline,
                indexPlan.UpdatedContent));

            current = sequenceDescriptor;
            stage = DocumentationLogSaveStage.SequencePreparation;
            artifacts.Add(TransactionArtifact.ForBytes(
                sequenceDescriptor,
                sequenceBaseline,
                sequencePlan.UpdatedStateContent));

            state = new TransactionState(
                artifacts,
                legacyBaseline,
                [hotelsMetadataBaseline, pmsMetadataBaseline],
                CreateOwnedPaths(artifacts));
            ExecuteTransaction(state, logEvent);
            string? cleanupWarning = CleanupAfterSuccess(state);

            return new DocumentationLogSaveResult(
                logEvent,
                runningFileName,
                destinations.RunningPath,
                destinations.HotelHistoryPaths,
                destinations.PmsHistoryPaths,
                paths.LogIndexFilePath,
                paths.SequenceStateFilePath,
                cleanupWarning);
        }
        catch (Exception exception)
        {
            if (state is null)
            {
                throw ClassifyFailure(exception, stage, current);
            }

            throw RollBackAndCreateFailure(state, exception);
        }
    }

    private void ExecuteTransaction(
        TransactionState state,
        DocumentationLogEvent logEvent)
    {
        foreach (TransactionArtifact artifact in state.Artifacts)
        {
            state.CurrentArtifact = artifact.Descriptor;
            state.CurrentStage = GetStagingStage(artifact.Descriptor.Kind);
            fileOperations.CreateDirectory(
                Path.GetDirectoryName(artifact.Descriptor.Path)!);
            artifact.StageOwned = true;
            fileOperations.WriteNewAndFlush(
                artifact.StagePath,
                artifact.ExpectedContent);
            VerifyArtifact(
                artifact,
                artifact.StagePath,
                logEvent,
                state.CurrentStage);
        }

        state.CurrentStage = DocumentationLogSaveStage.ConcurrencyCheck;
        foreach (TransactionArtifact artifact in state.Artifacts)
        {
            state.CurrentArtifact = artifact.Descriptor;
            EnsureBaselineUnchanged(
                artifact.Baseline,
                requireExclusiveAccess: artifact.Baseline.Exists);
            VerifyArtifact(
                artifact,
                artifact.StagePath,
                logEvent,
                DocumentationLogSaveStage.ConcurrencyCheck);
        }

        foreach (FileBaseline metadataGuard in state.MetadataGuards)
        {
            state.CurrentArtifact = metadataGuard.Descriptor;
            EnsureBaselineUnchanged(
                metadataGuard,
                requireExclusiveAccess: false);
        }

        AcquireMetadataGuardLeases(state);

        state.CurrentArtifact = state.LegacyGuard.Descriptor;
        EnsureBaselineUnchanged(
            state.LegacyGuard,
            requireExclusiveAccess: false);
        AcquireLegacyGuardLease(state);

        state.CurrentStage = DocumentationLogSaveStage.Backup;
        foreach (TransactionArtifact artifact in state.Artifacts)
        {
            state.CurrentArtifact = artifact.Descriptor;
            BackupArtifact(artifact);
        }

        // A legacy entry appearing after sequence preparation would make the
        // candidate ID stale. Existing legacy files remain leased; absence is
        // checked one last time before the first replacement commits.
        state.CurrentArtifact = state.LegacyGuard.Descriptor;
        ValidateLegacyGuard(state);

        state.CurrentStage = DocumentationLogSaveStage.Commit;
        foreach (TransactionArtifact artifact in state.Artifacts)
        {
            state.CurrentArtifact = artifact.Descriptor;
            if (fileOperations.FileExists(artifact.Descriptor.Path))
            {
                throw ConcurrentChange(artifact.Descriptor);
            }

            CommitArtifact(artifact);
        }

        state.CurrentStage = DocumentationLogSaveStage.FinalVerification;
        foreach (TransactionArtifact artifact in state.Artifacts)
        {
            state.CurrentArtifact = artifact.Descriptor;
            VerifyArtifact(
                artifact,
                artifact.Descriptor.Path,
                logEvent,
                DocumentationLogSaveStage.FinalVerification);
        }

        ValidateMetadataGuards(state);
        ValidateLegacyGuard(state);
    }

    private string ResolveRunningWorkbook(
        DocumentationLogSaveRequest request,
        out string runningFileName)
    {
        try
        {
            runningFileName = filenameService.ValidateStoredWorkbookFileName(
                request.RunningWorkbookFileName);
            return paths.ResolveRunningWorkbookPath(
                request.LogType,
                runningFileName);
        }
        catch (DocumentationLogWorkbookException exception)
        {
            throw new DocumentationLogSaveException(
                DocumentationLogSaveErrorCategory.InvalidDestination,
                DocumentationLogSaveStage.Validation,
                "Select a valid compatible Running workbook before saving.",
                exception);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            throw new DocumentationLogSaveException(
                DocumentationLogSaveErrorCategory.InvalidDestination,
                DocumentationLogSaveStage.Validation,
                "The selected Running workbook filename is not valid.",
                exception);
        }
    }

    private ResolvedDestinations ResolveDestinations(
        LogType logType,
        string runningPath,
        DocumentationLogResolvedDraft draft)
    {
        List<WorkbookDestination> workbooks = [];
        List<string> hotelPaths = [];
        List<string> pmsPaths = [];
        HashSet<string> uniquePaths = new(StringComparer.OrdinalIgnoreCase);

        AddWorkbook(
            workbooks,
            uniquePaths,
            new WorkbookDestination(
                new ArtifactDescriptor(
                    ArtifactKind.RunningWorkbook,
                    runningPath),
                DocumentationLogWorkbookSchema.CreateRunningContract(logType)));

        foreach (DocumentationLogHotel hotel in draft.Hotels)
        {
            string path = paths.ResolveHotelHistoryPath(
                logType,
                hotel.FolderName);
            AddWorkbook(
                workbooks,
                uniquePaths,
                new WorkbookDestination(
                    new ArtifactDescriptor(
                        ArtifactKind.HotelHistoryWorkbook,
                        path),
                    DocumentationLogWorkbookSchema.CreateHotelHistoryContract(
                        logType,
                        hotel)));
            hotelPaths.Add(path);
        }

        foreach (DocumentationLogPms pms in draft.UniquePmsSystems)
        {
            string path = paths.ResolvePmsHistoryPath(
                logType,
                pms.FolderName);
            AddWorkbook(
                workbooks,
                uniquePaths,
                new WorkbookDestination(
                    new ArtifactDescriptor(
                        ArtifactKind.PmsHistoryWorkbook,
                        path),
                    DocumentationLogWorkbookSchema.CreatePmsHistoryContract(
                        logType,
                        pms)));
            pmsPaths.Add(path);
        }

        return new ResolvedDestinations(
            runningPath,
            workbooks,
            hotelPaths,
            pmsPaths);
    }

    private static void AddWorkbook(
        ICollection<WorkbookDestination> workbooks,
        ISet<string> uniquePaths,
        WorkbookDestination destination)
    {
        if (!uniquePaths.Add(Path.GetFullPath(destination.Descriptor.Path)))
        {
            throw new DocumentationLogSaveException(
                DocumentationLogSaveErrorCategory.MetadataMismatch,
                DocumentationLogSaveStage.MetadataResolution,
                "Current Hotel/PMS metadata resolves two required workbook copies to the same path. Saving is blocked.",
                affectedPath: destination.Descriptor.Path);
        }

        workbooks.Add(destination);
    }

    private ArtifactDescriptor CreateLegacyGuard(
        LogType logType,
        DateTimeOffset timestamp)
    {
        return new ArtifactDescriptor(
            ArtifactKind.LegacyTxtGuard,
            paths.GetLegacyDailyLogFilePath(
                logType,
                timestamp.DateTime));
    }

    private FileBaseline CaptureBaseline(
        ArtifactDescriptor descriptor,
        bool mustExist)
    {
        try
        {
            QuickQaPathSafety.EnsureNoReparsePoints(
                paths.DocumentationRootPath,
                descriptor.Path);
            bool exists = fileOperations.FileExists(descriptor.Path);
            if (!exists)
            {
                if (mustExist)
                {
                    throw Unavailable(
                        descriptor,
                        GetLoadStage(descriptor.Kind),
                        innerException: null);
                }

                return new FileBaseline(
                    descriptor,
                    Exists: false,
                    Content: [],
                    Sha256: [],
                    Length: 0);
            }

            byte[] content = fileOperations.ReadAllBytes(descriptor.Path);
            return new FileBaseline(
                descriptor,
                Exists: true,
                content,
                SHA256.HashData(content),
                content.LongLength);
        }
        catch (DocumentationLogSaveException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw Unavailable(
                descriptor,
                GetLoadStage(descriptor.Kind),
                exception);
        }
    }

    private static DocumentationLogEvent CreateEvent(
        DocumentationLogResolvedDraft draft,
        string logId,
        DateTimeOffset timestamp)
    {
        return new DocumentationLogEvent(
            draft.LogType,
            logId,
            timestamp,
            draft.Hotels,
            draft.FieldValues);
    }

    private IReadOnlyDictionary<string, OwnedPaths> CreateOwnedPaths(
        IEnumerable<TransactionArtifact> artifacts)
    {
        string transactionId = Guid.NewGuid().ToString("N");
        Dictionary<string, OwnedPaths> result =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (TransactionArtifact artifact in artifacts)
        {
            string path = artifact.Descriptor.Path;
            string directory = Path.GetDirectoryName(path)!;
            string extension = Path.GetExtension(path);
            string stem = Path.GetFileNameWithoutExtension(path);
            string prefix = $".{stem}.documentationlog-{transactionId}";
            OwnedPaths owned = new(
                Path.Combine(directory, $"{prefix}.stage{extension}"),
                Path.Combine(directory, $"{prefix}.rollback{extension}"));
            artifact.StagePath = owned.StagePath;
            artifact.BackupPath = owned.BackupPath;
            result.Add(path, owned);
        }

        return result;
    }

    private void VerifyArtifact(
        TransactionArtifact artifact,
        string path,
        DocumentationLogEvent logEvent,
        DocumentationLogSaveStage stage)
    {
        VerifyExactBytes(
            path,
            artifact.ExpectedContent,
            artifact.Descriptor,
            stage);

        if (artifact.WorkbookDestination is null
            || artifact.WorkbookBuild is null)
        {
            return;
        }

        byte[] content = fileOperations.ReadAllBytes(path);
        workbookService.VerifyAppend(
            content,
            artifact.WorkbookDestination.Contract,
            logEvent,
            artifact.WorkbookBuild.AppendedRowNumber,
            artifact.WorkbookBuild.PreviousDataRowCount + 1);
    }

    private void VerifyExactBytes(
        string path,
        ReadOnlyMemory<byte> expected,
        ArtifactDescriptor descriptor,
        DocumentationLogSaveStage stage)
    {
        try
        {
            if (!fileOperations.FileExists(path)
                || fileOperations.GetFileLength(path) != expected.Length)
            {
                throw ContentVerificationFailure(
                    descriptor,
                    stage,
                    "A staged documentation-log artifact has an unexpected length.");
            }

            byte[] actualHash = fileOperations.ComputeSha256(path);
            byte[] expectedHash = SHA256.HashData(expected.Span);
            if (!CryptographicOperations.FixedTimeEquals(
                    actualHash,
                    expectedHash))
            {
                throw ContentVerificationFailure(
                    descriptor,
                    stage,
                    "A documentation-log artifact does not match its verified in-memory content.");
            }
        }
        catch (DocumentationLogSaveException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw Unavailable(descriptor, stage, exception);
        }
    }

    private void EnsureBaselineUnchanged(
        FileBaseline baseline,
        bool requireExclusiveAccess)
    {
        ArtifactDescriptor descriptor = baseline.Descriptor;

        try
        {
            bool exists = fileOperations.FileExists(descriptor.Path);
            if (exists != baseline.Exists)
            {
                throw ConcurrentChange(descriptor);
            }

            if (!baseline.Exists)
            {
                return;
            }

            if (fileOperations.GetFileLength(descriptor.Path) != baseline.Length)
            {
                throw ConcurrentChange(descriptor);
            }

            byte[] hash = fileOperations.ComputeSha256(descriptor.Path);
            if (!CryptographicOperations.FixedTimeEquals(
                    baseline.Sha256,
                    hash))
            {
                throw ConcurrentChange(descriptor);
            }

            if (requireExclusiveAccess)
            {
                fileOperations.VerifyExclusiveAccess(descriptor.Path);
            }
        }
        catch (DocumentationLogSaveException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw Unavailable(
                descriptor,
                DocumentationLogSaveStage.ConcurrencyCheck,
                exception);
        }
    }

    private void AcquireMetadataGuardLeases(TransactionState state)
    {
        foreach (FileBaseline baseline in state.MetadataGuards)
        {
            state.CurrentArtifact = baseline.Descriptor;

            try
            {
                IDocumentationLogExclusiveReadLease lease =
                    fileOperations.OpenExclusiveReadLease(
                        baseline.Descriptor.Path);
                MetadataGuardLease held = new(baseline, lease);
                state.MetadataGuardLeases.Add(held);
                ValidateLease(
                    held.Lease,
                    held.Baseline,
                    DocumentationLogSaveStage.ConcurrencyCheck);
            }
            catch (DocumentationLogSaveException)
            {
                throw;
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw Unavailable(
                    baseline.Descriptor,
                    DocumentationLogSaveStage.ConcurrencyCheck,
                    exception);
            }
        }
    }

    private static void ValidateMetadataGuards(TransactionState state)
    {
        foreach (MetadataGuardLease held in state.MetadataGuardLeases)
        {
            state.CurrentArtifact = held.Baseline.Descriptor;
            ValidateLease(
                held.Lease,
                held.Baseline,
                DocumentationLogSaveStage.ConcurrencyCheck);
        }
    }

    private void AcquireLegacyGuardLease(TransactionState state)
    {
        if (!state.LegacyGuard.Exists)
        {
            return;
        }

        try
        {
            state.LegacyGuardLease = fileOperations.OpenExclusiveReadLease(
                state.LegacyGuard.Descriptor.Path);
            ValidateLease(
                state.LegacyGuardLease,
                state.LegacyGuard,
                DocumentationLogSaveStage.ConcurrencyCheck);
        }
        catch (DocumentationLogSaveException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw Unavailable(
                state.LegacyGuard.Descriptor,
                DocumentationLogSaveStage.ConcurrencyCheck,
                exception);
        }
    }

    private void ValidateLegacyGuard(TransactionState state)
    {
        if (state.LegacyGuardLease is not null)
        {
            ValidateLease(
                state.LegacyGuardLease,
                state.LegacyGuard,
                DocumentationLogSaveStage.ConcurrencyCheck);
            return;
        }

        EnsureBaselineUnchanged(
            state.LegacyGuard,
            requireExclusiveAccess: false);
    }

    private void BackupArtifact(TransactionArtifact artifact)
    {
        EnsureBaselineUnchanged(
            artifact.Baseline,
            requireExclusiveAccess: artifact.Baseline.Exists);

        if (!artifact.Baseline.Exists)
        {
            return;
        }

        if (fileOperations.FileExists(artifact.BackupPath))
        {
            throw new DocumentationLogSaveException(
                DocumentationLogSaveErrorCategory.TransactionFailure,
                DocumentationLogSaveStage.Backup,
                "A private documentation-log backup name unexpectedly already exists.",
                affectedPath: artifact.BackupPath);
        }

        artifact.BackupAttempted = true;
        try
        {
            fileOperations.Move(
                artifact.Descriptor.Path,
                artifact.BackupPath);
            artifact.BackedUp = true;
        }
        catch
        {
            ReconcileBackupAttempt(artifact);
            throw;
        }

        artifact.BackupLease = fileOperations.OpenExclusiveReadLease(
            artifact.BackupPath);
        ValidateLease(
            artifact.BackupLease,
            artifact.Baseline,
            DocumentationLogSaveStage.Backup);
    }

    private void ReconcileBackupAttempt(TransactionArtifact artifact)
    {
        try
        {
            artifact.BackedUp = fileOperations.FileExists(
                artifact.BackupPath);
        }
        catch
        {
            // Rollback will report the ambiguity if the original cannot be
            // restored. Do not hide the triggering backup failure here.
        }
    }

    private static void ValidateLease(
        IDocumentationLogExclusiveReadLease lease,
        FileBaseline baseline,
        DocumentationLogSaveStage stage)
    {
        if (lease.Length != baseline.Length)
        {
            throw ConcurrentChange(baseline.Descriptor, stage);
        }

        byte[] hash = lease.ComputeSha256();
        if (!CryptographicOperations.FixedTimeEquals(
                baseline.Sha256,
                hash))
        {
            throw ConcurrentChange(baseline.Descriptor, stage);
        }
    }

    private void CommitArtifact(TransactionArtifact artifact)
    {
        artifact.CommitAttempted = true;

        try
        {
            fileOperations.Move(
                artifact.StagePath,
                artifact.Descriptor.Path);
            artifact.StageOwned = false;
            artifact.Committed = true;
        }
        catch
        {
            ReconcileCommitAttempt(artifact);
            throw;
        }
    }

    private void ReconcileCommitAttempt(TransactionArtifact artifact)
    {
        try
        {
            bool stageExists = fileOperations.FileExists(artifact.StagePath);
            bool destinationExists = fileOperations.FileExists(
                artifact.Descriptor.Path);
            if (!stageExists
                && destinationExists
                && fileOperations.GetFileLength(artifact.Descriptor.Path)
                    == artifact.ExpectedContent.LongLength)
            {
                byte[] actual = fileOperations.ComputeSha256(
                    artifact.Descriptor.Path);
                byte[] expected = SHA256.HashData(artifact.ExpectedContent);
                if (CryptographicOperations.FixedTimeEquals(actual, expected))
                {
                    artifact.StageOwned = false;
                    artifact.Committed = true;
                }
            }
        }
        catch
        {
            // Preserve the original commit failure. An ambiguous replacement
            // is surfaced as manual-review state if rollback cannot resolve it.
        }
    }

    private string? CleanupAfterSuccess(TransactionState state)
    {
        List<string> leftovers = [];

        ReleaseLeaseForCleanup(
            state.LegacyGuardLease,
            state.LegacyGuard.Descriptor.Path,
            leftovers);
        state.LegacyGuardLease = null;

        foreach (MetadataGuardLease held in state.MetadataGuardLeases.AsEnumerable().Reverse())
        {
            ReleaseLeaseForCleanup(
                held.Lease,
                held.Baseline.Descriptor.Path,
                leftovers);
        }

        state.MetadataGuardLeases.Clear();

        foreach (TransactionArtifact artifact in state.Artifacts.Reverse())
        {
            ReleaseLeaseForCleanup(
                artifact.BackupLease,
                artifact.BackupPath,
                leftovers);
            artifact.BackupLease = null;
        }

        foreach (TransactionArtifact artifact in state.Artifacts.Reverse())
        {
            if (artifact.BackedUp)
            {
                TryCleanup(artifact.BackupPath, leftovers);
            }

            if (artifact.StageOwned)
            {
                TryCleanup(artifact.StagePath, leftovers);
            }
        }

        if (leftovers.Count == 0)
        {
            return null;
        }

        return "The documentation log was saved, but transaction cleanup could not remove: "
            + string.Join(
                ", ",
                leftovers.Select(Path.GetFileName).Distinct().Take(6));
    }

    private DocumentationLogSaveException RollBackAndCreateFailure(
        TransactionState state,
        Exception originalFailure)
    {
        List<Exception> rollbackFailures = [];
        ReconcileStateForRollback(state, rollbackFailures);
        bool stateChanged = state.Artifacts.Any(artifact =>
            artifact.BackedUp || artifact.Committed);

        ReleaseLeaseForRollback(state.LegacyGuardLease, rollbackFailures);
        state.LegacyGuardLease = null;

        foreach (MetadataGuardLease held in state.MetadataGuardLeases.AsEnumerable().Reverse())
        {
            ReleaseLeaseForRollback(held.Lease, rollbackFailures);
        }

        state.MetadataGuardLeases.Clear();

        foreach (TransactionArtifact artifact in state.Artifacts.Reverse())
        {
            ReleaseLeaseForRollback(
                artifact.BackupLease,
                rollbackFailures);
            artifact.BackupLease = null;
        }

        foreach (TransactionArtifact artifact in state.Artifacts.Reverse())
        {
            if (artifact.Committed)
            {
                TryRollback(
                    () => fileOperations.Delete(artifact.Descriptor.Path),
                    rollbackFailures);
            }
        }

        foreach (TransactionArtifact artifact in state.Artifacts.Reverse())
        {
            if (artifact.BackedUp)
            {
                TryRollback(
                    () => fileOperations.Move(
                        artifact.BackupPath,
                        artifact.Descriptor.Path),
                    rollbackFailures);
            }
        }

        foreach (TransactionArtifact artifact in state.Artifacts.Reverse())
        {
            if (artifact.StageOwned)
            {
                TryRollback(
                    () => fileOperations.Delete(artifact.StagePath),
                    rollbackFailures);
            }
        }

        if (rollbackFailures.Count != 0)
        {
            return new DocumentationLogSaveException(
                DocumentationLogSaveErrorCategory.RollbackFailure,
                DocumentationLogSaveStage.Rollback,
                "The documentation-log save failed and rollback was incomplete. Review the listed workbook/index/state locations before retrying.",
                new AggregateException(
                    new[] { originalFailure }.Concat(rollbackFailures)),
                affectedPath: state.CurrentArtifact?.Path,
                previousStateRestored: false,
                manualReviewRequired: true,
                manualReviewLocations: GetManualReviewLocations(state));
        }

        DocumentationLogSaveException classified = ClassifyFailure(
            originalFailure,
            state.CurrentStage,
            state.CurrentArtifact);
        return new DocumentationLogSaveException(
            classified.ErrorCategory,
            classified.Stage,
            classified.Message,
            classified,
            classified.AffectedPath,
            previousStateRestored: stateChanged,
            manualReviewRequired: false,
            manualReviewLocations: []);
    }

    private void ReconcileStateForRollback(
        TransactionState state,
        ICollection<Exception> failures)
    {
        foreach (TransactionArtifact artifact in state.Artifacts.Reverse())
        {
            if (artifact.CommitAttempted && !artifact.Committed)
            {
                try
                {
                    ReconcileCommitAttempt(artifact);
                    bool stageExists = fileOperations.FileExists(
                        artifact.StagePath);
                    bool destinationExists = fileOperations.FileExists(
                        artifact.Descriptor.Path);
                    if (!artifact.Committed
                        && !stageExists
                        && destinationExists)
                    {
                        failures.Add(new IOException(
                            $"The commit outcome for '{Path.GetFileName(artifact.Descriptor.Path)}' is ambiguous."));
                    }
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }

            if (artifact.BackupAttempted && !artifact.BackedUp)
            {
                try
                {
                    bool backupExists = fileOperations.FileExists(
                        artifact.BackupPath);
                    bool originalExists = fileOperations.FileExists(
                        artifact.Descriptor.Path);
                    artifact.BackedUp = backupExists;
                    if (!backupExists && !originalExists)
                    {
                        failures.Add(new IOException(
                            $"The backup outcome for '{Path.GetFileName(artifact.Descriptor.Path)}' is ambiguous."));
                    }
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }
    }

    private static IReadOnlyList<string> GetManualReviewLocations(
        TransactionState state)
    {
        return state.Artifacts
            .SelectMany(artifact => new[]
            {
                artifact.Descriptor.Path,
                artifact.StagePath,
                artifact.BackupPath
            })
            .Append(state.LegacyGuard.Descriptor.Path)
            .Concat(state.MetadataGuards.Select(guard => guard.Descriptor.Path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private DocumentationLogSaveException ClassifyFailure(
        Exception exception,
        DocumentationLogSaveStage stage,
        ArtifactDescriptor? artifact)
    {
        if (exception is DocumentationLogSaveException saveException)
        {
            if (saveException.AffectedPath is not null || artifact is null)
            {
                return saveException;
            }

            return new DocumentationLogSaveException(
                saveException.ErrorCategory,
                saveException.Stage,
                saveException.Message,
                saveException,
                artifact.Path,
                saveException.PreviousStateRestored,
                saveException.ManualReviewRequired,
                saveException.ManualReviewLocations);
        }

        if (exception is DocumentationLogWorkbookException workbookException)
        {
            DocumentationLogSaveErrorCategory category =
                workbookException.Category switch
                {
                    DocumentationLogWorkbookErrorCategory.InvalidFilename =>
                        DocumentationLogSaveErrorCategory.InvalidDestination,
                    DocumentationLogWorkbookErrorCategory.InUseOrUnavailable =>
                        IsHistory(artifact?.Kind)
                            ? DocumentationLogSaveErrorCategory.HistoryWorkbookUnavailable
                            : DocumentationLogSaveErrorCategory.RunningWorkbookUnavailable,
                    DocumentationLogWorkbookErrorCategory.ContentVerificationFailure =>
                        DocumentationLogSaveErrorCategory.ContentVerificationFailure,
                    _ => DocumentationLogSaveErrorCategory.WorkbookInvalid
                };
            return new DocumentationLogSaveException(
                category,
                stage,
                workbookException.Message,
                workbookException,
                artifact?.Path ?? workbookException.WorkbookPath);
        }

        if (exception is UnauthorizedAccessException
            or System.Security.SecurityException)
        {
            return new DocumentationLogSaveException(
                DocumentationLogSaveErrorCategory.PermissionDenied,
                stage,
                "The documentation-log transaction does not have permission to use the configured storage location.",
                exception,
                artifact?.Path);
        }

        if (exception is IOException
            or NotSupportedException)
        {
            return Unavailable(artifact, stage, exception);
        }

        return new DocumentationLogSaveException(
            DocumentationLogSaveErrorCategory.TransactionFailure,
            stage,
            "The documentation-log transaction did not complete.",
            exception,
            artifact?.Path);
    }

    private static DocumentationLogSaveException Unavailable(
        ArtifactDescriptor? artifact,
        DocumentationLogSaveStage stage,
        Exception? innerException)
    {
        ArtifactKind kind = artifact?.Kind ?? ArtifactKind.Unknown;
        DocumentationLogSaveErrorCategory category = kind switch
        {
            ArtifactKind.RunningWorkbook =>
                DocumentationLogSaveErrorCategory.RunningWorkbookUnavailable,
            ArtifactKind.HotelHistoryWorkbook or ArtifactKind.PmsHistoryWorkbook =>
                DocumentationLogSaveErrorCategory.HistoryWorkbookUnavailable,
            ArtifactKind.Index => DocumentationLogSaveErrorCategory.IndexUnavailable,
            ArtifactKind.MetadataGuard =>
                DocumentationLogSaveErrorCategory.MetadataMismatch,
            ArtifactKind.SequenceState or ArtifactKind.LegacyTxtGuard =>
                DocumentationLogSaveErrorCategory.SequenceStateInvalid,
            _ => DocumentationLogSaveErrorCategory.TransactionFailure
        };
        string message = kind switch
        {
            ArtifactKind.RunningWorkbook =>
                "The selected Running workbook is open, missing, or unavailable. Close it or select another compatible workbook and try again.",
            ArtifactKind.HotelHistoryWorkbook or ArtifactKind.PmsHistoryWorkbook =>
                "A required Hotel/PMS history workbook is open or unavailable. Close it and try again.",
            ArtifactKind.Index =>
                "LogIndex.txt is open or unavailable. Close it and try again.",
            ArtifactKind.MetadataGuard =>
                "Current QA Hotel/PMS metadata changed or became unavailable while routing was being confirmed. Refresh and try again.",
            ArtifactKind.SequenceState =>
                "The documentation-log sequence state is open or unavailable. Close it and try again.",
            ArtifactKind.LegacyTxtGuard =>
                "The current legacy TXT history changed or became unavailable while the next Log ID was being confirmed. Try again.",
            _ => "A documentation-log transaction file is unavailable."
        };

        return new DocumentationLogSaveException(
            category,
            stage,
            message,
            innerException,
            artifact?.Path);
    }

    private static DocumentationLogSaveException ConcurrentChange(
        ArtifactDescriptor descriptor,
        DocumentationLogSaveStage stage =
            DocumentationLogSaveStage.ConcurrencyCheck)
    {
        return new DocumentationLogSaveException(
            descriptor.Kind == ArtifactKind.MetadataGuard
                ? DocumentationLogSaveErrorCategory.MetadataMismatch
                : DocumentationLogSaveErrorCategory.ConcurrentChange,
            stage,
            descriptor.Kind switch
            {
                ArtifactKind.LegacyTxtGuard =>
                    "The current legacy TXT history changed while the next Log ID was being confirmed. No update was kept.",
                ArtifactKind.MetadataGuard =>
                    "Current QA Hotel/PMS metadata changed while the save was being routed. No update was kept; refresh and try again.",
                _ =>
                    "A documentation-log destination changed while this save was staged. No update was kept."
            },
            affectedPath: descriptor.Path);
    }

    private static DocumentationLogSaveException ContentVerificationFailure(
        ArtifactDescriptor descriptor,
        DocumentationLogSaveStage stage,
        string message)
    {
        return new DocumentationLogSaveException(
            DocumentationLogSaveErrorCategory.ContentVerificationFailure,
            stage,
            message,
            affectedPath: descriptor.Path);
    }

    private static DocumentationLogSaveStage GetLoadStage(ArtifactKind kind)
    {
        return kind switch
        {
            ArtifactKind.RunningWorkbook => DocumentationLogSaveStage.RunningLoad,
            ArtifactKind.HotelHistoryWorkbook or ArtifactKind.PmsHistoryWorkbook =>
                DocumentationLogSaveStage.HistoryLoad,
            ArtifactKind.MetadataGuard =>
                DocumentationLogSaveStage.MetadataResolution,
            ArtifactKind.Index => DocumentationLogSaveStage.IndexPreparation,
            ArtifactKind.SequenceState or ArtifactKind.LegacyTxtGuard =>
                DocumentationLogSaveStage.SequencePreparation,
            _ => DocumentationLogSaveStage.Validation
        };
    }

    private static DocumentationLogSaveStage GetStagingStage(ArtifactKind kind)
    {
        return kind switch
        {
            ArtifactKind.Index => DocumentationLogSaveStage.IndexStaging,
            ArtifactKind.SequenceState => DocumentationLogSaveStage.SequenceStaging,
            _ => DocumentationLogSaveStage.WorkbookStaging
        };
    }

    private static bool IsHistory(ArtifactKind? kind)
    {
        return kind is ArtifactKind.HotelHistoryWorkbook
            or ArtifactKind.PmsHistoryWorkbook;
    }

    private void TryCleanup(string path, ICollection<string> leftovers)
    {
        try
        {
            fileOperations.Delete(path);
        }
        catch
        {
            leftovers.Add(path);
        }
    }

    private static void ReleaseLeaseForCleanup(
        IDisposable? lease,
        string location,
        ICollection<string> leftovers)
    {
        if (lease is null)
        {
            return;
        }

        try
        {
            lease.Dispose();
        }
        catch
        {
            leftovers.Add(location);
        }
    }

    private static void ReleaseLeaseForRollback(
        IDisposable? lease,
        ICollection<Exception> failures)
    {
        if (lease is not null)
        {
            TryRollback(lease.Dispose, failures);
        }
    }

    private static void TryRollback(
        Action action,
        ICollection<Exception> failures)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
    }

    private static bool IsFileSystemException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException;
    }

    private enum ArtifactKind
    {
        Unknown,
        RunningWorkbook,
        HotelHistoryWorkbook,
        PmsHistoryWorkbook,
        MetadataGuard,
        Index,
        SequenceState,
        LegacyTxtGuard
    }

    private sealed class ArtifactDescriptor
    {
        public ArtifactDescriptor(ArtifactKind kind, string path)
        {
            Kind = kind;
            Path = System.IO.Path.GetFullPath(path);
        }

        public ArtifactKind Kind { get; }

        public string Path { get; }
    }

    private sealed record FileBaseline(
        ArtifactDescriptor Descriptor,
        bool Exists,
        byte[] Content,
        byte[] Sha256,
        long Length);

    private sealed record MetadataGuardLease(
        FileBaseline Baseline,
        IDocumentationLogExclusiveReadLease Lease);

    private sealed record WorkbookDestination(
        ArtifactDescriptor Descriptor,
        DocumentationLogWorkbookContract Contract);

    private sealed record WorkbookPreparation(
        WorkbookDestination Destination,
        FileBaseline Baseline);

    private sealed record ResolvedDestinations(
        string RunningPath,
        IReadOnlyList<WorkbookDestination> Workbooks,
        IReadOnlyList<string> HotelHistoryPaths,
        IReadOnlyList<string> PmsHistoryPaths);

    private sealed record OwnedPaths(
        string StagePath,
        string BackupPath);

    private sealed class TransactionArtifact
    {
        private TransactionArtifact(
            ArtifactDescriptor descriptor,
            FileBaseline baseline,
            byte[] expectedContent,
            WorkbookDestination? workbookDestination,
            DocumentationLogWorkbookBuildResult? workbookBuild)
        {
            Descriptor = descriptor;
            Baseline = baseline;
            ExpectedContent = expectedContent;
            WorkbookDestination = workbookDestination;
            WorkbookBuild = workbookBuild;
        }

        public ArtifactDescriptor Descriptor { get; }

        public FileBaseline Baseline { get; }

        public byte[] ExpectedContent { get; }

        public WorkbookDestination? WorkbookDestination { get; }

        public DocumentationLogWorkbookBuildResult? WorkbookBuild { get; }

        public string StagePath { get; set; } = string.Empty;

        public string BackupPath { get; set; } = string.Empty;

        public bool StageOwned { get; set; }

        public bool BackupAttempted { get; set; }

        public bool BackedUp { get; set; }

        public IDocumentationLogExclusiveReadLease? BackupLease { get; set; }

        public bool CommitAttempted { get; set; }

        public bool Committed { get; set; }

        public static TransactionArtifact ForWorkbook(
            WorkbookDestination destination,
            FileBaseline baseline,
            DocumentationLogWorkbookBuildResult build)
        {
            return new TransactionArtifact(
                destination.Descriptor,
                baseline,
                build.Content,
                destination,
                build);
        }

        public static TransactionArtifact ForBytes(
            ArtifactDescriptor descriptor,
            FileBaseline baseline,
            byte[] expectedContent)
        {
            return new TransactionArtifact(
                descriptor,
                baseline,
                expectedContent,
                workbookDestination: null,
                workbookBuild: null);
        }
    }

    private sealed class TransactionState
    {
        public TransactionState(
            IReadOnlyList<TransactionArtifact> artifacts,
            FileBaseline legacyGuard,
            IReadOnlyList<FileBaseline> metadataGuards,
            IReadOnlyDictionary<string, OwnedPaths> ownedPaths)
        {
            Artifacts = artifacts;
            LegacyGuard = legacyGuard;
            MetadataGuards = metadataGuards;
            OwnedPaths = ownedPaths;
        }

        public IReadOnlyList<TransactionArtifact> Artifacts { get; }

        public FileBaseline LegacyGuard { get; }

        public IReadOnlyList<FileBaseline> MetadataGuards { get; }

        public List<MetadataGuardLease> MetadataGuardLeases { get; } = [];

        public IReadOnlyDictionary<string, OwnedPaths> OwnedPaths { get; }

        public IDocumentationLogExclusiveReadLease? LegacyGuardLease { get; set; }

        public DocumentationLogSaveStage CurrentStage { get; set; } =
            DocumentationLogSaveStage.WorkbookStaging;

        public ArtifactDescriptor? CurrentArtifact { get; set; }
    }
}
