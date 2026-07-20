using System.Diagnostics;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.QAReports.Forms;

/// <summary>
/// Collects the required values for one new hotel metadata record.
/// </summary>
public partial class AddHotelForm : Form
{
    private readonly QaMetadataService metadataService;
    private readonly IReadOnlyList<QaPmsMetadata> pmsSystems;
    private bool isRebindingPmsSystems;

    public AddHotelForm(
        QaMetadataService metadataService,
        IReadOnlyList<QaPmsMetadata> pmsSystems)
    {
        this.metadataService = metadataService
            ?? throw new ArgumentNullException(nameof(metadataService));
        this.pmsSystems = CreatePmsSnapshot(pmsSystems);

        InitializeComponent();
        ConfigurePmsList();
        WireEvents();
        ApplyPmsFilter(preserveSelection: false);
    }

    public QaHotelMetadata? CreatedHotel { get; private set; }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        hotelIdTextBox.Focus();
    }

    private static IReadOnlyList<QaPmsMetadata> CreatePmsSnapshot(
        IReadOnlyList<QaPmsMetadata> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        QaPmsMetadata[] snapshot = new QaPmsMetadata[source.Count];

        for (int index = 0; index < source.Count; index++)
        {
            QaPmsMetadata pmsSystem = source[index];

            if (pmsSystem is null)
            {
                throw new ArgumentException(
                    "The PMS metadata snapshot cannot contain a null record.",
                    nameof(source));
            }

            snapshot[index] = new QaPmsMetadata
            {
                PmsName = pmsSystem.PmsName,
                FolderName = pmsSystem.FolderName
            };
        }

        return snapshot;
    }

    private void ConfigurePmsList()
    {
        pmsListBox.DisplayMember = nameof(QaPmsMetadata.PmsName);
    }

    private void WireEvents()
    {
        hotelIdTextBox.TextChanged += (_, _) => UpdateAddButtonEnabled();
        hotelNameTextBox.TextChanged += (_, _) => UpdateAddButtonEnabled();
        pmsSearchTextBox.TextChanged += (_, _) => ApplyPmsFilter(preserveSelection: true);
        pmsListBox.SelectedIndexChanged += (_, _) => UpdateAddButtonEnabled();
        addButton.Click += (_, _) => AddHotel();
    }

    private void ApplyPmsFilter(bool preserveSelection)
    {
        string? selectedPmsName = preserveSelection
            ? (pmsListBox.SelectedItem as QaPmsMetadata)?.PmsName
            : null;
        IReadOnlyList<QaPmsMetadata> filteredPmsSystems =
            QaMetadataSearch.FilterPmsSystems(pmsSystems, pmsSearchTextBox.Text);

        isRebindingPmsSystems = true;

        try
        {
            pmsListBox.DataSource = null;
            pmsListBox.DisplayMember = nameof(QaPmsMetadata.PmsName);
            pmsListBox.DataSource = filteredPmsSystems.ToArray();
            pmsListBox.SelectedIndex = FindPmsIndex(filteredPmsSystems, selectedPmsName);
        }
        finally
        {
            isRebindingPmsSystems = false;
        }

        pmsEmptyStateLabel.Text = pmsSystems.Count == 0
            ? "No PMS systems are available. Close this dialog and add a PMS system first."
            : "No PMS systems match the current search.";
        pmsEmptyStateLabel.Visible = filteredPmsSystems.Count == 0;
        UpdateAddButtonEnabled();
    }

    private static int FindPmsIndex(
        IReadOnlyList<QaPmsMetadata> source,
        string? pmsName)
    {
        if (string.IsNullOrWhiteSpace(pmsName))
        {
            return -1;
        }

        for (int index = 0; index < source.Count; index++)
        {
            if (source[index].PmsName.Equals(
                    pmsName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void UpdateAddButtonEnabled()
    {
        addButton.Enabled = !isRebindingPmsSystems
            && !string.IsNullOrWhiteSpace(hotelIdTextBox.Text)
            && !string.IsNullOrWhiteSpace(hotelNameTextBox.Text)
            && pmsListBox.SelectedItem is QaPmsMetadata;
    }

    private void AddHotel()
    {
        if (string.IsNullOrWhiteSpace(hotelIdTextBox.Text))
        {
            hotelIdTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(hotelNameTextBox.Text))
        {
            hotelNameTextBox.Focus();
            return;
        }

        if (pmsListBox.SelectedItem is not QaPmsMetadata selectedPms)
        {
            pmsListBox.Focus();
            return;
        }

        try
        {
            QaHotelMetadata created = metadataService.AddHotel(
                hotelIdTextBox.Text,
                hotelNameTextBox.Text,
                selectedPms.PmsName);

            CreatedHotel = created;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (QaDuplicateMetadataException exception)
        {
            ShowAddError(
                exception,
                "A hotel with this Hotel ID, or with the same generated folder name, already exists.",
                hotelIdTextBox,
                selectAllText: true);
        }
        catch (QaUnsupportedMetadataSchemaException exception)
        {
            ShowAddError(
                exception,
                "QA metadata uses an unsupported version. The existing file was not changed.",
                hotelIdTextBox);
        }
        catch (ArgumentException exception)
            when (string.Equals(exception.ParamName, "pmsName", StringComparison.Ordinal))
        {
            pmsListBox.SelectedIndex = -1;
            ShowAddError(
                exception,
                "The selected PMS is no longer available. Refresh the metadata and select an existing PMS.",
                pmsListBox);
        }
        catch (ArgumentException exception)
        {
            ShowAddError(
                exception,
                "The Hotel ID or Hotel Name is invalid or does not produce a safe folder name.",
                hotelIdTextBox,
                selectAllText: true);
        }
        catch (QaMetadataException exception)
        {
            ShowAddError(
                exception,
                "The hotel could not be saved to the configured documentation folder.",
                hotelIdTextBox);
        }
        catch (Exception exception)
        {
            ShowAddError(
                exception,
                "The hotel could not be added.",
                hotelIdTextBox);
        }
    }

    private void ShowAddError(
        Exception exception,
        string message,
        Control focusControl,
        bool selectAllText = false)
    {
        Debug.WriteLine(exception);
        MessageBox.Show(
            this,
            message,
            "Unable to Add Hotel",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);

        focusControl.Focus();

        if (selectAllText && focusControl is TextBox textBox)
        {
            textBox.SelectAll();
        }

        UpdateAddButtonEnabled();
    }
}
