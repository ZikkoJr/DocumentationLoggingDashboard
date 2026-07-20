using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Parses only the deterministic, locked Phase 9 QA report filename formats.
/// </summary>
public sealed class QaReportFilenameParser
{
    private const string LockedSuffix = "_QAReport.pdf";
    private const string EncodedFormatMarker = "__";

    private readonly QaFolderNameSanitizer folderNameSanitizer;

    public QaReportFilenameParser(
        QaFolderNameSanitizer folderNameSanitizer)
    {
        this.folderNameSanitizer = folderNameSanitizer
            ?? throw new ArgumentNullException(nameof(folderNameSanitizer));
    }

    public bool TryParse(
        string? filename,
        [NotNullWhen(true)] out QaReportFilenameParts? parts)
    {
        parts = null;

        if (filename is null
            || !folderNameSanitizer.IsSafeGeneratedFileName(
                filename,
                QaReportFilenameService.MaximumFilenameLength)
            || filename.StartsWith(".qa-", StringComparison.OrdinalIgnoreCase)
            || !filename.EndsWith(
                LockedSuffix,
                StringComparison.OrdinalIgnoreCase)
            || filename.Length <= 11 + LockedSuffix.Length
            || filename[10] != '_'
            || !DateOnly.TryParseExact(
                filename.AsSpan(0, 10),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly qaDate))
        {
            return false;
        }

        int bodyLength = filename.Length - 11 - LockedSuffix.Length;
        string body = filename.Substring(11, bodyLength);
        bool usesEncodedFormat = body.StartsWith(
            EncodedFormatMarker,
            StringComparison.Ordinal);

        if (usesEncodedFormat)
        {
            body = body[EncodedFormatMarker.Length..];
        }

        if (!TryDecodeFields(body, usesEncodedFormat, out string[] fields)
            || fields.Length != 4
            || !TryParseFileMonth(fields[3], out QaFileMonth? fileMonth)
            || !AreCanonicalSanitizedComponents(fields[..3]))
        {
            return false;
        }

        bool componentsContainUnderscore = fields[..3].Any(
            component => component.Contains('_'));
        if (usesEncodedFormat != componentsContainUnderscore)
        {
            return false;
        }

        string canonicalBody = CreateComponentBody(
            fields[0],
            fields[1],
            fields[2],
            fileMonth,
            usesEncodedFormat);
        if (!string.Equals(body, canonicalBody, StringComparison.Ordinal))
        {
            return false;
        }

        parts = new QaReportFilenameParts(
            qaDate,
            fields[0],
            fields[1],
            fields[2],
            fileMonth);
        return true;
    }

    internal static bool RequiresEncodedFormat(
        QaReportFilenameComponents components)
    {
        return components.SanitizedHotelName.Contains('_')
            || components.SanitizedHotelId.Contains('_')
            || components.SanitizedPmsName.Contains('_');
    }

    internal static string CreateComponentBody(
        string sanitizedHotelName,
        string sanitizedHotelId,
        string sanitizedPmsName,
        QaFileMonth fileMonth,
        bool usesEncodedFormat)
    {
        string hotelName = usesEncodedFormat
            ? EncodeComponent(sanitizedHotelName)
            : sanitizedHotelName;
        string hotelId = usesEncodedFormat
            ? EncodeComponent(sanitizedHotelId)
            : sanitizedHotelId;
        string pmsName = usesEncodedFormat
            ? EncodeComponent(sanitizedPmsName)
            : sanitizedPmsName;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{hotelName}_{hotelId}_{pmsName}_{fileMonth}");
    }

    private static string EncodeComponent(string component)
    {
        return component.Replace("_", "__", StringComparison.Ordinal);
    }

    private static bool TryDecodeFields(
        string body,
        bool usesEncodedFormat,
        out string[] fields)
    {
        if (!usesEncodedFormat)
        {
            fields = body.Split('_');
            return fields.Length == 4
                && fields.All(field => field.Length != 0);
        }

        List<string> decodedFields = [];
        StringBuilder currentField = new();

        for (int index = 0; index < body.Length;)
        {
            if (body[index] != '_')
            {
                currentField.Append(body[index]);
                index++;
                continue;
            }

            int runStart = index;
            while (index < body.Length && body[index] == '_')
            {
                index++;
            }

            int runLength = index - runStart;
            switch (runLength)
            {
                case 1:
                    if (!FinishField(decodedFields, currentField))
                    {
                        fields = [];
                        return false;
                    }
                    break;

                case 2:
                    currentField.Append('_');
                    break;

                case 3:
                    if (!FinishField(decodedFields, currentField))
                    {
                        fields = [];
                        return false;
                    }
                    currentField.Append('_');
                    break;

                default:
                    fields = [];
                    return false;
            }
        }

        if (!FinishField(decodedFields, currentField))
        {
            fields = [];
            return false;
        }

        fields = decodedFields.ToArray();
        return true;
    }

    private static bool FinishField(
        ICollection<string> fields,
        StringBuilder currentField)
    {
        if (currentField.Length == 0)
        {
            return false;
        }

        fields.Add(currentField.ToString());
        currentField.Clear();
        return true;
    }

    private bool AreCanonicalSanitizedComponents(
        IReadOnlyList<string> components)
    {
        return components.Count == 3
            && components.All(component =>
                folderNameSanitizer.IsSafeStoredLeafName(component)
                && string.Equals(
                    folderNameSanitizer.SanitizeLeafName(component),
                    component,
                    StringComparison.Ordinal));
    }

    private static bool TryParseFileMonth(
        string text,
        [NotNullWhen(true)] out QaFileMonth? fileMonth)
    {
        fileMonth = null;

        if (text.Length != 7
            || text[4] != '-'
            || !int.TryParse(
                text.AsSpan(0, 4),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int year)
            || !int.TryParse(
                text.AsSpan(5, 2),
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
}
