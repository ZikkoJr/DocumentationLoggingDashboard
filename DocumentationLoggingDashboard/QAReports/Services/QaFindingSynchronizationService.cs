using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Forms;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Reconciles generated QA findings with the current in-memory report draft.
/// </summary>
public sealed class QaFindingSynchronizationService
{
    private readonly IReadOnlyList<QaCheckDefinition> checklistDefinitions;
    private readonly Dictionary<string, QaCheckDefinition> checklistDefinitionsById;
    private readonly HashSet<string> managedFindingIds;

    public QaFindingSynchronizationService()
        : this(QaChecklistCatalog.Definitions)
    {
    }

    public QaFindingSynchronizationService(
        IReadOnlyList<QaCheckDefinition> checklistDefinitions)
    {
        ArgumentNullException.ThrowIfNull(checklistDefinitions);

        this.checklistDefinitions = checklistDefinitions.ToArray();
        checklistDefinitionsById = ValidateAndIndexDefinitions(
            this.checklistDefinitions);
        managedFindingIds = CreateManagedFindingIds(this.checklistDefinitions);
    }

    /// <summary>
    /// Adds or removes the deterministic warning that represents one checklist row's
    /// Warning Found selection, then reconciles all generated findings.
    /// </summary>
    public void SetChecklistWarningSelected(
        QaReport report,
        string checkId,
        bool selected)
    {
        ArgumentNullException.ThrowIfNull(report);

        string findingId = QaFindingIds.WarningForCheck(checkId);

        if (!checklistDefinitionsById.TryGetValue(
                checkId,
                out QaCheckDefinition? definition))
        {
            throw new ArgumentException(
                $"Checklist ID '{checkId}' does not exist in the finding service's definition snapshot.",
                nameof(checkId));
        }

        Dictionary<string, QaCheckResult> resultsById =
            ValidateAndIndexResults(report);
        Dictionary<string, QaFinding> existingById =
            ValidateAndIndexFindings(report.Findings);

        QaFileCharacteristics characteristics = GetFileCharacteristics(report);
        QaCheckResult result = resultsById[checkId];
        bool canSelectWarning = selected
            && result.Status == QaCheckStatus.Pass
            && QaChecklistApplicabilityEvaluator.IsApplicable(
                definition,
                characteristics);

        if (!canSelectWarning)
        {
            if (existingById.TryGetValue(findingId, out QaFinding? existing))
            {
                report.Findings.Remove(existing);
            }
        }
        else if (!existingById.ContainsKey(findingId))
        {
            report.Findings.Add(CreateChecklistWarning(definition, result));
        }

        SynchronizeFindings(report);
    }

    /// <summary>
    /// Synchronizes generated failures and warnings while preserving valid state on
    /// finding objects whose deterministic IDs remain continuously expected.
    /// </summary>
    public void SynchronizeFindings(QaReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        QaFileCharacteristics characteristics = GetFileCharacteristics(report);
        Dictionary<string, QaCheckResult> resultsById =
            ValidateAndIndexResults(report);
        Dictionary<string, QaFinding> existingById =
            ValidateAndIndexFindings(report.Findings);

        Dictionary<string, QaFinding> expectedGeneratedById =
            BuildExpectedGeneratedFindings(
                resultsById,
                existingById,
                characteristics);

        List<QaFinding> synchronizedFindings = [];

        foreach (QaFinding expected in expectedGeneratedById.Values)
        {
            QaFinding synchronized;

            if (existingById.TryGetValue(expected.FindingId, out QaFinding? existing))
            {
                ApplyGeneratedFields(existing, expected);
                synchronized = existing;
            }
            else
            {
                synchronized = expected;
            }

            NormalizeResolution(
                synchronized,
                characteristics.IsCustomScriptSupportAvailable);
            synchronizedFindings.Add(synchronized);
        }

        foreach (QaFinding existing in report.Findings)
        {
            if (managedFindingIds.Contains(existing.FindingId))
            {
                continue;
            }

            NormalizeResolution(
                existing,
                characteristics.IsCustomScriptSupportAvailable);
            synchronizedFindings.Add(existing);
        }

        synchronizedFindings.Sort(
            (left, right) => StringComparer.Ordinal.Compare(
                left.FindingId,
                right.FindingId));

        report.Findings.Clear();
        report.Findings.AddRange(synchronizedFindings);

        ValidateAndIndexFindings(report.Findings);
    }

    private Dictionary<string, QaFinding> BuildExpectedGeneratedFindings(
        IReadOnlyDictionary<string, QaCheckResult> resultsById,
        IReadOnlyDictionary<string, QaFinding> existingById,
        QaFileCharacteristics characteristics)
    {
        Dictionary<string, QaFinding> expectedById =
            new(StringComparer.Ordinal);

        foreach (QaCheckDefinition definition in checklistDefinitions)
        {
            QaCheckResult result = resultsById[definition.Id];

            if (result.Status == QaCheckStatus.Fail)
            {
                AddExpected(
                    expectedById,
                    CreateChecklistFailure(definition, result));
                continue;
            }

            string warningId = QaFindingIds.WarningForCheck(definition.Id);

            if (result.Status == QaCheckStatus.Pass
                && existingById.ContainsKey(warningId)
                && QaChecklistApplicabilityEvaluator.IsApplicable(
                    definition,
                    characteristics))
            {
                AddExpected(
                    expectedById,
                    CreateChecklistWarning(definition, result));
            }
        }

        if (characteristics.NameColumnMode == QaNameColumnMode.FullName)
        {
            AddExpected(expectedById, CreateFullNameWarning());
        }

        if (characteristics.MonetaryColumnScenario ==
            QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns)
        {
            AddExpected(
                expectedById,
                CreateMoreThanTwoMonetaryColumnsWarning());
        }

        if (characteristics.HasMultipleConfirmationNumberCandidateColumns)
        {
            AddExpected(
                expectedById,
                CreateMultipleConfirmationCandidatesWarning());
        }

        return expectedById;
    }

    private static QaFinding CreateChecklistFailure(
        QaCheckDefinition definition,
        QaCheckResult result)
    {
        return new QaFinding
        {
            FindingId = QaFindingIds.FailureForCheck(result.CheckId),
            RelatedCheckId = result.CheckId,
            Severity = QaFindingSeverity.Failure,
            Title = $"Failed check: {definition.DisplayName}",
            Description = TrimToNull(result.Notes) ?? definition.Description,
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static QaFinding CreateChecklistWarning(
        QaCheckDefinition definition,
        QaCheckResult result)
    {
        return new QaFinding
        {
            FindingId = QaFindingIds.WarningForCheck(result.CheckId),
            RelatedCheckId = result.CheckId,
            Severity = QaFindingSeverity.Warning,
            Title = $"Warning for passed check: {definition.DisplayName}",
            Description = TrimToNull(result.Notes) ?? string.Empty,
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static QaFinding CreateFullNameWarning()
    {
        return new QaFinding
        {
            FindingId = QaFindingIds.FullNameCharacteristicWarning,
            RelatedCheckId = QaChecklistIds.Raw.RequiredNameFieldPresent,
            Severity = QaFindingSeverity.Warning,
            Title = "Full Name field is used",
            Description = "The file uses one Full Name field rather than separate First Name and Last Name fields.",
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static QaFinding CreateMoreThanTwoMonetaryColumnsWarning()
    {
        return new QaFinding
        {
            FindingId =
                QaFindingIds.MoreThanTwoMonetaryColumnsCharacteristicWarning,
            RelatedCheckId = QaChecklistIds.Raw.RequiredMonetaryValuePresent,
            Severity = QaFindingSeverity.Warning,
            Title = "More than two monetary-value columns were found",
            Description = "More than two candidate monetary-value columns require special review to identify the correct Average Rate and Stay Value fields.",
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static QaFinding CreateMultipleConfirmationCandidatesWarning()
    {
        return new QaFinding
        {
            FindingId =
                QaFindingIds.MultipleConfirmationCandidatesCharacteristicWarning,
            RelatedCheckId = QaChecklistIds.Raw.ConfirmationCandidatesReviewed,
            Severity = QaFindingSeverity.Warning,
            Title = "Multiple Confirmation Number candidates were found",
            Description = "Multiple candidate fields were found, and the correct Confirmation Number field must be confirmed.",
            Resolution = QaFindingResolution.Active,
            Source = QaFindingSource.Checklist
        };
    }

    private static void AddExpected(
        IDictionary<string, QaFinding> expectedById,
        QaFinding finding)
    {
        if (!expectedById.TryAdd(finding.FindingId, finding))
        {
            throw new InvalidOperationException(
                $"Finding generation produced duplicate ID '{finding.FindingId}'.");
        }
    }

    private static void ApplyGeneratedFields(
        QaFinding target,
        QaFinding expected)
    {
        target.FindingId = expected.FindingId;
        target.RelatedCheckId = expected.RelatedCheckId;
        target.Severity = expected.Severity;
        target.Title = expected.Title;
        target.Description = expected.Description;
        target.Source = expected.Source;
    }

    private static void NormalizeResolution(
        QaFinding finding,
        bool isCustomScriptSupportAvailable)
    {
        finding.ResolutionNotes = TrimToNull(finding.ResolutionNotes);
        finding.CustomScriptName = TrimToNull(finding.CustomScriptName);

        if (finding.Resolution == QaFindingResolution.HandledByCustomScript
            && !isCustomScriptSupportAvailable)
        {
            finding.Resolution = QaFindingResolution.Active;
            finding.CustomScriptName = null;
            finding.ResolutionNotes = null;
            return;
        }

        if (finding.Resolution == QaFindingResolution.ExplainedAndAccepted)
        {
            finding.CustomScriptName = null;

            if (finding.Severity == QaFindingSeverity.Warning)
            {
                return;
            }

            finding.Resolution = QaFindingResolution.Active;
            finding.ResolutionNotes = null;
            return;
        }

        if (finding.Resolution == QaFindingResolution.HandledByCustomScript)
        {
            return;
        }

        if (finding.Resolution != QaFindingResolution.Active)
        {
            finding.Resolution = QaFindingResolution.Active;
        }

        finding.CustomScriptName = null;
    }

    private static Dictionary<string, QaCheckDefinition>
        ValidateAndIndexDefinitions(
            IReadOnlyList<QaCheckDefinition> definitions)
    {
        if (definitions.Count == 0)
        {
            throw new ArgumentException(
                "At least one checklist definition is required.",
                nameof(definitions));
        }

        Dictionary<string, QaCheckDefinition> definitionsById =
            new(StringComparer.Ordinal);

        foreach (QaCheckDefinition? definition in definitions)
        {
            if (definition is null)
            {
                throw new ArgumentException(
                    "The checklist definition snapshot cannot contain null entries.",
                    nameof(definitions));
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new ArgumentException(
                    "Every checklist definition must have a nonblank stable ID.",
                    nameof(definitions));
            }

            if (string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                throw new ArgumentException(
                    $"Checklist definition '{definition.Id}' must have a nonblank display name.",
                    nameof(definitions));
            }

            if (string.IsNullOrWhiteSpace(definition.Description))
            {
                throw new ArgumentException(
                    $"Checklist definition '{definition.Id}' must have a nonblank description.",
                    nameof(definitions));
            }

            if (!definitionsById.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException(
                    $"The checklist definition snapshot contains duplicate stable ID '{definition.Id}'.",
                    nameof(definitions));
            }
        }

        return definitionsById;
    }

    private static HashSet<string> CreateManagedFindingIds(
        IEnumerable<QaCheckDefinition> definitions)
    {
        HashSet<string> ids = new(StringComparer.Ordinal)
        {
            QaFindingIds.FullNameCharacteristicWarning,
            QaFindingIds.MoreThanTwoMonetaryColumnsCharacteristicWarning,
            QaFindingIds.MultipleConfirmationCandidatesCharacteristicWarning
        };

        foreach (QaCheckDefinition definition in definitions)
        {
            if (!ids.Add(QaFindingIds.FailureForCheck(definition.Id))
                || !ids.Add(QaFindingIds.WarningForCheck(definition.Id)))
            {
                throw new ArgumentException(
                    $"Checklist ID '{definition.Id}' collides with a deterministic finding ID.",
                    nameof(definitions));
            }
        }

        return ids;
    }

    private Dictionary<string, QaCheckResult> ValidateAndIndexResults(
        QaReport report)
    {
        Dictionary<string, QaCheckResult> resultsById =
            new(StringComparer.Ordinal);

        foreach (QaCheckResult? result in report.ChecklistResults)
        {
            if (result is null)
            {
                throw new InvalidOperationException(
                    "The QA report checklist result collection cannot contain null entries.");
            }

            if (string.IsNullOrWhiteSpace(result.CheckId))
            {
                throw new InvalidOperationException(
                    "Every QA checklist result must have a nonblank stable ID.");
            }

            if (!checklistDefinitionsById.ContainsKey(result.CheckId))
            {
                throw new InvalidOperationException(
                    $"QA checklist result '{result.CheckId}' has no matching definition.");
            }

            if (!Enum.IsDefined(typeof(QaCheckStatus), result.Status))
            {
                throw new InvalidOperationException(
                    $"QA checklist result '{result.CheckId}' has an unknown status value.");
            }

            if (!resultsById.TryAdd(result.CheckId, result))
            {
                throw new InvalidOperationException(
                    $"The QA report contains duplicate checklist result ID '{result.CheckId}'.");
            }
        }

        if (resultsById.Count != checklistDefinitions.Count
            || checklistDefinitions.Any(
                definition => !resultsById.ContainsKey(definition.Id)))
        {
            throw new InvalidOperationException(
                "The QA report must contain exactly one checklist result for every definition in the finding service snapshot.");
        }

        return resultsById;
    }

    private static Dictionary<string, QaFinding> ValidateAndIndexFindings(
        IEnumerable<QaFinding> findings)
    {
        Dictionary<string, QaFinding> findingsById =
            new(StringComparer.Ordinal);

        foreach (QaFinding? finding in findings)
        {
            if (finding is null)
            {
                throw new InvalidOperationException(
                    "The QA report findings collection cannot contain null entries.");
            }

            if (string.IsNullOrWhiteSpace(finding.FindingId))
            {
                throw new InvalidOperationException(
                    "Every QA finding must have a nonblank deterministic ID.");
            }

            if (!findingsById.TryAdd(finding.FindingId, finding))
            {
                throw new InvalidOperationException(
                    $"The QA report contains duplicate finding ID '{finding.FindingId}'.");
            }
        }

        return findingsById;
    }

    private static QaFileCharacteristics GetFileCharacteristics(QaReport report)
    {
        return report.FileCharacteristics
            ?? throw new InvalidOperationException(
                "The QA report must have file characteristics before findings can be synchronized.");
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
