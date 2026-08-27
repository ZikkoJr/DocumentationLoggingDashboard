using ClosedXML.Excel;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.DocumentationLogs;

/// <summary>
/// Owns creation, discovery, strict schema validation, in-memory append, and
/// reopen verification for all documentation-log workbooks. Multi-file commit
/// and rollback remain the responsibility of <see cref="DocumentationLogSaveService"/>.
/// </summary>
public sealed class DocumentationLogWorkbookService
{
    private const string DateTimeNumberFormat = "yyyy-mm-dd hh:mm";
    private const string TextNumberFormat = "@";

    private readonly DocumentationLogStoragePaths paths;
    private readonly DocumentationLogWorkbookFilenameService filenameService;
    private readonly IDocumentationLogTransactionFileOperations fileOperations;

    public DocumentationLogWorkbookService(DocumentationLogStoragePaths paths)
        : this(
            paths,
            new DocumentationLogWorkbookFilenameService(paths),
            new DocumentationLogTransactionFileOperations())
    {
    }

    public DocumentationLogWorkbookService(
        DocumentationLogStoragePaths paths,
        DocumentationLogWorkbookFilenameService filenameService,
        IDocumentationLogTransactionFileOperations fileOperations)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.filenameService = filenameService
            ?? throw new ArgumentNullException(nameof(filenameService));
        this.fileOperations = fileOperations
            ?? throw new ArgumentNullException(nameof(fileOperations));
    }

    /// <summary>
    /// Returns compatible direct-child Running workbooks, newest first.
    /// Lock files, transaction artifacts, corrupt files, and incompatible files
    /// are deliberately omitted from best-effort discovery.
    /// </summary>
    public IReadOnlyList<string> GetCompatibleRunningWorkbookFileNames(
        LogType logType)
    {
        _ = DocumentationLogWorkbookSchema.GetHeaders(logType);
        string runningDirectory = paths.GetRunningDirectory(logType);

        try
        {
            QuickQaPathSafety.EnsureNoReparsePoints(
                paths.DocumentationRootPath,
                runningDirectory);

            if (!Directory.Exists(runningDirectory))
            {
                return Array.Empty<string>();
            }

            List<DocumentationLogWorkbookInfo> compatible = [];

            foreach (string fullPath in Directory.EnumerateFiles(
                         runningDirectory,
                         "*.xlsx",
                         SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(fullPath);
                if (IsInternalOrLockFile(fileName)
                    || !DocumentationLogWorkbookFilenameService
                        .IsSafeStoredWorkbookFileName(fileName))
                {
                    continue;
                }

                try
                {
                    compatible.Add(
                        ValidateRunningWorkbook(logType, fileName));
                }
                catch (DocumentationLogWorkbookException)
                {
                    // Explicit selection/save reports the focused reason.
                    // Discovery itself remains resilient.
                }
            }

            return compatible
                .OrderByDescending(item => item.LastWriteTimeUtc)
                .ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.FileName)
                .ToArray();
        }
        catch (DocumentationLogWorkbookException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception)
                                           || exception is InvalidOperationException)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.InUseOrUnavailable,
                DocumentationLogWorkbookSchema.CreateRunningContract(logType),
                runningDirectory,
                "The Running log folder is unavailable and its workbooks could not be discovered.",
                exception);
        }
    }

    public DocumentationLogWorkbookInfo ValidateRunningWorkbook(
        LogType logType,
        string workbookFileName)
    {
        DocumentationLogWorkbookContract contract =
            DocumentationLogWorkbookSchema.CreateRunningContract(logType);
        string safeFileName;
        string fullPath;

        try
        {
            safeFileName = filenameService.ValidateStoredWorkbookFileName(
                workbookFileName);
            fullPath = paths.ResolveRunningWorkbookPath(
                logType,
                safeFileName);
            QuickQaPathSafety.EnsureNoReparsePoints(
                paths.DocumentationRootPath,
                fullPath);
        }
        catch (Exception exception) when (IsInvalidPathException(exception))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.InvalidFilename,
                contract,
                workbookPath: null,
                "The selected Running log workbook filename is not valid.",
                exception);
        }

        if (!fileOperations.FileExists(fullPath))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.NotFound,
                contract,
                fullPath,
                "The selected Running log workbook no longer exists.");
        }

        byte[] content;

        try
        {
            content = fileOperations.ReadAllBytes(fullPath);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.InUseOrUnavailable,
                contract,
                fullPath,
                "The selected log workbook is currently open or unavailable. Close the workbook and try again.",
                exception);
        }

        WorkbookSnapshot snapshot = InspectWorkbookContent(
            content,
            contract,
            fullPath);
        DateTime lastWriteTimeUtc;

        try
        {
            if (!fileOperations.FileExists(fullPath))
            {
                throw Failure(
                    DocumentationLogWorkbookErrorCategory.NotFound,
                    contract,
                    fullPath,
                    "The selected Running log workbook no longer exists.");
            }

            lastWriteTimeUtc = File.GetLastWriteTimeUtc(fullPath);
        }
        catch (DocumentationLogWorkbookException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.InUseOrUnavailable,
                contract,
                fullPath,
                "The selected log workbook is currently unavailable.",
                exception);
        }

        return new DocumentationLogWorkbookInfo(
            safeFileName,
            fullPath,
            contract,
            snapshot.WorksheetName,
            snapshot.DataRowCount,
            lastWriteTimeUtc);
    }

    /// <summary>
    /// Creates and flushes a canonical empty Running workbook using create-new
    /// semantics. No existing file is ever replaced.
    /// </summary>
    public string CreateNewRunningWorkbook(
        LogType logType,
        string requestedFileName)
    {
        DocumentationLogWorkbookContract contract =
            DocumentationLogWorkbookSchema.CreateRunningContract(logType);
        string fullPath;

        try
        {
            fullPath = filenameService.ResolveNewWorkbookPath(
                logType,
                requestedFileName);
            QuickQaPathSafety.EnsureNoReparsePoints(
                paths.DocumentationRootPath,
                fullPath);
        }
        catch (IOException exception)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.AlreadyExists,
                contract,
                workbookPath: null,
                "A Running log workbook with that filename already exists.",
                exception);
        }
        catch (Exception exception) when (IsInvalidPathException(exception))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.InvalidFilename,
                contract,
                workbookPath: null,
                "The Running log workbook filename is not valid.",
                exception);
        }

        byte[] content = CreateEmptyWorkbookBytes(contract);
        string fileName = Path.GetFileName(fullPath);
        bool created = false;

        try
        {
            fileOperations.CreateDirectory(paths.GetRunningDirectory(logType));
            fileOperations.WriteNewAndFlush(fullPath, content);
            created = true;
            _ = ValidateRunningWorkbook(logType, fileName);
            return fileName;
        }
        catch (DocumentationLogWorkbookException)
        {
            if (created)
            {
                TryDeleteOwnedFile(fullPath);
            }

            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            if (created)
            {
                TryDeleteOwnedFile(fullPath);
            }

            DocumentationLogWorkbookErrorCategory category =
                fileOperations.FileExists(fullPath)
                    ? DocumentationLogWorkbookErrorCategory.AlreadyExists
                    : DocumentationLogWorkbookErrorCategory.InUseOrUnavailable;
            string message = category
                == DocumentationLogWorkbookErrorCategory.AlreadyExists
                ? "A Running log workbook with that filename already exists."
                : "The new Running log workbook could not be created in the configured folder.";

            throw Failure(
                category,
                contract,
                fullPath,
                message,
                exception);
        }
    }

    /// <summary>
    /// Builds one append wholly in memory. A null source creates the canonical
    /// workbook first, which is used for deterministic Hotel/PMS history files.
    /// </summary>
    public DocumentationLogWorkbookBuildResult BuildAppend(
        ReadOnlyMemory<byte>? sourceContent,
        DocumentationLogWorkbookContract contract,
        DocumentationLogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(logEvent);
        DocumentationLogWorkbookSchema.ValidateContract(contract);
        ValidateContractForEvent(contract, logEvent);

        using MemoryStream? input = sourceContent is null
            ? null
            : CreateReadStream(sourceContent.Value);
        using XLWorkbook workbook = input is null
            ? CreateEmptyWorkbook(contract)
            : LoadWorkbook(input, contract, workbookPath: null);

        WorkbookSnapshot snapshot = InspectWorkbook(
            workbook,
            contract,
            workbookPath: null);
        IXLWorksheet worksheet = workbook.Worksheet(
            DocumentationLogWorkbookSchema.DataWorksheetName);
        int newRowNumber = snapshot.LastBusinessRowNumber + 1;

        WriteEventRow(worksheet, newRowNumber, logEvent);
        EnsureTableCoversRow(
            workbook,
            worksheet,
            contract.LogType,
            newRowNumber);
        ApplyPresentation(worksheet, contract.LogType);

        byte[] content = Serialize(workbook, contract);
        VerifyAppend(
            content,
            contract,
            logEvent,
            newRowNumber,
            snapshot.DataRowCount + 1);

        return new DocumentationLogWorkbookBuildResult(
            content,
            newRowNumber,
            snapshot.DataRowCount);
    }

    /// <summary>
    /// Reopens staged bytes and verifies the exact typed row, append-only row
    /// count, marker, Table/filter, and frozen-header contract.
    /// </summary>
    public void VerifyAppend(
        ReadOnlyMemory<byte> content,
        DocumentationLogWorkbookContract contract,
        DocumentationLogEvent expectedEvent,
        int expectedRowNumber,
        int expectedDataRowCount)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(expectedEvent);
        DocumentationLogWorkbookSchema.ValidateContract(contract);
        ValidateContractForEvent(contract, expectedEvent);

        if (expectedRowNumber < 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedRowNumber),
                "A documentation-log data row cannot precede row 4.");
        }

        if (expectedDataRowCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedDataRowCount),
                "A verified append must leave at least one data row.");
        }

        WorkbookSnapshot snapshot = InspectWorkbookContent(
            content,
            contract,
            workbookPath: null);

        if (snapshot.DataRowCount != expectedDataRowCount)
        {
            throw VerificationFailure(
                contract,
                "The staged workbook data-row count is not the expected append-only count.");
        }

        using MemoryStream input = CreateReadStream(content);
        using XLWorkbook workbook = LoadWorkbook(
            input,
            contract,
            workbookPath: null);
        IXLWorksheet worksheet = workbook.Worksheet(
            DocumentationLogWorkbookSchema.DataWorksheetName);

        EnsureExactEventRow(
            worksheet,
            expectedRowNumber,
            expectedEvent,
            contract);

        IXLTable table = worksheet.Tables.Single();
        if (table.RangeAddress.LastAddress.RowNumber < expectedRowNumber
            || !table.ShowHeaderRow
            || !table.ShowAutoFilter
            || table.ShowTotalsRow
            || worksheet.SheetView.SplitRow != 3)
        {
            throw VerificationFailure(
                contract,
                "The staged workbook is missing its required Table, filter, or frozen header rows.");
        }
    }

    private byte[] CreateEmptyWorkbookBytes(
        DocumentationLogWorkbookContract contract)
    {
        using XLWorkbook workbook = CreateEmptyWorkbook(contract);
        byte[] content = Serialize(workbook, contract);
        _ = InspectWorkbookContent(content, contract, workbookPath: null);
        return content;
    }

    private static XLWorkbook CreateEmptyWorkbook(
        DocumentationLogWorkbookContract contract)
    {
        XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add(
            DocumentationLogWorkbookSchema.DataWorksheetName);
        IReadOnlyList<string> headers =
            DocumentationLogWorkbookSchema.GetHeaders(contract.LogType);

        worksheet.Cell(1, 1).Value = contract.Title;
        for (int column = 1; column <= headers.Count; column++)
        {
            worksheet.Cell(3, column).Value = headers[column - 1];
        }

        IXLTable table = worksheet.Range(3, 1, 4, headers.Count)
            .CreateTable(
                DocumentationLogWorkbookSchema.GetTableName(contract.LogType));
        table.ShowHeaderRow = true;
        table.ShowTotalsRow = false;
        table.ShowAutoFilter = true;
        ApplyPresentation(worksheet, contract.LogType);

        IXLWorksheet metadata = workbook.Worksheets.Add(
            DocumentationLogWorkbookSchema.MetadataWorksheetName);
        WriteMetadata(metadata, contract);
        metadata.Visibility = XLWorksheetVisibility.VeryHidden;
        return workbook;
    }

    private static void WriteMetadata(
        IXLWorksheet metadata,
        DocumentationLogWorkbookContract contract)
    {
        metadata.Cell(1, 1).Value = "schemaName";
        metadata.Cell(1, 2).Value =
            DocumentationLogWorkbookSchema.SchemaName;
        metadata.Cell(2, 1).Value = "schemaVersion";
        metadata.Cell(2, 2).Value =
            DocumentationLogWorkbookSchema.CurrentSchemaVersion;
        metadata.Cell(3, 1).Value = "logType";
        metadata.Cell(3, 2).Value = contract.LogType.ToString();
        metadata.Cell(4, 1).Value = "scopeType";
        metadata.Cell(4, 2).Value = contract.ScopeType.ToString();
        metadata.Cell(5, 1).Value = "scopeId";
        metadata.Cell(5, 2).Value = contract.ScopeId;
    }

    private static void ApplyPresentation(
        IXLWorksheet worksheet,
        LogType logType)
    {
        IReadOnlyList<double> widths =
            DocumentationLogWorkbookSchema.GetColumnWidths(logType);
        IReadOnlySet<int> textColumns =
            DocumentationLogWorkbookSchema.GetTextColumnNumbers(logType);
        IReadOnlySet<int> wrappedColumns =
            DocumentationLogWorkbookSchema.GetWrappedColumnNumbers(logType);

        worksheet.SheetView.FreezeRows(3);
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Row(3).Style.Alignment.WrapText = true;
        worksheet.Column(1).Style.NumberFormat.Format = DateTimeNumberFormat;

        for (int column = 1; column <= widths.Count; column++)
        {
            worksheet.Column(column).Width = widths[column - 1];
            worksheet.Column(column).Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Top;
        }

        foreach (int column in textColumns)
        {
            worksheet.Column(column).Style.NumberFormat.Format =
                TextNumberFormat;
        }

        foreach (int column in wrappedColumns)
        {
            worksheet.Column(column).Style.Alignment.WrapText = true;
        }
    }

    private static void EnsureTableCoversRow(
        XLWorkbook workbook,
        IXLWorksheet worksheet,
        LogType logType,
        int rowNumber)
    {
        IReadOnlyList<string> headers =
            DocumentationLogWorkbookSchema.GetHeaders(logType);
        IXLTable[] tables = worksheet.Tables.ToArray();

        if (tables.Length != 1)
        {
            throw Failure(
                tables.Length > 1
                    ? DocumentationLogWorkbookErrorCategory.AmbiguousSchema
                    : DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                expectedContract: null,
                workbookPath: null,
                "The workbook must contain exactly one approved documentation-log Excel Table.");
        }

        IXLTable table = tables[0];
        int effectiveLastRow = Math.Max(
            Math.Max(4, rowNumber),
            table.RangeAddress.LastAddress.RowNumber);
        table.ShowTotalsRow = false;
        table.Resize(3, 1, effectiveLastRow, headers.Count);
        table.Name = DocumentationLogWorkbookSchema.GetTableName(logType);
        table.ShowHeaderRow = true;
        table.ShowTotalsRow = false;
        table.ShowAutoFilter = true;

        if (workbook.Worksheets.SelectMany(item => item.Tables).Count() != 1)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.AmbiguousSchema,
                expectedContract: null,
                workbookPath: null,
                "The workbook contains unexpected additional Excel Tables.");
        }
    }

    private static WorkbookSnapshot InspectWorkbookContent(
        ReadOnlyMemory<byte> content,
        DocumentationLogWorkbookContract contract,
        string? workbookPath)
    {
        using MemoryStream input = CreateReadStream(content);
        using XLWorkbook workbook = LoadWorkbook(
            input,
            contract,
            workbookPath);
        return InspectWorkbook(workbook, contract, workbookPath);
    }

    private static WorkbookSnapshot InspectWorkbook(
        XLWorkbook workbook,
        DocumentationLogWorkbookContract contract,
        string? workbookPath)
    {
        IXLWorksheet? metadata = workbook.Worksheets
            .SingleOrDefault(worksheet => worksheet.Name.Equals(
                DocumentationLogWorkbookSchema.MetadataWorksheetName,
                StringComparison.Ordinal));

        if (metadata is null)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.MissingMetadata,
                contract,
                workbookPath,
                "The workbook is missing its dashboard schema metadata sheet.");
        }

        ValidateMetadata(metadata, contract, workbookPath);

        if (workbook.Worksheets.Count != 2)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                contract,
                workbookPath,
                "The workbook must contain exactly one data sheet and its hidden dashboard metadata sheet.");
        }

        IXLWorksheet? worksheet = workbook.Worksheets
            .SingleOrDefault(candidate => candidate.Name.Equals(
                DocumentationLogWorkbookSchema.DataWorksheetName,
                StringComparison.Ordinal));

        if (worksheet is null
            || worksheet.Visibility != XLWorksheetVisibility.Visible)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                contract,
                workbookPath,
                "The workbook is missing its visible documentation-log data sheet.");
        }

        ValidateTitleAndHeaders(worksheet, contract, workbookPath);
        int lastBusinessRow = ValidateTableAndRows(
            workbook,
            worksheet,
            contract,
            workbookPath);
        int dataRowCount = CountDataRows(
            worksheet,
            DocumentationLogWorkbookSchema.GetHeaders(contract.LogType).Count,
            lastBusinessRow);

        return new WorkbookSnapshot(
            worksheet.Name,
            dataRowCount,
            lastBusinessRow);
    }

    private static void ValidateMetadata(
        IXLWorksheet metadata,
        DocumentationLogWorkbookContract contract,
        string? workbookPath)
    {
        if (metadata.Visibility == XLWorksheetVisibility.Visible)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                contract,
                workbookPath,
                "The dashboard metadata sheet must remain hidden.");
        }

        string[] keys =
        [
            "schemaName",
            "schemaVersion",
            "logType",
            "scopeType",
            "scopeId"
        ];

        for (int row = 1; row <= keys.Length; row++)
        {
            if (!metadata.Cell(row, 1).GetString().Equals(
                    keys[row - 1],
                    StringComparison.Ordinal))
            {
                throw Failure(
                    DocumentationLogWorkbookErrorCategory.MissingMetadata,
                    contract,
                    workbookPath,
                    "The dashboard metadata marker is missing or malformed.");
            }
        }

        IXLCell? lastMetadataCell = metadata.LastCellUsed(
            XLCellsUsedOptions.Contents);
        if (lastMetadataCell is not null
            && (lastMetadataCell.Address.RowNumber > 5
                || lastMetadataCell.Address.ColumnNumber > 2))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                contract,
                workbookPath,
                "The dashboard metadata sheet contains unsupported marker fields.");
        }

        if (!metadata.Cell(1, 2).GetString().Equals(
                DocumentationLogWorkbookSchema.SchemaName,
                StringComparison.Ordinal))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.MissingMetadata,
                contract,
                workbookPath,
                "The workbook does not contain the approved dashboard schema marker.");
        }

        IXLCell versionCell = metadata.Cell(2, 2);
        if (versionCell.DataType != XLDataType.Number
            || versionCell.GetDouble()
                != DocumentationLogWorkbookSchema.CurrentSchemaVersion)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.UnsupportedSchemaVersion,
                contract,
                workbookPath,
                $"The workbook schema version is unsupported; expected {DocumentationLogWorkbookSchema.CurrentSchemaVersion}.");
        }

        if (!metadata.Cell(3, 2).GetString().Equals(
                contract.LogType.ToString(),
                StringComparison.Ordinal))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.WrongLogType,
                contract,
                workbookPath,
                $"The workbook belongs to a different log type; select a {DocumentationLogWorkbookSchema.GetDisplayName(contract.LogType)} workbook.");
        }

        if (!metadata.Cell(4, 2).GetString().Equals(
                contract.ScopeType.ToString(),
                StringComparison.Ordinal)
            || !metadata.Cell(5, 2).GetString().Equals(
                contract.ScopeId,
                StringComparison.Ordinal))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.WrongScope,
                contract,
                workbookPath,
                "The workbook belongs to a different Running, Hotel, or PMS scope.");
        }
    }

    private static void ValidateTitleAndHeaders(
        IXLWorksheet worksheet,
        DocumentationLogWorkbookContract contract,
        string? workbookPath)
    {
        IReadOnlyList<string> headers =
            DocumentationLogWorkbookSchema.GetHeaders(contract.LogType);

        if (!worksheet.Cell(1, 1).GetString().Equals(
                contract.Title,
                StringComparison.Ordinal)
            || worksheet.Row(1).CellsUsed(XLCellsUsedOptions.Contents)
                .Any(cell => cell.Address.ColumnNumber != 1))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                contract,
                workbookPath,
                "The workbook title does not match its approved type and scope.");
        }

        if (worksheet.Row(2).CellsUsed(XLCellsUsedOptions.Contents).Any())
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                contract,
                workbookPath,
                "Workbook row 2 must remain blank.");
        }

        for (int column = 1; column <= headers.Count; column++)
        {
            if (!worksheet.Cell(3, column).GetString().Equals(
                    headers[column - 1],
                    StringComparison.Ordinal))
            {
                throw Failure(
                    DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                    contract,
                    workbookPath,
                    "The workbook headers are missing or have changed from the approved schema.");
            }
        }

        if (worksheet.Row(3).CellsUsed(XLCellsUsedOptions.Contents)
            .Any(cell => cell.Address.ColumnNumber > headers.Count))
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                contract,
                workbookPath,
                "The workbook contains unexpected header columns.");
        }
    }

    private static int ValidateTableAndRows(
        XLWorkbook workbook,
        IXLWorksheet worksheet,
        DocumentationLogWorkbookContract contract,
        string? workbookPath)
    {
        IReadOnlyList<string> headers =
            DocumentationLogWorkbookSchema.GetHeaders(contract.LogType);
        IXLTable[] worksheetTables = worksheet.Tables.ToArray();
        int allTableCount = workbook.Worksheets
            .SelectMany(candidate => candidate.Tables)
            .Count();

        if (worksheetTables.Length != 1 || allTableCount != 1)
        {
            DocumentationLogWorkbookErrorCategory category =
                worksheetTables.Length > 1 || allTableCount > 1
                    ? DocumentationLogWorkbookErrorCategory.AmbiguousSchema
                    : DocumentationLogWorkbookErrorCategory.IncompatibleSchema;
            throw Failure(
                category,
                contract,
                workbookPath,
                "The workbook must contain exactly one approved documentation-log Excel Table.");
        }

        IXLTable table = worksheetTables[0];
        if (!table.Name.Equals(
                DocumentationLogWorkbookSchema.GetTableName(contract.LogType),
                StringComparison.Ordinal)
            || table.RangeAddress.FirstAddress.RowNumber != 3
            || table.RangeAddress.FirstAddress.ColumnNumber != 1
            || table.RangeAddress.LastAddress.ColumnNumber != headers.Count
            || table.RangeAddress.LastAddress.RowNumber < 4
            || table.ColumnCount() != headers.Count
            || !table.ShowHeaderRow
            || table.ShowTotalsRow
            || !table.ShowAutoFilter
            || worksheet.SheetView.SplitRow != 3)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                contract,
                workbookPath,
                "The workbook's Table, filter, or frozen-header structure is incompatible.");
        }

        foreach (IXLRange mergedRange in worksheet.MergedRanges)
        {
            if (RangesOverlap(
                    mergedRange.RangeAddress,
                    table.RangeAddress))
            {
                throw Failure(
                    DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                    contract,
                    workbookPath,
                    "Merged cells are not allowed inside the documentation-log data table.");
            }
        }

        IXLCell? lastUsed = worksheet.LastCellUsed(XLCellsUsedOptions.Contents);
        if (lastUsed is not null
            && lastUsed.Address.ColumnNumber > headers.Count)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                contract,
                workbookPath,
                "The workbook contains business content outside the approved columns.");
        }

        int lastBusinessRow = FindLastBusinessRow(
            worksheet,
            headers.Count);
        if (table.RangeAddress.LastAddress.RowNumber < lastBusinessRow)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                contract,
                workbookPath,
                "The workbook contains data rows outside its approved Excel Table.");
        }

        ValidateBusinessRows(
            worksheet,
            contract,
            workbookPath,
            lastBusinessRow);
        return lastBusinessRow;
    }

    private static void ValidateBusinessRows(
        IXLWorksheet worksheet,
        DocumentationLogWorkbookContract contract,
        string? workbookPath,
        int lastBusinessRow)
    {
        int columnCount =
            DocumentationLogWorkbookSchema.GetHeaders(contract.LogType).Count;
        IReadOnlySet<int> textColumns =
            DocumentationLogWorkbookSchema.GetTextColumnNumbers(
                contract.LogType);

        for (int row = 4; row <= lastBusinessRow; row++)
        {
            bool hasAnyValue = Enumerable.Range(1, columnCount)
                .Any(column => !worksheet.Cell(row, column).IsEmpty());
            if (!hasAnyValue)
            {
                continue;
            }

            if (Enumerable.Range(1, columnCount)
                .Any(column => worksheet.Cell(row, column).IsEmpty()))
            {
                throw Failure(
                    DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                    contract,
                    workbookPath,
                    $"Documentation-log row {row} is incomplete.");
            }

            if (worksheet.Cell(row, 1).DataType != XLDataType.DateTime)
            {
                throw Failure(
                    DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                    contract,
                    workbookPath,
                    $"Documentation-log row {row} does not contain a real Excel Date/Time value.");
            }

            foreach (int column in textColumns)
            {
                IXLCell cell = worksheet.Cell(row, column);
                if (cell.DataType != XLDataType.Text
                    || !cell.Style.NumberFormat.Format.Equals(
                        TextNumberFormat,
                        StringComparison.Ordinal))
                {
                    throw Failure(
                        DocumentationLogWorkbookErrorCategory.IncompatibleSchema,
                        contract,
                        workbookPath,
                        $"Documentation-log row {row} has an ID that is not stored explicitly as text.");
                }
            }
        }
    }

    private static void ValidateContractForEvent(
        DocumentationLogWorkbookContract contract,
        DocumentationLogEvent logEvent)
    {
        if (contract.LogType != logEvent.LogType)
        {
            throw new ArgumentException(
                "The workbook contract and documentation event must use the same log type.",
                nameof(logEvent));
        }

        if (logEvent.Hotels.Count == 0)
        {
            throw new ArgumentException(
                "A documentation event must resolve at least one canonical Hotel.",
                nameof(logEvent));
        }

        if (logEvent.LogType == LogType.DebuggingLog
            && logEvent.Hotels.Count != 1)
        {
            throw new ArgumentException(
                "A Debugging Log event must resolve exactly one canonical Hotel.",
                nameof(logEvent));
        }

        HashSet<string> hotelIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (DocumentationLogHotel hotel in logEvent.Hotels)
        {
            _ = RequireValue(hotel.HotelId, "Hotel ID");
            _ = RequireValue(hotel.HotelName, "Hotel Name");
            _ = RequireValue(hotel.FolderName, "Hotel folder");
            ArgumentNullException.ThrowIfNull(hotel.Pms);
            _ = RequireValue(hotel.Pms.PmsName, "PMS Name");
            _ = RequireValue(hotel.Pms.FolderName, "PMS folder");

            if (!hotelIds.Add(hotel.HotelId))
            {
                throw new ArgumentException(
                    "A documentation event cannot contain the same Hotel ID more than once.",
                    nameof(logEvent));
            }
        }

        foreach (DocumentationLogFormFieldDefinition field
                 in DocumentationLogWorkbookSchema.GetFormFields(
                     logEvent.LogType)
                     .Where(field => field.IsRequired
                                     && field.Key
                                         != DocumentationLogFieldKeys.HotelIds))
        {
            _ = RequireValue(logEvent.GetValue(field.Key), field.Label);
        }

        if (contract.ScopeType == DocumentationLogScopeType.Hotel)
        {
            DocumentationLogHotel[] matches = logEvent.Hotels
                .Where(hotel => hotel.HotelId.Equals(
                    contract.ScopeId,
                    StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1
                || !contract.Equals(
                    DocumentationLogWorkbookSchema
                        .CreateHotelHistoryContract(
                            contract.LogType,
                            matches[0])))
            {
                throw new ArgumentException(
                    "The Hotel-history contract is not an applicable canonical destination for this event.",
                    nameof(contract));
            }
        }
        else if (contract.ScopeType == DocumentationLogScopeType.PMS)
        {
            DocumentationLogPms? pms = logEvent.Hotels
                .Select(hotel => hotel.Pms)
                .FirstOrDefault(candidate => candidate.PmsName.Equals(
                    contract.ScopeId,
                    StringComparison.Ordinal));
            if (pms is null
                || !contract.Equals(
                    DocumentationLogWorkbookSchema
                        .CreatePmsHistoryContract(contract.LogType, pms)))
            {
                throw new ArgumentException(
                    "The PMS-history contract is not an applicable canonical destination for this event.",
                    nameof(contract));
            }
        }
    }

    private static void WriteEventRow(
        IXLWorksheet worksheet,
        int rowNumber,
        DocumentationLogEvent logEvent)
    {
        worksheet.Cell(rowNumber, 1).Style.NumberFormat.Format =
            DateTimeNumberFormat;
        // DateTime deliberately preserves the event's captured wall-clock
        // value; LocalDateTime would reconvert using the machine time zone.
        worksheet.Cell(rowNumber, 1).Value = logEvent.Timestamp.DateTime;
        WriteTextCell(worksheet.Cell(rowNumber, 2), logEvent.LogId);

        switch (logEvent.LogType)
        {
            case LogType.DebuggingLog:
                DocumentationLogHotel hotel = logEvent.Hotels.Single();
                worksheet.Cell(rowNumber, 3).Value =
                    RequireValue(hotel.HotelName, "Hotel Name");
                WriteTextCell(
                    worksheet.Cell(rowNumber, 4),
                    RequireValue(hotel.HotelId, "Hotel ID"));
                worksheet.Cell(rowNumber, 5).Value =
                    RequireValue(hotel.Pms.PmsName, "PMS Name");
                worksheet.Cell(rowNumber, 6).Value = RequiredField(
                    logEvent,
                    DocumentationLogFieldKeys.ErrorShownOnTicket,
                    "Error Shown On Ticket");
                worksheet.Cell(rowNumber, 7).Value = RequiredField(
                    logEvent,
                    DocumentationLogFieldKeys.RootCause,
                    "Root Cause");
                worksheet.Cell(rowNumber, 8).Value = RequiredField(
                    logEvent,
                    DocumentationLogFieldKeys.FixApplied,
                    "Fix Applied");
                worksheet.Cell(rowNumber, 9).Value = OptionalField(
                    logEvent,
                    DocumentationLogFieldKeys.CreatedBy);
                worksheet.Cell(rowNumber, 10).Value = OptionalField(
                    logEvent,
                    DocumentationLogFieldKeys.NotesFollowUp);
                break;

            case LogType.ScriptEditingLog:
                WriteTextCell(
                    worksheet.Cell(rowNumber, 3),
                    RequireValue(logEvent.CanonicalHotelIds, "Hotel IDs"));
                worksheet.Cell(rowNumber, 4).Value = RequiredField(
                    logEvent,
                    DocumentationLogFieldKeys.ScriptName,
                    "Script Name");
                worksheet.Cell(rowNumber, 5).Value = RequiredField(
                    logEvent,
                    DocumentationLogFieldKeys.ReasonForEdit,
                    "Reason For Edit");
                worksheet.Cell(rowNumber, 6).Value = RequiredField(
                    logEvent,
                    DocumentationLogFieldKeys.ChangesMade,
                    "Changes Made");
                worksheet.Cell(rowNumber, 7).Value = OptionalField(
                    logEvent,
                    DocumentationLogFieldKeys.CreatedBy);
                worksheet.Cell(rowNumber, 8).Value = OptionalField(
                    logEvent,
                    DocumentationLogFieldKeys.NotesFollowUp);
                break;

            case LogType.ScriptCreationLog:
                WriteTextCell(
                    worksheet.Cell(rowNumber, 3),
                    RequireValue(logEvent.CanonicalHotelIds, "Hotel IDs"));
                worksheet.Cell(rowNumber, 4).Value = RequiredField(
                    logEvent,
                    DocumentationLogFieldKeys.ScriptName,
                    "Script Name");
                worksheet.Cell(rowNumber, 5).Value = RequiredField(
                    logEvent,
                    DocumentationLogFieldKeys.ReasonForCreation,
                    "Reason For Creation");
                worksheet.Cell(rowNumber, 6).Value = RequiredField(
                    logEvent,
                    DocumentationLogFieldKeys.ScriptPurpose,
                    "Script Purpose / What It Does");
                worksheet.Cell(rowNumber, 7).Value = OptionalField(
                    logEvent,
                    DocumentationLogFieldKeys.CreatedBy);
                worksheet.Cell(rowNumber, 8).Value = OptionalField(
                    logEvent,
                    DocumentationLogFieldKeys.NotesFollowUp);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(logEvent),
                    logEvent.LogType,
                    "Unsupported documentation log type.");
        }
    }

    private static void EnsureExactEventRow(
        IXLWorksheet worksheet,
        int rowNumber,
        DocumentationLogEvent logEvent,
        DocumentationLogWorkbookContract contract)
    {
        IXLCell timestampCell = worksheet.Cell(rowNumber, 1);
        if (timestampCell.DataType != XLDataType.DateTime
            || Math.Abs(
                (timestampCell.GetDateTime()
                    - logEvent.Timestamp.DateTime).Ticks)
                > TimeSpan.TicksPerMillisecond)
        {
            throw VerificationFailure(
                contract,
                "The staged workbook does not contain the expected real Date/Time value.");
        }

        string[] expectedValues = CreateExpectedTextValues(logEvent);
        for (int column = 2; column <= expectedValues.Length + 1; column++)
        {
            if (!worksheet.Cell(rowNumber, column).GetString().Equals(
                    expectedValues[column - 2],
                    StringComparison.Ordinal))
            {
                throw VerificationFailure(
                    contract,
                    "The staged workbook does not contain the exact expected appended row.");
            }
        }
    }

    private static string[] CreateExpectedTextValues(
        DocumentationLogEvent logEvent)
    {
        return logEvent.LogType switch
        {
            LogType.DebuggingLog =>
            [
                logEvent.LogId,
                RequireValue(logEvent.Hotels.Single().HotelName, "Hotel Name"),
                RequireValue(logEvent.Hotels.Single().HotelId, "Hotel ID"),
                RequireValue(logEvent.Hotels.Single().Pms.PmsName, "PMS Name"),
                RequiredField(logEvent, DocumentationLogFieldKeys.ErrorShownOnTicket, "Error Shown On Ticket"),
                RequiredField(logEvent, DocumentationLogFieldKeys.RootCause, "Root Cause"),
                RequiredField(logEvent, DocumentationLogFieldKeys.FixApplied, "Fix Applied"),
                OptionalField(logEvent, DocumentationLogFieldKeys.CreatedBy),
                OptionalField(logEvent, DocumentationLogFieldKeys.NotesFollowUp)
            ],
            LogType.ScriptEditingLog =>
            [
                logEvent.LogId,
                RequireValue(logEvent.CanonicalHotelIds, "Hotel IDs"),
                RequiredField(logEvent, DocumentationLogFieldKeys.ScriptName, "Script Name"),
                RequiredField(logEvent, DocumentationLogFieldKeys.ReasonForEdit, "Reason For Edit"),
                RequiredField(logEvent, DocumentationLogFieldKeys.ChangesMade, "Changes Made"),
                OptionalField(logEvent, DocumentationLogFieldKeys.CreatedBy),
                OptionalField(logEvent, DocumentationLogFieldKeys.NotesFollowUp)
            ],
            LogType.ScriptCreationLog =>
            [
                logEvent.LogId,
                RequireValue(logEvent.CanonicalHotelIds, "Hotel IDs"),
                RequiredField(logEvent, DocumentationLogFieldKeys.ScriptName, "Script Name"),
                RequiredField(logEvent, DocumentationLogFieldKeys.ReasonForCreation, "Reason For Creation"),
                RequiredField(logEvent, DocumentationLogFieldKeys.ScriptPurpose, "Script Purpose / What It Does"),
                OptionalField(logEvent, DocumentationLogFieldKeys.CreatedBy),
                OptionalField(logEvent, DocumentationLogFieldKeys.NotesFollowUp)
            ],
            _ => throw new ArgumentOutOfRangeException(
                nameof(logEvent),
                logEvent.LogType,
                "Unsupported documentation log type.")
        };
    }

    private static string RequiredField(
        DocumentationLogEvent logEvent,
        string key,
        string label)
    {
        return RequireValue(logEvent.GetValue(key), label);
    }

    private static string OptionalField(
        DocumentationLogEvent logEvent,
        string key)
    {
        string value = logEvent.GetValue(key).Trim();
        return value.Length == 0 ? "N/A" : value;
    }

    private static string RequireValue(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{label} is required for a documentation-log workbook row.");
        }

        return value.Trim();
    }

    private static void WriteTextCell(IXLCell cell, string value)
    {
        cell.Style.NumberFormat.Format = TextNumberFormat;
        cell.Value = value;
    }

    private static int FindLastBusinessRow(
        IXLWorksheet worksheet,
        int columnCount)
    {
        int candidate = worksheet.LastRowUsed(
                XLCellsUsedOptions.Contents)?.RowNumber()
            ?? 3;

        for (int row = candidate; row >= 4; row--)
        {
            if (Enumerable.Range(1, columnCount)
                .Any(column => !worksheet.Cell(row, column).IsEmpty()))
            {
                return row;
            }
        }

        return 3;
    }

    private static int CountDataRows(
        IXLWorksheet worksheet,
        int columnCount,
        int lastBusinessRow)
    {
        int count = 0;
        for (int row = 4; row <= lastBusinessRow; row++)
        {
            if (Enumerable.Range(1, columnCount)
                .Any(column => !worksheet.Cell(row, column).IsEmpty()))
            {
                count++;
            }
        }

        return count;
    }

    private static bool RangesOverlap(
        IXLRangeAddress left,
        IXLRangeAddress right)
    {
        return left.FirstAddress.RowNumber <= right.LastAddress.RowNumber
            && left.LastAddress.RowNumber >= right.FirstAddress.RowNumber
            && left.FirstAddress.ColumnNumber
                <= right.LastAddress.ColumnNumber
            && left.LastAddress.ColumnNumber
                >= right.FirstAddress.ColumnNumber;
    }

    private static XLWorkbook LoadWorkbook(
        Stream input,
        DocumentationLogWorkbookContract contract,
        string? workbookPath)
    {
        try
        {
            return new XLWorkbook(input);
        }
        catch (DocumentationLogWorkbookException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw Failure(
                DocumentationLogWorkbookErrorCategory.Corrupt,
                contract,
                workbookPath,
                "The documentation-log workbook is corrupt or cannot be read as an .xlsx workbook.",
                exception);
        }
    }

    private static byte[] Serialize(
        XLWorkbook workbook,
        DocumentationLogWorkbookContract contract)
    {
        try
        {
            using MemoryStream output = new();
            workbook.SaveAs(output);
            return output.ToArray();
        }
        catch (Exception exception)
        {
            throw VerificationFailure(
                contract,
                "The documentation-log workbook could not be serialized for staging.",
                exception);
        }
    }

    private static MemoryStream CreateReadStream(ReadOnlyMemory<byte> content)
    {
        return new MemoryStream(content.ToArray(), writable: false);
    }

    private static DocumentationLogWorkbookException VerificationFailure(
        DocumentationLogWorkbookContract contract,
        string message,
        Exception? innerException = null)
    {
        return Failure(
            DocumentationLogWorkbookErrorCategory.ContentVerificationFailure,
            contract,
            workbookPath: null,
            message,
            innerException);
    }

    private static DocumentationLogWorkbookException Failure(
        DocumentationLogWorkbookErrorCategory category,
        DocumentationLogWorkbookContract? expectedContract,
        string? workbookPath,
        string message,
        Exception? innerException = null)
    {
        return new DocumentationLogWorkbookException(
            category,
            expectedContract,
            workbookPath,
            message,
            innerException);
    }

    private static bool IsInternalOrLockFile(string fileName)
    {
        return fileName.StartsWith("~$", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith(".", StringComparison.Ordinal)
            || fileName.Contains(
                ".documentation-log-",
                StringComparison.OrdinalIgnoreCase)
            || fileName.Contains(".stage.", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains(
                ".rollback.",
                StringComparison.OrdinalIgnoreCase);
    }

    private void TryDeleteOwnedFile(string path)
    {
        try
        {
            fileOperations.Delete(path);
        }
        catch (Exception)
        {
            // Preserve the creation or persisted-content verification failure.
        }
    }

    private static bool IsInvalidPathException(Exception exception)
    {
        return exception is ArgumentException
            or InvalidOperationException
            or NotSupportedException
            or PathTooLongException
            or System.Security.SecurityException;
    }

    private static bool IsFileSystemException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException;
    }

    private sealed record WorkbookSnapshot(
        string WorksheetName,
        int DataRowCount,
        int LastBusinessRowNumber);
}
