using System.Diagnostics;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Forms.Controls;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.QAReports.Forms;

/// <summary>
/// Owns and synchronizes one in-memory QA report draft through readiness review
/// and the paired QA report save workflow.
/// </summary>
public partial class QaReportForm : Form
{
    private const int ExpectedChecklistDefinitionCount = 28;
    private const int MaximumOverwriteFilenamesPerLocation = 3;
    private const int MaximumDisplayedFilenameLength = 100;
    private const int DisplayedFilenameSuffixLength = 30;

    private readonly QaMetadataService metadataService;
    private readonly QaReportSaveService reportSaveService;
    private readonly QaPdfGenerationService pdfGenerationService = new();
    private readonly IReadOnlyList<QaCheckDefinition> checklistDefinitions;
    private readonly IReadOnlyDictionary<string, QaCheckDefinition> checklistDefinitionsById;
    private readonly Dictionary<string, QaCheckResult> checklistResultsById;
    private readonly Dictionary<string, QaChecklistItemControl> checklistControlsById =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, QaFindingItemControl> findingControlsById =
        new(StringComparer.Ordinal);
    private readonly QaFindingSynchronizationService findingSynchronizationService;
    private readonly QaStatisticsCalculationService statisticsCalculationService = new();
    private readonly QaReportValidationService reportValidationService = new();
    private readonly HashSet<TextBox> pendingReportTextBoxes = [];

    private IReadOnlyList<QaPmsMetadata> pmsSystems;
    private IReadOnlyList<QaHotelMetadata> hotels;
    private bool isSynchronizingFindings;
    private bool isInitializingPhase7 = true;
    private bool isCheckingReportReadiness;
    private bool isSavingQaReport;
    private bool isCommittingAllPendingTextEdits;
    private bool hasReadinessResult;

    public QaReportForm(
        QaMetadataService metadataService,
        IReadOnlyList<QaPmsMetadata> pmsSystems,
        IReadOnlyList<QaHotelMetadata> hotels,
        QaStoragePaths paths)
    {
        this.metadataService = metadataService
            ?? throw new ArgumentNullException(nameof(metadataService));
        ArgumentNullException.ThrowIfNull(paths);
        reportSaveService = new QaReportSaveService(
            paths,
            this.metadataService,
            new QaFolderNameSanitizer());
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
        statisticsControl.Bind(CurrentReport);
        InitializeReportInputs();
        BuildChecklistControls();
        WireEvents();

        hotelSelectorControl.SetHotels(this.hotels);
        SynchronizeHotelSelection();
        SynchronizeReportInputs();
        SynchronizeFileCharacteristics();
        isInitializingPhase7 = false;
        statisticsControl.RefreshFromReport();
    }

    /// <summary>
    /// Gets the single report draft updated directly by this form's controls.
    /// Closing the form does not persist this object.
    /// </summary>
    public QaReport CurrentReport { get; }

    /// <summary>
    /// Gets the most recent non-stale readiness result, or null until the report is
    /// checked again after an edit.
    /// </summary>
    public QaReportValidationResult? LastReadinessResult { get; private set; }

    /// <summary>
    /// Gets the trimmed QA person or the approved team fallback without mutating
    /// <see cref="QaReport.CreatedBy"/>.
    /// </summary>
    public string EffectiveCreatedBy =>
        TrimToNull(CurrentReport.CreatedBy)
        ?? QaReportValidationService.DefaultEffectiveCreatedBy;

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
        WireDeferredReportTextBox(createdByTextBox);
        WireDeferredReportTextBox(originalFileNameTextBox);
        WireDeferredReportTextBox(generalNotesTextBox);

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
        generateAndSaveQaReportButton.CausesValidation = false;
        generateAndSaveQaReportButton.Enter += (_, _) =>
            CommitAllPendingTextEdits(refreshChildUi: false);
        generateAndSaveQaReportButton.Click += (_, _) =>
            GenerateAndSaveQaReport();
        reportTabControl.Selecting += (_, _) =>
            CommitAllPendingTextEdits();
        FormClosing += (_, _) => CommitAllPendingTextEdits();

        WirePhase7Events();
    }

    private void SynchronizeReportInputs()
    {
        SynchronizeFileMonth();
        SynchronizeQaDate();
        _ = CommitPendingReportTextEdits();
    }

    private void WireDeferredReportTextBox(TextBox textBox)
    {
        textBox.TextChanged += (_, _) => MarkReportTextEditPending(textBox);
        textBox.Validated += (_, _) => CommitPendingReportTextEdit(textBox);
    }

    private void MarkReportTextEditPending(TextBox textBox)
    {
        if (pendingReportTextBoxes.Add(textBox))
        {
            InvalidateReportReadiness();
        }
    }

    private bool CommitPendingReportTextEdit(TextBox textBox)
    {
        if (!pendingReportTextBoxes.Remove(textBox))
        {
            return false;
        }

        string? value = TrimToNull(textBox.Text);

        if (ReferenceEquals(textBox, createdByTextBox))
        {
            return ApplyReportTextValue(
                CurrentReport.CreatedBy,
                value,
                committed => CurrentReport.CreatedBy = committed);
        }

        if (ReferenceEquals(textBox, originalFileNameTextBox))
        {
            return ApplyReportTextValue(
                CurrentReport.OriginalFileName,
                value,
                committed => CurrentReport.OriginalFileName = committed);
        }

        if (ReferenceEquals(textBox, generalNotesTextBox))
        {
            return ApplyReportTextValue(
                CurrentReport.GeneralNotes,
                value,
                committed => CurrentReport.GeneralNotes = committed);
        }

        throw new InvalidOperationException(
            "An unregistered report text box had a pending edit.");
    }

    private bool CommitPendingReportTextEdits()
    {
        bool changed = false;

        foreach (TextBox textBox in pendingReportTextBoxes.ToArray())
        {
            changed |= CommitPendingReportTextEdit(textBox);
        }

        return changed;
    }

    private static bool ApplyReportTextValue(
        string? modelValue,
        string? committedValue,
        Action<string?> applyValue)
    {
        if (string.Equals(modelValue, committedValue, StringComparison.Ordinal))
        {
            return false;
        }

        applyValue(committedValue);
        return true;
    }

    /// <summary>
    /// Flushes every deferred QA report text field. Child notifications are
    /// suppressed while the batch is collected so an action boundary performs at
    /// most one findings/statistics refresh.
    /// </summary>
    public bool CommitAllPendingTextEdits()
    {
        return CommitAllPendingTextEdits(refreshChildUi: true);
    }

    private bool CommitAllPendingTextEdits(bool refreshChildUi)
    {
        if (isCommittingAllPendingTextEdits)
        {
            return false;
        }

        bool changed = false;
        bool reportTextChanged = false;
        bool childModelChanged = false;
        isCommittingAllPendingTextEdits = true;

        try
        {
            reportTextChanged = CommitPendingReportTextEdits();
            changed |= reportTextChanged;

            foreach (QaChecklistItemControl itemControl
                     in checklistControlsById.Values)
            {
                childModelChanged |= itemControl.CommitPendingTextEdits();
            }

            foreach (QaFindingItemControl itemControl
                     in findingControlsById.Values)
            {
                childModelChanged |= itemControl.CommitPendingTextEdits();
            }

            childModelChanged |= statisticsControl.CommitPendingTextEdits();
            changed |= childModelChanged;

            if (childModelChanged && refreshChildUi)
            {
                bool wasSynchronizingFindings = isSynchronizingFindings;
                isSynchronizingFindings = true;

                try
                {
                    SynchronizeAllChecklistWarningSelections();
                    SynchronizeFindingsAndRefreshUiCore();
                }
                finally
                {
                    isSynchronizingFindings = wasSynchronizingFindings;
                }
            }
        }
        finally
        {
            isCommittingAllPendingTextEdits = false;
        }

        if (childModelChanged)
        {
            InvalidateReportReadiness();
        }

        return changed;
    }

    private void SynchronizeAllChecklistWarningSelections()
    {
        foreach ((string checkId, QaChecklistItemControl itemControl)
                 in checklistControlsById)
        {
            findingSynchronizationService.SetChecklistWarningSelected(
                CurrentReport,
                checkId,
                itemControl.WarningFound);
        }
    }

    private void SynchronizeFileMonth()
    {
        CurrentReport.HotelInformation.FileMonth = new QaFileMonth(
            fileMonthPicker.Value.Year,
            fileMonthPicker.Value.Month);
        InvalidateReportReadiness();
    }

    private void SynchronizeQaDate()
    {
        CurrentReport.QaDate = DateOnly.FromDateTime(qaDatePicker.Value);
        InvalidateReportReadiness();
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
            InvalidateReportReadiness();
            return;
        }

        hotelInformation.HotelId = selectedHotel.HotelId;
        hotelInformation.HotelName = selectedHotel.HotelName;
        hotelInformation.PmsName = selectedHotel.PmsName;
        selectedHotelIdTextBox.Text = selectedHotel.HotelId;
        selectedPmsTextBox.Text = selectedHotel.PmsName;
        InvalidateReportReadiness();
    }

    private void OpenMetadataManagement()
    {
        _ = CommitAllPendingTextEdits();

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
        if (isSynchronizingFindings || isCommittingAllPendingTextEdits)
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
            _ = CommitAllPendingTextEdits(refreshChildUi: false);
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

        InvalidateReportReadiness();
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
            _ = CommitAllPendingTextEdits(refreshChildUi: false);
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
            SynchronizeRejectedRecordsCharacteristicFromStatistics();

            moreThanTwoMonetaryColumnsNoteLabel.Visible =
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns;

            statisticsCalculationService.Synchronize(CurrentReport);
            ApplyChecklistApplicability();
            SynchronizeFindingsAndRefreshUiCore();
        }
        finally
        {
            isSynchronizingFindings = false;
        }

        InvalidateReportReadiness();
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
            _ = CommitAllPendingTextEdits(refreshChildUi: false);
            SynchronizeFindingsAndRefreshUiCore();
        }
        finally
        {
            isSynchronizingFindings = false;
        }
    }

    private void SynchronizeFindingsAndRefreshUiCore()
    {
        statisticsCalculationService.Synchronize(CurrentReport);
        findingSynchronizationService.SynchronizeFindings(CurrentReport);
        SynchronizeChecklistWarningControls();
        RefreshFindingControls();
        statisticsControl.RefreshFromReport();
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
                itemControl.DiscardPendingTextEdits();
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
        if (isCommittingAllPendingTextEdits)
        {
            return;
        }

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
        InvalidateReportReadiness();
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

    private void WirePhase7Events()
    {
        statisticsControl.StatisticsChanged +=
            StatisticsControl_StatisticsChanged;
        checkReportReadinessButton.CausesValidation = false;
        checkReportReadinessButton.Enter += (_, _) =>
            CommitAllPendingTextEdits(refreshChildUi: false);
        checkReportReadinessButton.Click += (_, _) =>
            CheckReportReadiness();
    }

    private void StatisticsControl_StatisticsChanged(
        object? sender,
        EventArgs eventArgs)
    {
        if (isSynchronizingFindings
            || isCheckingReportReadiness
            || isCommittingAllPendingTextEdits)
        {
            return;
        }

        isSynchronizingFindings = true;

        try
        {
            _ = CommitAllPendingTextEdits(refreshChildUi: false);
            statisticsControl.CommitCurrentValues();
            statisticsCalculationService.Synchronize(CurrentReport);
            SynchronizeRejectedRecordsCharacteristicFromStatistics();
            ApplyChecklistApplicability();
            SynchronizeFindingsAndRefreshUiCore();
        }
        finally
        {
            isSynchronizingFindings = false;
        }

        InvalidateReportReadiness();
    }

    private void SynchronizeRejectedRecordsCharacteristicFromStatistics()
    {
        int rejectedRecordCount = CurrentReport.Statistics.Database
            .RejectedRecordCount;
        bool hasRejectedRecords = rejectedRecordCount > 0;

        CurrentReport.FileCharacteristics.HasRejectedDatabaseRecords =
            hasRejectedRecords;
        rejectedRecordsExistRadioButton.Checked = hasRejectedRecords;
        noRejectedRecordsRadioButton.Checked = !hasRejectedRecords;
    }

    private void CheckReportReadiness()
    {
        if (isSavingQaReport)
        {
            return;
        }

        _ = RunFreshReadinessCheck();
    }

    private QaReportValidationResult? RunFreshReadinessCheck()
    {
        if (isCheckingReportReadiness || isSynchronizingFindings)
        {
            return null;
        }

        isCheckingReportReadiness = true;

        try
        {
            _ = CommitAllPendingTextEdits(refreshChildUi: false);
            SynchronizeHotelSelection();
            SynchronizeReportInputs();
            statisticsControl.CommitCurrentValues();
            SynchronizeFileCharacteristics();

            QaReportValidationResult result =
                reportValidationService.Validate(CurrentReport, hotels);

            CurrentReport.ReportStatus = result.IsReady
                ? result.CalculatedStatus
                : null;
            DisplayReadinessResult(result);
            return result;
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            CurrentReport.ReportStatus = null;
            QaReportValidationResult failureResult =
                CreateUnexpectedReadinessFailureResult();
            DisplayReadinessResult(failureResult);
            return failureResult;
        }
        finally
        {
            isCheckingReportReadiness = false;
        }
    }

    private void DisplayReadinessResult(QaReportValidationResult result)
    {
        LastReadinessResult = result;
        hasReadinessResult = true;
        reportTabControl.SelectedTab = statisticsReadinessTabPage;
        statisticsControl.ShowReadinessResult(result);
    }

    private void GenerateAndSaveQaReport()
    {
        if (isSavingQaReport)
        {
            return;
        }

        isSavingQaReport = true;
        bool generateButtonWasEnabled = generateAndSaveQaReportButton.Enabled;
        bool readinessButtonWasEnabled = checkReportReadinessButton.Enabled;
        generateAndSaveQaReportButton.Enabled = false;
        checkReportReadinessButton.Enabled = false;

        try
        {
            QaReportValidationResult? validationResult =
                RunFreshReadinessCheck();

            if (validationResult is null
                || !IsReadyForPdfGeneration(validationResult))
            {
                ShowReportNotReady();
                return;
            }

            if (!ConfirmEffectiveCreatedBy(validationResult))
            {
                return;
            }

            // Keep this explicit gate immediately beside the rendering boundary.
            if (!IsReadyForPdfGeneration(validationResult))
            {
                ShowReportNotReady();
                return;
            }

            DateTimeOffset generatedAt = DateTimeOffset.UtcNow;
            byte[] pdfBytes = pdfGenerationService.GeneratePdf(
                CurrentReport,
                validationResult,
                generatedAt);

            QaReportSaveRequest request = new(
                CurrentReport,
                validationResult,
                generatedAt);
            var preparation = reportSaveService.Prepare(request);
            bool overwriteConfirmed = false;

            if (preparation.ExistingFiles.HasMatches)
            {
                overwriteConfirmed =
                    ConfirmExistingReportOverwrite(preparation);

                if (!overwriteConfirmed)
                {
                    return;
                }
            }

            var saveResult = reportSaveService.Save(
                preparation,
                pdfBytes,
                overwriteConfirmed);

            if (saveResult.Cancelled)
            {
                return;
            }

            if (!saveResult.Success
                || !saveResult.IndexUpdated
                || saveResult.ManualReviewRequired)
            {
                ShowUnsuccessfulQaReportSaveResult(saveResult);
                return;
            }

            ShowQaReportSaveSuccess(saveResult);
        }
        catch (QaPdfGenerationException exception)
        {
            Debug.WriteLine(exception);
            MessageBox.Show(
                this,
                "The QA report PDF could not be generated.\r\n\r\n" +
                    exception.Message,
                "QA Report Generation Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (QaReportIndexException exception)
        {
            Debug.WriteLine(exception);
            MessageBox.Show(
                this,
                "The QA report index could not be updated. The new report copies were not kept.",
                "QA Report Save Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (QaReportSaveException exception)
        {
            ShowQaReportSaveFailure(exception);
        }
        catch (QaMetadataException exception)
        {
            Debug.WriteLine(exception);
            MessageBox.Show(
                this,
                "The saved Hotel or PMS information changed while this report was open. Refresh the report details and try again.",
                "QA Report Save Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            MessageBox.Show(
                this,
                "The QA report could not be saved because an unexpected error occurred. No success was recorded.",
                "QA Report Save Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            generateAndSaveQaReportButton.Enabled = generateButtonWasEnabled;
            checkReportReadinessButton.Enabled = readinessButtonWasEnabled;
            isSavingQaReport = false;
        }
    }

    private bool IsReadyForPdfGeneration(
        QaReportValidationResult validationResult)
    {
        return validationResult.IsReady
            && validationResult.BlockingErrors.Count == 0
            && validationResult.CalculatedStatus is not null
            && CurrentReport.ReportStatus == validationResult.CalculatedStatus
            && !string.IsNullOrWhiteSpace(
                validationResult.ValidatedReportFingerprint);
    }

    private void ShowReportNotReady()
    {
        reportTabControl.SelectedTab = statisticsReadinessTabPage;
        MessageBox.Show(
            this,
            "The QA report is not ready to be generated. Review the listed validation errors.",
            "QA Report Not Ready",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private bool ConfirmEffectiveCreatedBy(
        QaReportValidationResult validationResult)
    {
        if (!string.IsNullOrWhiteSpace(CurrentReport.CreatedBy))
        {
            return true;
        }

        DialogResult response = MessageBox.Show(
            this,
            "This QA report does not have an assigned QA person.\r\n\r\n" +
                "The generated PDF and QA index will use:\r\n" +
                validationResult.EffectiveCreatedBy +
                "\r\n\r\nDo you want to continue?",
            "Created By Not Assigned",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        return response == DialogResult.Yes;
    }

    private bool ConfirmExistingReportOverwrite(
        QaReportSavePreparation preparation)
    {
        List<string> lines =
        [
            "A QA report already exists for this Hotel and File Month.",
            string.Empty,
            $"Hotel: {NormalizeDialogValue(preparation.HotelName)} / {NormalizeDialogValue(preparation.HotelId)}",
            $"PMS: {NormalizeDialogValue(preparation.PmsName)}",
            $"File Month: {preparation.FileMonth}",
            string.Empty
        ];

        AppendExistingFileSummary(
            lines,
            "Hotel location",
            preparation.ExistingFiles.HotelFilePaths);
        AppendExistingFileSummary(
            lines,
            "PMS location",
            preparation.ExistingFiles.PmsFilePaths);

        if (preparation.ExistingFiles.IsInconsistent)
        {
            lines.Add(string.Empty);
            lines.Add(
                "The existing Hotel and PMS report copies are inconsistent.");
        }

        lines.Add(string.Empty);
        lines.Add("Do you want to overwrite the existing QA report?");

        DialogResult response = MessageBox.Show(
            this,
            string.Join("\r\n", lines),
            "Replace Existing QA Report",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        return response == DialogResult.Yes;
    }

    private static void AppendExistingFileSummary(
        ICollection<string> lines,
        string locationLabel,
        IReadOnlyList<string> filePaths)
    {
        lines.Add($"{locationLabel}: {filePaths.Count} file(s)");

        foreach (string filePath in filePaths
                     .Take(MaximumOverwriteFilenamesPerLocation))
        {
            string filename = Path.GetFileName(filePath) ?? "Existing QA report";
            lines.Add($"  {AbbreviateFilename(filename)}");
        }

        int additionalCount =
            filePaths.Count - MaximumOverwriteFilenamesPerLocation;
        if (additionalCount > 0)
        {
            lines.Add($"  ... and {additionalCount} more");
        }
    }

    private static string AbbreviateFilename(string filename)
    {
        if (filename.Length <= MaximumDisplayedFilenameLength)
        {
            return filename;
        }

        int prefixLength = MaximumDisplayedFilenameLength
            - DisplayedFilenameSuffixLength
            - 1;
        if (char.IsHighSurrogate(filename[prefixLength - 1]))
        {
            prefixLength--;
        }

        int suffixStart = filename.Length - DisplayedFilenameSuffixLength;
        if (char.IsLowSurrogate(filename[suffixStart]))
        {
            suffixStart++;
        }

        return filename[..prefixLength] + "\u2026" + filename[suffixStart..];
    }

    private void ShowQaReportSaveSuccess(QaReportSaveResult result)
    {
        List<string> lines =
        [
            "The QA report was saved successfully.",
            string.Empty,
            $"Filename: {result.FinalFilename}",
            $"Hotel copy: {result.HotelCopyPath}",
            $"PMS copy: {result.PmsCopyPath}",
            $"Status: {FormatReportStatus(result.FinalStatus)}",
            $"Existing report replaced: {(result.OverwriteOccurred ? "Yes" : "No")}"
        ];

        if (!string.IsNullOrWhiteSpace(result.CleanupWarning))
        {
            lines.Add(string.Empty);
            lines.Add("Cleanup warning: " + result.CleanupWarning);
        }

        MessageBox.Show(
            this,
            string.Join("\r\n", lines),
            "QA Report Saved",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowUnsuccessfulQaReportSaveResult(
        QaReportSaveResult result)
    {
        string message = result.ManualReviewRequired
            ? "The QA report save failed and the previous state could not be fully restored. Manual review of the Hotel and PMS QA report folders is required."
            : !result.IndexUpdated
                ? "The QA report index could not be updated. The new report copies were not kept."
                : "The QA report could not be saved to both required locations. No success was recorded.";

        MessageBox.Show(
            this,
            message,
            "QA Report Save Failed",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void ShowQaReportSaveFailure(QaReportSaveException exception)
    {
        Debug.WriteLine(exception);

        string message;

        if (exception.ManualReviewRequired)
        {
            message =
                "The QA report save failed and the previous state could not be fully restored. Manual review of the Hotel and PMS QA report folders is required.";

            if (exception.ManualReviewLocations.Count > 0)
            {
                string locations = string.Join(
                    "\r\n",
                    exception.ManualReviewLocations
                        .Take(3)
                        .Select(path => "- " + AbbreviateFilename(
                            NormalizeDialogValue(path))));
                message += "\r\n\r\nReview locations:\r\n" + locations;
            }
        }
        else
        {
            message = exception.ErrorCategory switch
            {
                QaReportSaveErrorCategory.MetadataMismatch =>
                    "The saved Hotel or PMS information changed while this report was open. Refresh the report details and try again.",
                QaReportSaveErrorCategory.FileInUse =>
                    "The existing QA report could not be replaced because one of its files is currently in use.",
                QaReportSaveErrorCategory.PermissionDenied =>
                    "The QA report could not be written to the configured documentation folder.",
                QaReportSaveErrorCategory.IndexFailure =>
                    "The QA report index could not be updated. The new report copies were not kept.",
                QaReportSaveErrorCategory.ReportNotReady
                    or QaReportSaveErrorCategory.StaleReadiness =>
                    "The QA report is not ready to be generated. Review the listed validation errors.",
                QaReportSaveErrorCategory.FilenameFailure
                    or QaReportSaveErrorCategory.InvalidDestination =>
                    "The QA report filename or configured destination is not safe for saving.",
                QaReportSaveErrorCategory.OverwriteRequired
                    or QaReportSaveErrorCategory.ConcurrentChange =>
                    "The existing QA report files changed before the save could be completed. Review the report folders and try again.",
                QaReportSaveErrorCategory.RollbackFailure =>
                    "The QA report save failed and the previous state could not be fully restored. Manual review of the Hotel and PMS QA report folders is required.",
                _ when exception.PreviousStateRestored =>
                    "The QA report could not be saved to both required locations. The previous report files were restored.",
                _ =>
                    "The QA report could not be saved to both required locations. No success was recorded."
            };
        }

        if (exception.ErrorCategory is QaReportSaveErrorCategory.ReportNotReady
            or QaReportSaveErrorCategory.StaleReadiness)
        {
            reportTabControl.SelectedTab = statisticsReadinessTabPage;
        }

        MessageBox.Show(
            this,
            message,
            "QA Report Save Failed",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static string NormalizeDialogValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        string controlsReplaced = new(value
            .Select(character => char.IsControl(character) ? ' ' : character)
            .ToArray());
        string normalized = string.Join(
            " ",
            controlsReplaced.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        return AbbreviateFilename(normalized);
    }

    private static string FormatReportStatus(QaReportStatus status)
    {
        return status switch
        {
            QaReportStatus.Pass => "Pass",
            QaReportStatus.PassWithWarnings => "Pass with Warnings",
            QaReportStatus.Fail => "Fail",
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown QA report status.")
        };
    }

    private QaReportValidationResult CreateUnexpectedReadinessFailureResult()
    {
        string? createdBy = TrimToNull(CurrentReport.CreatedBy);
        List<string> workflowWarnings = [];

        if (createdBy is null)
        {
            workflowWarnings.Add(
                QaReportValidationService.MissingCreatedByWarning);
        }

        int warningCount = CurrentReport.Findings.Count(
            finding => finding is not null
                && finding.Severity == QaFindingSeverity.Warning);
        int failureCount = CurrentReport.Findings.Count(
            finding => finding is not null
                && finding.Severity == QaFindingSeverity.Failure);
        int handledCount = CurrentReport.Findings.Count(
            finding => finding is not null
                && finding.Resolution != QaFindingResolution.Active);

        return new QaReportValidationResult(
            ["The report could not be synchronized for readiness because its in-memory data is internally inconsistent."],
            workflowWarnings,
            calculatedStatus: null,
            createdBy ?? QaReportValidationService.DefaultEffectiveCreatedBy,
            warningCount,
            failureCount,
            handledCount);
    }

    private void InvalidateReportReadiness()
    {
        CurrentReport.ReportStatus = null;
        LastReadinessResult = null;

        if (isInitializingPhase7
            || isCheckingReportReadiness
            || isSynchronizingFindings
            || !hasReadinessResult)
        {
            return;
        }

        statisticsControl.MarkReadinessStale();
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
