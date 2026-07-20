using System.Text.Json;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

internal static class QaMetadataJson
{
    internal static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    internal static byte[] SerializeToUtf8Bytes<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.SerializeToUtf8Bytes(value, Options);
    }

    internal static T? Deserialize<T>(ReadOnlySpan<byte> utf8Json)
    {
        return JsonSerializer.Deserialize<T>(utf8Json, Options);
    }

    internal static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    internal static QaHotelMetadataDocument CreateEmptyHotelsDocument()
    {
        return new QaHotelMetadataDocument
        {
            SchemaVersion = QaHotelMetadataDocument.CurrentSchemaVersion,
            Hotels = []
        };
    }

    internal static QaPmsMetadataDocument CreateEmptyPmsSystemsDocument()
    {
        return new QaPmsMetadataDocument
        {
            SchemaVersion = QaPmsMetadataDocument.CurrentSchemaVersion,
            PmsSystems = []
        };
    }
}
