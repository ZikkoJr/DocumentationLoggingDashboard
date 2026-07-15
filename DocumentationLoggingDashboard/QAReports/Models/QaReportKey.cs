using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Identifies one logical QA report by trimmed Hotel ID and File Month.
/// </summary>
public sealed class QaReportKey : IEquatable<QaReportKey>
{
    private static readonly StringComparer HotelIdComparer =
        StringComparer.OrdinalIgnoreCase;

    public QaReportKey(string hotelId, QaFileMonth fileMonth)
    {
        if (string.IsNullOrWhiteSpace(hotelId))
        {
            throw new ArgumentException(
                "A Hotel ID is required for the QA report key.",
                nameof(hotelId));
        }

        ArgumentNullException.ThrowIfNull(fileMonth);

        if (fileMonth.Year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fileMonth),
                "The QA report key requires a valid four-digit File Month year.");
        }

        HotelId = hotelId.Trim();
        FileMonth = fileMonth;
    }

    public string HotelId { get; }

    public QaFileMonth FileMonth { get; }

    public static QaReportKey Parse(string value)
    {
        if (!TryParse(value, out QaReportKey? reportKey))
        {
            throw new FormatException(
                "A QA report key must contain a Hotel ID and File Month in the form HotelID|yyyy-MM.");
        }

        return reportKey;
    }

    public static bool TryParse(
        string? value,
        [NotNullWhen(true)] out QaReportKey? reportKey)
    {
        reportKey = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalizedValue = value.Trim();
        int separatorIndex = normalizedValue.LastIndexOf('|');

        if (separatorIndex <= 0 || separatorIndex == normalizedValue.Length - 1)
        {
            return false;
        }

        string hotelId = normalizedValue[..separatorIndex].Trim();
        ReadOnlySpan<char> fileMonthText =
            normalizedValue.AsSpan(separatorIndex + 1);

        if (hotelId.Length == 0
            || fileMonthText.Length != 7
            || fileMonthText[4] != '-'
            || !int.TryParse(
                fileMonthText[..4],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int year)
            || !int.TryParse(
                fileMonthText[5..],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int month)
            || year is < 1 or > 9999
            || month is < 1 or > 12)
        {
            return false;
        }

        reportKey = new QaReportKey(hotelId, new QaFileMonth(year, month));
        return true;
    }

    public bool Equals(QaReportKey? other)
    {
        return other is not null
            && HotelIdComparer.Equals(HotelId, other.HotelId)
            && FileMonth.Year == other.FileMonth.Year
            && FileMonth.Month == other.FileMonth.Month;
    }

    public override bool Equals(object? obj)
    {
        return obj is QaReportKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            HotelIdComparer.GetHashCode(HotelId),
            FileMonth.Year,
            FileMonth.Month);
    }

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{HotelId}|{FileMonth}");
    }

    public static bool operator ==(QaReportKey? left, QaReportKey? right)
    {
        return EqualityComparer<QaReportKey>.Default.Equals(left, right);
    }

    public static bool operator !=(QaReportKey? left, QaReportKey? right)
    {
        return !(left == right);
    }
}
