using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.DocumentationLogs;

/// <summary>
/// Validates business input and resolves one fresh canonical Hotel/PMS routing
/// snapshot for a preview or save attempt.
/// </summary>
public sealed class DocumentationLogRoutingService
{
    private readonly QaMetadataService metadataService;
    private readonly HotelIdListParser hotelIdListParser;

    public DocumentationLogRoutingService(
        QaMetadataService metadataService,
        HotelIdListParser? hotelIdListParser = null)
    {
        this.metadataService = metadataService
            ?? throw new ArgumentNullException(nameof(metadataService));
        this.hotelIdListParser = hotelIdListParser ?? new HotelIdListParser();
    }

    public DocumentationLogResolvedDraft ResolveDraft(
        DocumentationLogSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            IReadOnlyList<QaHotelMetadata> hotels = metadataService.LoadHotels();
            IReadOnlyList<QaPmsMetadata> pmsSystems = metadataService.LoadPmsSystems();
            IReadOnlyList<DocumentationLogHotel> resolvedHotels = request.LogType switch
            {
                LogType.DebuggingLog => ResolveDebuggingHotel(
                    request,
                    hotels,
                    pmsSystems),
                LogType.ScriptEditingLog or LogType.ScriptCreationLog =>
                    ResolveManualHotels(request.HotelIdsInput, hotels, pmsSystems),
                _ => throw Validation("The selected documentation log type is not supported.")
            };
            IReadOnlyDictionary<string, string> values = ValidateAndNormalizeFields(request);
            IReadOnlyList<DocumentationLogPms> uniquePms = resolvedHotels
                .Select(hotel => hotel.Pms)
                .DistinctBy(
                    pms => $"{pms.PmsName}\u001f{pms.FolderName}",
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new DocumentationLogResolvedDraft(
                request.LogType,
                resolvedHotels,
                uniquePms,
                values);
        }
        catch (DocumentationLogSaveException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is QaMetadataException
                or QaUnsupportedMetadataSchemaException
                or ArgumentException
                or InvalidOperationException
                or IOException
                or UnauthorizedAccessException)
        {
            throw new DocumentationLogSaveException(
                DocumentationLogSaveErrorCategory.MetadataMismatch,
                DocumentationLogSaveStage.MetadataResolution,
                "Current QA Hotel/PMS metadata could not authorize this documentation log save.",
                exception);
        }
    }

    private static IReadOnlyList<DocumentationLogHotel> ResolveDebuggingHotel(
        DocumentationLogSaveRequest request,
        IReadOnlyList<QaHotelMetadata> hotels,
        IReadOnlyList<QaPmsMetadata> pmsSystems)
    {
        QaHotelMetadata selected = request.SelectedHotel
            ?? throw Validation("Select one Hotel from current QA metadata before saving the Debugging Log.");
        QaPmsMetadata selectedPms = request.SelectedPms
            ?? throw Validation("Refresh and reselect the Hotel so its canonical PMS routing can be confirmed.");
        string selectedHotelId = Require(selected.HotelId, "Hotel ID");
        QaHotelMetadata[] matches = hotels
            .Where(hotel => string.Equals(
                hotel.HotelId?.Trim(),
                selectedHotelId,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length != 1)
        {
            throw MetadataMismatch(
                "The selected Hotel no longer identifies one canonical metadata record. Refresh and reselect it before saving.");
        }

        QaHotelMetadata canonicalHotel = matches[0];
        QaPmsMetadata canonicalPms = ResolvePms(canonicalHotel, pmsSystems);
        bool hotelSnapshotMatches = Exact(selected.HotelId, canonicalHotel.HotelId)
            && Exact(selected.HotelName, canonicalHotel.HotelName)
            && Exact(selected.PmsName, canonicalHotel.PmsName)
            && Exact(selected.FolderName, canonicalHotel.FolderName);
        bool pmsSnapshotMatches = Exact(selectedPms.PmsName, canonicalPms.PmsName)
            && Exact(selectedPms.FolderName, canonicalPms.FolderName);

        if (!hotelSnapshotMatches || !pmsSnapshotMatches)
        {
            throw MetadataMismatch(
                "The selected Hotel or its canonical PMS routing changed. Refresh and reselect the Hotel before saving.");
        }

        return [CreateResolvedHotel(canonicalHotel, canonicalPms)];
    }

    private IReadOnlyList<DocumentationLogHotel> ResolveManualHotels(
        string input,
        IReadOnlyList<QaHotelMetadata> hotels,
        IReadOnlyList<QaPmsMetadata> pmsSystems)
    {
        IReadOnlyList<string> ids = hotelIdListParser.Parse(input);
        if (ids.Count == 0)
        {
            throw Validation("Enter at least one Hotel ID before saving.");
        }

        List<string> unknownIds = [];
        List<(QaHotelMetadata Hotel, QaPmsMetadata Pms)> matches = [];

        foreach (string id in ids)
        {
            QaHotelMetadata[] hotelMatches = hotels
                .Where(hotel => string.Equals(
                    hotel.HotelId?.Trim(),
                    id,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (hotelMatches.Length == 0)
            {
                unknownIds.Add(id);
                continue;
            }

            if (hotelMatches.Length != 1)
            {
                throw MetadataMismatch(
                    $"Hotel ID {id} no longer identifies one canonical metadata record.");
            }

            matches.Add((hotelMatches[0], ResolvePms(hotelMatches[0], pmsSystems)));
        }

        if (unknownIds.Count == 1)
        {
            throw Validation(
                $"Hotel ID {unknownIds[0]} could not be found. Add the Hotel to QA metadata or correct the ID before saving.");
        }

        if (unknownIds.Count > 1)
        {
            throw Validation(
                $"Hotel IDs {string.Join(", ", unknownIds)} could not be found. Saving is blocked until every ID exists in QA metadata or is corrected.");
        }

        return matches
            .Select(match => CreateResolvedHotel(match.Hotel, match.Pms))
            .ToArray();
    }

    private static QaPmsMetadata ResolvePms(
        QaHotelMetadata hotel,
        IReadOnlyList<QaPmsMetadata> pmsSystems)
    {
        string pmsName = Require(hotel.PmsName, "canonical PMS");
        QaPmsMetadata[] matches = pmsSystems
            .Where(pms => string.Equals(
                pms.PmsName?.Trim(),
                pmsName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length != 1)
        {
            throw MetadataMismatch(
                $"Hotel ID {hotel.HotelId} no longer resolves to one canonical PMS metadata record.");
        }

        return matches[0];
    }

    private static DocumentationLogHotel CreateResolvedHotel(
        QaHotelMetadata hotel,
        QaPmsMetadata pms)
    {
        return new DocumentationLogHotel(
            Require(hotel.HotelId, "canonical Hotel ID"),
            Require(hotel.HotelName, "canonical Hotel Name"),
            new DocumentationLogPms(
                Require(pms.PmsName, "canonical PMS Name"),
                Require(pms.FolderName, "canonical PMS folder")),
            Require(hotel.FolderName, "canonical Hotel folder"));
    }

    private static IReadOnlyDictionary<string, string> ValidateAndNormalizeFields(
        DocumentationLogSaveRequest request)
    {
        Dictionary<string, string> result = new(StringComparer.Ordinal);

        switch (request.LogType)
        {
            case LogType.DebuggingLog:
                AddRequired(result, request, DocumentationLogFieldKeys.ErrorShownOnTicket, "Error Shown On Ticket");
                AddRequired(result, request, DocumentationLogFieldKeys.RootCause, "Root Cause");
                AddRequired(result, request, DocumentationLogFieldKeys.FixApplied, "Fix Applied");
                break;
            case LogType.ScriptEditingLog:
                AddRequired(result, request, DocumentationLogFieldKeys.ScriptName, "Script Name");
                AddRequired(result, request, DocumentationLogFieldKeys.ReasonForEdit, "Reason For Edit");
                AddRequired(result, request, DocumentationLogFieldKeys.ChangesMade, "Changes Made");
                break;
            case LogType.ScriptCreationLog:
                AddRequired(result, request, DocumentationLogFieldKeys.ScriptName, "Script Name");
                AddRequired(result, request, DocumentationLogFieldKeys.ReasonForCreation, "Reason For Creation");
                AddRequired(result, request, DocumentationLogFieldKeys.ScriptPurpose, "Script Purpose / What It Does");
                break;
            default:
                throw Validation("The selected documentation log type is not supported.");
        }

        result[DocumentationLogFieldKeys.CreatedBy] = GetOptional(
            request,
            DocumentationLogFieldKeys.CreatedBy);
        result[DocumentationLogFieldKeys.NotesFollowUp] = GetOptional(
            request,
            DocumentationLogFieldKeys.NotesFollowUp);
        return result;
    }

    private static void AddRequired(
        IDictionary<string, string> destination,
        DocumentationLogSaveRequest request,
        string key,
        string label)
    {
        string value = Get(request, key).Trim();
        if (value.Length == 0)
        {
            throw Validation($"{label} is required before saving.");
        }

        destination[key] = value;
    }

    private static string GetOptional(
        DocumentationLogSaveRequest request,
        string key)
    {
        string value = Get(request, key).Trim();
        return value.Length == 0 ? "N/A" : value;
    }

    private static string Get(DocumentationLogSaveRequest request, string key)
    {
        return request.FieldValues.TryGetValue(key, out string? value)
            ? value ?? string.Empty
            : string.Empty;
    }

    private static bool Exact(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.Ordinal);
    }

    private static string Require(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw MetadataMismatch($"{fieldName} is missing from current canonical metadata.");
        }

        return value.Trim();
    }

    private static DocumentationLogSaveException Validation(string message)
    {
        return new DocumentationLogSaveException(
            DocumentationLogSaveErrorCategory.Validation,
            DocumentationLogSaveStage.Validation,
            message);
    }

    private static DocumentationLogSaveException MetadataMismatch(string message)
    {
        return new DocumentationLogSaveException(
            DocumentationLogSaveErrorCategory.MetadataMismatch,
            DocumentationLogSaveStage.MetadataResolution,
            message);
    }
}
