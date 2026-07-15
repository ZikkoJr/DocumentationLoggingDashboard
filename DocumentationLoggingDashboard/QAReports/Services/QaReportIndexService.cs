using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Reads and prepares complete deterministic replacements for the dedicated QA report index.
/// </summary>
public sealed class QaReportIndexService
{
    private const string EntrySeparator = "---";
    private const int EntryLineCount = 13;
    private const int MaximumFilenameLength = 240;

    private static readonly UTF8Encoding Utf8NoBom = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private static readonly QaFolderNameSanitizer FolderNameSanitizer = new();
    private static readonly QaReportFilenameService FilenameService = new(
        FolderNameSanitizer);

    private readonly QaStoragePaths paths;

    public QaReportIndexService(QaStoragePaths paths)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <summary>
    /// Reads and strictly parses the current index without changing it.
    /// </summary>
    public IReadOnlyList<QaReportIndexEntry> LoadEntries()
    {
        return ReadSnapshot().Entries.ToArray();
    }

    /// <summary>
    /// Builds a complete one-entry-per-key replacement while retaining the exact
    /// source bytes and hash for a commit-time concurrency check.
    /// </summary>
    internal QaReportIndexUpdatePlan PrepareUpdate(QaReportIndexEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        IndexSnapshot snapshot = ReadSnapshot();
        QaReportIndexEntry normalizedEntry = NormalizeEntryForWrite(entry);
        List<QaReportIndexEntry> updatedEntries = snapshot.Entries
            .Where(existing => existing.ReportKey != normalizedEntry.ReportKey)
            .Append(normalizedEntry)
            .OrderBy(existing => existing, QaReportIndexEntryComparer.Instance)
            .ToList();

        byte[] updatedContent;

        try
        {
            updatedContent = Serialize(updatedEntries);
        }
        catch (QaReportIndexException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw CreateException(
                QaReportIndexErrorCategory.SerializationFailure,
                "The complete replacement index could not be encoded.",
                exception);
        }

        return new QaReportIndexUpdatePlan(
            snapshot.SourceContent,
            snapshot.SourceContentHash,
            updatedContent);
    }

    private IndexSnapshot ReadSnapshot()
    {
        byte[] sourceContent;

        try
        {
            sourceContent = File.ReadAllBytes(paths.QaReportIndexFilePath);
        }
        catch (QaReportIndexException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or DirectoryNotFoundException)
        {
            throw CreateException(
                QaReportIndexErrorCategory.Missing,
                "The initialized index file is missing.",
                exception);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            throw CreateException(
                QaReportIndexErrorCategory.ReadFailure,
                "The index file could not be read.",
                exception);
        }

        IReadOnlyList<QaReportIndexEntry> entries = Parse(sourceContent);
        byte[] sourceContentHash = SHA256.HashData(sourceContent);

        return new IndexSnapshot(sourceContent, sourceContentHash, entries);
    }

    private IReadOnlyList<QaReportIndexEntry> Parse(byte[] sourceContent)
    {
        if (sourceContent.Length == 0)
        {
            return Array.Empty<QaReportIndexEntry>();
        }

        if (HasUtf8Bom(sourceContent))
        {
            throw Malformed("A byte-order mark is not permitted.");
        }

        string text;

        try
        {
            text = Utf8NoBom.GetString(sourceContent);
        }
        catch (DecoderFallbackException exception)
        {
            throw Malformed("The index is not valid UTF-8.", exception);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw Malformed("A nonempty index cannot contain only whitespace.");
        }

        string normalizedNewlines = text.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (normalizedNewlines.Contains('\r'))
        {
            throw Malformed("The index contains an unsupported line ending.");
        }

        string[] lines = normalizedNewlines.Split('\n');

        if (lines.Length > 0 && lines[^1].Length == 0)
        {
            lines = lines[..^1];
        }

        if (lines.Length == 0 || lines.Length % EntryLineCount != 0)
        {
            throw Malformed("The index does not contain complete entry blocks.");
        }

        List<QaReportIndexEntry> entries = [];
        HashSet<QaReportKey> reportKeys = [];

        for (int lineIndex = 0;
             lineIndex < lines.Length;
             lineIndex += EntryLineCount)
        {
            int entryNumber = (lineIndex / EntryLineCount) + 1;
            QaReportIndexEntry entry = ParseEntry(
                lines.AsSpan(lineIndex, EntryLineCount),
                entryNumber);

            if (!reportKeys.Add(entry.ReportKey))
            {
                throw Malformed(
                    $"Entry {entryNumber} duplicates an existing logical report key.");
            }

            entries.Add(entry);
        }

        return entries.ToArray();
    }

    private QaReportIndexEntry ParseEntry(
        ReadOnlySpan<string> lines,
        int entryNumber)
    {
        if (!string.Equals(
                lines[EntryLineCount - 1],
                EntrySeparator,
                StringComparison.Ordinal))
        {
            throw Malformed($"Entry {entryNumber} has no valid block terminator.");
        }

        string reportKeyText = ParseField(lines[0], "ReportKey", entryNumber);
        string qaDateText = ParseField(lines[1], "QA Date", entryNumber);
        string hotelName = ParseField(lines[2], "Hotel", entryNumber);
        string hotelId = ParseField(lines[3], "Hotel ID", entryNumber);
        string pmsName = ParseField(lines[4], "PMS", entryNumber);
        string fileMonthText = ParseField(lines[5], "File Month", entryNumber);
        string statusText = ParseField(lines[6], "Status", entryNumber);
        string effectiveCreatedBy = ParseField(
            lines[7],
            "Created By",
            entryNumber);
        string filename = ParseField(lines[8], "Filename", entryNumber);
        string relativeHotelPath = ParseField(
            lines[9],
            "Hotel Copy",
            entryNumber);
        string relativePmsPath = ParseField(
            lines[10],
            "PMS Copy",
            entryNumber);
        string savedAtText = ParseField(
            lines[11],
            "Saved At UTC",
            entryNumber);

        if (!QaReportKey.TryParse(reportKeyText, out QaReportKey? reportKey))
        {
            throw Malformed($"Entry {entryNumber} has an invalid report key.");
        }

        if (!DateOnly.TryParseExact(
                qaDateText,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly qaDate))
        {
            throw Malformed($"Entry {entryNumber} has an invalid QA Date.");
        }

        if (!TryParseFileMonth(fileMonthText, out QaFileMonth? fileMonth))
        {
            throw Malformed($"Entry {entryNumber} has an invalid File Month.");
        }

        if (!TryParseStatus(statusText, out QaReportStatus status))
        {
            throw Malformed($"Entry {entryNumber} has an invalid report status.");
        }

        if (!TryParseUtcTimestamp(savedAtText, out DateTimeOffset savedAtUtc))
        {
            throw Malformed($"Entry {entryNumber} has an invalid UTC timestamp.");
        }

        if (!string.Equals(
                reportKey.HotelId,
                hotelId,
                StringComparison.OrdinalIgnoreCase)
            || reportKey.FileMonth != fileMonth)
        {
            throw Malformed(
                $"Entry {entryNumber} has inconsistent report-key fields.");
        }

        if (!IsSafeFilename(filename)
            || !IsLockedFilename(
                filename,
                qaDate,
                hotelName,
                hotelId,
                pmsName,
                fileMonth))
        {
            throw Malformed($"Entry {entryNumber} has an invalid filename.");
        }

        if (!IsSafeRelativeCopyPath(
                relativeHotelPath,
                "ByHotel",
                filename)
            || !IsSafeRelativeCopyPath(
                relativePmsPath,
                "ByPMS",
                filename))
        {
            throw Malformed($"Entry {entryNumber} has an invalid relative copy path.");
        }

        try
        {
            return new QaReportIndexEntry(
                reportKey,
                qaDate,
                hotelName,
                hotelId,
                pmsName,
                fileMonth,
                status,
                effectiveCreatedBy,
                filename,
                relativeHotelPath,
                relativePmsPath,
                savedAtUtc);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException)
        {
            throw Malformed(
                $"Entry {entryNumber} contains inconsistent required values.",
                exception);
        }
    }

    private string ParseField(
        string line,
        string label,
        int entryNumber)
    {
        string prefix = label + ": ";

        if (!line.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw Malformed(
                $"Entry {entryNumber} does not have the expected '{label}' field.");
        }

        string value = line[prefix.Length..];

        if (string.IsNullOrWhiteSpace(value)
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || ContainsForbiddenSingleLineCharacter(value))
        {
            throw Malformed(
                $"Entry {entryNumber} has an invalid '{label}' value.");
        }

        return value;
    }

    private QaReportIndexEntry NormalizeEntryForWrite(QaReportIndexEntry entry)
    {
        try
        {
            string hotelName = NormalizeRequiredSingleLine(entry.HotelName);
            string hotelId = NormalizeRequiredSingleLine(entry.HotelId);
            string pmsName = NormalizeRequiredSingleLine(entry.PmsName);
            string effectiveCreatedBy = NormalizeRequiredSingleLine(
                entry.EffectiveCreatedBy);
            string filename = NormalizeRequiredSingleLine(entry.Filename);
            string relativeHotelPath = NormalizeRelativePathForWrite(
                entry.RelativeHotelCopyPath);
            string relativePmsPath = NormalizeRelativePathForWrite(
                entry.RelativePmsCopyPath);
            string keyHotelId = NormalizeRequiredSingleLine(
                entry.ReportKey.HotelId);

            if (!string.Equals(
                    keyHotelId,
                    hotelId,
                    StringComparison.OrdinalIgnoreCase)
                || entry.ReportKey.FileMonth != entry.FileMonth)
            {
                throw new ArgumentException(
                    "The report key does not agree with the index entry.");
            }

            if (!IsSafeFilename(filename)
                || !IsLockedFilename(
                    filename,
                    entry.QaDate,
                    hotelName,
                    hotelId,
                    pmsName,
                    entry.FileMonth)
                || !IsSafeRelativeCopyPath(
                    relativeHotelPath,
                    "ByHotel",
                    filename)
                || !IsSafeRelativeCopyPath(
                    relativePmsPath,
                    "ByPMS",
                    filename))
            {
                throw new ArgumentException(
                    "The index entry has an unsafe filename or relative path.");
            }

            QaReportKey normalizedKey = new(hotelId, entry.FileMonth);

            return new QaReportIndexEntry(
                normalizedKey,
                entry.QaDate,
                hotelName,
                hotelId,
                pmsName,
                entry.FileMonth,
                entry.Status,
                effectiveCreatedBy,
                filename,
                relativeHotelPath,
                relativePmsPath,
                entry.SavedAtUtc);
        }
        catch (QaReportIndexException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw CreateException(
                QaReportIndexErrorCategory.SerializationFailure,
                "The replacement entry contains an invalid index value.",
                exception);
        }
    }

    private byte[] Serialize(IReadOnlyList<QaReportIndexEntry> entries)
    {
        StringBuilder builder = new();

        foreach (QaReportIndexEntry entry in entries)
        {
            AppendField(builder, "ReportKey", entry.ReportKey.ToString());
            AppendField(
                builder,
                "QA Date",
                entry.QaDate.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture));
            AppendField(builder, "Hotel", entry.HotelName);
            AppendField(builder, "Hotel ID", entry.HotelId);
            AppendField(builder, "PMS", entry.PmsName);
            AppendField(builder, "File Month", entry.FileMonth.ToString());
            AppendField(builder, "Status", FormatStatus(entry.Status));
            AppendField(builder, "Created By", entry.EffectiveCreatedBy);
            AppendField(builder, "Filename", entry.Filename);
            AppendField(
                builder,
                "Hotel Copy",
                entry.RelativeHotelCopyPath);
            AppendField(
                builder,
                "PMS Copy",
                entry.RelativePmsCopyPath);
            AppendField(
                builder,
                "Saved At UTC",
                entry.SavedAtUtc.UtcDateTime.ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
                    CultureInfo.InvariantCulture));
            builder.Append(EntrySeparator).Append("\r\n");
        }

        try
        {
            return Utf8NoBom.GetBytes(builder.ToString());
        }
        catch (EncoderFallbackException exception)
        {
            throw CreateException(
                QaReportIndexErrorCategory.SerializationFailure,
                "The replacement index contains text that is not valid Unicode.",
                exception);
        }
    }

    private static void AppendField(
        StringBuilder builder,
        string label,
        string value)
    {
        builder
            .Append(label)
            .Append(": ")
            .Append(value)
            .Append("\r\n");
    }

    private static string NormalizeRequiredSingleLine(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A nonblank index value is required.");
        }

        StringBuilder builder = new(value.Length);
        bool pendingWhitespaceReplacement = false;

        foreach (char character in value.Trim())
        {
            if (char.IsWhiteSpace(character)
                && (char.IsControl(character)
                    || character is '\u2028' or '\u2029'))
            {
                pendingWhitespaceReplacement = true;
                continue;
            }

            if (pendingWhitespaceReplacement && builder.Length > 0)
            {
                builder.Append(' ');
            }

            pendingWhitespaceReplacement = false;

            // The approved sanitizer maps non-whitespace controls to '_'.
            // Mirroring that representation keeps the readable index value
            // single-line while allowing its locked filename to be verified.
            builder.Append(char.IsControl(character) ? '_' : character);
        }

        string normalized = builder.ToString().Trim();

        if (normalized.Length == 0)
        {
            throw new ArgumentException(
                "The index value is empty after single-line normalization.");
        }

        return normalized;
    }

    private static string NormalizeRelativePathForWrite(string value)
    {
        return NormalizeRequiredSingleLine(value).Replace('\\', '/');
    }

    private static bool IsSafeFilename(string filename)
    {
        return FolderNameSanitizer.IsSafeGeneratedFileName(
                filename,
                MaximumFilenameLength)
            && filename.EndsWith(".pdf", StringComparison.Ordinal);
    }

    private static bool IsLockedFilename(
        string filename,
        DateOnly qaDate,
        string hotelName,
        string hotelId,
        string pmsName,
        QaFileMonth fileMonth)
    {
        try
        {
            QaReport report = new()
            {
                QaDate = qaDate,
                HotelInformation = new QaHotelInformation
                {
                    FileMonth = fileMonth
                }
            };
            QaHotelMetadata hotel = new()
            {
                HotelName = hotelName,
                HotelId = hotelId,
                PmsName = pmsName
            };
            QaPmsMetadata pms = new()
            {
                PmsName = pmsName
            };

            return string.Equals(
                FilenameService.CreateFilename(report, hotel, pms),
                filename,
                StringComparison.Ordinal);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or NotSupportedException)
        {
            return false;
        }
    }

    private static bool IsSafeRelativeCopyPath(
        string relativePath,
        string expectedRoot,
        string filename)
    {
        if (string.IsNullOrWhiteSpace(relativePath)
            || Path.IsPathRooted(relativePath)
            || relativePath.StartsWith('/')
            || relativePath.Contains('\\'))
        {
            return false;
        }

        string[] segments = relativePath.Split('/');

        return segments.Length == 3
            && string.Equals(
                segments[0],
                expectedRoot,
                StringComparison.Ordinal)
            && IsSafeRelativeSegment(segments[1])
            && string.Equals(
                segments[2],
                filename,
                StringComparison.Ordinal);
    }

    private static bool IsSafeRelativeSegment(string segment)
    {
        return FolderNameSanitizer.IsSafeStoredLeafName(segment);
    }

    private static bool ContainsForbiddenSingleLineCharacter(string value)
    {
        return value.Any(character =>
            char.IsControl(character)
            || character is '\u2028' or '\u2029');
    }

    private static bool TryParseFileMonth(
        string value,
        out QaFileMonth? fileMonth)
    {
        fileMonth = null;

        if (value.Length != 7
            || value[4] != '-'
            || !int.TryParse(
                value.AsSpan(0, 4),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int year)
            || !int.TryParse(
                value.AsSpan(5, 2),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int month)
            || year is < 1 or > 9999
            || month is < 1 or > 12)
        {
            return false;
        }

        fileMonth = new QaFileMonth(year, month);
        return true;
    }

    private static bool TryParseStatus(
        string value,
        out QaReportStatus status)
    {
        switch (value)
        {
            case "Pass":
                status = QaReportStatus.Pass;
                return true;
            case "Pass with Warnings":
                status = QaReportStatus.PassWithWarnings;
                return true;
            case "Fail":
                status = QaReportStatus.Fail;
                return true;
            default:
                status = default;
                return false;
        }
    }

    private static string FormatStatus(QaReportStatus status)
    {
        return status switch
        {
            QaReportStatus.Pass => "Pass",
            QaReportStatus.PassWithWarnings => "Pass with Warnings",
            QaReportStatus.Fail => "Fail",
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "The QA report status is invalid.")
        };
    }

    private static bool TryParseUtcTimestamp(
        string value,
        out DateTimeOffset savedAtUtc)
    {
        string[] formats =
        [
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"
        ];

        return DateTimeOffset.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out savedAtUtc);
    }

    private static bool HasUtf8Bom(ReadOnlySpan<byte> content)
    {
        return content.Length >= 3
            && content[0] == 0xEF
            && content[1] == 0xBB
            && content[2] == 0xBF;
    }

    private QaReportIndexException Malformed(
        string reason,
        Exception? innerException = null)
    {
        return CreateException(
            QaReportIndexErrorCategory.MalformedContent,
            reason,
            innerException);
    }

    private QaReportIndexException CreateException(
        QaReportIndexErrorCategory category,
        string reason,
        Exception? innerException = null)
    {
        return new QaReportIndexException(
            category,
            paths.QaReportIndexFilePath,
            reason,
            innerException);
    }

    private static bool IsFileSystemException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException;
    }

    private sealed record IndexSnapshot(
        byte[] SourceContent,
        byte[] SourceContentHash,
        IReadOnlyList<QaReportIndexEntry> Entries);

    private sealed class QaReportIndexEntryComparer
        : IComparer<QaReportIndexEntry>
    {
        public static QaReportIndexEntryComparer Instance { get; } = new();

        public int Compare(QaReportIndexEntry? left, QaReportIndexEntry? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            int comparison = StringComparer.OrdinalIgnoreCase.Compare(
                left.HotelId,
                right.HotelId);

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.FileMonth.Year.CompareTo(right.FileMonth.Year);

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.FileMonth.Month.CompareTo(right.FileMonth.Month);

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.QaDate.CompareTo(right.QaDate);

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.OrdinalIgnoreCase.Compare(
                left.Filename,
                right.Filename);

            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(left.Filename, right.Filename);
        }
    }

}

/// <summary>
/// Holds one complete prepared index replacement and exact source evidence.
/// </summary>
internal sealed class QaReportIndexUpdatePlan
{
    public QaReportIndexUpdatePlan(
        byte[] sourceContent,
        byte[] sourceContentHash,
        byte[] updatedContent)
    {
        ArgumentNullException.ThrowIfNull(sourceContent);
        ArgumentNullException.ThrowIfNull(sourceContentHash);
        ArgumentNullException.ThrowIfNull(updatedContent);

        SourceContent = sourceContent.ToArray();
        SourceContentHash = sourceContentHash.ToArray();
        UpdatedContent = updatedContent.ToArray();
    }

    public byte[] SourceContent { get; }

    public byte[] SourceContentHash { get; }

    public byte[] UpdatedContent { get; }
}
