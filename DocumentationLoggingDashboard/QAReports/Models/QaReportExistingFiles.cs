using System.Collections.ObjectModel;

namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Describes existing filesystem matches in the Hotel and PMS locations.
/// </summary>
public sealed class QaReportExistingFiles
{
    private readonly ReadOnlyCollection<string> hotelFilePaths;
    private readonly ReadOnlyCollection<string> pmsFilePaths;

    public QaReportExistingFiles(
        IEnumerable<string> hotelFilePaths,
        IEnumerable<string> pmsFilePaths)
    {
        this.hotelFilePaths = Array.AsReadOnly(
            NormalizeAndSort(hotelFilePaths, nameof(hotelFilePaths)));
        this.pmsFilePaths = Array.AsReadOnly(
            NormalizeAndSort(pmsFilePaths, nameof(pmsFilePaths)));
    }

    public IReadOnlyList<string> HotelFilePaths => hotelFilePaths;

    public IReadOnlyList<string> PmsFilePaths => pmsFilePaths;

    public bool HasMatches => TotalMatchCount != 0;

    public int TotalMatchCount =>
        HotelFilePaths.Count + PmsFilePaths.Count;

    /// <summary>
    /// Gets whether the existing filesystem state is not exactly one matching,
    /// same-named file in each required location.
    /// </summary>
    public bool IsInconsistent
    {
        get
        {
            if (!HasMatches)
            {
                return false;
            }

            return HotelFilePaths.Count != 1
                || PmsFilePaths.Count != 1
                || !string.Equals(
                    Path.GetFileName(HotelFilePaths[0]),
                    Path.GetFileName(PmsFilePaths[0]),
                    StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string[] NormalizeAndSort(
        IEnumerable<string> source,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(source, parameterName);

        List<string> paths = [];

        foreach (string? path in source)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "Existing QA report paths cannot contain blank values.",
                    parameterName);
            }

            paths.Add(Path.GetFullPath(path));
        }

        return paths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }
}
