using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Definitions;

/// <summary>
/// Provides the exact, ordered Quick QA checklist contract.
/// </summary>
public static class QuickQaChecklistCatalog
{
    public const int ExpectedDefinitionCount = 21;

    private static readonly IReadOnlyList<QuickQaCheckDefinition> AllDefinitions =
        Array.AsReadOnly(new[]
        {
            Definition(
                QuickQaChecklistIds.Raw.NamesAvailable,
                "Names are available to be pulled correctly",
                QaChecklistSection.RawFile),
            Definition(
                QuickQaChecklistIds.Raw.ConfirmationNumberAvailable,
                "Confirmation Number is available",
                QaChecklistSection.RawFile),
            Definition(
                QuickQaChecklistIds.Raw.ReservationDateAvailable,
                "Reservation/Booking Date is available",
                QaChecklistSection.RawFile),
            Definition(
                QuickQaChecklistIds.Raw.ArrivalDateAvailable,
                "Arrival Date is available",
                QaChecklistSection.RawFile),
            Definition(
                QuickQaChecklistIds.Raw.DepartureDateAvailable,
                "Departure Date is available",
                QaChecklistSection.RawFile),
            Definition(
                QuickQaChecklistIds.Raw.MonetaryAvailable,
                "A usable monetary column is available",
                QaChecklistSection.RawFile),
            Definition(
                QuickQaChecklistIds.Raw.FileFormatConsistent,
                "File formatting is consistent throughout",
                QaChecklistSection.RawFile),
            Definition(
                QuickQaChecklistIds.Raw.DateFormatValid,
                "Date values use valid and interpretable formats",
                QaChecklistSection.RawFile),
            Definition(
                QuickQaChecklistIds.Raw.DateSequenceValid,
                "Reservation/Booking, Arrival, and Departure dates are sequenced correctly",
                QaChecklistSection.RawFile),
            Definition(
                QuickQaChecklistIds.Raw.CurrencyConsistent,
                "Currency values are consistent when applicable",
                QaChecklistSection.RawFile,
                allowsNotApplicable: true),
            Definition(
                QuickQaChecklistIds.Raw.SourceColumnAvailable,
                "Source column available",
                QaChecklistSection.RawFile,
                isStrategyAvailability: true),
            Definition(
                QuickQaChecklistIds.Raw.RateColumnAvailable,
                "Rate column available",
                QaChecklistSection.RawFile,
                isStrategyAvailability: true),
            Definition(
                QuickQaChecklistIds.Raw.MarketColumnAvailable,
                "Market column available",
                QaChecklistSection.RawFile,
                isStrategyAvailability: true),
            Definition(
                QuickQaChecklistIds.Raw.ConfirmationCandidatesReviewed,
                "Potential Confirmation Number fields were reviewed and the correct source identified",
                QaChecklistSection.RawFile,
                allowsNotApplicable: true),
            Definition(
                QuickQaChecklistIds.Database.NamesPulledCorrectly,
                "Names are being pulled correctly",
                QaChecklistSection.Database),
            Definition(
                QuickQaChecklistIds.Database.MonetaryPulledCorrectly,
                "Monetary values are being pulled/calculated correctly for the available monetary type",
                QaChecklistSection.Database),
            Definition(
                QuickQaChecklistIds.Database.DatesPulledCorrectly,
                "Dates are being pulled correctly",
                QaChecklistSection.Database),
            Definition(
                QuickQaChecklistIds.Database.ConfirmationNumberPulledCorrectly,
                "Confirmation Number is being pulled correctly",
                QaChecklistSection.Database),
            Definition(
                QuickQaChecklistIds.Database.RequiredValuesPresent,
                "Required database values are present",
                QaChecklistSection.Database),
            Definition(
                QuickQaChecklistIds.Database.RejectedRecordsAccountedFor,
                "Rejected records are accounted for when applicable",
                QaChecklistSection.Database,
                allowsNotApplicable: true),
            Definition(
                QuickQaChecklistIds.Database.EmailPulledCorrectly,
                "Email is being pulled correctly when an email source exists",
                QaChecklistSection.Database,
                allowsNotApplicable: true)
        });

    private static readonly IReadOnlyDictionary<string, QuickQaCheckDefinition>
        AllDefinitionsById = BuildAndValidateIndex();

    public static IReadOnlyList<QuickQaCheckDefinition> Definitions =>
        AllDefinitions;

    public static IReadOnlyDictionary<string, QuickQaCheckDefinition>
        DefinitionsById => AllDefinitionsById;

    private static QuickQaCheckDefinition Definition(
        string id,
        string displayName,
        QaChecklistSection section,
        bool allowsNotApplicable = false,
        bool isStrategyAvailability = false)
    {
        return new QuickQaCheckDefinition
        {
            Id = id,
            DisplayName = displayName,
            Section = section,
            AllowsNotApplicable = allowsNotApplicable,
            IsStrategyAvailability = isStrategyAvailability
        };
    }

    private static IReadOnlyDictionary<string, QuickQaCheckDefinition>
        BuildAndValidateIndex()
    {
        if (AllDefinitions.Count != ExpectedDefinitionCount)
        {
            throw new InvalidOperationException(
                $"The Quick QA checklist must contain exactly {ExpectedDefinitionCount} definitions.");
        }

        Dictionary<string, QuickQaCheckDefinition> definitionsById =
            new(StringComparer.Ordinal);

        foreach (QuickQaCheckDefinition definition in AllDefinitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id)
                || string.IsNullOrWhiteSpace(definition.DisplayName)
                || !definitionsById.TryAdd(definition.Id, definition))
            {
                throw new InvalidOperationException(
                    "Every Quick QA definition must have one unique, nonblank stable ID and display name.");
            }
        }

        string[] notApplicableIds = AllDefinitions
            .Where(definition => definition.AllowsNotApplicable)
            .Select(definition => definition.Id)
            .ToArray();
        string[] expectedNotApplicableIds =
        [
            QuickQaChecklistIds.Raw.CurrencyConsistent,
            QuickQaChecklistIds.Raw.ConfirmationCandidatesReviewed,
            QuickQaChecklistIds.Database.RejectedRecordsAccountedFor,
            QuickQaChecklistIds.Database.EmailPulledCorrectly
        ];

        if (!notApplicableIds.SequenceEqual(
                expectedNotApplicableIds,
                StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "The Quick QA catalog must allow Not Applicable on exactly the four approved checks.");
        }

        string[] strategyIds = AllDefinitions
            .Where(definition => definition.IsStrategyAvailability)
            .Select(definition => definition.Id)
            .ToArray();
        string[] expectedStrategyIds =
        [
            QuickQaChecklistIds.Raw.SourceColumnAvailable,
            QuickQaChecklistIds.Raw.RateColumnAvailable,
            QuickQaChecklistIds.Raw.MarketColumnAvailable
        ];

        if (!strategyIds.SequenceEqual(expectedStrategyIds, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "The Quick QA catalog must contain exactly the three approved Strategy availability checks.");
        }

        return new System.Collections.ObjectModel.ReadOnlyDictionary<
            string,
            QuickQaCheckDefinition>(definitionsById);
    }
}
