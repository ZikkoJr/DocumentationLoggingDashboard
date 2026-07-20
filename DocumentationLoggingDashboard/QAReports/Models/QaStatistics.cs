namespace DocumentationLoggingDashboard.QAReports.Models;

/// <summary>
/// Groups structured statistics captured for a QA report.
/// </summary>
public sealed class QaStatistics
{
    public QaFileInformationStatistics FileInformation { get; set; } = new();

    public List<QaBlankValueStatistic> BlankValues { get; } = [];

    public List<QaBrokenDataStatistic> BrokenData { get; } = [];

    public QaMultiwordNameStatistics MultiwordNames { get; set; } = new();

    public QaFileMonthStatistics FileMonth { get; set; } = new();

    public QaUnusualMonetaryValueStatistics UnusualAverageRateValues { get; set; } = new();

    public QaUnusualMonetaryValueStatistics UnusualStayValues { get; set; } = new();

    public QaHighStayValueStatistics HighStayValues { get; set; } = new();

    public QaDatabaseStatistics Database { get; set; } = new();
}
