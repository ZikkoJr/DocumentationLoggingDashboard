using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Computes deterministic evidence for the Quick QA state that drives its
/// generated Summary. Report details and Summary text are intentionally excluded.
/// </summary>
public static class QuickQaStateFingerprint
{
    private const string FingerprintVersion = "QUICK-QA-SUMMARY-V1";

    public static string Compute(QuickQaReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        StringBuilder value = new();
        Append(value, "version", FingerprintVersion);
        Append(
            value,
            "customScriptAvailable",
            report.CustomScriptAvailable ? "1" : "0");
        Append(
            value,
            "finalStatus",
            Convert.ToInt64(report.FinalStatus, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture));

        Dictionary<string, int> catalogOrder = QuickQaChecklistCatalog.Definitions
            .Select((definition, index) => new { definition.Id, Index = index })
            .ToDictionary(
                entry => entry.Id,
                entry => entry.Index,
                StringComparer.Ordinal);

        Append(
            value,
            "catalogCount",
            QuickQaChecklistCatalog.Definitions.Count.ToString(
                CultureInfo.InvariantCulture));
        foreach (QuickQaCheckDefinition definition in
                 QuickQaChecklistCatalog.Definitions)
        {
            Append(value, "catalogId", definition.Id);
        }

        Append(
            value,
            "resultCount",
            report.ChecklistResults.Count.ToString(CultureInfo.InvariantCulture));
        foreach (QuickQaCheckResult? result in report.ChecklistResults
                     .Select((item, index) => new { Item = item, Index = index })
                     .OrderBy(entry =>
                         entry.Item is not null
                         && catalogOrder.TryGetValue(
                             entry.Item.CheckId,
                             out int order)
                             ? order
                             : int.MaxValue)
                     .ThenBy(entry => entry.Item?.CheckId, StringComparer.Ordinal)
                     .ThenBy(entry => entry.Index)
                     .Select(entry => entry.Item))
        {
            Append(value, "resultPresent", result is null ? "0" : "1");

            if (result is null)
            {
                continue;
            }

            Append(value, "checkId", result.CheckId);
            Append(
                value,
                "checkStatus",
                Convert.ToInt64(result.Status, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture));
        }

        Append(
            value,
            "findingCount",
            report.Findings.Count.ToString(CultureInfo.InvariantCulture));
        foreach (QaFinding? finding in report.Findings
                     .Select((item, index) => new { Item = item, Index = index })
                     .OrderBy(
                         entry => entry.Item?.FindingId,
                         StringComparer.Ordinal)
                     .ThenBy(entry => entry.Index)
                     .Select(entry => entry.Item))
        {
            Append(value, "findingPresent", finding is null ? "0" : "1");

            if (finding is null)
            {
                continue;
            }

            Append(value, "findingId", finding.FindingId);
            Append(value, "relatedCheckId", finding.RelatedCheckId);
            Append(
                value,
                "severity",
                Convert.ToInt64(finding.Severity, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture));
            Append(value, "title", finding.Title);
            Append(value, "description", finding.Description);
            Append(
                value,
                "resolution",
                Convert.ToInt64(finding.Resolution, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture));
            Append(value, "resolutionNotes", finding.ResolutionNotes);
            Append(
                value,
                "source",
                Convert.ToInt64(finding.Source, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture));
        }

        byte[] digest = SHA256.HashData(
            Encoding.UTF8.GetBytes(value.ToString()));
        return Convert.ToHexString(digest);
    }

    private static void Append(
        StringBuilder builder,
        string name,
        string? item)
    {
        builder.Append(name.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(name);
        builder.Append('=');

        if (item is null)
        {
            builder.Append("-1:");
        }
        else
        {
            builder.Append(item.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(item);
        }

        builder.Append(';');
    }
}
