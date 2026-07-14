namespace DocumentationLoggingDashboard.QAReports.Definitions;

/// <summary>
/// Defines deterministic identifiers for findings generated from the QA checklist draft.
/// </summary>
public static class QaFindingIds
{
    public const string FullNameCharacteristicWarning =
        "WARN:CHARACTERISTIC:FULL_NAME";

    public const string MoreThanTwoMonetaryColumnsCharacteristicWarning =
        "WARN:CHARACTERISTIC:MORE_THAN_TWO_MONETARY_COLUMNS";

    public const string MultipleConfirmationCandidatesCharacteristicWarning =
        "WARN:CHARACTERISTIC:MULTIPLE_CONFIRMATION_CANDIDATES";

    public static string FailureForCheck(string checkId)
    {
        return CreateForCheck("FAIL:", checkId);
    }

    public static string WarningForCheck(string checkId)
    {
        return CreateForCheck("WARN:", checkId);
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
}
