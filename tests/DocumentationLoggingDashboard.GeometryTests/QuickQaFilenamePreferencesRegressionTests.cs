using System.Text.Json;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.GeometryTests;

/// <summary>
/// Focused regression coverage for Quick QA filename, preference, and storage
/// foundations. Program.cs invokes <see cref="RunAll"/> with the other semantic
/// suites.
/// </summary>
internal static class QuickQaFilenamePreferencesRegressionTests
{
    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        (string Name, Action Body)[] tests =
        [
            ("workbook filename normalization and rejection", TestWorkbookFilenameRules),
            ("Surface QA direct-child resolution and no-overwrite", TestDirectChildAndNoOverwrite),
            ("filename-only preference lifecycle", TestPreferenceLifecycle),
            ("strict preference JSON rejection", TestInvalidPreferenceDocuments),
            ("Surface QA initializer and canonical history paths", TestStorageInitializationAndPaths)
        ];

        int failed = 0;
        output.WriteLine($"Quick QA filename/preferences harness: {tests.Length} tests");

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
                ? $"[PASS] All {tests.Length} Quick QA filename/preferences tests passed."
                : $"[FAIL] {failed} of {tests.Length} Quick QA filename/preferences tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void TestWorkbookFilenameRules()
    {
        using TemporaryQaRoot fixture = new();
        QuickQaWorkbookFilenameService service = fixture.FilenameService;

        CheckEqual(
            "Surface QA August.xlsx",
            service.CreateSafeWorkbookFileName("  Surface QA August  "),
            "Legitimate spaces or automatic .xlsx addition changed unexpectedly.");
        CheckEqual(
            "Surface QA August.xlsx",
            service.CreateSafeWorkbookFileName("Surface QA August.XLSX"),
            "The supplied .xlsx extension was not canonicalized.");
        CheckEqual(
            "Surface_QA_August_.xlsx",
            service.CreateSafeWorkbookFileName("Surface:QA*August?.xlsx"),
            "Invalid Windows filename characters were not sanitized predictably.");
        CheckEqual(
            "Trimmed QA.xlsx",
            service.CreateSafeWorkbookFileName("  Trimmed QA .xlsx  "),
            "Surrounding whitespace or the trailing stem space was not trimmed.");

        AssertThrows<ArgumentException>(
            () => service.CreateSafeWorkbookFileName("."),
            "The current-directory traversal token was accepted.");
        AssertThrows<ArgumentException>(
            () => service.CreateSafeWorkbookFileName(".."),
            "The parent-directory traversal token was accepted.");
        AssertThrows<ArgumentException>(
            () => service.CreateSafeWorkbookFileName("folder\\Surface QA.xlsx"),
            "A backslash path separator was accepted.");
        AssertThrows<ArgumentException>(
            () => service.CreateSafeWorkbookFileName("folder/Surface QA.xlsx"),
            "A forward-slash path separator was accepted.");

        string volumeRoot = Path.GetPathRoot(fixture.RootPath)
            ?? throw new RegressionAssertionException("The temporary root has no volume root.");
        string rootedPath = Path.Combine(volumeRoot, "outside", "Surface QA.xlsx");
        AssertThrows<ArgumentException>(
            () => service.CreateSafeWorkbookFileName(rootedPath),
            "A rooted workbook path was accepted.");

        AssertThrows<ArgumentException>(
            () => service.CreateSafeWorkbookFileName("Surface QA.xls"),
            "A non-xlsx extension was accepted.");
        AssertThrows<ArgumentException>(
            () => service.CreateSafeWorkbookFileName("Surface QA.xlsx.exe"),
            "A trailing extension escaped the .xlsx contract.");

        foreach (string reservedName in new[] { "CON", "PRN.xlsx", "LPT1.xlsx", "COM9.xlsx" })
        {
            AssertThrows<ArgumentException>(
                () => service.CreateSafeWorkbookFileName(reservedName),
                $"Reserved Windows device name '{reservedName}' was accepted.");
        }

        string overlongStem = new(
            'A',
            QuickQaWorkbookFilenameService.MaximumWorkbookFileNameLength);
        AssertThrows<ArgumentException>(
            () => service.CreateSafeWorkbookFileName(overlongStem),
            "A workbook filename over the documented limit was accepted.");
    }

    private static void TestDirectChildAndNoOverwrite()
    {
        using TemporaryQaRoot fixture = new();

        string path = fixture.FilenameService.ResolveNewWorkbookPath("Surface QA August");
        CheckEqual(
            Path.GetFullPath(fixture.Paths.SurfaceQaRootPath),
            Path.GetFullPath(Path.GetDirectoryName(path)!),
            "The resolved workbook was not a direct child of SurfaceQA.");
        CheckEqual(
            "Surface QA August.xlsx",
            Path.GetFileName(path),
            "The resolved workbook leaf filename was unexpected.");

        string storageResolved = fixture.Paths.ResolveSurfaceQaWorkbookPath(
            "Another Surface QA.xlsx");
        CheckEqual(
            Path.GetFullPath(fixture.Paths.SurfaceQaRootPath),
            Path.GetFullPath(Path.GetDirectoryName(storageResolved)!),
            "QaStoragePaths did not preserve the strict direct-child contract.");

        AssertThrows<ArgumentException>(
            () => fixture.Paths.ResolveSurfaceQaWorkbookPath("..\\Escape.xlsx"),
            "QaStoragePaths accepted a traversal filename.");
        AssertThrows<ArgumentException>(
            () => fixture.Paths.ResolveSurfaceQaWorkbookPath("nested/Surface QA.xlsx"),
            "QaStoragePaths accepted a nested workbook filename.");
        AssertThrows<ArgumentException>(
            () => fixture.Paths.ResolveSurfaceQaWorkbookPath(
                Path.Combine(Path.GetPathRoot(fixture.RootPath)!, "Outside.xlsx")),
            "QaStoragePaths accepted a rooted workbook filename.");

        const string sentinel = "existing-workbook-sentinel";
        File.WriteAllText(path, sentinel);
        AssertThrows<IOException>(
            () => fixture.FilenameService.ResolveNewWorkbookPath("Surface QA August.xlsx"),
            "An existing workbook destination was offered for replacement.");
        CheckEqual(
            sentinel,
            File.ReadAllText(path),
            "The existing workbook was modified during the no-overwrite check.");
    }

    private static void TestPreferenceLifecycle()
    {
        using TemporaryQaRoot fixture = new();

        Check(
            fixture.PreferencesService.LoadLastUsedWorkbookFileName() is null,
            "A missing preference file did not load as an empty preference.");
        Check(
            !File.Exists(fixture.Paths.QuickQaSettingsFilePath),
            "Loading a missing preference unexpectedly created the settings file.");

        const string workbookA = "Surface QA A.xlsx";
        const string workbookB = "Surface QA B.xlsx";
        string workbookAPath = fixture.Paths.ResolveSurfaceQaWorkbookPath(workbookA);
        string workbookBPath = fixture.Paths.ResolveSurfaceQaWorkbookPath(workbookB);
        File.WriteAllBytes(workbookAPath, [0x41]);
        File.WriteAllBytes(workbookBPath, [0x42]);

        fixture.PreferencesService.SaveLastUsedWorkbookFileName(workbookA);
        CheckEqual(
            workbookA,
            fixture.PreferencesService.LoadLastUsedWorkbookFileName(),
            "The first selected workbook did not round-trip.");
        AssertFilenameOnlySettings(fixture, workbookA);
        AssertNoAtomicTemporaryFiles(fixture.Paths.MetadataRootPath);

        fixture.PreferencesService.SaveLastUsedWorkbookFileName(workbookB);
        CheckEqual(
            workbookB,
            fixture.PreferencesService.LoadLastUsedWorkbookFileName(),
            "Switching the remembered workbook from A to B did not persist.");
        AssertFilenameOnlySettings(fixture, workbookB);
        AssertNoAtomicTemporaryFiles(fixture.Paths.MetadataRootPath);

        File.Delete(workbookBPath);
        CheckEqual(
            workbookB,
            fixture.PreferencesService.LoadLastUsedWorkbookFileName(),
            "Deleting a remembered workbook caused preference loading to fail or discard its filename.");
        QuickQaPreferencesException disappeared =
            AssertThrows<QuickQaPreferencesException>(
                () => fixture.PreferencesService
                    .SaveLastUsedWorkbookFileName(workbookB),
                "A workbook that disappeared after discovery did not produce a preferences-domain failure.");
        CheckEqual(
            fixture.Paths.QuickQaSettingsFilePath,
            disappeared.SettingsFilePath,
            "The disappeared-workbook failure did not identify the Quick QA settings file.");

        fixture.PreferencesService.ClearLastUsedWorkbookFileName();
        Check(
            fixture.PreferencesService.LoadLastUsedWorkbookFileName() is null,
            "Clearing the remembered workbook did not produce an empty preference.");
        AssertClearedSettings(fixture);
        AssertNoAtomicTemporaryFiles(fixture.Paths.MetadataRootPath);
    }

    private static void TestInvalidPreferenceDocuments()
    {
        using TemporaryQaRoot fixture = new();

        (string Name, string Json)[] invalidDocuments =
        [
            ("corrupt JSON", "{ not-json"),
            (
                "unknown property",
                "{\"schemaVersion\":1,\"lastUsedWorkbookFileName\":null,\"absolutePath\":\"C:\\\\escape.xlsx\"}"),
            (
                "duplicate property",
                "{\"schemaVersion\":1,\"SchemaVersion\":1,\"lastUsedWorkbookFileName\":null}"),
            (
                "unsupported schema",
                "{\"schemaVersion\":2,\"lastUsedWorkbookFileName\":null}"),
            (
                "absolute remembered path",
                "{\"schemaVersion\":1,\"lastUsedWorkbookFileName\":\"C:\\\\outside\\\\Surface QA.xlsx\"}")
        ];

        foreach ((string name, string json) in invalidDocuments)
        {
            File.WriteAllText(fixture.Paths.QuickQaSettingsFilePath, json);
            QuickQaPreferencesException exception = AssertThrows<QuickQaPreferencesException>(
                () => fixture.PreferencesService.LoadLastUsedWorkbookFileName(),
                $"The {name} settings document was accepted.");
            CheckEqual(
                fixture.Paths.QuickQaSettingsFilePath,
                exception.SettingsFilePath,
                $"The {name} failure did not identify the Quick QA settings file.");
        }
    }

    private static void TestStorageInitializationAndPaths()
    {
        using TemporaryQaRoot fixture = new();

        string expectedSurfaceRoot = Path.GetFullPath(Path.Combine(
            fixture.RootPath,
            "QAReports",
            "SurfaceQA"));
        string expectedSettingsPath = Path.GetFullPath(Path.Combine(
            fixture.RootPath,
            "QAReports",
            "Metadata",
            "quick-qa-settings.json"));

        CheckEqual(
            expectedSurfaceRoot,
            fixture.Paths.SurfaceQaRootPath,
            "SurfaceQaRootPath does not use the required canonical location.");
        Check(
            Directory.Exists(fixture.Paths.SurfaceQaRootPath),
            "QaStorageInitializer did not create the SurfaceQA directory.");
        CheckEqual(
            expectedSettingsPath,
            fixture.Paths.QuickQaSettingsFilePath,
            "QuickQaSettingsFilePath does not use QAReports/Metadata.");

        const string canonicalHotelFolder = "H-100_HarbourHotel";
        string expectedHistoryPath = Path.GetFullPath(Path.Combine(
            fixture.Paths.ByHotelRootPath,
            canonicalHotelFolder,
            "QuickQAHistory.xlsx"));
        string historyPath = fixture.Paths.ResolveQuickQaHistoryPath(canonicalHotelFolder);
        CheckEqual(
            expectedHistoryPath,
            historyPath,
            "Quick QA history did not resolve to the canonical Hotel folder and filename.");
        CheckEqual(
            Path.GetFullPath(fixture.Paths.ResolveHotelDirectory(canonicalHotelFolder)),
            Path.GetFullPath(Path.GetDirectoryName(historyPath)!),
            "Quick QA history was not a direct child of the canonical Hotel directory.");

        AssertThrows<ArgumentException>(
            () => fixture.Paths.ResolveQuickQaHistoryPath("..\\Outside"),
            "Quick QA history accepted a non-canonical Hotel path.");
    }

    private static void AssertFilenameOnlySettings(
        TemporaryQaRoot fixture,
        string expectedFileName)
    {
        string json = File.ReadAllText(fixture.Paths.QuickQaSettingsFilePath);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonProperty[] properties = root.EnumerateObject().ToArray();

        Check(
            root.ValueKind == JsonValueKind.Object,
            "The Quick QA settings root is not an object.");
        Check(
            properties.Length == 2,
            "The Quick QA settings document persisted fields beyond schema and filename.");
        Check(
            root.GetProperty("schemaVersion").GetInt32()
                == QuickQaPreferencesService.CurrentSchemaVersion,
            "The Quick QA settings schema version was not persisted.");
        CheckEqual(
            expectedFileName,
            root.GetProperty("lastUsedWorkbookFileName").GetString(),
            "The settings document did not persist the selected leaf filename.");
        Check(
            !json.Contains(fixture.RootPath, StringComparison.OrdinalIgnoreCase)
            && !json.Contains(fixture.Paths.SurfaceQaRootPath, StringComparison.OrdinalIgnoreCase),
            "The settings document leaked an absolute documentation or SurfaceQA path.");
        Check(
            !Path.IsPathRooted(root.GetProperty("lastUsedWorkbookFileName").GetString()),
            "The persisted workbook preference is rooted instead of filename-only.");
    }

    private static void AssertClearedSettings(TemporaryQaRoot fixture)
    {
        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllText(fixture.Paths.QuickQaSettingsFilePath));
        JsonElement root = document.RootElement;

        Check(
            root.GetProperty("schemaVersion").GetInt32()
                == QuickQaPreferencesService.CurrentSchemaVersion,
            "Clearing the preference damaged the settings schema version.");
        Check(
            root.GetProperty("lastUsedWorkbookFileName").ValueKind == JsonValueKind.Null,
            "Clearing the preference did not atomically persist a null filename.");
    }

    private static void AssertNoAtomicTemporaryFiles(string metadataRootPath)
    {
        string[] temporaryFiles = Directory.GetFiles(
            metadataRootPath,
            ".quick-qa-settings.json.*.tmp",
            SearchOption.TopDirectoryOnly);
        Check(
            temporaryFiles.Length == 0,
            "An atomic preference operation left a temporary file behind.");
    }

    private static TException AssertThrows<TException>(
        Action action,
        string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }
        catch (Exception exception)
        {
            throw new RegressionAssertionException(
                $"{message} Expected {typeof(TException).Name}, but received "
                + $"{exception.GetType().Name}.",
                exception);
        }

        throw new RegressionAssertionException(
            $"{message} Expected {typeof(TException).Name}, but no exception was thrown.");
    }

    private static void CheckEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new RegressionAssertionException(
                $"{message} Expected '{expected}', received '{actual}'.");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new RegressionAssertionException(message);
        }
    }

    private sealed class TemporaryQaRoot : IDisposable
    {
        private static readonly string TemporaryParentPath = Path.GetFullPath(Path.Combine(
            Path.GetTempPath(),
            "DocumentationLoggingDashboard.QuickQaFilenamePreferencesRegressionTests"));

        public TemporaryQaRoot()
        {
            RootPath = Path.GetFullPath(Path.Combine(
                TemporaryParentPath,
                "r-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(RootPath);

            Paths = new QaStoragePaths(RootPath);
            new QaStorageInitializer(Paths).Initialize();
            FilenameService = new QuickQaWorkbookFilenameService(Paths);
            PreferencesService = new QuickQaPreferencesService(Paths, FilenameService);
        }

        public string RootPath { get; }

        public QaStoragePaths Paths { get; }

        public QuickQaWorkbookFilenameService FilenameService { get; }

        public QuickQaPreferencesService PreferencesService { get; }

        public void Dispose()
        {
            string? actualParent = Path.GetDirectoryName(
                Path.TrimEndingDirectorySeparator(RootPath));
            if (!string.Equals(
                actualParent,
                TemporaryParentPath,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new RegressionAssertionException(
                    "Refused to clean a temporary root outside the dedicated test parent.");
            }

            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }

    private sealed class RegressionAssertionException : Exception
    {
        public RegressionAssertionException(string message)
            : base(message)
        {
        }

        public RegressionAssertionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
