namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Carries one Quick QA draft and a filename-only Surface workbook selection.
/// </summary>
public sealed class QuickQaSaveRequest
{
    public QuickQaSaveRequest(
        QuickQaReport report,
        string surfaceWorkbookFileName)
    {
        Report = report ?? throw new ArgumentNullException(nameof(report));
        SurfaceWorkbookFileName = string.IsNullOrWhiteSpace(
                surfaceWorkbookFileName)
            ? throw new ArgumentException(
                "A selected Surface QA workbook filename is required.",
                nameof(surfaceWorkbookFileName))
            : surfaceWorkbookFileName;
    }

    public QuickQaReport Report { get; }

    public string SurfaceWorkbookFileName { get; }
}
