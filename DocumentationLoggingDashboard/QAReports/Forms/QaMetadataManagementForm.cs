using System.Diagnostics;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.QAReports.Forms;

public partial class QaMetadataManagementForm : Form
{
    private readonly QaMetadataService metadataService;
    private IReadOnlyList<QaPmsMetadata> pmsSystems;
    private IReadOnlyList<QaHotelMetadata> hotels;
    private bool isRebinding;

    public QaMetadataManagementForm(
        QaMetadataService metadataService,
        IReadOnlyList<QaPmsMetadata> initialPmsSystems,
        IReadOnlyList<QaHotelMetadata> initialHotels)
    {
        this.metadataService = metadataService
            ?? throw new ArgumentNullException(nameof(metadataService));
        ArgumentNullException.ThrowIfNull(initialPmsSystems);
        ArgumentNullException.ThrowIfNull(initialHotels);

        pmsSystems = initialPmsSystems.ToArray();
        hotels = initialHotels.ToArray();

        InitializeComponent();
        ConfigureLists();
        WireEvents();
        RebindAllLists();
    }

    private void ConfigureLists()
    {
        pmsListBox.DisplayMember = nameof(QaPmsMetadata.PmsName);
    }

    private void WireEvents()
    {
        pmsSearchTextBox.TextChanged += (_, _) =>
            RebindPmsList(GetSelectedPmsName());
        hotelSearchTextBox.TextChanged += (_, _) =>
            RebindHotelList(GetSelectedHotelId());
        pmsListBox.SelectedIndexChanged += (_, _) => UpdatePmsDetails();
        hotelListBox.SelectedIndexChanged += (_, _) => UpdateHotelDetails();
        hotelListBox.Format += FormatHotelListItem;
        addPmsButton.Click += (_, _) => AddPmsSystem();
        addHotelButton.Click += (_, _) => AddHotel();
        refreshButton.Click += (_, _) => TryRefreshMetadata();
    }

    private void RebindAllLists(
        string? preferredPmsName = null,
        string? preferredHotelId = null)
    {
        isRebinding = true;

        try
        {
            RebindPmsListCore(preferredPmsName);
            RebindHotelListCore(preferredHotelId);
        }
        finally
        {
            isRebinding = false;
        }

        UpdatePmsDetails();
        UpdateHotelDetails();
    }

    private void RebindPmsList(string? preferredPmsName)
    {
        isRebinding = true;

        try
        {
            RebindPmsListCore(preferredPmsName);
        }
        finally
        {
            isRebinding = false;
        }

        UpdatePmsDetails();
    }

    private void RebindPmsListCore(string? preferredPmsName)
    {
        IReadOnlyList<QaPmsMetadata> filteredPmsSystems =
            QaMetadataSearch.FilterPmsSystems(pmsSystems, pmsSearchTextBox.Text);

        pmsListBox.BeginUpdate();

        try
        {
            pmsListBox.DataSource = null;
            pmsListBox.DataSource = filteredPmsSystems.ToArray();
            SelectPms(
                preferredPmsName,
                selectFirstWhenMissing: string.IsNullOrWhiteSpace(preferredPmsName)
                    && filteredPmsSystems.Count > 0);
        }
        finally
        {
            pmsListBox.EndUpdate();
        }

        if (pmsSystems.Count == 0)
        {
            pmsEmptyStateLabel.Text =
                "No PMS systems have been added. Add a PMS system to begin.";
            pmsEmptyStateLabel.Visible = true;
        }
        else
        {
            pmsEmptyStateLabel.Text = "No PMS systems match the current search.";
            pmsEmptyStateLabel.Visible = filteredPmsSystems.Count == 0;
        }

        addHotelButton.Enabled = pmsSystems.Count > 0;
    }

    private void RebindHotelList(string? preferredHotelId)
    {
        isRebinding = true;

        try
        {
            RebindHotelListCore(preferredHotelId);
        }
        finally
        {
            isRebinding = false;
        }

        UpdateHotelDetails();
    }

    private void RebindHotelListCore(string? preferredHotelId)
    {
        IReadOnlyList<QaHotelMetadata> filteredHotels =
            QaMetadataSearch.FilterHotels(hotels, hotelSearchTextBox.Text);

        hotelListBox.BeginUpdate();

        try
        {
            hotelListBox.DataSource = null;
            hotelListBox.DataSource = filteredHotels.ToArray();
            SelectHotel(
                preferredHotelId,
                selectFirstWhenMissing: string.IsNullOrWhiteSpace(preferredHotelId)
                    && filteredHotels.Count > 0);
        }
        finally
        {
            hotelListBox.EndUpdate();
        }

        if (pmsSystems.Count == 0)
        {
            hotelEmptyStateLabel.Text =
                "Add a PMS system before adding a hotel.";
            hotelEmptyStateLabel.Visible = true;
        }
        else if (hotels.Count == 0)
        {
            hotelEmptyStateLabel.Text = "No hotels have been added.";
            hotelEmptyStateLabel.Visible = true;
        }
        else
        {
            hotelEmptyStateLabel.Text = "No hotels match the current search.";
            hotelEmptyStateLabel.Visible = filteredHotels.Count == 0;
        }

        addHotelButton.Enabled = pmsSystems.Count > 0;
    }

    private void SelectPms(string? preferredPmsName, bool selectFirstWhenMissing)
    {
        int preferredIndex = FindPmsIndex(preferredPmsName);

        if (preferredIndex >= 0)
        {
            pmsListBox.SelectedIndex = preferredIndex;
        }
        else if (selectFirstWhenMissing && pmsListBox.Items.Count > 0)
        {
            pmsListBox.SelectedIndex = 0;
        }
        else
        {
            pmsListBox.SelectedIndex = -1;
        }
    }

    private int FindPmsIndex(string? pmsName)
    {
        if (string.IsNullOrWhiteSpace(pmsName))
        {
            return -1;
        }

        for (int index = 0; index < pmsListBox.Items.Count; index++)
        {
            if (pmsListBox.Items[index] is QaPmsMetadata pms
                && pms.PmsName.Equals(pmsName, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void SelectHotel(string? preferredHotelId, bool selectFirstWhenMissing)
    {
        int preferredIndex = FindHotelIndex(preferredHotelId);

        if (preferredIndex >= 0)
        {
            hotelListBox.SelectedIndex = preferredIndex;
        }
        else if (selectFirstWhenMissing && hotelListBox.Items.Count > 0)
        {
            hotelListBox.SelectedIndex = 0;
        }
        else
        {
            hotelListBox.SelectedIndex = -1;
        }
    }

    private int FindHotelIndex(string? hotelId)
    {
        if (string.IsNullOrWhiteSpace(hotelId))
        {
            return -1;
        }

        for (int index = 0; index < hotelListBox.Items.Count; index++)
        {
            if (hotelListBox.Items[index] is QaHotelMetadata hotel
                && hotel.HotelId.Equals(hotelId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void UpdatePmsDetails()
    {
        if (isRebinding)
        {
            return;
        }

        if (pmsListBox.SelectedItem is QaPmsMetadata selectedPms)
        {
            selectedPmsNameTextBox.Text = selectedPms.PmsName;
            selectedPmsFolderNameTextBox.Text = selectedPms.FolderName;
            return;
        }

        selectedPmsNameTextBox.Clear();
        selectedPmsFolderNameTextBox.Clear();
    }

    private void UpdateHotelDetails()
    {
        if (isRebinding)
        {
            return;
        }

        if (hotelListBox.SelectedItem is not QaHotelMetadata selectedHotel)
        {
            selectedHotelIdTextBox.Clear();
            selectedHotelNameTextBox.Clear();
            selectedHotelPmsTextBox.Clear();
            selectedHotelFolderNameTextBox.Clear();
            return;
        }

        selectedHotelIdTextBox.Text = selectedHotel.HotelId;
        selectedHotelNameTextBox.Text = selectedHotel.HotelName;
        selectedHotelPmsTextBox.Text = selectedHotel.PmsName;
        selectedHotelFolderNameTextBox.Text = selectedHotel.FolderName;
    }

    private bool TryRefreshMetadata(
        string? preferredPmsName = null,
        string? preferredHotelId = null,
        string? savedItemDescription = null)
    {
        preferredPmsName ??= GetSelectedPmsName();
        preferredHotelId ??= GetSelectedHotelId();

        try
        {
            IReadOnlyList<QaPmsMetadata> refreshedPmsSystems =
                metadataService.LoadPmsSystems().ToArray();
            IReadOnlyList<QaHotelMetadata> refreshedHotels =
                metadataService.LoadHotels().ToArray();

            pmsSystems = refreshedPmsSystems;
            hotels = refreshedHotels;
            RebindAllLists(preferredPmsName, preferredHotelId);
            return true;
        }
        catch (QaUnsupportedMetadataSchemaException exception)
        {
            ShowRefreshError(
                exception,
                "QA metadata uses an unsupported version. The existing metadata files were not changed.",
                savedItemDescription);
        }
        catch (QaMetadataException exception)
        {
            ShowRefreshError(
                exception,
                "QA metadata could not be refreshed because a metadata file is invalid or inaccessible. The existing file was not changed.",
                savedItemDescription);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException
                or InvalidOperationException)
        {
            ShowRefreshError(
                exception,
                "QA metadata could not be refreshed because the configured documentation folder could not be used.",
                savedItemDescription);
        }
        catch (Exception exception)
        {
            ShowRefreshError(
                exception,
                "QA metadata could not be refreshed.",
                savedItemDescription);
        }

        return false;
    }

    private void AddPmsSystem()
    {
        using AddPmsForm form = new(metadataService);

        if (form.ShowDialog(this) == DialogResult.OK && form.CreatedPms is not null)
        {
            TryRefreshMetadata(
                preferredPmsName: form.CreatedPms.PmsName,
                savedItemDescription: "The PMS system");
        }
    }

    private void AddHotel()
    {
        if (pmsSystems.Count == 0)
        {
            MessageBox.Show(
                this,
                "Add a PMS system before adding a hotel.",
                "PMS Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using AddHotelForm form = new(metadataService, pmsSystems.ToArray());

        if (form.ShowDialog(this) == DialogResult.OK && form.CreatedHotel is not null)
        {
            TryRefreshMetadata(
                preferredHotelId: form.CreatedHotel.HotelId,
                savedItemDescription: "The hotel");
        }
    }

    private string? GetSelectedPmsName()
    {
        return pmsListBox.SelectedItem is QaPmsMetadata selectedPms
            ? selectedPms.PmsName
            : null;
    }

    private string? GetSelectedHotelId()
    {
        return hotelListBox.SelectedItem is QaHotelMetadata selectedHotel
            ? selectedHotel.HotelId
            : null;
    }

    private void FormatHotelListItem(object? sender, ListControlConvertEventArgs eventArgs)
    {
        if (eventArgs.ListItem is QaHotelMetadata hotel)
        {
            eventArgs.Value = QaMetadataSearch.GetHotelDisplayText(hotel);
        }
    }

    private void ShowRefreshError(
        Exception exception,
        string failureMessage,
        string? savedItemDescription)
    {
        Debug.WriteLine(exception);

        string message = savedItemDescription is null
            ? failureMessage
            : $"{savedItemDescription} was saved, but the display could not be refreshed. {failureMessage} The previous metadata view is still shown.";

        MessageBox.Show(
            this,
            message,
            "QA Metadata Refresh Failed",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
