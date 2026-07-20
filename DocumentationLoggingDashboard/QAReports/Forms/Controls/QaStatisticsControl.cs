using System.Globalization;
using System.Text;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

/// <summary>
/// Edits the approved statistics objects and presents the latest readiness result.
/// </summary>
public sealed class QaStatisticsControl : UserControl
{
    private const string PrivacyReminder =
        "Use counts and non-sensitive summaries only. Do not enter guest names,\r\n" +
        "email addresses, payment information, credentials, or reservation-level data.";

    private readonly QaStatisticsCalculationService calculationService = new();
    private readonly FlowLayoutPanel contentFlowLayoutPanel;
    private readonly FlowLayoutPanel blankRowsFlowLayoutPanel;
    private readonly FlowLayoutPanel brokenRowsFlowLayoutPanel;
    private readonly Dictionary<string, QaBlankStatisticRowControl> blankRowsById =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, QaBrokenDataStatisticRowControl> brokenRowsById =
        new(StringComparer.Ordinal);
    private readonly List<Label> privacyReminderLabels = [];
    private readonly HashSet<TextBox> pendingExplanationTextBoxes = [];

    private readonly NumericUpDown totalDataRowsNumericUpDown;
    private readonly ComboBox headersPresentComboBox;
    private readonly ComboBox usefulHeadersComboBox;
    private readonly NumericUpDown dataStartRowNumericUpDown;

    private readonly GroupBox nameStatisticsGroupBox;
    private readonly NumericUpDown multiwordFirstNameNumericUpDown;
    private readonly TextBox firstNameDenominatorTextBox;
    private readonly TextBox multiwordFirstNamePercentageTextBox;
    private readonly NumericUpDown multiwordLastNameNumericUpDown;
    private readonly TextBox lastNameDenominatorTextBox;
    private readonly TextBox multiwordLastNamePercentageTextBox;

    private readonly NumericUpDown validArrivalDateNumericUpDown;
    private readonly NumericUpDown withinFileMonthNumericUpDown;
    private readonly NumericUpDown outsideFileMonthNumericUpDown;
    private readonly TextBox withinFileMonthPercentageTextBox;
    private readonly TextBox outsideFileMonthPercentageTextBox;

    private readonly ComboBox unusualAverageRateComboBox;
    private readonly NumericUpDown unusualAverageRateCountNumericUpDown;
    private readonly TextBox averageRateDenominatorTextBox;
    private readonly TextBox unusualAverageRatePercentageTextBox;
    private readonly TextBox unusualAverageRateExplanationTextBox;

    private readonly GroupBox stayValueGroupBox;
    private readonly ComboBox unusualStayValueComboBox;
    private readonly NumericUpDown unusualStayValueCountNumericUpDown;
    private readonly TextBox stayValueDenominatorTextBox;
    private readonly TextBox unusualStayValuePercentageTextBox;
    private readonly TextBox unusualStayValueExplanationTextBox;
    private readonly NumericUpDown highStayValueCountNumericUpDown;
    private readonly TextBox highStayValueDenominatorTextBox;
    private readonly TextBox highStayValuePercentageTextBox;
    private readonly ComboBox highStayValuesExpectedComboBox;
    private readonly TextBox highStayValueExplanationTextBox;

    private readonly NumericUpDown importedRecordCountNumericUpDown;
    private readonly NumericUpDown rejectedRecordCountNumericUpDown;
    private readonly NumericUpDown missingRequiredDatabaseValuesNumericUpDown;
    private readonly TextBox signedDifferenceTextBox;
    private readonly TextBox absoluteDifferenceTextBox;
    private readonly TableLayoutPanel rowDifferenceExplanationPanel;
    private readonly TextBox rowDifferenceExplanationTextBox;
    private readonly TableLayoutPanel rejectedRecordsExplanationPanel;
    private readonly TextBox rejectedRecordsExplanationTextBox;

    private readonly TextBox readinessSummaryTextBox;

    private QaReport? boundReport;
    private bool isRefreshing;
    private bool isResizingContent;
    private bool isResizeContentQueued;

    public QaStatisticsControl()
    {
        AutoScaleMode = AutoScaleMode.Font;
        Dock = DockStyle.Fill;

        contentFlowLayoutPanel = new FlowLayoutPanel
        {
            AutoScroll = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(0, 0, 8, 8),
            WrapContents = false
        };
        Controls.Add(contentFlowLayoutPanel);

        Label privacyLabel = CreatePrivacyReminderLabel();
        contentFlowLayoutPanel.Controls.Add(privacyLabel);

        totalDataRowsNumericUpDown = CreateCountInput(
            "totalDataRowsNumericUpDown",
            "Total Data Rows");
        headersPresentComboBox = CreateChoice(
            "headersPresentComboBox",
            "Headers Present",
            "No",
            "Yes");
        usefulHeadersComboBox = CreateChoice(
            "usefulHeadersComboBox",
            "Useful Headers",
            "Yes",
            "Partially",
            "No");
        dataStartRowNumericUpDown = CreateCountInput(
            "dataStartRowNumericUpDown",
            "Data Start Row");
        contentFlowLayoutPanel.Controls.Add(CreateGroup(
            "File Information",
            CreateWrappingMetrics(
                CreateMetric(
                    "Total Data Rows (excluding header rows)",
                    totalDataRowsNumericUpDown),
                CreateMetric("Headers Present", headersPresentComboBox),
                CreateMetric("Useful Headers", usefulHeadersComboBox),
                CreateMetric("Data Start Row (0 = not entered)", dataStartRowNumericUpDown))));

        blankRowsFlowLayoutPanel = CreateVerticalFlow();
        contentFlowLayoutPanel.Controls.Add(CreateGroup(
            "Blank Value Statistics",
            blankRowsFlowLayoutPanel));

        brokenRowsFlowLayoutPanel = CreateVerticalFlow();
        contentFlowLayoutPanel.Controls.Add(CreateGroup(
            "Broken Data Statistics",
            CreateSection(
                CreatePrivacyReminderLabel(),
                brokenRowsFlowLayoutPanel)));

        multiwordFirstNameNumericUpDown = CreateCountInput(
            "multiwordFirstNameNumericUpDown",
            "Multiword First Name count");
        firstNameDenominatorTextBox = CreateReadOnlyValue(
            "Derived nonblank First Name denominator");
        multiwordFirstNamePercentageTextBox = CreateReadOnlyValue(
            "Calculated Multiword First Name percentage");
        multiwordLastNameNumericUpDown = CreateCountInput(
            "multiwordLastNameNumericUpDown",
            "Multiword Last Name count");
        lastNameDenominatorTextBox = CreateReadOnlyValue(
            "Derived nonblank Last Name denominator");
        multiwordLastNamePercentageTextBox = CreateReadOnlyValue(
            "Calculated Multiword Last Name percentage");
        nameStatisticsGroupBox = CreateGroup(
            "Name Statistics (informational)",
            CreateWrappingMetrics(
                CreateMetric("Multiword First Name count", multiwordFirstNameNumericUpDown),
                CreateMetric("Derived nonblank First Name", firstNameDenominatorTextBox),
                CreateMetric("First Name percentage", multiwordFirstNamePercentageTextBox),
                CreateMetric("Multiword Last Name count", multiwordLastNameNumericUpDown),
                CreateMetric("Derived nonblank Last Name", lastNameDenominatorTextBox),
                CreateMetric("Last Name percentage", multiwordLastNamePercentageTextBox)));
        contentFlowLayoutPanel.Controls.Add(nameStatisticsGroupBox);

        validArrivalDateNumericUpDown = CreateCountInput(
            "validArrivalDateNumericUpDown",
            "Valid Arrival Date count");
        withinFileMonthNumericUpDown = CreateCountInput(
            "withinFileMonthNumericUpDown",
            "Arrival Dates within File Month");
        outsideFileMonthNumericUpDown = CreateCountInput(
            "outsideFileMonthNumericUpDown",
            "Arrival Dates outside File Month");
        withinFileMonthPercentageTextBox = CreateReadOnlyValue(
            "Calculated percentage within File Month");
        outsideFileMonthPercentageTextBox = CreateReadOnlyValue(
            "Calculated percentage outside File Month");
        contentFlowLayoutPanel.Controls.Add(CreateGroup(
            "File Month Statistics",
            CreateWrappingMetrics(
                CreateMetric("Valid Arrival Date count", validArrivalDateNumericUpDown),
                CreateMetric("Within File Month", withinFileMonthNumericUpDown),
                CreateMetric("Outside File Month", outsideFileMonthNumericUpDown),
                CreateMetric("Percentage within", withinFileMonthPercentageTextBox),
                CreateMetric("Percentage outside", outsideFileMonthPercentageTextBox))));

        unusualAverageRateComboBox = CreateChoice(
            "unusualAverageRateComboBox",
            "Unusual Average Rate values found",
            "No",
            "Yes");
        unusualAverageRateCountNumericUpDown = CreateCountInput(
            "unusualAverageRateCountNumericUpDown",
            "Unusual Average Rate count");
        averageRateDenominatorTextBox = CreateReadOnlyValue(
            "Derived nonblank Average Rate denominator");
        unusualAverageRatePercentageTextBox = CreateReadOnlyValue(
            "Calculated unusual Average Rate percentage");
        unusualAverageRateExplanationTextBox = CreateExplanation(
            "unusualAverageRateExplanationTextBox",
            "Unusual Average Rate explanation");
        GroupBox averageRateGroupBox = CreateGroup(
            "Unusual Average Rate Values",
            CreateSection(
                CreateWrappingMetrics(
                    CreateMetric("Unusual values found", unusualAverageRateComboBox),
                    CreateMetric("Unusual count", unusualAverageRateCountNumericUpDown),
                    CreateMetric("Derived nonblank count", averageRateDenominatorTextBox),
                    CreateMetric("Percentage", unusualAverageRatePercentageTextBox)),
                CreateExplanationPanel(
                    "Non-sensitive explanation (required when Yes)",
                    unusualAverageRateExplanationTextBox)));

        unusualStayValueComboBox = CreateChoice(
            "unusualStayValueComboBox",
            "Unusual Stay Values found",
            "No",
            "Yes");
        unusualStayValueCountNumericUpDown = CreateCountInput(
            "unusualStayValueCountNumericUpDown",
            "Unusual Stay Value count");
        stayValueDenominatorTextBox = CreateReadOnlyValue(
            "Derived nonblank Stay Value denominator");
        unusualStayValuePercentageTextBox = CreateReadOnlyValue(
            "Calculated unusual Stay Value percentage");
        unusualStayValueExplanationTextBox = CreateExplanation(
            "unusualStayValueExplanationTextBox",
            "Unusual Stay Value explanation");
        highStayValueCountNumericUpDown = CreateCountInput(
            "highStayValueCountNumericUpDown",
            "Stay Values strictly above 10,000 count");
        highStayValueDenominatorTextBox = CreateReadOnlyValue(
            "Derived nonblank Stay Value denominator for high values");
        highStayValuePercentageTextBox = CreateReadOnlyValue(
            "Calculated high Stay Value percentage");
        highStayValuesExpectedComboBox = CreateChoice(
            "highStayValuesExpectedComboBox",
            "High Stay Values expected",
            "No",
            "Yes");
        highStayValueExplanationTextBox = CreateExplanation(
            "highStayValueExplanationTextBox",
            "Unexpected high Stay Value explanation");
        stayValueGroupBox = CreateGroup(
            "Stay Value Statistics",
            CreateSection(
                CreateWrappingMetrics(
                    CreateMetric("Unusual values found", unusualStayValueComboBox),
                    CreateMetric("Unusual count", unusualStayValueCountNumericUpDown),
                    CreateMetric("Derived nonblank count", stayValueDenominatorTextBox),
                    CreateMetric("Unusual percentage", unusualStayValuePercentageTextBox)),
                CreateExplanationPanel(
                    "Unusual Stay Value explanation (required when Yes)",
                    unusualStayValueExplanationTextBox),
                CreateWrappingMetrics(
                    CreateMetric("Stay Values strictly above 10,000", highStayValueCountNumericUpDown),
                    CreateMetric("Derived nonblank count", highStayValueDenominatorTextBox),
                    CreateMetric("High-value percentage", highStayValuePercentageTextBox),
                    CreateMetric("High Values Expected", highStayValuesExpectedComboBox)),
                CreateExplanationPanel(
                    "Explanation (required when high values are unexpected)",
                    highStayValueExplanationTextBox)));

        FlowLayoutPanel monetaryFlowLayoutPanel = CreateVerticalFlow();
        monetaryFlowLayoutPanel.Controls.Add(CreatePrivacyReminderLabel());
        monetaryFlowLayoutPanel.Controls.Add(averageRateGroupBox);
        monetaryFlowLayoutPanel.Controls.Add(stayValueGroupBox);
        contentFlowLayoutPanel.Controls.Add(CreateGroup(
            "Monetary Statistics",
            monetaryFlowLayoutPanel));

        importedRecordCountNumericUpDown = CreateCountInput(
            "importedRecordCountNumericUpDown",
            "Imported Record Count");
        rejectedRecordCountNumericUpDown = CreateCountInput(
            "rejectedRecordCountNumericUpDown",
            "Rejected Record Count");
        missingRequiredDatabaseValuesNumericUpDown = CreateCountInput(
            "missingRequiredDatabaseValuesNumericUpDown",
            "Records with missing required DB values");
        signedDifferenceTextBox = CreateReadOnlyValue(
            "Signed Total Data Rows minus Imported Record Count");
        absoluteDifferenceTextBox = CreateReadOnlyValue(
            "Absolute row difference");
        rowDifferenceExplanationTextBox = CreateExplanation(
            "rowDifferenceExplanationTextBox",
            "Row-difference explanation");
        rowDifferenceExplanationPanel = CreateExplanationPanel(
            "Row-difference explanation (required when absolute difference is 10 or more)",
            rowDifferenceExplanationTextBox);
        rejectedRecordsExplanationTextBox = CreateExplanation(
            "rejectedRecordsExplanationTextBox",
            "Rejected-record explanation");
        rejectedRecordsExplanationPanel = CreateExplanationPanel(
            "Rejected-record explanation (required when the checklist result is Pass)",
            rejectedRecordsExplanationTextBox);
        contentFlowLayoutPanel.Controls.Add(CreateGroup(
            "Database Statistics",
            CreateSection(
                CreateWrappingMetrics(
                    CreateMetric("Imported Record Count", importedRecordCountNumericUpDown),
                    CreateMetric("Rejected Record Count", rejectedRecordCountNumericUpDown),
                    CreateMetric("Missing required DB values", missingRequiredDatabaseValuesNumericUpDown),
                    CreateMetric("Signed raw minus imported", signedDifferenceTextBox),
                    CreateMetric("Absolute row difference", absoluteDifferenceTextBox)),
                CreatePrivacyReminderLabel(),
                rowDifferenceExplanationPanel,
                rejectedRecordsExplanationPanel)));

        readinessSummaryTextBox = new TextBox
        {
            AccessibleName = "Report Readiness Summary",
            BackColor = SystemColors.Window,
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 230),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            TabStop = false,
            Text = "Calculated Status: Not Ready\r\n\r\nUse Check Report Readiness after completing the report."
        };
        contentFlowLayoutPanel.Controls.Add(CreateGroup(
            "Report Readiness Summary",
            readinessSummaryTextBox));

        WireInputEvents();
        SizeChanged += (_, _) => ResizeContent();
        VisibleChanged += (_, _) => ResizeContent();
        FontChanged += (_, _) => ResizeContentAfterInheritedLayout();
        DpiChangedAfterParent += (_, _) => ResizeContentAfterInheritedLayout();
        contentFlowLayoutPanel.ClientSizeChanged += (_, _) => ResizeContent();
    }

    public event EventHandler? StatisticsChanged;

    public void Bind(QaReport report)
    {
        boundReport = report ?? throw new ArgumentNullException(nameof(report));
        calculationService.Synchronize(report);
        RefreshFromReport();
    }

    public void CommitCurrentValues()
    {
        QaReport report = GetBoundReport();
        QaStatistics statistics = report.Statistics;

        statistics.FileInformation.TotalDataRows = ToInt(totalDataRowsNumericUpDown);
        statistics.FileInformation.HeadersArePresent =
            headersPresentComboBox.SelectedIndex == 1;
        statistics.FileInformation.UsefulHeaders = usefulHeadersComboBox.SelectedIndex switch
        {
            1 => QaUsefulHeadersResult.Partially,
            2 => QaUsefulHeadersResult.No,
            _ => QaUsefulHeadersResult.Yes
        };
        statistics.FileInformation.DataStartRow = ToInt(dataStartRowNumericUpDown);

        foreach (QaBlankStatisticRowControl row in blankRowsById.Values)
        {
            row.CommitCurrentValues();
        }

        foreach (QaBrokenDataStatisticRowControl row in brokenRowsById.Values)
        {
            row.CommitCurrentValues();
        }

        statistics.MultiwordNames.MultiwordFirstNameCount =
            ToInt(multiwordFirstNameNumericUpDown);
        statistics.MultiwordNames.MultiwordLastNameCount =
            ToInt(multiwordLastNameNumericUpDown);

        statistics.FileMonth.ValidArrivalDateCount =
            ToInt(validArrivalDateNumericUpDown);
        statistics.FileMonth.ArrivalDatesWithinFileMonth =
            ToInt(withinFileMonthNumericUpDown);
        statistics.FileMonth.ArrivalDatesOutsideFileMonth =
            ToInt(outsideFileMonthNumericUpDown);

        statistics.UnusualAverageRateValues.HasUnusualValues =
            unusualAverageRateComboBox.SelectedIndex == 1;
        statistics.UnusualAverageRateValues.UnusualValueCount =
            ToInt(unusualAverageRateCountNumericUpDown);

        statistics.UnusualStayValues.HasUnusualValues =
            unusualStayValueComboBox.SelectedIndex == 1;
        statistics.UnusualStayValues.UnusualValueCount =
            ToInt(unusualStayValueCountNumericUpDown);
        statistics.HighStayValues.StayValuesAboveTenThousandCount =
            ToInt(highStayValueCountNumericUpDown);
        statistics.HighStayValues.AreHighValuesExpected =
            highStayValuesExpectedComboBox.SelectedIndex == 1;

        statistics.Database.ImportedRecordCount =
            ToInt(importedRecordCountNumericUpDown);
        statistics.Database.RejectedRecordCount =
            ToInt(rejectedRecordCountNumericUpDown);
        statistics.Database.RecordsWithMissingRequiredDatabaseValues =
            ToInt(missingRequiredDatabaseValuesNumericUpDown);

        _ = CommitExplanationValues(report, pendingOnly: false);

        calculationService.Synchronize(report);
        RefreshStatisticRowValues();
        RefreshCalculatedValues(report);
    }

    /// <summary>
    /// Commits all dirty free-text explanations and raises one statistics event
    /// after the completed batch.
    /// </summary>
    public bool CommitPendingTextEdits()
    {
        if (isRefreshing || boundReport is null)
        {
            return false;
        }

        QaReport report = GetBoundReport();
        bool changed = CommitPendingTextEditsCore(report);

        if (changed)
        {
            calculationService.Synchronize(report);
            RefreshCalculatedValues(report);
            StatisticsChanged?.Invoke(this, EventArgs.Empty);
        }

        return changed;
    }

    public void RefreshFromReport()
    {
        QaReport report = GetBoundReport();
        calculationService.Synchronize(report);

        bool wasRefreshing = isRefreshing;
        isRefreshing = true;

        try
        {
            QaStatistics statistics = report.Statistics;
            SetCount(totalDataRowsNumericUpDown, statistics.FileInformation.TotalDataRows);
            headersPresentComboBox.SelectedIndex =
                statistics.FileInformation.HeadersArePresent ? 1 : 0;
            usefulHeadersComboBox.SelectedIndex = statistics.FileInformation.UsefulHeaders switch
            {
                QaUsefulHeadersResult.Partially => 1,
                QaUsefulHeadersResult.No => 2,
                _ => 0
            };
            SetCount(dataStartRowNumericUpDown, statistics.FileInformation.DataStartRow);

            ReconcileBlankRows(statistics.BlankValues);
            ReconcileBrokenRows(statistics.BrokenData);

            SetCount(
                multiwordFirstNameNumericUpDown,
                statistics.MultiwordNames.MultiwordFirstNameCount);
            SetCount(
                multiwordLastNameNumericUpDown,
                statistics.MultiwordNames.MultiwordLastNameCount);
            SetCount(validArrivalDateNumericUpDown, statistics.FileMonth.ValidArrivalDateCount);
            SetCount(withinFileMonthNumericUpDown, statistics.FileMonth.ArrivalDatesWithinFileMonth);
            SetCount(outsideFileMonthNumericUpDown, statistics.FileMonth.ArrivalDatesOutsideFileMonth);

            unusualAverageRateComboBox.SelectedIndex =
                statistics.UnusualAverageRateValues.HasUnusualValues ? 1 : 0;
            SetCount(
                unusualAverageRateCountNumericUpDown,
                statistics.UnusualAverageRateValues.UnusualValueCount);
            SetOptionalTextUnlessPending(
                unusualAverageRateExplanationTextBox,
                statistics.UnusualAverageRateValues.Explanation);
            unusualStayValueComboBox.SelectedIndex =
                statistics.UnusualStayValues.HasUnusualValues ? 1 : 0;
            SetCount(
                unusualStayValueCountNumericUpDown,
                statistics.UnusualStayValues.UnusualValueCount);
            SetOptionalTextUnlessPending(
                unusualStayValueExplanationTextBox,
                statistics.UnusualStayValues.Explanation);
            SetCount(
                highStayValueCountNumericUpDown,
                statistics.HighStayValues.StayValuesAboveTenThousandCount);
            highStayValuesExpectedComboBox.SelectedIndex =
                statistics.HighStayValues.AreHighValuesExpected ? 1 : 0;
            SetOptionalTextUnlessPending(
                highStayValueExplanationTextBox,
                statistics.HighStayValues.Explanation);

            SetCount(importedRecordCountNumericUpDown, statistics.Database.ImportedRecordCount);
            SetCount(rejectedRecordCountNumericUpDown, statistics.Database.RejectedRecordCount);
            SetCount(
                missingRequiredDatabaseValuesNumericUpDown,
                statistics.Database.RecordsWithMissingRequiredDatabaseValues);

            QaFinding? rowDifferenceFinding = report.Findings.FirstOrDefault(
                finding => finding.FindingId.Equals(
                    QaFindingIds.RowDifferenceStatisticWarning,
                    StringComparison.Ordinal));
            SetOptionalTextUnlessPending(
                rowDifferenceExplanationTextBox,
                rowDifferenceFinding?.ResolutionNotes);

            QaCheckResult? rejectedResult = report.ChecklistResults.FirstOrDefault(
                result => result.CheckId.Equals(
                    QaChecklistIds.Database.RejectedRecordsAccountedFor,
                    StringComparison.Ordinal));
            SetOptionalTextUnlessPending(
                rejectedRecordsExplanationTextBox,
                statistics.Database.RejectedRecordCount > 0
                    ? rejectedResult?.Notes
                    : null);

            RefreshCalculatedValues(report);
            ResizeContent();
        }
        finally
        {
            isRefreshing = wasRefreshing;
        }
    }

    public void ShowReadinessResult(QaReportValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        StringBuilder summary = new();
        AppendListSection(summary, "Blocking Errors", result.BlockingErrors);
        summary.AppendLine();
        AppendListSection(summary, "Nonblocking Workflow Warnings", result.WorkflowWarnings);
        summary.AppendLine();
        summary.AppendLine($"Warning finding count: {result.WarningFindingCount}");
        summary.AppendLine($"Failure / Failed Checks count: {result.FailureFindingCount}");
        summary.AppendLine($"Handled finding count: {result.HandledFindingCount}");
        summary.AppendLine($"Calculated Status: {FormatStatus(result.CalculatedStatus)}");
        summary.AppendLine($"Effective Created By: {result.EffectiveCreatedBy}");
        readinessSummaryTextBox.Text = summary.ToString().TrimEnd();
        ScrollToReadinessSummary();
    }

    public void MarkReadinessStale()
    {
        readinessSummaryTextBox.Text =
            "Calculated Status: Not Ready \u2014 report changed; validate again\r\n\r\n" +
            "The previous readiness result is stale. Use Check Report Readiness after completing edits.";
    }

    public void ScrollToReadinessSummary()
    {
        Control target = readinessSummaryTextBox.Parent ?? readinessSummaryTextBox;
        contentFlowLayoutPanel.ScrollControlIntoView(target);
        readinessSummaryTextBox.Focus();
    }

    private void WireInputEvents()
    {
        Control[] inputs =
        [
            totalDataRowsNumericUpDown,
            headersPresentComboBox,
            usefulHeadersComboBox,
            dataStartRowNumericUpDown,
            multiwordFirstNameNumericUpDown,
            multiwordLastNameNumericUpDown,
            validArrivalDateNumericUpDown,
            withinFileMonthNumericUpDown,
            outsideFileMonthNumericUpDown,
            unusualAverageRateComboBox,
            unusualAverageRateCountNumericUpDown,
            unusualAverageRateExplanationTextBox,
            unusualStayValueComboBox,
            unusualStayValueCountNumericUpDown,
            unusualStayValueExplanationTextBox,
            highStayValueCountNumericUpDown,
            highStayValuesExpectedComboBox,
            highStayValueExplanationTextBox,
            importedRecordCountNumericUpDown,
            rejectedRecordCountNumericUpDown,
            missingRequiredDatabaseValuesNumericUpDown,
            rowDifferenceExplanationTextBox,
            rejectedRecordsExplanationTextBox
        ];

        foreach (Control input in inputs)
        {
            switch (input)
            {
                case NumericUpDown numeric:
                    numeric.ValueChanged += (_, _) => ApplyUserChanges();
                    break;
                case ComboBox combo:
                    combo.SelectedIndexChanged += (_, _) => ApplyUserChanges();
                    break;
                case TextBox text:
                    text.TextChanged += (_, _) =>
                        MarkExplanationEditPending(text);
                    text.Validated += (_, _) => CommitPendingTextEdits();
                    break;
            }
        }
    }

    private void ApplyUserChanges()
    {
        if (isRefreshing || boundReport is null)
        {
            return;
        }

        _ = CommitPendingTextEditsCore(GetBoundReport());
        CommitCurrentValues();
        StatisticsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void MarkExplanationEditPending(TextBox textBox)
    {
        if (!isRefreshing && boundReport is not null)
        {
            pendingExplanationTextBoxes.Add(textBox);
        }
    }

    private bool CommitPendingTextEditsCore(QaReport report)
    {
        bool changed = CommitExplanationValues(report, pendingOnly: true);

        foreach (QaBrokenDataStatisticRowControl row in brokenRowsById.Values)
        {
            changed |= row.CommitPendingTextEditsWithoutNotification();
        }

        return changed;
    }

    private bool CommitExplanationValues(QaReport report, bool pendingOnly)
    {
        bool changed = false;
        QaStatistics statistics = report.Statistics;

        changed |= CommitOptionalText(
            unusualAverageRateExplanationTextBox,
            statistics.UnusualAverageRateValues.Explanation,
            value => statistics.UnusualAverageRateValues.Explanation = value,
            pendingOnly);
        changed |= CommitOptionalText(
            unusualStayValueExplanationTextBox,
            statistics.UnusualStayValues.Explanation,
            value => statistics.UnusualStayValues.Explanation = value,
            pendingOnly);
        changed |= CommitOptionalText(
            highStayValueExplanationTextBox,
            statistics.HighStayValues.Explanation,
            value => statistics.HighStayValues.Explanation = value,
            pendingOnly);

        QaFinding? rowDifferenceFinding = report.Findings.FirstOrDefault(
            finding => finding.FindingId.Equals(
                QaFindingIds.RowDifferenceStatisticWarning,
                StringComparison.Ordinal));
        if (rowDifferenceFinding is not null)
        {
            changed |= CommitOptionalText(
                rowDifferenceExplanationTextBox,
                rowDifferenceFinding.ResolutionNotes,
                value => rowDifferenceFinding.ResolutionNotes = value,
                pendingOnly);
        }
        else if (!pendingOnly)
        {
            pendingExplanationTextBoxes.Remove(rowDifferenceExplanationTextBox);
        }

        QaCheckResult? rejectedResult = report.ChecklistResults.FirstOrDefault(
            result => result.CheckId.Equals(
                QaChecklistIds.Database.RejectedRecordsAccountedFor,
                StringComparison.Ordinal));
        if (statistics.Database.RejectedRecordCount > 0 && rejectedResult is not null)
        {
            changed |= CommitOptionalText(
                rejectedRecordsExplanationTextBox,
                rejectedResult.Notes,
                value => rejectedResult.Notes = value,
                pendingOnly);
        }
        else if (!pendingOnly)
        {
            pendingExplanationTextBoxes.Remove(rejectedRecordsExplanationTextBox);
        }

        return changed;
    }

    private bool CommitOptionalText(
        TextBox control,
        string? modelValue,
        Action<string?> applyValue,
        bool pendingOnly)
    {
        if (pendingOnly && !pendingExplanationTextBoxes.Contains(control))
        {
            return false;
        }

        string? normalizedValue = TrimToNull(control.Text);
        bool changed = !string.Equals(
            modelValue,
            normalizedValue,
            StringComparison.Ordinal);

        if (changed)
        {
            applyValue(normalizedValue);
        }

        pendingExplanationTextBoxes.Remove(control);
        return changed;
    }

    private void ReconcileBlankRows(IEnumerable<QaBlankValueStatistic> statistics)
    {
        QaBlankValueStatistic[] snapshot = statistics.ToArray();
        HashSet<string> currentIds = snapshot
            .Select(statistic => statistic.FieldId)
            .ToHashSet(StringComparer.Ordinal);

        foreach ((string id, QaBlankStatisticRowControl row) in blankRowsById.ToArray())
        {
            if (currentIds.Contains(id))
            {
                continue;
            }

            row.StatisticChanged -= StatisticRow_StatisticChanged;
            blankRowsFlowLayoutPanel.Controls.Remove(row);
            row.Dispose();
            blankRowsById.Remove(id);
        }

        for (int index = 0; index < snapshot.Length; index++)
        {
            QaBlankValueStatistic statistic = snapshot[index];
            if (!blankRowsById.TryGetValue(
                    statistic.FieldId,
                    out QaBlankStatisticRowControl? row))
            {
                row = new QaBlankStatisticRowControl();
                row.StatisticChanged += StatisticRow_StatisticChanged;
                blankRowsById.Add(statistic.FieldId, row);
                blankRowsFlowLayoutPanel.Controls.Add(row);
            }

            row.Bind(statistic);
            blankRowsFlowLayoutPanel.Controls.SetChildIndex(row, index);
        }
    }

    private void ReconcileBrokenRows(IEnumerable<QaBrokenDataStatistic> statistics)
    {
        QaBrokenDataStatistic[] snapshot = statistics.ToArray();
        HashSet<string> currentIds = snapshot
            .Select(statistic => statistic.FieldId)
            .ToHashSet(StringComparer.Ordinal);

        foreach ((string id, QaBrokenDataStatisticRowControl row) in brokenRowsById.ToArray())
        {
            if (currentIds.Contains(id))
            {
                continue;
            }

            row.StatisticChanged -= StatisticRow_StatisticChanged;
            row.DiscardPendingTextEdits();
            brokenRowsFlowLayoutPanel.Controls.Remove(row);
            row.Dispose();
            brokenRowsById.Remove(id);
        }

        for (int index = 0; index < snapshot.Length; index++)
        {
            QaBrokenDataStatistic statistic = snapshot[index];
            if (!brokenRowsById.TryGetValue(
                    statistic.FieldId,
                    out QaBrokenDataStatisticRowControl? row))
            {
                row = new QaBrokenDataStatisticRowControl();
                row.StatisticChanged += StatisticRow_StatisticChanged;
                brokenRowsById.Add(statistic.FieldId, row);
                brokenRowsFlowLayoutPanel.Controls.Add(row);
            }

            row.Bind(statistic);
            brokenRowsFlowLayoutPanel.Controls.SetChildIndex(row, index);
        }
    }

    private void StatisticRow_StatisticChanged(object? sender, EventArgs eventArgs)
    {
        ApplyUserChanges();
    }

    private void RefreshStatisticRowValues()
    {
        foreach (QaBlankStatisticRowControl row in blankRowsById.Values)
        {
            row.RefreshFromStatistic();
        }

        foreach (QaBrokenDataStatisticRowControl row in brokenRowsById.Values)
        {
            row.RefreshFromStatistic();
        }
    }

    private void RefreshCalculatedValues(QaReport report)
    {
        QaStatistics statistics = report.Statistics;
        int firstDenominator = GetDerivedDenominator(
            statistics,
            QaStatisticFieldIds.FirstName);
        int lastDenominator = GetDerivedDenominator(
            statistics,
            QaStatisticFieldIds.LastName);
        int averageRateDenominator = GetDerivedDenominator(
            statistics,
            QaStatisticFieldIds.AverageRate);
        int stayValueDenominator = GetDerivedDenominator(
            statistics,
            QaStatisticFieldIds.StayValue);

        firstNameDenominatorTextBox.Text = FormatCount(firstDenominator);
        lastNameDenominatorTextBox.Text = FormatCount(lastDenominator);
        multiwordFirstNamePercentageTextBox.Text = FormatPercentage(
            statistics.MultiwordNames.MultiwordFirstNamePercentage);
        multiwordLastNamePercentageTextBox.Text = FormatPercentage(
            statistics.MultiwordNames.MultiwordLastNamePercentage);
        withinFileMonthPercentageTextBox.Text = FormatPercentage(
            statistics.FileMonth.PercentageWithinFileMonth);
        outsideFileMonthPercentageTextBox.Text = FormatPercentage(
            statistics.FileMonth.PercentageOutsideFileMonth);
        averageRateDenominatorTextBox.Text = FormatCount(averageRateDenominator);
        unusualAverageRatePercentageTextBox.Text = FormatPercentage(
            statistics.UnusualAverageRateValues.UnusualValuePercentage);
        stayValueDenominatorTextBox.Text = FormatCount(stayValueDenominator);
        highStayValueDenominatorTextBox.Text = FormatCount(stayValueDenominator);
        unusualStayValuePercentageTextBox.Text = FormatPercentage(
            statistics.UnusualStayValues.UnusualValuePercentage);
        highStayValuePercentageTextBox.Text = FormatPercentage(
            statistics.HighStayValues.StayValuesAboveTenThousandPercentage);
        signedDifferenceTextBox.Text = statistics.Database
            .RawMinusImportedRecordCountDifference
            .ToString(CultureInfo.InvariantCulture);
        absoluteDifferenceTextBox.Text =
            QaStatisticsCalculationService.GetAbsoluteDifference(
                    statistics.Database.RawMinusImportedRecordCountDifference)
                .ToString(CultureInfo.InvariantCulture);

        bool separateNames = report.FileCharacteristics.NameColumnMode ==
            QaNameColumnMode.SeparateFirstAndLastName;
        bool stayValueApplies = QaStatisticFieldCatalog.IsApplicable(
            QaStatisticFieldCatalog.GetRequired(QaStatisticFieldIds.StayValue),
            report.FileCharacteristics);
        nameStatisticsGroupBox.Visible = separateNames;
        stayValueGroupBox.Visible = stayValueApplies;
        unusualAverageRateCountNumericUpDown.Enabled =
            statistics.UnusualAverageRateValues.HasUnusualValues;
        unusualAverageRateExplanationTextBox.Enabled =
            statistics.UnusualAverageRateValues.HasUnusualValues;
        unusualStayValueCountNumericUpDown.Enabled =
            stayValueApplies && statistics.UnusualStayValues.HasUnusualValues;
        unusualStayValueExplanationTextBox.Enabled =
            stayValueApplies && statistics.UnusualStayValues.HasUnusualValues;
        highStayValueExplanationTextBox.Enabled = stayValueApplies
            && statistics.HighStayValues.StayValuesAboveTenThousandCount > 0
            && !statistics.HighStayValues.AreHighValuesExpected;
        rowDifferenceExplanationPanel.Visible =
            QaStatisticsCalculationService.GetAbsoluteDifference(
                statistics.Database.RawMinusImportedRecordCountDifference) >= 10;
        rejectedRecordsExplanationPanel.Visible =
            statistics.Database.RejectedRecordCount > 0;
    }

    private void ResizeContent()
    {
        if (isResizingContent || contentFlowLayoutPanel.ClientSize.Width <= 0)
        {
            return;
        }

        isResizingContent = true;

        try
        {
            // A vertical scrollbar can change the live display width during this
            // layout. A bounded second/third pass consumes that change without
            // allowing Layout events raised below to recurse.
            for (int pass = 0; pass < 3; pass++)
            {
                contentFlowLayoutPanel.PerformLayout();
                int width = GetStatisticsViewportWidth();
                if (width <= 0)
                {
                    return;
                }

                foreach (GroupBox groupBox in contentFlowLayoutPanel.Controls
                             .OfType<GroupBox>())
                {
                    groupBox.Width = Math.Max(
                        groupBox.MinimumSize.Width,
                        width - groupBox.Margin.Horizontal);
                    groupBox.PerformLayout();
                    ResizeNestedGroupWidths(groupBox);
                }

                ResizeStatisticRows();
                ResizePrivacyReminders();
                ResizeGroupsToContent(contentFlowLayoutPanel);
                contentFlowLayoutPanel.PerformLayout();

                if (GetStatisticsViewportWidth() == width)
                {
                    break;
                }
            }
        }
        finally
        {
            isResizingContent = false;
        }
    }

    private void ResizeContentAfterInheritedLayout()
    {
        ResizeContent();

        if (!IsHandleCreated || IsDisposed || Disposing || isResizeContentQueued)
        {
            return;
        }

        isResizeContentQueued = true;
        BeginInvoke((Action)(() =>
        {
            isResizeContentQueued = false;
            if (!IsDisposed && !Disposing)
            {
                ResizeContent();
            }
        }));
    }

    private int GetStatisticsViewportWidth()
    {
        int clientDisplayWidth = Math.Max(
            0,
            contentFlowLayoutPanel.ClientRectangle.Width
                - contentFlowLayoutPanel.Padding.Horizontal);
        int displayWidth = Math.Max(0, contentFlowLayoutPanel.DisplayRectangle.Width);

        return displayWidth == 0
            ? clientDisplayWidth
            : Math.Min(clientDisplayWidth, displayWidth);
    }

    private static void ResizeNestedGroupWidths(Control container)
    {
        foreach (Control child in container.Controls)
        {
            if (child is GroupBox groupBox)
            {
                int availableWidth = Math.Max(
                    0,
                    container.DisplayRectangle.Width - groupBox.Margin.Horizontal);
                if (availableWidth > 0)
                {
                    groupBox.Width = Math.Max(
                        groupBox.MinimumSize.Width,
                        availableWidth);
                    groupBox.PerformLayout();
                }
            }

            ResizeNestedGroupWidths(child);
        }
    }

    private void ResizeStatisticRows()
    {
        ResizeStatisticRows(blankRowsFlowLayoutPanel, blankRowsById.Values);
        ResizeStatisticRows(brokenRowsFlowLayoutPanel, brokenRowsById.Values);
    }

    private static void ResizeStatisticRows<T>(
        FlowLayoutPanel parent,
        IEnumerable<T> rows)
        where T : Control
    {
        parent.PerformLayout();

        foreach (T row in rows)
        {
            int availableWidth = Math.Max(
                row.MinimumSize.Width,
                parent.DisplayRectangle.Width - row.Margin.Horizontal);
            row.Width = availableWidth;
        }
    }

    private void ResizePrivacyReminders()
    {
        foreach (Label reminder in privacyReminderLabels)
        {
            Control? parent = reminder.Parent;
            if (parent is null)
            {
                continue;
            }

            int availableWidth = Math.Max(
                0,
                parent.DisplayRectangle.Width - reminder.Margin.Horizontal);
            if (availableWidth > 0)
            {
                reminder.MaximumSize = new Size(availableWidth, 0);
            }
        }
    }

    private static void ResizeGroupsToContent(Control container)
    {
        foreach (Control child in container.Controls)
        {
            ResizeGroupsToContent(child);
        }

        if (container is not GroupBox groupBox || groupBox.Controls.Count == 0)
        {
            return;
        }

        groupBox.PerformLayout();
        int contentWidth = groupBox.DisplayRectangle.Width;
        if (contentWidth <= 0)
        {
            return;
        }

        int requiredBottom = groupBox.DisplayRectangle.Top;
        foreach (Control content in groupBox.Controls)
        {
            content.Width = Math.Max(
                content.MinimumSize.Width,
                contentWidth - content.Margin.Horizontal);
            content.PerformLayout();

            Size preferredSize = content.GetPreferredSize(
                new Size(content.Width, 0));
            content.Height = Math.Max(
                content.MinimumSize.Height,
                preferredSize.Height);
            content.PerformLayout();
            requiredBottom = Math.Max(requiredBottom, content.Bottom);
        }

        groupBox.Height = Math.Max(
            groupBox.MinimumSize.Height,
            requiredBottom + groupBox.Padding.Bottom);
    }

    private QaReport GetBoundReport()
    {
        return boundReport
            ?? throw new InvalidOperationException(
                "Bind must be called before using statistics controls.");
    }

    private static int GetDerivedDenominator(QaStatistics statistics, string fieldId)
    {
        QaBlankValueStatistic? blank = statistics.BlankValues.FirstOrDefault(
            statistic => statistic.FieldId.Equals(fieldId, StringComparison.Ordinal));
        return blank is null
            ? 0
            : Math.Max(
                0,
                QaStatisticsCalculationService.CalculateDerivedNonblankCount(blank));
    }

    private static NumericUpDown CreateCountInput(string name, string accessibleName)
    {
        return new NumericUpDown
        {
            AccessibleName = accessibleName,
            Maximum = int.MaxValue,
            Minimum = 0,
            Name = name,
            Size = new Size(132, 23),
            ThousandsSeparator = true
        };
    }

    private static ComboBox CreateChoice(
        string name,
        string accessibleName,
        params string[] choices)
    {
        ComboBox comboBox = new()
        {
            AccessibleName = accessibleName,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Name = name,
            Size = new Size(155, 23)
        };
        comboBox.Items.AddRange(choices);
        comboBox.SelectedIndex = 0;
        return comboBox;
    }

    private static TextBox CreateReadOnlyValue(string accessibleName)
    {
        return new TextBox
        {
            AccessibleName = accessibleName,
            ReadOnly = true,
            Size = new Size(132, 23),
            TabStop = false,
            Text = "0"
        };
    }

    private static TextBox CreateExplanation(string name, string accessibleName)
    {
        return new TextBox
        {
            AcceptsReturn = true,
            AccessibleDescription = PrivacyReminder,
            AccessibleName = accessibleName,
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 62),
            Multiline = true,
            Name = name,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true
        };
    }

    private Label CreatePrivacyReminderLabel()
    {
        Label label = new()
        {
            AutoSize = true,
            ForeColor = SystemColors.ControlDarkDark,
            Margin = new Padding(6, 4, 6, 10),
            MaximumSize = new Size(520, 0),
            Padding = new Padding(4),
            Text = PrivacyReminder
        };

        privacyReminderLabels.Add(label);
        return label;
    }

    private static FlowLayoutPanel CreateWrappingMetrics(params Control[] controls)
    {
        FlowLayoutPanel panel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 2),
            WrapContents = true
        };
        panel.Controls.AddRange(controls);
        return panel;
    }

    private static FlowLayoutPanel CreateVerticalFlow()
    {
        return new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
    }

    private static TableLayoutPanel CreateMetric(string labelText, Control valueControl)
    {
        TableLayoutPanel panel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Margin = new Padding(3, 2, 18, 8),
            RowCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 3),
            Text = labelText
        }, 0, 0);
        panel.Controls.Add(valueControl, 0, 1);
        return panel;
    }

    private static TableLayoutPanel CreateExplanationPanel(
        string labelText,
        TextBox textBox)
    {
        TableLayoutPanel panel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Margin = new Padding(3, 5, 3, 8),
            RowCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4),
            Text = labelText
        }, 0, 0);
        panel.Controls.Add(textBox, 0, 1);
        return panel;
    }

    private static TableLayoutPanel CreateSection(params Control[] controls)
    {
        TableLayoutPanel panel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            RowCount = controls.Length
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (int index = 0; index < controls.Length; index++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(controls[index], 0, index);
        }

        return panel;
    }

    private static GroupBox CreateGroup(string text, Control content)
    {
        GroupBox groupBox = new()
        {
            AutoSize = false,
            Margin = new Padding(0, 0, 0, 10),
            MinimumSize = new Size(560, 0),
            Padding = new Padding(10),
            Text = text
        };
        content.Dock = DockStyle.Top;
        groupBox.Controls.Add(content);
        return groupBox;
    }

    private static void AppendListSection(
        StringBuilder builder,
        string heading,
        IReadOnlyList<string> items)
    {
        builder.AppendLine($"{heading} ({items.Count})");

        if (items.Count == 0)
        {
            builder.AppendLine("  None");
            return;
        }

        foreach (string item in items)
        {
            builder.AppendLine($"  - {item}");
        }
    }

    private static void SetCount(NumericUpDown control, int value)
    {
        decimal safeValue = Math.Clamp(value, 0, int.MaxValue);
        if (control.Value != safeValue)
        {
            control.Value = safeValue;
        }
    }

    private static int ToInt(NumericUpDown control)
    {
        return decimal.ToInt32(control.Value);
    }

    private static void SetOptionalText(TextBox control, string? modelValue)
    {
        if (!string.Equals(
                TrimToNull(control.Text),
                TrimToNull(modelValue),
                StringComparison.Ordinal))
        {
            control.Text = modelValue ?? string.Empty;
        }
    }

    private void SetOptionalTextUnlessPending(TextBox control, string? modelValue)
    {
        if (!pendingExplanationTextBoxes.Contains(control))
        {
            SetOptionalText(control, modelValue);
        }
    }

    private static string FormatCount(int value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }

    private static string FormatPercentage(decimal value)
    {
        return $"{value:0.00}%";
    }

    private static string FormatStatus(QaReportStatus? status)
    {
        return status switch
        {
            QaReportStatus.Pass => "Pass",
            QaReportStatus.PassWithWarnings => "Pass with Warnings",
            QaReportStatus.Fail => "Fail",
            null => "Not Ready",
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown QA report status value.")
        };
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
