namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Defines the result state for a single QA checklist item.
/// </summary>
public enum QaCheckStatus
{
    NotEvaluated,
    Pass,
    Fail,
    NotApplicable
}

/// <summary>
/// Identifies whether a checklist result was entered manually or produced by automation.
/// </summary>
public enum QaResultSource
{
    Manual,
    Automated
}

/// <summary>
/// Defines the original severity of a warning or failed QA finding.
/// </summary>
public enum QaFindingSeverity
{
    Warning,
    Failure
}

/// <summary>
/// Defines the resolution state for an individual QA finding.
/// </summary>
public enum QaFindingResolution
{
    Active,
    HandledByCustomScript,
    ExplainedAndAccepted
}

/// <summary>
/// Identifies the source that produced or motivated a QA finding.
/// </summary>
public enum QaFindingSource
{
    Checklist,
    Statistic,
    DatabaseComparison,
    Manual
}

/// <summary>
/// Defines the optional calculated status for a completed QA report.
/// </summary>
public enum QaReportStatus
{
    Pass,
    PassWithWarnings,
    Fail
}

/// <summary>
/// Defines the checklist section where a QA check appears.
/// </summary>
public enum QaChecklistSection
{
    RawFile,
    Database
}

/// <summary>
/// Defines the metadata that later phases will use to decide whether a check applies.
/// </summary>
public enum QaCheckApplicability
{
    Always,
    SeparateNameColumns,
    FullNameColumn,
    CurrencyColumnPresent,
    OneMonetaryColumn,
    TwoMonetaryColumns,
    MoreThanTwoMonetaryColumns,
    RejectedRecordsPresent
}

/// <summary>
/// Defines how guest-name columns are represented in the source file.
/// </summary>
public enum QaNameColumnMode
{
    SeparateFirstAndLastName,
    FullName
}

/// <summary>
/// Defines the number of monetary-value columns that require QA review.
/// </summary>
public enum QaMonetaryColumnScenario
{
    OneMonetaryColumn,
    TwoMonetaryColumns,
    MoreThanTwoMonetaryColumns
}

/// <summary>
/// Defines whether the source-file headers are useful for later review.
/// </summary>
public enum QaUsefulHeadersResult
{
    Yes,
    Partially,
    No
}
