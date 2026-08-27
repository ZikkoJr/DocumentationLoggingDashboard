using DocumentationLoggingDashboard.DocumentationLogs;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.QAReports.Models;

namespace DocumentationLoggingDashboard.GeometryTests;

internal static class DocumentationLogHotelIdParserRegressionTests
{
    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        (string Name, Action Body)[] tests =
        [
            ("delimiter normalization, duplicates, ordering, and leading zeroes", TestParserContract),
            ("fresh canonical Hotel/PMS resolution and unique PMS routing", TestCanonicalResolution),
            ("unknown and mixed IDs block the complete draft", TestUnknownIdsBlock),
            ("Debugging stale or removed Hotel/PMS snapshots are rejected", TestDebuggingStaleSnapshot),
            ("required business fields block while optional blanks normalize to N/A", TestBusinessFieldValidation)
        ];
        return Run(tests, output);
    }

    private static void TestParserContract()
    {
        HotelIdListParser parser = new();
        IReadOnlyList<string> parsed = parser.Parse(
            " 1953, 2093;1953\r\n0012\n3001; ; ");
        DocumentationLogTestAssert.True(
            parsed.SequenceEqual(
                ["1953", "2093", "0012", "3001"],
                StringComparer.Ordinal),
            "The parser did not preserve first-entered order, leading zeroes, or stable case-insensitive deduplication.");
        DocumentationLogTestAssert.Equal(
            "1953; 2093; 0012; 3001",
            parser.FormatCanonical(parsed),
            "The canonical workbook Hotel IDs representation changed.");
        DocumentationLogTestAssert.Equal(
            0,
            parser.Parse(" , ; \r\n ").Count,
            "Empty tokens were not removed.");
    }

    private static void TestCanonicalResolution()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.ScriptEditingLog,
            "Parser Resolution");
        DocumentationLogResolvedDraft draft = environment.RoutingService.ResolveDraft(
            environment.CreateScriptRequest(
                LogType.ScriptEditingLog,
                running,
                "2093, 1953; 3001\r\n0012;2093"));

        DocumentationLogTestAssert.True(
            draft.Hotels.Select(hotel => hotel.HotelId).SequenceEqual(
                ["2093", "1953", "3001", "0012"],
                StringComparer.Ordinal),
            "Manual IDs were not resolved to canonical metadata in first-entered order.");
        DocumentationLogTestAssert.True(
            draft.UniquePmsSystems.Select(pms => pms.PmsName).SequenceEqual(
                ["Mews", "Opera"],
                StringComparer.Ordinal),
            "Canonical PMS routing was not deduplicated in stable Hotel order.");
    }

    private static void TestUnknownIdsBlock()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.ScriptCreationLog,
            "Parser Unknown");

        DocumentationLogSaveException single =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => environment.RoutingService.ResolveDraft(
                    environment.CreateScriptRequest(
                        LogType.ScriptCreationLog,
                        running,
                        "2093" + Environment.NewLine + "UNKNOWN")),
                "A mixed valid/unknown Hotel list did not block the full draft.");
        DocumentationLogTestAssert.True(
            single.ErrorCategory == DocumentationLogSaveErrorCategory.Validation
            && single.Message.Contains("UNKNOWN", StringComparison.Ordinal),
            "The single unknown-ID error was not concise and operator-safe.");

        DocumentationLogSaveException multiple =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => environment.RoutingService.ResolveDraft(
                    environment.CreateScriptRequest(
                        LogType.ScriptCreationLog,
                        running,
                        "BAD-A; 1953; BAD-B")),
                "Multiple unknown IDs did not block the full draft.");
        DocumentationLogTestAssert.True(
            multiple.Message.Contains("BAD-A", StringComparison.Ordinal)
            && multiple.Message.Contains("BAD-B", StringComparison.Ordinal)
            && multiple.Message.Contains("blocked", StringComparison.OrdinalIgnoreCase),
            "The multiple unknown-ID message did not identify every blocked value.");
    }

    private static void TestDebuggingStaleSnapshot()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string running = environment.CreateRunning(
            LogType.DebuggingLog,
            "Debug Stale");
        DocumentationLogResolvedDraft valid = environment.RoutingService.ResolveDraft(
            environment.CreateDebuggingRequest(running));
        DocumentationLogTestAssert.Equal(
            "1953",
            valid.Hotels.Single().HotelId,
            "A current Debugging Hotel snapshot did not resolve.");

        QaHotelMetadata staleHotel = new()
        {
            HotelId = environment.Hotel1953.HotelId,
            HotelName = environment.Hotel1953.HotelName,
            PmsName = environment.Hotel1953.PmsName,
            FolderName = "stale-routing-folder"
        };
        DocumentationLogSaveException stale =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => environment.RoutingService.ResolveDraft(
                    environment.CreateDebuggingRequest(
                        running,
                        staleHotel,
                        environment.Mews)),
                "A stale Debugging Hotel routing snapshot was accepted.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveErrorCategory.MetadataMismatch,
            stale.ErrorCategory,
            "Stale Debugging metadata used the wrong failure category.");

        environment.MetadataService.SaveHotels(new QaHotelMetadataDocument
        {
            SchemaVersion = QaHotelMetadataDocument.CurrentSchemaVersion,
            Hotels = environment.MetadataService.LoadHotels()
                .Where(hotel => !hotel.HotelId.Equals(
                    environment.Hotel1953.HotelId,
                    StringComparison.Ordinal))
                .Select<QaHotelMetadata, QaHotelMetadata?>(hotel => hotel)
                .ToList()
        });
        DocumentationLogSaveException removed =
            DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => environment.RoutingService.ResolveDraft(
                    environment.CreateDebuggingRequest(running)),
                "A removed Debugging Hotel selection was accepted at fresh resolution.");
        DocumentationLogTestAssert.Equal(
            DocumentationLogSaveErrorCategory.MetadataMismatch,
            removed.ErrorCategory,
            "A removed Debugging Hotel used the wrong failure category.");
    }

    private static void TestBusinessFieldValidation()
    {
        using DocumentationLogSyntheticEnvironment environment = new();
        string debugRunning = environment.CreateRunning(
            LogType.DebuggingLog,
            "Debug Required");
        Dictionary<string, string> validDebug = new(StringComparer.Ordinal)
        {
            [DocumentationLogFieldKeys.ErrorShownOnTicket] = "Error",
            [DocumentationLogFieldKeys.RootCause] = "Cause",
            [DocumentationLogFieldKeys.FixApplied] = "Fix",
            [DocumentationLogFieldKeys.CreatedBy] = string.Empty,
            [DocumentationLogFieldKeys.NotesFollowUp] = string.Empty
        };

        foreach ((string key, string label) in new[]
                 {
                     (DocumentationLogFieldKeys.ErrorShownOnTicket, "Error Shown On Ticket"),
                     (DocumentationLogFieldKeys.RootCause, "Root Cause"),
                     (DocumentationLogFieldKeys.FixApplied, "Fix Applied")
                 })
        {
            Dictionary<string, string> fields = new(validDebug, StringComparer.Ordinal)
            {
                [key] = " "
            };
            DocumentationLogSaveException exception =
                DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                    () => environment.RoutingService.ResolveDraft(
                        new DocumentationLogSaveRequest(
                            LogType.DebuggingLog,
                            debugRunning,
                            fields,
                            environment.Hotel1953,
                            environment.Mews)),
                    $"Blank {label} did not block Debugging.");
            DocumentationLogTestAssert.True(
                exception.ErrorCategory == DocumentationLogSaveErrorCategory.Validation
                && exception.Message.Contains(label, StringComparison.Ordinal),
                $"Blank {label} did not return a focused validation error.");
        }

        DocumentationLogResolvedDraft normalized = environment.RoutingService
            .ResolveDraft(new DocumentationLogSaveRequest(
                LogType.DebuggingLog,
                debugRunning,
                validDebug,
                environment.Hotel1953,
                environment.Mews));
        DocumentationLogTestAssert.Equal(
            "N/A",
            normalized.FieldValues[DocumentationLogFieldKeys.CreatedBy],
            "Blank optional Created By did not normalize to N/A.");
        DocumentationLogTestAssert.Equal(
            "N/A",
            normalized.FieldValues[DocumentationLogFieldKeys.NotesFollowUp],
            "Blank optional Notes did not normalize to N/A.");

        foreach ((LogType type, string[] requiredKeys) in new[]
                 {
                     (LogType.ScriptEditingLog, new[]
                     {
                         DocumentationLogFieldKeys.ScriptName,
                         DocumentationLogFieldKeys.ReasonForEdit,
                         DocumentationLogFieldKeys.ChangesMade
                     }),
                     (LogType.ScriptCreationLog, new[]
                     {
                         DocumentationLogFieldKeys.ScriptName,
                         DocumentationLogFieldKeys.ReasonForCreation,
                         DocumentationLogFieldKeys.ScriptPurpose
                     })
                 })
        {
            string running = environment.CreateRunning(type, $"{type} Required");
            Dictionary<string, string> validFields = CreateValidScriptFields(type);

            _ = DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                () => environment.RoutingService.ResolveDraft(
                    new DocumentationLogSaveRequest(
                        type,
                        running,
                        validFields,
                        hotelIdsInput: string.Empty)),
                $"Blank Hotel ID(s) did not block {type}.");

            foreach (string key in requiredKeys)
            {
                Dictionary<string, string> fields = new(
                    validFields,
                    StringComparer.Ordinal)
                {
                    [key] = string.Empty
                };
                DocumentationLogSaveException exception =
                    DocumentationLogTestAssert.Throws<DocumentationLogSaveException>(
                        () => environment.RoutingService.ResolveDraft(
                            new DocumentationLogSaveRequest(
                                type,
                                running,
                                fields,
                                hotelIdsInput: "1953")),
                        $"Blank required field '{key}' did not block {type}.");
                DocumentationLogTestAssert.Equal(
                    DocumentationLogSaveErrorCategory.Validation,
                    exception.ErrorCategory,
                    $"Blank required field '{key}' used the wrong category.");
            }
        }
    }

    private static Dictionary<string, string> CreateValidScriptFields(
        LogType logType)
    {
        Dictionary<string, string> fields = new(StringComparer.Ordinal)
        {
            [DocumentationLogFieldKeys.ScriptName] = "Synthetic.csx",
            [DocumentationLogFieldKeys.CreatedBy] = string.Empty,
            [DocumentationLogFieldKeys.NotesFollowUp] = string.Empty
        };

        if (logType == LogType.ScriptEditingLog)
        {
            fields[DocumentationLogFieldKeys.ReasonForEdit] = "Reason";
            fields[DocumentationLogFieldKeys.ChangesMade] = "Changes";
        }
        else
        {
            fields[DocumentationLogFieldKeys.ReasonForCreation] = "Reason";
            fields[DocumentationLogFieldKeys.ScriptPurpose] = "Purpose";
        }

        return fields;
    }

    private static int Run(
        IEnumerable<(string Name, Action Body)> tests,
        TextWriter output)
    {
        (string Name, Action Body)[] cases = tests.ToArray();
        int failed = 0;
        output.WriteLine($"Documentation-log Hotel ID parser harness: {cases.Length} tests");

        foreach ((string name, Action body) in cases)
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
                output.WriteLine(exception);
            }
        }

        output.WriteLine(failed == 0
            ? $"[PASS] All {cases.Length} documentation-log Hotel ID parser tests passed."
            : $"[FAIL] {failed} of {cases.Length} documentation-log Hotel ID parser tests failed.");
        return failed == 0 ? 0 : 1;
    }
}
