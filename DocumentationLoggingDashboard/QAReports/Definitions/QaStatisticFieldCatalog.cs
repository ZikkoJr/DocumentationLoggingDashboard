using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Definitions;

/// <summary>
/// Provides the deterministic statistics field catalog shared by the UI and services.
/// </summary>
public static class QaStatisticFieldCatalog
{
    private static readonly IReadOnlyList<QaStatisticFieldDefinition> AllDefinitions =
        Array.AsReadOnly<QaStatisticFieldDefinition>(
    [
        Definition(
            QaStatisticFieldIds.FirstName,
            "First Name",
            QaStatisticFieldApplicability.SeparateNameColumns,
            QaChecklistIds.Raw.FirstNameValuesValid),
        Definition(
            QaStatisticFieldIds.LastName,
            "Last Name",
            QaStatisticFieldApplicability.SeparateNameColumns,
            QaChecklistIds.Raw.LastNameValuesValid),
        Definition(
            QaStatisticFieldIds.FullName,
            "Full Name",
            QaStatisticFieldApplicability.FullNameColumn,
            QaChecklistIds.Raw.FullNameValuesValid),
        Definition(
            QaStatisticFieldIds.ConfirmationNumber,
            "Confirmation Number",
            QaStatisticFieldApplicability.Always),
        Definition(
            QaStatisticFieldIds.Email,
            "Email",
            QaStatisticFieldApplicability.Always),
        Definition(
            QaStatisticFieldIds.ReservationDate,
            "Reservation Date",
            QaStatisticFieldApplicability.Always,
            QaChecklistIds.Raw.DateFormatValid),
        Definition(
            QaStatisticFieldIds.ArrivalDate,
            "Arrival Date",
            QaStatisticFieldApplicability.Always,
            QaChecklistIds.Raw.DateFormatValid),
        Definition(
            QaStatisticFieldIds.DepartureDate,
            "Departure Date",
            QaStatisticFieldApplicability.Always,
            QaChecklistIds.Raw.DateFormatValid),
        Definition(
            QaStatisticFieldIds.AverageRate,
            "Average Rate",
            QaStatisticFieldApplicability.Always),
        Definition(
            QaStatisticFieldIds.StayValue,
            "Stay Value",
            QaStatisticFieldApplicability.StayValueAvailable),
        Definition(
            QaStatisticFieldIds.SourceRateMarket,
            "Source / Rate / Market",
            QaStatisticFieldApplicability.Always)
    ]);

    private static readonly IReadOnlyDictionary<string, QaStatisticFieldDefinition>
        DefinitionsById = CreateIndex(AllDefinitions);

    public static IReadOnlyList<QaStatisticFieldDefinition> Definitions =>
        AllDefinitions;

    public static QaStatisticFieldDefinition GetRequired(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "A nonblank statistics field ID is required.",
                nameof(id));
        }

        return DefinitionsById.TryGetValue(id, out QaStatisticFieldDefinition? definition)
            ? definition
            : throw new ArgumentException(
                $"Statistics field ID '{id}' is not in the approved catalog.",
                nameof(id));
    }

    public static bool IsApplicable(
        QaStatisticFieldDefinition definition,
        QaFileCharacteristics characteristics)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(characteristics);

        return definition.Applicability switch
        {
            QaStatisticFieldApplicability.Always => true,
            QaStatisticFieldApplicability.SeparateNameColumns =>
                characteristics.NameColumnMode ==
                    QaNameColumnMode.SeparateFirstAndLastName,
            QaStatisticFieldApplicability.FullNameColumn =>
                characteristics.NameColumnMode == QaNameColumnMode.FullName,
            QaStatisticFieldApplicability.StayValueAvailable =>
                characteristics.MonetaryColumnScenario is
                    QaMonetaryColumnScenario.TwoMonetaryColumns or
                    QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns,
            _ => throw new ArgumentOutOfRangeException(
                nameof(definition),
                definition.Applicability,
                "Unknown statistics-field applicability value.")
        };
    }

    private static QaStatisticFieldDefinition Definition(
        string id,
        string displayName,
        QaStatisticFieldApplicability applicability,
        string? relatedChecklistId = null)
    {
        return new QaStatisticFieldDefinition(
            id,
            displayName,
            applicability,
            relatedChecklistId,
            SupportsBlankStatistics: true,
            SupportsBrokenDataStatistics: true);
    }

    private static IReadOnlyDictionary<string, QaStatisticFieldDefinition>
        CreateIndex(IEnumerable<QaStatisticFieldDefinition> definitions)
    {
        Dictionary<string, QaStatisticFieldDefinition> byId =
            new(StringComparer.Ordinal);

        foreach (QaStatisticFieldDefinition definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id)
                || string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                throw new InvalidOperationException(
                    "Every statistics field definition requires a stable ID and display name.");
            }

            if (!byId.TryAdd(definition.Id, definition))
            {
                throw new InvalidOperationException(
                    $"The statistics field catalog contains duplicate ID '{definition.Id}'.");
            }
        }

        return byId;
    }
}
