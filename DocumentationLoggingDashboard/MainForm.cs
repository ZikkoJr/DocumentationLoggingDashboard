using System.Diagnostics;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.Services;

namespace DocumentationLoggingDashboard;

public partial class MainForm : Form
{
    private readonly LogTemplateService logTemplateService = new();
    private readonly SettingsService settingsService = new();
    private readonly LogFileService logFileService;
    private readonly LogIdService logIdService;
    private readonly LogIndexService logIndexService;
    private readonly Dictionary<LogFieldDefinition, TextBox> fieldInputs = new();

    public MainForm()
    {
        logFileService = new LogFileService(settingsService, logTemplateService);
        logIdService = new LogIdService(logTemplateService, logFileService);
        logIndexService = new LogIndexService(logFileService, logTemplateService);

        InitializeComponent();
        ConfigureLogTypeDropdown();
        WireButtonEvents();
        RefreshDocumentationRootFolderDisplay();
        RenderFieldsForSelectedLogType();
    }

    private void ConfigureLogTypeDropdown()
    {
        logTypeComboBox.DisplayMember = nameof(LogTypeOption.DisplayName);
        logTypeComboBox.ValueMember = nameof(LogTypeOption.LogType);

        foreach (LogType logType in logTemplateService.GetSupportedLogTypes())
        {
            logTypeComboBox.Items.Add(new LogTypeOption(logType, logTemplateService.GetDisplayName(logType)));
        }

        logTypeComboBox.SelectedIndexChanged += (_, _) => RenderFieldsForSelectedLogType();
        logTypeComboBox.SelectedIndex = 0;
    }

    private void WireButtonEvents()
    {
        previewEntryButton.Click += (_, _) => PreviewEntry();
        submitEntryButton.Click += (_, _) => SubmitEntry();
        clearFormButton.Click += (_, _) => ClearForm();
        openTodaysLogFileButton.Click += (_, _) => OpenTodaysLogFile();
        openLogsFolderButton.Click += (_, _) => OpenLogsFolder();
        openLogIndexButton.Click += (_, _) => OpenLogIndex();
        changeLogsFolderButton.Click += (_, _) => ChangeLogsFolder();
        resetDefaultFolderButton.Click += (_, _) => ResetToDefaultFolder();
    }

    private void RenderFieldsForSelectedLogType()
    {
        fieldsTableLayoutPanel.SuspendLayout();
        fieldsTableLayoutPanel.Controls.Clear();
        fieldsTableLayoutPanel.RowStyles.Clear();
        fieldsTableLayoutPanel.RowCount = 0;
        fieldInputs.Clear();

        foreach (LogFieldDefinition field in logTemplateService.GetFields(GetSelectedLogType()))
        {
            AddFieldRow(field);
        }

        fieldsTableLayoutPanel.ResumeLayout();
        previewTextBox.Clear();
    }

    private void AddFieldRow(LogFieldDefinition field)
    {
        int rowIndex = fieldsTableLayoutPanel.RowCount++;
        fieldsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label label = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 7, 12, 6),
            Text = field.IsRequired ? $"{field.Label} *" : field.Label,
            TextAlign = ContentAlignment.TopLeft
        };

        TextBox textBox = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 3, 0, 8),
            Multiline = field.IsMultiline,
            ScrollBars = field.IsMultiline ? ScrollBars.Vertical : ScrollBars.None,
            Height = field.IsMultiline ? 84 : 27
        };

        fieldsTableLayoutPanel.Controls.Add(label, 0, rowIndex);
        fieldsTableLayoutPanel.Controls.Add(textBox, 1, rowIndex);
        fieldInputs[field] = textBox;
    }

    private void PreviewEntry()
    {
        if (!ValidateRequiredFields())
        {
            return;
        }

        try
        {
            UpdatePreview();
        }
        catch (Exception ex)
        {
            ShowError("Failed to generate preview.", ex);
        }
    }

    private void SubmitEntry()
    {
        if (!ValidateRequiredFields())
        {
            return;
        }

        try
        {
            DateTime entryDateTime = DateTime.Now;
            LogEntry entry = BuildLogEntryFromInputs(logIdService.GenerateNextLogId(GetSelectedLogType(), entryDateTime), entryDateTime);
            string formattedEntry = logTemplateService.FormatEntry(entry);
            string savedFilePath = logFileService.SaveEntry(entry, formattedEntry);
            string indexFilePath;

            try
            {
                indexFilePath = logIndexService.AppendEntry(entry, savedFilePath);
            }
            catch (Exception ex)
            {
                previewTextBox.Text = formattedEntry;
                MessageBox.Show(
                    $"Entry was saved to the daily log, but the index could not be updated.\r\n\r\nDaily log: {savedFilePath}\r\n\r\nIndex error: {ex.Message}",
                    "Index Update Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                ClearForm();
                return;
            }

            previewTextBox.Text = formattedEntry;
            MessageBox.Show(
                $"Entry saved successfully.\r\n\r\nDaily log: {savedFilePath}\r\n\r\nIndex updated: {indexFilePath}",
                "Entry Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ClearForm();
        }
        catch (Exception ex)
        {
            ShowError("Failed to save entry.", ex);
        }
    }

    private void ClearForm()
    {
        foreach (TextBox textBox in fieldInputs.Values)
        {
            textBox.Clear();
        }

        previewTextBox.Clear();
    }

    private bool ValidateRequiredFields()
    {
        List<string> missingFields = fieldInputs
            .Where(fieldInput => fieldInput.Key.IsRequired && string.IsNullOrWhiteSpace(fieldInput.Value.Text))
            .Select(fieldInput => fieldInput.Key.Label)
            .ToList();

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

    private void UpdatePreview()
    {
        DateTime previewDateTime = DateTime.Now;
        string previewLogId = logIdService.GenerateNextLogId(GetSelectedLogType(), previewDateTime);
        previewTextBox.Text = logTemplateService.FormatEntry(BuildLogEntryFromInputs(previewLogId, previewDateTime));
    }

    private LogEntry BuildLogEntryFromInputs(string logId, DateTime dateTime)
    {
        LogEntry entry = new()
        {
            LogType = GetSelectedLogType(),
            LogId = logId,
            DateTime = dateTime
        };

        foreach ((LogFieldDefinition field, TextBox textBox) in fieldInputs)
        {
            string value = textBox.Text.Trim();

            if (field.Key == LogTemplateService.CreatedByKey)
            {
                entry.CreatedBy = value;
            }
            else if (field.Key == LogTemplateService.NotesFollowUpKey)
            {
                entry.NotesFollowUp = value;
            }
            else
            {
                entry.FieldValues[field.Key] = value;
            }
        }

        return entry;
    }

    private void OpenTodaysLogFile()
    {
        try
        {
            string filePath = logFileService.GetDailyLogFilePath(GetSelectedLogType(), DateTime.Now);

            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    "Today's log file does not exist yet. Submit an entry first.",
                    "Log File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            OpenPath(filePath);
        }
        catch (Exception ex)
        {
            ShowError("Failed to open today's log file.", ex);
        }
    }

    private void OpenLogsFolder()
    {
        try
        {
            OpenPath(logFileService.EnsureDocumentationFolderStructure());
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
            string filePath = logIndexService.GetIndexFilePath();

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
