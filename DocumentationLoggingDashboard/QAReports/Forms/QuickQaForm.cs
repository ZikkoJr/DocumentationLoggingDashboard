using System.Diagnostics;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Forms.Controls;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.QAReports.Forms;

/// <summary>
/// Owns one manual, surface-level Quick QA draft and saves it only through the
/// dedicated two-workbook transaction.
/// </summary>
public partial class QuickQaForm : Form
{
    private readonly QaMetadataService metadataService;
    private readonly QaStoragePaths paths;
    private readonly QuickQaFindingSynchronizationService findingService = new();
    private readonly QuickQaSummaryService summaryService = new();
    private readonly QuickQaValidationService validationService = new();
    private readonly QuickQaWorkbookService workbookService;
    private readonly QuickQaWorkbookFilenameService filenameService;
    private readonly QuickQaPreferencesService preferencesService;
    private readonly QuickQaSaveService saveService;
    private readonly Dictionary<string, QuickQaChecklistItemControl>
        checklistControlsById = new(StringComparer.Ordinal);
    private IReadOnlyList<QaPmsMetadata> pmsSystems;
    private IReadOnlyList<QaHotelMetadata> hotels;
    private bool isSynchronizingSummary;
    private bool isSynchronizingInputs;
    private bool isRefreshingFindings;
    private bool isSavingQuickQa;

    public QuickQaForm(
        QaMetadataService metadataService,
        IReadOnlyList<QaPmsMetadata> pmsSystems,
        IReadOnlyList<QaHotelMetadata> hotels,
        QaStoragePaths paths)
    {
        this.metadataService = metadataService
            ?? throw new ArgumentNullException(nameof(metadataService));
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.pmsSystems = CreatePmsSnapshot(pmsSystems);
        this.hotels = CreateHotelSnapshot(hotels);
        filenameService = new QuickQaWorkbookFilenameService(paths);
        workbookService = new QuickQaWorkbookService(paths);
        preferencesService = new QuickQaPreferencesService(paths, filenameService);
        saveService = new QuickQaSaveService(paths, metadataService);

        CurrentReport = new QuickQaReport();
        InitializeComponent();
        InitializeInputs();
        BuildChecklistControls();
        WireEvents();

        hotelSelectorControl.SetHotels(this.hotels);
        SynchronizeHotelSelection();
        RefreshWorkbookChoices(preferredFileName: null);
        RefreshQaState();
    }

    public QuickQaReport CurrentReport { get; }

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
                "The Hotel metadata snapshot cannot contain a null record.",
                nameof(source));
        }

        return snapshot;
    }

    private void InitializeInputs()
    {
        DateTime today = DateTime.Today;
        fileMonthPicker.Value = new DateTime(today.Year, today.Month, 1);
        CurrentReport.HotelInformation.FileMonth =
            new QaFileMonth(today.Year, today.Month);
        noCustomScriptAvailableRadioButton.Checked = true;
    }

    private void BuildChecklistControls()
    {
        checklistFlowLayoutPanel.SuspendLayout();

        try
        {
            checklistFlowLayoutPanel.Controls.Clear();
            checklistControlsById.Clear();
            QaChecklistSection? previousSection = null;

            foreach (QuickQaCheckDefinition definition in
                     QuickQaChecklistCatalog.Definitions)
            {
                if (definition.Section != previousSection)
                {
                    checklistFlowLayoutPanel.Controls.Add(
                        CreateSectionHeader(definition.Section));
                    previousSection = definition.Section;
                }

                QuickQaChecklistItemControl control = new();
                control.Bind(
                    definition,
                    CurrentReport.GetChecklistResult(definition.Id));
                control.ResultChanged += (_, _) => RefreshQaState();
                checklistFlowLayoutPanel.Controls.Add(control);
                checklistControlsById.Add(definition.Id, control);
            }
        }
        finally
        {
            checklistFlowLayoutPanel.ResumeLayout();
            ResizeStackedChildren(checklistFlowLayoutPanel);
        }
    }

    private static Label CreateSectionHeader(QaChecklistSection section)
    {
        return new Label
        {
            AutoSize = false,
            Font = new Font(
                SystemFonts.MessageBoxFont ?? Control.DefaultFont,
                FontStyle.Bold),
            Height = 34,
            Margin = new Padding(0, 10, 0, 4),
            Padding = new Padding(4, 7, 4, 4),
            Text = section == QaChecklistSection.RawFile
                ? "Raw File QA"
                : "Database QA",
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private void WireEvents()
    {
        hotelSelectorControl.SelectedHotelChanged += (_, _) =>
            SynchronizeHotelSelection();
        fileMonthPicker.ValueChanged += (_, _) => SynchronizeFileMonth();
        fileIdTextBox.Validated += (_, _) => CommitFileId();
        customScriptAvailableRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCustomScriptAvailability(
                customScriptAvailableRadioButton,
                isAvailable: true);
        noCustomScriptAvailableRadioButton.CheckedChanged += (_, _) =>
            SynchronizeCustomScriptAvailability(
                noCustomScriptAvailableRadioButton,
                isAvailable: false);
        surfaceWorkbookComboBox.SelectedIndexChanged += (_, _) =>
            PersistSelectedWorkbook();
        createSurfaceWorkbookButton.Click += (_, _) =>
            CreateSurfaceWorkbook();
        summaryTextBox.TextChanged += (_, _) =>
            MarkSummaryManuallyEdited();
        regenerateSummaryButton.Click += (_, _) => RegenerateSummary();
        saveQuickQaButton.CausesValidation = false;
        saveQuickQaButton.Click += (_, _) => SaveQuickQa();
        FormClosing += (_, _) => CommitFileId();
    }

    private void SynchronizeHotelSelection()
    {
        if (isSynchronizingInputs)
        {
            return;
        }

        QaHotelMetadata? selected = hotelSelectorControl.SelectedHotel;
        CurrentReport.HotelInformation.HotelName = selected?.HotelName ?? string.Empty;
        CurrentReport.HotelInformation.HotelId = selected?.HotelId ?? string.Empty;
        CurrentReport.HotelInformation.PmsName = selected?.PmsName ?? string.Empty;

        isSynchronizingInputs = true;

        try
        {
            hotelNameTextBox.Text = selected?.HotelName ?? string.Empty;
            hotelIdTextBox.Text = selected?.HotelId ?? string.Empty;
            pmsTextBox.Text = selected?.PmsName ?? string.Empty;
        }
        finally
        {
            isSynchronizingInputs = false;
        }
    }

    private void SynchronizeFileMonth()
    {
        CurrentReport.HotelInformation.FileMonth = new QaFileMonth(
            fileMonthPicker.Value.Year,
            fileMonthPicker.Value.Month);
    }

    private void SynchronizeCustomScriptAvailability(
        RadioButton source,
        bool isAvailable)
    {
        if (!source.Checked || isSynchronizingInputs)
        {
            return;
        }

        CurrentReport.CustomScriptAvailable = isAvailable;
        RefreshQaState();
    }

    private void CommitFileId()
    {
        string normalized = fileIdTextBox.Text.Trim();
        CurrentReport.FileId = normalized;

        if (!string.Equals(
                fileIdTextBox.Text,
                normalized,
                StringComparison.Ordinal))
        {
            int selectionStart = Math.Min(
                fileIdTextBox.SelectionStart,
                normalized.Length);
            fileIdTextBox.Text = normalized;
            fileIdTextBox.SelectionStart = selectionStart;
        }
    }

    private void RefreshQaState()
    {
        if (isRefreshingFindings)
        {
            return;
        }

        isRefreshingFindings = true;

        try
        {
            findingService.Synchronize(CurrentReport);
            summaryService.Synchronize(CurrentReport);
            RefreshFindingControls();
            RefreshResult();
            RefreshSummaryFromReport();
        }
        finally
        {
            isRefreshingFindings = false;
        }
    }

    private void RefreshFindingControls()
    {
        Control[] previous = findingsFlowLayoutPanel.Controls
            .Cast<Control>()
            .ToArray();
        findingsFlowLayoutPanel.SuspendLayout();

        try
        {
            findingsFlowLayoutPanel.Controls.Clear();

            if (CurrentReport.Findings.Count == 0)
            {
                findingsFlowLayoutPanel.Controls.Add(noFindingsLabel);
            }
            else
            {
                foreach (QaFinding finding in CurrentReport.Findings)
                {
                    QuickQaFindingItemControl control = new();
                    control.Bind(
                        finding,
                        CurrentReport.CustomScriptAvailable);
                    control.ResolutionChanged += (_, _) => RefreshQaState();
                    findingsFlowLayoutPanel.Controls.Add(control);
                }
            }
        }
        finally
        {
            findingsFlowLayoutPanel.ResumeLayout();
            ResizeStackedChildren(findingsFlowLayoutPanel);

            foreach (Control control in previous)
            {
                if (!ReferenceEquals(control, noFindingsLabel))
                {
                    control.Dispose();
                }
            }
        }
    }

    private void RefreshResult()
    {
        resultValueLabel.Text = FormatStatus(CurrentReport.FinalStatus);
        resultValueLabel.ForeColor = CurrentReport.FinalStatus switch
        {
            QaReportStatus.Pass => Color.DarkGreen,
            QaReportStatus.PassWithWarnings => Color.DarkGoldenrod,
            QaReportStatus.Fail => Color.DarkRed,
            _ => SystemColors.ControlText
        };
    }

    private void RefreshSummaryFromReport()
    {
        bool previous = isSynchronizingSummary;
        isSynchronizingSummary = true;

        try
        {
            if (!string.Equals(
                    summaryTextBox.Text,
                    CurrentReport.Summary,
                    StringComparison.Ordinal))
            {
                summaryTextBox.Text = CurrentReport.Summary;
            }

            RefreshSummaryStaleState();
        }
        finally
        {
            isSynchronizingSummary = previous;
        }
    }

    private void MarkSummaryManuallyEdited()
    {
        if (isSynchronizingSummary)
        {
            return;
        }

        summaryService.MarkManuallyEdited(
            CurrentReport,
            summaryTextBox.Text);
        RefreshSummaryStaleState();
    }

    private void RegenerateSummary()
    {
        summaryService.Regenerate(CurrentReport);
        RefreshSummaryFromReport();
        summaryTextBox.Focus();
    }

    private void RefreshSummaryStaleState()
    {
        bool stale = summaryService.IsStale(CurrentReport);
        summaryStaleLabel.Visible = stale;
        regenerateSummaryButton.Enabled = stale
            || CurrentReport.SummaryWasManuallyEdited;
    }

    private void RefreshWorkbookChoices(string? preferredFileName)
    {
        string? remembered = preferredFileName;

        if (remembered is null)
        {
            try
            {
                remembered = preferencesService.LoadLastUsedWorkbookFileName();
            }
            catch (QuickQaPreferencesException exception)
            {
                Debug.WriteLine(exception);
                workbookStatusLabel.Text =
                    "The remembered workbook setting could not be read. Select a workbook to continue.";
            }
        }

        IReadOnlyList<string> compatible;

        try
        {
            compatible = workbookService.GetCompatibleSurfaceWorkbookFileNames();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            compatible = Array.Empty<string>();
            workbookStatusLabel.Text =
                "Surface QA workbooks are currently unavailable. Close open workbooks and refresh this form.";
        }

        string? selection = FindCaseInsensitive(compatible, remembered)
            ?? SelectUnambiguousMostRecent(compatible);
        isSynchronizingInputs = true;

        try
        {
            surfaceWorkbookComboBox.BeginUpdate();
            surfaceWorkbookComboBox.Items.Clear();
            surfaceWorkbookComboBox.Items.AddRange(compatible.Cast<object>().ToArray());
            surfaceWorkbookComboBox.SelectedItem = selection;
        }
        finally
        {
            surfaceWorkbookComboBox.EndUpdate();
            isSynchronizingInputs = false;
        }

        if (selection is null)
        {
            workbookStatusLabel.Text = compatible.Count == 0
                ? "No compatible Surface QA workbook is available. Create one to continue."
                : "Select a Surface QA workbook.";
        }
        else
        {
            workbookStatusLabel.Text = $"Selected: {selection}";
            TryPersistWorkbookPreference(selection);
        }
    }

    private string? SelectUnambiguousMostRecent(
        IReadOnlyList<string> compatible)
    {
        if (compatible.Count == 0)
        {
            return null;
        }

        (string Name, DateTime Modified)[] candidates = compatible
            .Select(name => (
                Name: name,
                Modified: File.GetLastWriteTimeUtc(
                    paths.ResolveSurfaceQaWorkbookPath(name))))
            .ToArray();
        DateTime newest = candidates.Max(candidate => candidate.Modified);
        string[] newestNames = candidates
            .Where(candidate => candidate.Modified == newest)
            .Select(candidate => candidate.Name)
            .ToArray();
        return newestNames.Length == 1 ? newestNames[0] : null;
    }

    private void PersistSelectedWorkbook()
    {
        if (isSynchronizingInputs)
        {
            return;
        }

        if (surfaceWorkbookComboBox.SelectedItem is string selected)
        {
            workbookStatusLabel.Text = $"Selected: {selected}";
            TryPersistWorkbookPreference(selected);
        }
    }

    private void TryPersistWorkbookPreference(string fileName)
    {
        try
        {
            preferencesService.SaveLastUsedWorkbookFileName(fileName);
        }
        catch (QuickQaPreferencesException exception)
        {
            Debug.WriteLine(exception);
            workbookStatusLabel.Text =
                $"Selected: {fileName} (the last-used preference could not be saved).";
        }
    }

    private void CreateSurfaceWorkbook()
    {
        string? requested = PromptForWorkbookName();

        if (requested is null)
        {
            return;
        }

        try
        {
            string created = workbookService.CreateNewSurfaceWorkbook(requested);
            RefreshWorkbookChoices(created);
            TryPersistWorkbookPreference(created);
            MessageBox.Show(
                this,
                $"Surface QA workbook created successfully.\r\n\r\n{created}",
                "Surface QA Workbook Created",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception) when (exception is ArgumentException
            or IOException
            or UnauthorizedAccessException
            or QuickQaWorkbookException)
        {
            Debug.WriteLine(exception);
            MessageBox.Show(
                this,
                CreateWorkbookFailureMessage(exception),
                "Surface QA Workbook Not Created",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void SaveQuickQa()
    {
        if (isSavingQuickQa)
        {
            return;
        }

        isSavingQuickQa = true;
        saveQuickQaButton.Enabled = false;

        try
        {
            CommitFileId();
            SynchronizeFileMonth();
            findingService.Synchronize(CurrentReport);
            summaryService.Synchronize(CurrentReport);
            string? selectedFileName =
                surfaceWorkbookComboBox.SelectedItem as string;
            IReadOnlyList<QaHotelMetadata> currentHotels =
                metadataService.LoadHotels();
            QuickQaValidationResult validation = validationService.Validate(
                CurrentReport,
                currentHotels,
                selectedFileName);

            if (!validation.IsValid)
            {
                MessageBox.Show(
                    this,
                    "Complete the following before saving:\r\n\r\n- "
                        + string.Join("\r\n- ", validation.Errors),
                    "Quick QA Not Ready",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                RefreshQaState();
                return;
            }

            QuickQaSaveResult result = saveService.Save(
                CurrentReport,
                selectedFileName!);
            TryPersistWorkbookPreference(result.SurfaceWorkbookFileName);
            ShowSaveSuccess(result);
        }
        catch (QuickQaSaveException exception)
        {
            Debug.WriteLine(exception);
            MessageBox.Show(
                this,
                CreateSaveFailureMessage(exception),
                "Quick QA Save Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            MessageBox.Show(
                this,
                "Quick QA could not be saved. No success was recorded. Close any open workbooks and try again.",
                "Quick QA Save Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            saveQuickQaButton.Enabled = true;
            isSavingQuickQa = false;
        }
    }

    private void ShowSaveSuccess(QuickQaSaveResult result)
    {
        List<string> lines =
        [
            "Quick QA was saved successfully.",
            string.Empty,
            $"Hotel: {result.HotelName} ({result.HotelId})",
            $"File ID: {result.FileId}",
            $"Result: {FormatStatus(result.FinalStatus)}",
            $"Surface workbook: {result.SurfaceWorkbookFileName}",
            "Hotel Quick QA history was updated."
        ];

        if (!string.IsNullOrWhiteSpace(result.CleanupWarning))
        {
            lines.Add(string.Empty);
            lines.Add("Cleanup warning: " + result.CleanupWarning);
        }

        MessageBox.Show(
            this,
            string.Join("\r\n", lines),
            "Quick QA Saved",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static string CreateSaveFailureMessage(
        QuickQaSaveException exception)
    {
        if (exception.ManualReviewRequired)
        {
            return "The Quick QA save failed and rollback could not fully restore both workbooks. Manual review of the Surface QA workbook and Hotel history is required.";
        }

        return exception.ErrorCategory switch
        {
            QuickQaSaveErrorCategory.SurfaceWorkbookInUse =>
                "The Surface QA workbook is currently open or unavailable. Close the workbook and try again.",
            QuickQaSaveErrorCategory.HotelHistoryWorkbookInUse =>
                "The Hotel Quick QA history workbook is currently open or unavailable. Close the workbook and try again.",
            QuickQaSaveErrorCategory.MetadataMismatch =>
                "The selected Hotel or PMS metadata changed while this Quick QA was open. Reselect the Hotel and try again.",
            QuickQaSaveErrorCategory.ConcurrentChange =>
                "A workbook changed while the Quick QA was being saved. Review the latest workbook and try again.",
            QuickQaSaveErrorCategory.SurfaceWorkbookInvalid =>
                "The selected Surface QA workbook does not use the approved seven-column schema and was not changed.",
            QuickQaSaveErrorCategory.HotelHistoryWorkbookInvalid =>
                "The Hotel Quick QA history workbook is incompatible or corrupt and was not changed.",
            QuickQaSaveErrorCategory.ReportInvalid =>
                "The Quick QA is no longer valid. Review the report details, checklist, findings, and Result, then try again.",
            QuickQaSaveErrorCategory.SummaryStale =>
                "The Summary is out of date. Regenerate it before saving.",
            QuickQaSaveErrorCategory.InvalidDestination =>
                "A Quick QA workbook destination is outside the approved storage locations.",
            QuickQaSaveErrorCategory.PermissionDenied =>
                "The Quick QA workbooks could not be written in the configured documentation folder.",
            _ =>
                "Quick QA could not be saved to both workbooks. No success was recorded."
        };
    }

    private static string CreateWorkbookFailureMessage(Exception exception)
    {
        return exception switch
        {
            IOException =>
                "A workbook with that filename already exists or is currently unavailable. Select the existing compatible workbook or choose another filename.",
            UnauthorizedAccessException =>
                "The Surface QA workbook could not be created in the configured documentation folder.",
            QuickQaWorkbookException workbookException
                when workbookException.ErrorCategory
                    == QuickQaWorkbookErrorCategory.AlreadyExists =>
                "A workbook with that filename already exists. Select the existing compatible workbook or choose another filename; it was not overwritten.",
            QuickQaWorkbookException workbookException
                when workbookException.ErrorCategory
                    == QuickQaWorkbookErrorCategory.InvalidFilename =>
                "Enter a safe .xlsx filename without folders, traversal, reserved device names, or another extension.",
            QuickQaWorkbookException =>
                "The Surface QA workbook could not be created or verified. No workbook was selected.",
            _ => exception.Message
        };
    }

    private string? PromptForWorkbookName()
    {
        using Form prompt = new()
        {
            AcceptButton = null,
            AutoScaleMode = AutoScaleMode.Font,
            ClientSize = new Size(520, 150),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterParent,
            Text = "Create New Surface QA File"
        };
        Label instruction = new()
        {
            AutoSize = true,
            Location = new Point(16, 16),
            Text = "Workbook filename (spaces are allowed; .xlsx is added when omitted):"
        };
        TextBox input = new()
        {
            Location = new Point(19, 45),
            Size = new Size(482, 23),
            Text = "Surface QA"
        };
        Button create = new()
        {
            DialogResult = DialogResult.OK,
            Location = new Point(326, 93),
            Size = new Size(85, 30),
            Text = "Create"
        };
        Button cancel = new()
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(417, 93),
            Size = new Size(85, 30),
            Text = "Cancel"
        };
        prompt.Controls.AddRange([instruction, input, create, cancel]);
        prompt.AcceptButton = create;
        prompt.CancelButton = cancel;
        input.SelectAll();

        return prompt.ShowDialog(this) == DialogResult.OK
            ? input.Text
            : null;
    }

    private static string FormatStatus(QaReportStatus status) => status switch
    {
        QaReportStatus.Pass => "Pass",
        QaReportStatus.PassWithWarnings => "Pass with Warnings",
        QaReportStatus.Fail => "Fail",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static string? FindCaseInsensitive(
        IEnumerable<string> source,
        string? value)
    {
        return value is null
            ? null
            : source.FirstOrDefault(item => item.Equals(
                value,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void ResizeStackedChildren(FlowLayoutPanel panel)
    {
        int width = Math.Max(
            200,
            panel.ClientSize.Width
                - panel.Padding.Horizontal
                - SystemInformation.VerticalScrollBarWidth
                - 4);

        foreach (Control control in panel.Controls)
        {
            control.Width = width - control.Margin.Horizontal;
        }
    }
}
