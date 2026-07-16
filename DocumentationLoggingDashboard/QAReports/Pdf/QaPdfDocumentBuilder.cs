using System.Globalization;
using System.Text;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Validation;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace DocumentationLoggingDashboard.QAReports.Pdf;

internal sealed class QaPdfDocumentBuilder
{
    private const string PrivacyReminder =
        "Do not include guest names, guest emails, payment data, credentials, or full hotel-file contents in QA reports.";

    private const int MaximumFindingNarrativeChunkLength = 900;
    private const int MaximumFindingNarrativeLineBreaksPerChunk = 16;

    private static readonly IReadOnlyDictionary<string, int> ChecklistOrder =
        QaChecklistCatalog.Definitions
            .Select((definition, index) => new { definition.Id, Index = index })
            .ToDictionary(entry => entry.Id, entry => entry.Index, StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, int> StatisticOrder =
        QaStatisticFieldCatalog.Definitions
            .Select((definition, index) => new { definition.Id, Index = index })
            .ToDictionary(entry => entry.Id, entry => entry.Index, StringComparer.Ordinal);

    public Document Build(
        QaReport report,
        QaReportValidationResult validationResult,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(validationResult);

        Document document = new();
        document.Info.Title = "Hotel QA Report";
        document.Info.Subject = "Internal Hotel File QA";
        document.Info.Author = validationResult.EffectiveCreatedBy;
        document.Info.Keywords = "Hotel QA, PMS, File Month";
        QaPdfStyles.Configure(document);

        Section section = document.AddSection();
        ConfigurePage(section);
        AddFooter(section, report);

        AddReportHeader(section, report, validationResult, generatedAt);
        AddInformationAndStatistics(section, report);
        AddChecklistSection(
            section,
            report,
            QaChecklistSection.RawFile,
            "Raw File QA");
        AddChecklistSection(
            section,
            report,
            QaChecklistSection.Database,
            "DB QA");
        AddFindingsSection(
            section,
            report,
            QaFindingSeverity.Warning,
            "Warnings");
        AddFindingsSection(
            section,
            report,
            QaFindingSeverity.Failure,
            "Failed Checks");
        AddNotes(section, report.GeneralNotes);

        return document;
    }

    private static void ConfigurePage(Section section)
    {
        section.PageSetup.PageFormat = PageFormat.Letter;
        section.PageSetup.Orientation =
            MigraDoc.DocumentObjectModel.Orientation.Portrait;
        section.PageSetup.TopMargin = Unit.FromInch(0.58);
        section.PageSetup.BottomMargin = Unit.FromInch(0.86);
        section.PageSetup.LeftMargin = Unit.FromInch(0.65);
        section.PageSetup.RightMargin = Unit.FromInch(0.65);
        section.PageSetup.FooterDistance = Unit.FromInch(0.28);
    }

    private static void AddFooter(Section section, QaReport report)
    {
        HeaderFooter footer = section.Footers.Primary;
        string hotelId = TrimToNull(report.HotelInformation?.HotelId) ?? "N/A";
        string fileMonth = report.HotelInformation?.FileMonth?.ToString() ?? "N/A";

        Paragraph context = footer.AddParagraph();
        context.Style = QaPdfStyles.Footer;
        context.AddText($"Hotel ID: {WrapLongWords(hotelId)} | File Month: {fileMonth} | Page ");
        context.AddPageField();
        context.AddText(" of ");
        context.AddNumPagesField();

        Paragraph privacy = footer.AddParagraph();
        privacy.Style = QaPdfStyles.Footer;
        privacy.AddText(PrivacyReminder);
    }

    private static void AddReportHeader(
        Section section,
        QaReport report,
        QaReportValidationResult validationResult,
        DateTimeOffset generatedAt)
    {
        Paragraph title = section.AddParagraph("Hotel QA Report");
        title.Style = QaPdfStyles.Title;

        Table summary = CreateKeyValueTable(section, 4.5, 13.5);
        AddKeyValueRow(
            summary,
            "Overall Status",
            FormatStatus(report.ReportStatus!.Value),
            emphasize: true);
        AddKeyValueRow(summary, "Hotel Name", report.HotelInformation.HotelName);
        AddKeyValueRow(summary, "Hotel ID", report.HotelInformation.HotelId);
        AddKeyValueRow(summary, "PMS", report.HotelInformation.PmsName);
        AddKeyValueRow(
            summary,
            "File Month",
            report.HotelInformation.FileMonth!.ToString());
        AddKeyValueRow(
            summary,
            "QA Date",
            report.QaDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddKeyValueRow(
            summary,
            "Created By",
            validationResult.EffectiveCreatedBy);

        string? safeFileName = GetSafeOriginalFileName(report.OriginalFileName);
        if (safeFileName is not null)
        {
            AddKeyValueRow(summary, "Original Filename", safeFileName);
        }

        AddKeyValueRow(
            summary,
            "PDF Generated",
            generatedAt.ToString(
                "yyyy-MM-dd HH:mm:ss zzz",
                CultureInfo.InvariantCulture));
        AddKeyValueRow(
            summary,
            "Schema Version",
            report.SchemaVersion.ToString(CultureInfo.InvariantCulture));
        KeepRowsTogether(summary);
    }

    private static void AddInformationAndStatistics(
        Section section,
        QaReport report)
    {
        AddSectionHeading(section, "Information and Statistics");
        AddFileInformation(section, report.Statistics.FileInformation);
        AddWarningSummary(section, report.Findings);
        AddBlankStatistics(section, report.Statistics);
        AddBrokenStatistics(section, report.Statistics);
        AddNameStatistics(section, report);
        AddFileMonthStatistics(section, report.Statistics.FileMonth);
        AddMonetaryStatistics(section, report);
        AddDatabaseStatistics(section, report);
    }

    private static void AddFileInformation(
        Section section,
        QaFileInformationStatistics statistics)
    {
        AddSubsectionHeading(section, "File Information");
        Table table = CreateKeyValueTable(section, 6.2, 11.8);
        AddKeyValueRow(
            table,
            "Total Data Rows",
            FormatInt(statistics.TotalDataRows));
        AddKeyValueRow(
            table,
            "Headers Present",
            statistics.HeadersArePresent ? "Yes" : "No");
        AddKeyValueRow(
            table,
            "Useful Headers",
            FormatUsefulHeaders(statistics.UsefulHeaders));
        AddKeyValueRow(
            table,
            "Data Start Row",
            FormatInt(statistics.DataStartRow));
        KeepRowsTogether(table);
    }

    private static void AddWarningSummary(
        Section section,
        IEnumerable<QaFinding> findings)
    {
        QaFinding[] warnings = OrderFindings(findings
                .Where(finding => finding.Severity == QaFindingSeverity.Warning))
            .ToArray();

        AddSubsectionHeading(section, "Warning Summary");
        Paragraph count = section.AddParagraph();
        count.AddFormattedText("Total warnings: ", TextFormat.Bold);
        count.AddText(warnings.Length.ToString(CultureInfo.InvariantCulture));

        if (warnings.Length == 0)
        {
            section.AddParagraph("No Warning-severity findings are present.");
            return;
        }

        Table table = CreateTable(section, 10.8, 7.2);
        AddHeaderRow(table, "Warning", "Resolution");
        foreach (QaFinding finding in warnings)
        {
            Row row = table.AddRow();
            AddCellText(row.Cells[0], finding.Title);
            AddCellText(row.Cells[1], FormatResolution(finding.Resolution));
        }
    }

    private static void AddBlankStatistics(
        Section section,
        QaStatistics statistics)
    {
        AddSubsectionHeading(section, "Blank Value Statistics");
        IReadOnlyDictionary<string, QaBlankValueStatistic> byId =
            statistics.BlankValues.ToDictionary(
                statistic => statistic.FieldId,
                StringComparer.Ordinal);

        Table table = CreateTable(section, 7.0, 3.1, 4.3, 3.2);
        AddHeaderRow(
            table,
            "Field",
            "Blank Count",
            "Applicable Rows",
            "Blank %");

        foreach (QaStatisticFieldDefinition definition
                 in QaStatisticFieldCatalog.Definitions)
        {
            if (!byId.TryGetValue(
                    definition.Id,
                    out QaBlankValueStatistic? statistic))
            {
                continue;
            }

            Row row = table.AddRow();
            AddCellText(row.Cells[0], definition.DisplayName);
            AddCellText(row.Cells[1], FormatInt(statistic.BlankCount));
            AddCellText(row.Cells[2], FormatInt(statistic.TotalApplicableRows));
            AddCellText(row.Cells[3], FormatPercentage(statistic.BlankPercentage));
        }
    }

    private static void AddBrokenStatistics(
        Section section,
        QaStatistics statistics)
    {
        AddSubsectionHeading(section, "Broken Data Statistics");
        IReadOnlyDictionary<string, QaBrokenDataStatistic> byId =
            statistics.BrokenData.ToDictionary(
                statistic => statistic.FieldId,
                StringComparer.Ordinal);

        Table table = CreateTable(section, 4.7, 2.5, 3.5, 2.7, 4.6);
        AddHeaderRow(
            table,
            "Field",
            "Broken",
            "Nonblank Rows",
            "Broken %",
            "Explanation");

        foreach (QaStatisticFieldDefinition definition
                 in QaStatisticFieldCatalog.Definitions)
        {
            if (!byId.TryGetValue(
                    definition.Id,
                    out QaBrokenDataStatistic? statistic))
            {
                continue;
            }

            Row row = table.AddRow();
            AddCellText(row.Cells[0], definition.DisplayName);
            AddCellText(row.Cells[1], FormatInt(statistic.BrokenValueCount));
            AddCellText(
                row.Cells[2],
                FormatInt(statistic.TotalApplicableNonblankValues));
            AddCellText(
                row.Cells[3],
                FormatPercentage(statistic.BrokenDataPercentage));
            AddCellText(row.Cells[4], TrimToNull(statistic.Explanation) ?? "-");
        }
    }

    private static void AddNameStatistics(Section section, QaReport report)
    {
        if (report.FileCharacteristics.NameColumnMode !=
            QaNameColumnMode.SeparateFirstAndLastName)
        {
            return;
        }

        AddSubsectionHeading(section, "Name Statistics");
        QaMultiwordNameStatistics names = report.Statistics.MultiwordNames;
        IReadOnlyDictionary<string, QaBlankValueStatistic> blankById =
            report.Statistics.BlankValues.ToDictionary(
                statistic => statistic.FieldId,
                StringComparer.Ordinal);

        Table table = CreateTable(section, 7.0, 3.2, 4.3, 3.5);
        table.KeepTogether = true;
        AddHeaderRow(table, "Measure", "Count", "Nonblank Rows", "Percentage");

        AddStatisticMeasureRow(
            table,
            "Multiword First Name values",
            names.MultiwordFirstNameCount,
            GetNonblankCount(blankById[QaStatisticFieldIds.FirstName]),
            names.MultiwordFirstNamePercentage);
        AddStatisticMeasureRow(
            table,
            "Multiword Last Name values",
            names.MultiwordLastNameCount,
            GetNonblankCount(blankById[QaStatisticFieldIds.LastName]),
            names.MultiwordLastNamePercentage);
        KeepRowsTogether(table);
    }

    private static void AddFileMonthStatistics(
        Section section,
        QaFileMonthStatistics statistics)
    {
        AddSubsectionHeading(section, "File Month Statistics");
        Table table = CreateKeyValueTable(section, 9.4, 8.6);
        AddKeyValueRow(
            table,
            "Valid Arrival Date Count",
            FormatInt(statistics.ValidArrivalDateCount));
        AddKeyValueRow(
            table,
            "Arrival Dates inside File Month",
            FormatInt(statistics.ArrivalDatesWithinFileMonth));
        AddKeyValueRow(
            table,
            "Arrival Dates outside File Month",
            FormatInt(statistics.ArrivalDatesOutsideFileMonth));
        AddKeyValueRow(
            table,
            "Percentage inside",
            FormatPercentage(statistics.PercentageWithinFileMonth));
        AddKeyValueRow(
            table,
            "Percentage outside",
            FormatPercentage(statistics.PercentageOutsideFileMonth));
        KeepRowsTogether(table);
    }

    private static void AddMonetaryStatistics(
        Section section,
        QaReport report)
    {
        AddSubsectionHeading(section, "Monetary Statistics");
        Table table = CreateTable(section, 5.1, 2.6, 3.0, 2.8, 4.5);
        table.KeepTogether = true;
        AddHeaderRow(
            table,
            "Measure",
            "Found",
            "Count",
            "Percentage",
            "Explanation");

        AddMonetaryRow(
            table,
            "Unusual Average Rate Values",
            report.Statistics.UnusualAverageRateValues.HasUnusualValues,
            report.Statistics.UnusualAverageRateValues.UnusualValueCount,
            report.Statistics.UnusualAverageRateValues.UnusualValuePercentage,
            report.Statistics.UnusualAverageRateValues.Explanation);

        bool stayValueApplies = QaStatisticFieldCatalog.IsApplicable(
            QaStatisticFieldCatalog.GetRequired(QaStatisticFieldIds.StayValue),
            report.FileCharacteristics);

        if (!stayValueApplies)
        {
            return;
        }

        AddMonetaryRow(
            table,
            "Unusual Stay Values",
            report.Statistics.UnusualStayValues.HasUnusualValues,
            report.Statistics.UnusualStayValues.UnusualValueCount,
            report.Statistics.UnusualStayValues.UnusualValuePercentage,
            report.Statistics.UnusualStayValues.Explanation);

        QaHighStayValueStatistics high = report.Statistics.HighStayValues;
        Row highRow = table.AddRow();
        AddCellText(highRow.Cells[0], "Stay Values strictly above 10,000");
        AddCellText(
            highRow.Cells[1],
            high.StayValuesAboveTenThousandCount > 0 ? "Yes" : "No");
        AddCellText(
            highRow.Cells[2],
            FormatInt(high.StayValuesAboveTenThousandCount));
        AddCellText(
            highRow.Cells[3],
            FormatPercentage(high.StayValuesAboveTenThousandPercentage));
        AddCellText(highRow.Cells[4], TrimToNull(high.Explanation) ?? "-");

        Row expectedRow = table.AddRow();
        AddCellText(expectedRow.Cells[0], "High Values Expected");
        expectedRow.Cells[0].MergeRight = 2;
        AddCellText(
            expectedRow.Cells[3],
            high.AreHighValuesExpected ? "Yes" : "No");
        expectedRow.Cells[3].MergeRight = 1;
        KeepRowsTogether(table);
    }

    private static void AddDatabaseStatistics(
        Section section,
        QaReport report)
    {
        AddSubsectionHeading(section, "Database Statistics");
        QaDatabaseStatistics statistics = report.Statistics.Database;
        Table table = CreateKeyValueTable(section, 9.4, 8.6);
        AddKeyValueRow(
            table,
            "Imported Record Count",
            FormatInt(statistics.ImportedRecordCount));
        AddKeyValueRow(
            table,
            "Absolute Raw/DB Row-Count Difference",
            Math.Abs((long)statistics.RawMinusImportedRecordCountDifference)
                .ToString(CultureInfo.InvariantCulture));
        KeepRowsTogether(table);

        bool hasRejected = statistics.RejectedRecordCount > 0;
        bool hasMissing = statistics.RecordsWithMissingRequiredDatabaseValues > 0;
        if (!hasRejected && !hasMissing)
        {
            return;
        }

        AddSubsectionHeading(section, "DB Issues");
        Table issues = CreateTable(section, 5.0, 2.4, 10.6);
        issues.KeepTogether = true;
        AddHeaderRow(issues, "Issue", "Count", "Existing QA Context");

        if (hasRejected)
        {
            AddDatabaseIssueRow(
                issues,
                report,
                "Rejected records",
                statistics.RejectedRecordCount,
                QaChecklistIds.Database.RejectedRecordsAccountedFor);
        }

        if (hasMissing)
        {
            AddDatabaseIssueRow(
                issues,
                report,
                "Missing required DB values",
                statistics.RecordsWithMissingRequiredDatabaseValues,
                QaChecklistIds.Database.RequiredValuesPresent);
        }

        KeepRowsTogether(issues);
    }

    private static void AddChecklistSection(
        Section section,
        QaReport report,
        QaChecklistSection checklistSection,
        string title)
    {
        AddSectionHeading(section, title);
        IReadOnlyDictionary<string, QaCheckResult> resultsById =
            report.ChecklistResults.ToDictionary(
                result => result.CheckId,
                StringComparer.Ordinal);

        Table table = CreateTable(section, 6.4, 2.4, 9.2);
        AddHeaderRow(table, "Check", "Result", "Details");

        foreach (QaCheckDefinition definition in QaChecklistCatalog.Definitions
                     .Where(definition => definition.Section == checklistSection))
        {
            QaCheckResult result = resultsById[definition.Id];
            string details = TrimToNull(result.Notes)
                ?? (result.Status == QaCheckStatus.NotApplicable
                    ? "Not applicable to the selected file configuration."
                    : "-");
            IReadOnlyList<string> detailChunks = SplitFindingNarrative(details);

            for (int index = 0; index < detailChunks.Count; index++)
            {
                Row row = table.AddRow();
                AddCellText(
                    row.Cells[0],
                    index == 0
                        ? definition.DisplayName
                        : definition.DisplayName + " (continued)");
                AddCellText(row.Cells[1], FormatCheckStatus(result.Status));
                AddCellText(row.Cells[2], detailChunks[index]);
            }
        }
    }

    private static void AddFindingsSection(
        Section section,
        QaReport report,
        QaFindingSeverity severity,
        string title)
    {
        QaFinding[] findings = OrderFindings(
                report.Findings.Where(finding => finding.Severity == severity))
            .ToArray();
        if (findings.Length == 0)
        {
            return;
        }

        AddSectionHeading(section, title);
        foreach (QaFinding finding in findings)
        {
            IReadOnlyList<string> descriptionChunks =
                SplitFindingNarrative(finding.Description);
            string? resolutionNotes = TrimToNull(finding.ResolutionNotes);
            IReadOnlyList<string> resolutionNoteChunks = resolutionNotes is null
                ? []
                : SplitFindingNarrative(resolutionNotes);
            bool requiresPagination = descriptionChunks.Count > 1
                || resolutionNoteChunks.Count > 1;

            Paragraph heading = section.AddParagraph();
            heading.Style = QaPdfStyles.FindingHeading;
            heading.AddText(
                $"{FormatSeverity(finding.Severity)} - {FormatResolution(finding.Resolution)}");

            Table details = CreateKeyValueTable(section, 4.4, 13.6);
            details.KeepTogether = !requiresPagination;
            AddKeyValueRow(details, "Title", finding.Title, emphasize: true);
            AddFindingNarrativeRows(
                details,
                "Description",
                descriptionChunks);

            string? relatedCheck = GetRelatedChecklistDisplayName(
                finding.RelatedCheckId);
            if (relatedCheck is not null)
            {
                AddKeyValueRow(details, "Related Check", relatedCheck);
            }

            AddKeyValueRow(details, "Source", FormatSource(finding.Source));
            AddKeyValueRow(
                details,
                "Resolution",
                FormatResolution(finding.Resolution));

            if (TrimToNull(finding.CustomScriptName) is string scriptName)
            {
                AddKeyValueRow(details, "Custom Script Name", scriptName);
            }

            if (resolutionNoteChunks.Count > 0)
            {
                AddFindingNarrativeRows(
                    details,
                    "Resolution Notes",
                    resolutionNoteChunks);
            }

            if (requiresPagination)
            {
                // Keep the bounded title and first narrative chunk with the
                // finding heading, while allowing later narrative rows to flow.
                details.Rows[0].KeepWith = 1;
            }
            else
            {
                KeepRowsTogether(details);
            }

            Paragraph spacer = section.AddParagraph();
            spacer.Format.SpaceAfter = Unit.FromPoint(2);
        }
    }

    private static void AddNotes(Section section, string? notes)
    {
        string? value = TrimToNull(notes);
        if (value is null)
        {
            return;
        }

        AddSectionHeading(section, "Notes");
        Paragraph paragraph = section.AddParagraph();
        AddPlainText(paragraph, value);
    }

    private static Table CreateKeyValueTable(
        Section section,
        double labelWidthCm,
        double valueWidthCm)
    {
        Table table = CreateTable(section, labelWidthCm, valueWidthCm);
        table.KeepTogether = true;
        return table;
    }

    private static Table CreateTable(Section section, params double[] widthsCm)
    {
        Table table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.45);
        table.Borders.Color = Colors.Gray;
        table.Format.Font.Name = QaPdfStyles.FontFamily;
        table.Format.Font.Size = Unit.FromPoint(7.8);
        table.Format.SpaceAfter = Unit.FromPoint(4);

        foreach (double width in widthsCm)
        {
            table.AddColumn(Unit.FromCentimeter(width));
        }

        return table;
    }

    private static void AddHeaderRow(Table table, params string[] labels)
    {
        Row row = table.AddRow();
        row.HeadingFormat = true;
        row.Format.Font.Bold = true;
        row.Shading.Color = Colors.LightGray;

        for (int index = 0; index < labels.Length; index++)
        {
            AddCellText(row.Cells[index], labels[index]);
        }
    }

    private static void KeepRowsTogether(Table table)
    {
        if (table.Rows.Count > 1)
        {
            table.Rows[0].KeepWith = table.Rows.Count - 1;
        }
    }

    private static void AddKeyValueRow(
        Table table,
        string label,
        string? value,
        bool emphasize = false)
    {
        Row row = table.AddRow();
        row.Cells[0].Format.Font.Bold = true;
        row.Cells[0].Shading.Color = Colors.LightGray;
        AddCellText(row.Cells[0], label);
        AddCellText(row.Cells[1], value ?? "-");

        if (emphasize)
        {
            row.Cells[1].Format.Font.Bold = true;
            row.Cells[1].Format.Font.Size = Unit.FromPoint(9.2);
        }
    }

    private static void AddFindingNarrativeRows(
        Table table,
        string label,
        IReadOnlyList<string> chunks)
    {
        for (int index = 0; index < chunks.Count; index++)
        {
            string rowLabel = index == 0
                ? label
                : label + " (continued)";
            AddKeyValueRow(table, rowLabel, chunks[index]);
        }
    }

    private static IReadOnlyList<string> SplitFindingNarrative(string? value)
    {
        string narrative = value ?? "-";

        List<string> chunks = [];
        int start = 0;

        while (start < narrative.Length)
        {
            int hardEnd = Math.Min(
                start + MaximumFindingNarrativeChunkLength,
                narrative.Length);
            if (hardEnd < narrative.Length
                && narrative[hardEnd - 1] == '\r'
                && narrative[hardEnd] == '\n')
            {
                hardEnd--;
            }

            int end = FindLineBoundedChunkEnd(narrative, start, hardEnd);

            if (end == hardEnd && end < narrative.Length)
            {
                for (int index = end - 1; index > start; index--)
                {
                    if (char.IsWhiteSpace(narrative[index]))
                    {
                        int candidate = index + 1;
                        if (narrative[index] == '\r'
                            && candidate < narrative.Length
                            && narrative[candidate] == '\n')
                        {
                            candidate++;
                        }

                        if (candidate <= hardEnd)
                        {
                            end = candidate;
                        }

                        break;
                    }
                }
            }

            chunks.Add(narrative[start..end]);
            start = end;
        }

        if (chunks.Count == 0)
        {
            chunks.Add(narrative);
        }

        return chunks;
    }

    private static int FindLineBoundedChunkEnd(
        string narrative,
        int start,
        int hardEnd)
    {
        int lineBreaks = 0;
        for (int index = start; index < hardEnd; index++)
        {
            char character = narrative[index];
            if (character != '\r' && character != '\n')
            {
                continue;
            }

            if (lineBreaks == MaximumFindingNarrativeLineBreaksPerChunk)
            {
                return index;
            }

            lineBreaks++;
            if (character == '\r'
                && index + 1 < hardEnd
                && narrative[index + 1] == '\n')
            {
                index++;
            }
        }

        return hardEnd;
    }

    private static void AddStatisticMeasureRow(
        Table table,
        string measure,
        int count,
        int denominator,
        decimal percentage)
    {
        Row row = table.AddRow();
        AddCellText(row.Cells[0], measure);
        AddCellText(row.Cells[1], FormatInt(count));
        AddCellText(row.Cells[2], FormatInt(denominator));
        AddCellText(row.Cells[3], FormatPercentage(percentage));
    }

    private static void AddMonetaryRow(
        Table table,
        string measure,
        bool found,
        int count,
        decimal percentage,
        string? explanation)
    {
        Row row = table.AddRow();
        AddCellText(row.Cells[0], measure);
        AddCellText(row.Cells[1], found ? "Yes" : "No");
        AddCellText(row.Cells[2], FormatInt(count));
        AddCellText(row.Cells[3], FormatPercentage(percentage));
        AddCellText(row.Cells[4], TrimToNull(explanation) ?? "-");
    }

    private static void AddDatabaseIssueRow(
        Table table,
        QaReport report,
        string label,
        int count,
        string relatedCheckId)
    {
        Row row = table.AddRow();
        AddCellText(row.Cells[0], label);
        AddCellText(row.Cells[1], FormatInt(count));
        AddCellText(
            row.Cells[2],
            GetDatabaseContext(report, relatedCheckId) ?? "See the related checklist result.");
    }

    private static string? GetDatabaseContext(
        QaReport report,
        string relatedCheckId)
    {
        List<string> values = [];
        QaCheckResult? result = report.ChecklistResults.FirstOrDefault(
            item => string.Equals(
                item.CheckId,
                relatedCheckId,
                StringComparison.Ordinal));
        AddDistinct(values, result?.Notes);

        foreach (QaFinding finding in report.Findings
                     .Where(finding => string.Equals(
                         finding.RelatedCheckId,
                         relatedCheckId,
                         StringComparison.Ordinal))
                     .OrderBy(finding => finding.FindingId, StringComparer.Ordinal))
        {
            AddDistinct(values, finding.Description);
            AddDistinct(values, finding.ResolutionNotes);
        }

        return values.Count == 0
            ? null
            : string.Join("\n", values);
    }

    private static void AddDistinct(ICollection<string> values, string? candidate)
    {
        string? trimmed = TrimToNull(candidate);
        if (trimmed is not null && !values.Contains(trimmed, StringComparer.Ordinal))
        {
            values.Add(trimmed);
        }
    }

    private static void AddSectionHeading(Section section, string text)
    {
        Paragraph paragraph = section.AddParagraph(text);
        paragraph.Style = QaPdfStyles.SectionHeading;
    }

    private static void AddSubsectionHeading(Section section, string text)
    {
        Paragraph paragraph = section.AddParagraph(text);
        paragraph.Style = QaPdfStyles.SubsectionHeading;
    }

    private static void AddCellText(Cell cell, string? text)
    {
        cell.VerticalAlignment = VerticalAlignment.Center;
        Paragraph paragraph = cell.AddParagraph();
        paragraph.Format.SpaceBefore = Unit.FromPoint(0.8);
        paragraph.Format.SpaceAfter = Unit.FromPoint(0.8);
        AddPlainText(paragraph, text ?? string.Empty);
    }

    private static void AddPlainText(Paragraph paragraph, string text)
    {
        string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        string[] lines = normalized.Split('\n');

        for (int index = 0; index < lines.Length; index++)
        {
            paragraph.AddText(WrapLongWords(lines[index]));
            if (index < lines.Length - 1)
            {
                paragraph.AddLineBreak();
            }
        }
    }

    private static string WrapLongWords(string value)
    {
        const int maximumRunLength = 42;
        StringBuilder wrapped = new(value.Length + value.Length / maximumRunLength);
        int runLength = 0;

        foreach (char character in value)
        {
            bool breakable = char.IsWhiteSpace(character)
                || character is '-' or '/' or '\\';

            if (!breakable && runLength == maximumRunLength)
            {
                wrapped.Append('\n');
                runLength = 0;
            }

            wrapped.Append(character);
            runLength = breakable ? 0 : runLength + 1;
        }

        return wrapped.ToString();
    }

    private static IEnumerable<QaFinding> OrderFindings(
        IEnumerable<QaFinding> findings)
    {
        return findings
            .Select(finding => new
            {
                Finding = finding,
                Order = GetFindingOrder(finding)
            })
            .OrderBy(entry => entry.Order.Category)
            .ThenBy(entry => entry.Order.Primary)
            .ThenBy(entry => entry.Order.Secondary)
            .ThenBy(entry => entry.Finding.FindingId, StringComparer.Ordinal)
            .Select(entry => entry.Finding);
    }

    private static (int Category, int Primary, int Secondary) GetFindingOrder(
        QaFinding finding)
    {
        if (finding.RelatedCheckId is string relatedId
            && ChecklistOrder.TryGetValue(relatedId, out int checklistOrder))
        {
            return (0, checklistOrder, 0);
        }

        if (TryGetStatisticFindingOrder(
                finding.FindingId,
                out int group,
                out int fieldOrder))
        {
            return (1, group, fieldOrder);
        }

        return finding.Source == QaFindingSource.Manual
            ? (3, 0, 0)
            : (2, (int)finding.Source, 0);
    }

    private static bool TryGetStatisticFindingOrder(
        string findingId,
        out int group,
        out int fieldOrder)
    {
        const string blankPrefix = "WARN:STAT:BLANK:";
        const string brokenPrefix = "FAIL:STAT:BROKEN:";

        if (findingId.StartsWith(blankPrefix, StringComparison.Ordinal))
        {
            group = 0;
            return StatisticOrder.TryGetValue(
                findingId[blankPrefix.Length..],
                out fieldOrder);
        }

        if (findingId.StartsWith(brokenPrefix, StringComparison.Ordinal))
        {
            group = 1;
            return StatisticOrder.TryGetValue(
                findingId[brokenPrefix.Length..],
                out fieldOrder);
        }

        group = findingId switch
        {
            QaFindingIds.UnusualAverageRateStatisticWarning => 2,
            QaFindingIds.UnusualStayValueStatisticWarning => 3,
            QaFindingIds.HighStayValueStatisticWarning => 4,
            QaFindingIds.RowDifferenceStatisticWarning => 5,
            QaFindingIds.RejectedRecordsStatisticWarning => 6,
            _ => -1
        };
        fieldOrder = 0;
        return group >= 0;
    }

    private static string? GetRelatedChecklistDisplayName(string? checkId)
    {
        string? id = TrimToNull(checkId);
        if (id is null)
        {
            return null;
        }

        QaCheckDefinition? definition = QaChecklistCatalog.Definitions
            .FirstOrDefault(definition => string.Equals(
                definition.Id,
                id,
                StringComparison.Ordinal));

        return definition is null
            ? id
            : $"{definition.DisplayName} ({definition.Id})";
    }

    internal static string? GetSafeOriginalFileName(string? value)
    {
        string? trimmed = TrimToNull(value);
        if (trimmed is null
            || trimmed[^1] is '/' or '\\')
        {
            return null;
        }

        int separator = trimmed.LastIndexOfAny(['/', '\\']);
        string fileName = separator < 0
            ? trimmed
            : trimmed[(separator + 1)..];
        return TrimToNull(fileName);
    }

    private static int GetNonblankCount(QaBlankValueStatistic statistic)
    {
        return statistic.TotalApplicableRows - statistic.BlankCount;
    }

    private static string FormatStatus(QaReportStatus status)
    {
        return status switch
        {
            QaReportStatus.Pass => "Pass",
            QaReportStatus.PassWithWarnings => "Pass with Warnings",
            QaReportStatus.Fail => "Fail",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    private static string FormatCheckStatus(QaCheckStatus status)
    {
        return status switch
        {
            QaCheckStatus.Pass => "Pass",
            QaCheckStatus.Fail => "Fail",
            QaCheckStatus.NotApplicable => "N/A",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    private static string FormatSeverity(QaFindingSeverity severity)
    {
        return severity switch
        {
            QaFindingSeverity.Warning => "Warning",
            QaFindingSeverity.Failure => "Failure",
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
        };
    }

    private static string FormatResolution(QaFindingResolution resolution)
    {
        return resolution switch
        {
            QaFindingResolution.Active => "Active",
            QaFindingResolution.HandledByCustomScript =>
                "Handled by Custom Script",
            QaFindingResolution.ExplainedAndAccepted =>
                "Explained and Accepted",
            _ => throw new ArgumentOutOfRangeException(
                nameof(resolution),
                resolution,
                null)
        };
    }

    private static string FormatSource(QaFindingSource source)
    {
        return source switch
        {
            QaFindingSource.Checklist => "Checklist",
            QaFindingSource.Statistic => "Statistic",
            QaFindingSource.DatabaseComparison => "Database Comparison",
            QaFindingSource.Manual => "Manual",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }

    private static string FormatUsefulHeaders(QaUsefulHeadersResult result)
    {
        return result switch
        {
            QaUsefulHeadersResult.Yes => "Yes",
            QaUsefulHeadersResult.Partially => "Partially",
            QaUsefulHeadersResult.No => "No",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };
    }

    private static string FormatInt(int value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatPercentage(decimal value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture) + "%";
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
