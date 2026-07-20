using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Forms;

/// <summary>
/// Provides reusable, side-effect-free filtering and display behavior for QA metadata.
/// </summary>
public static class QaMetadataSearch
{
    public static IReadOnlyList<QaPmsMetadata> FilterPmsSystems(
        IEnumerable<QaPmsMetadata> source,
        string? searchText)
    {
        ArgumentNullException.ThrowIfNull(source);

        string normalizedSearchText = NormalizeForComparison(searchText);

        if (normalizedSearchText.Length == 0)
        {
            return source.ToArray();
        }

        return source
            .Where(pms => NormalizeForComparison(pms.PmsName).Contains(
                normalizedSearchText,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public static IReadOnlyList<QaHotelMetadata> FilterHotels(
        IEnumerable<QaHotelMetadata> source,
        string? searchText)
    {
        ArgumentNullException.ThrowIfNull(source);

        string normalizedSearchText = NormalizeForComparison(searchText);

        if (normalizedSearchText.Length == 0)
        {
            return source.ToArray();
        }

        return source
            .Where(hotel =>
                NormalizeForComparison(hotel.HotelId).Contains(
                    normalizedSearchText,
                    StringComparison.OrdinalIgnoreCase)
                || NormalizeForComparison(hotel.HotelName).Contains(
                    normalizedSearchText,
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public static string GetHotelDisplayText(QaHotelMetadata hotel)
    {
        ArgumentNullException.ThrowIfNull(hotel);

        return $"{hotel.HotelId} \u2014 {hotel.HotelName}";
    }

    private static string NormalizeForComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
            ' ',
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
