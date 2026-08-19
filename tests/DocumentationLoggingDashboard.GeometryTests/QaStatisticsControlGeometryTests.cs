using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Forms;
using DocumentationLoggingDashboard.QAReports.Forms.Controls;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.GeometryTests;

internal static class QaStatisticsControlGeometryTests
{
    private const int GwlStyle = -16;
    private const int WsHScroll = 0x00100000;
    private const int WmNcHitTest = 0x0084;
    private const uint PmRemove = 0x0001;
    private const uint WmQuit = 0x0012;
    private const int HtNowhere = 0;
    private const int HtTransparent = -1;

    private static readonly string[] FileInformationInputNames =
    [
        "totalDataRowsNumericUpDown",
        "headersPresentComboBox",
        "usefulHeadersComboBox",
        "dataStartRowNumericUpDown"
    ];

    private static readonly string[] FileInformationLabelTexts =
    [
        "Total Data Rows (excluding header rows)",
        "Headers Present",
        "Useful Headers",
        "Data Start Row (0 = not entered)"
    ];

    public static int RunAll(TextWriter output)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            output.WriteLine("[FAIL] Geometry harness must run on an STA thread.");
            return 1;
        }

        (string Name, Action<TextWriter> Body)[] tests =
        [
            ("real QA form tab initial display and resize lifecycle", TestQaReportFormLifecycle),
            ("initial geometry at supported sizes and font pressure", TestInitialGeometryMatrix),
            ("resize, maximize-sized, and live font changes", TestResizeAndFontLifecycle),
            ("Blank and Broken Auto/manual propagation with bounded events", TestAutoManualPropagation),
            ("maximum applicability and dynamic hide/show", TestDynamicApplicability),
            ("native hit testing and forward/reverse tab traversal", TestHitTestingAndTabTraversal),
            ("outer scrolling and horizontal-scroll suppression", TestScrolling)
        ];

        string? filter = Environment.GetEnvironmentVariable(
            "DDL_GEOMETRY_TEST_FILTER");
        if (!string.IsNullOrWhiteSpace(filter))
        {
            tests = tests
                .Where(test => test.Name.Contains(
                    filter,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            output.WriteLine($"Geometry test filter: {filter}");
            if (tests.Length == 0)
            {
                output.WriteLine("[FAIL] The geometry test filter matched no tests.");
                return 1;
            }
        }

        int failed = 0;
        output.WriteLine($"STA geometry harness: {tests.Length} tests");
        output.WriteLine($"Thread apartment: {Thread.CurrentThread.GetApartmentState()}");

        foreach ((string name, Action<TextWriter> body) in tests)
        {
            output.WriteLine($"[RUN ] {name}");

            try
            {
                body(output);
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
                ? $"[PASS] All {tests.Length} STA geometry tests passed."
                : $"[FAIL] {failed} of {tests.Length} STA geometry tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void TestQaReportFormLifecycle(TextWriter output)
    {
        string syntheticRoot = Path.Combine(
            Path.GetTempPath(),
            "DocumentationLoggingDashboard.GeometryTests",
            $"no-save-{Guid.NewGuid():N}");
        QaStoragePaths paths = new(syntheticRoot);
        QaMetadataService metadataService = new(paths, new QaFolderNameSanitizer());

        using QaReportForm form = new(
            metadataService,
            Array.Empty<QaPmsMetadata>(),
            Array.Empty<QaHotelMetadata>(),
            paths)
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual
        };
        Rectangle virtualScreen = SystemInformation.VirtualScreen;
        form.Location = new Point(
            Math.Max(short.MinValue + 100, virtualScreen.Left - form.Width - 200),
            Math.Max(short.MinValue + 100, virtualScreen.Top - form.Height - 200));
        form.Show();
        PumpMessages();

        TabControl tabs = Descendants(form)
            .OfType<TabControl>()
            .Single(control => control.Name == "reportTabControl");
        TabPage statisticsPage = tabs.TabPages
            .Cast<TabPage>()
            .Single(page => page.Text == "Statistics && Readiness");
        QaStatisticsControl statistics = Descendants(statisticsPage)
            .OfType<QaStatisticsControl>()
            .Single();

        tabs.SelectedTab = statisticsPage;
        LayoutQaReportForm(form, statistics);
        AssertCompleteGeometry(statistics, "qa-form-initial-tab-display");

        form.Size = new Size(880, 600);
        LayoutQaReportForm(form, statistics);
        AssertCompleteGeometry(statistics, "qa-form-minimum");
        Size scaledMinimum = form.Size;

        form.Size = new Size(1600, 1000);
        LayoutQaReportForm(form, statistics);
        AssertCompleteGeometry(statistics, "qa-form-large");

        tabs.SelectedIndex = 0;
        PumpMessages();
        tabs.SelectedTab = statisticsPage;
        LayoutQaReportForm(form, statistics);
        AssertCompleteGeometry(statistics, "qa-form-tab-restored");

        Check(!Directory.Exists(syntheticRoot), () =>
            $"The no-save QA form test unexpectedly created its synthetic root '{syntheticRoot}'.");
        output.WriteLine(
            $"  [QA FORM] DeviceDpi={statistics.DeviceDpi}, " +
            $"scaledMinimum={Format(scaledMinimum)}, large={Format(form.Size)}, " +
            "tab hide/show passed; synthetic root was not created.");
    }

    private static void LayoutQaReportForm(
        QaReportForm form,
        QaStatisticsControl statistics)
    {
        for (int pass = 0; pass < 3; pass++)
        {
            form.PerformLayout();
            statistics.PerformLayout();
            GetOuterFlow(statistics).PerformLayout();
            PumpMessages();
        }
    }

    public static int RunVisibleSmoke(string screenshotDirectory, TextWriter output)
    {
        string destination = PrepareScreenshotDirectory(screenshotDirectory);
        output.WriteLine($"[SMOKE] Screenshot destination: {destination}");
        output.WriteLine("[SMOKE] Synthetic in-memory report only; no save/readiness/PDF command is invoked.");

        try
        {
            using GeometryFixture fixture = GeometryFixture.Create(
                new Size(1100, 780),
                1F,
                maximumApplicability: false,
                visibleOnScreen: true);

            fixture.Host.Text =
                "Synthetic QA Report - Statistics & Readiness (NO SAVE)";
            Capture(
                fixture,
                destination,
                "01-default-baseline-font-current-dpi-1100x780.png",
                output);

            fixture.ResizeHost(new Size(880, 600));
            Capture(
                fixture,
                destination,
                "02-minimum-baseline-font-current-dpi-880x600.png",
                output);

            SetMaximumApplicability(fixture.Report, enabled: true);
            fixture.RefreshAndLayout();
            fixture.ResizeHost(new Size(1100, 780));
            Capture(
                fixture,
                destination,
                "03-maximum-applicability-baseline-font-current-dpi.png",
                output);

            NumericUpDown totalRows = (NumericUpDown)FindByName(
                fixture.Statistics,
                "totalDataRowsNumericUpDown");
            totalRows.Value = 119;
            QaBlankStatisticRowControl blankEmail = FindBlankRow(
                fixture.Statistics,
                QaStatisticFieldIds.Email);
            NumericUpDown blankCount = FindByAccessibleName<NumericUpDown>(
                blankEmail,
                "Blank count for Email");
            NumericUpDown blankDenominator = FindByAccessibleName<NumericUpDown>(
                blankEmail,
                "Total applicable rows for Email");
            blankCount.Value = 12;
            blankDenominator.Value = 90;
            CaptureControl(
                fixture,
                blankEmail,
                destination,
                "04-blank-email-manual-override-current-dpi.png",
                output);

            QaBrokenDataStatisticRowControl brokenEmail = FindBrokenRow(
                fixture.Statistics,
                QaStatisticFieldIds.Email);
            NumericUpDown brokenDenominator = FindByAccessibleName<NumericUpDown>(
                brokenEmail,
                "Derived applicable nonblank values for Email");
            brokenDenominator.Value = 70;
            CaptureControl(
                fixture,
                brokenEmail,
                destination,
                "05-broken-email-manual-override-current-dpi.png",
                output);

            ((CheckBox)FindByName(
                blankEmail,
                "automaticTotalRowsCheckBox")).Checked = true;
            ((CheckBox)FindByName(
                brokenEmail,
                "automaticNonblankCountCheckBox")).Checked = true;
            CaptureControl(
                fixture,
                brokenEmail,
                destination,
                "06-broken-email-restored-auto-current-dpi.png",
                output);

            totalRows.Focus();
            GetOuterFlow(fixture.Statistics).AutoScrollPosition = Point.Empty;
            fixture.LayoutAndPump();
            fixture.SetFontScale(1.25F);
            Capture(
                fixture,
                destination,
                "07-maximum-applicability-font-pressure-125-current-dpi.png",
                output);

            fixture.SetFontScale(1.50F);
            Capture(
                fixture,
                destination,
                "08-maximum-applicability-font-pressure-150-current-dpi.png",
                output);

            fixture.Host.WindowState = FormWindowState.Maximized;
            fixture.LayoutAndPump();
            Capture(
                fixture,
                destination,
                "09-maximized-maximum-applicability-font-pressure-150-current-dpi.png",
                output);

            fixture.Host.WindowState = FormWindowState.Normal;
            fixture.LayoutAndPump();
            output.WriteLine("[SMOKE] Visible smoke completed and the synthetic form was closed without saving.");
            return 0;
        }
        catch (Exception exception)
        {
            output.WriteLine($"[SMOKE-FAIL] {exception}");
            return 1;
        }
    }

    private static void TestInitialGeometryMatrix(TextWriter output)
    {
        GeometryScenario[] scenarios =
        [
            new("default-baseline-font", new Size(1100, 780), 1F),
            new("minimum-baseline-font", new Size(880, 600), 1F),
            new("large-baseline-font", new Size(1600, 1000), 1F),
            new("minimum-font-pressure-125", new Size(880, 600), 1.25F),
            new("minimum-font-pressure-150", new Size(880, 600), 1.50F)
        ];

        foreach (GeometryScenario scenario in scenarios)
        {
            using GeometryFixture fixture = GeometryFixture.Create(
                scenario.WindowSize,
                scenario.FontScale,
                maximumApplicability: true);
            AssertCompleteGeometry(fixture, scenario.Name);
            WriteGeometrySnapshot(fixture, scenario.Name, output);
        }
    }

    private static void TestResizeAndFontLifecycle(TextWriter output)
    {
        using GeometryFixture fixture = GeometryFixture.Create(
            new Size(1100, 780),
            1F,
            maximumApplicability: true);

        AssertCompleteGeometry(fixture, "lifecycle-initial");

        fixture.ResizeHost(new Size(880, 600));
        AssertCompleteGeometry(fixture, "lifecycle-minimum");

        fixture.ResizeHost(new Size(1600, 1000));
        AssertCompleteGeometry(fixture, "lifecycle-maximize-sized");

        fixture.SetFontScale(1.25F);
        AssertCompleteGeometry(fixture, "lifecycle-font-pressure-125");

        fixture.SetFontScale(1.50F);
        AssertCompleteGeometry(fixture, "lifecycle-font-pressure-150");

        fixture.ResizeHost(new Size(880, 600));
        AssertCompleteGeometry(fixture, "lifecycle-font-pressure-150-minimum");
        WriteGeometrySnapshot(fixture, "lifecycle-final", output);
    }

    private static void TestAutoManualPropagation(TextWriter output)
    {
        using GeometryFixture fixture = GeometryFixture.Create(
            new Size(1100, 780),
            1F,
            maximumApplicability: true);
        QaStatisticsControl statistics = fixture.Statistics;
        QaReport report = fixture.Report;
        NumericUpDown totalRows = (NumericUpDown)FindByName(
            statistics,
            "totalDataRowsNumericUpDown");
        QaBlankStatisticRowControl blankEmail = FindBlankRow(
            statistics,
            QaStatisticFieldIds.Email);
        QaBrokenDataStatisticRowControl brokenEmail = FindBrokenRow(
            statistics,
            QaStatisticFieldIds.Email);
        NumericUpDown blankCount = FindByAccessibleName<NumericUpDown>(
            blankEmail,
            "Blank count for Email");
        NumericUpDown blankDenominator = FindByAccessibleName<NumericUpDown>(
            blankEmail,
            "Total applicable rows for Email");
        CheckBox blankAuto = (CheckBox)FindByName(
            blankEmail,
            "automaticTotalRowsCheckBox");
        NumericUpDown brokenDenominator = FindByAccessibleName<NumericUpDown>(
            brokenEmail,
            "Derived applicable nonblank values for Email");
        CheckBox brokenAuto = (CheckBox)FindByName(
            brokenEmail,
            "automaticNonblankCountCheckBox");

        Check(brokenDenominator.Enabled
            && brokenDenominator.TabStop
            && brokenDenominator.CanSelect, () =>
            "The Broken denominator is not an editable keyboard-selectable NumericUpDown.");
        Check(blankAuto.Visible && blankAuto.Enabled && blankAuto.CanSelect, () =>
            "The Blank Auto control is not visible and selectable.");
        Check(brokenAuto.Visible && brokenAuto.Enabled && brokenAuto.CanSelect, () =>
            "The Broken Auto control is not visible and selectable.");

        int statisticsChangedEvents = 0;
        int layoutEvents = 0;
        statistics.StatisticsChanged += (_, _) => statisticsChangedEvents++;
        statistics.Layout += (_, _) => layoutEvents++;

        void ApplyOneChange(Action action, string context)
        {
            int before = statisticsChangedEvents;
            action();
            PumpMessages();
            Check(statisticsChangedEvents == before + 1, () =>
                $"[{context}] Expected exactly one StatisticsChanged event; " +
                $"before={before}, after={statisticsChangedEvents}.");
        }

        ApplyOneChange(() => totalRows.Value = 100, "total-100");
        Check(report.Statistics.BlankValues
            .Where(row => row.UseAutomaticTotalApplicableRows)
            .All(row => row.TotalApplicableRows == 100), () =>
            "Not every applicable Auto Blank denominator followed Total Data Rows=100.");
        AssertAutomaticBrokenDenominators(report, "total-100");

        ApplyOneChange(() => blankCount.Value = 12, "email-blank-12");
        AssertEmailValues(
            report,
            expectedBlankDenominator: 100,
            expectedBrokenDenominator: 88,
            expectBlankAuto: true,
            expectBrokenAuto: true,
            context: "email-blank-12");
        Check(brokenDenominator.Value == 88, () =>
            $"The displayed Email Broken denominator did not update to 88; " +
            $"actual={brokenDenominator.Value}.");

        ApplyOneChange(() => totalRows.Value = 120, "total-120");
        AssertEmailValues(report, 120, 108, true, true, "total-120");

        ApplyOneChange(() => blankDenominator.Value = 90, "blank-manual-90");
        AssertEmailValues(report, 90, 78, false, true, "blank-manual-90");
        Check(!blankAuto.Checked, () =>
            "Editing the Blank denominator did not visibly clear Auto mode.");

        ApplyOneChange(() => totalRows.Value = 130, "total-130-blank-manual");
        AssertEmailValues(report, 90, 78, false, true, "total-130-blank-manual");

        ApplyOneChange(() => brokenDenominator.Value = 70, "broken-manual-70");
        AssertEmailValues(report, 90, 70, false, false, "broken-manual-70");
        Check(!brokenAuto.Checked, () =>
            "Editing the Broken denominator did not visibly clear Auto mode.");

        ApplyOneChange(() => totalRows.Value = 140, "total-140-both-manual");
        ApplyOneChange(() => blankCount.Value = 15, "blank-count-15-broken-manual");
        AssertEmailValues(report, 90, 70, false, false, "both-manual-persist");

        ApplyOneChange(() => blankAuto.Checked = true, "blank-reset-auto");
        AssertEmailValues(report, 140, 70, true, false, "blank-reset-auto");
        Check(blankDenominator.Value == 140, () =>
            $"Blank reset did not immediately display 140; actual={blankDenominator.Value}.");

        ApplyOneChange(() => brokenAuto.Checked = true, "broken-reset-auto");
        AssertEmailValues(report, 140, 125, true, true, "broken-reset-auto");
        Check(brokenDenominator.Value == 125, () =>
            $"Broken reset did not immediately display 125; actual={brokenDenominator.Value}.");

        int beforeRefresh = statisticsChangedEvents;
        statistics.RefreshFromReport();
        fixture.LayoutAndPump();
        Check(statisticsChangedEvents == beforeRefresh, () =>
            "Programmatic refresh raised a recursive StatisticsChanged event.");
        Check(layoutEvents < 128, () =>
            $"The propagation scenario raised an excessive number of layout events: {layoutEvents}.");
        int settledLayoutEvents = layoutEvents;
        PumpMessages();
        PumpMessages();
        Check(layoutEvents == settledLayoutEvents, () =>
            $"Layout events continued after the bounded pump: " +
            $"settled={settledLayoutEvents}, final={layoutEvents}.");

        AssertCompleteGeometry(fixture, "auto-manual-propagation-final");
        output.WriteLine(
            $"  [AUTO/MANUAL] StatisticsChanged={statisticsChangedEvents}; " +
            $"layoutEvents={layoutEvents}; final Email Blank=15/140, Broken=0/125.");
    }

    private static void TestDynamicApplicability(TextWriter output)
    {
        using GeometryFixture fixture = GeometryFixture.Create(
            new Size(880, 600),
            1.50F,
            maximumApplicability: true);

        AssertApplicability(fixture, expectSeparateNames: true, expectStayValue: true);
        AssertCompleteGeometry(fixture, "dynamic-maximum");
        WriteGeometrySnapshot(fixture, "dynamic-maximum", output);

        QaBlankStatisticRowControl oldBlankStay = FindBlankRow(
            fixture.Statistics,
            QaStatisticFieldIds.StayValue);
        QaBrokenDataStatisticRowControl oldBrokenStay = FindBrokenRow(
            fixture.Statistics,
            QaStatisticFieldIds.StayValue);
        FindByAccessibleName<NumericUpDown>(
            oldBlankStay,
            "Total applicable rows for Stay Value").Value = 77;
        FindByAccessibleName<NumericUpDown>(
            oldBrokenStay,
            "Derived applicable nonblank values for Stay Value").Value = 55;
        Check(!((CheckBox)FindByName(
            oldBlankStay,
            "automaticTotalRowsCheckBox")).Checked, () =>
            "The Stay Value Blank denominator did not enter Manual mode before hiding.");
        Check(!((CheckBox)FindByName(
            oldBrokenStay,
            "automaticNonblankCountCheckBox")).Checked, () =>
            "The Stay Value Broken denominator did not enter Manual mode before hiding.");

        fixture.Report.FileCharacteristics.NameColumnMode = QaNameColumnMode.FullName;
        fixture.Report.FileCharacteristics.MonetaryColumnScenario =
            QaMonetaryColumnScenario.OneMonetaryColumn;
        fixture.Report.Statistics.Database.ImportedRecordCount =
            fixture.Report.Statistics.FileInformation.TotalDataRows;
        fixture.Report.Statistics.Database.RejectedRecordCount = 0;
        fixture.Report.Statistics.UnusualAverageRateValues.HasUnusualValues = false;
        fixture.Report.Statistics.HighStayValues.StayValuesAboveTenThousandCount = 0;
        fixture.RefreshAndLayout();

        AssertApplicability(fixture, expectSeparateNames: false, expectStayValue: false);
        Check(oldBlankStay.IsDisposed && oldBrokenStay.IsDisposed, () =>
            "Non-applicable Stay Value row controls were not disposed.");
        Check(!fixture.Report.Statistics.BlankValues.Any(
            row => row.FieldId == QaStatisticFieldIds.StayValue)
            && !fixture.Report.Statistics.BrokenData.Any(
                row => row.FieldId == QaStatisticFieldIds.StayValue), () =>
            "Non-applicable Stay Value model rows were retained.");
        AssertCompleteGeometry(fixture, "dynamic-minimum");
        WriteGeometrySnapshot(fixture, "dynamic-minimum", output);

        fixture.Report.Statistics.FileInformation.TotalDataRows = 333;
        fixture.RefreshAndLayout();

        SetMaximumApplicability(fixture.Report, enabled: true);
        fixture.RefreshAndLayout();

        AssertApplicability(fixture, expectSeparateNames: true, expectStayValue: true);
        QaBlankStatisticRowControl newBlankStay = FindBlankRow(
            fixture.Statistics,
            QaStatisticFieldIds.StayValue);
        QaBrokenDataStatisticRowControl newBrokenStay = FindBrokenRow(
            fixture.Statistics,
            QaStatisticFieldIds.StayValue);
        QaBlankValueStatistic blankStayStatistic = fixture.Report.Statistics.BlankValues
            .Single(row => row.FieldId == QaStatisticFieldIds.StayValue);
        QaBrokenDataStatistic brokenStayStatistic = fixture.Report.Statistics.BrokenData
            .Single(row => row.FieldId == QaStatisticFieldIds.StayValue);
        Check(!ReferenceEquals(oldBlankStay, newBlankStay)
            && !ReferenceEquals(oldBrokenStay, newBrokenStay), () =>
            "Reapplying Stay Value reused stale hidden row controls.");
        Check(blankStayStatistic.UseAutomaticTotalApplicableRows
            && blankStayStatistic.TotalApplicableRows == 333, () =>
            $"Fresh Stay Value Blank row was not Auto/333: " +
            $"auto={blankStayStatistic.UseAutomaticTotalApplicableRows}, " +
            $"denominator={blankStayStatistic.TotalApplicableRows}.");
        Check(brokenStayStatistic.UseAutomaticTotalApplicableNonblankValues
            && brokenStayStatistic.TotalApplicableNonblankValues == 333, () =>
            $"Fresh Stay Value Broken row was not Auto/333: " +
            $"auto={brokenStayStatistic.UseAutomaticTotalApplicableNonblankValues}, " +
            $"denominator={brokenStayStatistic.TotalApplicableNonblankValues}.");
        Check(((CheckBox)FindByName(
                newBlankStay,
                "automaticTotalRowsCheckBox")).Checked
            && ((CheckBox)FindByName(
                newBrokenStay,
                "automaticNonblankCountCheckBox")).Checked, () =>
            "Fresh Stay Value Auto controls were not visibly selected.");
        AssertCompleteGeometry(fixture, "dynamic-restored");
        WriteGeometrySnapshot(fixture, "dynamic-restored", output);
        output.WriteLine(
            "  [DYNAMIC] stale Stay Value manual 77/55 discarded; fresh Auto rows restored at 333/333.");
    }

    private static void TestHitTestingAndTabTraversal(TextWriter output)
    {
        using GeometryFixture fixture = GeometryFixture.Create(
            new Size(880, 600),
            1.50F,
            maximumApplicability: true);

        GroupBox fileGroup = FindGroup(fixture.Statistics, "File Information");
        Control[] inputs = FileInformationInputNames
            .Select(name => FindByName(fixture.Statistics, name))
            .ToArray();

        AssertInteractiveAndNativeHit(fileGroup, inputs, "File Information");
        AssertTabTraversal(
            fixture.Statistics,
            inputs,
            "File Information");

        FlowLayoutPanel outer = GetOuterFlow(fixture.Statistics);
        GroupBox blankGroup = FindGroup(fixture.Statistics, "Blank Value Statistics");
        QaBlankStatisticRowControl blankEmail = FindBlankRow(
            fixture.Statistics,
            QaStatisticFieldIds.Email);
        Control[] blankInputs =
        [
            FindByAccessibleName<NumericUpDown>(blankEmail, "Blank count for Email"),
            FindByAccessibleName<NumericUpDown>(blankEmail, "Total applicable rows for Email"),
            FindByName(blankEmail, "automaticTotalRowsCheckBox")
        ];
        outer.ScrollControlIntoView(blankEmail);
        fixture.LayoutAndPump();
        AssertInteractiveAndNativeHit(blankGroup, blankInputs, "Blank Email");
        AssertTabTraversal(fixture.Statistics, blankInputs, "Blank Email");
        AssertNoHorizontalScrollbar(outer, "blank-hit-tab");

        GroupBox brokenGroup = FindGroup(fixture.Statistics, "Broken Data Statistics");
        QaBrokenDataStatisticRowControl brokenEmail = FindBrokenRow(
            fixture.Statistics,
            QaStatisticFieldIds.Email);
        Control[] brokenInputs =
        [
            FindByAccessibleName<NumericUpDown>(
                brokenEmail,
                "Broken value count for Email"),
            FindByAccessibleName<NumericUpDown>(
                brokenEmail,
                "Derived applicable nonblank values for Email"),
            FindByName(brokenEmail, "automaticNonblankCountCheckBox"),
            FindByAccessibleName<TextBox>(
                brokenEmail,
                "Broken-data explanation for Email")
        ];
        outer.ScrollControlIntoView(brokenEmail);
        fixture.LayoutAndPump();
        AssertInteractiveAndNativeHit(brokenGroup, brokenInputs, "Broken Email");
        AssertTabTraversal(fixture.Statistics, brokenInputs, "Broken Email");
        AssertNoHorizontalScrollbar(outer, "broken-hit-tab");

        output.WriteLine(
            "  [TAB] File Information: " +
            string.Join(" -> ", inputs.Select(ControlIdentity)) +
            "; reverse passed.");
        output.WriteLine(
            "  [TAB] Blank Email: " +
            string.Join(" -> ", blankInputs.Select(ControlIdentity)) +
            "; reverse passed.");
        output.WriteLine(
            "  [TAB] Broken Email: " +
            string.Join(" -> ", brokenInputs.Select(ControlIdentity)) +
            "; reverse passed.");
    }

    private static void AssertInteractiveAndNativeHit(
        GroupBox group,
        IEnumerable<Control> inputs,
        string context)
    {
        foreach (Control input in inputs)
        {
            Check(input.Enabled && input.TabStop && input.CanSelect, () =>
                $"[{context}] Input is not keyboard-selectable: {Describe(input)}; " +
                $"Enabled={input.Enabled}, TabStop={input.TabStop}, CanSelect={input.CanSelect}.");
            AssertNativeHit(group, input);
        }
    }

    private static void AssertTabTraversal(
        Control traversalRoot,
        Control[] inputs,
        string context)
    {
        Check(inputs.Length > 0, () => $"[{context}] No controls were supplied for Tab traversal.");
        FocusAndAssert(inputs[0]);
        for (int index = 1; index < inputs.Length; index++)
        {
            bool moved = traversalRoot.SelectNextControl(
                inputs[index - 1],
                forward: true,
                tabStopOnly: true,
                nested: true,
                wrap: false);
            PumpMessages();
            Check(moved, () =>
                $"[{context}] Forward Tab traversal stopped before {Describe(inputs[index])}.");
            Check(FocusIsWithin(inputs[index]), () =>
                $"[{context}] Forward Tab from {Describe(inputs[index - 1])} did not focus " +
                $"{Describe(inputs[index])}; focused HWND=0x{GetFocus().ToInt64():X}.");
        }

        FocusAndAssert(inputs[^1]);
        for (int index = inputs.Length - 2; index >= 0; index--)
        {
            bool moved = traversalRoot.SelectNextControl(
                inputs[index + 1],
                forward: false,
                tabStopOnly: true,
                nested: true,
                wrap: false);
            PumpMessages();
            Check(moved, () =>
                $"[{context}] Reverse Tab traversal stopped before {Describe(inputs[index])}.");
            Check(FocusIsWithin(inputs[index]), () =>
                $"[{context}] Reverse Tab from {Describe(inputs[index + 1])} did not focus " +
                $"{Describe(inputs[index])}; focused HWND=0x{GetFocus().ToInt64():X}.");
        }
    }

    private static void TestScrolling(TextWriter output)
    {
        using GeometryFixture fixture = GeometryFixture.Create(
            new Size(880, 600),
            1.50F,
            maximumApplicability: true);
        FlowLayoutPanel outer = GetOuterFlow(fixture.Statistics);
        GroupBox[] groups = outer.Controls
            .OfType<GroupBox>()
            .Where(group => group.Visible)
            .ToArray();

        Check(groups.Length >= 2, () => "Expected at least two visible statistics groups.");
        outer.AutoScrollPosition = Point.Empty;
        fixture.LayoutAndPump();
        int topValue = outer.VerticalScroll.Value;

        GroupBox last = groups[^1];
        outer.ScrollControlIntoView(last);
        PumpMessages();
        int bottomValue = outer.VerticalScroll.Value;
        Check(bottomValue > topValue, () =>
            $"Scrolling to final group did not advance the outer scrollbar: " +
            $"top={topValue}, bottom={bottomValue}, max={outer.VerticalScroll.Maximum}.");
        Rectangle outerOnScreen = outer.RectangleToScreen(outer.ClientRectangle);
        Rectangle lastOnScreen = last.RectangleToScreen(last.ClientRectangle);
        Check(outerOnScreen.IntersectsWith(lastOnScreen), () =>
            $"Final group is not visible after ScrollControlIntoView: " +
            $"viewportScreen={Format(outerOnScreen)}, groupScreen={Format(lastOnScreen)}, " +
            $"logicalGroup={Format(last.Bounds)}.");

        GroupBox first = groups[0];
        outer.ScrollControlIntoView(first);
        PumpMessages();
        int returnValue = outer.VerticalScroll.Value;
        Check(returnValue < bottomValue, () =>
            $"Returning to the first group did not move upward: " +
            $"bottom={bottomValue}, return={returnValue}.");
        Rectangle firstOnScreen = first.RectangleToScreen(first.ClientRectangle);
        Check(outerOnScreen.IntersectsWith(firstOnScreen), () =>
            $"First group is not visible after returning: " +
            $"viewportScreen={Format(outerOnScreen)}, groupScreen={Format(firstOnScreen)}, " +
            $"logicalGroup={Format(first.Bounds)}.");
        AssertNoHorizontalScrollbar(outer, "scroll-return");

        output.WriteLine(
            $"  [SCROLL] top={topValue}, bottom={bottomValue}, return={returnValue}, " +
            $"maximum={outer.VerticalScroll.Maximum}; horizontal=false.");
    }

    private static void AssertCompleteGeometry(GeometryFixture fixture, string context)
    {
        fixture.LayoutAndPump();
        AssertCompleteGeometry(fixture.Statistics, context);
    }

    private static void AssertCompleteGeometry(
        QaStatisticsControl statistics,
        string context)
    {
        AssertFileInformationContained(statistics, context);
        AssertAllVisibleGroupContentContained(statistics, context);
        AssertStatisticRowsContainedAndSeparated(statistics, context);
        AssertConsecutiveGroupsDoNotIntersect(statistics, context);
        AssertNoHorizontalScrollbar(GetOuterFlow(statistics), context);
        AssertScrollbarOwnership(statistics, context);
    }

    private static void AssertFileInformationContained(
        QaStatisticsControl statistics,
        string context)
    {
        GroupBox group = FindGroup(statistics, "File Information");
        Rectangle display = group.DisplayRectangle;

        foreach (string labelText in FileInformationLabelTexts)
        {
            Label label = Descendants(group)
                .OfType<Label>()
                .SingleOrDefault(candidate => candidate.Text == labelText)
                ?? throw new GeometryAssertionException(
                    $"[{context}] File Information label '{labelText}' was not found.");
            AssertContained(group, label, context);
        }

        foreach (string name in FileInformationInputNames)
        {
            Control input = FindByName(group, name);
            AssertContained(group, input, context);
            Check(input.Width > 0 && input.Height > 0, () =>
                $"[{context}] File Information input has no usable area: {Describe(input)}.");
        }

        Check(display.Width > 0 && display.Height > 0, () =>
            $"[{context}] File Information display area is empty: {Format(display)}.");
    }

    private static void AssertAllVisibleGroupContentContained(
        QaStatisticsControl statistics,
        string context)
    {
        foreach (GroupBox group in Descendants(statistics)
                     .OfType<GroupBox>()
                     .Where(group => group.Visible))
        {
            foreach (Control content in Descendants(group).Where(control => control.Visible))
            {
                AssertContained(group, content, context);
            }
        }
    }

    private static void AssertConsecutiveGroupsDoNotIntersect(
        QaStatisticsControl statistics,
        string context)
    {
        IEnumerable<FlowLayoutPanel> verticalFlows = DescendantsAndSelf(statistics)
            .OfType<FlowLayoutPanel>()
            .Where(panel => panel.FlowDirection == FlowDirection.TopDown);

        foreach (FlowLayoutPanel flow in verticalFlows)
        {
            GroupBox[] groups = flow.Controls
                .OfType<GroupBox>()
                .Where(group => group.Visible)
                .ToArray();

            for (int index = 0; index + 1 < groups.Length; index++)
            {
                GroupBox current = groups[index];
                GroupBox next = groups[index + 1];
                Check(current.Bottom <= next.Top, () =>
                    $"[{context}] Consecutive groups intersect in {Describe(flow)}: " +
                    $"'{current.Text}'={Format(current.Bounds)}, " +
                    $"'{next.Text}'={Format(next.Bounds)}.");
            }
        }
    }

    private static void AssertStatisticRowsContainedAndSeparated(
        QaStatisticsControl statistics,
        string context)
    {
        AssertStatisticRowsContainedAndSeparated<QaBlankStatisticRowControl>(
            FindGroup(statistics, "Blank Value Statistics"),
            context,
            "Blank");
        AssertStatisticRowsContainedAndSeparated<QaBrokenDataStatisticRowControl>(
            FindGroup(statistics, "Broken Data Statistics"),
            context,
            "Broken");
    }

    private static void AssertStatisticRowsContainedAndSeparated<TRow>(
        GroupBox group,
        string context,
        string rowKind)
        where TRow : Control
    {
        TRow[] rows = Descendants(group)
            .OfType<TRow>()
            .Where(row => row.Visible)
            .ToArray();
        Check(rows.Length > 0, () =>
            $"[{context}] No visible {rowKind} statistic rows were found.");

        foreach (TRow row in rows)
        {
            AssertContained(group, row, context);
            Check(row.Width > 0 && row.Height > 0, () =>
                $"[{context}] {rowKind} row has no usable area: {Describe(row)}.");

            foreach (Control child in Descendants(row).Where(child => child.Visible))
            {
                Rectangle childRectangle = RectangleRelativeTo(child, row);
                Check(row.ClientRectangle.Contains(childRectangle), () =>
                    $"[{context}] Visible {rowKind} row content escapes its row: " +
                    $"row={Describe(row)}, client={Format(row.ClientRectangle)}, " +
                    $"child={Describe(child)}, relativeBounds={Format(childRectangle)}.");
            }
        }

        foreach (IGrouping<Control?, TRow> siblingRows in rows.GroupBy(row => row.Parent))
        {
            TRow[] orderedRows = siblingRows
                .OrderBy(row => row.Top)
                .ToArray();
            for (int index = 0; index + 1 < orderedRows.Length; index++)
            {
                TRow current = orderedRows[index];
                TRow next = orderedRows[index + 1];
                Check(current.Bottom <= next.Top, () =>
                    $"[{context}] Consecutive {rowKind} rows intersect: " +
                    $"{Describe(current)}={Format(current.Bounds)}, " +
                    $"{Describe(next)}={Format(next.Bounds)}.");
            }
        }
    }

    private static void AssertNoHorizontalScrollbar(
        FlowLayoutPanel outer,
        string context)
    {
        bool nativeHorizontal = (GetWindowLong(outer.Handle, GwlStyle) & WsHScroll) != 0;
        Check(!outer.HorizontalScroll.Visible && !nativeHorizontal, () =>
            $"[{context}] Unexpected outer horizontal scrollbar: " +
            $"managedVisible={outer.HorizontalScroll.Visible}, nativeStyle={nativeHorizontal}, " +
            $"client={Format(outer.ClientRectangle)}, display={Format(outer.DisplayRectangle)}.");
    }

    private static void AssertScrollbarOwnership(
        QaStatisticsControl statistics,
        string context)
    {
        FlowLayoutPanel outer = GetOuterFlow(statistics);
        ScrollableControl[] autoScrollingContainers = DescendantsAndSelf(statistics)
            .OfType<ScrollableControl>()
            .Where(control => control.AutoScroll)
            .ToArray();
        Check(autoScrollingContainers.Length == 1 && autoScrollingContainers[0] == outer, () =>
            $"[{context}] Expected exactly one AutoScroll container owned by the outer flow; found " +
            string.Join(", ", autoScrollingContainers.Select(Describe)) + ".");

        TextBox[] multilineTextBoxes = Descendants(statistics)
            .OfType<TextBox>()
            .Where(textBox => textBox.Multiline)
            .ToArray();
        Check(multilineTextBoxes.Length > 0, () =>
            $"[{context}] Expected explanation/readiness text boxes.");
        foreach (TextBox textBox in multilineTextBoxes)
        {
            Check(textBox.ScrollBars == ScrollBars.Vertical, () =>
                $"[{context}] Inner explanation/readiness scrollbar changed for " +
                $"{Describe(textBox)}: {textBox.ScrollBars}.");
        }
    }

    private static void AssertApplicability(
        GeometryFixture fixture,
        bool expectSeparateNames,
        bool expectStayValue)
    {
        GroupBox nameGroup = FindGroup(fixture.Statistics, "Name Statistics (informational)");
        GroupBox stayGroup = FindGroup(fixture.Statistics, "Stay Value Statistics");
        Check(nameGroup.Visible == expectSeparateNames, () =>
            $"Name Statistics visibility mismatch: expected={expectSeparateNames}, " +
            $"actual={nameGroup.Visible}.");
        Check(stayGroup.Visible == expectStayValue, () =>
            $"Stay Value Statistics visibility mismatch: expected={expectStayValue}, " +
            $"actual={stayGroup.Visible}.");

        int blankRows = Descendants(fixture.Statistics)
            .OfType<QaBlankStatisticRowControl>()
            .Count(row => row.Visible);
        int brokenRows = Descendants(fixture.Statistics)
            .OfType<QaBrokenDataStatisticRowControl>()
            .Count(row => row.Visible);
        Check(blankRows == fixture.Report.Statistics.BlankValues.Count, () =>
            $"Blank row reconciliation mismatch: UI={blankRows}, " +
            $"model={fixture.Report.Statistics.BlankValues.Count}.");
        Check(brokenRows == fixture.Report.Statistics.BrokenData.Count, () =>
            $"Broken row reconciliation mismatch: UI={brokenRows}, " +
            $"model={fixture.Report.Statistics.BrokenData.Count}.");
    }

    private static void AssertAutomaticBrokenDenominators(
        QaReport report,
        string context)
    {
        foreach (QaBrokenDataStatistic broken in report.Statistics.BrokenData
                     .Where(row => row.UseAutomaticTotalApplicableNonblankValues))
        {
            QaBlankValueStatistic blank = report.Statistics.BlankValues.Single(
                row => row.FieldId == broken.FieldId);
            int expected = blank.TotalApplicableRows - blank.BlankCount;
            Check(broken.TotalApplicableNonblankValues == expected, () =>
                $"[{context}] Auto Broken denominator mismatch for {broken.FieldId}: " +
                $"expected={expected}, actual={broken.TotalApplicableNonblankValues}.");
        }
    }

    private static void AssertEmailValues(
        QaReport report,
        int expectedBlankDenominator,
        int expectedBrokenDenominator,
        bool expectBlankAuto,
        bool expectBrokenAuto,
        string context)
    {
        QaBlankValueStatistic blank = report.Statistics.BlankValues.Single(
            row => row.FieldId == QaStatisticFieldIds.Email);
        QaBrokenDataStatistic broken = report.Statistics.BrokenData.Single(
            row => row.FieldId == QaStatisticFieldIds.Email);
        Check(blank.TotalApplicableRows == expectedBlankDenominator
            && blank.UseAutomaticTotalApplicableRows == expectBlankAuto, () =>
            $"[{context}] Email Blank state mismatch: expected denominator/Auto=" +
            $"{expectedBlankDenominator}/{expectBlankAuto}, actual=" +
            $"{blank.TotalApplicableRows}/{blank.UseAutomaticTotalApplicableRows}.");
        Check(broken.TotalApplicableNonblankValues == expectedBrokenDenominator
            && broken.UseAutomaticTotalApplicableNonblankValues == expectBrokenAuto, () =>
            $"[{context}] Email Broken state mismatch: expected denominator/Auto=" +
            $"{expectedBrokenDenominator}/{expectBrokenAuto}, actual=" +
            $"{broken.TotalApplicableNonblankValues}/" +
            $"{broken.UseAutomaticTotalApplicableNonblankValues}.");
    }

    private static void AssertContained(GroupBox group, Control content, string context)
    {
        Rectangle contentRectangle = RectangleRelativeTo(content, group);
        Rectangle display = group.DisplayRectangle;
        Check(display.Contains(contentRectangle), () =>
            $"[{context}] Visible content escapes GroupBox display area: " +
            $"group='{group.Text}', display={Format(display)}, " +
            $"content={Describe(content)}, relativeBounds={Format(contentRectangle)}, " +
            $"localBounds={Format(content.Bounds)}, preferred={Format(content.GetPreferredSize(new Size(content.Width, 0)))}.");
    }

    private static void AssertNativeHit(GroupBox group, Control target)
    {
        _ = group.Handle;
        _ = target.Handle;
        Point centerOnScreen = target.PointToScreen(
            new Point(Math.Max(0, target.Width / 2), Math.Max(0, target.Height / 2)));
        Point centerInGroup = group.PointToClient(centerOnScreen);
        Check(group.DisplayRectangle.Contains(centerInGroup), () =>
            $"Native hit point for {Describe(target)} is clipped by '{group.Text}': " +
            $"point={Format(centerInGroup)}, display={Format(group.DisplayRectangle)}.");

        IntPtr deepest = DeepestNativeChildAtPoint(group.Handle, centerOnScreen);
        bool belongsToTarget = deepest == target.Handle || IsChild(target.Handle, deepest);
        Check(belongsToTarget, () =>
            $"Native child hit test did not resolve {Describe(target)}: " +
            $"target=0x{target.Handle.ToInt64():X}, hit=0x{deepest.ToInt64():X}, " +
            $"screenPoint={Format(centerOnScreen)}.");

        IntPtr hitCode = SendMessage(
            target.Handle,
            WmNcHitTest,
            IntPtr.Zero,
            MakePointLParam(centerOnScreen));
        Check(hitCode.ToInt64() is not HtNowhere and not HtTransparent, () =>
            $"WM_NCHITTEST rejected {Describe(target)} at {Format(centerOnScreen)}: " +
            $"result={hitCode.ToInt64()}.");
    }

    private static IntPtr DeepestNativeChildAtPoint(IntPtr root, Point screenPoint)
    {
        IntPtr current = root;
        HashSet<IntPtr> visited = [];

        for (int depth = 0; depth < 64 && visited.Add(current); depth++)
        {
            NativePoint clientPoint = new(screenPoint.X, screenPoint.Y);
            Check(ScreenToClient(current, ref clientPoint), () =>
                $"ScreenToClient failed for HWND 0x{current.ToInt64():X}.");
            IntPtr child = ChildWindowFromPointEx(current, clientPoint, 0);
            if (child == IntPtr.Zero || child == current)
            {
                break;
            }

            current = child;
        }

        return current;
    }

    private static void FocusAndAssert(Control target)
    {
        target.Select();
        target.Focus();
        PumpMessages();
        Check(FocusIsWithin(target), () =>
            $"Could not focus {Describe(target)}; focused HWND=0x{GetFocus().ToInt64():X}.");
    }

    private static bool FocusIsWithin(Control target)
    {
        IntPtr focused = GetFocus();
        return focused != IntPtr.Zero
            && (focused == target.Handle || IsChild(target.Handle, focused));
    }

    private static void WriteGeometrySnapshot(
        GeometryFixture fixture,
        string scenario,
        TextWriter output)
    {
        FlowLayoutPanel outer = GetOuterFlow(fixture.Statistics);
        GroupBox file = FindGroup(fixture.Statistics, "File Information");
        GroupBox blank = FindGroup(fixture.Statistics, "Blank Value Statistics");
        Control content = file.Controls.Cast<Control>().Single();
        output.WriteLine(
            $"  [GEOMETRY] {scenario}: window={Format(fixture.Host.Size)}, " +
            $"client={Format(fixture.Host.ClientSize)}, deviceDpi={fixture.Statistics.DeviceDpi}, " +
            $"font={fixture.Statistics.Font.SizeInPoints:0.##}pt, " +
            $"viewportClient={Format(outer.ClientRectangle)}, " +
            $"viewportDisplay={Format(outer.DisplayRectangle)}, hscroll={outer.HorizontalScroll.Visible}");
        output.WriteLine(
            $"  [GEOMETRY] {scenario}: fileGroup={Format(file.Bounds)}, " +
            $"fileDisplay={Format(file.DisplayRectangle)}, " +
            $"fileContent={Format(content.Bounds)}, blankGroup={Format(blank.Bounds)}, " +
            $"gap={blank.Top - file.Bottom}");
    }

    private static void Capture(
        GeometryFixture fixture,
        string destination,
        string fileName,
        TextWriter output,
        bool performLayout = true)
    {
        if (performLayout)
        {
            fixture.LayoutAndPump();
        }

        AssertCompleteGeometry(
            fixture.Statistics,
            $"visible-smoke-{fileName}");
        string path = Path.Combine(destination, fileName);

        using Bitmap bitmap = new(
            fixture.Host.ClientSize.Width,
            fixture.Host.ClientSize.Height);
        fixture.Host.DrawToBitmap(
            bitmap,
            new Rectangle(Point.Empty, fixture.Host.ClientSize));
        bitmap.Save(path, ImageFormat.Png);
        output.WriteLine($"[SMOKE] Captured {path}");
    }

    private static void CaptureControl(
        GeometryFixture fixture,
        Control target,
        string destination,
        string fileName,
        TextWriter output)
    {
        FlowLayoutPanel outer = GetOuterFlow(fixture.Statistics);
        const int targetTopMargin = 12;
        Rectangle visibleTarget = Rectangle.Empty;
        Control? focusTarget = Descendants(target)
            .FirstOrDefault(control => control.Visible && control.TabStop && control.CanSelect);
        focusTarget?.Focus();
        fixture.LayoutAndPump();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            visibleTarget = outer.RectangleToClient(
                target.RectangleToScreen(target.ClientRectangle));
            output.WriteLine(
                $"[SMOKE] Scroll target attempt {attempt + 1}: " +
                $"value={outer.VerticalScroll.Value}, target={Format(visibleTarget)}.");
            if (outer.ClientRectangle.Contains(visibleTarget)
                && visibleTarget.Top >= targetTopMargin
                && visibleTarget.Top <= targetTopMargin + 4)
            {
                break;
            }

            int desiredScrollY = Math.Max(
                0,
                outer.VerticalScroll.Value + visibleTarget.Top - targetTopMargin);
            outer.AutoScrollPosition = new Point(0, desiredScrollY);
            fixture.LayoutAndPump();
        }

        fixture.Host.Refresh();
        PumpMessages();
        visibleTarget = outer.RectangleToClient(
            target.RectangleToScreen(target.ClientRectangle));
        string targetName = string.IsNullOrWhiteSpace(target.AccessibleName)
            ? target.GetType().Name
            : target.AccessibleName;
        Check(outer.ClientRectangle.Contains(visibleTarget), () =>
            $"Screenshot target '{targetName}' was not fully visible after scrolling: " +
            $"target={Format(visibleTarget)}, viewport={Format(outer.ClientRectangle)}.");
        Capture(
            fixture,
            destination,
            fileName,
            output,
            performLayout: false);
    }

    private static string PrepareScreenshotDirectory(string screenshotDirectory)
    {
        if (!Path.IsPathFullyQualified(screenshotDirectory))
        {
            throw new ArgumentException(
                "Screenshot destination must be an absolute path.",
                nameof(screenshotDirectory));
        }

        string destination = Path.GetFullPath(screenshotDirectory);
        string? repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory)
            ?? FindRepositoryRoot(Environment.CurrentDirectory);
        if (repositoryRoot is not null && IsWithin(destination, repositoryRoot))
        {
            throw new ArgumentException(
                $"Screenshot destination must be outside the repository '{repositoryRoot}'.",
                nameof(screenshotDirectory));
        }

        Directory.CreateDirectory(destination);
        return destination;
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(startPath));
        if (!directory.Exists && directory.Parent is not null)
        {
            directory = directory.Parent;
        }

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bool IsWithin(string path, string parent)
    {
        string relative = Path.GetRelativePath(parent, path);
        return relative == "."
            || (!relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !Path.IsPathFullyQualified(relative));
    }

    private static void SetMaximumApplicability(QaReport report, bool enabled)
    {
        report.FileCharacteristics.NameColumnMode = enabled
            ? QaNameColumnMode.SeparateFirstAndLastName
            : QaNameColumnMode.FullName;
        report.FileCharacteristics.MonetaryColumnScenario = enabled
            ? QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns
            : QaMonetaryColumnScenario.OneMonetaryColumn;
        report.FileCharacteristics.HasCurrencyColumn = enabled;
        report.FileCharacteristics.HasMultipleConfirmationNumberCandidateColumns = enabled;
        report.FileCharacteristics.HasRejectedDatabaseRecords = enabled;

        report.Statistics.UnusualAverageRateValues.HasUnusualValues = enabled;
        report.Statistics.UnusualAverageRateValues.UnusualValueCount = enabled ? 4 : 0;
        report.Statistics.UnusualAverageRateValues.Explanation = enabled
            ? "Synthetic summary for layout coverage only."
            : null;
        report.Statistics.UnusualStayValues.HasUnusualValues = enabled;
        report.Statistics.UnusualStayValues.UnusualValueCount = enabled ? 3 : 0;
        report.Statistics.UnusualStayValues.Explanation = enabled
            ? "Synthetic summary for layout coverage only."
            : null;
        report.Statistics.HighStayValues.StayValuesAboveTenThousandCount = enabled ? 2 : 0;
        report.Statistics.HighStayValues.AreHighValuesExpected = false;
        report.Statistics.HighStayValues.Explanation = enabled
            ? "Synthetic summary for layout coverage only."
            : null;
        report.Statistics.Database.RejectedRecordCount = enabled ? 3 : 0;
        report.Statistics.Database.ImportedRecordCount = enabled ? 200 : 250;
    }

    private static QaReport CreateSyntheticReport(bool maximumApplicability)
    {
        QaReport report = new()
        {
            ReportId = "synthetic-geometry-no-save",
            FileId = "001234",
            CreatedBy = "Geometry Harness",
            OriginalFileName = "synthetic-layout-only.csv",
            Statistics = new QaStatistics
            {
                FileInformation = new QaFileInformationStatistics
                {
                    TotalDataRows = 250,
                    HeadersArePresent = true,
                    UsefulHeaders = QaUsefulHeadersResult.Partially,
                    DataStartRow = 2
                },
                Database = new QaDatabaseStatistics
                {
                    ImportedRecordCount = 200,
                    RejectedRecordCount = 3,
                    RecordsWithMissingRequiredDatabaseValues = 2
                },
                FileMonth = new QaFileMonthStatistics
                {
                    ValidArrivalDateCount = 240,
                    ArrivalDatesWithinFileMonth = 230,
                    ArrivalDatesOutsideFileMonth = 10
                }
            }
        };

        SetMaximumApplicability(report, maximumApplicability);
        return report;
    }

    private static FlowLayoutPanel GetOuterFlow(QaStatisticsControl statistics)
    {
        return statistics.Controls
            .OfType<FlowLayoutPanel>()
            .Single(panel => panel.AutoScroll);
    }

    private static GroupBox FindGroup(Control root, string text)
    {
        return Descendants(root)
            .OfType<GroupBox>()
            .SingleOrDefault(group => group.Text == text)
            ?? throw new GeometryAssertionException($"GroupBox '{text}' was not found.");
    }

    private static QaBlankStatisticRowControl FindBlankRow(
        Control root,
        string fieldId)
    {
        return Descendants(root)
            .OfType<QaBlankStatisticRowControl>()
            .SingleOrDefault(row => row.FieldId == fieldId)
            ?? throw new GeometryAssertionException(
                $"Blank statistic row '{fieldId}' was not found.");
    }

    private static QaBrokenDataStatisticRowControl FindBrokenRow(
        Control root,
        string fieldId)
    {
        return Descendants(root)
            .OfType<QaBrokenDataStatisticRowControl>()
            .SingleOrDefault(row => row.FieldId == fieldId)
            ?? throw new GeometryAssertionException(
                $"Broken statistic row '{fieldId}' was not found.");
    }

    private static Control FindByName(Control root, string name)
    {
        return DescendantsAndSelf(root)
            .SingleOrDefault(control => control.Name == name)
            ?? throw new GeometryAssertionException($"Control '{name}' was not found.");
    }

    private static TControl FindByAccessibleName<TControl>(
        Control root,
        string accessibleName)
        where TControl : Control
    {
        return DescendantsAndSelf(root)
            .OfType<TControl>()
            .SingleOrDefault(control => string.Equals(
                control.AccessibleName,
                accessibleName,
                StringComparison.Ordinal))
            ?? throw new GeometryAssertionException(
                $"{typeof(TControl).Name} with AccessibleName '{accessibleName}' was not found.");
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;

            foreach (Control descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<Control> DescendantsAndSelf(Control root)
    {
        yield return root;

        foreach (Control descendant in Descendants(root))
        {
            yield return descendant;
        }
    }

    private static Rectangle RectangleRelativeTo(Control control, Control ancestor)
    {
        Point location = control.Parent is null
            ? ancestor.PointToClient(control.PointToScreen(Point.Empty))
            : ancestor.PointToClient(
                control.Parent.PointToScreen(control.Bounds.Location));
        return new Rectangle(location, control.Size);
    }

    private static string Describe(Control control)
    {
        string identity = ControlIdentity(control);
        return $"{control.GetType().Name}('{identity}')";
    }

    private static string ControlIdentity(Control control)
    {
        return !string.IsNullOrWhiteSpace(control.AccessibleName)
            ? control.AccessibleName
            : !string.IsNullOrWhiteSpace(control.Name)
                ? control.Name
                : !string.IsNullOrWhiteSpace(control.Text)
                    ? control.Text
                    : control.GetType().Name;
    }

    private static string Format(Rectangle value) =>
        $"({value.X},{value.Y},{value.Width}x{value.Height})";

    private static string Format(Size value) => $"{value.Width}x{value.Height}";

    private static string Format(Point value) => $"({value.X},{value.Y})";

    private static void Check(bool condition, Func<string> messageFactory)
    {
        if (!condition)
        {
            throw new GeometryAssertionException(messageFactory());
        }
    }

    private static void PumpMessages()
    {
        // Application.DoEvents drains until the queue is empty. A control that
        // schedules follow-up layout work while handling Layout can therefore
        // make a geometry test spend an unbounded amount of time in the pump.
        // Layout itself is synchronous; this bounded native pump only delivers
        // enough pending window messages for handle/focus/scroll state to settle.
        for (int count = 0; count < 128; count++)
        {
            if (!PeekMessage(
                    out NativeMessage message,
                    IntPtr.Zero,
                    0,
                    0,
                    PmRemove))
            {
                break;
            }

            if (message.Message == WmQuit)
            {
                PostQuitMessage(unchecked((int)message.WParam.ToUInt64()));
                break;
            }

            _ = TranslateMessage(in message);
            _ = DispatchMessage(in message);
        }
    }

    private static IntPtr MakePointLParam(Point point)
    {
        long value = ((long)(point.Y & 0xFFFF) << 16) | (uint)(point.X & 0xFFFF);
        return new IntPtr(value);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr ChildWindowFromPointEx(
        IntPtr parent,
        NativePoint point,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(IntPtr window, ref NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsChild(IntPtr parent, IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr window,
        int message,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr window, int index);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(
        out NativeMessage message,
        IntPtr window,
        uint messageFilterMinimum,
        uint messageFilterMaximum,
        uint removeMessage);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(in NativeMessage message);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(in NativeMessage message);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr Window;
        public uint Message;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public NativePoint Point;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint(int x, int y)
    {
        public int X = x;
        public int Y = y;
    }

    private sealed record GeometryScenario(string Name, Size WindowSize, float FontScale);

    private sealed class GeometryAssertionException(string message) : Exception(message);

    private sealed class GeometryFixture : IDisposable
    {
        private Font ownedFont;

        private GeometryFixture(
            Size windowSize,
            float fontScale,
            bool maximumApplicability,
            bool visibleOnScreen)
        {
            ownedFont = CreateScaledFont(fontScale);
            Report = CreateSyntheticReport(maximumApplicability);
            Host = new GeometryHostForm
            {
                Font = ownedFont,
                MinimumSize = new Size(880, 600),
                Size = windowSize,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Text = "Statistics Geometry Harness - Synthetic / No Save"
            };
            Statistics = new QaStatisticsControl
            {
                Dock = DockStyle.Fill,
                Font = ownedFont
            };
            Host.Controls.Add(Statistics);
            Statistics.Bind(Report);

            if (visibleOnScreen)
            {
                Rectangle workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
                Host.Location = new Point(
                    workingArea.Left + Math.Max(0, (workingArea.Width - Host.Width) / 2),
                    workingArea.Top + Math.Max(0, (workingArea.Height - Host.Height) / 2));
                Host.ShowInTaskbar = true;
                Host.TopMost = true;
            }
            else
            {
                Rectangle virtualScreen = SystemInformation.VirtualScreen;
                Host.Location = new Point(
                    Math.Max(short.MinValue + 100, virtualScreen.Left - Host.Width - 200),
                    Math.Max(short.MinValue + 100, virtualScreen.Top - Host.Height - 200));
            }

            Host.Show();
            Host.Activate();
            LayoutAndPump();
        }

        public GeometryHostForm Host { get; }

        public QaStatisticsControl Statistics { get; }

        public QaReport Report { get; }

        public static GeometryFixture Create(
            Size windowSize,
            float fontScale,
            bool maximumApplicability,
            bool visibleOnScreen = false)
        {
            return new GeometryFixture(
                windowSize,
                fontScale,
                maximumApplicability,
                visibleOnScreen);
        }

        public void ResizeHost(Size size)
        {
            Host.WindowState = FormWindowState.Normal;
            Host.Size = size;
            LayoutAndPump();
        }

        public void SetFontScale(float scale)
        {
            Font replacement = CreateScaledFont(scale);
            Font previous = ownedFont;
            ownedFont = replacement;
            Host.Font = replacement;
            Statistics.Font = replacement;
            LayoutAndPump();
            previous.Dispose();
        }

        public void RefreshAndLayout()
        {
            Statistics.RefreshFromReport();
            LayoutAndPump();
        }

        public void LayoutAndPump()
        {
            FlowLayoutPanel outer = GetOuterFlow(Statistics);
            Size previousDisplaySize = Size.Empty;

            for (int pass = 0; pass < 3; pass++)
            {
                Host.PerformLayout();
                Statistics.PerformLayout();
                outer.PerformLayout();
                PumpMessages();

                Size displaySize = outer.DisplayRectangle.Size;
                if (displaySize == previousDisplaySize)
                {
                    break;
                }

                previousDisplaySize = displaySize;
            }
        }

        public void Dispose()
        {
            Host.Close();
            Host.Dispose();
            ownedFont.Dispose();
            PumpMessages();
        }

        private static Font CreateScaledFont(float scale)
        {
            Font baseline = SystemFonts.MessageBoxFont ?? Control.DefaultFont;
            return new Font(
                baseline.FontFamily,
                baseline.SizeInPoints * scale,
                baseline.Style,
                GraphicsUnit.Point,
                baseline.GdiCharSet,
                baseline.GdiVerticalFont);
        }
    }

    private sealed class GeometryHostForm : Form
    {
    }
}
