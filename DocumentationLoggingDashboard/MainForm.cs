using System.Diagnostics;
using DocumentationLoggingDashboard.DocumentationLogs;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.QAReports.Forms;
using DocumentationLoggingDashboard.QAReports.Forms.Controls;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using DocumentationLoggingDashboard.Services;

namespace DocumentationLoggingDashboard;

public partial class MainForm : Form
{
    private readonly LogTemplateService logTemplateService = new();
    private readonly SettingsService settingsService = new();
    private readonly LogFileService logFileService;
    private readonly Dictionary<DocumentationLogFormFieldDefinition, TextBox> fieldInputs = new();
    private readonly DocumentationLogPreviewFormatter documentationLogPreviewFormatter = new();

    private DocumentationLogStoragePaths? documentationLogPaths;
    private DocumentationLogWorkbookFilenameService? documentationLogFilenameService;
    private DocumentationLogWorkbookService? documentationLogWorkbookService;
    private DocumentationLogWorkbookPreferencesService? documentationLogPreferencesService;
    private DocumentationLogSaveService? documentationLogSaveService;
    private IReadOnlyList<QaHotelMetadata> documentationLogHotels = Array.Empty<QaHotelMetadata>();
    private IReadOnlyList<QaPmsMetadata> documentationLogPmsSystems = Array.Empty<QaPmsMetadata>();
    private QaHotelSelectorControl? debuggingHotelSelectorControl;
    private TextBox? debuggingHotelNameTextBox;
    private TextBox? debuggingHotelIdTextBox;
    private TextBox? debuggingPmsTextBox;
    private Exception? documentationLogInitializationError;
    private bool isRefreshingRunningWorkbookChoices;
    private bool isSavingDocumentationLog;

    public MainForm()
    {
        logFileService = new LogFileService(settingsService, logTemplateService);

        InitializeComponent();
        WireButtonEvents();
        InitializeDocumentationLogWorkflow();
        ConfigureLogTypeDropdown();
        RefreshDocumentationRootFolderDisplay();
    }

    private void ConfigureLogTypeDropdown()
    {
        logTypeComboBox.DisplayMember = nameof(LogTypeOption.DisplayName);
        logTypeComboBox.ValueMember = nameof(LogTypeOption.LogType);

        foreach (LogType logType in logTemplateService.GetSupportedLogTypes())
        {
            logTypeComboBox.Items.Add(new LogTypeOption(logType, logTemplateService.GetDisplayName(logType)));
        }

        logTypeComboBox.SelectedIndexChanged += (_, _) =>
            HandleSelectedLogTypeChanged();
        logTypeComboBox.SelectedIndex = 0;
    }

    private void WireButtonEvents()
    {
        previewEntryButton.Click += (_, _) => PreviewEntry();
        submitEntryButton.Click += (_, _) => SubmitEntry();
        clearFormButton.Click += (_, _) => ClearForm();
        openSelectedLogWorkbookButton.Click += (_, _) => OpenSelectedLogWorkbook();
        createNewLogFileButton.Click += (_, _) => CreateNewLogFile();
        runningWorkbookComboBox.SelectedIndexChanged += (_, _) =>
            SaveSelectedRunningWorkbookPreference();
        openLogsFolderButton.Click += (_, _) => OpenLogsFolder();
        openLogIndexButton.Click += (_, _) => OpenLogIndex();
        changeLogsFolderButton.Click += (_, _) => ChangeLogsFolder();
        resetDefaultFolderButton.Click += (_, _) => ResetToDefaultFolder();
        detailedQaReportButton.Click += (_, _) => OpenDetailedQaReport();
        quickQaButton.Click += (_, _) => OpenQuickQa();
        manageQaHotelsPmsButton.Click += (_, _) => OpenQaMetadataManagement();
    }

    private void RenderFieldsForSelectedLogType()
    {
        fieldsTableLayoutPanel.SuspendLayout();

        Control[] previousControls = fieldsTableLayoutPanel.Controls
            .Cast<Control>()
            .ToArray();
        fieldsTableLayoutPanel.Controls.Clear();

        foreach (Control control in previousControls)
        {
            control.Dispose();
        }

        fieldsTableLayoutPanel.RowStyles.Clear();
        fieldsTableLayoutPanel.RowCount = 0;
        fieldInputs.Clear();
        debuggingHotelSelectorControl = null;
        debuggingHotelNameTextBox = null;
        debuggingHotelIdTextBox = null;
        debuggingPmsTextBox = null;

        DocumentationLogFormContract contract =
            DocumentationLogWorkbookSchema.GetFormContract(GetSelectedLogType());

        if (contract.UsesHotelSelector)
        {
            AddDebuggingHotelRows();
        }

        foreach (DocumentationLogFormFieldDefinition field in contract.Fields)
        {
            AddFieldRow(field);
        }

        fieldsTableLayoutPanel.ResumeLayout();
        previewTextBox.Clear();
    }

    private void AddDebuggingHotelRows()
    {
        QaHotelSelectorControl selector = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 3, 0, 10),
            Height = 220,
            MinimumSize = new Size(300, 180)
        };
        selector.SelectedHotelChanged += (_, _) =>
            UpdateDebuggingHotelContext();
        selector.SetHotels(documentationLogHotels, preferredHotelId: string.Empty);
        debuggingHotelSelectorControl = selector;
        AddControlRow("Hotel *", selector, height: 230);

        debuggingHotelNameTextBox = AddReadOnlyContextRow("Hotel Name");
        debuggingHotelIdTextBox = AddReadOnlyContextRow("Hotel ID");
        debuggingPmsTextBox = AddReadOnlyContextRow("PMS");
        UpdateDebuggingHotelContext();
    }

    private TextBox AddReadOnlyContextRow(string label)
    {
        TextBox textBox = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 3, 0, 8),
            ReadOnly = true,
            TabStop = false
        };
        AddControlRow(label, textBox);
        return textBox;
    }

    private void AddFieldRow(DocumentationLogFormFieldDefinition field)
    {
        TextBox textBox = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 3, 0, 8),
            Multiline = field.IsMultiline,
            ScrollBars = field.IsMultiline ? ScrollBars.Vertical : ScrollBars.None,
            Height = field.IsMultiline ? 84 : 27
        };
        AddControlRow(
            field.IsRequired ? $"{field.Label} *" : field.Label,
            textBox,
            field.IsMultiline ? 95 : null);
        fieldInputs[field] = textBox;
    }

    private void AddControlRow(string labelText, Control control, int? height = null)
    {
        int rowIndex = fieldsTableLayoutPanel.RowCount++;
        fieldsTableLayoutPanel.RowStyles.Add(height.HasValue
            ? new RowStyle(SizeType.Absolute, height.Value)
            : new RowStyle(SizeType.AutoSize));

        Label label = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 7, 12, 6),
            Text = labelText,
            TextAlign = ContentAlignment.TopLeft
        };
        fieldsTableLayoutPanel.Controls.Add(label, 0, rowIndex);
        fieldsTableLayoutPanel.Controls.Add(control, 1, rowIndex);
    }

    private void UpdateDebuggingHotelContext()
    {
        QaHotelMetadata? hotel = debuggingHotelSelectorControl?.SelectedHotel;

        if (debuggingHotelNameTextBox is not null)
        {
            debuggingHotelNameTextBox.Text = hotel?.HotelName ?? string.Empty;
        }

        if (debuggingHotelIdTextBox is not null)
        {
            debuggingHotelIdTextBox.Text = hotel?.HotelId ?? string.Empty;
        }

        if (debuggingPmsTextBox is not null)
        {
            debuggingPmsTextBox.Text = hotel?.PmsName ?? string.Empty;
        }
    }

    private void PreviewEntry()
    {
        if (!ValidateRequiredFields() || !EnsureDocumentationLogWorkflowAvailable())
        {
            return;
        }

        try
        {
            DocumentationLogEvent preview = documentationLogSaveService!
                .CreatePreview(BuildDocumentationLogSaveRequest());
            previewTextBox.Text = documentationLogPreviewFormatter.Format(preview);
        }
        catch (DocumentationLogSaveException ex)
        {
            ShowDocumentationLogSaveError("Preview could not be generated", ex);
        }
        catch (Exception ex)
        {
            ShowError("Failed to generate preview.", ex);
        }
    }

    private void SubmitEntry()
    {
        if (isSavingDocumentationLog
            || !ValidateRequiredFields()
            || !EnsureDocumentationLogWorkflowAvailable())
        {
            return;
        }

        isSavingDocumentationLog = true;
        submitEntryButton.Enabled = false;

        try
        {
            DocumentationLogSaveResult result = documentationLogSaveService!
                .Save(BuildDocumentationLogSaveRequest());
            SaveSelectedRunningWorkbookPreference();
            string formattedPreview = documentationLogPreviewFormatter.Format(result.Event);
            RenderFieldsForSelectedLogType();
            previewTextBox.Text = formattedPreview;

            string cleanupNotice = string.IsNullOrWhiteSpace(result.CleanupWarning)
                ? string.Empty
                : $"\r\n\r\nCleanup warning: {result.CleanupWarning}";
            MessageBox.Show(
                $"Entry saved successfully.\r\n\r\n"
                + $"Running workbook: {result.RunningWorkbookPath}\r\n"
                + $"Hotel histories updated: {result.HotelHistoryPaths.Count}\r\n"
                + $"PMS histories updated: {result.PmsHistoryPaths.Count}\r\n"
                + $"Index updated: {result.IndexPath}"
                + cleanupNotice,
                "Entry Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (DocumentationLogSaveException ex)
        {
            ShowDocumentationLogSaveError("Entry was not saved", ex);
        }
        catch (Exception ex)
        {
            ShowError("Failed to save entry.", ex);
        }
        finally
        {
            isSavingDocumentationLog = false;
            submitEntryButton.Enabled = documentationLogSaveService is not null;
        }
    }

    private void ClearForm()
    {
        RenderFieldsForSelectedLogType();
    }

    private bool ValidateRequiredFields()
    {
        List<string> missingFields = fieldInputs
            .Where(fieldInput => fieldInput.Key.IsRequired && string.IsNullOrWhiteSpace(fieldInput.Value.Text))
            .Select(fieldInput => fieldInput.Key.Label)
            .ToList();

        if (GetSelectedLogType() == LogType.DebuggingLog
            && debuggingHotelSelectorControl?.SelectedHotel is null)
        {
            missingFields.Insert(0, "Hotel");
        }

        if (runningWorkbookComboBox.SelectedItem is not string)
        {
            missingFields.Insert(0, "Running Log Workbook");
        }

        if (missingFields.Count == 0)
        {
            return true;
        }

        MessageBox.Show(
            "Please complete the following required fields:\r\n\r\n" + string.Join("\r\n", missingFields),
            "Missing Required Fields",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);

        return false;
    }

    private DocumentationLogSaveRequest BuildDocumentationLogSaveRequest()
    {
        Dictionary<string, string> values = fieldInputs.ToDictionary(
            pair => pair.Key.Key,
            pair => pair.Value.Text,
            StringComparer.Ordinal);
        values.TryGetValue(
            DocumentationLogFieldKeys.HotelIds,
            out string? hotelIdsInput);
        QaHotelMetadata? selectedHotel = debuggingHotelSelectorControl?.SelectedHotel;
        QaPmsMetadata? selectedPms = selectedHotel is null
            ? null
            : documentationLogPmsSystems.SingleOrDefault(pms =>
                pms.PmsName.Equals(
                    selectedHotel.PmsName,
                    StringComparison.OrdinalIgnoreCase));

        return new DocumentationLogSaveRequest(
            GetSelectedLogType(),
            (string)runningWorkbookComboBox.SelectedItem!,
            values,
            selectedHotel,
            selectedPms,
            hotelIdsInput);
    }

    private bool EnsureDocumentationLogWorkflowAvailable()
    {
        if (documentationLogSaveService is not null)
        {
            return true;
        }

        ShowError(
            "The Excel documentation-log workflow is unavailable.",
            documentationLogInitializationError
                ?? new InvalidOperationException(
                    "The documentation-log services were not initialized."));
        return false;
    }

    private void HandleSelectedLogTypeChanged()
    {
        RenderFieldsForSelectedLogType();
        RefreshRunningWorkbookChoices();
    }

    private void InitializeDocumentationLogWorkflow()
    {
        documentationLogPaths = null;
        documentationLogFilenameService = null;
        documentationLogWorkbookService = null;
        documentationLogPreferencesService = null;
        documentationLogSaveService = null;
        documentationLogHotels = Array.Empty<QaHotelMetadata>();
        documentationLogPmsSystems = Array.Empty<QaPmsMetadata>();

        try
        {
            string documentationRoot = settingsService.GetDocumentationRootFolder();
            QaStoragePaths qaPaths = new(documentationRoot);
            new QaStorageInitializer(qaPaths).Initialize();
            QaMetadataService metadataService = new(
                qaPaths,
                new QaFolderNameSanitizer());
            DocumentationLogStoragePaths paths = new(qaPaths);
            paths.EnsureBaseDirectories();
            DocumentationLogWorkbookFilenameService filenameService =
                new(paths);
            DocumentationLogWorkbookService workbookService = new(paths);
            DocumentationLogWorkbookPreferencesService preferencesService =
                new(paths, filenameService);
            DocumentationLogSaveService saveService = new(
                paths,
                metadataService);
            IReadOnlyList<QaPmsMetadata> pmsSystems =
                metadataService.LoadPmsSystems();
            IReadOnlyList<QaHotelMetadata> hotels = metadataService.LoadHotels();

            documentationLogPaths = paths;
            documentationLogFilenameService = filenameService;
            documentationLogWorkbookService = workbookService;
            documentationLogPreferencesService = preferencesService;
            documentationLogSaveService = saveService;
            documentationLogPmsSystems = pmsSystems;
            documentationLogHotels = hotels;
            documentationLogInitializationError = null;
        }
        catch (Exception ex)
        {
            documentationLogInitializationError = ex;
        }

        bool available = documentationLogSaveService is not null;
        previewEntryButton.Enabled = available;
        submitEntryButton.Enabled = available;
        createNewLogFileButton.Enabled = documentationLogWorkbookService is not null;
    }

    private void RefreshRunningWorkbookChoices(string? preferredFileName = null)
    {
        isRefreshingRunningWorkbookChoices = true;

        try
        {
            runningWorkbookComboBox.Items.Clear();

            if (documentationLogWorkbookService is null)
            {
                runningWorkbookComboBox.Enabled = false;
                openSelectedLogWorkbookButton.Enabled = false;
                return;
            }

            IReadOnlyList<string> fileNames = documentationLogWorkbookService
                .GetCompatibleRunningWorkbookFileNames(GetSelectedLogType());

            foreach (string fileName in fileNames)
            {
                runningWorkbookComboBox.Items.Add(fileName);
            }

            string? rememberedFileName = null;
            try
            {
                rememberedFileName = documentationLogPreferencesService?
                    .LoadLastUsedWorkbookFileName(GetSelectedLogType());
            }
            catch (DocumentationLogWorkbookPreferencesException ex)
            {
                Debug.WriteLine(ex);
            }

            string? selectedFileName = FindAvailableWorkbook(
                fileNames,
                preferredFileName)
                ?? FindAvailableWorkbook(fileNames, rememberedFileName)
                ?? fileNames.FirstOrDefault();
            runningWorkbookComboBox.SelectedItem = selectedFileName;
            runningWorkbookComboBox.Enabled = true;
            openSelectedLogWorkbookButton.Enabled = selectedFileName is not null;
        }
        catch (Exception ex)
        {
            documentationLogInitializationError = ex;
            runningWorkbookComboBox.Items.Clear();
            runningWorkbookComboBox.Enabled = true;
            openSelectedLogWorkbookButton.Enabled = false;
        }
        finally
        {
            isRefreshingRunningWorkbookChoices = false;
        }
    }

    private static string? FindAvailableWorkbook(
        IReadOnlyList<string> fileNames,
        string? requestedFileName)
    {
        if (string.IsNullOrWhiteSpace(requestedFileName))
        {
            return null;
        }

        return fileNames.FirstOrDefault(fileName => fileName.Equals(
            requestedFileName,
            StringComparison.OrdinalIgnoreCase));
    }

    private void SaveSelectedRunningWorkbookPreference()
    {
        if (isRefreshingRunningWorkbookChoices
            || runningWorkbookComboBox.SelectedItem is not string fileName
            || documentationLogPreferencesService is null)
        {
            return;
        }

        try
        {
            documentationLogPreferencesService.SaveLastUsedWorkbookFileName(
                GetSelectedLogType(),
                fileName);
            openSelectedLogWorkbookButton.Enabled = true;
        }
        catch (DocumentationLogWorkbookPreferencesException ex)
        {
            Debug.WriteLine(ex);
            MessageBox.Show(
                this,
                "The selected workbook is available, but its last-used preference could not be saved.",
                "Workbook Preference Not Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void CreateNewLogFile()
    {
        if (documentationLogWorkbookService is null)
        {
            _ = EnsureDocumentationLogWorkflowAvailable();
            return;
        }

        using DocumentationLogWorkbookNameForm form = new(
            $"{DocumentationLogWorkbookSchema.GetDisplayName(GetSelectedLogType())} Running");
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            string fileName = documentationLogWorkbookService
                .CreateNewRunningWorkbook(
                    GetSelectedLogType(),
                    form.RequestedFileName);
            RefreshRunningWorkbookChoices(fileName);
            runningWorkbookComboBox.SelectedItem = fileName;
            SaveSelectedRunningWorkbookPreference();
        }
        catch (DocumentationLogWorkbookException ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Log Workbook Not Created",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            ShowError("Failed to create the Running log workbook.", ex);
        }
    }

    private void OpenSelectedLogWorkbook()
    {
        try
        {
            if (runningWorkbookComboBox.SelectedItem is not string fileName
                || documentationLogPaths is null
                || documentationLogFilenameService is null)
            {
                MessageBox.Show(
                    "Select or create a compatible Running log workbook first.",
                    "Log Workbook Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string safeFileName = documentationLogFilenameService
                .ValidateStoredWorkbookFileName(fileName);
            string filePath = documentationLogPaths.ResolveRunningWorkbookPath(
                GetSelectedLogType(),
                safeFileName);

            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    "The selected Running log workbook no longer exists. Select or create another workbook.",
                    "Log Workbook Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                RefreshRunningWorkbookChoices();
                return;
            }

            OpenPath(filePath);
        }
        catch (Exception ex)
        {
            ShowError("Failed to open the selected log workbook.", ex);
        }
    }

    private void OpenLogsFolder()
    {
        try
        {
            if (documentationLogPaths is not null)
            {
                documentationLogPaths.EnsureBaseDirectories();
                OpenPath(documentationLogPaths.DocumentationRootPath);
            }
            else
            {
                OpenPath(logFileService.EnsureDocumentationFolderStructure());
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to open logs folder.", ex);
        }
    }

    private void OpenLogIndex()
    {
        try
        {
            string filePath = documentationLogPaths?.LogIndexFilePath
                ?? Path.Combine(logFileService.GetIndexFolderPath(), "LogIndex.txt");

            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    "The log index does not exist yet. Submit an entry first.",
                    "Log Index Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            OpenPath(filePath);
        }
        catch (Exception ex)
        {
            ShowError("Failed to open log index.", ex);
        }
    }

    private void OpenQaMetadataManagement()
    {
        try
        {
            var dependencies = CreateQaWorkflowDependencies();

            using QaMetadataManagementForm form = new(
                dependencies.MetadataService,
                dependencies.PmsSystems,
                dependencies.Hotels);
            form.ShowDialog(this);
            InitializeDocumentationLogWorkflow();
            HandleSelectedLogTypeChanged();
        }
        catch (QaUnsupportedMetadataSchemaException ex)
        {
            ShowQaWorkflowError(
                "QA Metadata Management",
                "QA metadata uses an unsupported version. The existing metadata files were not changed.",
                ex);
        }
        catch (QaStorageInitializationException ex)
        {
            ShowQaWorkflowError(
                "QA Metadata Management",
                "QA storage could not be initialized in the configured documentation folder. Check that the folder is available and writable.",
                ex);
        }
        catch (QaMetadataException ex)
        {
            ShowQaWorkflowError(
                "QA Metadata Management",
                "QA metadata could not be loaded because a metadata file is invalid or inaccessible. The existing file was not changed.",
                ex);
        }
        catch (Exception ex) when (ex is ArgumentException
            or NotSupportedException
            or PathTooLongException
            or InvalidOperationException)
        {
            ShowQaWorkflowError(
                "QA Metadata Management",
                "The configured documentation folder could not be used for QA setup.",
                ex);
        }
        catch (Exception ex)
        {
            ShowQaWorkflowError(
                "QA Metadata Management",
                "QA metadata management could not be opened.",
                ex);
        }
    }

    private void OpenDetailedQaReport()
    {
        try
        {
            var dependencies = CreateQaWorkflowDependencies();

            using QaReportForm form = new(
                dependencies.MetadataService,
                dependencies.PmsSystems,
                dependencies.Hotels,
                dependencies.Paths);
            form.ShowDialog(this);
        }
        catch (QaUnsupportedMetadataSchemaException ex)
        {
            ShowQaWorkflowError(
                "Detailed QA Report",
                "QA metadata uses an unsupported version. The existing metadata files were not changed.",
                ex);
        }
        catch (QaStorageInitializationException ex)
        {
            ShowQaWorkflowError(
                "Detailed QA Report",
                "QA storage could not be initialized in the configured documentation folder. Check that the folder is available and writable.",
                ex);
        }
        catch (QaMetadataException ex)
        {
            ShowQaWorkflowError(
                "Detailed QA Report",
                "QA metadata could not be loaded because a metadata file is invalid or inaccessible. The existing file was not changed.",
                ex);
        }
        catch (Exception ex) when (ex is ArgumentException
            or NotSupportedException
            or PathTooLongException
            or InvalidOperationException)
        {
            ShowQaWorkflowError(
                "Detailed QA Report",
                "The configured documentation folder could not be used for QA setup.",
                ex);
        }
        catch (Exception ex)
        {
            ShowQaWorkflowError(
                "Detailed QA Report",
                "The Detailed QA Report form could not be opened.",
                ex);
        }
    }

    private void OpenQuickQa()
    {
        try
        {
            var dependencies = CreateQaWorkflowDependencies();

            using QuickQaForm form = new(
                dependencies.MetadataService,
                dependencies.PmsSystems,
                dependencies.Hotels,
                dependencies.Paths);
            form.ShowDialog(this);
        }
        catch (QaUnsupportedMetadataSchemaException ex)
        {
            ShowQaWorkflowError(
                "Quick QA",
                "QA metadata uses an unsupported version. The existing metadata files were not changed.",
                ex);
        }
        catch (QaStorageInitializationException ex)
        {
            ShowQaWorkflowError(
                "Quick QA",
                "QA storage could not be initialized in the configured documentation folder. Check that the folder is available and writable.",
                ex);
        }
        catch (QaMetadataException ex)
        {
            ShowQaWorkflowError(
                "Quick QA",
                "QA metadata could not be loaded because a metadata file is invalid or inaccessible. The existing file was not changed.",
                ex);
        }
        catch (Exception ex) when (ex is ArgumentException
            or NotSupportedException
            or PathTooLongException
            or InvalidOperationException)
        {
            ShowQaWorkflowError(
                "Quick QA",
                "The configured documentation folder could not be used for QA setup.",
                ex);
        }
        catch (Exception ex)
        {
            ShowQaWorkflowError(
                "Quick QA",
                "The Quick QA form could not be opened.",
                ex);
        }
    }

    private (
        QaMetadataService MetadataService,
        IReadOnlyList<QaPmsMetadata> PmsSystems,
        IReadOnlyList<QaHotelMetadata> Hotels,
        QaStoragePaths Paths) CreateQaWorkflowDependencies()
    {
        string documentationRoot = settingsService.GetDocumentationRootFolder();
        QaStoragePaths paths = new(documentationRoot);
        QaStorageInitializer initializer = new(paths);
        initializer.Initialize();

        QaFolderNameSanitizer folderNameSanitizer = new();
        QaMetadataService metadataService = new(paths, folderNameSanitizer);
        IReadOnlyList<QaPmsMetadata> pmsSystems = metadataService.LoadPmsSystems();
        IReadOnlyList<QaHotelMetadata> hotels = metadataService.LoadHotels();

        return (metadataService, pmsSystems, hotels, paths);
    }

    private void ChangeLogsFolder()
    {
        using FolderBrowserDialog folderBrowserDialog = new()
        {
            Description = "Select the folder where documentation logs should be saved.",
            UseDescriptionForTitle = true
        };

        string currentFolder = logFileService.GetDocumentationRootFolder();

        if (Directory.Exists(currentFolder))
        {
            folderBrowserDialog.SelectedPath = currentFolder;
        }

        DialogResult result;

        try
        {
            result = folderBrowserDialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            ShowError("Failed to open the folder picker.", ex);
            return;
        }

        if (result != DialogResult.OK)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
        {
            MessageBox.Show(
                "Please select a valid folder.",
                "Folder Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        string selectedFolder;

        try
        {
            selectedFolder = Path.GetFullPath(folderBrowserDialog.SelectedPath);
        }
        catch (Exception ex)
        {
            ShowError("The selected folder path is invalid.", ex);
            return;
        }

        try
        {
            logFileService.EnsureDocumentationFolderStructure(selectedFolder);
        }
        catch (Exception ex)
        {
            ShowError("Failed to create or access the selected logs folder.", ex);
            return;
        }

        try
        {
            settingsService.UpdateDocumentationRootFolder(selectedFolder);
        }
        catch (Exception ex)
        {
            ShowError("Failed to save the selected logs folder setting.", ex);
            return;
        }

        RefreshDocumentationRootFolderDisplay();
        InitializeDocumentationLogWorkflow();
        HandleSelectedLogTypeChanged();

        MessageBox.Show(
            $"Logs folder updated successfully.\r\n\r\n{selectedFolder}",
            "Logs Folder Updated",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ResetToDefaultFolder()
    {
        string defaultFolder;

        try
        {
            defaultFolder = settingsService.ResolveDocumentationRootFolder(SettingsService.DefaultDocumentationRootFolder);
            logFileService.EnsureDocumentationFolderStructure(defaultFolder);
        }
        catch (Exception ex)
        {
            ShowError("Failed to create or access the default logs folder.", ex);
            return;
        }

        try
        {
            settingsService.UpdateDocumentationRootFolder(SettingsService.DefaultDocumentationRootFolder);
        }
        catch (Exception ex)
        {
            ShowError("Failed to save the default logs folder setting.", ex);
            return;
        }

        RefreshDocumentationRootFolderDisplay();
        InitializeDocumentationLogWorkflow();
        HandleSelectedLogTypeChanged();

        MessageBox.Show(
            $"Logs folder reset successfully.\r\n\r\n{defaultFolder}",
            "Logs Folder Reset",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void RefreshDocumentationRootFolderDisplay()
    {
        documentationRootFolderPathLabel.Text = logFileService.GetDocumentationRootFolder();
    }

    private static void OpenPath(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void ShowQaWorkflowError(
        string title,
        string message,
        Exception exception)
    {
        Debug.WriteLine(exception);
        MessageBox.Show(
            this,
            message,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void ShowDocumentationLogSaveError(
        string title,
        DocumentationLogSaveException exception)
    {
        Debug.WriteLine(exception);
        string restorationText = exception.ManualReviewRequired
            ? "\r\n\r\nRollback could not fully restore every destination. Manual review is required."
            : exception.PreviousStateRestored
                ? "\r\n\r\nThe previous file state was restored."
                : string.Empty;
        string locationText = exception.ManualReviewLocations.Count == 0
            ? string.Empty
            : "\r\n\r\nReview:\r\n"
                + string.Join("\r\n", exception.ManualReviewLocations);
        string affectedText = exception.AffectedPath is null
            ? string.Empty
            : $"\r\n\r\nAffected file: {exception.AffectedPath}";

        MessageBox.Show(
            this,
            exception.Message + restorationText + affectedText + locationText,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void ShowError(string message, Exception ex)
    {
        MessageBox.Show(
            $"{message}\r\nError: {ex.Message}",
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private LogType GetSelectedLogType()
    {
        return logTypeComboBox.SelectedItem is LogTypeOption selectedOption
            ? selectedOption.LogType
            : LogType.DebuggingLog;
    }

    private sealed record LogTypeOption(LogType LogType, string DisplayName);
}
