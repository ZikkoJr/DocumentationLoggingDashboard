using System.Globalization;
using System.Reflection;
using System.Text;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.GeometryTests;

/// <summary>
/// Focused, additive coverage for the post-pilot Detailed QA contract. Program.cs
/// invokes <see cref="RunAll"/> with the other semantic suites.
/// </summary>
internal static class QaDetailedPostPilotRegressionTests
{
    private static readonly QaStatisticsCalculationService CalculationService = new();
    private static readonly QaFindingSynchronizationService FindingService = new();
    private static readonly QaReportValidationService ValidationService = new();
    private static readonly QaPdfGenerationService PdfService = new();

    private static readonly string[] StrategyCheckIds =
    [
        QaChecklistIds.Raw.StrategySourceColumnAvailable,
        QaChecklistIds.Raw.StrategyRateColumnAvailable,
        QaChecklistIds.Raw.StrategyMarketColumnAvailable
    ];

    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        (string Name, Action Body)[] tests =
        [
            ("catalog retirements and schema-2 File ID", TestCatalogAndFileId),
            ("Detailed Email warning behavior", TestEmailAvailability),
            ("Detailed strategy eight-combination matrix", TestStrategyMatrix),
            ("legacy, new, and mixed index compatibility", TestIndexCompatibility)
        ];

        int failed = 0;
        output.WriteLine($"Detailed post-pilot harness: {tests.Length} tests");

        foreach ((string name, Action body) in tests)
        {
            output.WriteLine($"[RUN ] {name}");

            try
            {
                body();
                output.WriteLine($"[PASS] {name}");
            }
            catch (Exception exception)
            {
                failed++;
                output.WriteLine($"[FAIL] {name}");
                output.WriteLine(exception.ToString());
            }
        }

        output.WriteLine(
            failed == 0
                ? $"[PASS] All {tests.Length} Detailed post-pilot tests passed."
                : $"[FAIL] {failed} of {tests.Length} Detailed post-pilot tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void TestCatalogAndFileId()
    {
        IReadOnlyList<QaCheckDefinition> definitions =
            QaChecklistCatalog.Definitions;
        string[] activeIds = definitions
            .Select(definition => definition.Id)
            .ToArray();

        Check(
            QaReport.CurrentSchemaVersion == 2,
            "New Detailed reports are not schema version 2.");
        Check(
            definitions.Count == QaChecklistCatalog.ExpectedDefinitionCount
            && definitions.Count == 29,
            "The Detailed catalog does not contain exactly 29 definitions.");
        Check(
            definitions.Count(definition =>
                definition.Section == QaChecklistSection.RawFile) == 22,
            "The Detailed catalog does not contain exactly 22 Raw definitions.");
        Check(
            definitions.Count(definition =>
                definition.Section == QaChecklistSection.Database) == 7,
            "The Detailed catalog does not contain exactly 7 Database definitions.");
        Check(
            activeIds.Distinct(StringComparer.Ordinal).Count() == activeIds.Length,
            "The Detailed catalog contains duplicate stable IDs.");
        Check(
            !activeIds.Contains(
                QaChecklistIds.Raw.RequiredEmailPresent,
                StringComparer.Ordinal)
            && !activeIds.Contains(
                QaChecklistIds.Raw.SourceColumnPresent,
                StringComparer.Ordinal)
            && !activeIds.Contains(
                QaChecklistIds.Raw.ArrivalWithinFileMonth,
                StringComparer.Ordinal),
            "A retired Detailed checklist ID remains active.");

        string[] expectedAvailabilityOrder =
        [
            QaChecklistIds.Raw.EmailColumnAvailable,
            QaChecklistIds.Raw.StrategySourceColumnAvailable,
            QaChecklistIds.Raw.StrategyRateColumnAvailable,
            QaChecklistIds.Raw.StrategyMarketColumnAvailable
        ];
        foreach (string id in expectedAvailabilityOrder)
        {
            Check(
                activeIds.Count(candidate => candidate == id) == 1,
                $"Availability ID '{id}' is not present exactly once.");
        }

        int sourceIndex = Array.IndexOf(
            activeIds,
            QaChecklistIds.Raw.StrategySourceColumnAvailable);
        Check(
            sourceIndex >= 0
            && activeIds[sourceIndex + 1] ==
                QaChecklistIds.Raw.StrategyRateColumnAvailable
            && activeIds[sourceIndex + 2] ==
                QaChecklistIds.Raw.StrategyMarketColumnAvailable,
            "Source, Rate, and Market are not consecutive in defined order.");
        Check(
            QaFindingIds.ArrivalOutsideFileMonthStatisticFailure ==
                "STAT:FAIL:ARRIVAL_OUTSIDE_FILE_MONTH",
            "The Arrival/File Month stable finding ID is incorrect.");

        QaReport report = CreateCompleteReport();
        report.FileId = "  001234  ";
        Check(
            report.FileId == "001234",
            "File ID surrounding whitespace was not trimmed or leading zeroes changed.");

        QaReportValidationResult validation = Validate(report);
        AssertReady(validation, QaReportStatus.Pass, "valid File ID");
        report.ReportStatus = validation.CalculatedStatus;

        string ddl = BuildPdfDdl(
            report,
            validation,
            new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));
        Check(
            ddl.Contains("File ID", StringComparison.Ordinal)
            && ddl.Contains("001234", StringComparison.Ordinal),
            "The Detailed PDF document does not display File ID.");

        QaHotelMetadata hotel = CanonicalHotel();
        QaPmsMetadata pms = CanonicalPms();
        QaReportFilenameService filenameService = new(new QaFolderNameSanitizer());
        string firstFilename = filenameService.CreateFilename(report, hotel, pms);
        QaReportKey firstKey = new(
            report.HotelInformation.HotelId,
            report.HotelInformation.FileMonth!);

        report.FileId = "009999";
        string secondFilename = filenameService.CreateFilename(report, hotel, pms);
        QaReportKey secondKey = new(
            report.HotelInformation.HotelId,
            report.HotelInformation.FileMonth!);
        Check(
            firstFilename == secondFilename,
            "Changing File ID changed the Detailed PDF filename.");
        Check(
            firstKey == secondKey,
            "Changing File ID changed the Hotel ID + File Month report key.");

        bool stalePdfRejected = false;
        try
        {
            _ = PdfService.GeneratePdf(
                report,
                validation,
                new DateTimeOffset(2026, 8, 18, 12, 1, 0, TimeSpan.Zero));
        }
        catch (QaPdfGenerationException)
        {
            stalePdfRejected = true;
        }

        Check(
            stalePdfRejected,
            "Changing File ID did not stale the readiness fingerprint.");

        report.FileId = "   ";
        QaReportValidationResult blank = Validate(report);
        Check(
            !blank.IsReady
            && blank.BlockingErrors.Any(error =>
                error.Contains("File ID", StringComparison.Ordinal)),
            "A blank File ID was not blocked.");

        report.FileId = "001234";
        report.SchemaVersion = 1;
        QaReportValidationResult oldSchema = Validate(report);
        Check(
            !oldSchema.IsReady
            && oldSchema.BlockingErrors.Any(error =>
                error.Contains("schema version", StringComparison.OrdinalIgnoreCase)),
            "A new schema-1 Detailed report was not blocked.");
    }

    private static void TestEmailAvailability()
    {
        QaReport available = CreateCompleteReport();
        Synchronize(available);
        QaReportValidationResult availableValidation = Validate(available);
        AssertReady(availableValidation, QaReportStatus.Pass, "Email available");
        Check(
            !available.Findings.Any(finding =>
                finding.RelatedCheckId ==
                    QaChecklistIds.Raw.EmailColumnAvailable),
            "Available Email generated a finding.");

        QaReport unavailable = CreateCompleteReport();
        SetStatus(
            unavailable,
            QaChecklistIds.Raw.EmailColumnAvailable,
            QaCheckStatus.Fail);
        Synchronize(unavailable);

        QaFinding[] findings = unavailable.Findings
            .Where(finding =>
                finding.RelatedCheckId ==
                    QaChecklistIds.Raw.EmailColumnAvailable)
            .ToArray();
        Check(
            findings.Length == 1
            && findings[0].FindingId == QaFindingIds.WarningForCheck(
                QaChecklistIds.Raw.EmailColumnAvailable)
            && findings[0].Severity == QaFindingSeverity.Warning,
            "Unavailable Email did not create exactly one Warning.");
        Check(
            !unavailable.Findings.Any(finding =>
                finding.Severity == QaFindingSeverity.Failure),
            "Unavailable Email generated a Failure.");

        QaReportValidationResult validation = Validate(unavailable);
        AssertReady(
            validation,
            QaReportStatus.PassWithWarnings,
            "Email unavailable");
        Check(
            validation.WarningFindingCount == 1
            && validation.FailureFindingCount == 0,
            "Unavailable Email produced incorrect finding counts.");
    }

    private static void TestStrategyMatrix()
    {
        for (int mask = 0; mask < 8; mask++)
        {
            QaReport report = CreateCompleteReport();
            List<string> missingCategories = [];

            for (int index = 0; index < StrategyCheckIds.Length; index++)
            {
                bool missing = (mask & (1 << index)) != 0;
                SetStatus(
                    report,
                    StrategyCheckIds[index],
                    missing ? QaCheckStatus.Fail : QaCheckStatus.Pass);

                if (missing)
                {
                    missingCategories.Add(index switch
                    {
                        0 => "Source",
                        1 => "Rate",
                        2 => "Market",
                        _ => throw new InvalidOperationException()
                    });
                }
            }

            Synchronize(report);
            int missingCount = missingCategories.Count;
            QaFinding[] aggregate = report.Findings
                .Where(finding =>
                    finding.FindingId ==
                        QaFindingIds.StrategySourceRateMarketWarning
                    || finding.FindingId ==
                        QaFindingIds.StrategySourceRateMarketFailure)
                .ToArray();

            Check(
                !report.Findings.Any(finding => StrategyCheckIds.Any(checkId =>
                    finding.FindingId == QaFindingIds.WarningForCheck(checkId)
                    || finding.FindingId == QaFindingIds.FailureForCheck(checkId))),
                $"Strategy mask {mask}: an individual strategy finding was generated.");

            if (missingCount == 0)
            {
                Check(
                    aggregate.Length == 0,
                    "All strategy columns available still generated a finding.");
            }
            else if (missingCount is 1 or 2)
            {
                string expectedSentence = CreateExpectedStrategyWarning(
                    missingCategories);
                Check(
                    aggregate.Length == 1
                    && aggregate[0].FindingId ==
                        QaFindingIds.StrategySourceRateMarketWarning
                    && aggregate[0].Severity == QaFindingSeverity.Warning
                    && aggregate[0].Title.StartsWith(
                        "Strategy Warning",
                        StringComparison.Ordinal)
                    && aggregate[0].Description == expectedSentence,
                    $"Strategy mask {mask}: aggregate Strategy Warning is incorrect.");
            }
            else
            {
                Check(
                    aggregate.Length == 1
                    && aggregate[0].FindingId ==
                        QaFindingIds.StrategySourceRateMarketFailure
                    && aggregate[0].Severity == QaFindingSeverity.Failure
                    && aggregate[0].Description ==
                        "Source, Rate, and Market strategy columns are all unavailable.",
                    "All strategy columns missing did not create exactly one aggregate Failure.");
            }

            QaReportStatus expectedStatus = missingCount switch
            {
                0 => QaReportStatus.Pass,
                1 or 2 => QaReportStatus.PassWithWarnings,
                3 => QaReportStatus.Fail,
                _ => throw new InvalidOperationException()
            };
            QaReportValidationResult validation = Validate(report);
            AssertReady(validation, expectedStatus, $"strategy mask {mask}");
            Check(
                validation.WarningFindingCount == (missingCount is 1 or 2 ? 1 : 0)
                && validation.FailureFindingCount == (missingCount == 3 ? 1 : 0),
                $"Strategy mask {mask}: validation finding counts are incorrect.");
        }
    }

    private static void TestIndexCompatibility()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DocumentationLoggingDashboard.DetailedIndex.{Guid.NewGuid():N}");

        try
        {
            QaStoragePaths paths = new(root);
            Directory.CreateDirectory(paths.IndexRootPath);
            QaReportIndexService service = new(paths);

            QaReportIndexEntry legacy = CreateIndexEntry(
                "INDEX-OLD",
                new QaFileMonth(2026, 6),
                new DateOnly(2026, 7, 1),
                fileId: null);
            QaReportIndexEntry current = CreateIndexEntry(
                "INDEX-NEW",
                new QaFileMonth(2026, 7),
                new DateOnly(2026, 7, 2),
                fileId: "001234");

            WriteIndex(paths, SerializeIndexEntry(legacy));
            IReadOnlyList<QaReportIndexEntry> oldOnly = service.LoadEntries();
            Check(
                oldOnly.Count == 1 && oldOnly[0].FileId is null,
                "A historical 13-line index entry did not load without File ID.");

            WriteIndex(paths, SerializeIndexEntry(current));
            IReadOnlyList<QaReportIndexEntry> newOnly = service.LoadEntries();
            Check(
                newOnly.Count == 1 && newOnly[0].FileId == "001234",
                "A new File-ID index entry did not load or preserve leading zeroes.");

            string mixedSource = SerializeIndexEntry(legacy)
                + SerializeIndexEntry(current);
            WriteIndex(paths, mixedSource);
            IReadOnlyList<QaReportIndexEntry> mixed = service.LoadEntries();
            Check(
                mixed.Count == 2
                && mixed.Single(entry => entry.ReportKey == legacy.ReportKey)
                    .FileId is null
                && mixed.Single(entry => entry.ReportKey == current.ReportKey)
                    .FileId == "001234",
                "Mixed historical/new index entries did not load correctly.");

            QaReportIndexEntry replacement = CreateIndexEntry(
                "INDEX-THIRD",
                new QaFileMonth(2026, 8),
                new DateOnly(2026, 7, 3),
                fileId: "000777");
            byte[] updatedBytes = PrepareIndexUpdate(service, replacement);
            string updatedText = new UTF8Encoding(false, true).GetString(updatedBytes);
            string legacyBlock = ExtractEntryBlock(updatedText, legacy.ReportKey);
            Check(
                !legacyBlock.Contains("File ID: ", StringComparison.Ordinal),
                "Preparing a new entry migrated File ID into a historical block.");
            Check(
                CountOccurrences(updatedText, "File ID: ") == 2,
                "The mixed replacement did not contain exactly the two real File IDs.");

            File.WriteAllBytes(paths.QaReportIndexFilePath, updatedBytes);
            IReadOnlyList<QaReportIndexEntry> updated = service.LoadEntries();
            Check(
                updated.Count == 3
                && updated.Single(entry => entry.ReportKey == legacy.ReportKey)
                    .FileId is null
                && updated.Single(entry => entry.ReportKey == replacement.ReportKey)
                    .FileId == "000777",
                "The no-migration mixed replacement did not round-trip.");

            string duplicatedFileId = SerializeIndexEntry(current).Replace(
                "File ID: 001234\r\n",
                "File ID: 001234\r\nFile ID: 009999\r\n",
                StringComparison.Ordinal);
            WriteIndex(paths, duplicatedFileId);
            bool duplicateRejected = false;
            try
            {
                _ = service.LoadEntries();
            }
            catch (QaReportIndexException)
            {
                duplicateRejected = true;
            }

            Check(
                duplicateRejected,
                "The index parser accepted a duplicate File ID field.");

            string unexpectedField = SerializeIndexEntry(current).Replace(
                "File ID: 001234\r\n",
                "File ID: 001234\r\nUnexpected: value\r\n",
                StringComparison.Ordinal);
            WriteIndex(paths, unexpectedField);
            bool unexpectedRejected = false;
            try
            {
                _ = service.LoadEntries();
            }
            catch (QaReportIndexException)
            {
                unexpectedRejected = true;
            }

            Check(
                unexpectedRejected,
                "The index parser accepted an unexpected field label.");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static QaReport CreateCompleteReport()
    {
        QaReport report = new()
        {
            ReportId = $"synthetic-detailed-{Guid.NewGuid():N}",
            FileId = "001234",
            HotelInformation = new QaHotelInformation
            {
                HotelId = "SYN-DETAIL-001",
                HotelName = "Synthetic Detailed Hotel",
                PmsName = "Synthetic PMS",
                FileMonth = new QaFileMonth(2026, 7)
            },
            QaDate = new DateOnly(2026, 8, 18),
            CreatedBy = "Synthetic QA",
            OriginalFileName = "synthetic-detailed.csv",
            FileCharacteristics = new QaFileCharacteristics
            {
                NameColumnMode = QaNameColumnMode.SeparateFirstAndLastName,
                HasCurrencyColumn = false,
                MonetaryColumnScenario =
                    QaMonetaryColumnScenario.OneMonetaryColumn,
                HasMultipleConfirmationNumberCandidateColumns = false,
                IsCustomScriptSupportAvailable = false,
                HasRejectedDatabaseRecords = false
            },
            Statistics = new QaStatistics
            {
                FileInformation = new QaFileInformationStatistics
                {
                    TotalDataRows = 100,
                    HeadersArePresent = true,
                    UsefulHeaders = QaUsefulHeadersResult.Yes,
                    DataStartRow = 2
                },
                FileMonth = new QaFileMonthStatistics
                {
                    ValidArrivalDateCount = 100,
                    ArrivalDatesWithinFileMonth = 100,
                    ArrivalDatesOutsideFileMonth = 0
                },
                Database = new QaDatabaseStatistics
                {
                    ImportedRecordCount = 100,
                    RejectedRecordCount = 0,
                    RecordsWithMissingRequiredDatabaseValues = 0
                }
            },
            GeneralNotes = "Synthetic field-level QA evidence only."
        };

        foreach (QaCheckDefinition definition in QaChecklistCatalog.Definitions)
        {
            report.ChecklistResults.Add(new QaCheckResult
            {
                CheckId = definition.Id,
                Status = IsApplicable(definition, report.FileCharacteristics)
                    ? QaCheckStatus.Pass
                    : QaCheckStatus.NotApplicable,
                ResultSource = QaResultSource.Manual
            });
        }

        Synchronize(report);
        return report;
    }

    private static void Synchronize(QaReport report)
    {
        CalculationService.Synchronize(report);
        FindingService.SynchronizeFindings(report);
    }

    private static QaReportValidationResult Validate(QaReport report)
    {
        return ValidationService.Validate(report, [CanonicalHotel()]);
    }

    private static void SetStatus(
        QaReport report,
        string checkId,
        QaCheckStatus status)
    {
        QaCheckResult result = report.ChecklistResults.Single(candidate =>
            candidate.CheckId == checkId);
        result.Status = status;
        result.ResultSource = QaResultSource.Manual;
        result.Notes = null;
    }

    private static bool IsApplicable(
        QaCheckDefinition definition,
        QaFileCharacteristics characteristics)
    {
        return definition.Applicability switch
        {
            QaCheckApplicability.Always => true,
            QaCheckApplicability.SeparateNameColumns =>
                characteristics.NameColumnMode ==
                    QaNameColumnMode.SeparateFirstAndLastName,
            QaCheckApplicability.FullNameColumn =>
                characteristics.NameColumnMode == QaNameColumnMode.FullName,
            QaCheckApplicability.CurrencyColumnPresent =>
                characteristics.HasCurrencyColumn,
            QaCheckApplicability.OneMonetaryColumn =>
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.OneMonetaryColumn,
            QaCheckApplicability.TwoMonetaryColumns =>
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.TwoMonetaryColumns,
            QaCheckApplicability.MoreThanTwoMonetaryColumns =>
                characteristics.MonetaryColumnScenario ==
                    QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns,
            QaCheckApplicability.RejectedRecordsPresent =>
                characteristics.HasRejectedDatabaseRecords,
            _ => false
        };
    }

    private static QaHotelMetadata CanonicalHotel()
    {
        return new QaHotelMetadata
        {
            HotelId = "SYN-DETAIL-001",
            HotelName = "Synthetic Detailed Hotel",
            PmsName = "Synthetic PMS",
            FolderName = "Synthetic Detailed Hotel - SYN-DETAIL-001"
        };
    }

    private static QaPmsMetadata CanonicalPms()
    {
        return new QaPmsMetadata
        {
            PmsName = "Synthetic PMS",
            FolderName = "Synthetic PMS"
        };
    }

    private static string BuildPdfDdl(
        QaReport report,
        QaReportValidationResult validation,
        DateTimeOffset generatedAt)
    {
        Assembly assembly = typeof(QaPdfGenerationService).Assembly;
        Type builderType = assembly.GetType(
                "DocumentationLoggingDashboard.QAReports.Pdf.QaPdfDocumentBuilder",
                throwOnError: true)!
            ?? throw new InvalidOperationException("PDF builder type was not found.");
        object builder = Activator.CreateInstance(builderType, nonPublic: true)
            ?? throw new InvalidOperationException("PDF builder could not be created.");
        MethodInfo build = builderType.GetMethod(
                "Build",
                BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("PDF builder method was not found.");
        object document = build.Invoke(
                builder,
                [report, validation, generatedAt])
            ?? throw new InvalidOperationException("PDF document was not created.");
        Type ddlWriterType = document.GetType().Assembly.GetType(
                "MigraDoc.DocumentObjectModel.IO.DdlWriter",
                throwOnError: true)!
            ?? throw new InvalidOperationException("MigraDoc DDL writer was not found.");
        MethodInfo writeToString = ddlWriterType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == "WriteToString"
                && method.GetParameters().Length == 1
                && method.GetParameters()[0].ParameterType.IsInstanceOfType(
                    document));
        return (string)(writeToString.Invoke(null, [document])
            ?? throw new InvalidOperationException("PDF DDL was not produced."));
    }

    private static QaReportIndexEntry CreateIndexEntry(
        string hotelId,
        QaFileMonth fileMonth,
        DateOnly qaDate,
        string? fileId)
    {
        string hotelName = "Index Hotel " + hotelId;
        const string pmsName = "Index PMS";
        QaHotelMetadata hotel = new()
        {
            HotelId = hotelId,
            HotelName = hotelName,
            PmsName = pmsName
        };
        QaPmsMetadata pms = new() { PmsName = pmsName };
        QaReport report = new()
        {
            FileId = fileId ?? string.Empty,
            QaDate = qaDate,
            HotelInformation = new QaHotelInformation
            {
                HotelId = hotelId,
                HotelName = hotelName,
                PmsName = pmsName,
                FileMonth = fileMonth
            }
        };
        string filename = new QaReportFilenameService(
                new QaFolderNameSanitizer())
            .CreateFilename(report, hotel, pms);
        string hotelFolder = "Index-Hotel-" + hotelId;
        string pmsFolder = "Index-PMS";

        return new QaReportIndexEntry(
            new QaReportKey(hotelId, fileMonth),
            qaDate,
            hotelName,
            hotelId,
            pmsName,
            fileMonth,
            fileId,
            QaReportStatus.Pass,
            "Synthetic QA",
            filename,
            $"ByHotel/{hotelFolder}/{filename}",
            $"ByPMS/{pmsFolder}/{filename}",
            new DateTimeOffset(
                qaDate.Year,
                qaDate.Month,
                qaDate.Day,
                12,
                0,
                0,
                TimeSpan.Zero));
    }

    private static string SerializeIndexEntry(QaReportIndexEntry entry)
    {
        StringBuilder builder = new();
        AppendIndexField(builder, "ReportKey", entry.ReportKey.ToString());
        AppendIndexField(
            builder,
            "QA Date",
            entry.QaDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendIndexField(builder, "Hotel", entry.HotelName);
        AppendIndexField(builder, "Hotel ID", entry.HotelId);
        AppendIndexField(builder, "PMS", entry.PmsName);
        AppendIndexField(builder, "File Month", entry.FileMonth.ToString());
        if (entry.FileId is not null)
        {
            AppendIndexField(builder, "File ID", entry.FileId);
        }

        AppendIndexField(builder, "Status", "Pass");
        AppendIndexField(builder, "Created By", entry.EffectiveCreatedBy);
        AppendIndexField(builder, "Filename", entry.Filename);
        AppendIndexField(builder, "Hotel Copy", entry.RelativeHotelCopyPath);
        AppendIndexField(builder, "PMS Copy", entry.RelativePmsCopyPath);
        AppendIndexField(
            builder,
            "Saved At UTC",
            entry.SavedAtUtc.UtcDateTime.ToString(
                "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
                CultureInfo.InvariantCulture));
        builder.Append("---\r\n");
        return builder.ToString();
    }

    private static void AppendIndexField(
        StringBuilder builder,
        string label,
        string value)
    {
        builder.Append(label)
            .Append(": ")
            .Append(value)
            .Append("\r\n");
    }

    private static void WriteIndex(QaStoragePaths paths, string content)
    {
        File.WriteAllText(
            paths.QaReportIndexFilePath,
            content,
            new UTF8Encoding(false, true));
    }

    private static byte[] PrepareIndexUpdate(
        QaReportIndexService service,
        QaReportIndexEntry entry)
    {
        MethodInfo prepare = typeof(QaReportIndexService).GetMethod(
                "PrepareUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "QaReportIndexService.PrepareUpdate was not found.");
        object plan = prepare.Invoke(service, [entry])
            ?? throw new InvalidOperationException(
                "QaReportIndexService did not return an update plan.");
        PropertyInfo updatedContent = plan.GetType().GetProperty(
                "UpdatedContent",
                BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException(
                "The index update plan has no UpdatedContent property.");
        return ((byte[])(updatedContent.GetValue(plan)
            ?? throw new InvalidOperationException(
                "The index update plan has no updated bytes."))).ToArray();
    }

    private static string ExtractEntryBlock(
        string indexText,
        QaReportKey reportKey)
    {
        string marker = "ReportKey: " + reportKey + "\r\n";
        int start = indexText.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException(
                $"Index block '{reportKey}' was not found.");
        }

        int end = indexText.IndexOf("---\r\n", start, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException(
                $"Index block '{reportKey}' has no terminator.");
        }

        return indexText[start..end];
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int start = 0;

        while ((start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }

    private static string CreateExpectedStrategyWarning(
        IReadOnlyList<string> missingCategories)
    {
        return missingCategories.Count switch
        {
            1 => $"Strategy Warning: {missingCategories[0]} column is not available.",
            2 => $"Strategy Warning: {missingCategories[0]} and {missingCategories[1]} columns are not available.",
            _ => throw new ArgumentOutOfRangeException(nameof(missingCategories))
        };
    }

    private static void AssertReady(
        QaReportValidationResult result,
        QaReportStatus expectedStatus,
        string scenario)
    {
        Check(
            result.IsReady && result.BlockingErrors.Count == 0,
            $"{scenario}: report was not ready: {string.Join(" | ", result.BlockingErrors)}");
        Check(
            result.CalculatedStatus == expectedStatus,
            $"{scenario}: status was {result.CalculatedStatus}, expected {expectedStatus}.");
        Check(
            !string.IsNullOrWhiteSpace(result.ValidatedReportFingerprint),
            $"{scenario}: readiness fingerprint is missing.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
