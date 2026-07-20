using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Forms;

/// <summary>
/// Evaluates presentation-time checklist applicability from the report draft's file characteristics.
/// </summary>
internal static class QaChecklistApplicabilityEvaluator
{
    public static bool IsApplicable(
        QaCheckDefinition definition,
        QaFileCharacteristics characteristics)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(characteristics);

        return definition.Applicability switch
        {
            QaCheckApplicability.Always => true,
            QaCheckApplicability.SeparateNameColumns =>
                characteristics.NameColumnMode ==
                    QaNameColumnMode.SeparateFirstAndLastName,
            QaCheckApplicability.FullNameColumn =>
                characteristics.NameColumnMode == QaNameColumnMode.FullName,
            QaCheckApplicability.CurrencyColumnPresent =>
                characteristics.HasCurrencyColumn,
            QaCheckApplicability.OneMonetaryColumn =>
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.OneMonetaryColumn,
            QaCheckApplicability.TwoMonetaryColumns =>
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.TwoMonetaryColumns,
            QaCheckApplicability.MoreThanTwoMonetaryColumns =>
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns,
            QaCheckApplicability.RejectedRecordsPresent =>
                characteristics.HasRejectedDatabaseRecords,
            _ => throw new ArgumentOutOfRangeException(
                nameof(definition),
                definition.Applicability,
                "Unknown QA checklist applicability value.")
        };
    }
}
