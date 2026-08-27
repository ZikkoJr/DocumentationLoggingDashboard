using System.Security.Cryptography;
using ClosedXML.Excel;
using DocumentationLoggingDashboard.DocumentationLogs;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.GeometryTests;

internal sealed class DocumentationLogSyntheticEnvironment : IDisposable
{
    private static readonly string TemporaryParentPath = Path.GetFullPath(
        Path.Combine(
            Path.GetTempPath(),
            "DocumentationLoggingDashboard.DocumentationLogRegressionTests"));

    public DocumentationLogSyntheticEnvironment()
    {
        RootPath = Path.GetFullPath(Path.Combine(
            TemporaryParentPath,
            "r-" + Guid.NewGuid().ToString("N")));

        try
        {
            Directory.CreateDirectory(RootPath);
            QaPaths = new QaStoragePaths(RootPath);
            new QaStorageInitializer(QaPaths).Initialize();
            MetadataService = new QaMetadataService(
                QaPaths,
                new QaFolderNameSanitizer());
            Mews = MetadataService.AddPmsSystem("Mews");
            Opera = MetadataService.AddPmsSystem("Opera");
            Hotel1953 = MetadataService.AddHotel(
                "1953",
                "Example Hotel",
                Mews.PmsName);
            Hotel2093 = MetadataService.AddHotel(
                "2093",
                "Another Hotel",
                Mews.PmsName);
            Hotel3001 = MetadataService.AddHotel(
                "3001",
                "Opera Hotel",
                Opera.PmsName);
            HotelLeadingZero = MetadataService.AddHotel(
                "0012",
                "Leading Zero Hotel",
                Opera.PmsName);
            Paths = new DocumentationLogStoragePaths(QaPaths);
            Paths.EnsureBaseDirectories();
            WorkbookService = new DocumentationLogWorkbookService(Paths);
            PreferencesService = new DocumentationLogWorkbookPreferencesService(Paths);
            RoutingService = new DocumentationLogRoutingService(MetadataService);
        }
        catch
        {
            CleanupRoot();
            throw;
        }
    }

    public string RootPath { get; }

    public QaStoragePaths QaPaths { get; }

    public QaMetadataService MetadataService { get; }

    public DocumentationLogStoragePaths Paths { get; }

    public DocumentationLogWorkbookService WorkbookService { get; }

    public DocumentationLogWorkbookPreferencesService PreferencesService { get; }

    public DocumentationLogRoutingService RoutingService { get; }

    public QaPmsMetadata Mews { get; }

    public QaPmsMetadata Opera { get; }

    public QaHotelMetadata Hotel1953 { get; }

    public QaHotelMetadata Hotel2093 { get; }

    public QaHotelMetadata Hotel3001 { get; }

    public QaHotelMetadata HotelLeadingZero { get; }

    public string CreateRunning(LogType logType, string stem)
    {
        return WorkbookService.CreateNewRunningWorkbook(logType, stem);
    }

    public DocumentationLogSaveService CreateSaveService(
        DateTimeOffset? localTimestamp = null,
        IDocumentationLogTransactionFileOperations? fileOperations = null)
    {
        DateTimeOffset timestamp = localTimestamp
            ?? new DateTimeOffset(2026, 8, 26, 14, 30, 0, TimeSpan.FromHours(-4));
        TimeProvider timeProvider = new FixedDocumentationLogTimeProvider(timestamp);

        if (fileOperations is null)
        {
            return new DocumentationLogSaveService(
                Paths,
                MetadataService,
                timeProvider);
        }

        return new DocumentationLogSaveService(
            Paths,
            RoutingService,
            WorkbookService,
            new DocumentationLogSequenceService(Paths),
            new DocumentationLogIndexService(),
            fileOperations,
            timeProvider);
    }

    public DocumentationLogSaveRequest CreateDebuggingRequest(
        string runningFileName,
        QaHotelMetadata? hotel = null,
        QaPmsMetadata? pms = null,
        string createdBy = "Tester",
        string notes = "Follow up")
    {
        QaHotelMetadata selectedHotel = hotel ?? Hotel1953;
        QaPmsMetadata selectedPms = pms
            ?? (selectedHotel.PmsName.Equals(Mews.PmsName, StringComparison.Ordinal)
                ? Mews
                : Opera);
        return new DocumentationLogSaveRequest(
            LogType.DebuggingLog,
            runningFileName,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [DocumentationLogFieldKeys.ErrorShownOnTicket] = "Ticket error",
                [DocumentationLogFieldKeys.RootCause] = "Root cause",
                [DocumentationLogFieldKeys.FixApplied] = "Fix applied",
                [DocumentationLogFieldKeys.CreatedBy] = createdBy,
                [DocumentationLogFieldKeys.NotesFollowUp] = notes
            },
            selectedHotel,
            selectedPms);
    }

    public DocumentationLogSaveRequest CreateScriptRequest(
        LogType logType,
        string runningFileName,
        string hotelIds,
        string createdBy = "Tester",
        string notes = "Follow up")
    {
        Dictionary<string, string> fields = new(StringComparer.Ordinal)
        {
            [DocumentationLogFieldKeys.ScriptName] = "SyntheticScript.csx",
            [DocumentationLogFieldKeys.CreatedBy] = createdBy,
            [DocumentationLogFieldKeys.NotesFollowUp] = notes
        };

        if (logType == LogType.ScriptEditingLog)
        {
            fields[DocumentationLogFieldKeys.ReasonForEdit] = "Ticket requested edit";
            fields[DocumentationLogFieldKeys.ChangesMade] = "Updated matching logic";
        }
        else if (logType == LogType.ScriptCreationLog)
        {
            fields[DocumentationLogFieldKeys.ReasonForCreation] = "New workflow";
            fields[DocumentationLogFieldKeys.ScriptPurpose] = "Creates the requested output";
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(logType));
        }

        return new DocumentationLogSaveRequest(
            logType,
            runningFileName,
            fields,
            hotelIdsInput: hotelIds);
    }

    public static int CountWorkbookRows(string path)
    {
        using XLWorkbook workbook = new(path);
        IXLWorksheet worksheet = workbook.Worksheet(
            DocumentationLogWorkbookSchema.DataWorksheetName);
        int lastRow = worksheet.LastRowUsed(XLCellsUsedOptions.Contents)?.RowNumber()
            ?? 3;
        return Enumerable.Range(4, Math.Max(0, lastRow - 3))
            .Count(row => !worksheet.Cell(row, 2).IsEmpty());
    }

    public static byte[] HashFile(string path)
    {
        return SHA256.HashData(File.ReadAllBytes(path));
    }

    public void Dispose()
    {
        CleanupRoot();
    }

    private void CleanupRoot()
    {
        string? actualParent = Path.GetDirectoryName(
            Path.TrimEndingDirectorySeparator(RootPath));
        if (!string.Equals(
                actualParent,
                TemporaryParentPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Refused to clean a documentation-log regression root outside its dedicated parent.");
        }

        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
internal sealed class FixedDocumentationLogTimeProvider : TimeProvider
{
    private readonly DateTimeOffset utcTimestamp;
    private readonly TimeZoneInfo localTimeZone;

    public FixedDocumentationLogTimeProvider(DateTimeOffset localTimestamp)
    {
        utcTimestamp = localTimestamp.ToUniversalTime();
        localTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "DocumentationLogTestZone-" + Guid.NewGuid().ToString("N"),
            localTimestamp.Offset,
            "Documentation Log Test Zone",
            "Documentation Log Test Zone");
    }

    public override DateTimeOffset GetUtcNow()
    {
        return utcTimestamp;
    }

    public override TimeZoneInfo LocalTimeZone => localTimeZone;
}

internal static class DocumentationLogTestAssert
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new DocumentationLogRegressionAssertionException(message);
        }
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new DocumentationLogRegressionAssertionException(
                $"{message} Expected '{expected}', received '{actual}'.");
        }
    }

    public static TException Throws<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new DocumentationLogRegressionAssertionException(message);
    }
}

internal sealed class DocumentationLogRegressionAssertionException : Exception
{
    public DocumentationLogRegressionAssertionException(string message)
        : base(message)
    {
    }
}
