using System.Globalization;
using ClosedXML.Excel;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Owns the exact Surface and Hotel-history workbook schemas. Live paired-save
/// replacement remains the responsibility of <see cref="QuickQaSaveService"/>.
/// </summary>
public sealed class QuickQaWorkbookService
{
    public const string SurfaceWorksheetName = "Surface QA";
    public const string SurfaceTableName = "SurfaceQaTable";
    public const string HistoryWorksheetName = "Quick QA History";
    public const string HistoryTableName = "QuickQaHistoryTable";
    public const string HistoryFileName = "QuickQAHistory.xlsx";

    private static readonly string[] CanonicalSurfaceHeaders =
    [
        "File Month",
        "Hotel Name",
        "Hotel ID",
        "PMS",
        "File ID",
        "Result",
        "Summary"
    ];

    private static readonly string[] CanonicalHistoryHeaders =
    [
        "QA Timestamp",
        "File Month",
        "Hotel Name",
        "Hotel ID",
        "PMS",
        "File ID",
        "Result",
        "Summary"
    ];

    private static readonly string[] LegacySurfaceHeaders =
    [
        "Month 2026",
        "Hotel Name",
        "Hotel Id",
        "PMS",
        "file id",
        "Pass/Fail/Warning",
        "summary"
    ];

    private readonly QaStoragePaths paths;
    private readonly QuickQaWorkbookFilenameService filenameService;
    private readonly IQuickQaTransactionFileOperations fileOperations;

    public QuickQaWorkbookService(QaStoragePaths paths)
        : this(
            paths,
            new QuickQaWorkbookFilenameService(paths),
            new QuickQaTransactionFileOperations())
    {
    }

    public QuickQaWorkbookService(
        QaStoragePaths paths,
        QuickQaWorkbookFilenameService filenameService,
        IQuickQaTransactionFileOperations fileOperations)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.filenameService = filenameService
            ?? throw new ArgumentNullException(nameof(filenameService));
        this.fileOperations = fileOperations
            ?? throw new ArgumentNullException(nameof(fileOperations));
    }

    public static IReadOnlyList<string> SurfaceHeaders =>
        Array.AsReadOnly(CanonicalSurfaceHeaders);

    public static IReadOnlyList<string> HistoryHeaders =>
        Array.AsReadOnly(CanonicalHistoryHeaders);

    /// <summary>
    /// Returns compatible direct-child workbooks, newest first. Lock files,
    /// transaction artifacts, corrupt files, and incompatible files are omitted.
    /// </summary>
    public IReadOnlyList<string> GetCompatibleSurfaceWorkbookFileNames()
    {
        QuickQaPathSafety.EnsureNoReparsePoints(
            paths.QaReportsRootPath,
            paths.SurfaceQaRootPath);

        if (!Directory.Exists(paths.SurfaceQaRootPath))
        {
            return Array.Empty<string>();
        }

        List<QuickQaSurfaceWorkbookInfo> compatible = [];

        foreach (string path in Directory.EnumerateFiles(
                     paths.SurfaceQaRootPath,
                     "*.xlsx",
                     SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileName(path);

            if (IsInternalOrLockFile(fileName)
                || !QuickQaWorkbookFilenameService
                    .IsSafeStoredWorkbookFileName(fileName))
            {
                continue;
            }

            try
            {
                compatible.Add(ValidateSurfaceWorkbook(fileName));
            }
            catch (QuickQaWorkbookException)
            {
                // Discovery is best-effort. Explicit selection/save reports the
                // focused reason rather than making form startup fail.
            }
        }

        return compatible
            .OrderByDescending(item => item.LastWriteTimeUtc)
            .ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.FileName)
            .ToArray();
    }

    public QuickQaSurfaceWorkbookInfo ValidateSurfaceWorkbook(
        string workbookFileName)
    {
        string safeFileName;
        string fullPath;

        try
        {
            safeFileName = filenameService.ValidateStoredWorkbookFileName(
                workbookFileName);
            fullPath = paths.ResolveSurfaceQaWorkbookPath(safeFileName);
            QuickQaPathSafety.EnsureNoReparsePoints(
                paths.QaReportsRootPath,
                fullPath);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or NotSupportedException
                or PathTooLongException)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.InvalidFilename,
                QuickQaWorkbookRole.Surface,
                "The selected Surface QA workbook filename is not valid.",
                exception);
        }

        if (!fileOperations.FileExists(fullPath))
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.NotFound,
                QuickQaWorkbookRole.Surface,
                "The selected Surface QA workbook no longer exists.");
        }

        byte[] content;

        try
        {
            content = fileOperations.ReadAllBytes(fullPath);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.InUseOrUnavailable,
                QuickQaWorkbookRole.Surface,
                "The Surface QA workbook is currently open or unavailable. Close the workbook and try again.",
                exception);
        }

        SurfaceWorkbookSnapshot snapshot = InspectSurfaceWorkbook(content);
        DateTime lastWriteTimeUtc;

        try
        {
            if (!fileOperations.FileExists(fullPath))
            {
                throw new QuickQaWorkbookException(
                    QuickQaWorkbookErrorCategory.NotFound,
                    QuickQaWorkbookRole.Surface,
                    "The selected Surface QA workbook no longer exists.");
            }

            lastWriteTimeUtc = fileOperations.GetLastWriteTimeUtc(fullPath);
        }
        catch (QuickQaWorkbookException)
        {
            throw;
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.InUseOrUnavailable,
                QuickQaWorkbookRole.Surface,
                "The Surface QA workbook is currently open or unavailable. Close the workbook and try again.",
                exception);
        }

        return new QuickQaSurfaceWorkbookInfo(
            safeFileName,
            fullPath,
            snapshot.WorksheetName,
            snapshot.DataRowCount,
            snapshot.UsedLegacyHeaders,
            lastWriteTimeUtc);
    }

    /// <summary>
    /// Creates a canonical, empty Surface workbook with create-new semantics and
    /// returns its normalized filename.
    /// </summary>
    public string CreateNewSurfaceWorkbook(string requestedFileName)
    {
        string fullPath;

        try
        {
            fullPath = filenameService.ResolveNewWorkbookPath(requestedFileName);
            QuickQaPathSafety.EnsureNoReparsePoints(
                paths.QaReportsRootPath,
                fullPath);
        }
        catch (IOException exception)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.AlreadyExists,
                QuickQaWorkbookRole.Surface,
                "A Surface QA workbook with that filename already exists.",
                exception);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or NotSupportedException
                or PathTooLongException)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.InvalidFilename,
                QuickQaWorkbookRole.Surface,
                "The Surface QA workbook filename is not valid.",
                exception);
        }

        byte[] content = CreateEmptySurfaceWorkbookBytes();
        _ = InspectSurfaceWorkbook(content);
        bool created = false;

        try
        {
            fileOperations.CreateDirectory(paths.SurfaceQaRootPath);
            fileOperations.WriteNewAndFlush(fullPath, content);
            created = true;
            _ = ValidateSurfaceWorkbook(Path.GetFileName(fullPath));
            return Path.GetFileName(fullPath);
        }
        catch (QuickQaWorkbookException)
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

            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.InUseOrUnavailable,
                QuickQaWorkbookRole.Surface,
                "The new Surface QA workbook could not be created in the configured folder.",
                exception);
        }
    }

    internal QuickQaWorkbookBuildResult BuildSurfaceAppend(
        ReadOnlyMemory<byte> sourceContent,
        QuickQaWorkbookRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        using MemoryStream input = CreateReadStream(sourceContent);
        using XLWorkbook workbook = LoadWorkbook(
            input,
            QuickQaWorkbookRole.Surface);
        SurfaceWorksheetMatch match = FindSurfaceWorksheet(workbook);
        IXLWorksheet worksheet = match.Worksheet;
        int previousDataRowCount = CountDataRows(
            worksheet,
            CanonicalSurfaceHeaders.Length);
        int newRowNumber = FindLastBusinessRow(
            worksheet,
            CanonicalSurfaceHeaders.Length) + 1;

        CanonicalizeHeaders(worksheet, CanonicalSurfaceHeaders);
        WriteSurfaceRow(worksheet, newRowNumber, row);
        EnsureTable(
            workbook,
            worksheet,
            SurfaceTableName,
            CanonicalSurfaceHeaders.Length,
            newRowNumber);
        ApplySurfacePresentation(worksheet, isNewWorkbook: false);

        byte[] content = Serialize(workbook);
        VerifySurfaceAppend(
            content,
            row,
            newRowNumber,
            previousDataRowCount + 1);
        return new QuickQaWorkbookBuildResult(
            content,
            newRowNumber,
            previousDataRowCount);
    }

    internal QuickQaWorkbookBuildResult BuildHistoryAppend(
        ReadOnlyMemory<byte>? sourceContent,
        QuickQaWorkbookRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        using XLWorkbook workbook = sourceContent is null
            ? CreateEmptyHistoryWorkbook()
            : LoadWorkbook(
                CreateReadStream(sourceContent.Value),
                QuickQaWorkbookRole.HotelHistory);
        IXLWorksheet worksheet = GetHistoryWorksheet(workbook);
        ValidateHistoryHeaders(worksheet);
        int previousDataRowCount = CountDataRows(
            worksheet,
            CanonicalHistoryHeaders.Length);
        int newRowNumber = FindLastBusinessRow(
            worksheet,
            CanonicalHistoryHeaders.Length) + 1;

        WriteHistoryRow(worksheet, newRowNumber, row);
        EnsureTable(
            workbook,
            worksheet,
            HistoryTableName,
            CanonicalHistoryHeaders.Length,
            newRowNumber);
        ApplyHistoryPresentation(worksheet);

        byte[] content = Serialize(workbook);
        VerifyHistoryAppend(
            content,
            row,
            newRowNumber,
            previousDataRowCount + 1);
        return new QuickQaWorkbookBuildResult(
            content,
            newRowNumber,
            previousDataRowCount);
    }

    internal void VerifySurfaceAppend(
        ReadOnlyMemory<byte> content,
        QuickQaWorkbookRow expected,
        int expectedRowNumber,
        int expectedDataRowCount)
    {
        using MemoryStream input = CreateReadStream(content);
        using XLWorkbook workbook = LoadWorkbook(
            input,
            QuickQaWorkbookRole.Surface);
        SurfaceWorksheetMatch match = FindSurfaceWorksheet(workbook);
        IXLWorksheet worksheet = match.Worksheet;

        EnsureExactRow(
            worksheet,
            expectedRowNumber,
            CreateSurfaceValues(expected),
            QuickQaWorkbookRole.Surface);
        EnsureDataRowCount(
            worksheet,
            CanonicalSurfaceHeaders.Length,
            expectedDataRowCount,
            QuickQaWorkbookRole.Surface);
        EnsurePresentation(
            worksheet,
            SurfaceTableName,
            CanonicalSurfaceHeaders.Length,
            expectedRowNumber,
            QuickQaWorkbookRole.Surface);
    }

    internal void VerifyHistoryAppend(
        ReadOnlyMemory<byte> content,
        QuickQaWorkbookRow expected,
        int expectedRowNumber,
        int expectedDataRowCount)
    {
        using MemoryStream input = CreateReadStream(content);
        using XLWorkbook workbook = LoadWorkbook(
            input,
            QuickQaWorkbookRole.HotelHistory);
        IXLWorksheet worksheet = GetHistoryWorksheet(workbook);
        ValidateHistoryHeaders(worksheet);

        EnsureExactRow(
            worksheet,
            expectedRowNumber,
            CreateHistoryValues(expected),
            QuickQaWorkbookRole.HotelHistory);
        EnsureDataRowCount(
            worksheet,
            CanonicalHistoryHeaders.Length,
            expectedDataRowCount,
            QuickQaWorkbookRole.HotelHistory);
        EnsurePresentation(
            worksheet,
            HistoryTableName,
            CanonicalHistoryHeaders.Length,
            expectedRowNumber,
            QuickQaWorkbookRole.HotelHistory);
    }

    private byte[] CreateEmptySurfaceWorkbookBytes()
    {
        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add(
            SurfaceWorksheetName);
        CanonicalizeHeaders(worksheet, CanonicalSurfaceHeaders);
        EnsureTable(
            workbook,
            worksheet,
            SurfaceTableName,
            CanonicalSurfaceHeaders.Length,
            lastRowNumber: 2);
        ApplySurfacePresentation(worksheet, isNewWorkbook: true);
        return Serialize(workbook);
    }

    private static XLWorkbook CreateEmptyHistoryWorkbook()
    {
        XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add(
            HistoryWorksheetName);
        CanonicalizeHeaders(worksheet, CanonicalHistoryHeaders);
        EnsureTable(
            workbook,
            worksheet,
            HistoryTableName,
            CanonicalHistoryHeaders.Length,
            lastRowNumber: 2);
        ApplyHistoryPresentation(worksheet);
        return workbook;
    }

    private SurfaceWorkbookSnapshot InspectSurfaceWorkbook(byte[] content)
    {
        using MemoryStream input = new(content, writable: false);
        using XLWorkbook workbook = LoadWorkbook(
            input,
            QuickQaWorkbookRole.Surface);
        SurfaceWorksheetMatch match = FindSurfaceWorksheet(workbook);
        ValidateSurfaceTableCompatibility(
            workbook,
            match.Worksheet);

        return new SurfaceWorkbookSnapshot(
            match.Worksheet.Name,
            CountDataRows(
                match.Worksheet,
                CanonicalSurfaceHeaders.Length),
            match.UsedLegacyHeaders);
    }

    private static SurfaceWorksheetMatch FindSurfaceWorksheet(
        XLWorkbook workbook)
    {
        List<SurfaceWorksheetMatch> matches = [];

        foreach (IXLWorksheet worksheet in workbook.Worksheets)
        {
            if (TryMatchSurfaceHeaders(
                    worksheet,
                    out bool usedLegacyHeaders))
            {
                matches.Add(new SurfaceWorksheetMatch(
                    worksheet,
                    usedLegacyHeaders));
            }
        }

        if (matches.Count == 0)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.IncompatibleSchema,
                QuickQaWorkbookRole.Surface,
                "The Surface QA workbook does not contain the approved seven-column header schema.");
        }

        if (matches.Count != 1)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.AmbiguousSchema,
                QuickQaWorkbookRole.Surface,
                "The Surface QA workbook contains more than one worksheet with the approved header schema.");
        }

        return matches[0];
    }

    private static bool TryMatchSurfaceHeaders(
        IXLWorksheet worksheet,
        out bool usedLegacyHeaders)
    {
        string[] actualHeaders = Enumerable
            .Range(1, CanonicalSurfaceHeaders.Length)
            .Select(column => worksheet.Cell(1, column).GetString().Trim())
            .ToArray();
        bool canonicalMatch = HeadersEqual(
            actualHeaders,
            CanonicalSurfaceHeaders);
        bool legacyMatch = HeadersEqual(
            actualHeaders,
            LegacySurfaceHeaders);

        usedLegacyHeaders = legacyMatch && !canonicalMatch;

        IXLCell? lastHeaderCell = worksheet.Row(1).LastCellUsed(
            XLCellsUsedOptions.Contents);
        bool hasNoExtraHeader = lastHeaderCell is null
            || lastHeaderCell.Address.ColumnNumber
                <= CanonicalSurfaceHeaders.Length;

        return hasNoExtraHeader && (canonicalMatch || legacyMatch);
    }

    private static bool HeadersEqual(
        IReadOnlyList<string> actual,
        IReadOnlyList<string> expected)
    {
        return actual.Count == expected.Count
            && actual
                .Zip(expected)
                .All(pair => pair.First.Equals(
                    pair.Second,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static IXLWorksheet GetHistoryWorksheet(XLWorkbook workbook)
    {
        IXLWorksheet[] matches = workbook.Worksheets
            .Where(worksheet => worksheet.Name.Equals(
                HistoryWorksheetName,
                StringComparison.Ordinal))
            .ToArray();

        if (matches.Length != 1 || workbook.Worksheets.Count != 1)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.IncompatibleSchema,
                QuickQaWorkbookRole.HotelHistory,
                "The Hotel Quick QA history workbook has an incompatible worksheet structure.");
        }

        return matches[0];
    }

    private static void ValidateSurfaceTableCompatibility(
        XLWorkbook workbook,
        IXLWorksheet worksheet)
    {
        IXLTable[] worksheetTables = worksheet.Tables.ToArray();

        if (worksheetTables.Length > 1)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.AmbiguousSchema,
                QuickQaWorkbookRole.Surface,
                "The Surface QA workbook contains ambiguous Excel Table coverage.");
        }

        IXLTable? table = worksheetTables.SingleOrDefault();
        if (table is not null
            && (table.RangeAddress.FirstAddress.RowNumber != 1
                || table.RangeAddress.FirstAddress.ColumnNumber != 1
                || table.ColumnCount() != CanonicalSurfaceHeaders.Length))
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.IncompatibleSchema,
                QuickQaWorkbookRole.Surface,
                "The Surface QA workbook's existing Excel Table does not cover the approved schema.");
        }

        bool requiredNameUsedElsewhere = workbook.Worksheets
            .SelectMany(item => item.Tables)
            .Any(candidate =>
                candidate.Name.Equals(
                    SurfaceTableName,
                    StringComparison.OrdinalIgnoreCase)
                && !ReferenceEquals(candidate, table));

        if (requiredNameUsedElsewhere)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.AmbiguousSchema,
                QuickQaWorkbookRole.Surface,
                $"The required Excel Table name '{SurfaceTableName}' is already used elsewhere in the workbook.");
        }
    }

    private static void ValidateHistoryHeaders(IXLWorksheet worksheet)
    {
        for (int column = 1; column <= CanonicalHistoryHeaders.Length; column++)
        {
            if (!worksheet.Cell(1, column).GetString().Equals(
                    CanonicalHistoryHeaders[column - 1],
                    StringComparison.Ordinal))
            {
                throw new QuickQaWorkbookException(
                    QuickQaWorkbookErrorCategory.IncompatibleSchema,
                    QuickQaWorkbookRole.HotelHistory,
                    "The Hotel Quick QA history workbook has incompatible headers.");
            }
        }

        IXLCell? lastHeaderCell = worksheet.Row(1).LastCellUsed(
            XLCellsUsedOptions.Contents);
        if (lastHeaderCell is not null
            && lastHeaderCell.Address.ColumnNumber
                > CanonicalHistoryHeaders.Length)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.IncompatibleSchema,
                QuickQaWorkbookRole.HotelHistory,
                "The Hotel Quick QA history workbook has unexpected header columns.");
        }
    }

    private static XLWorkbook LoadWorkbook(
        Stream input,
        QuickQaWorkbookRole role)
    {
        try
        {
            return new XLWorkbook(input);
        }
        catch (QuickQaWorkbookException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.Corrupt,
                role,
                role == QuickQaWorkbookRole.Surface
                    ? "The Surface QA workbook is corrupt or cannot be read as an .xlsx workbook."
                    : "The Hotel Quick QA history workbook is corrupt or cannot be read as an .xlsx workbook.",
                exception);
        }
    }

    private static byte[] Serialize(XLWorkbook workbook)
    {
        using MemoryStream output = new();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static MemoryStream CreateReadStream(ReadOnlyMemory<byte> content)
    {
        return new MemoryStream(content.ToArray(), writable: false);
    }

    private static void CanonicalizeHeaders(
        IXLWorksheet worksheet,
        IReadOnlyList<string> headers)
    {
        for (int column = 1; column <= headers.Count; column++)
        {
            worksheet.Cell(1, column).Value = headers[column - 1];
        }
    }

    private static void WriteSurfaceRow(
        IXLWorksheet worksheet,
        int rowNumber,
        QuickQaWorkbookRow row)
    {
        WriteTextCell(worksheet.Cell(rowNumber, 1), row.FileMonth);
        worksheet.Cell(rowNumber, 2).Value = row.HotelName;
        WriteTextCell(worksheet.Cell(rowNumber, 3), row.HotelId);
        worksheet.Cell(rowNumber, 4).Value = row.PmsName;
        WriteTextCell(worksheet.Cell(rowNumber, 5), row.FileId);
        worksheet.Cell(rowNumber, 6).Value = row.ResultText;
        worksheet.Cell(rowNumber, 7).Value = row.Summary;
        worksheet.Cell(rowNumber, 7).Style.Alignment.WrapText = true;
    }

    private static void WriteHistoryRow(
        IXLWorksheet worksheet,
        int rowNumber,
        QuickQaWorkbookRow row)
    {
        WriteTextCell(
            worksheet.Cell(rowNumber, 1),
            row.Timestamp.ToString("O", CultureInfo.InvariantCulture));
        WriteTextCell(worksheet.Cell(rowNumber, 2), row.FileMonth);
        worksheet.Cell(rowNumber, 3).Value = row.HotelName;
        WriteTextCell(worksheet.Cell(rowNumber, 4), row.HotelId);
        worksheet.Cell(rowNumber, 5).Value = row.PmsName;
        WriteTextCell(worksheet.Cell(rowNumber, 6), row.FileId);
        worksheet.Cell(rowNumber, 7).Value = row.ResultText;
        worksheet.Cell(rowNumber, 8).Value = row.Summary;
        worksheet.Cell(rowNumber, 8).Style.Alignment.WrapText = true;
    }

    private static void WriteTextCell(IXLCell cell, string value)
    {
        cell.Style.NumberFormat.Format = "@";
        cell.Value = value;
    }

    private static void EnsureTable(
        XLWorkbook workbook,
        IXLWorksheet worksheet,
        string tableName,
        int columnCount,
        int lastRowNumber)
    {
        int effectiveLastRow = Math.Max(2, lastRowNumber);
        IXLTable[] worksheetTables = worksheet.Tables.ToArray();
        IXLTable? table = null;

        if (worksheetTables.Length > 1)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.AmbiguousSchema,
                tableName == SurfaceTableName
                    ? QuickQaWorkbookRole.Surface
                    : QuickQaWorkbookRole.HotelHistory,
                "The workbook contains ambiguous Excel Table coverage.");
        }

        if (worksheetTables.Length == 1)
        {
            table = worksheetTables[0];

            if (table.RangeAddress.FirstAddress.RowNumber != 1
                || table.RangeAddress.FirstAddress.ColumnNumber != 1
                || table.ColumnCount() != columnCount)
            {
                throw new QuickQaWorkbookException(
                    QuickQaWorkbookErrorCategory.IncompatibleSchema,
                    tableName == SurfaceTableName
                        ? QuickQaWorkbookRole.Surface
                        : QuickQaWorkbookRole.HotelHistory,
                    "The workbook's existing Excel Table does not cover the approved schema.");
            }
        }

        bool nameUsedElsewhere = workbook.Worksheets
            .SelectMany(item => item.Tables)
            .Any(candidate =>
                candidate.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase)
                && !ReferenceEquals(candidate, table));

        if (nameUsedElsewhere)
        {
            throw new QuickQaWorkbookException(
                QuickQaWorkbookErrorCategory.AmbiguousSchema,
                tableName == SurfaceTableName
                    ? QuickQaWorkbookRole.Surface
                    : QuickQaWorkbookRole.HotelHistory,
                $"The required Excel Table name '{tableName}' is already used elsewhere in the workbook.");
        }

        if (table is null)
        {
            table = worksheet.Range(
                    1,
                    1,
                    effectiveLastRow,
                    columnCount)
                .CreateTable(tableName);
        }
        else
        {
            table.ShowTotalsRow = false;
            table.Resize(1, 1, effectiveLastRow, columnCount);
            table.Name = tableName;
        }

        table.ShowHeaderRow = true;
        table.ShowTotalsRow = false;
        table.ShowAutoFilter = true;
    }

    private static void ApplySurfacePresentation(
        IXLWorksheet worksheet,
        bool isNewWorkbook)
    {
        worksheet.SheetView.FreezeRows(1);
        worksheet.Column(7).Style.Alignment.WrapText = true;

        if (!isNewWorkbook)
        {
            return;
        }

        double[] widths = [12, 36, 14, 22, 18, 20, 60];
        for (int column = 1; column <= widths.Length; column++)
        {
            worksheet.Column(column).Width = widths[column - 1];
        }
    }

    private static void ApplyHistoryPresentation(IXLWorksheet worksheet)
    {
        worksheet.SheetView.FreezeRows(1);
        worksheet.Column(8).Style.Alignment.WrapText = true;

        double[] widths = [30, 12, 36, 14, 22, 18, 20, 60];
        for (int column = 1; column <= widths.Length; column++)
        {
            if (worksheet.Column(column).Width <= 0
                || worksheet.Column(column).Width == 8.43)
            {
                worksheet.Column(column).Width = widths[column - 1];
            }
        }
    }

    private static int FindLastBusinessRow(
        IXLWorksheet worksheet,
        int columnCount)
    {
        int candidate = worksheet.LastRowUsed(
                XLCellsUsedOptions.Contents)?.RowNumber()
            ?? 1;

        for (int row = candidate; row >= 2; row--)
        {
            if (Enumerable.Range(1, columnCount).Any(
                    column => !worksheet.Cell(row, column).IsEmpty()))
            {
                return row;
            }
        }

        return 1;
    }

    private static int CountDataRows(
        IXLWorksheet worksheet,
        int columnCount)
    {
        int lastRow = FindLastBusinessRow(worksheet, columnCount);
        int count = 0;

        for (int row = 2; row <= lastRow; row++)
        {
            if (Enumerable.Range(1, columnCount).Any(
                    column => !worksheet.Cell(row, column).IsEmpty()))
            {
                count++;
            }
        }

        return count;
    }

    private static string[] CreateSurfaceValues(QuickQaWorkbookRow row)
    {
        return
        [
            row.FileMonth,
            row.HotelName,
            row.HotelId,
            row.PmsName,
            row.FileId,
            row.ResultText,
            row.Summary
        ];
    }

    private static string[] CreateHistoryValues(QuickQaWorkbookRow row)
    {
        return
        [
            row.Timestamp.ToString("O", CultureInfo.InvariantCulture),
            row.FileMonth,
            row.HotelName,
            row.HotelId,
            row.PmsName,
            row.FileId,
            row.ResultText,
            row.Summary
        ];
    }

    private static void EnsureExactRow(
        IXLWorksheet worksheet,
        int rowNumber,
        IReadOnlyList<string> expected,
        QuickQaWorkbookRole role)
    {
        for (int column = 1; column <= expected.Count; column++)
        {
            string actual = worksheet.Cell(rowNumber, column).GetString();
            if (!actual.Equals(expected[column - 1], StringComparison.Ordinal))
            {
                throw VerificationFailure(
                    role,
                    "The staged workbook does not contain the exact expected appended row.");
            }
        }
    }

    private static void EnsureDataRowCount(
        IXLWorksheet worksheet,
        int columnCount,
        int expectedCount,
        QuickQaWorkbookRole role)
    {
        if (CountDataRows(worksheet, columnCount) != expectedCount)
        {
            throw VerificationFailure(
                role,
                "The staged workbook data-row count is not the expected append-only count.");
        }
    }

    private static void EnsurePresentation(
        IXLWorksheet worksheet,
        string tableName,
        int columnCount,
        int expectedLastRow,
        QuickQaWorkbookRole role)
    {
        IXLTable[] tables = worksheet.Tables
            .Where(table => table.Name.Equals(
                tableName,
                StringComparison.Ordinal))
            .ToArray();

        if (tables.Length != 1
            || tables[0].RangeAddress.FirstAddress.RowNumber != 1
            || tables[0].RangeAddress.FirstAddress.ColumnNumber != 1
            || tables[0].RangeAddress.LastAddress.RowNumber
                < Math.Max(2, expectedLastRow)
            || tables[0].ColumnCount() != columnCount
            || !tables[0].ShowAutoFilter
            || worksheet.SheetView.SplitRow < 1)
        {
            throw VerificationFailure(
                role,
                "The staged workbook is missing its required Table, filter, or frozen header row.");
        }
    }

    private static QuickQaWorkbookException VerificationFailure(
        QuickQaWorkbookRole role,
        string message)
    {
        return new QuickQaWorkbookException(
            QuickQaWorkbookErrorCategory.ContentVerificationFailure,
            role,
            message);
    }

    private static bool IsInternalOrLockFile(string fileName)
    {
        return fileName.StartsWith("~$", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith(".", StringComparison.Ordinal)
            || fileName.Contains(".quickqa-", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains(".stage.", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains(".rollback.", StringComparison.OrdinalIgnoreCase);
    }

    private void TryDeleteOwnedFile(string path)
    {
        try
        {
            fileOperations.Delete(path);
        }
        catch (Exception)
        {
            // Preserve the creation/verification failure.
        }
    }

    private static bool IsFileSystemException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException;
    }

    private sealed record SurfaceWorksheetMatch(
        IXLWorksheet Worksheet,
        bool UsedLegacyHeaders);

    private sealed record SurfaceWorkbookSnapshot(
        string WorksheetName,
        int DataRowCount,
        bool UsedLegacyHeaders);
}
