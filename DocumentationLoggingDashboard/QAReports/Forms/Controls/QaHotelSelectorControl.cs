using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

/// <summary>
/// Selects an existing hotel metadata record without treating search text as report data.
/// </summary>
public partial class QaHotelSelectorControl : UserControl
{
    private IReadOnlyList<QaHotelMetadata> hotels = Array.Empty<QaHotelMetadata>();
    private QaHotelMetadata? selectedHotel;
    private bool isRebinding;

    public QaHotelSelectorControl()
    {
        InitializeComponent();

        searchTextBox.TextChanged += (_, _) =>
            RebindHotels(selectedHotel?.HotelId);
        hotelListBox.SelectedIndexChanged += (_, _) =>
            UpdateSelectedHotelFromList();
        hotelListBox.Format += FormatHotelListItem;

        RebindHotels(preferredHotelId: null);
    }

    /// <summary>
    /// Gets the real metadata record selected from the filtered list.
    /// </summary>
    public QaHotelMetadata? SelectedHotel => selectedHotel;

    public event EventHandler? SelectedHotelChanged;

    /// <summary>
    /// Replaces the available metadata snapshot and restores a preferred hotel only when
    /// that hotel remains in the current filtered view.
    /// </summary>
    public void SetHotels(
        IReadOnlyList<QaHotelMetadata> hotels,
        string? preferredHotelId = null)
    {
        ArgumentNullException.ThrowIfNull(hotels);

        string? hotelIdToPreserve = preferredHotelId ?? selectedHotel?.HotelId;
        this.hotels = hotels.ToArray();
        RebindHotels(hotelIdToPreserve);
    }

    private void RebindHotels(string? preferredHotelId)
    {
        QaHotelMetadata? previousSelection = selectedHotel;
        IReadOnlyList<QaHotelMetadata> filteredHotels =
            QaMetadataSearch.FilterHotels(hotels, searchTextBox.Text);
        int preferredIndex = FindHotelIndex(filteredHotels, preferredHotelId);

        isRebinding = true;
        hotelListBox.BeginUpdate();

        try
        {
            hotelListBox.DataSource = null;
            hotelListBox.DataSource = filteredHotels.ToArray();
            hotelListBox.SelectedIndex = preferredIndex;
            selectedHotel = hotelListBox.SelectedItem as QaHotelMetadata;
        }
        finally
        {
            hotelListBox.EndUpdate();
            isRebinding = false;
        }

        UpdateEmptyState(filteredHotels.Count);

        if (!ReferenceEquals(previousSelection, selectedHotel))
        {
            OnSelectedHotelChanged();
        }
    }

    private static int FindHotelIndex(
        IReadOnlyList<QaHotelMetadata> source,
        string? preferredHotelId)
    {
        if (string.IsNullOrWhiteSpace(preferredHotelId))
        {
            return -1;
        }

        for (int index = 0; index < source.Count; index++)
        {
            if (source[index].HotelId.Equals(
                preferredHotelId,
                StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void UpdateSelectedHotelFromList()
    {
        if (isRebinding)
        {
            return;
        }

        QaHotelMetadata? newSelection = hotelListBox.SelectedItem as QaHotelMetadata;

        if (ReferenceEquals(selectedHotel, newSelection))
        {
            return;
        }

        selectedHotel = newSelection;
        OnSelectedHotelChanged();
    }

    private void UpdateEmptyState(int filteredHotelCount)
    {
        if (hotels.Count == 0)
        {
            emptyStateLabel.Text = "No hotels have been added.";
            emptyStateLabel.Visible = true;
            return;
        }

        emptyStateLabel.Text = "No hotels match the current search.";
        emptyStateLabel.Visible = filteredHotelCount == 0;
    }

    private static void FormatHotelListItem(
        object? sender,
        ListControlConvertEventArgs eventArgs)
    {
        if (eventArgs.ListItem is QaHotelMetadata hotel)
        {
            eventArgs.Value = QaMetadataSearch.GetHotelDisplayText(hotel);
        }
    }

    private void OnSelectedHotelChanged()
    {
        SelectedHotelChanged?.Invoke(this, EventArgs.Empty);
    }
}
