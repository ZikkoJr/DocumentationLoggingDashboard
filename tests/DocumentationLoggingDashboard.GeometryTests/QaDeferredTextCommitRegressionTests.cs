using System.Diagnostics;
using System.Reflection;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Forms;
using DocumentationLoggingDashboard.QAReports.Forms.Controls;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.GeometryTests;

internal static class QaDeferredTextCommitRegressionTests
{
    public static int RunAll(TextWriter output)
    {
        int failed = 0;

        failed += Run(output, "finding resolution notes defer model/event work", TestFindingNotes);
        failed += Run(output, "finding custom script name defers model/event work", TestCustomScriptName);
        failed += Run(output, "checklist notes defer model/event work", TestChecklistNotes);
        failed += Run(output, "statistics explanation defers model/event work", TestStatisticsExplanation);
        failed += Run(output, "broken-data explanation defers and survives refresh", TestBrokenExplanation);
        failed += Run(output, "report fields defer and flush without findings refresh", TestReportFields);
        failed += Run(output, "multiple finding drafts flush in one refresh batch", TestBatchedFindingFlush);
        failed += Run(output, "status transitions commit or clear drafts once", TestTransitions);

        output.WriteLine(
            failed == 0
                ? "[PASS] All 8 deferred text-commit tests passed."
                : $"[FAIL] {failed} of 8 deferred text-commit tests failed.");
        return failed == 0 ? 0 : 1;
    }

    public static int RunVisibleSmoke(string screenshotDirectory, TextWriter output)
    {
        try
        {
            using QaReportForm form = CreateQaReportForm();
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Size = new Size(1100, 780);
            form.CurrentReport.FileCharacteristics.IsCustomScriptSupportAvailable = true;

            for (int index = 0; index < 5; index++)
            {
                form.CurrentReport.Findings.Add(new QaFinding
                {
                    FindingId = $"visible-deferred-{index}",
                    Severity = index % 2 == 0
                        ? QaFindingSeverity.Warning
                        : QaFindingSeverity.Failure,
                    Resolution = index % 2 == 0
                        ? QaFindingResolution.ExplainedAndAccepted
                        : QaFindingResolution.HandledByCustomScript,
                    Source = QaFindingSource.Manual,
                    Title = $"Visible deferred finding {index}",
                    Description = "Synthetic visible no-save focus and scrolling evidence."
                });
            }

            InvokePrivate(form, "RefreshFindingControls");
            TabControl tabs = DescendantsAndSelf(form)
                .OfType<TabControl>()
                .Single(control => control.Name == "reportTabControl");
            tabs.SelectedTab = tabs.TabPages
                .Cast<TabPage>()
                .Single(page => page.Text == "Findings");
            form.Show();
            PumpMessages();

            QaFindingItemControl findingControl = DescendantsAndSelf(form)
                .OfType<QaFindingItemControl>()
                .Single(control => control.FindingId == "visible-deferred-0");
            ScrollableControl findingPanel = findingControl.Parent as ScrollableControl
                ?? throw new InvalidOperationException("The visible finding panel is not scrollable.");
            findingPanel.ScrollControlIntoView(findingControl);
            PumpMessages();

            TextBox notes = FindTextBox(findingControl, "resolutionNotesTextBox");
            int findingChanged = 0;
            findingControl.FindingChanged += (_, _) => findingChanged++;
            Check(notes.Focus(), "The visible long-note TextBox could not receive focus.");
            notes.SelectionStart = notes.TextLength;
            Point scrollBefore = findingPanel.AutoScrollPosition;
            QaFindingItemControl originalFindingControl = findingControl;
            int findingCountBefore = form.CurrentReport.Findings.Count;
            string text = CreateText(1050)
                + "\r\nSecond synthetic line. DEFERRED-VISIBLE-FINAL-Z";
            Stopwatch timer = Stopwatch.StartNew();

            for (int index = 0; index < text.Length; index++)
            {
                notes.AppendText(text[index].ToString());
                if (index % 100 == 0)
                {
                    PumpMessages();
                }
            }

            PumpMessages();
            timer.Stop();
            Point scrollAfter = findingPanel.AutoScrollPosition;
            QaFinding finding = form.CurrentReport.Findings.Single(candidate =>
                candidate.FindingId == findingControl.FindingId);
            Check(finding.ResolutionNotes is null, "Visible typing committed before a boundary.");
            Check(findingChanged == 0, "Visible typing raised FindingChanged per character.");
            Check(
                form.CurrentReport.Findings.Count == findingCountBefore,
                "Visible typing changed the finding collection.");
            Check(
                !originalFindingControl.IsDisposed,
                "Visible typing disposed the active finding control.");
            Check(notes.Focused, "Visible typing moved focus away from the TextBox.");
            Check(
                notes.SelectionStart == notes.TextLength && notes.SelectionLength == 0,
                "Visible typing moved the caret or text selection.");
            Check(scrollAfter == scrollBefore, "Visible typing moved the findings scroll position.");
            Check(timer.Elapsed < TimeSpan.FromSeconds(5), "Visible 1,000-character typing was not responsive.");

            string destination = Path.Combine(
                Path.GetFullPath(screenshotDirectory),
                "10-deferred-finding-long-note-current-dpi.png");
            using (Bitmap bitmap = new(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save(destination);
            }

            Button readinessButton = DescendantsAndSelf(form)
                .OfType<Button>()
                .Single(button => button.Name == "checkReportReadinessButton");
            readinessButton.PerformClick();
            PumpMessages();
            Check(
                finding.ResolutionNotes == text,
                "Direct readiness did not capture the final visible character.");
            Check(findingChanged == 1, "Direct readiness raised more than one completed finding event.");
            Check(
                form.CurrentReport.Findings.Count == findingCountBefore,
                "Direct readiness changed the synthetic finding count.");

            form.Close();
            PumpMessages();
            string syntheticRoot = GetSyntheticRoot(form);
            Check(
                !Directory.Exists(syntheticRoot),
                $"The visible no-save test created synthetic root '{syntheticRoot}'.");
            output.WriteLine(
                $"[PASS] Visible deferred text smoke at DeviceDpi={form.DeviceDpi}: " +
                $"characters={text.Length}, elapsedMs={timer.Elapsed.TotalMilliseconds:0.0}, " +
                "focus/caret/selection/scroll stable, final text committed at readiness.");
            output.WriteLine($"  Screenshot: {destination}");
            return 0;
        }
        catch (Exception exception)
        {
            output.WriteLine("[FAIL] Visible deferred text smoke");
            output.WriteLine(exception.ToString());
            return 1;
        }
    }

    private static void TestFindingNotes(TextWriter output)
    {
        QaFinding finding = new()
        {
            FindingId = "deferred-warning",
            Severity = QaFindingSeverity.Warning,
            Resolution = QaFindingResolution.ExplainedAndAccepted,
            Title = "Deferred warning",
            Description = "Synthetic regression finding."
        };
        using QaFindingItemControl control = new();
        control.Bind(finding, relatedCheckDisplayName: null, isCustomScriptSupportAvailable: true);
        TextBox notes = FindTextBox(control, "resolutionNotesTextBox");
        int textChanged = 0;
        int modelCommits = 0;
        int findingChanged = 0;
        notes.TextChanged += (_, _) => textChanged++;
        control.FindingChanged += (_, _) => findingChanged++;

        string text = CreateText(250);
        Stopwatch timer = Stopwatch.StartNew();
        AppendOneCharacterAtATime(
            notes,
            text,
            () => finding.ResolutionNotes,
            ref modelCommits);
        timer.Stop();

        output.WriteLine(
            $"  [COUNT] Finding notes before commit: TextChanged={textChanged}, " +
            $"ModelCommits={modelCommits}, FindingChanged={findingChanged}, " +
            $"ElapsedMs={timer.Elapsed.TotalMilliseconds:0.0}");
        Check(modelCommits == 0, "Finding notes changed the model while typing.");
        Check(findingChanged == 0, "Finding notes raised FindingChanged while typing.");

        bool changed = InvokeCommit(control);
        Check(changed, "Finding notes did not report a completed edit.");
        Check(finding.ResolutionNotes == text, "Finding notes did not commit the exact final text.");
        Check(findingChanged == 1, "Finding notes did not raise exactly one completed change event.");

        notes.Text = "Committed by Validated";
        RaiseValidated(notes);
        Check(
            finding.ResolutionNotes == "Committed by Validated",
            "The finding Validated boundary did not commit its draft.");
        Check(findingChanged == 2, "The finding Validated boundary raised a duplicate event.");
    }

    private static void TestCustomScriptName(TextWriter output)
    {
        QaFinding finding = new()
        {
            FindingId = "deferred-script",
            Severity = QaFindingSeverity.Failure,
            Resolution = QaFindingResolution.HandledByCustomScript,
            Title = "Deferred script",
            Description = "Synthetic regression finding."
        };
        using QaFindingItemControl control = new();
        control.Bind(finding, relatedCheckDisplayName: null, isCustomScriptSupportAvailable: true);
        TextBox scriptName = FindTextBox(control, "customScriptNameTextBox");
        int modelCommits = 0;
        int findingChanged = 0;
        control.FindingChanged += (_, _) => findingChanged++;

        string text = CreateText(30);
        AppendOneCharacterAtATime(
            scriptName,
            text,
            () => finding.CustomScriptName,
            ref modelCommits);

        output.WriteLine(
            $"  [COUNT] Script name before commit: TextChanged={text.Length}, " +
            $"ModelCommits={modelCommits}, FindingChanged={findingChanged}");
        Check(modelCommits == 0, "Custom script name changed the model while typing.");
        Check(findingChanged == 0, "Custom script name raised FindingChanged while typing.");

        bool changed = InvokeCommit(control);
        Check(changed, "Custom script name did not report a completed edit.");
        Check(finding.CustomScriptName == text, "Custom script name did not commit exactly.");
        Check(findingChanged == 1, "Custom script name did not raise exactly one completed event.");
    }

    private static void TestChecklistNotes(TextWriter output)
    {
        QaCheckDefinition definition = new()
        {
            Id = "deferred-check",
            DisplayName = "Deferred check",
            Description = "Synthetic regression check.",
            Section = QaChecklistSection.RawFile,
            Applicability = QaCheckApplicability.Always
        };
        QaCheckResult result = new()
        {
            CheckId = definition.Id,
            Status = QaCheckStatus.Pass
        };
        using QaChecklistItemControl control = new();
        control.Bind(definition, result);
        TextBox notes = FindTextBox(control, "notesTextBox");
        int modelCommits = 0;
        int resultChanged = 0;
        control.ResultChanged += (_, _) => resultChanged++;

        string text = CreateText(500);
        Stopwatch timer = Stopwatch.StartNew();
        AppendOneCharacterAtATime(notes, text, () => result.Notes, ref modelCommits);
        timer.Stop();

        output.WriteLine(
            $"  [COUNT] Checklist notes before commit: TextChanged={text.Length}, " +
            $"ModelCommits={modelCommits}, ResultChanged={resultChanged}, " +
            $"ElapsedMs={timer.Elapsed.TotalMilliseconds:0.0}");
        Check(modelCommits == 0, "Checklist notes changed the model while typing.");
        Check(resultChanged == 0, "Checklist notes raised ResultChanged while typing.");

        bool changed = InvokeCommit(control);
        Check(changed, "Checklist notes did not report a completed edit.");
        Check(result.Notes == text, "Checklist notes did not commit the exact final text.");
        Check(resultChanged == 1, "Checklist notes did not raise exactly one completed event.");
    }

    private static void TestStatisticsExplanation(TextWriter output)
    {
        QaReport report = new();
        report.FileCharacteristics.MonetaryColumnScenario =
            QaMonetaryColumnScenario.TwoMonetaryColumns;
        report.Statistics.UnusualAverageRateValues.HasUnusualValues = true;
        report.Statistics.UnusualStayValues.HasUnusualValues = true;
        report.Statistics.UnusualStayValues.UnusualValueCount = 1;
        report.Statistics.HighStayValues.StayValuesAboveTenThousandCount = 1;
        report.Statistics.HighStayValues.AreHighValuesExpected = false;
        report.Statistics.FileInformation.TotalDataRows = 20;
        report.Statistics.Database.RejectedRecordCount = 1;
        QaCheckResult rejectedResult = new()
        {
            CheckId = QaChecklistIds.Database.RejectedRecordsAccountedFor,
            Status = QaCheckStatus.Pass
        };
        report.ChecklistResults.Add(rejectedResult);
        QaFinding rowDifferenceFinding = new()
        {
            FindingId = QaFindingIds.RowDifferenceStatisticWarning,
            Severity = QaFindingSeverity.Warning,
            Resolution = QaFindingResolution.Active,
            Title = "Row difference",
            Description = "Synthetic row difference."
        };
        report.Findings.Add(rowDifferenceFinding);
        using QaStatisticsControl control = new();
        control.Bind(report);
        TextBox explanation = FindTextBox(control, "unusualAverageRateExplanationTextBox");
        int modelCommits = 0;
        int statisticsChanged = 0;
        control.StatisticsChanged += (_, _) => statisticsChanged++;

        string text = CreateText(500);
        Stopwatch timer = Stopwatch.StartNew();
        AppendOneCharacterAtATime(
            explanation,
            text,
            () => report.Statistics.UnusualAverageRateValues.Explanation,
            ref modelCommits);
        timer.Stop();

        output.WriteLine(
            $"  [COUNT] Statistics explanation before commit: TextChanged={text.Length}, " +
            $"ModelCommits={modelCommits}, StatisticsChanged={statisticsChanged}, " +
            $"ElapsedMs={timer.Elapsed.TotalMilliseconds:0.0}");
        Check(modelCommits == 0, "Statistics explanation changed the model while typing.");
        Check(statisticsChanged == 0, "Statistics explanation raised StatisticsChanged while typing.");

        bool changed = InvokeCommit(control);
        Check(changed, "Statistics explanation did not report a completed edit.");
        Check(
            report.Statistics.UnusualAverageRateValues.Explanation == text,
            "Statistics explanation did not commit the exact final text.");
        Check(
            statisticsChanged == 1,
            "Statistics explanation did not raise exactly one completed event.");

        Dictionary<string, string> otherTopLevelDrafts = new(StringComparer.Ordinal)
        {
            ["unusualStayValueExplanationTextBox"] = "Unusual stay explanation",
            ["highStayValueExplanationTextBox"] = "High stay explanation",
            ["rowDifferenceExplanationTextBox"] = "Row difference explanation",
            ["rejectedRecordsExplanationTextBox"] = "Rejected records explanation"
        };
        foreach ((string name, string draft) in otherTopLevelDrafts)
        {
            FindTextBox(control, name).Text = draft;
        }

        Dictionary<string, string> brokenDrafts = new(StringComparer.Ordinal);
        foreach (QaBrokenDataStatisticRowControl row in DescendantsAndSelf(control)
                     .OfType<QaBrokenDataStatisticRowControl>())
        {
            string draft = "Broken explanation for " + row.FieldId;
            DescendantsAndSelf(row).OfType<TextBox>().Single(textBox => textBox.Multiline).Text = draft;
            brokenDrafts.Add(row.FieldId, draft);
        }

        changed = control.CommitPendingTextEdits();
        Check(changed, "The multi-explanation statistics batch reported no change.");
        Check(statisticsChanged == 2, "A multi-explanation batch raised more than one additional event.");
        Check(
            report.Statistics.UnusualStayValues.Explanation == otherTopLevelDrafts["unusualStayValueExplanationTextBox"],
            "Unusual Stay Value explanation was not committed.");
        Check(
            report.Statistics.HighStayValues.Explanation == otherTopLevelDrafts["highStayValueExplanationTextBox"],
            "High Stay Value explanation was not committed.");
        Check(
            rowDifferenceFinding.ResolutionNotes == otherTopLevelDrafts["rowDifferenceExplanationTextBox"],
            "Row-difference explanation was not committed.");
        Check(
            rejectedResult.Notes == otherTopLevelDrafts["rejectedRecordsExplanationTextBox"],
            "Rejected-record explanation was not committed.");
        foreach ((string fieldId, string draft) in brokenDrafts)
        {
            QaBrokenDataStatistic statistic = report.Statistics.BrokenData.Single(
                candidate => candidate.FieldId == fieldId);
            Check(statistic.Explanation == draft, $"Broken explanation '{fieldId}' was not committed.");
        }

        output.WriteLine(
            $"  [COUNT] Statistics multi-field flush: TopLevel={otherTopLevelDrafts.Count}, " +
            $"BrokenRows={brokenDrafts.Count}, StatisticsChanged=1.");
    }

    private static void TestBrokenExplanation(TextWriter output)
    {
        QaBrokenDataStatistic statistic = new()
        {
            FieldId = "deferred-broken",
            DisplayName = "Deferred Broken Data"
        };
        using QaBrokenDataStatisticRowControl control = new();
        control.Bind(statistic);
        TextBox explanation = DescendantsAndSelf(control)
            .OfType<TextBox>()
            .Single(textBox => textBox.Multiline);
        int changedEvents = 0;
        control.StatisticChanged += (_, _) => changedEvents++;
        string draft = "  " + CreateText(500) + "  ";

        explanation.Text = draft;
        control.RefreshFromStatistic();
        Check(explanation.Text == draft, "A same-row refresh overwrote a dirty Broken Data draft.");
        Check(statistic.Explanation is null, "Broken Data explanation committed before validation.");

        bool changed = control.CommitPendingTextEdits();
        Check(changed, "Broken Data explanation did not report a completed edit.");
        Check(
            statistic.Explanation == draft.Trim(),
            "Broken Data explanation did not apply Trim-to-null normalization.");
        Check(changedEvents == 1, "Broken Data explanation did not raise one completed event.");
    }

    private static void TestReportFields(TextWriter output)
    {
        using QaReportForm form = CreateQaReportForm();
        TextBox createdBy = FindTextBox(form, "createdByTextBox");
        TextBox originalFileName = FindTextBox(form, "originalFileNameTextBox");
        TextBox generalNotes = FindTextBox(form, "generalNotesTextBox");
        form.CurrentReport.ReportStatus = QaReportStatus.Pass;
        QaFinding[] findingsBefore = form.CurrentReport.Findings.ToArray();

        string notes = CreateText(999) + "Z";
        int notesCommits = 0;
        AppendOneCharacterAtATime(
            generalNotes,
            notes,
            () => form.CurrentReport.GeneralNotes,
            ref notesCommits);
        createdBy.Text = "Deferred QA Person";
        originalFileName.Text = "deferred-input.csv";

        Check(notesCommits == 0, "General Notes changed the report model while typing.");
        Check(form.CurrentReport.CreatedBy is null, "Created By committed before the boundary.");
        Check(form.CurrentReport.OriginalFileName is null, "Original File Name committed early.");
        Check(form.CurrentReport.ReportStatus is null, "A draft report edit did not stale readiness.");

        bool changed = form.CommitAllPendingTextEdits();
        Check(changed, "The form did not report committed report-detail drafts.");
        Check(form.CurrentReport.GeneralNotes == notes, "General Notes lost the final character.");
        Check(form.CurrentReport.CreatedBy == "Deferred QA Person", "Created By was not committed.");
        Check(
            form.CurrentReport.OriginalFileName == "deferred-input.csv",
            "Original File Name was not committed.");
        Check(
            form.CurrentReport.Findings.SequenceEqual(findingsBefore),
            "Report-only text changed the finding collection.");

        createdBy.Text = "   ";
        _ = form.CommitAllPendingTextEdits();
        Check(form.CurrentReport.CreatedBy is null, "Whitespace-only Created By did not normalize to null.");
        Check(
            form.EffectiveCreatedBy == QaReportValidationService.DefaultEffectiveCreatedBy,
            "The Created By fallback changed.");

        output.WriteLine(
            $"  [COUNT] Report details: TextChanged={notes.Length + 3}, " +
            "ModelCommits=3, findings unchanged, readiness stale before the action flush.");
    }

    private static void TestBatchedFindingFlush(TextWriter output)
    {
        using QaReportForm form = CreateQaReportForm();
        form.CurrentReport.FileCharacteristics.IsCustomScriptSupportAvailable = true;
        List<QaFinding> findings = [];

        for (int index = 0; index < 5; index++)
        {
            QaFinding finding = new()
            {
                FindingId = $"deferred-batch-{index}",
                Severity = index % 2 == 0
                    ? QaFindingSeverity.Warning
                    : QaFindingSeverity.Failure,
                Resolution = index % 2 == 0
                    ? QaFindingResolution.ExplainedAndAccepted
                    : QaFindingResolution.HandledByCustomScript,
                Source = QaFindingSource.Manual,
                Title = $"Deferred batch {index}",
                Description = "Synthetic batching finding."
            };
            findings.Add(finding);
            form.CurrentReport.Findings.Add(finding);
        }

        InvokePrivate(form, "RefreshFindingControls");
        QaFindingItemControl[] controls = DescendantsAndSelf(form)
            .OfType<QaFindingItemControl>()
            .Where(control => control.FindingId.StartsWith("deferred-batch-", StringComparison.Ordinal))
            .OrderBy(control => control.FindingId, StringComparer.Ordinal)
            .ToArray();
        Check(controls.Length == findings.Count, "The synthetic finding controls were not created.");

        int findingChanged = 0;
        foreach (QaFindingItemControl control in controls)
        {
            control.FindingChanged += (_, _) => findingChanged++;
            FindTextBox(control, "resolutionNotesTextBox").Text =
                "Notes for " + control.FindingId;
        }

        bool changed = form.CommitAllPendingTextEdits();

        Check(changed, "The batched finding flush reported no change.");
        Check(findingChanged == findings.Count, "Each changed finding did not raise exactly one event.");

        foreach (QaFinding finding in findings)
        {
            Check(
                finding.ResolutionNotes == "Notes for " + finding.FindingId,
                $"Draft text moved away from finding '{finding.FindingId}'.");
            Check(
                form.CurrentReport.Findings.Count(candidate =>
                    candidate.FindingId == finding.FindingId) == 1,
                $"Finding '{finding.FindingId}' was removed or duplicated.");
        }

        output.WriteLine(
            $"  [COUNT] Five-field action flush: ModelCommits={findings.Count}, " +
            $"FindingChanged={findingChanged}, finding identities preserved, no duplicates.");
    }

    private static void TestTransitions(TextWriter output)
    {
        QaFinding finding = new()
        {
            FindingId = "deferred-transition-finding",
            Severity = QaFindingSeverity.Warning,
            Resolution = QaFindingResolution.ExplainedAndAccepted,
            Title = "Transition finding",
            Description = "Synthetic transition finding."
        };
        using QaFindingItemControl findingControl = new();
        findingControl.Bind(finding, null, true);
        int findingChanged = 0;
        findingControl.FindingChanged += (_, _) => findingChanged++;
        FindTextBox(findingControl, "resolutionNotesTextBox").Text = "Discarded by Active";
        FindRadioButton(findingControl, "activeRadioButton").Checked = true;
        Check(finding.Resolution == QaFindingResolution.Active, "Finding resolution did not change.");
        Check(finding.ResolutionNotes is null, "Active resolution did not apply the approved note clearing rule.");
        Check(findingChanged == 1, "Finding transition raised more than one logical event.");

        QaCheckDefinition definition = new()
        {
            Id = "deferred-transition-check",
            DisplayName = "Transition check",
            Description = "Synthetic transition check.",
            Section = QaChecklistSection.RawFile,
            Applicability = QaCheckApplicability.Always
        };
        QaCheckResult result = new()
        {
            CheckId = definition.Id,
            Status = QaCheckStatus.Pass
        };
        using QaChecklistItemControl checklistControl = new();
        checklistControl.Bind(definition, result);
        int resultChanged = 0;
        checklistControl.ResultChanged += (_, _) => resultChanged++;
        FindTextBox(checklistControl, "notesTextBox").Text = "Preserved on Fail";
        FindRadioButton(checklistControl, "failRadioButton").Checked = true;
        Check(result.Status == QaCheckStatus.Fail, "Checklist status did not change.");
        Check(result.Notes == "Preserved on Fail", "Checklist status change lost the pending note.");
        Check(resultChanged == 1, "Checklist status transition raised more than one event.");

        FindTextBox(checklistControl, "notesTextBox").Text = "Cleared when inapplicable";
        checklistControl.SetApplicable(false);
        Check(result.Status == QaCheckStatus.NotApplicable, "Applicability did not update status.");
        Check(result.Notes is null, "Inapplicability did not apply the approved note clearing rule.");
        Check(resultChanged == 2, "Applicability transition raised more than one additional event.");
    }

    private static void AppendOneCharacterAtATime(
        TextBox textBox,
        string value,
        Func<string?> readModelValue,
        ref int modelCommitCount)
    {
        string? previousModelValue = readModelValue();

        foreach (char character in value)
        {
            textBox.AppendText(character.ToString());
            string? currentModelValue = readModelValue();
            if (!string.Equals(previousModelValue, currentModelValue, StringComparison.Ordinal))
            {
                modelCommitCount++;
                previousModelValue = currentModelValue;
            }
        }
    }

    private static bool InvokeCommit(object target)
    {
        MethodInfo? method = target.GetType().GetMethod(
            "CommitPendingTextEdits",
            BindingFlags.Instance | BindingFlags.Public);
        Check(method is not null, $"{target.GetType().Name} has no public CommitPendingTextEdits method.");
        object? result = method!.Invoke(target, null);
        return result is bool changed && changed;
    }

    private static TextBox FindTextBox(Control root, string name)
    {
        return DescendantsAndSelf(root)
            .OfType<TextBox>()
            .SingleOrDefault(control => control.Name == name)
            ?? throw new InvalidOperationException($"TextBox '{name}' was not found.");
    }

    private static RadioButton FindRadioButton(Control root, string name)
    {
        return DescendantsAndSelf(root)
            .OfType<RadioButton>()
            .SingleOrDefault(control => control.Name == name)
            ?? throw new InvalidOperationException($"RadioButton '{name}' was not found.");
    }

    private static QaReportForm CreateQaReportForm()
    {
        string syntheticRoot = Path.Combine(
            Path.GetTempPath(),
            "DocumentationLoggingDashboard.DeferredTextTests",
            $"no-save-{Guid.NewGuid():N}");
        QaStoragePaths paths = new(syntheticRoot);
        QaMetadataService metadataService = new(paths, new QaFolderNameSanitizer());
        return new QaReportForm(
            metadataService,
            Array.Empty<QaPmsMetadata>(),
            Array.Empty<QaHotelMetadata>(),
            paths)
        {
            ShowInTaskbar = false
        };
    }

    private static string GetSyntheticRoot(QaReportForm form)
    {
        FieldInfo? saveServiceField = typeof(QaReportForm).GetField(
            "reportSaveService",
            BindingFlags.Instance | BindingFlags.NonPublic);
        object saveService = saveServiceField?.GetValue(form)
            ?? throw new InvalidOperationException("The QA report save service was not found.");
        FieldInfo? pathsField = saveService.GetType().GetField(
            "paths",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return pathsField?.GetValue(saveService) is QaStoragePaths paths
            ? paths.DocumentationRootPath
            : throw new InvalidOperationException("The synthetic QA paths were not found.");
    }

    private static void PumpMessages()
    {
        Application.DoEvents();
        Thread.Sleep(10);
        Application.DoEvents();
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo? method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(method is not null, $"Private method '{methodName}' was not found.");
        _ = method!.Invoke(target, null);
    }

    private static void RaiseValidated(Control control)
    {
        MethodInfo? method = typeof(Control).GetMethod(
            "OnValidated",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(method is not null, "Control.OnValidated was not found.");
        _ = method!.Invoke(control, [EventArgs.Empty]);
    }

    private static IEnumerable<Control> DescendantsAndSelf(Control root)
    {
        yield return root;

        foreach (Control child in root.Controls)
        {
            foreach (Control descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }

    private static string CreateText(int length)
    {
        const string source = "Deferred text 0123456789 ABC xyz. ";
        return string.Concat(Enumerable.Repeat(source, (length / source.Length) + 1))[..length];
    }

    private static int Run(TextWriter output, string name, Action<TextWriter> test)
    {
        try
        {
            test(output);
            output.WriteLine($"[PASS] {name}");
            return 0;
        }
        catch (Exception exception)
        {
            output.WriteLine($"[FAIL] {name}");
            output.WriteLine(exception.ToString());
            return 1;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
