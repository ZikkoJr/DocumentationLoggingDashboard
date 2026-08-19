using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Definitions;

/// <summary>
/// Provides the deterministic Phase 2 QA checklist definition catalog.
/// </summary>
public static class QaChecklistCatalog
{
    public const int ExpectedDefinitionCount = 29;

    private static readonly IReadOnlyList<QaCheckDefinition> AllDefinitions =
    [
        Definition(
            QaChecklistIds.Raw.RequiredNameFieldPresent,
            "Required name field is present",
            "Passes when either separate First Name and Last Name fields exist or a Full Name field exists. Full Name can pass this field-presence check while later producing a warning.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.RequiredConfirmationPresent,
            "Confirmation Number field is present",
            "Confirms that the raw file includes a field that can be used as the Confirmation Number.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.EmailColumnAvailable,
            "Email column available",
            "Records whether the raw file includes an Email column. An unavailable Email column produces a Warning rather than a Failure.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.RequiredReservationDatePresent,
            "Reservation Date field is present",
            "Confirms that the raw file includes a Reservation Date field.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.RequiredArrivalDatePresent,
            "Arrival Date field is present",
            "Confirms that the raw file includes an Arrival Date field.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.RequiredDepartureDatePresent,
            "Departure Date field is present",
            "Confirms that the raw file includes a Departure Date field.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.RequiredMonetaryValuePresent,
            "Required monetary-value field is present",
            "Confirms that the raw file includes the required monetary-value field or fields used for Average Rate and Stay Value review.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.FileFormatConsistent,
            "File formatting is consistent throughout",
            "Confirms that row and column formatting is consistent enough for manual QA review of the source file.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.FirstNameValuesValid,
            "First Name values contain first names",
            "Confirms that populated First Name values contain first names. Email addresses or unrelated populated values are Broken Data; up to and including 50% may remain Pass with a Warning, while above 50% requires Fail. Blank cells are evaluated separately.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.SeparateNameColumns),
        Definition(
            QaChecklistIds.Raw.LastNameValuesValid,
            "Last Name values contain last names",
            "Confirms that populated Last Name values contain last names. Unrelated or misplaced populated values are Broken Data; up to and including 50% may remain Pass with a Warning, while above 50% requires Fail. Blank cells are evaluated separately.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.SeparateNameColumns),
        Definition(
            QaChecklistIds.Raw.FullNameValuesValid,
            "Full Name values contain valid full names",
            "Confirms that populated Full Name values contain usable full guest names when the file uses one name column. Malformed populated values are Broken Data; up to and including 50% may remain Pass with a Warning, while above 50% requires Fail. Blank cells are evaluated separately.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.FullNameColumn),
        Definition(
            QaChecklistIds.Raw.DateSequenceValid,
            "Reservation Date is less than or equal to Arrival Date, and Arrival Date is less than or equal to Departure Date",
            "Confirms that Reservation Date, Arrival Date, and Departure Date values appear in a valid chronological sequence.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.DateFormatValid,
            "Date values use valid and interpretable formats",
            "Confirms that populated date values can be interpreted consistently. Invalid populated dates are Broken Data; up to and including 50% may remain Pass with a Warning, while above 50% requires Fail. Blank cells are evaluated separately.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.CurrencyConsistent,
            "Currency values are consistent",
            "Confirms that Currency values are consistent when a Currency column exists; files without a Currency column will later be marked not applicable.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.CurrencyColumnPresent),
        Definition(
            QaChecklistIds.Raw.SingleMonetaryColumnIsAverageRate,
            "The single monetary-value column represents Average Rate",
            "Confirms that the single monetary-value column should be treated as Average Rate.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.OneMonetaryColumn),
        Definition(
            QaChecklistIds.Raw.TwoMonetaryColumnsIdentified,
            "Average Rate and Stay Value fields are correctly identified",
            "Confirms that the Average Rate and Stay Value fields are identified correctly when two monetary-value columns exist.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.TwoMonetaryColumns),
        Definition(
            QaChecklistIds.Raw.MonetarySpotCheckValid,
            "Three records were spot-checked and Stay Value equals Average Rate multiplied by nights stayed",
            "Confirms that a manual three-record spot check supports the relationship between Average Rate, nights stayed, and Stay Value.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.TwoMonetaryColumns),
        Definition(
            QaChecklistIds.Raw.AverageRateNotAboveStayValue,
            "Average Rate does not exceed Stay Value",
            "Confirms that Average Rate does not exceed Stay Value for records reviewed under the two-monetary-column scenario.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.TwoMonetaryColumns),
        Definition(
            QaChecklistIds.Raw.StrategySourceColumnAvailable,
            "Source column available",
            "Records whether a Source or Booking Source strategy column is available.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.StrategyRateColumnAvailable,
            "Rate column available",
            "Records whether a Rate Code, Rate Plan, or Rate Plan Code strategy column is available. Average Rate does not qualify.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.StrategyMarketColumnAvailable,
            "Market column available",
            "Records whether a Market or Market Segment strategy column is available.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.ConfirmationCandidatesReviewed,
            "Potential Confirmation Number fields were reviewed and the correct field was identified",
            "Confirms that potential Confirmation Number candidates were reviewed. Multiple candidates may later produce a warning while this checklist result can still pass after manual review.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Database.NamesMappedCorrectly,
            "Name fields were assigned correctly in the database",
            "Confirms that name fields were assigned correctly in the database, including verifying that First Name and Last Name were not reversed by the column predictor.",
            QaChecklistSection.Database,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Database.MonetaryMappedCorrectly,
            "Monetary fields and calculations were assigned correctly in the database",
            "Confirms correct monetary mapping for one monetary column mapped to Average Rate with Stay Value calculated correctly, two monetary columns mapped correctly, and more than two monetary columns using the correct Average Rate and Stay Value.",
            QaChecklistSection.Database,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Database.EmailMappedCorrectly,
            "Email field was assigned correctly in the database",
            "Confirms that the Email field was assigned correctly in the database.",
            QaChecklistSection.Database,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Database.DatesMappedCorrectly,
            "Reservation, Arrival, and Departure dates were assigned correctly in the database",
            "Confirms that Reservation, Arrival, and Departure dates were assigned correctly in the database while preserving known risks such as Reservation Date appearing after Check-in, invalid date formats, or more than three date-like columns.",
            QaChecklistSection.Database,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Database.ConfirmationMappedCorrectly,
            "Confirmation Number was assigned correctly in the database",
            "Confirms that Confirmation Number was assigned correctly in the database.",
            QaChecklistSection.Database,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Database.RequiredValuesPresent,
            "Required database values are present",
            "Confirms that required database values are present. Missing required DB values may later fail this check.",
            QaChecklistSection.Database,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Database.RejectedRecordsAccountedFor,
            "Rejected database records were reviewed and accounted for",
            "Confirms that rejected database records were reviewed and accounted for. An unexplained inconsistency will later fail, while an explained rejection may pass and produce a warning.",
            QaChecklistSection.Database,
            QaCheckApplicability.RejectedRecordsPresent)
    ];

    public static IReadOnlyList<QaCheckDefinition> Definitions => AllDefinitions;

    private static QaCheckDefinition Definition(
        string id,
        string displayName,
        string description,
        QaChecklistSection section,
        QaCheckApplicability applicability)
    {
        return new QaCheckDefinition
        {
            Id = id,
            DisplayName = displayName,
            Description = description,
            Section = section,
            Applicability = applicability,
            AppearsOnGeneratedReport = true,
            MustBeResolvedBeforeReportGeneration = true
        };
    }
}
