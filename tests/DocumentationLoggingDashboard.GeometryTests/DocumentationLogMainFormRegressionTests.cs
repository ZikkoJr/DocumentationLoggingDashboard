using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using DocumentationLoggingDashboard.DocumentationLogs;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.QAReports.Forms.Controls;
using FormsLabel = System.Windows.Forms.Label;

namespace DocumentationLoggingDashboard.GeometryTests;

/// <summary>
/// Focused, noninteractive UI-contract coverage for the Excel documentation-log
/// workflow hosted by <see cref="MainForm"/>.
/// </summary>
internal static class DocumentationLogMainFormRegressionTests
{
    private const string PrivacyReminder =
        "Reminder: Do not enter guest names, emails, payment data, credentials, or full hotel files into logs. "
        + "Use ticket IDs, hotel IDs, script names, and summarized issues instead.";

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
            ("Dashboard labels, accessibility, and single button routes", TestDashboardContractAndRoutes),
            ("Per-type inputs and canonical Debug Hotel context", TestDynamicInputsAndCanonicalDebugContext),
            ("Independent Running selections and remembered workbooks", TestRunningWorkbookSelectionAndPreferenceIsolation),
            ("Submit reentry guard and explicit clear lifecycle", TestSubmitReentryAndClearLifecycle)
        ];

        int failed = 0;
        output.WriteLine($"Documentation-log MainForm harness: {tests.Length} tests");

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
                ? $"[PASS] All {tests.Length} documentation-log MainForm tests passed."
                : $"[FAIL] {failed} of {tests.Length} documentation-log MainForm tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void TestDashboardContractAndRoutes()
    {
        using MainForm form = new();
        ComboBox logType = GetField<ComboBox>(form, "logTypeComboBox");
        ComboBox runningWorkbook = GetField<ComboBox>(
            form,
            "runningWorkbookComboBox");

        CheckEqual(
            "Log Type",
            GetField<FormsLabel>(form, "logTypeLabel").Text,
            "The existing log-type selector label changed.");
        Check(
            logType.DropDownStyle == ComboBoxStyle.DropDownList,
            "The existing log-type selector permits arbitrary text.");
        CheckSequenceEqual(
            new[] { "Debugging Log", "Script Editing Log", "Script Creation Log" },
            logType.Items.Cast<object>().Select(item => logType.GetItemText(item) ?? string.Empty),
            "The dashboard no longer exposes the three original log types in their established order.");
        CheckEqual(
            PrivacyReminder,
            GetField<FormsLabel>(form, "privacyReminderLabel").Text,
            "The dashboard privacy reminder changed.");

        CheckEqual(
            "Running Log Workbook:",
            GetField<FormsLabel>(form, "runningWorkbookLabel").Text,
            "The active Running-workbook label changed.");
        Check(
            runningWorkbook.DropDownStyle == ComboBoxStyle.DropDownList,
            "The Running-workbook selector permits arbitrary output text.");
        CheckEqual(
            "Running documentation log workbook",
            runningWorkbook.AccessibleName,
            "The Running-workbook selector lost its accessible name.");

        AssertSingleButtonRoute(form, "previewEntryButton", "Preview Entry", "PreviewEntry");
        AssertSingleButtonRoute(form, "submitEntryButton", "Submit Entry", "SubmitEntry");
        AssertSingleButtonRoute(form, "clearFormButton", "Clear Form", "ClearForm");
        AssertSingleButtonRoute(
            form,
            "createNewLogFileButton",
            "Create New Log File",
            "CreateNewLogFile");
        AssertSingleButtonRoute(
            form,
            "openSelectedLogWorkbookButton",
            "Open Selected Log Workbook",
            "OpenSelectedLogWorkbook");
        AssertSingleButtonRoute(
            form,
            "openLogsFolderButton",
            "Open Logs Folder",
            "OpenLogsFolder");
        AssertSingleButtonRoute(
            form,
            "openLogIndexButton",
            "Open Log Index",
            "OpenLogIndex");
        AssertSingleButtonRoute(
            form,
            "changeLogsFolderButton",
            "Change Logs Folder",
            "ChangeLogsFolder");
        AssertSingleButtonRoute(
            form,
            "resetDefaultFolderButton",
            "Reset to Default Folder",
            "ResetToDefaultFolder");
        AssertSingleButtonRoute(
            form,
            "manageQaHotelsPmsButton",
            "Manage QA Hotels / PMS",
            "OpenQaMetadataManagement");
        AssertSingleButtonRoute(
            form,
            "quickQaButton",
            "Quick QA",
            "OpenQuickQa");
        AssertSingleButtonRoute(
            form,
            "detailedQaReportButton",
            "Detailed QA Report",
            "OpenDetailedQaReport");

        MethodInfo bootstrap = GetPrivateMethod(
            typeof(MainForm),
            "CreateQaWorkflowDependencies");
        Check(
            CountMethodCalls(
                GetPrivateMethod(typeof(MainForm), "OpenQuickQa"),
                bootstrap) == 1,
            "Quick QA no longer uses the shared QA dependency bootstrap exactly once.");
        Check(
            CountMethodCalls(
                GetPrivateMethod(typeof(MainForm), "OpenDetailedQaReport"),
                bootstrap) == 1,
            "Detailed QA no longer uses the shared QA dependency bootstrap exactly once.");
    }

    private static void TestDynamicInputsAndCanonicalDebugContext()
    {
        using MainFormDocumentationFixture fixture = new();
        MainForm form = fixture.Form;
        ComboBox logType = GetField<ComboBox>(form, "logTypeComboBox");

        SelectLogType(logType, LogType.DebuggingLog);
        AssertFieldLabels(
            form,
            "Hotel *",
            "Hotel Name",
            "Hotel ID",
            "PMS",
            "Error Shown On Ticket *",
            "Root Cause *",
            "Fix Applied *",
            "Created By",
            "Notes / Follow-up");

        QaHotelSelectorControl debugSelector = GetField<QaHotelSelectorControl>(
            form,
            "debuggingHotelSelectorControl");
        TextBox search = GetField<TextBox>(debugSelector, "searchTextBox");
        ListBox hotels = GetField<ListBox>(debugSelector, "hotelListBox");
        CheckEqual(
            "Search existing hotels",
            search.AccessibleName,
            "The Debug Hotel search lost its accessible name.");
        CheckEqual(
            "Filters existing hotels by Hotel ID or Hotel Name. Typed text does not select or create a hotel.",
            search.AccessibleDescription,
            "The Debug Hotel search no longer explains its canonical selection behavior.");
        CheckEqual(
            "Existing QA hotels",
            hotels.AccessibleName,
            "The Debug Hotel result list lost its accessible name.");
        CheckEqual(
            "Select one existing hotel metadata record for the QA report.",
            hotels.AccessibleDescription,
            "The Debug Hotel result list no longer identifies canonical metadata selection.");

        search.Text = "Example Hotel";
        PumpMessages();
        Check(
            hotels.Items.Count == 1,
            "Debug Hotel search by canonical Hotel Name returned the wrong result count.");
        search.Text = "1953";
        PumpMessages();
        Check(
            hotels.Items.Count == 1,
            "Debug Hotel search by canonical Hotel ID returned the wrong result count.");
        hotels.SelectedIndex = 0;
        PumpMessages();

        TextBox canonicalName = GetField<TextBox>(
            form,
            "debuggingHotelNameTextBox");
        TextBox canonicalId = GetField<TextBox>(
            form,
            "debuggingHotelIdTextBox");
        TextBox canonicalPms = GetField<TextBox>(
            form,
            "debuggingPmsTextBox");
        CheckEqual(
            "Example Hotel",
            canonicalName.Text,
            "Debug selection did not show the canonical Hotel Name.");
        CheckEqual(
            "1953",
            canonicalId.Text,
            "Debug selection did not show the canonical Hotel ID.");
        CheckEqual(
            "Mews",
            canonicalPms.Text,
            "Debug selection did not derive the canonical PMS.");
        Check(
            canonicalName.ReadOnly
            && canonicalId.ReadOnly
            && canonicalPms.ReadOnly
            && !canonicalName.TabStop
            && !canonicalId.TabStop
            && !canonicalPms.TabStop,
            "Canonical Debug Hotel/PMS context can be edited or entered by tab.");

        TextBox debugError = GetInput(
            form,
            DocumentationLogFieldKeys.ErrorShownOnTicket);
        debugError.Text = "Stale Debug content";

        SelectLogType(logType, LogType.ScriptEditingLog);
        Check(
            debugSelector.IsDisposed && debugError.IsDisposed,
            "Switching away from Debugging retained its selector or business input controls.");
        AssertNoHotelPicker(form, "Script Editing");
        AssertFieldLabels(
            form,
            "Hotel ID(s) *",
            "Script Name *",
            "Reason For Edit *",
            "Changes Made *",
            "Created By",
            "Notes / Follow-up");
        AssertAllInputValuesBlank(form, "Script Editing inherited stale Debug values.");
        GetInput(form, DocumentationLogFieldKeys.HotelIds).Text = "1953; 2093";
        GetInput(form, DocumentationLogFieldKeys.ScriptName).Text = "EditScript.csx";

        SelectLogType(logType, LogType.ScriptCreationLog);
        AssertNoHotelPicker(form, "Script Creation");
        AssertFieldLabels(
            form,
            "Hotel ID(s) *",
            "Script Name *",
            "Reason For Creation *",
            "Script Purpose / What It Does *",
            "Created By",
            "Notes / Follow-up");
        AssertAllInputValuesBlank(form, "Script Creation inherited stale Script Editing values.");

        SelectLogType(logType, LogType.DebuggingLog);
        QaHotelSelectorControl freshDebugSelector =
            GetField<QaHotelSelectorControl>(form, "debuggingHotelSelectorControl");
        Check(
            freshDebugSelector.SelectedHotel is null,
            "Returning to Debugging leaked the prior Hotel selection.");
        AssertAllInputValuesBlank(form, "Returning to Debugging leaked prior business values.");
    }

    private static void TestRunningWorkbookSelectionAndPreferenceIsolation()
    {
        using MainFormDocumentationFixture fixture = new();
        string debugFirst = fixture.Environment.CreateRunning(
            LogType.DebuggingLog,
            "debug-first");
        string debugRemembered = fixture.Environment.CreateRunning(
            LogType.DebuggingLog,
            "debug-remembered");
        string editRemembered = fixture.Environment.CreateRunning(
            LogType.ScriptEditingLog,
            "edit-remembered");
        string creationFallback = fixture.Environment.CreateRunning(
            LogType.ScriptCreationLog,
            "creation-available");
        string creationMissing = fixture.Environment.CreateRunning(
            LogType.ScriptCreationLog,
            "creation-remembered-then-missing");

        fixture.Environment.PreferencesService.SaveLastUsedWorkbookFileName(
            LogType.DebuggingLog,
            debugRemembered);
        fixture.Environment.PreferencesService.SaveLastUsedWorkbookFileName(
            LogType.ScriptEditingLog,
            editRemembered);
        fixture.Environment.PreferencesService.SaveLastUsedWorkbookFileName(
            LogType.ScriptCreationLog,
            creationMissing);
        File.Delete(fixture.Environment.Paths.ResolveRunningWorkbookPath(
            LogType.ScriptCreationLog,
            creationMissing));

        fixture.RefreshCurrentLogType();
        MainForm form = fixture.Form;
        ComboBox logType = GetField<ComboBox>(form, "logTypeComboBox");
        ComboBox running = GetField<ComboBox>(form, "runningWorkbookComboBox");
        Button openSelected = GetField<Button>(
            form,
            "openSelectedLogWorkbookButton");

        SelectLogType(logType, LogType.DebuggingLog, fixture.RefreshCurrentLogType);
        CheckSetEqual(
            new[] { debugFirst, debugRemembered },
            running.Items.Cast<object>().Select(item => running.GetItemText(item) ?? string.Empty),
            "Debugging listed a workbook from another log type or omitted a compatible workbook.");
        CheckEqual(
            debugRemembered,
            running.SelectedItem as string,
            "Debugging did not restore its remembered workbook.");
        Check(openSelected.Enabled, "The selected Debugging workbook cannot be opened.");

        running.SelectedItem = debugFirst;
        PumpMessages();
        CheckEqual(
            debugFirst,
            fixture.Environment.PreferencesService.LoadLastUsedWorkbookFileName(
                LogType.DebuggingLog),
            "Selecting a Debugging workbook did not persist its independent preference.");

        SelectLogType(logType, LogType.ScriptEditingLog);
        CheckSequenceEqual(
            new[] { editRemembered },
            running.Items.Cast<object>().Select(item => running.GetItemText(item) ?? string.Empty),
            "Script Editing did not isolate its compatible Running workbook list.");
        CheckEqual(
            editRemembered,
            running.SelectedItem as string,
            "Script Editing did not restore its remembered workbook.");
        CheckEqual(
            debugFirst,
            fixture.Environment.PreferencesService.LoadLastUsedWorkbookFileName(
                LogType.DebuggingLog),
            "Changing log type overwrote the Debugging preference.");

        SelectLogType(logType, LogType.ScriptCreationLog);
        CheckSequenceEqual(
            new[] { creationFallback },
            running.Items.Cast<object>().Select(item => running.GetItemText(item) ?? string.Empty),
            "Script Creation did not gracefully exclude its missing remembered workbook.");
        CheckEqual(
            creationFallback,
            running.SelectedItem as string,
            "Script Creation did not fall back to an available compatible workbook.");
        Check(
            openSelected.Enabled,
            "The fallback Script Creation workbook cannot be opened.");
        CheckEqual(
            editRemembered,
            fixture.Environment.PreferencesService.LoadLastUsedWorkbookFileName(
                LogType.ScriptEditingLog),
            "Changing to Script Creation overwrote the Script Editing preference.");

        string selectedBeforeClear = (string)running.SelectedItem!;
        GetInput(form, DocumentationLogFieldKeys.HotelIds).Text = "3001";
        GetInput(form, DocumentationLogFieldKeys.ScriptName).Text = "CreateScript.csx";
        InvokePrivate(form, "ClearForm");
        CheckEqual(
            selectedBeforeClear,
            running.SelectedItem as string,
            "Clearing business fields also cleared the active Running workbook.");
        AssertAllInputValuesBlank(
            form,
            "Clear Form did not clear the current Script Creation business values.");
        CheckEqual(
            creationMissing,
            fixture.Environment.PreferencesService.LoadLastUsedWorkbookFileName(
                LogType.ScriptCreationLog),
            "Rendering or clearing form fields rewrote the persistent Script Creation preference.");
    }

    private static void TestSubmitReentryAndClearLifecycle()
    {
        using MainFormDocumentationFixture fixture = new();
        string runningFile = fixture.Environment.CreateRunning(
            LogType.DebuggingLog,
            "debug-submit-guard");
        fixture.Environment.PreferencesService.SaveLastUsedWorkbookFileName(
            LogType.DebuggingLog,
            runningFile);
        fixture.RefreshCurrentLogType();

        MainForm form = fixture.Form;
        QaHotelSelectorControl selector = GetField<QaHotelSelectorControl>(
            form,
            "debuggingHotelSelectorControl");
        ListBox hotels = GetField<ListBox>(selector, "hotelListBox");
        hotels.SelectedIndex = 0;
        PumpMessages();
        GetInput(form, DocumentationLogFieldKeys.ErrorShownOnTicket).Text = "Error";
        GetInput(form, DocumentationLogFieldKeys.RootCause).Text = "Cause";
        GetInput(form, DocumentationLogFieldKeys.FixApplied).Text = "Fix";
        TextBox preview = GetField<TextBox>(form, "previewTextBox");
        preview.Text = "Existing preview";
        Button submit = GetField<Button>(form, "submitEntryButton");
        FieldInfo saveGuard = GetPrivateField(
            typeof(MainForm),
            "isSavingDocumentationLog");

        saveGuard.SetValue(form, true);
        submit.Enabled = false;
        InvokePrivate(form, "SubmitEntry");
        PumpMessages();
        Check(
            (bool)saveGuard.GetValue(form)! && !submit.Enabled,
            "A reentrant Submit invocation entered or disturbed the active save operation.");
        CheckEqual(
            "Error",
            GetInput(form, DocumentationLogFieldKeys.ErrorShownOnTicket).Text,
            "A rejected reentrant Submit cleared entered business content.");
        CheckEqual(
            "1953",
            selector.SelectedHotel?.HotelId,
            "A rejected reentrant Submit cleared the canonical Hotel selection.");
        CheckEqual(
            "Existing preview",
            preview.Text,
            "A rejected reentrant Submit cleared the existing preview.");

        MethodInfo submitMethod = GetPrivateMethod(typeof(MainForm), "SubmitEntry");
        MethodInfo saveMethod = typeof(DocumentationLogSaveService).GetMethod(
                nameof(DocumentationLogSaveService.Save),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: [typeof(DocumentationLogSaveRequest)],
                modifiers: null)
            ?? throw Failure("DocumentationLogSaveService.Save was not found.");
        MethodInfo renderMethod = GetPrivateMethod(
            typeof(MainForm),
            "RenderFieldsForSelectedLogType");
        int saveOffset = GetMethodCallOffsets(submitMethod, saveMethod).SingleOrDefault(-1);
        int renderOffset = GetMethodCallOffsets(submitMethod, renderMethod).SingleOrDefault(-1);
        Check(
            saveOffset >= 0 && renderOffset > saveOffset,
            "Submit does not defer form clearing until after the transactional save returns successfully.");

        saveGuard.SetValue(form, false);
        submit.Enabled = true;
        string selectedWorkbook = (string)GetField<ComboBox>(
            form,
            "runningWorkbookComboBox").SelectedItem!;
        InvokePrivate(form, "ClearForm");
        PumpMessages();
        QaHotelSelectorControl replacementSelector =
            GetField<QaHotelSelectorControl>(form, "debuggingHotelSelectorControl");
        Check(
            selector.IsDisposed
            && replacementSelector.SelectedHotel is null,
            "Clear Form retained the prior Debug Hotel selector state.");
        AssertAllInputValuesBlank(form, "Clear Form retained Debug business content.");
        CheckEqual(
            string.Empty,
            GetField<TextBox>(form, "previewTextBox").Text,
            "Clear Form retained a stale preview.");
        CheckEqual(
            selectedWorkbook,
            GetField<ComboBox>(form, "runningWorkbookComboBox").SelectedItem as string,
            "Clear Form changed the selected Running workbook.");
    }

    private static void AssertSingleButtonRoute(
        MainForm form,
        string buttonFieldName,
        string expectedText,
        string targetMethodName)
    {
        Button button = GetField<Button>(form, buttonFieldName);
        CheckEqual(
            expectedText,
            button.Text,
            $"The '{buttonFieldName}' operator label changed.");

        Delegate[] routes = GetClickHandlers(button);
        MethodInfo target = GetPrivateMethod(typeof(MainForm), targetMethodName);
        Check(
            routes.Length == 1,
            $"'{expectedText}' has {routes.Length} Click handlers instead of exactly one.");
        Check(
            CountMethodCalls(routes[0].Method, target) == 1,
            $"'{expectedText}' does not route exactly once to {targetMethodName}.");
    }

    private static void AssertFieldLabels(MainForm form, params string[] expected)
    {
        TableLayoutPanel fields = GetField<TableLayoutPanel>(
            form,
            "fieldsTableLayoutPanel");
        string[] actual = Enumerable.Range(0, fields.RowCount)
            .Select(row => fields.GetControlFromPosition(0, row)?.Text ?? "<missing>")
            .ToArray();
        CheckSequenceEqual(
            expected,
            actual,
            "The selected log type rendered the wrong labels or field order.");
    }

    private static void AssertNoHotelPicker(MainForm form, string context)
    {
        TableLayoutPanel fields = GetField<TableLayoutPanel>(
            form,
            "fieldsTableLayoutPanel");
        Check(
            !Descendants(fields).OfType<QaHotelSelectorControl>().Any(),
            $"{context} rendered a Hotel picker instead of manual Hotel ID(s).");
        Check(
            GetPrivateField(typeof(MainForm), "debuggingHotelSelectorControl")
                .GetValue(form) is null,
            $"{context} retained a hidden Debug Hotel selector reference.");
    }

    private static void AssertAllInputValuesBlank(MainForm form, string message)
    {
        Check(
            GetInputs(form).Values.All(textBox => textBox.Text.Length == 0),
            message);
    }

    private static TextBox GetInput(MainForm form, string fieldKey)
    {
        return GetInputs(form)
            .Single(pair => pair.Key.Key.Equals(fieldKey, StringComparison.Ordinal))
            .Value;
    }

    private static Dictionary<DocumentationLogFormFieldDefinition, TextBox> GetInputs(
        MainForm form)
    {
        return GetField<Dictionary<DocumentationLogFormFieldDefinition, TextBox>>(
            form,
            "fieldInputs");
    }

    private static void SelectLogType(
        ComboBox selector,
        LogType logType,
        Action? refreshWhenAlreadySelected = null)
    {
        int index = logType switch
        {
            LogType.DebuggingLog => 0,
            LogType.ScriptEditingLog => 1,
            LogType.ScriptCreationLog => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(logType))
        };

        if (selector.SelectedIndex == index)
        {
            refreshWhenAlreadySelected?.Invoke();
        }
        else
        {
            selector.SelectedIndex = index;
        }

        PumpMessages();
    }

    private static Delegate[] GetClickHandlers(Button button)
    {
        PropertyInfo eventsProperty = typeof(Component).GetProperty(
            "Events",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw Failure("Could not inspect WinForms event registrations.");
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
            ?? throw Failure("Could not locate the WinForms Click event key.");
        return handlers[clickKey]?.GetInvocationList() ?? Array.Empty<Delegate>();
    }

    private static int CountMethodCalls(MethodInfo caller, MethodBase expected)
    {
        return GetMethodCallOffsets(caller, expected).Count;
    }

    private static IReadOnlyList<int> GetMethodCallOffsets(
        MethodInfo caller,
        MethodBase expected)
    {
        MethodBody body = caller.GetMethodBody()
            ?? throw Failure($"Method '{caller.Name}' has no inspectable body.");
        byte[] il = body.GetILAsByteArray()
            ?? throw Failure($"Method '{caller.Name}' has no inspectable IL.");
        List<int> offsets = new();
        int offset = 0;

        while (offset < il.Length)
        {
            int instructionOffset = offset;
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
                    offsets.Add(instructionOffset);
                }
            }

            offset += operandSize;
        }

        return offsets;
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
            : throw Failure($"Unknown IL opcode 0x{unchecked((ushort)value):X4}.");
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
            _ => throw Failure($"Unsupported IL operand type '{opCode.OperandType}'.")
        };
    }

    private static T GetField<T>(object instance, string fieldName)
        where T : class
    {
        return GetPrivateField(instance.GetType(), fieldName).GetValue(instance) as T
            ?? throw Failure(
                $"Field '{instance.GetType().Name}.{fieldName}' was not a {typeof(T).Name}.");
    }

    private static FieldInfo GetPrivateField(Type type, string fieldName)
    {
        return type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw Failure($"Private field '{type.Name}.{fieldName}' was not found.");
    }

    private static void SetPrivateField(
        object instance,
        string fieldName,
        object? value)
    {
        GetPrivateField(instance.GetType(), fieldName).SetValue(instance, value);
    }

    private static MethodInfo GetPrivateMethod(Type type, string methodName)
    {
        return type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw Failure($"Private method '{type.Name}.{methodName}' was not found.");
    }

    private static object? InvokePrivate(object instance, string methodName)
    {
        return GetPrivateMethod(instance.GetType(), methodName).Invoke(
            instance,
            parameters: null);
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
            throw Failure(
                $"{message} Expected [{string.Join(", ", expectedArray)}], "
                + $"received [{string.Join(", ", actualArray)}].");
        }
    }

    private static void CheckSetEqual(
        IEnumerable<string> expected,
        IEnumerable<string> actual,
        string message)
    {
        HashSet<string> expectedSet = new(expected, StringComparer.Ordinal);
        HashSet<string> actualSet = new(actual, StringComparer.Ordinal);
        if (!expectedSet.SetEquals(actualSet))
        {
            throw Failure(
                $"{message} Expected [{string.Join(", ", expectedSet)}], "
                + $"received [{string.Join(", ", actualSet)}].");
        }
    }

    private static void CheckEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw Failure($"{message} Expected '{expected}', received '{actual}'.");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw Failure(message);
        }
    }

    private static DocumentationLogRegressionAssertionException Failure(
        string message)
    {
        return new DocumentationLogRegressionAssertionException(message);
    }

    private static void EnsureStaThread()
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw Failure(
                "Documentation-log MainForm regression tests must run on the STA harness thread.");
        }
    }

    private static void PumpMessages()
    {
        Application.DoEvents();
    }

    private sealed class MainFormDocumentationFixture : IDisposable
    {
        public MainFormDocumentationFixture()
        {
            Environment = new DocumentationLogSyntheticEnvironment();

            try
            {
                Form = new MainForm();
                SetPrivateField(Form, "documentationLogPaths", Environment.Paths);
                SetPrivateField(
                    Form,
                    "documentationLogFilenameService",
                    new DocumentationLogWorkbookFilenameService(Environment.Paths));
                SetPrivateField(
                    Form,
                    "documentationLogWorkbookService",
                    Environment.WorkbookService);
                SetPrivateField(
                    Form,
                    "documentationLogPreferencesService",
                    Environment.PreferencesService);
                SetPrivateField(
                    Form,
                    "documentationLogSaveService",
                    Environment.CreateSaveService());
                SetPrivateField(
                    Form,
                    "documentationLogHotels",
                    new[]
                    {
                        Environment.Hotel1953,
                        Environment.Hotel2093,
                        Environment.Hotel3001,
                        Environment.HotelLeadingZero
                    });
                SetPrivateField(
                    Form,
                    "documentationLogPmsSystems",
                    new[] { Environment.Mews, Environment.Opera });
                SetPrivateField(Form, "documentationLogInitializationError", null);
                RefreshCurrentLogType();
            }
            catch
            {
                Form?.Dispose();
                Environment.Dispose();
                throw;
            }
        }

        public DocumentationLogSyntheticEnvironment Environment { get; }

        public MainForm Form { get; }

        public void RefreshCurrentLogType()
        {
            InvokePrivate(Form, "HandleSelectedLogTypeChanged");
            PumpMessages();
        }

        public void Dispose()
        {
            Form.Dispose();
            Environment.Dispose();
        }
    }
}
