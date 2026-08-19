namespace DocumentationLoggingDashboard.QAReports.Definitions;

/// <summary>
/// Defines stable QA checklist identifiers shared by the UI, report output, and diagnostics.
/// </summary>
public static class QaChecklistIds
{
    public static class Raw
    {
        public const string RequiredNameFieldPresent = "RAW.REQUIRED.NAME_FIELD_PRESENT";
        public const string RequiredConfirmationPresent = "RAW.REQUIRED.CONFIRMATION_PRESENT";
        // Retired from the active catalog; retained only as a stable compatibility identifier.
        public const string RequiredEmailPresent = "RAW.REQUIRED.EMAIL_PRESENT";
        public const string EmailColumnAvailable = "RAW.EMAIL.COLUMN_AVAILABLE";
        public const string RequiredReservationDatePresent = "RAW.REQUIRED.RESERVATION_DATE_PRESENT";
        public const string RequiredArrivalDatePresent = "RAW.REQUIRED.ARRIVAL_DATE_PRESENT";
        public const string RequiredDepartureDatePresent = "RAW.REQUIRED.DEPARTURE_DATE_PRESENT";
        public const string RequiredMonetaryValuePresent = "RAW.REQUIRED.MONETARY_VALUE_PRESENT";
        public const string FileFormatConsistent = "RAW.FILE.FORMAT_CONSISTENT";
        public const string FirstNameValuesValid = "RAW.NAMES.FIRST_NAME_VALUES_VALID";
        public const string LastNameValuesValid = "RAW.NAMES.LAST_NAME_VALUES_VALID";
        public const string FullNameValuesValid = "RAW.NAMES.FULL_NAME_VALUES_VALID";
        public const string DateSequenceValid = "RAW.DATES.SEQUENCE_VALID";
        public const string DateFormatValid = "RAW.DATES.FORMAT_VALID";
        // Retired from the active catalog; retained only as a stable compatibility identifier.
        public const string ArrivalWithinFileMonth = "RAW.DATES.ARRIVAL_WITHIN_FILE_MONTH";
        public const string CurrencyConsistent = "RAW.CURRENCY.CONSISTENT";
        public const string SingleMonetaryColumnIsAverageRate = "RAW.MONETARY.SINGLE_COLUMN_IS_AVERAGE_RATE";
        public const string TwoMonetaryColumnsIdentified = "RAW.MONETARY.TWO_COLUMNS_IDENTIFIED";
        public const string MonetarySpotCheckValid = "RAW.MONETARY.SPOT_CHECK_VALID";
        public const string AverageRateNotAboveStayValue = "RAW.MONETARY.AVERAGE_RATE_NOT_ABOVE_STAY_VALUE";
        // Retired from the active catalog; retained only as a stable compatibility identifier.
        public const string SourceColumnPresent = "RAW.SOURCE.COLUMN_PRESENT";
        public const string StrategySourceColumnAvailable =
            "RAW.STRATEGY.SOURCE_COLUMN_AVAILABLE";
        public const string StrategyRateColumnAvailable =
            "RAW.STRATEGY.RATE_COLUMN_AVAILABLE";
        public const string StrategyMarketColumnAvailable =
            "RAW.STRATEGY.MARKET_COLUMN_AVAILABLE";
        public const string ConfirmationCandidatesReviewed = "RAW.CONFIRMATION.CANDIDATES_REVIEWED";
    }

    public static class Database
    {
        public const string NamesMappedCorrectly = "DB.NAMES.MAPPED_CORRECTLY";
        public const string MonetaryMappedCorrectly = "DB.MONETARY.MAPPED_CORRECTLY";
        public const string EmailMappedCorrectly = "DB.EMAIL.MAPPED_CORRECTLY";
        public const string DatesMappedCorrectly = "DB.DATES.MAPPED_CORRECTLY";
        public const string ConfirmationMappedCorrectly = "DB.CONFIRMATION.MAPPED_CORRECTLY";
        public const string RequiredValuesPresent = "DB.REQUIRED_VALUES_PRESENT";
        public const string RejectedRecordsAccountedFor = "DB.REJECTED_RECORDS_ACCOUNTED_FOR";
    }
}
