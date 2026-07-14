using System.Diagnostics;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Forms.Controls;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.QAReports.Forms;

/// <summary>
/// Owns and synchronizes one in-memory Phase 6 QA report draft.
/// </summary>
public partial class QaReportForm : Form
{
    private const int ExpectedChecklistDefinitionCount = 28;

    private readonly QaMetadataService metadataService;
    private readonly IReadOnlyList<QaCheckDefinition> checklistDefinitions;
    private readonly IReadOnlyDictionary<string, QaCheckDefinition> checklistDefinitionsById;
    private readonly Dictionary<string, QaCheckResult> checklistResultsById;
    private readonly Dictionary<string, QaChecklistItemControl> checklistControlsById =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, QaFindingItemControl> findingControlsById =
        new(StringComparer.Ordinal);
    private readonly QaFindingSynchronizationService findingSynchronizationService;

    private IReadOnlyList<QaPmsMetadata> pmsSystems;
    private IReadOnlyList<QaHotelMetadata> hotels;
    private bool isSynchronizingFindings;

    public QaReportForm(
        QaMetadataService metadataService,
        IReadOnlyList<QaPmsMetadata> pmsSystems,
        IReadOnlyList<QaHotelMetadata> hotels)
    {
        this.metadataService = metadataService
            ?? throw new ArgumentNullException(nameof(metadataService));
        this.pmsSystems = CreatePmsSnapshot(pmsSystems);
        this.hotels = CreateHotelSnapshot(hotels);
        checklistDefinitions = QaChecklistCatalog.Definitions.ToArray();
        CurrentReport = CreateCurrentReport(
            checklistDefinitions,
            out checklistResultsById);
        checklistDefinitionsById = checklistDefinitions.ToDictionary(
            definition => definition.Id,
            StringComparer.Ordinal);
        findingSynchronizationService = new QaFindingSynchronizationService(
            checklistDefinitions);

        InitializeComponent();
        InitializeReportInputs();
        BuildChecklistControls();
        WireEvents();

        hotelSelectorControl.SetHotels(this.hotels);
        SynchronizeHotelSelection();
        SynchronizeReportInputs();
        SynchronizeFileCharacteristics();
    }

    /// <summary>
    /// Gets the single report draft updated directly by this form's controls.
    /// Closing the form does not persist this object.
    /// </summary>
    public QaReport CurrentReport { get; }

    private static IReadOnlyList<QaPmsMetadata> CreatePmsSnapshot(
        IReadOnlyList<QaPmsMetadata> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        QaPmsMetadata[] snapshot = source.ToArray();

        if (snapshot.Any(item => item is null))
        {
            throw new ArgumentException(
                "The PMS metadata snapshot cannot contain a null record.",
                nameof(source));
        }

        return snapshot;
    }

    private static IReadOnlyList<QaHotelMetadata> CreateHotelSnapshot(
        IReadOnlyList<QaHotelMetadata> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        QaHotelMetadata[] snapshot = source.ToArray();

        if (snapshot.Any(item => item is null))
        {
            throw new ArgumentException(
                "The hotel metadata snapshot cannot contain a null record.",
                nameof(source));
        }

        return snapshot;
    }

    private static QaReport CreateCurrentReport(
        IReadOnlyList<QaCheckDefinition> definitions,
        out Dictionary<string, QaCheckResult> resultsById)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Count != ExpectedChecklistDefinitionCount)
        {
            throw new InvalidOperationException(
                $"The QA checklist catalog must contain exactly {ExpectedChecklistDefinitionCount} definitions for the QA report form.");
        }

        HashSet<string> definitionIds = new(StringComparer.Ordinal);

        foreach (QaCheckDefinition? definition in definitions)
        {
            if (definition is null)
            {
                throw new InvalidOperationException(
                    "The QA checklist catalog cannot contain a null definition.");
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new InvalidOperationException(
                    "Every QA checklist definition must have a nonblank stable ID.");
            }

            if (!definitionIds.Add(definition.Id))
            {
                throw new InvalidOperationException(
                    $"The QA checklist catalog contains the duplicate stable ID '{definition.Id}'.");
            }
        }

        QaCheckResult[] results = definitions
            .Select(definition => new QaCheckResult
            {
                CheckId = definition.Id,
                Status = QaCheckStatus.NotEvaluated,
                ResultSource = QaResultSource.Manual
            })
            .ToArray();

        HashSet<string> resultIds = new(StringComparer.Ordinal);

        foreach (QaCheckResult result in results)
        {
            if (string.IsNullOrWhiteSpace(result.CheckId)
                || !resultIds.Add(result.CheckId))
            {
                throw new InvalidOperationException(
                    "Every QA checklist result must have one unique, nonblank stable ID.");
            }
        }

        if (results.Length != definitions.Count
            || !definitionIds.SetEquals(resultIds))
        {
            throw new InvalidOperationException(
                "The QA checklist result collection must contain exactly one result for every catalog definition.");
        }

        resultsById = results.ToDictionary(
            result => result.CheckId,
            StringComparer.Ordinal);

        QaReport report = new();
        report.ChecklistResults.AddRange(results);

        if (report.ChecklistResults.Count != definitions.Count)
        {
            throw new InvalidOperationException(
                "The QA report draft did not retain every initialized checklist result.");
        }

        return report;
    }

    private void InitializeReportInputs()
    {
        DateTime today = DateTime.Today;
        fileMonthPicker.Value = new DateTime(today.Year, today.Month, 1);
        qaDatePicker.Value = today;
    }

    private void WireEvents()
    {
        hotelSelectorControl.SelectedHotelChanged += (_, _) =>
            SynchronizeHotelSelection();
        manageHotelsPmsButton.Click += (_, _) => OpenMetadataManagement();

        fileMonthPicker.ValueChanged += (_, _) => SynchronizeFileMonth();
        qaDatePicker.ValueChanged += (_, _) => SynchronizeQaDate();
        createdByTextBox.TextChanged += (_, _) =>
            CurrentReport.CreatedBy = TrimToNull(createdByTextBox.Text);
        originalFileNameTextBox.TextChanged += (_, _) =>
            CurrentReport.OriginalFileName = TrimToNull(originalFileNameTextBox.Text);
        generalNotesTextBox.TextChanged += (_, _) =>
            CurrentReport.GeneralNotes = TrimToNull(generalNotesTextBox.Text);

        separateNameColumnsRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(separateNameColumnsRadioButton);
        fullNameColumnRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(fullNameColumnRadioButton);
        currencyColumnFoundRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(currencyColumnFoundRadioButton);
        noCurrencyColumnRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(noCurrencyColumnRadioButton);
        oneMonetaryColumnRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(oneMonetaryColumnRadioButton);
        twoMonetaryColumnsRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(twoMonetaryColumnsRadioButton);
        moreThanTwoMonetaryColumnsRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(moreThanTwoMonetaryColumnsRadioButton);
        multipleConfirmationCandidatesRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(multipleConfirmationCandidatesRadioButton);
        noMultipleConfirmationCandidatesRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(noMultipleConfirmationCandidatesRadioButton);
        customScriptAvailableRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(customScriptAvailableRadioButton);
        noCustomScriptAvailableRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(noCustomScriptAvailableRadioButton);
        rejectedRecordsExistRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(rejectedRecordsExistRadioButton);
        noRejectedRecordsRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCharacteristicsWhenChecked(noRejectedRecordsRadioButton);

        rawChecklistFlowLayoutPanel.SizeChanged += (_, _) =>
            ResizeChecklistControls(rawChecklistFlowLayoutPanel);
        databaseChecklistFlowLayoutPanel.SizeChanged += (_, _) =>
            ResizeChecklistControls(databaseChecklistFlowLayoutPanel);
        warningsFlowLayoutPanel.SizeChanged += (_, _) =>
            ResizeFindingControls(warningsFlowLayoutPanel);
        failedChecksFlowLayoutPanel.SizeChanged += (_, _) =>
            ResizeFindingControls(failedChecksFlowLayoutPanel);
    }

    private void SynchronizeReportInputs()
    {
        SynchronizeFileMonth();
        SynchronizeQaDate();
        CurrentReport.CreatedBy = TrimToNull(createdByTextBox.Text);
        CurrentReport.OriginalFileName = TrimToNull(originalFileNameTextBox.Text);
        CurrentReport.GeneralNotes = TrimToNull(generalNotesTextBox.Text);
    }

    private void SynchronizeFileMonth()
    {
        CurrentReport.HotelInformation.FileMonth = new QaFileMonth(
            fileMonthPicker.Value.Year,
            fileMonthPicker.Value.Month);
    }

    private void SynchronizeQaDate()
    {
        CurrentReport.QaDate = DateOnly.FromDateTime(qaDatePicker.Value);
    }

    private void SynchronizeHotelSelection()
    {
        QaHotelMetadata? selectedHotel = hotelSelectorControl.SelectedHotel;
        QaHotelInformation hotelInformation = CurrentReport.HotelInformation;

        if (selectedHotel is null)
        {
            hotelInformation.HotelId = string.Empty;
            hotelInformation.HotelName = string.Empty;
            hotelInformation.PmsName = string.Empty;
            selectedHotelIdTextBox.Clear();
            selectedPmsTextBox.Clear();
            return;
        }

        hotelInformation.HotelId = selectedHotel.HotelId;
        hotelInformation.HotelName = selectedHotel.HotelName;
        hotelInformation.PmsName = selectedHotel.PmsName;
        selectedHotelIdTextBox.Text = selectedHotel.HotelId;
        selectedPmsTextBox.Text = selectedHotel.PmsName;
    }

    private void OpenMetadataManagement()
    {
        try
        {
            using QaMetadataManagementForm form = new(
                metadataService,
                pmsSystems,
                hotels);

            form.ShowDialog(this);

            string? preferredHotelId = hotelSelectorControl.SelectedHotel?.HotelId;
            IReadOnlyList<QaPmsMetadata> refreshedPmsSystems =
                CreatePmsSnapshot(metadataService.LoadPmsSystems());
            IReadOnlyList<QaHotelMetadata> refreshedHotels =
                CreateHotelSnapshot(metadataService.LoadHotels());

            pmsSystems = refreshedPmsSystems;
            hotels = refreshedHotels;
            hotelSelectorControl.SetHotels(hotels, preferredHotelId);
            SynchronizeHotelSelection();
        }
        catch (QaUnsupportedMetadataSchemaException exception)
        {
            ShowMetadataRefreshError(
                exception,
                "QA metadata uses an unsupported version. The existing metadata files were not changed.");
        }
        catch (QaMetadataException exception)
        {
            ShowMetadataRefreshError(
                exception,
                "QA metadata could not be refreshed because a metadata file is invalid or inaccessible. The existing file was not changed.");
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException
                or InvalidOperationException)
        {
            ShowMetadataRefreshError(
                exception,
                "QA metadata could not be refreshed because the configured documentation folder could not be used.");
        }
        catch (Exception exception)
        {
            ShowMetadataRefreshError(
                exception,
                "QA metadata management could not be opened or refreshed.");
        }
    }

    private void ShowMetadataRefreshError(Exception exception, string message)
    {
        Debug.WriteLine(exception);
        MessageBox.Show(
            this,
            message + " The previous metadata view and valid selection are still shown.",
            "QA Metadata Refresh Failed",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void BuildChecklistControls()
    {
        foreach (QaCheckDefinition definition in checklistDefinitions)
        {
            if (!checklistResultsById.TryGetValue(
                    definition.Id,
                    out QaCheckResult? result))
            {
                throw new InvalidOperationException(
                    $"No report result exists for QA checklist definition '{definition.Id}'.");
            }

            FlowLayoutPanel targetPanel = definition.Section switch
            {
                QaChecklistSection.RawFile => rawChecklistFlowLayoutPanel,
                QaChecklistSection.Database => databaseChecklistFlowLayoutPanel,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(definition),
                    definition.Section,
                    "Unknown QA checklist section.")
            };

            QaChecklistItemControl itemControl = new()
            {
                Margin = new Padding(0, 0, 0, 8),
                Width = GetChecklistControlWidth(targetPanel)
            };

            itemControl.Bind(definition, result);
            itemControl.ResultChanged += ChecklistItemControl_ResultChanged;
            targetPanel.Controls.Add(itemControl);

            if (!checklistControlsById.TryAdd(definition.Id, itemControl))
            {
                itemControl.Dispose();
                throw new InvalidOperationException(
                    $"A checklist control already exists for stable ID '{definition.Id}'.");
            }
        }

        if (checklistControlsById.Count != checklistDefinitions.Count)
        {
            throw new InvalidOperationException(
                "The QA form must create exactly one checklist control for every catalog definition.");
        }
    }

    private static int GetChecklistControlWidth(FlowLayoutPanel panel)
    {
        int availableWidth = panel.ClientSize.Width
            - panel.Padding.Horizontal
            - SystemInformation.VerticalScrollBarWidth
            - 4;

        return Math.Max(520, availableWidth);
    }

    private static void ResizeChecklistControls(FlowLayoutPanel panel)
    {
        int width = GetChecklistControlWidth(panel);

        foreach (QaChecklistItemControl itemControl
                 in panel.Controls.OfType<QaChecklistItemControl>())
        {
            itemControl.Width = width;
        }
    }

    private void ChecklistItemControl_ResultChanged(
        object? sender,
        EventArgs eventArgs)
    {
        if (isSynchronizingFindings)
        {
            return;
        }

        if (sender is not QaChecklistItemControl itemControl
            || !checklistControlsById.TryGetValue(
                itemControl.CheckId,
                out QaChecklistItemControl? registeredControl)
            || !ReferenceEquals(itemControl, registeredControl))
        {
            throw new InvalidOperationException(
                "A checklist result-change event came from an unregistered control.");
        }

        isSynchronizingFindings = true;

        try
        {
            findingSynchronizationService.SetChecklistWarningSelected(
                CurrentReport,
                itemControl.CheckId,
                itemControl.WarningFound);
            SynchronizeFindingsAndRefreshUiCore();
        }
        finally
        {
            isSynchronizingFindings = false;
        }
    }

    private void SynchronizeCharacteristicsWhenChecked(RadioButton radioButton)
    {
        if (radioButton.Checked)
        {
            SynchronizeFileCharacteristics();
        }
    }

    private void SynchronizeFileCharacteristics()
    {
        if (isSynchronizingFindings)
        {
            return;
        }

        isSynchronizingFindings = true;

        try
        {
            QaFileCharacteristics characteristics = CurrentReport.FileCharacteristics;

            characteristics.NameColumnMode = fullNameColumnRadioButton.Checked
                ? QaNameColumnMode.FullName
                : QaNameColumnMode.SeparateFirstAndLastName;
            characteristics.HasCurrencyColumn = currencyColumnFoundRadioButton.Checked;
            characteristics.MonetaryColumnScenario =
                moreThanTwoMonetaryColumnsRadioButton.Checked
                    ? QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns
                    : twoMonetaryColumnsRadioButton.Checked
                        ? QaMonetaryColumnScenario.TwoMonetaryColumns
                        : QaMonetaryColumnScenario.OneMonetaryColumn;
            characteristics.HasMultipleConfirmationNumberCandidateColumns =
                multipleConfirmationCandidatesRadioButton.Checked;
            characteristics.IsCustomScriptSupportAvailable =
                customScriptAvailableRadioButton.Checked;
            characteristics.HasRejectedDatabaseRecords =
                rejectedRecordsExistRadioButton.Checked;

            moreThanTwoMonetaryColumnsNoteLabel.Visible =
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns;

            ApplyChecklistApplicability();
            SynchronizeFindingsAndRefreshUiCore();
        }
        finally
        {
            isSynchronizingFindings = false;
        }
    }

    private void ApplyChecklistApplicability()
    {
        foreach (QaCheckDefinition definition in checklistDefinitions)
        {
            if (!checklistControlsById.TryGetValue(
                    definition.Id,
                    out QaChecklistItemControl? itemControl))
            {
                throw new InvalidOperationException(
                    $"No checklist control exists for stable ID '{definition.Id}'.");
            }

            bool isApplicable = QaChecklistApplicabilityEvaluator.IsApplicable(
                definition,
                CurrentReport.FileCharacteristics);
            itemControl.SetApplicable(isApplicable);
        }
    }

    private void SynchronizeFindingsAndRefreshUi()
    {
        if (isSynchronizingFindings)
        {
            return;
        }

        isSynchronizingFindings = true;

        try
        {
            SynchronizeFindingsAndRefreshUiCore();
        }
        finally
        {
            isSynchronizingFindings = false;
        }
    }

    private void SynchronizeFindingsAndRefreshUiCore()
    {
        findingSynchronizationService.SynchronizeFindings(CurrentReport);
        SynchronizeChecklistWarningControls();
        RefreshFindingControls();
    }

    private void SynchronizeChecklistWarningControls()
    {
        HashSet<string> findingIds = CurrentReport.Findings
            .Select(finding => finding.FindingId)
            .ToHashSet(StringComparer.Ordinal);

        foreach ((string checkId, QaChecklistItemControl itemControl)
                 in checklistControlsById)
        {
            itemControl.SetWarningFound(
                findingIds.Contains(QaFindingIds.WarningForCheck(checkId)));
        }
    }

    private void RefreshFindingControls()
    {
        HashSet<string> currentIds = CurrentReport.Findings
            .Select(finding => finding.FindingId)
            .ToHashSet(StringComparer.Ordinal);

        warningsFlowLayoutPanel.SuspendLayout();
        failedChecksFlowLayoutPanel.SuspendLayout();

        try
        {
            foreach ((string findingId, QaFindingItemControl itemControl)
                     in findingControlsById.ToArray())
            {
                if (currentIds.Contains(findingId))
                {
                    continue;
                }

                itemControl.FindingChanged -= FindingItemControl_FindingChanged;
                itemControl.Parent?.Controls.Remove(itemControl);
                itemControl.Dispose();
                findingControlsById.Remove(findingId);
            }

            int warningIndex = 1;
            int failureIndex = 1;

            foreach (QaFinding finding in CurrentReport.Findings)
            {
                FlowLayoutPanel targetPanel = finding.Severity switch
                {
                    QaFindingSeverity.Warning => warningsFlowLayoutPanel,
                    QaFindingSeverity.Failure => failedChecksFlowLayoutPanel,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(finding),
                        finding.Severity,
                        "Unknown QA finding severity.")
                };

                if (!findingControlsById.TryGetValue(
                        finding.FindingId,
                        out QaFindingItemControl? itemControl))
                {
                    itemControl = new QaFindingItemControl
                    {
                        Margin = new Padding(0, 0, 0, 8),
                        Width = GetFindingControlWidth(targetPanel)
                    };
                    itemControl.FindingChanged += FindingItemControl_FindingChanged;
                    findingControlsById.Add(finding.FindingId, itemControl);
                }

                if (!ReferenceEquals(itemControl.Parent, targetPanel))
                {
                    targetPanel.Controls.Add(itemControl);
                }

                string? relatedCheckDisplay = null;

                if (!string.IsNullOrWhiteSpace(finding.RelatedCheckId))
                {
                    relatedCheckDisplay = checklistDefinitionsById.TryGetValue(
                        finding.RelatedCheckId,
                        out QaCheckDefinition? definition)
                            ? definition.DisplayName
                            : finding.RelatedCheckId;
                }

                itemControl.Bind(
                    finding,
                    relatedCheckDisplay,
                    CurrentReport.FileCharacteristics
                        .IsCustomScriptSupportAvailable);

                int childIndex = finding.Severity == QaFindingSeverity.Warning
                    ? warningIndex++
                    : failureIndex++;
                targetPanel.Controls.SetChildIndex(itemControl, childIndex);
            }

            noWarningsLabel.Visible = warningIndex == 1;
            noFailedChecksLabel.Visible = failureIndex == 1;

            ResizeFindingControls(warningsFlowLayoutPanel);
            ResizeFindingControls(failedChecksFlowLayoutPanel);
        }
        finally
        {
            warningsFlowLayoutPanel.ResumeLayout(performLayout: true);
            failedChecksFlowLayoutPanel.ResumeLayout(performLayout: true);
        }
    }

    private void FindingItemControl_FindingChanged(
        object? sender,
        EventArgs eventArgs)
    {
        if (sender is not QaFindingItemControl itemControl
            || !findingControlsById.TryGetValue(
                itemControl.FindingId,
                out QaFindingItemControl? registeredControl)
            || !ReferenceEquals(itemControl, registeredControl))
        {
            throw new InvalidOperationException(
                "A finding-change event came from an unregistered control.");
        }

        SynchronizeFindingsAndRefreshUi();
    }

    private static int GetFindingControlWidth(FlowLayoutPanel panel)
    {
        int availableWidth = panel.ClientSize.Width
            - panel.Padding.Horizontal
            - SystemInformation.VerticalScrollBarWidth
            - 4;

        return Math.Max(520, availableWidth);
    }

    private static void ResizeFindingControls(FlowLayoutPanel panel)
    {
        int width = GetFindingControlWidth(panel);

        foreach (QaFindingItemControl itemControl
                 in panel.Controls.OfType<QaFindingItemControl>())
        {
            itemControl.Width = width;
        }
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
