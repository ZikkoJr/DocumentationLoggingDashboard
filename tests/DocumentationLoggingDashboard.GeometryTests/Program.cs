namespace DocumentationLoggingDashboard.GeometryTests;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        try
        {
            GeometryTestOptions options = GeometryTestOptions.Parse(args);
            if (options.ShowHelp)
            {
                WriteUsage(Console.Out);
                return 0;
            }

            int geometryResult = options.SemanticOnly
                || options.DeferredOnly
                || options.V1Only
                || options.VisibleSmokeOnly
                ? 0
                : QaStatisticsControlGeometryTests.RunAll(Console.Out);
            int semanticResult = options.GeometryOnly
                || options.DeferredOnly
                || options.V1Only
                || options.VisibleSmokeOnly
                ? 0
                : QaBlankBrokenStatisticsRegressionTests.RunAll(Console.Out);
            int saveEvidenceResult = options.GeometryOnly
                || options.DeferredOnly
                || options.V1Only
                || options.VisibleSmoke
                ? 0
                : QaBlankBrokenSavePdfIndexEvidenceTests.RunAll(
                    Console.Out,
                    options.SaveEvidenceDirectory);
            int deferredTextResult = options.GeometryOnly
                || options.SemanticOnly
                || options.V1Only
                || options.VisibleSmoke
                    ? 0
                    : QaDeferredTextCommitRegressionTests.RunAll(Console.Out);
            int v1Result = options.GeometryOnly
                || options.SemanticOnly
                || options.DeferredOnly
                || options.VisibleSmoke
                    ? 0
                    : V1SyntheticRegressionTests.RunAll(Console.Out);
            int result = geometryResult == 0
                && semanticResult == 0
                && saveEvidenceResult == 0
                && deferredTextResult == 0
                && v1Result == 0
                    ? 0
                    : 1;
            if (result != 0 || !options.VisibleSmoke)
            {
                return result;
            }

            int visibleGeometryResult = QaStatisticsControlGeometryTests.RunVisibleSmoke(
                options.ScreenshotDirectory!,
                Console.Out);
            return visibleGeometryResult == 0
                ? QaDeferredTextCommitRegressionTests.RunVisibleSmoke(
                    options.ScreenshotDirectory!,
                    Console.Out)
                : visibleGeometryResult;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[FATAL] {exception}");
            WriteUsage(Console.Error);
            return 2;
        }
    }

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine(
            "Usage: dotnet run --project tests/DocumentationLoggingDashboard.GeometryTests " +
            "[--geometry-only | --semantic-only | --deferred-only | --v1-only] " +
            "[--save-evidence-dir <absolute-external-parent>] " +
            "[(--visible-smoke | --visible-smoke-only) " +
            "--screenshot-dir <absolute-external-directory>]");
        writer.WriteLine(
            "The default run uses and removes an isolated synthetic QA root under the system " +
            "temporary directory. --save-evidence-dir preserves a unique evidence child outside " +
            "the repository. Visible smoke mode never saves a QA report.");
    }
}

internal sealed record GeometryTestOptions(
    bool VisibleSmoke,
    bool VisibleSmokeOnly,
    string? ScreenshotDirectory,
    bool ShowHelp,
    bool GeometryOnly,
    bool SemanticOnly,
    bool DeferredOnly,
    bool V1Only,
    string? SaveEvidenceDirectory)
{
    public static GeometryTestOptions Parse(string[] args)
    {
        bool visibleSmoke = false;
        bool visibleSmokeOnly = false;
        bool showHelp = false;
        bool geometryOnly = false;
        bool semanticOnly = false;
        bool deferredOnly = false;
        bool v1Only = false;
        string? screenshotDirectory = null;
        string? saveEvidenceDirectory = null;

        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--visible-smoke":
                    visibleSmoke = true;
                    break;
                case "--visible-smoke-only":
                    visibleSmoke = true;
                    visibleSmokeOnly = true;
                    break;
                case "--geometry-only":
                    geometryOnly = true;
                    break;
                case "--semantic-only":
                    semanticOnly = true;
                    break;
                case "--deferred-only":
                    deferredOnly = true;
                    break;
                case "--v1-only":
                    v1Only = true;
                    break;
                case "--screenshot-dir" when index + 1 < args.Length:
                    screenshotDirectory = args[++index];
                    break;
                case "--save-evidence-dir" when index + 1 < args.Length:
                    saveEvidenceDirectory = args[++index];
                    break;
                case "--help" or "-h" or "/?":
                    showHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown or incomplete argument '{args[index]}'.");
            }
        }

        if (visibleSmoke && string.IsNullOrWhiteSpace(screenshotDirectory))
        {
            throw new ArgumentException(
                "--visible-smoke requires --screenshot-dir with an absolute path outside the repository.");
        }

        if (!visibleSmoke && screenshotDirectory is not null)
        {
            throw new ArgumentException("--screenshot-dir is only valid with --visible-smoke.");
        }

        if ((geometryOnly ? 1 : 0)
            + (semanticOnly ? 1 : 0)
            + (deferredOnly ? 1 : 0)
            + (v1Only ? 1 : 0) > 1)
        {
            throw new ArgumentException(
                "--geometry-only, --semantic-only, --deferred-only, and --v1-only cannot be combined.");
        }

        if (visibleSmoke && semanticOnly)
        {
            throw new ArgumentException(
                "Visible smoke mode requires the geometry harness.");
        }

        if (visibleSmoke && deferredOnly)
        {
            throw new ArgumentException(
                "Visible smoke mode cannot be combined with --deferred-only.");
        }

        if (visibleSmoke && v1Only)
        {
            throw new ArgumentException(
                "Visible smoke mode cannot be combined with --v1-only.");
        }

        if (visibleSmokeOnly && geometryOnly)
        {
            throw new ArgumentException(
                "--visible-smoke-only cannot be combined with --geometry-only.");
        }

        if (saveEvidenceDirectory is not null && geometryOnly)
        {
            throw new ArgumentException(
                "--save-evidence-dir requires the semantic harness.");
        }

        if (saveEvidenceDirectory is not null && v1Only)
        {
            throw new ArgumentException(
                "--save-evidence-dir cannot be combined with --v1-only.");
        }

        if (saveEvidenceDirectory is not null && visibleSmoke)
        {
            throw new ArgumentException(
                "--save-evidence-dir cannot be combined with no-save visible smoke mode.");
        }

        return new GeometryTestOptions(
            visibleSmoke,
            visibleSmokeOnly,
            screenshotDirectory,
            showHelp,
            geometryOnly,
            semanticOnly,
            deferredOnly,
            v1Only,
            saveEvidenceDirectory);
    }
}
