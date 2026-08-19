namespace DocumentationLoggingDashboard.QAReports.Definitions;

/// <summary>
/// Defines stable identifiers for the fixed Quick QA checklist.
/// Quick identifiers are intentionally independent from Detailed QA identifiers.
/// </summary>
public static class QuickQaChecklistIds
{
    public static class Raw
    {
        public const string NamesAvailable = "QUICK.RAW.NAMES.AVAILABLE";
        public const string ConfirmationNumberAvailable =
            "QUICK.RAW.CONFIRMATION_NUMBER.AVAILABLE";
        public const string ReservationDateAvailable =
            "QUICK.RAW.RESERVATION_DATE.AVAILABLE";
        public const string ArrivalDateAvailable =
            "QUICK.RAW.ARRIVAL_DATE.AVAILABLE";
        public const string DepartureDateAvailable =
            "QUICK.RAW.DEPARTURE_DATE.AVAILABLE";
        public const string MonetaryAvailable = "QUICK.RAW.MONETARY.AVAILABLE";
        public const string FileFormatConsistent =
            "QUICK.RAW.FILE.FORMAT_CONSISTENT";
        public const string DateFormatValid = "QUICK.RAW.DATES.FORMAT_VALID";
        public const string DateSequenceValid =
            "QUICK.RAW.DATES.SEQUENCE_VALID";
        public const string CurrencyConsistent =
            "QUICK.RAW.CURRENCY.CONSISTENT";
        public const string SourceColumnAvailable =
            "QUICK.RAW.STRATEGY.SOURCE_COLUMN_AVAILABLE";
        public const string RateColumnAvailable =
            "QUICK.RAW.STRATEGY.RATE_COLUMN_AVAILABLE";
        public const string MarketColumnAvailable =
            "QUICK.RAW.STRATEGY.MARKET_COLUMN_AVAILABLE";
        public const string ConfirmationCandidatesReviewed =
            "QUICK.RAW.CONFIRMATION.CANDIDATES_REVIEWED";
    }

    public static class Database
    {
        public const string NamesPulledCorrectly =
            "QUICK.DB.NAMES.PULLED_CORRECTLY";
        public const string MonetaryPulledCorrectly =
            "QUICK.DB.MONETARY.PULLED_CORRECTLY";
        public const string DatesPulledCorrectly =
            "QUICK.DB.DATES.PULLED_CORRECTLY";
        public const string ConfirmationNumberPulledCorrectly =
            "QUICK.DB.CONFIRMATION_NUMBER.PULLED_CORRECTLY";
        public const string RequiredValuesPresent =
            "QUICK.DB.REQUIRED_VALUES.PRESENT";
        public const string RejectedRecordsAccountedFor =
            "QUICK.DB.REJECTED_RECORDS.ACCOUNTED_FOR";
        public const string EmailPulledCorrectly =
            "QUICK.DB.EMAIL.PULLED_CORRECTLY";
    }
}
