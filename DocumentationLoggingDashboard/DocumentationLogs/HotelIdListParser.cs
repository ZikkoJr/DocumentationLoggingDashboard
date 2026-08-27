namespace DocumentationLoggingDashboard.DocumentationLogs;

/// <summary>
/// Parses the manual Editing/Creation Hotel ID field without ever treating an
/// identifier as a number.
/// </summary>
public sealed class HotelIdListParser
{
    private static readonly char[] Separators = [',', ';', '\r', '\n'];

    public IReadOnlyList<string> Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<string>();
        }

        List<string> result = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (string token in input.Split(
                     Separators,
                     StringSplitOptions.RemoveEmptyEntries))
        {
            string value = token.Trim();
            if (value.Length > 0 && seen.Add(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    public string FormatCanonical(IEnumerable<string> hotelIds)
    {
        ArgumentNullException.ThrowIfNull(hotelIds);
        return string.Join("; ", hotelIds);
    }
}
