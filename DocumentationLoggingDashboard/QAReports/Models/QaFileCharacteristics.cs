namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Captures raw-file facts that later phases will use for checklist applicability and warnings.
/// </summary>
public sealed class QaFileCharacteristics
{
    public QaNameColumnMode NameColumnMode { get; set; } = QaNameColumnMode.SeparateFirstAndLastName;

    public bool HasCurrencyColumn { get; set; }

    public QaMonetaryColumnScenario MonetaryColumnScenario { get; set; } = QaMonetaryColumnScenario.OneMonetaryColumn;

    public bool HasMultipleConfirmationNumberCandidateColumns { get; set; }

    public bool IsCustomScriptSupportAvailable { get; set; }

    public bool HasRejectedDatabaseRecords { get; set; }
}
