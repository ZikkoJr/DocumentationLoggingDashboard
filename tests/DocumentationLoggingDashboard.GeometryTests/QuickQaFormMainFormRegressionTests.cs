using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Forms;
using DocumentationLoggingDashboard.QAReports.Forms.Controls;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using FormsLabel = System.Windows.Forms.Label;

namespace DocumentationLoggingDashboard.GeometryTests;

/// <summary>
/// Focused UI-contract coverage for the two dashboard QA entry points and the
/// dedicated Quick QA form. Program.cs intentionally does not invoke this class;
/// the shared harness owner can add one RunAll call with the other suites.
/// </summary>
internal static class QuickQaFormMainFormRegressionTests
{
    private const string RequiredPrivacyReminder =
        "Do not enter guest names, guest emails, payment information, credentials, "
        + "confirmation-level PII, or copied raw rows. Describe the QA issue at field/check level.";

    private static readonly IReadOnlyDictionary<short, OpCode> OpCodesByValue =
        typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(opCode => opCode.Value);

    public static int RunAll(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        (string Name, Action Body)[] tests =
        [
            ("MainForm QA entry labels, routes, and shared bootstrap", TestMainFormEntryPoints),
            ("Quick QA single-page layout and checklist semantics", TestQuickQaPageAndChecklist),
            ("Quick QA Summary lifecycle, privacy, and save reentry", TestSummaryPrivacyAndSaveReentry),
            ("Quick QA duplicate-name selector and canonical fields", TestSelectorAndCanonicalFields)
        ];

        int failed = 0;
        output.WriteLine($"Quick QA form/MainForm harness: {tests.Length} tests");

        foreach ((string name, Action body) in tests)
        {
            output.WriteLine($"[RUN ] {name}");

            try
            {
                EnsureStaThread();
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
                ? $"[PASS] All {tests.Length} Quick QA form/MainForm tests passed."
                : $"[FAIL] {failed} of {tests.Length} Quick QA form/MainForm tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void TestMainFormEntryPoints()
    {
        using MainForm form = new();
        Button detailedButton = GetField<Button>(form, "detailedQaReportButton");
        Button quickButton = GetField<Button>(form, "quickQaButton");

        CheckEqual(
            "Detailed QA Report",
            detailedButton.Text,
            "The Detailed QA dashboard action label changed.");
        CheckEqual(
            "Quick QA",
            quickButton.Text,
            "The Quick QA dashboard action label changed.");

        MethodInfo openDetailed = GetPrivateMethod(typeof(MainForm), "OpenDetailedQaReport");
        MethodInfo openQuick = GetPrivateMethod(typeof(MainForm), "OpenQuickQa");
        MethodInfo bootstrap = GetPrivateMethod(typeof(MainForm), "CreateQaWorkflowDependencies");
        Delegate[] detailedRoutes = GetClickHandlers(detailedButton);
        Delegate[] quickRoutes = GetClickHandlers(quickButton);

        Check(
            detailedRoutes.Length == 1,
            $"Detailed QA has {detailedRoutes.Length} Click routes instead of exactly one.");
        Check(
            quickRoutes.Length == 1,
            $"Quick QA has {quickRoutes.Length} Click routes instead of exactly one.");
        Check(
            CountMethodCalls(detailedRoutes[0].Method, openDetailed) == 1
            && CountMethodCalls(detailedRoutes[0].Method, openQuick) == 0,
            "The Detailed QA button does not route exactly once to OpenDetailedQaReport.");
        Check(
            CountMethodCalls(quickRoutes[0].Method, openQuick) == 1
            && CountMethodCalls(quickRoutes[0].Method, openDetailed) == 0,
            "The Quick QA button does not route exactly once to OpenQuickQa.");
        Check(
            CountMethodCalls(openDetailed, bootstrap) == 1,
            "Detailed QA does not use the shared QA dependency bootstrap exactly once.");
        Check(
            CountMethodCalls(openQuick, bootstrap) == 1,
            "Quick QA does not use the shared QA dependency bootstrap exactly once.");
    }

    private static void TestQuickQaPageAndChecklist()
    {
        using QuickQaUiFixture fixture = new();
        QuickQaForm form = fixture.Form;
        Panel scrollPanel = GetField<Panel>(form, "contentScrollPanel");
        TableLayoutPanel page = GetField<TableLayoutPanel>(form, "contentLayoutPanel");
        FlowLayoutPanel checklistPanel = GetField<FlowLayoutPanel>(
            form,
            "checklistFlowLayoutPanel");

        Check(
            form.Controls.Count == 1
            && ReferenceEquals(form.Controls[0], scrollPanel)
            && scrollPanel.Dock == DockStyle.Fill
            && scrollPanel.AutoScroll,
            "Quick QA is not hosted by one fill-docked scrollable page.");
        Check(
            ReferenceEquals(page.Parent, scrollPanel)
            && page.ColumnCount == 1
            && page.RowCount == 8
            && page.Dock == DockStyle.Top,
            "Quick QA content is not one vertically stacked page.");
        Check(
            !Descendants(form).OfType<TabControl>().Any(),
            "Quick QA introduced a tabbed workflow instead of one page.");

        string[] sectionHeaders = checklistPanel.Controls
            .OfType<FormsLabel>()
            .Select(label => label.Text)
            .ToArray();
        CheckSequenceEqual(
            new[] { "Raw File QA", "Database QA" },
            sectionHeaders,
            "Raw and Database checks are not separated by the two required page headers.");

        QuickQaChecklistItemControl[] rows = checklistPanel.Controls
            .OfType<QuickQaChecklistItemControl>()
            .ToArray();
        Check(
            rows.Length == QuickQaChecklistCatalog.ExpectedDefinitionCount
            && rows.Length == 21,
            $"The form rendered {rows.Length} checklist rows instead of exactly 21.");
        CheckSequenceEqual(
            QuickQaChecklistCatalog.Definitions.Select(definition => definition.Id),
            rows.Select(row => row.CheckId),
            "The 21 Quick QA rows are missing, duplicated, or out of catalog order.");

        IReadOnlyDictionary<string, QuickQaChecklistItemControl> rowsById =
            rows.ToDictionary(row => row.CheckId, StringComparer.Ordinal);

        foreach (QuickQaCheckDefinition definition in QuickQaChecklistCatalog.Definitions)
        {
            QuickQaChecklistItemControl row = rowsById[definition.Id];
            RadioButton pass = GetField<RadioButton>(row, "passRadioButton");
            RadioButton warning = GetField<RadioButton>(row, "warningRadioButton");
            RadioButton fail = GetField<RadioButton>(row, "failRadioButton");
            RadioButton notApplicable = GetField<RadioButton>(
                row,
                "notApplicableRadioButton");

            Check(
                !Descendants(row).OfType<CheckBox>().Any(),
                $"'{definition.Id}' imported a Detailed-style warning checkbox.");

            if (definition.IsStrategyAvailability)
            {
                Check(
                    pass.Text == "Available"
                    && fail.Text == "Unavailable"
                    && pass.Visible
                    && fail.Visible
                    && !warning.Visible
                    && !notApplicable.Visible,
                    $"'{definition.Id}' does not expose only Available/Unavailable semantics.");
            }
            else
            {
                Check(
                    pass.Text == "Pass"
                    && warning.Text == "Warning"
                    && fail.Text == "Fail"
                    && pass.Visible
                    && warning.Visible
                    && fail.Visible,
                    $"'{definition.Id}' does not expose direct Pass/Warning/Fail semantics.");
                Check(
                    notApplicable.Visible == definition.AllowsNotApplicable,
                    $"'{definition.Id}' has the wrong N/A visibility.");
            }
        }

        QuickQaChecklistItemControl ordinary = rowsById[
            QuickQaChecklistIds.Raw.NamesAvailable];
        AssertRadioUpdatesStatus(
            form,
            ordinary,
            "passRadioButton",
            QuickQaCheckStatus.Pass);
        AssertRadioUpdatesStatus(
            form,
            ordinary,
            "warningRadioButton",
            QuickQaCheckStatus.Warning);
        AssertRadioUpdatesStatus(
            form,
            ordinary,
            "failRadioButton",
            QuickQaCheckStatus.Fail);

        QuickQaChecklistItemControl optional = rowsById[
            QuickQaChecklistIds.Raw.CurrencyConsistent];
        AssertRadioUpdatesStatus(
            form,
            optional,
            "notApplicableRadioButton",
            QuickQaCheckStatus.NotApplicable);

        string[] strategyIds =
        [
            QuickQaChecklistIds.Raw.SourceColumnAvailable,
            QuickQaChecklistIds.Raw.RateColumnAvailable,
            QuickQaChecklistIds.Raw.MarketColumnAvailable
        ];
        foreach (string strategyId in strategyIds)
        {
            AssertRadioUpdatesStatus(
                form,
                rowsById[strategyId],
                "passRadioButton",
                QuickQaCheckStatus.Pass);
        }

        AssertRadioUpdatesStatus(
            form,
            rowsById[strategyIds[0]],
            "failRadioButton",
            QuickQaCheckStatus.Fail);
        AssertSingleStrategyFinding(
            form.CurrentReport,
            QuickQaFindingSynchronizationService.StrategyWarningFindingId,
            QaFindingSeverity.Warning,
            "one unavailable Strategy column");

        AssertRadioUpdatesStatus(
            form,
            rowsById[strategyIds[1]],
            "failRadioButton",
            QuickQaCheckStatus.Fail);
        AssertSingleStrategyFinding(
            form.CurrentReport,
            QuickQaFindingSynchronizationService.StrategyWarningFindingId,
            QaFindingSeverity.Warning,
            "two unavailable Strategy columns");

        AssertRadioUpdatesStatus(
            form,
            rowsById[strategyIds[2]],
            "failRadioButton",
            QuickQaCheckStatus.Fail);
        AssertSingleStrategyFinding(
            form.CurrentReport,
            QuickQaFindingSynchronizationService.StrategyFailureFindingId,
            QaFindingSeverity.Failure,
            "three unavailable Strategy columns");
    }

    private static void TestSummaryPrivacyAndSaveReentry()
    {
        using QuickQaUiFixture fixture = new();
        QuickQaForm form = fixture.Form;
        FlowLayoutPanel checklistPanel = GetField<FlowLayoutPanel>(
            form,
            "checklistFlowLayoutPanel");
        IReadOnlyDictionary<string, QuickQaChecklistItemControl> rowsById =
            checklistPanel.Controls
                .OfType<QuickQaChecklistItemControl>()
                .ToDictionary(row => row.CheckId, StringComparer.Ordinal);
        TextBox summary = GetField<TextBox>(form, "summaryTextBox");
        FormsLabel stale = GetField<FormsLabel>(form, "summaryStaleLabel");
        FormsLabel privacy = GetField<FormsLabel>(form, "privacyReminderLabel");
        Button regenerate = GetField<Button>(form, "regenerateSummaryButton");
        Button save = GetField<Button>(form, "saveQuickQaButton");

        CheckEqual(
            RequiredPrivacyReminder,
            privacy.Text,
            "The concise Quick QA privacy reminder changed or lost a prohibited-data category.");
        Check(
            summary.Multiline && summary.WordWrap && summary.ScrollBars == ScrollBars.Vertical,
            "The Summary is not a multiline editable textbox.");
        CheckEqual(
            "Regenerate Summary",
            regenerate.Text,
            "The explicit stale-summary recovery action is unclear.");
        CheckEqual(
            "Save Quick QA",
            save.Text,
            "The Quick QA save action label changed.");
        Check(
            !save.CausesValidation,
            "Save Quick QA relies on implicit focus validation instead of its explicit commit path.");

        AssertRadioUpdatesStatus(
            form,
            rowsById[QuickQaChecklistIds.Raw.NamesAvailable],
            "warningRadioButton",
            QuickQaCheckStatus.Warning);
        string generatedSummary = summary.Text;
        Check(
            generatedSummary.Length > 0
            && !string.Equals(
                generatedSummary,
                QuickQaSummaryService.CleanPassSummary,
                StringComparison.Ordinal)
            && !form.CurrentReport.SummaryWasManuallyEdited
            && !stale.Visible,
            "A Warning did not produce a current generated Summary.");

        const string manualSummary = "Manual field-level QA note.";
        summary.Text = manualSummary;
        PumpMessages();
        Check(
            form.CurrentReport.SummaryWasManuallyEdited
            && form.CurrentReport.Summary == manualSummary
            && !stale.Visible
            && regenerate.Enabled,
            "Manual Summary editing was not retained as a current editable override.");

        AssertRadioUpdatesStatus(
            form,
            rowsById[QuickQaChecklistIds.Raw.ConfirmationNumberAvailable],
            "failRadioButton",
            QuickQaCheckStatus.Fail);
        Check(
            form.CurrentReport.Summary == manualSummary
            && form.CurrentReport.SummaryWasManuallyEdited
            && stale.Visible
            && regenerate.Enabled,
            "A QA-state change silently replaced a manual Summary or failed to mark it stale.");

        regenerate.PerformClick();
        PumpMessages();
        Check(
            !form.CurrentReport.SummaryWasManuallyEdited
            && !stale.Visible
            && summary.Text == form.CurrentReport.Summary
            && summary.Text != manualSummary,
            "Regenerate Summary did not reset the stale manual text to current generated content.");

        Delegate[] saveRoutes = GetClickHandlers(save);
        MethodInfo saveMethod = GetPrivateMethod(typeof(QuickQaForm), "SaveQuickQa");
        Check(
            saveRoutes.Length == 1
            && CountMethodCalls(saveRoutes[0].Method, saveMethod) == 1,
            "Save Quick QA does not have exactly one route to the guarded save method.");

        FieldInfo saveGuard = GetPrivateField(typeof(QuickQaForm), "isSavingQuickQa");
        saveGuard.SetValue(form, true);
        save.Enabled = false;
        saveMethod.Invoke(form, parameters: null);
        PumpMessages();
        Check(
            (bool)saveGuard.GetValue(form)!
            && !save.Enabled,
            "A reentrant save invocation entered or disturbed the active save operation.");

        saveGuard.SetValue(form, false);
        save.Enabled = true;
    }

    private static void TestSelectorAndCanonicalFields()
    {
        using QuickQaUiFixture fixture = new();
        QuickQaForm form = fixture.Form;
        QaHotelSelectorControl selector = GetField<QaHotelSelectorControl>(
            form,
            "hotelSelectorControl");
        ListBox list = GetField<ListBox>(selector, "hotelListBox");
        TextBox search = GetField<TextBox>(selector, "searchTextBox");
        TextBox hotelName = GetField<TextBox>(form, "hotelNameTextBox");
        TextBox hotelId = GetField<TextBox>(form, "hotelIdTextBox");
        TextBox pms = GetField<TextBox>(form, "pmsTextBox");

        Check(
            list.Items.Count == 2
            && list.SelectionMode == SelectionMode.One,
            "The selector did not expose both real duplicate-name metadata records.");
        string firstDisplay = list.GetItemText(list.Items[0]) ?? string.Empty;
        string secondDisplay = list.GetItemText(list.Items[1]) ?? string.Empty;
        Check(
            firstDisplay == "H-100 — Shared Harbour"
            && secondDisplay == "H-200 — Shared Harbour"
            && !string.Equals(firstDisplay, secondDisplay, StringComparison.Ordinal),
            "Duplicate Hotel names are not visibly distinguished by canonical Hotel ID.");

        search.Text = "Shared Harbour";
        PumpMessages();
        Check(
            list.Items.Count == 2,
            "Hotel Name search did not retain both duplicate-name records.");
        search.Text = "H-200";
        PumpMessages();
        Check(
            list.Items.Count == 1 && list.SelectedIndex == -1,
            "Hotel ID search selected an arbitrary record or returned the wrong result count.");

        list.SelectedIndex = 0;
        PumpMessages();
        Check(
            selector.SelectedHotel is not null
            && selector.SelectedHotel.HotelId == "H-200",
            "The selector did not return the real metadata record chosen from the filtered list.");
        CheckEqual(
            "Shared Harbour",
            hotelName.Text,
            "The canonical Hotel Name display did not synchronize from metadata.");
        CheckEqual(
            "H-200",
            hotelId.Text,
            "The canonical Hotel ID display did not synchronize from metadata.");
        CheckEqual(
            "PMS Two",
            pms.Text,
            "The canonical PMS display did not synchronize from metadata.");
        Check(
            hotelName.ReadOnly
            && hotelId.ReadOnly
            && pms.ReadOnly
            && !hotelName.TabStop
            && !hotelId.TabStop
            && !pms.TabStop,
            "Canonical Hotel/PMS fields permit arbitrary editing or tab entry.");
    }

    private static void AssertRadioUpdatesStatus(
        QuickQaForm form,
        QuickQaChecklistItemControl row,
        string radioButtonFieldName,
        QuickQaCheckStatus expectedStatus)
    {
        RadioButton radioButton = GetField<RadioButton>(row, radioButtonFieldName);
        radioButton.Checked = true;
        PumpMessages();
        Check(
            form.CurrentReport.GetChecklistResult(row.CheckId).Status == expectedStatus,
            $"'{row.CheckId}' did not map '{radioButton.Text}' directly to {expectedStatus}.");
    }

    private static void AssertSingleStrategyFinding(
        QuickQaReport report,
        string expectedFindingId,
        QaFindingSeverity expectedSeverity,
        string context)
    {
        QaFinding[] strategyFindings = report.Findings
            .Where(finding => finding.FindingId
                is QuickQaFindingSynchronizationService.StrategyWarningFindingId
                or QuickQaFindingSynchronizationService.StrategyFailureFindingId)
            .ToArray();
        Check(
            strategyFindings.Length == 1
            && strategyFindings[0].FindingId == expectedFindingId
            && strategyFindings[0].Severity == expectedSeverity,
            $"Strategy aggregation is incorrect for {context}.");
    }

    private static Delegate[] GetClickHandlers(Button button)
    {
        PropertyInfo eventsProperty = typeof(Component).GetProperty(
            "Events",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new RegressionAssertionException(
                "Could not inspect WinForms event registrations.");
        EventHandlerList handlers = (EventHandlerList)eventsProperty.GetValue(button)!;
        const BindingFlags staticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        FieldInfo? clickKeyField = typeof(Control).GetField(
                "s_clickEvent",
                staticPrivate)
            ?? typeof(Control).GetField("EventClick", staticPrivate)
            ?? typeof(Control).GetFields(staticPrivate).SingleOrDefault(field =>
                field.FieldType == typeof(object)
                && field.Name.Replace("_", string.Empty, StringComparison.Ordinal)
                    .Equals("sclickevent", StringComparison.OrdinalIgnoreCase));
        object clickKey = clickKeyField?.GetValue(null)
            ?? throw new RegressionAssertionException(
                "Could not locate the WinForms Click event key.");
        return handlers[clickKey]?.GetInvocationList() ?? Array.Empty<Delegate>();
    }

    private static int CountMethodCalls(MethodInfo caller, MethodBase expected)
    {
        MethodBody body = caller.GetMethodBody()
            ?? throw new RegressionAssertionException(
                $"Method '{caller.Name}' has no inspectable body.");
        byte[] il = body.GetILAsByteArray()
            ?? throw new RegressionAssertionException(
                $"Method '{caller.Name}' has no inspectable IL.");
        int offset = 0;
        int count = 0;

        while (offset < il.Length)
        {
            OpCode opCode = ReadOpCode(il, ref offset);
            int operandOffset = offset;
            int operandSize = GetOperandSize(opCode, il, operandOffset);

            if ((opCode == OpCodes.Call || opCode == OpCodes.Callvirt)
                && opCode.OperandType == OperandType.InlineMethod)
            {
                int token = BitConverter.ToInt32(il, operandOffset);
                MethodBase? called = ResolveMethod(caller, token);
                if (called is not null
                    && called.Module == expected.Module
                    && called.MetadataToken == expected.MetadataToken)
                {
                    count++;
                }
            }

            offset += operandSize;
        }

        return count;
    }

    private static MethodBase? ResolveMethod(MethodInfo caller, int token)
    {
        try
        {
            Type[]? typeArguments = caller.DeclaringType?.IsGenericType == true
                ? caller.DeclaringType.GetGenericArguments()
                : null;
            Type[]? methodArguments = caller.IsGenericMethod
                ? caller.GetGenericArguments()
                : null;
            return caller.Module.ResolveMethod(token, typeArguments, methodArguments);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static OpCode ReadOpCode(byte[] il, ref int offset)
    {
        byte first = il[offset++];
        short value = first == 0xFE
            ? unchecked((short)(0xFE00 | il[offset++]))
            : first;
        return OpCodesByValue.TryGetValue(value, out OpCode opCode)
            ? opCode
            : throw new RegressionAssertionException(
                $"Unknown IL opcode 0x{unchecked((ushort)value):X4}.");
    }

    private static int GetOperandSize(OpCode opCode, byte[] il, int operandOffset)
    {
        return opCode.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or
                OperandType.ShortInlineI or
                OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget or
                OperandType.InlineField or
                OperandType.InlineI or
                OperandType.InlineMethod or
                OperandType.InlineSig or
                OperandType.InlineString or
                OperandType.InlineTok or
                OperandType.InlineType or
                OperandType.ShortInlineR => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, operandOffset)),
            _ => throw new RegressionAssertionException(
                $"Unsupported IL operand type '{opCode.OperandType}'.")
        };
    }

    private static T GetField<T>(object instance, string fieldName)
        where T : class
    {
        FieldInfo field = GetPrivateField(instance.GetType(), fieldName);
        return field.GetValue(instance) as T
            ?? throw new RegressionAssertionException(
                $"Field '{instance.GetType().Name}.{fieldName}' was not a {typeof(T).Name}.");
    }

    private static FieldInfo GetPrivateField(Type type, string fieldName)
    {
        return type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new RegressionAssertionException(
                $"Private field '{type.Name}.{fieldName}' was not found.");
    }

    private static MethodInfo GetPrivateMethod(Type type, string methodName)
    {
        return type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new RegressionAssertionException(
                $"Private method '{type.Name}.{methodName}' was not found.");
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;

            foreach (Control descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static void CheckSequenceEqual(
        IEnumerable<string> expected,
        IEnumerable<string> actual,
        string message)
    {
        string[] expectedArray = expected.ToArray();
        string[] actualArray = actual.ToArray();
        if (!expectedArray.SequenceEqual(actualArray, StringComparer.Ordinal))
        {
            throw new RegressionAssertionException(
                $"{message} Expected [{string.Join(", ", expectedArray)}], "
                + $"received [{string.Join(", ", actualArray)}].");
        }
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

    private static void EnsureStaThread()
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new RegressionAssertionException(
                "Quick QA UI regression tests must run on the STA harness thread.");
        }
    }

    private static void PumpMessages()
    {
        Application.DoEvents();
    }

    private sealed class QuickQaUiFixture : IDisposable
    {
        private static readonly string TemporaryParentPath = Path.GetFullPath(Path.Combine(
            Path.GetTempPath(),
            "DocumentationLoggingDashboard.QuickQaFormMainFormRegressionTests"));

        public QuickQaUiFixture()
        {
            RootPath = Path.GetFullPath(Path.Combine(
                TemporaryParentPath,
                "r-" + Guid.NewGuid().ToString("N")));

            try
            {
                Directory.CreateDirectory(RootPath);
                QaStoragePaths paths = new(RootPath);
                new QaStorageInitializer(paths).Initialize();
                QaMetadataService metadataService = new(
                    paths,
                    new QaFolderNameSanitizer());
                QaPmsMetadata[] pmsSystems =
                [
                    new()
                    {
                        PmsName = "PMS One",
                        FolderName = "PMS_One"
                    },
                    new()
                    {
                        PmsName = "PMS Two",
                        FolderName = "PMS_Two"
                    }
                ];
                QaHotelMetadata[] hotels =
                [
                    new()
                    {
                        HotelId = "H-100",
                        HotelName = "Shared Harbour",
                        PmsName = "PMS One",
                        FolderName = "Shared_Harbour__H-100"
                    },
                    new()
                    {
                        HotelId = "H-200",
                        HotelName = "Shared Harbour",
                        PmsName = "PMS Two",
                        FolderName = "Shared_Harbour__H-200"
                    }
                ];

                Form = new QuickQaForm(
                    metadataService,
                    pmsSystems,
                    hotels,
                    paths)
                {
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual
                };
                Rectangle virtualScreen = SystemInformation.VirtualScreen;
                Form.Location = new Point(
                    Math.Max(short.MinValue + 100, virtualScreen.Left - Form.Width - 200),
                    Math.Max(short.MinValue + 100, virtualScreen.Top - Form.Height - 200));
                Form.Show();

                for (int pass = 0; pass < 3; pass++)
                {
                    Form.PerformLayout();
                    PumpMessages();
                }
            }
            catch
            {
                CleanupRoot();
                throw;
            }
        }

        public string RootPath { get; }

        public QuickQaForm Form { get; private set; } = null!;

        public void Dispose()
        {
            if (Form is not null)
            {
                Form.Close();
                Form.Dispose();
                PumpMessages();
            }

            CleanupRoot();
        }

        private void CleanupRoot()
        {
            string? actualParent = Path.GetDirectoryName(
                Path.TrimEndingDirectorySeparator(RootPath));
            if (!string.Equals(
                actualParent,
                TemporaryParentPath,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new RegressionAssertionException(
                    "Refused to clean a Quick QA UI root outside the dedicated test parent.");
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
    }
}
