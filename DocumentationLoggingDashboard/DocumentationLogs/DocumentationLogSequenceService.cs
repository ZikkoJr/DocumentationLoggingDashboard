using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.Services;

namespace DocumentationLoggingDashboard.DocumentationLogs;

public sealed record DocumentationLogSequencePlan(
    string LogId,
    int Sequence,
    byte[] UpdatedStateContent);

/// <summary>
/// Reconciles the private sequence state with applicable legacy TXT and index
/// history. Preparation is side-effect free; the returned JSON is committed by
/// DocumentationLogSaveService with the other event artifacts.
/// </summary>
public sealed class DocumentationLogSequenceService
{
    public const string SchemaName =
        "DocumentationLoggingDashboard.DocumentationLogSequences";
    public const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly DocumentationLogStoragePaths paths;
    private readonly LogTemplateService templateService;

    public DocumentationLogSequenceService(
        DocumentationLogStoragePaths paths,
        LogTemplateService? templateService = null)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.templateService = templateService ?? new LogTemplateService();
    }

    public string PreviewNextLogId(LogType logType, DateTimeOffset timestamp)
    {
        string statePath = paths.SequenceStateFilePath;
        string indexPath = paths.LogIndexFilePath;
        string legacyPath = paths.GetLegacyDailyLogFilePath(
            logType,
            timestamp.DateTime);
        bool stateExists = File.Exists(statePath);
        byte[] state = stateExists ? File.ReadAllBytes(statePath) : [];
        byte[] index = File.Exists(indexPath) ? File.ReadAllBytes(indexPath) : [];
        byte[] legacy = File.Exists(legacyPath) ? File.ReadAllBytes(legacyPath) : [];

        return PrepareNext(
            logType,
            timestamp,
            stateExists,
            state,
            index,
            legacy).LogId;
    }

    public DocumentationLogSequencePlan PrepareNext(
        LogType logType,
        DateTimeOffset timestamp,
        bool stateExists,
        ReadOnlyMemory<byte> stateContent,
        ReadOnlyMemory<byte> indexContent,
        ReadOnlyMemory<byte> legacyTxtContent)
    {
        SequenceDocument document = ParseState(stateExists, stateContent);
        string prefix = templateService.GetLogIdPrefix(logType);
        string datePart = timestamp.DateTime.ToString(
            "yyyyMMdd",
            CultureInfo.InvariantCulture);
        string key = CreateKey(logType, datePart);
        int stateMaximum = document.Sequences.TryGetValue(key, out int value)
            ? value
            : 0;
        int observedMaximum = Math.Max(
            stateMaximum,
            Math.Max(
                FindMaximum(prefix, datePart, legacyTxtContent.Span),
                FindMaximum(prefix, datePart, indexContent.Span)));

        if (observedMaximum == int.MaxValue)
        {
            throw InvalidState("The documentation-log sequence has reached its supported limit.");
        }

        int next = observedMaximum + 1;
        document.Sequences[key] = next;
        byte[] updated = JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions);
        return new DocumentationLogSequencePlan(
            $"{prefix}-{datePart}-{next:000}",
            next,
            updated);
    }

    private static SequenceDocument ParseState(
        bool stateExists,
        ReadOnlyMemory<byte> content)
    {
        if (!stateExists)
        {
            return CreateEmptyDocument();
        }

        if (content.IsEmpty || content.Length > 1024 * 1024)
        {
            throw InvalidState(
                "The documentation-log sequence state is empty or exceeds the supported size.");
        }

        try
        {
            using JsonDocument json = JsonDocument.Parse(
                content,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 8
                });
            if (json.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw InvalidState(
                    "The documentation-log sequence state has an unsupported or invalid schema.");
            }

            string? schemaName = null;
            int? schemaVersion = null;
            Dictionary<string, int>? sequences = null;
            HashSet<string> rootProperties = new(
                StringComparer.OrdinalIgnoreCase);

            foreach (JsonProperty property in json.RootElement.EnumerateObject())
            {
                if (!rootProperties.Add(property.Name))
                {
                    throw InvalidState(
                        $"The documentation-log sequence state contains duplicate '{property.Name}' properties.");
                }

                if (property.Name.Equals(
                        "schemaName",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind != JsonValueKind.String)
                    {
                        throw InvalidState("Sequence schemaName must be a string.");
                    }

                    schemaName = property.Value.GetString();
                }
                else if (property.Name.Equals(
                             "schemaVersion",
                             StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind != JsonValueKind.Number
                        || !property.Value.TryGetInt32(out int parsedVersion))
                    {
                        throw InvalidState("Sequence schemaVersion must be an integer.");
                    }

                    schemaVersion = parsedVersion;
                }
                else if (property.Name.Equals(
                             "sequences",
                             StringComparison.OrdinalIgnoreCase))
                {
                    sequences = ParseSequences(property.Value);
                }
                else
                {
                    throw InvalidState(
                        $"The documentation-log sequence property '{property.Name}' is not supported.");
                }
            }

            if (!string.Equals(schemaName, SchemaName, StringComparison.Ordinal)
                || schemaVersion != SchemaVersion
                || sequences is null)
            {
                throw InvalidState(
                    "The documentation-log sequence state has an unsupported or invalid schema.");
            }

            return new SequenceDocument
            {
                SchemaName = SchemaName,
                SchemaVersion = SchemaVersion,
                Sequences = sequences
            };
        }
        catch (DocumentationLogSaveException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new DocumentationLogSaveException(
                DocumentationLogSaveErrorCategory.SequenceStateInvalid,
                DocumentationLogSaveStage.SequencePreparation,
                "The documentation-log sequence state is corrupt. It was not changed.",
                exception);
        }
    }

    private static Dictionary<string, int> ParseSequences(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw InvalidState("Sequence values must be stored in a JSON object.");
        }

        Dictionary<string, int> result = new(StringComparer.Ordinal);
        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!keys.Add(property.Name))
            {
                throw InvalidState(
                    $"The documentation-log sequence key '{property.Name}' is duplicated.");
            }

            if (!IsValidSequenceKey(property.Name)
                || property.Value.ValueKind != JsonValueKind.Number
                || !property.Value.TryGetInt32(out int value)
                || value < 0)
            {
                throw InvalidState(
                    $"The documentation-log sequence key '{property.Name}' or its value is invalid.");
            }

            result.Add(property.Name, value);
        }

        return result;
    }

    private static bool IsValidSequenceKey(string key)
    {
        string[] parts = key.Split('|', StringSplitOptions.None);
        bool supportedType = parts.Length == 2
            && Enum.TryParse(parts[0], ignoreCase: false, out LogType logType)
            && logType is LogType.DebuggingLog
                or LogType.ScriptEditingLog
                or LogType.ScriptCreationLog;
        return supportedType
            && DateTime.TryParseExact(
                parts[1],
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _);
    }

    private static int FindMaximum(
        string prefix,
        string datePart,
        ReadOnlySpan<byte> utf8Content)
    {
        if (utf8Content.IsEmpty)
        {
            return 0;
        }

        string text = Encoding.UTF8.GetString(utf8Content);
        string pattern = $@"\b{Regex.Escape(prefix)}-{datePart}-(\d{{3,}})\b";
        int maximum = 0;

        foreach (Match match in Regex.Matches(
                     text,
                     pattern,
                     RegexOptions.CultureInvariant))
        {
            if (int.TryParse(
                    match.Groups[1].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int sequence))
            {
                maximum = Math.Max(maximum, sequence);
            }
        }

        return maximum;
    }

    private static string CreateKey(LogType logType, string datePart)
    {
        return $"{logType}|{datePart}";
    }

    private static SequenceDocument CreateEmptyDocument()
    {
        return new SequenceDocument
        {
            SchemaName = SchemaName,
            SchemaVersion = SchemaVersion,
            Sequences = new Dictionary<string, int>(StringComparer.Ordinal)
        };
    }

    private static DocumentationLogSaveException InvalidState(string message)
    {
        return new DocumentationLogSaveException(
            DocumentationLogSaveErrorCategory.SequenceStateInvalid,
            DocumentationLogSaveStage.SequencePreparation,
            message);
    }

    private sealed class SequenceDocument
    {
        public string SchemaName { get; set; } = string.Empty;

        public int SchemaVersion { get; set; }

        public Dictionary<string, int> Sequences { get; set; } = new();
    }
}
