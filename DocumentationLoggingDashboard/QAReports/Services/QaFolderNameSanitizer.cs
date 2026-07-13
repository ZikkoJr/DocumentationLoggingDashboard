using System.Text;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Generates deterministic Windows-safe directory leaf names for QA metadata.
/// </summary>
public sealed class QaFolderNameSanitizer
{
    /// <summary>
    /// Defines the maximum generated leaf-name length in UTF-16 code units.
    /// </summary>
    public const int MaximumLeafNameLength = 100;

    private const char ReplacementCharacter = '_';

    public string SanitizeLeafName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value that can produce a folder name is required.", nameof(value));
        }

        string trimmedValue = value.Trim();

        if (IsDotDirectoryName(trimmedValue))
        {
            throw new ArgumentException("The value cannot represent a dot directory.", nameof(value));
        }

        StringBuilder builder = new(trimmedValue.Length);

        foreach (char character in trimmedValue)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            char outputCharacter = IsInvalidWindowsFileNameCharacter(character)
                ? ReplacementCharacter
                : character;

            if (outputCharacter == ReplacementCharacter
                && builder.Length > 0
                && builder[^1] == ReplacementCharacter)
            {
                continue;
            }

            builder.Append(outputCharacter);
        }

        string safeName = TrimGeneratedEdges(builder.ToString());
        EnsureNonemptyLeafName(safeName, nameof(value));

        safeName = TruncateAndTrimGeneratedEnd(safeName);
        EnsureNonemptyLeafName(safeName, nameof(value));

        if (IsReservedWindowsDeviceName(safeName))
        {
            safeName = ReplacementCharacter + safeName;
        }

        safeName = TruncateAndTrimGeneratedEnd(safeName);
        EnsureNonemptyLeafName(safeName, nameof(value));

        return safeName;
    }

    public string CreateHotelFolderName(string hotelId, string hotelName)
    {
        string safeHotelId = SanitizeLeafName(hotelId);
        string safeHotelName = SanitizeLeafName(hotelName);
        string folderName = CollapseConsecutiveReplacementCharacters(
            $"{safeHotelId}_{safeHotelName}");

        folderName = TruncateAndTrimGeneratedEnd(folderName);
        EnsureNonemptyLeafName(folderName, nameof(hotelName));

        if (IsReservedWindowsDeviceName(folderName))
        {
            folderName = ReplacementCharacter + folderName;
            folderName = TruncateAndTrimGeneratedEnd(folderName);
        }

        return folderName.TrimEnd('.', ' ', ReplacementCharacter);
    }

    private static string CollapseConsecutiveReplacementCharacters(string value)
    {
        StringBuilder builder = new(value.Length);

        foreach (char character in value)
        {
            if (character == ReplacementCharacter
                && builder.Length > 0
                && builder[^1] == ReplacementCharacter)
            {
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    public bool IsSafeStoredLeafName(string? folderName)
    {
        if (string.IsNullOrEmpty(folderName)
            || folderName.Length > MaximumLeafNameLength
            || IsDotDirectoryName(folderName)
            || Path.IsPathRooted(folderName)
            || folderName.EndsWith('.')
            || folderName.EndsWith(' ')
            || folderName.EndsWith(ReplacementCharacter)
            || IsReservedWindowsDeviceName(folderName))
        {
            return false;
        }

        char previousCharacter = '\0';

        foreach (char character in folderName)
        {
            if (char.IsWhiteSpace(character)
                || IsInvalidWindowsFileNameCharacter(character)
                || (character == ReplacementCharacter && previousCharacter == ReplacementCharacter))
            {
                return false;
            }

            previousCharacter = character;
        }

        return true;
    }

    private static bool IsInvalidWindowsFileNameCharacter(char character)
    {
        return char.IsControl(character)
            || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*';
    }

    private static bool IsReservedWindowsDeviceName(string value)
    {
        int extensionSeparatorIndex = value.IndexOf('.', StringComparison.Ordinal);
        string baseName = extensionSeparatorIndex >= 0
            ? value[..extensionSeparatorIndex]
            : value;

        if (baseName.Equals("CON", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("PRN", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("AUX", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("NUL", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return baseName.Length == 4
            && (baseName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                || baseName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
            && baseName[3] is >= '1' and <= '9';
    }

    private static bool IsDotDirectoryName(string value)
    {
        return value.Equals(".", StringComparison.Ordinal)
            || value.Equals("..", StringComparison.Ordinal);
    }

    private static string TrimGeneratedEdges(string value)
    {
        int startIndex = 0;
        int endIndex = value.Length;

        while (startIndex < endIndex && value[startIndex] == ReplacementCharacter)
        {
            startIndex++;
        }

        while (endIndex > startIndex && value[endIndex - 1] is ReplacementCharacter or '.' or ' ')
        {
            endIndex--;
        }

        return value[startIndex..endIndex];
    }

    private static string TruncateWithoutSplittingSurrogatePair(string value)
    {
        if (value.Length <= MaximumLeafNameLength)
        {
            return value;
        }

        int length = MaximumLeafNameLength;

        if (char.IsHighSurrogate(value[length - 1]) && char.IsLowSurrogate(value[length]))
        {
            length--;
        }

        return value[..length];
    }

    private static string TruncateAndTrimGeneratedEnd(string value)
    {
        string remainingValue = value;

        while (remainingValue.Length > 0)
        {
            string truncatedValue = TruncateWithoutSplittingSurrogatePair(remainingValue);
            string safePrefix = truncatedValue.TrimEnd('.', ' ', ReplacementCharacter);

            if (safePrefix.Length > 0 || truncatedValue.Length == remainingValue.Length)
            {
                return safePrefix;
            }

            remainingValue = TrimGeneratedEdges(remainingValue[truncatedValue.Length..]);
        }

        return string.Empty;
    }

    private static void EnsureNonemptyLeafName(string value, string parameterName)
    {
        if (value.Length == 0 || IsDotDirectoryName(value))
        {
            throw new ArgumentException("The value does not produce a nonempty safe folder name.", parameterName);
        }
    }
}
