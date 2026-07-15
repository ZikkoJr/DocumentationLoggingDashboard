using MigraDoc.DocumentObjectModel;

namespace DocumentationLoggingDashboard.QAReports.Pdf;

internal static class QaPdfStyles
{
    public const string FontFamily = "Arial";
    public const string Title = "QaTitle";
    public const string SectionHeading = "QaSectionHeading";
    public const string SubsectionHeading = "QaSubsectionHeading";
    public const string FindingHeading = "QaFindingHeading";
    public const string Footer = "QaFooter";

    public static void Configure(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Style normal = document.Styles[StyleNames.Normal]
            ?? throw new InvalidOperationException("MigraDoc Normal style is unavailable.");
        normal.Font.Name = FontFamily;
        normal.Font.Size = Unit.FromPoint(8.4);
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);
        normal.ParagraphFormat.LineSpacingRule = LineSpacingRule.Single;

        Style title = document.AddStyle(Title, StyleNames.Normal);
        title.Font.Name = FontFamily;
        title.Font.Size = Unit.FromPoint(20);
        title.Font.Bold = true;
        title.Font.Color = Colors.Black;
        title.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);
        title.ParagraphFormat.KeepWithNext = true;

        Style section = document.AddStyle(SectionHeading, StyleNames.Heading1);
        section.Font.Name = FontFamily;
        section.Font.Size = Unit.FromPoint(13);
        section.Font.Bold = true;
        section.Font.Color = Colors.Black;
        section.ParagraphFormat.SpaceBefore = Unit.FromPoint(10);
        section.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);
        section.ParagraphFormat.KeepWithNext = true;
        section.ParagraphFormat.Borders.Bottom.Width = Unit.FromPoint(0.8);
        section.ParagraphFormat.Borders.Bottom.Color = Colors.DimGray;
        section.ParagraphFormat.Borders.DistanceFromBottom = Unit.FromPoint(2);

        Style subsection = document.AddStyle(
            SubsectionHeading,
            StyleNames.Heading2);
        subsection.Font.Name = FontFamily;
        subsection.Font.Size = Unit.FromPoint(10);
        subsection.Font.Bold = true;
        subsection.Font.Color = Colors.Black;
        subsection.ParagraphFormat.SpaceBefore = Unit.FromPoint(7);
        subsection.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);
        subsection.ParagraphFormat.KeepWithNext = true;

        Style finding = document.AddStyle(FindingHeading, StyleNames.Heading3);
        finding.Font.Name = FontFamily;
        finding.Font.Size = Unit.FromPoint(9);
        finding.Font.Bold = true;
        finding.Font.Color = Colors.Black;
        finding.ParagraphFormat.SpaceAfter = Unit.FromPoint(2);
        finding.ParagraphFormat.KeepWithNext = true;

        Style footer = document.AddStyle(Footer, StyleNames.Normal);
        footer.Font.Name = FontFamily;
        footer.Font.Size = Unit.FromPoint(6.5);
        footer.Font.Color = Colors.DimGray;
        footer.ParagraphFormat.SpaceAfter = Unit.FromPoint(1);
        footer.ParagraphFormat.Alignment = ParagraphAlignment.Center;
    }
}
