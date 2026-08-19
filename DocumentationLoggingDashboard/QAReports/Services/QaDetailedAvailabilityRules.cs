using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Owns the Detailed QA finding semantics for Email and the aggregate
/// Source/Rate/Market availability group.
/// </summary>
internal static class QaDetailedAvailabilityRules
{
    private static readonly IReadOnlyList<StrategyCheck> StrategyChecks =
    [
        new(QaChecklistIds.Raw.StrategySourceColumnAvailable, "Source"),
        new(QaChecklistIds.Raw.StrategyRateColumnAvailable, "Rate"),
        new(QaChecklistIds.Raw.StrategyMarketColumnAvailable, "Market")
    ];

    public static bool UsesAvailabilityPresentation(string checkId)
    {
        return IsEmailAvailabilityCheck(checkId)
            || IsStrategyAvailabilityCheck(checkId);
    }

    public static bool SuppressesOrdinaryChecklistFinding(string checkId)
    {
        return UsesAvailabilityPresentation(checkId);
    }

    public static IReadOnlyList<QaFinding> CreateExpectedFindings(
        IReadOnlyDictionary<string, QaCheckResult> resultsById)
    {
        ArgumentNullException.ThrowIfNull(resultsById);

        List<QaFinding> findings = [];

        if (resultsById.TryGetValue(
                QaChecklistIds.Raw.EmailColumnAvailable,
                out QaCheckResult? emailResult)
            && emailResult.Status == QaCheckStatus.Fail)
        {
            findings.Add(new QaFinding
            {
                FindingId = QaFindingIds.WarningForCheck(
                    QaChecklistIds.Raw.EmailColumnAvailable),
                RelatedCheckId = QaChecklistIds.Raw.EmailColumnAvailable,
                Severity = QaFindingSeverity.Warning,
                Title = "Email column unavailable",
                Description = "Email column is not available in the raw file.",
                Resolution = QaFindingResolution.Active,
                Source = QaFindingSource.Checklist
            });
        }

        StrategyCheck[] missing = StrategyChecks
            .Where(check => resultsById.TryGetValue(
                    check.CheckId,
                    out QaCheckResult? result)
                && result.Status == QaCheckStatus.Fail)
            .ToArray();

        if (missing.Length is 1 or 2)
        {
            string categories = FormatCategories(
                missing.Select(check => check.Category).ToArray());
            string sentence = missing.Length == 1
                ? $"Strategy Warning: {categories} column is not available."
                : $"Strategy Warning: {categories} columns are not available.";

            findings.Add(new QaFinding
            {
                FindingId = QaFindingIds.StrategySourceRateMarketWarning,
                RelatedCheckId = null,
                Severity = QaFindingSeverity.Warning,
                Title = sentence,
                Description = sentence,
                Resolution = QaFindingResolution.Active,
                Source = QaFindingSource.Checklist
            });
        }
        else if (missing.Length == StrategyChecks.Count)
        {
            const string sentence =
                "Source, Rate, and Market strategy columns are all unavailable.";

            findings.Add(new QaFinding
            {
                FindingId = QaFindingIds.StrategySourceRateMarketFailure,
                RelatedCheckId = null,
                Severity = QaFindingSeverity.Failure,
                Title = sentence,
                Description = sentence,
                Resolution = QaFindingResolution.Active,
                Source = QaFindingSource.Checklist
            });
        }

        return findings;
    }

    private static bool IsEmailAvailabilityCheck(string checkId)
    {
        return string.Equals(
            checkId,
            QaChecklistIds.Raw.EmailColumnAvailable,
            StringComparison.Ordinal);
    }

    private static bool IsStrategyAvailabilityCheck(string checkId)
    {
        return StrategyChecks.Any(check => string.Equals(
            check.CheckId,
            checkId,
            StringComparison.Ordinal));
    }

    private static string FormatCategories(IReadOnlyList<string> categories)
    {
        return categories.Count switch
        {
            1 => categories[0],
            2 => categories[0] + " and " + categories[1],
            _ => throw new ArgumentOutOfRangeException(
                nameof(categories),
                "One or two strategy categories are required.")
        };
    }

    private sealed record StrategyCheck(string CheckId, string Category);
}
