using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Definitions;

/// <summary>
/// Provides the deterministic Phase 2 QA checklist definition catalog.
/// </summary>
public static class QaChecklistCatalog
{
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
            QaChecklistIds.Raw.RequiredEmailPresent,
            "Email field is present",
            "Confirms that the raw file includes an Email field.",
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
            "Confirms that First Name values contain first names; email addresses or unrelated values in First Name are examples of failures.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.SeparateNameColumns),
        Definition(
            QaChecklistIds.Raw.LastNameValuesValid,
            "Last Name values contain last names",
            "Confirms that Last Name values contain last names rather than unrelated or misplaced values.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.SeparateNameColumns),
        Definition(
            QaChecklistIds.Raw.FullNameValuesValid,
            "Full Name values contain valid full names",
            "Confirms that Full Name values contain usable full guest names when the file uses a single name column.",
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
            "Confirms that date values can be interpreted consistently during manual QA review.",
            QaChecklistSection.RawFile,
            QaCheckApplicability.Always),
        Definition(
            QaChecklistIds.Raw.ArrivalWithinFileMonth,
            "Arrival Dates are within the selected File Month",
            "Confirms that Arrival Dates align with the selected File Month. A later phase may fail this check when any Arrival Date is outside that month.",
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
            QaChecklistIds.Raw.SourceColumnPresent,
            "A Source, qualifying Rate, or Market field is present",
            "Confirms that a source-related field is present, such as Source, Booking Source, Market, Market Segment, Rate Code, Rate Plan, or Rate Plan Code. Average Rate does not satisfy this check.",
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
