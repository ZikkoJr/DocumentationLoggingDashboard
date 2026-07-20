using System.Diagnostics;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.QAReports.Forms;

public partial class AddPmsForm : Form
{
    private readonly QaMetadataService metadataService;

    public AddPmsForm(QaMetadataService metadataService)
    {
        this.metadataService = metadataService
            ?? throw new ArgumentNullException(nameof(metadataService));

        InitializeComponent();
        WireEvents();
        UpdateAddButtonState();
    }

    public QaPmsMetadata? CreatedPms { get; private set; }

    private void WireEvents()
    {
        pmsNameTextBox.TextChanged += PmsNameTextBox_TextChanged;
        addButton.Click += AddButton_Click;
        Shown += AddPmsForm_Shown;
    }

    private void PmsNameTextBox_TextChanged(object? sender, EventArgs e)
    {
        UpdateAddButtonState();
    }

    private void AddPmsForm_Shown(object? sender, EventArgs e)
    {
        pmsNameTextBox.Focus();
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(pmsNameTextBox.Text))
        {
            UpdateAddButtonState();
            pmsNameTextBox.Focus();
            return;
        }

        try
        {
            addButton.Enabled = false;

            QaPmsMetadata created = metadataService.AddPmsSystem(pmsNameTextBox.Text);

            CreatedPms = created;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (QaDuplicateMetadataException exception)
        {
            ShowAddError(
                "A PMS system with this name, or with the same generated folder name, already exists.",
                exception);
        }
        catch (QaUnsupportedMetadataSchemaException exception)
        {
            ShowAddError(
                "QA metadata uses an unsupported version. The existing file was not changed.",
                exception);
        }
        catch (ArgumentException exception)
        {
            ShowAddError(
                "The PMS name is invalid or does not produce a safe folder name.",
                exception);
        }
        catch (QaMetadataException exception)
        {
            ShowAddError(
                "The PMS system could not be saved to the configured documentation folder.",
                exception);
        }
        catch (Exception exception)
        {
            ShowAddError("The PMS system could not be added.", exception);
        }
        finally
        {
            if (DialogResult != DialogResult.OK)
            {
                UpdateAddButtonState();
            }
        }
    }

    private void ShowAddError(string message, Exception exception)
    {
        Debug.WriteLine(exception);

        MessageBox.Show(
            this,
            message,
            "Unable to Add PMS System",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);

        pmsNameTextBox.Focus();
        pmsNameTextBox.SelectAll();
    }

    private void UpdateAddButtonState()
    {
        addButton.Enabled = !string.IsNullOrWhiteSpace(pmsNameTextBox.Text);
    }
}
