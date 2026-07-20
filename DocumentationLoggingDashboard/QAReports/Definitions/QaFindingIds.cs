namespace DocumentationLoggingDashboard.QAReports.Definitions;

/// <summary>
/// Defines deterministic identifiers for findings generated from the QA report draft.
/// </summary>
public static class QaFindingIds
{
    public const string FullNameCharacteristicWarning =
        "WARN:CHARACTERISTIC:FULL_NAME";

    public const string MoreThanTwoMonetaryColumnsCharacteristicWarning =
        "WARN:CHARACTERISTIC:MORE_THAN_TWO_MONETARY_COLUMNS";

    public const string MultipleConfirmationCandidatesCharacteristicWarning =
        "WARN:CHARACTERISTIC:MULTIPLE_CONFIRMATION_CANDIDATES";

    public const string UnusualAverageRateStatisticWarning =
        "WARN:STAT:UNUSUAL:AVERAGE_RATE";

    public const string UnusualStayValueStatisticWarning =
        "WARN:STAT:UNUSUAL:STAY_VALUE";

    public const string HighStayValueStatisticWarning =
        "WARN:STAT:HIGH_STAY_VALUE";

    public const string RowDifferenceStatisticWarning =
        "WARN:STAT:ROW_DIFFERENCE";

    public const string RejectedRecordsStatisticWarning =
        "WARN:STAT:REJECTED_RECORDS";

    public static string FailureForCheck(string checkId)
    {
        return CreateForCheck("FAIL:", checkId);
    }

    public static string WarningForCheck(string checkId)
    {
        return CreateForCheck("WARN:", checkId);
    }

    public static string WarningForBlankStatistic(string fieldId)
    {
        return CreateForStatisticField("WARN:STAT:BLANK:", fieldId);
    }

    public static string FailureForBlankStatistic(string fieldId)
    {
        return CreateForStatisticField("FAIL:STAT:BLANK:", fieldId);
    }

    public static string WarningForBrokenStatistic(string fieldId)
    {
        return CreateForStatisticField("WARN:STAT:BROKEN:", fieldId);
    }

    public static string FailureForBrokenStatistic(string fieldId)
    {
        return CreateForStatisticField("FAIL:STAT:BROKEN:", fieldId);
    }

    private static string CreateForCheck(string prefix, string checkId)
    {
        if (string.IsNullOrWhiteSpace(checkId))
        {
            throw new ArgumentException(
                "A nonblank checklist ID is required to create a finding ID.",
                nameof(checkId));
        }

        return prefix + checkId;
    }

    private static string CreateForStatisticField(string prefix, string fieldId)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            throw new ArgumentException(
                "A nonblank statistic field ID is required to create a finding ID.",
                nameof(fieldId));
        }

        return prefix + fieldId;
    }
}
