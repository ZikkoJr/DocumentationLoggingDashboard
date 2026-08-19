using System.Globalization;
using System.Security.Cryptography;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Appends one Quick QA event to the selected Surface workbook and canonical
/// Hotel history workbook as one staged, verified, rollback-capable transaction.
/// </summary>
public sealed class QuickQaSaveService
{
    private static readonly object SaveLock = new();

    private readonly QaStoragePaths paths;
    private readonly QaMetadataService metadataService;
    private readonly QuickQaWorkbookFilenameService filenameService;
    private readonly QuickQaWorkbookService workbookService;
    private readonly IQuickQaTransactionFileOperations fileOperations;
    private readonly QuickQaFindingSynchronizationService findingService;
    private readonly QuickQaSummaryService summaryService;
    private readonly TimeProvider timeProvider;

    public QuickQaSaveService(
        QaStoragePaths paths,
        QaMetadataService metadataService,
        TimeProvider? timeProvider = null)
        : this(
            paths,
            metadataService,
            new QuickQaWorkbookFilenameService(paths),
            new QuickQaWorkbookService(paths),
            new QuickQaTransactionFileOperations(),
            new QuickQaFindingSynchronizationService(),
            new QuickQaSummaryService(),
            timeProvider ?? TimeProvider.System)
    {
    }

    public QuickQaSaveService(
        QaStoragePaths paths,
        QaMetadataService metadataService,
        QuickQaWorkbookFilenameService filenameService,
        QuickQaWorkbookService workbookService,
        IQuickQaTransactionFileOperations fileOperations,
        QuickQaFindingSynchronizationService findingService,
        QuickQaSummaryService summaryService,
        TimeProvider timeProvider)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.metadataService = metadataService
            ?? throw new ArgumentNullException(nameof(metadataService));
        this.filenameService = filenameService
            ?? throw new ArgumentNullException(nameof(filenameService));
        this.workbookService = workbookService
            ?? throw new ArgumentNullException(nameof(workbookService));
        this.fileOperations = fileOperations
            ?? throw new ArgumentNullException(nameof(fileOperations));
        this.findingService = findingService
            ?? throw new ArgumentNullException(nameof(findingService));
        this.summaryService = summaryService
            ?? throw new ArgumentNullException(nameof(summaryService));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public QuickQaSaveResult Save(
        QuickQaReport report,
        string surfaceWorkbookFileName)
    {
        if (string.IsNullOrWhiteSpace(surfaceWorkbookFileName))
        {
            throw new QuickQaSaveException(
                QuickQaSaveErrorCategory.InvalidDestination,
                QuickQaSaveStage.MetadataResolution,
                "Select a compatible Surface QA workbook before saving.");
        }

        return Save(new QuickQaSaveRequest(
            report,
            surfaceWorkbookFileName));
    }

    public QuickQaSaveResult Save(QuickQaSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (SaveLock)
        {
            return SaveCore(request);
        }
    }

    private QuickQaSaveResult SaveCore(QuickQaSaveRequest request)
    {
        QuickQaReport report = request.Report;
        ValidateAndSynchronizeReport(report);

        // One timestamp is captured after report validation and reused verbatim
        // in the Hotel history row and result object.
        DateTimeOffset timestamp = timeProvider.GetLocalNow();
        CanonicalMetadata canonical = ResolveCanonicalMetadata(report);
        ResolvedDestinations destinations = ResolveDestinations(
            request.SurfaceWorkbookFileName,
            canonical.Hotel.FolderName);
        QuickQaWorkbookRow row = CreateRow(
            report,
            canonical,
            timestamp);

        QuickQaSaveStage stage = QuickQaSaveStage.SurfaceLoad;
        TransactionState? state = null;

        try
        {
            QuickQaFileBaseline surfaceBaseline = CaptureBaseline(
                destinations.SurfacePath,
                mustExist: true,
                QuickQaWorkbookRole.Surface);

            stage = QuickQaSaveStage.HistoryLoad;
            QuickQaFileBaseline historyBaseline = CaptureBaseline(
                destinations.HistoryPath,
                mustExist: false,
                QuickQaWorkbookRole.HotelHistory);

            QuickQaWorkbookBuildResult surfaceBuild =
                workbookService.BuildSurfaceAppend(
                    surfaceBaseline.Content,
                    row);
            ReadOnlyMemory<byte>? historyContent = null;
            if (historyBaseline.Exists)
            {
                historyContent = new ReadOnlyMemory<byte>(
                    historyBaseline.Content);
            }

            QuickQaWorkbookBuildResult historyBuild =
                workbookService.BuildHistoryAppend(
                    historyContent,
                    row);

            state = CreateTransactionState(
                destinations,
                surfaceBaseline,
                historyBaseline,
                surfaceBuild,
                historyBuild);

            stage = QuickQaSaveStage.SurfaceStaging;
            fileOperations.CreateDirectory(
                Path.GetDirectoryName(destinations.SurfacePath)!);
            fileOperations.WriteNewAndFlush(
                state.SurfaceStagePath,
                surfaceBuild.Content);
            state.SurfaceStageCreated = true;
            VerifyStagedFile(
                state.SurfaceStagePath,
                surfaceBuild.Content,
                QuickQaWorkbookRole.Surface,
                row,
                surfaceBuild);

            stage = QuickQaSaveStage.HistoryStaging;
            fileOperations.CreateDirectory(
                Path.GetDirectoryName(destinations.HistoryPath)!);
            fileOperations.WriteNewAndFlush(
                state.HistoryStagePath,
                historyBuild.Content);
            state.HistoryStageCreated = true;
            VerifyStagedFile(
                state.HistoryStagePath,
                historyBuild.Content,
                QuickQaWorkbookRole.HotelHistory,
                row,
                historyBuild);

            stage = QuickQaSaveStage.ConcurrencyCheck;
            EnsureBaselineUnchanged(
                surfaceBaseline,
                QuickQaWorkbookRole.Surface,
                requireExclusiveAccess: true);
            EnsureBaselineUnchanged(
                historyBaseline,
                QuickQaWorkbookRole.HotelHistory,
                requireExclusiveAccess: historyBaseline.Exists);

            stage = QuickQaSaveStage.Backup;
            EnsureBaselineUnchanged(
                surfaceBaseline,
                QuickQaWorkbookRole.Surface,
                requireExclusiveAccess: true);
            MoveForBackup(
                destinations.SurfacePath,
                state.SurfaceRollbackPath,
                QuickQaWorkbookRole.Surface);
            state.SurfaceBackedUp = true;
            state.SurfaceRollbackLease = OpenRollbackLease(
                state.SurfaceRollbackPath,
                QuickQaWorkbookRole.Surface);
            ValidateRollbackLease(
                state.SurfaceRollbackLease,
                surfaceBaseline,
                QuickQaWorkbookRole.Surface);

            // The second destination is checked again after the first backup so
            // an external edit in this narrow window cannot be overwritten.
            EnsureBaselineUnchanged(
                historyBaseline,
                QuickQaWorkbookRole.HotelHistory,
                requireExclusiveAccess: historyBaseline.Exists);
            if (historyBaseline.Exists)
            {
                MoveForBackup(
                    destinations.HistoryPath,
                    state.HistoryRollbackPath,
                    QuickQaWorkbookRole.HotelHistory);
                state.HistoryBackedUp = true;
                state.HistoryRollbackLease = OpenRollbackLease(
                    state.HistoryRollbackPath,
                    QuickQaWorkbookRole.HotelHistory);
                ValidateRollbackLease(
                    state.HistoryRollbackLease,
                    historyBaseline,
                    QuickQaWorkbookRole.HotelHistory);
            }

            stage = QuickQaSaveStage.SurfaceCommit;
            fileOperations.Move(
                state.SurfaceStagePath,
                destinations.SurfacePath);
            state.SurfaceStageCreated = false;
            state.SurfaceCommitted = true;

            stage = QuickQaSaveStage.HistoryCommit;
            fileOperations.Move(
                state.HistoryStagePath,
                destinations.HistoryPath);
            state.HistoryStageCreated = false;
            state.HistoryCommitted = true;

            stage = QuickQaSaveStage.FinalVerification;
            VerifyFinalFile(
                destinations.SurfacePath,
                surfaceBuild.Content,
                QuickQaWorkbookRole.Surface,
                row,
                surfaceBuild);
            VerifyFinalFile(
                destinations.HistoryPath,
                historyBuild.Content,
                QuickQaWorkbookRole.HotelHistory,
                row,
                historyBuild);

            string? cleanupWarning = CleanupAfterSuccess(state);

            return new QuickQaSaveResult(
                destinations.SurfaceFileName,
                destinations.SurfacePath,
                destinations.HistoryPath,
                timestamp,
                row.FinalStatus,
                row.HotelName,
                row.HotelId,
                row.PmsName,
                row.FileId,
                row.Summary,
                cleanupWarning);
        }
        catch (Exception exception)
        {
            if (state is null)
            {
                throw ClassifyFailure(exception, stage);
            }

            throw RollBackAndCreateFailure(
                state,
                destinations,
                stage,
                exception);
        }
    }

    private void ValidateAndSynchronizeReport(QuickQaReport report)
    {
        if (report.HotelInformation is null)
        {
            throw ReportInvalid("Select a Hotel before saving Quick QA.");
        }

        QaFileMonth? fileMonth = report.HotelInformation.FileMonth;
        if (fileMonth is null
            || fileMonth.Year is < 1 or > 9999
            || fileMonth.Month is < 1 or > 12)
        {
            throw ReportInvalid("Select a valid File Month before saving Quick QA.");
        }

        string fileId = report.FileId?.Trim() ?? string.Empty;
        if (fileId.Length == 0)
        {
            throw ReportInvalid("File ID is required for Quick QA.");
        }

        ValidateChecklistResults(report);
        ValidateCurrentFindingsAndStatus(report);
        report.FileId = fileId;
        _ = findingService.Synchronize(report);
        summaryService.Synchronize(report);

        if (summaryService.IsStale(report))
        {
            throw new QuickQaSaveException(
                QuickQaSaveErrorCategory.SummaryStale,
                QuickQaSaveStage.Validation,
                "The Quick QA Summary is out of date. Regenerate the Summary and try again.");
        }

        report.Summary = report.Summary?.Trim() ?? string.Empty;
        if (report.FinalStatus != QaReportStatus.Pass
            && report.Summary.Length == 0)
        {
            throw ReportInvalid(
                "A Quick QA with warnings or failures requires a Summary.");
        }

        if (report.FinalStatus == QaReportStatus.Pass
            && report.Findings.Count == 0
            && !report.Summary.Equals(
                QuickQaSummaryService.CleanPassSummary,
                StringComparison.Ordinal))
        {
            throw ReportInvalid(
                "A clean Quick QA must use the current generated Summary.");
        }

    }

    private static void ValidateChecklistResults(QuickQaReport report)
    {
        Dictionary<string, QuickQaCheckResult> resultsById =
            new(StringComparer.Ordinal);

        foreach (QuickQaCheckResult? result in report.ChecklistResults)
        {
            if (result is null
                || string.IsNullOrWhiteSpace(result.CheckId)
                || !QuickQaChecklistCatalog.DefinitionsById.TryGetValue(
                    result.CheckId,
                    out QuickQaCheckDefinition? definition)
                || !Enum.IsDefined(result.Status)
                || !resultsById.TryAdd(result.CheckId, result))
            {
                throw ReportInvalid(
                    "The Quick QA checklist contains an invalid or duplicate result.");
            }

            if (result.Status == QuickQaCheckStatus.NotEvaluated)
            {
                throw ReportInvalid(
                    $"Complete '{definition.DisplayName}' before saving Quick QA.");
            }

            if (result.Status == QuickQaCheckStatus.NotApplicable
                && !definition.AllowsNotApplicable)
            {
                throw ReportInvalid(
                    $"'{definition.DisplayName}' does not allow Not Applicable.");
            }

            if (definition.IsStrategyAvailability
                && result.Status is not QuickQaCheckStatus.Pass
                    and not QuickQaCheckStatus.Fail)
            {
                throw ReportInvalid(
                    $"'{definition.DisplayName}' must be Available or Unavailable.");
            }
        }

        if (resultsById.Count != QuickQaChecklistCatalog.ExpectedDefinitionCount
            || QuickQaChecklistCatalog.Definitions.Any(
                definition => !resultsById.ContainsKey(definition.Id)))
        {
            throw ReportInvalid(
                "Quick QA must contain exactly one result for every fixed checklist item.");
        }
    }

    private void ValidateCurrentFindingsAndStatus(QuickQaReport report)
    {
        Dictionary<string, QaFinding> actualById =
            new(StringComparer.Ordinal);

        foreach (QaFinding? finding in report.Findings)
        {
            if (finding is null
                || string.IsNullOrWhiteSpace(finding.FindingId)
                || !Enum.IsDefined(finding.Severity)
                || !Enum.IsDefined(finding.Source)
                || !Enum.IsDefined(finding.Resolution)
                || !actualById.TryAdd(finding.FindingId, finding))
            {
                throw ReportInvalid(
                    "The Quick QA findings are invalid or duplicated. Refresh the QA state and try again.");
            }

            if (finding.Resolution is not QaFindingResolution.Active
                and not QaFindingResolution.HandledByCustomScript)
            {
                throw ReportInvalid(
                    "Quick QA findings may only be Active or Handled by Custom Script.");
            }

            if (!report.CustomScriptAvailable
                && finding.Resolution
                    == QaFindingResolution.HandledByCustomScript)
            {
                throw ReportInvalid(
                    "A finding cannot be Handled by Custom Script when Custom Script Available is No.");
            }
        }

        QuickQaReport expected = new()
        {
            CustomScriptAvailable = report.CustomScriptAvailable
        };
        expected.ChecklistResults.Clear();
        expected.ChecklistResults.AddRange(
            report.ChecklistResults.Select(result => new QuickQaCheckResult
            {
                CheckId = result.CheckId,
                Status = result.Status
            }));
        _ = findingService.Synchronize(expected);

        if (expected.Findings.Count != report.Findings.Count
            || expected.Findings.Any(expectedFinding =>
                !actualById.TryGetValue(
                    expectedFinding.FindingId,
                    out QaFinding? actualFinding)
                || !GeneratedFindingFieldsEqual(
                    expectedFinding,
                    actualFinding)))
        {
            throw ReportInvalid(
                "The Quick QA findings do not match the current checklist state. Refresh the QA state and try again.");
        }

        QaReportStatus? calculatedStatus;

        try
        {
            calculatedStatus = new QaReportStatusService().CalculateStatus(
                report.Findings,
                hasBlockingErrors: false);
        }
        catch (ArgumentException exception)
        {
            throw new QuickQaSaveException(
                QuickQaSaveErrorCategory.ReportInvalid,
                QuickQaSaveStage.Validation,
                "The Quick QA Result cannot be calculated from the current findings.",
                exception);
        }

        if (!Enum.IsDefined(report.FinalStatus)
            || calculatedStatus is null
            || report.FinalStatus != calculatedStatus)
        {
            throw ReportInvalid(
                "The final Quick QA Result does not match the current findings. Refresh the QA state and try again.");
        }
    }

    private static bool GeneratedFindingFieldsEqual(
        QaFinding expected,
        QaFinding actual)
    {
        return expected.Severity == actual.Severity
            && expected.Source == actual.Source
            && string.Equals(
                expected.RelatedCheckId,
                actual.RelatedCheckId,
                StringComparison.Ordinal)
            && string.Equals(
                expected.Title,
                actual.Title,
                StringComparison.Ordinal)
            && string.Equals(
                expected.Description,
                actual.Description,
                StringComparison.Ordinal);
    }

    private CanonicalMetadata ResolveCanonicalMetadata(QuickQaReport report)
    {
        try
        {
            IReadOnlyList<QaHotelMetadata> hotels =
                metadataService.LoadHotels();
            IReadOnlyList<QaPmsMetadata> pmsSystems =
                metadataService.LoadPmsSystems();
            QaHotelInformation selected = report.HotelInformation;
            string selectedHotelId = RequireTrimmed(
                selected.HotelId,
                "Hotel ID");
            string selectedHotelName = RequireTrimmed(
                selected.HotelName,
                "Hotel Name");
            string selectedPmsName = RequireTrimmed(
                selected.PmsName,
                "PMS");
            QaHotelMetadata[] hotelMatches = hotels
                .Where(item => item is not null)
                .Where(item => string.Equals(
                    item.HotelId?.Trim(),
                    selectedHotelId,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (hotelMatches.Length != 1)
            {
                throw MetadataMismatch(
                    "The selected Hotel no longer identifies one canonical metadata record.");
            }

            QaHotelMetadata hotel = hotelMatches[0];
            string canonicalHotelId = RequireTrimmed(
                hotel.HotelId,
                "canonical Hotel ID");
            string canonicalHotelName = RequireTrimmed(
                hotel.HotelName,
                "canonical Hotel Name");
            string canonicalPmsName = RequireTrimmed(
                hotel.PmsName,
                "canonical PMS");

            if (!selectedHotelId.Equals(
                    canonicalHotelId,
                    StringComparison.Ordinal)
                || !selectedHotelName.Equals(
                    canonicalHotelName,
                    StringComparison.Ordinal)
                || !selectedPmsName.Equals(
                    canonicalPmsName,
                    StringComparison.Ordinal))
            {
                throw MetadataMismatch(
                    "The selected Hotel Name or PMS no longer matches canonical metadata.");
            }

            QaPmsMetadata[] pmsMatches = pmsSystems
                .Where(item => item is not null)
                .Where(item => string.Equals(
                    item.PmsName?.Trim(),
                    canonicalPmsName,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (pmsMatches.Length != 1)
            {
                throw MetadataMismatch(
                    "The selected Hotel's PMS no longer identifies one canonical metadata record.");
            }

            return new CanonicalMetadata(hotel, pmsMatches[0]);
        }
        catch (QuickQaSaveException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is QaMetadataException
                or ArgumentException
                or InvalidOperationException
                or IOException
                or UnauthorizedAccessException)
        {
            throw new QuickQaSaveException(
                QuickQaSaveErrorCategory.MetadataMismatch,
                QuickQaSaveStage.MetadataResolution,
                "Current QA Hotel/PMS metadata could not authorize this save.",
                exception);
        }
    }

    private ResolvedDestinations ResolveDestinations(
        string selectedSurfaceFileName,
        string hotelFolderName)
    {
        try
        {
            string fileName = filenameService.ValidateStoredWorkbookFileName(
                selectedSurfaceFileName);
            string surfacePath = paths.ResolveSurfaceQaWorkbookPath(fileName);
            string historyPath = paths.ResolveQuickQaHistoryPath(
                hotelFolderName);
            QuickQaPathSafety.EnsureNoReparsePoints(
                paths.QaReportsRootPath,
                surfacePath);
            QuickQaPathSafety.EnsureNoReparsePoints(
                paths.QaReportsRootPath,
                historyPath);

            return new ResolvedDestinations(
                fileName,
                surfacePath,
                historyPath);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or NotSupportedException
                or PathTooLongException)
        {
            throw new QuickQaSaveException(
                QuickQaSaveErrorCategory.InvalidDestination,
                QuickQaSaveStage.MetadataResolution,
                "A Quick QA workbook destination is outside the approved storage policy.",
                exception);
        }
    }

    private QuickQaFileBaseline CaptureBaseline(
        string path,
        bool mustExist,
        QuickQaWorkbookRole role)
    {
        bool exists = fileOperations.FileExists(path);

        if (!exists)
        {
            if (mustExist)
            {
                throw new QuickQaWorkbookException(
                    QuickQaWorkbookErrorCategory.NotFound,
                    role,
                    "The selected Surface QA workbook no longer exists.");
            }

            return new QuickQaFileBaseline(
                path,
                Exists: false,
                Content: [],
                Sha256: [],
                Length: 0);
        }

        try
        {
            byte[] content = fileOperations.ReadAllBytes(path);
            return new QuickQaFileBaseline(
                path,
                Exists: true,
                content,
                SHA256.HashData(content),
                content.LongLength);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw WorkbookUnavailable(role, exception);
        }
    }

    private static QuickQaWorkbookRow CreateRow(
        QuickQaReport report,
        CanonicalMetadata canonical,
        DateTimeOffset timestamp)
    {
        QaFileMonth fileMonth = report.HotelInformation.FileMonth!;

        return new QuickQaWorkbookRow(
            timestamp,
            fileMonth.ToString(),
            canonical.Hotel.HotelName.Trim(),
            canonical.Hotel.HotelId.Trim(),
            canonical.Pms.PmsName.Trim(),
            report.FileId,
            report.FinalStatus,
            report.Summary);
    }

    private static TransactionState CreateTransactionState(
        ResolvedDestinations destinations,
        QuickQaFileBaseline surfaceBaseline,
        QuickQaFileBaseline historyBaseline,
        QuickQaWorkbookBuildResult surfaceBuild,
        QuickQaWorkbookBuildResult historyBuild)
    {
        string transactionId = Guid.NewGuid().ToString("N");
        string surfaceDirectory = Path.GetDirectoryName(
            destinations.SurfacePath)!;
        string historyDirectory = Path.GetDirectoryName(
            destinations.HistoryPath)!;

        return new TransactionState(
            surfaceBaseline,
            historyBaseline,
            surfaceBuild,
            historyBuild,
            Path.Combine(
                surfaceDirectory,
                $".{Path.GetFileName(destinations.SurfacePath)}.quickqa-{transactionId}.stage.xlsx"),
            Path.Combine(
                historyDirectory,
                $".{Path.GetFileName(destinations.HistoryPath)}.quickqa-{transactionId}.stage.xlsx"),
            Path.Combine(
                surfaceDirectory,
                $".{Path.GetFileName(destinations.SurfacePath)}.quickqa-{transactionId}.rollback.xlsx"),
            Path.Combine(
                historyDirectory,
                $".{Path.GetFileName(destinations.HistoryPath)}.quickqa-{transactionId}.rollback.xlsx"));
    }

    private void VerifyStagedFile(
        string path,
        byte[] expectedContent,
        QuickQaWorkbookRole role,
        QuickQaWorkbookRow row,
        QuickQaWorkbookBuildResult build)
    {
        try
        {
            VerifyFileBytes(
                path,
                expectedContent,
                role,
                role == QuickQaWorkbookRole.Surface
                    ? QuickQaSaveStage.SurfaceStaging
                    : QuickQaSaveStage.HistoryStaging);
            byte[] staged = fileOperations.ReadAllBytes(path);

            if (role == QuickQaWorkbookRole.Surface)
            {
                workbookService.VerifySurfaceAppend(
                    staged,
                    row,
                    build.AppendedRowNumber,
                    build.PreviousDataRowCount + 1);
            }
            else
            {
                workbookService.VerifyHistoryAppend(
                    staged,
                    row,
                    build.AppendedRowNumber,
                    build.PreviousDataRowCount + 1);
            }
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw WorkbookUnavailable(
                role,
                exception,
                role == QuickQaWorkbookRole.Surface
                    ? QuickQaSaveStage.SurfaceStaging
                    : QuickQaSaveStage.HistoryStaging);
        }
    }

    private void VerifyFinalFile(
        string path,
        byte[] expectedContent,
        QuickQaWorkbookRole role,
        QuickQaWorkbookRow row,
        QuickQaWorkbookBuildResult build)
    {
        try
        {
            VerifyFileBytes(
                path,
                expectedContent,
                role,
                QuickQaSaveStage.FinalVerification);
            byte[] finalContent = fileOperations.ReadAllBytes(path);

            if (role == QuickQaWorkbookRole.Surface)
            {
                workbookService.VerifySurfaceAppend(
                    finalContent,
                    row,
                    build.AppendedRowNumber,
                    build.PreviousDataRowCount + 1);
            }
            else
            {
                workbookService.VerifyHistoryAppend(
                    finalContent,
                    row,
                    build.AppendedRowNumber,
                    build.PreviousDataRowCount + 1);
            }
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw WorkbookUnavailable(
                role,
                exception,
                QuickQaSaveStage.FinalVerification);
        }
    }

    private void VerifyFileBytes(
        string path,
        ReadOnlyMemory<byte> expectedContent,
        QuickQaWorkbookRole role,
        QuickQaSaveStage verificationStage)
    {
        if (!fileOperations.FileExists(path)
            || fileOperations.GetFileLength(path) != expectedContent.Length)
        {
            throw ContentVerificationFailure(
                role,
                verificationStage,
                "A Quick QA workbook has an unexpected verified length.");
        }

        byte[] expectedHash = SHA256.HashData(expectedContent.Span);
        byte[] actualHash = fileOperations.ComputeSha256(path);
        if (!CryptographicOperations.FixedTimeEquals(
                expectedHash,
                actualHash))
        {
            throw ContentVerificationFailure(
                role,
                verificationStage,
                "A Quick QA workbook does not match its verified in-memory content.");
        }
    }

    private void MoveForBackup(
        string sourcePath,
        string destinationPath,
        QuickQaWorkbookRole role)
    {
        try
        {
            fileOperations.Move(sourcePath, destinationPath);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw WorkbookUnavailable(
                role,
                exception,
                QuickQaSaveStage.Backup);
        }
    }

    private IQuickQaExclusiveReadLease OpenRollbackLease(
        string rollbackPath,
        QuickQaWorkbookRole role)
    {
        try
        {
            return fileOperations.OpenExclusiveReadLease(rollbackPath);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw WorkbookUnavailable(
                role,
                exception,
                QuickQaSaveStage.Backup);
        }
    }

    private static void ValidateRollbackLease(
        IQuickQaExclusiveReadLease lease,
        QuickQaFileBaseline baseline,
        QuickQaWorkbookRole role)
    {
        try
        {
            if (lease.Length != baseline.Length)
            {
                throw ConcurrentChange(
                    role,
                    QuickQaSaveStage.Backup);
            }

            byte[] rollbackHash = lease.ComputeSha256();
            if (!CryptographicOperations.FixedTimeEquals(
                    baseline.Sha256,
                    rollbackHash))
            {
                throw ConcurrentChange(
                    role,
                    QuickQaSaveStage.Backup);
            }
        }
        catch (QuickQaSaveException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw WorkbookUnavailable(
                role,
                exception,
                QuickQaSaveStage.Backup);
        }
    }

    private void EnsureBaselineUnchanged(
        QuickQaFileBaseline baseline,
        QuickQaWorkbookRole role,
        bool requireExclusiveAccess)
    {
        bool exists = fileOperations.FileExists(baseline.Path);

        if (exists != baseline.Exists)
        {
            throw ConcurrentChange(role);
        }

        if (!baseline.Exists)
        {
            return;
        }

        try
        {
            if (fileOperations.GetFileLength(baseline.Path) != baseline.Length)
            {
                throw ConcurrentChange(role);
            }

            byte[] currentHash = fileOperations.ComputeSha256(baseline.Path);
            if (!CryptographicOperations.FixedTimeEquals(
                    baseline.Sha256,
                    currentHash))
            {
                throw ConcurrentChange(role);
            }

            if (requireExclusiveAccess)
            {
                fileOperations.VerifyExclusiveAccess(baseline.Path);
            }
        }
        catch (QuickQaSaveException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw WorkbookUnavailable(role, exception);
        }
    }

    private string? CleanupAfterSuccess(TransactionState state)
    {
        List<string> leftovers = [];

        ReleaseRollbackLeasesForCleanup(state, leftovers);

        if (state.SurfaceBackedUp)
        {
            TryCleanup(state.SurfaceRollbackPath, leftovers);
        }

        if (state.HistoryBackedUp)
        {
            TryCleanup(state.HistoryRollbackPath, leftovers);
        }

        if (leftovers.Count == 0)
        {
            return null;
        }

        return "The Quick QA was saved, but transaction cleanup could not remove: "
            + string.Join(
                ", ",
                leftovers.Select(Path.GetFileName).Take(4));
    }

    private QuickQaSaveException RollBackAndCreateFailure(
        TransactionState state,
        ResolvedDestinations destinations,
        QuickQaSaveStage failedStage,
        Exception originalFailure)
    {
        List<Exception> rollbackFailures = [];
        bool stateChanged = state.SurfaceBackedUp
            || state.HistoryBackedUp
            || state.SurfaceCommitted
            || state.HistoryCommitted;

        ReleaseRollbackLeasesForRollback(
            state,
            rollbackFailures);

        if (state.HistoryCommitted)
        {
            TryRollback(
                () => fileOperations.Delete(destinations.HistoryPath),
                rollbackFailures);
        }

        if (state.SurfaceCommitted)
        {
            TryRollback(
                () => fileOperations.Delete(destinations.SurfacePath),
                rollbackFailures);
        }

        if (state.HistoryBackedUp)
        {
            TryRollback(
                () => fileOperations.Move(
                    state.HistoryRollbackPath,
                    destinations.HistoryPath),
                rollbackFailures);
        }

        if (state.SurfaceBackedUp)
        {
            TryRollback(
                () => fileOperations.Move(
                    state.SurfaceRollbackPath,
                    destinations.SurfacePath),
                rollbackFailures);
        }

        if (state.HistoryStageCreated)
        {
            TryRollback(
                () => fileOperations.Delete(state.HistoryStagePath),
                rollbackFailures);
        }

        if (state.SurfaceStageCreated)
        {
            TryRollback(
                () => fileOperations.Delete(state.SurfaceStagePath),
                rollbackFailures);
        }

        if (originalFailure is QuickQaOwnedTemporaryCleanupException owned)
        {
            TryRollback(
                () => fileOperations.Delete(owned.TemporaryPath),
                rollbackFailures);
        }

        bool manualReviewRequired = rollbackFailures.Count != 0;
        bool previousStateRestored = stateChanged && !manualReviewRequired;

        if (manualReviewRequired)
        {
            return new QuickQaSaveException(
                QuickQaSaveErrorCategory.RollbackFailure,
                QuickQaSaveStage.Rollback,
                "The Quick QA save failed and rollback was incomplete. Review the Surface and Hotel history locations before retrying.",
                new AggregateException(
                    new[] { originalFailure }.Concat(rollbackFailures)),
                previousStateRestored: false,
                manualReviewRequired: true,
                manualReviewLocations:
                [
                    Path.GetDirectoryName(destinations.SurfacePath)!,
                    Path.GetDirectoryName(destinations.HistoryPath)!
                ]);
        }

        QuickQaSaveException classified = ClassifyFailure(
            originalFailure,
            failedStage);
        return new QuickQaSaveException(
            classified.ErrorCategory,
            classified.Stage,
            classified.Message,
            classified,
            previousStateRestored,
            manualReviewRequired: false,
            manualReviewLocations: []);
    }

    private QuickQaSaveException ClassifyFailure(
        Exception exception,
        QuickQaSaveStage stage)
    {
        if (exception is QuickQaSaveException saveException)
        {
            return saveException;
        }

        if (exception is QuickQaWorkbookException workbookException)
        {
            QuickQaSaveErrorCategory category = workbookException.Role switch
            {
                _ when workbookException.ErrorCategory
                    == QuickQaWorkbookErrorCategory.ContentVerificationFailure =>
                    QuickQaSaveErrorCategory.ContentVerificationFailure,
                QuickQaWorkbookRole.Surface
                    when workbookException.ErrorCategory
                        == QuickQaWorkbookErrorCategory.InUseOrUnavailable =>
                    QuickQaSaveErrorCategory.SurfaceWorkbookInUse,
                QuickQaWorkbookRole.HotelHistory
                    when workbookException.ErrorCategory
                        == QuickQaWorkbookErrorCategory.InUseOrUnavailable =>
                    QuickQaSaveErrorCategory.HotelHistoryWorkbookInUse,
                QuickQaWorkbookRole.Surface =>
                    QuickQaSaveErrorCategory.SurfaceWorkbookInvalid,
                _ => QuickQaSaveErrorCategory.HotelHistoryWorkbookInvalid
            };

            return new QuickQaSaveException(
                category,
                stage,
                workbookException.Message,
                workbookException);
        }

        if (exception is UnauthorizedAccessException
            or System.Security.SecurityException)
        {
            return new QuickQaSaveException(
                QuickQaSaveErrorCategory.PermissionDenied,
                stage,
                "Quick QA could not write to the configured documentation folder.",
                exception);
        }

        if (exception is IOException)
        {
            bool historyStage = stage is QuickQaSaveStage.HistoryLoad
                or QuickQaSaveStage.HistoryStaging
                or QuickQaSaveStage.HistoryCommit;
            return WorkbookUnavailable(
                historyStage
                    ? QuickQaWorkbookRole.HotelHistory
                    : QuickQaWorkbookRole.Surface,
                exception,
                stage);
        }

        return new QuickQaSaveException(
            QuickQaSaveErrorCategory.TransactionFailure,
            stage,
            "The Quick QA paired workbook transaction did not complete.",
            exception);
    }

    private static QuickQaSaveException WorkbookUnavailable(
        QuickQaWorkbookRole role,
        Exception innerException,
        QuickQaSaveStage? stage = null)
    {
        bool surface = role == QuickQaWorkbookRole.Surface;
        return new QuickQaSaveException(
            surface
                ? QuickQaSaveErrorCategory.SurfaceWorkbookInUse
                : QuickQaSaveErrorCategory.HotelHistoryWorkbookInUse,
            stage ?? (surface
                ? QuickQaSaveStage.SurfaceLoad
                : QuickQaSaveStage.HistoryLoad),
            surface
                ? "The Surface QA workbook is currently open or unavailable. Close the workbook and try again."
                : "The Hotel Quick QA history workbook is currently open or unavailable. Close the workbook and try again.",
            innerException);
    }

    private static QuickQaSaveException ConcurrentChange(
        QuickQaWorkbookRole role,
        QuickQaSaveStage stage = QuickQaSaveStage.ConcurrencyCheck)
    {
        return new QuickQaSaveException(
            QuickQaSaveErrorCategory.ConcurrentChange,
            stage,
            role == QuickQaWorkbookRole.Surface
                ? "The Surface QA workbook changed while this save was being staged. No update was kept."
                : "The Hotel Quick QA history workbook changed while this save was being staged. No update was kept.");
    }

    private static QuickQaSaveException ContentVerificationFailure(
        QuickQaWorkbookRole role,
        QuickQaSaveStage stage,
        string message)
    {
        return new QuickQaSaveException(
            QuickQaSaveErrorCategory.ContentVerificationFailure,
            stage,
            message);
    }

    private static QuickQaSaveException ReportInvalid(string message)
    {
        return new QuickQaSaveException(
            QuickQaSaveErrorCategory.ReportInvalid,
            QuickQaSaveStage.Validation,
            message);
    }

    private static QuickQaSaveException MetadataMismatch(string message)
    {
        return new QuickQaSaveException(
            QuickQaSaveErrorCategory.MetadataMismatch,
            QuickQaSaveStage.MetadataResolution,
            message);
    }

    private static string RequireTrimmed(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw MetadataMismatch(
                $"Current {fieldName} metadata is unavailable.");
        }

        return value.Trim();
    }

    private void TryCleanup(string path, ICollection<string> leftovers)
    {
        try
        {
            fileOperations.Delete(path);
        }
        catch (Exception)
        {
            leftovers.Add(path);
        }
    }

    private static void ReleaseRollbackLeasesForCleanup(
        TransactionState state,
        ICollection<string> leftovers)
    {
        ReleaseLeaseForCleanup(
            state.HistoryRollbackLease,
            state.HistoryRollbackPath,
            leftovers);
        state.HistoryRollbackLease = null;
        ReleaseLeaseForCleanup(
            state.SurfaceRollbackLease,
            state.SurfaceRollbackPath,
            leftovers);
        state.SurfaceRollbackLease = null;
    }

    private static void ReleaseLeaseForCleanup(
        IQuickQaExclusiveReadLease? lease,
        string rollbackPath,
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
        catch (Exception)
        {
            leftovers.Add(rollbackPath);
        }
    }

    private static void ReleaseRollbackLeasesForRollback(
        TransactionState state,
        ICollection<Exception> failures)
    {
        ReleaseLeaseForRollback(
            state.HistoryRollbackLease,
            failures);
        state.HistoryRollbackLease = null;
        ReleaseLeaseForRollback(
            state.SurfaceRollbackLease,
            failures);
        state.SurfaceRollbackLease = null;
    }

    private static void ReleaseLeaseForRollback(
        IQuickQaExclusiveReadLease? lease,
        ICollection<Exception> failures)
    {
        if (lease is null)
        {
            return;
        }

        TryRollback(lease.Dispose, failures);
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

    private sealed record CanonicalMetadata(
        QaHotelMetadata Hotel,
        QaPmsMetadata Pms);

    private sealed record ResolvedDestinations(
        string SurfaceFileName,
        string SurfacePath,
        string HistoryPath);

    private sealed class TransactionState
    {
        public TransactionState(
            QuickQaFileBaseline surfaceBaseline,
            QuickQaFileBaseline historyBaseline,
            QuickQaWorkbookBuildResult surfaceBuild,
            QuickQaWorkbookBuildResult historyBuild,
            string surfaceStagePath,
            string historyStagePath,
            string surfaceRollbackPath,
            string historyRollbackPath)
        {
            SurfaceBaseline = surfaceBaseline;
            HistoryBaseline = historyBaseline;
            SurfaceBuild = surfaceBuild;
            HistoryBuild = historyBuild;
            SurfaceStagePath = surfaceStagePath;
            HistoryStagePath = historyStagePath;
            SurfaceRollbackPath = surfaceRollbackPath;
            HistoryRollbackPath = historyRollbackPath;
        }

        public QuickQaFileBaseline SurfaceBaseline { get; }

        public QuickQaFileBaseline HistoryBaseline { get; }

        public QuickQaWorkbookBuildResult SurfaceBuild { get; }

        public QuickQaWorkbookBuildResult HistoryBuild { get; }

        public string SurfaceStagePath { get; }

        public string HistoryStagePath { get; }

        public string SurfaceRollbackPath { get; }

        public string HistoryRollbackPath { get; }

        public bool SurfaceStageCreated { get; set; }

        public bool HistoryStageCreated { get; set; }

        public bool SurfaceBackedUp { get; set; }

        public bool HistoryBackedUp { get; set; }

        public IQuickQaExclusiveReadLease? SurfaceRollbackLease { get; set; }

        public IQuickQaExclusiveReadLease? HistoryRollbackLease { get; set; }

        public bool SurfaceCommitted { get; set; }

        public bool HistoryCommitted { get; set; }
    }
}
