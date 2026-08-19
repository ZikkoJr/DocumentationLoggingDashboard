# Quick QA / Surface QA Test Matrix

## Use of this matrix

This matrix records the final verification evidence for the post-pilot
Detailed/Quick QA update on 2026-08-19. Status values are actual results:
`Pass`, `Fail`, `Blocked`, or `Not Run`. `Blocked` identifies an environment
constraint; `Not Run` identifies wording that was not fully exercised. A green
suite is not treated as evidence for requirements that the suite does not
assert. The console harness reports named scenarios, not an instrumented count
of every individual assertion.

Verification used only synthetic Hotel/report data under unique
system-temporary fixture roots. The supplied workbook was inspected read-only
and was neither copied nor modified. No generated `.xlsx`/PDF, `bin`, `obj`, or
publish output was added to Git, and pre-existing ignored publish/runtime
content was preserved.

## Commands and recorded outcomes

From the repository root:

```powershell
dotnet build DocumentationLoggingDashboard.sln --configuration Debug
dotnet run --project tests\DocumentationLoggingDashboard.GeometryTests\DocumentationLoggingDashboard.GeometryTests.csproj --configuration Debug --no-build --
dotnet build DocumentationLoggingDashboard.sln --configuration Release
dotnet run --project tests\DocumentationLoggingDashboard.GeometryTests\DocumentationLoggingDashboard.GeometryTests.csproj --configuration Release --no-build --
# Intended online publish (rejected before execution because restore approval was unavailable):
dotnet publish .\DocumentationLoggingDashboard\DocumentationLoggingDashboard.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true --output <new-empty-temp-directory>
# Executed offline fallback (failed NETSDK1047 before output generation):
dotnet publish .\DocumentationLoggingDashboard\DocumentationLoggingDashboard.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true --output <new-empty-temp-directory> --no-restore
git status --short
```

The regression project is a console harness, not a `dotnet test` test-adapter
project. Supported focused routes include `--geometry-only`, `--semantic-only`,
`--arrival-month-only`, `--deferred-only`, `--v1-only`, and bounded visible
smoke/evidence paths described by the harness `--help` output. Those routes are
capabilities, not evidence that each focused route ran separately; the final
record below identifies the commands actually executed.

## Baseline and repository gates

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| BASE-001 | Fetch and inspect remote | `origin/v2-qa-reports` is explicitly fetched and exact approved baseline is recorded | Pass |
| BASE-002 | Branch/tag identity | Work occurs on update branch from approved V2 commit; `main` remains V1; `v1.0.0` is unchanged | Pass |
| BASE-003 | Worktree ownership | Existing user/concurrent changes are identified and preserved before edits | Pass |
| BASE-004 | Baseline Debug build | Pre-implementation baseline exit 0 and warning/error counts recorded | Not Run |
| BASE-005 | Baseline full console harness | Pre-implementation baseline exit 0 and existing suites recorded | Not Run |
| BASE-006 | No prohibited Git action | No merge, rebase, tag mutation, branch deletion, or other unrequested Git action; final commit/push only | Pass |

## MainForm and workflow boundaries

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| UI-001 | `Detailed QA Report` action | Opens the existing Detailed workflow | Blocked |
| UI-002 | `Quick QA` action | Opens the dedicated Quick form | Blocked |
| UI-003 | Handler inspection | Each action fires once; no duplicate handler/bootstrap path | Pass |
| UI-004 | V1 actions | Preview, submit, open, folder, and settings actions remain functional | Blocked |
| UI-005 | Quick form shape | One scrollable page with Report Details, Raw/DB headers, custom-script controls, Result, Summary, and save | Not Run |
| UI-006 | Quick omissions | No Statistics/File Characteristics tabs, Detailed wizard, PDF controls, Created By, Original Filename, or QA Date input | Not Run |

## Detailed File ID and index

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| DET-ID-001 | Blank File ID | Readiness/save blocked | Not Run |
| DET-ID-002 | Whitespace-only File ID | Readiness/save blocked | Pass |
| DET-ID-003 | Outer whitespace | Stored/displayed value is trimmed | Pass |
| DET-ID-004 | `001234` | Remains exact text `001234` | Pass |
| DET-ID-005 | Readiness edit | File ID participates in fingerprint/staleness | Pass |
| DET-ID-006 | PDF | Schema-2 report information displays File ID | Pass |
| DET-ID-007 | Index serialize/load | New entry contains and reloads `File ID:` | Pass |
| DET-ID-008 | Filename | Existing Detailed filename remains unchanged | Pass |
| DET-ID-009 | Logical key | Hotel ID + File Month overwrite identity remains unchanged | Pass |
| DET-ID-010 | Schema | New rendered report schema is 2; old PDF remains schema 1 | Not Run |
| DET-ID-011 | Historical index only | Existing 13-line entries load without File ID | Pass |
| DET-ID-012 | New index only | Label-based schema-2 entries load | Pass |
| DET-ID-013 | Mixed index | Old and new entries load together | Pass |
| DET-ID-014 | No migration | Loading/saving unrelated report does not add File ID to legacy entry | Pass |
| DET-ID-015 | Replacement | Replacing matching logical report updates that entry with new File ID | Not Run |
| DET-ID-016 | Malformed/duplicate labels | Safely rejected; no partial rewrite | Not Run |

## Detailed Email

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| DET-EMAIL-001 | New catalog | `RAW.EMAIL.COLUMN_AVAILABLE` exists once; `RAW.REQUIRED.EMAIL_PRESENT` is absent | Pass |
| DET-EMAIL-002 | Available | Pass; no generated finding | Pass |
| DET-EMAIL-003 | Unavailable | Exactly one Warning; no Failure or generic duplicate | Pass |
| DET-EMAIL-004 | Email unavailable alone | Overall `Pass with Warnings` | Pass |
| DET-EMAIL-005 | Historical old ID | Old artifact remains readable; no migration | Not Run |

## Strategy matrices (run independently for Detailed and Quick)

Detailed uses `WARN:STRATEGY:SOURCE_RATE_MARKET` and
`FAIL:STRATEGY:SOURCE_RATE_MARKET`; Quick uses
`QUICK:WARN:STRATEGY:SOURCE_RATE_MARKET` and
`QUICK:FAIL:STRATEGY:SOURCE_RATE_MARKET`.

| ID suffix | Source | Rate | Market | Expected | Detailed | Quick |
| --- | :---: | :---: | :---: | --- | --- | --- |
| 000 | Available | Available | Available | No strategy finding | Pass | Pass |
| 100 | Missing | Available | Available | Exactly one Strategy Warning naming Source | Pass | Pass |
| 010 | Available | Missing | Available | Exactly one Strategy Warning naming Rate | Pass | Pass |
| 001 | Available | Available | Missing | Exactly one Strategy Warning naming Market | Pass | Pass |
| 110 | Missing | Missing | Available | Exactly one Strategy Warning naming Source and Rate | Pass | Pass |
| 101 | Missing | Available | Missing | Exactly one Strategy Warning naming Source and Market | Pass | Pass |
| 011 | Available | Missing | Missing | Exactly one Strategy Warning naming Rate and Market | Pass | Pass |
| 111 | Missing | Missing | Missing | Exactly one aggregate Failure; no Strategy Warning | Pass | Pass |

For each of `DET-STRAT-*` and `QUICK-STRAT-*`, the executed harness asserted
exact missing categories, deterministic ID, no normal Warning for one/two
missing, no individual Failure, no duplicate finding, and correct overall
Result. The separate alias qualification claim (every approved alias qualifies
and `Average Rate` does not) was **Not Run** because no current test exercises
those header aliases.

## Detailed Arrival and Statistics regressions

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| DET-ARR-001 | Catalog/form/PDF | Retired Arrival checklist row is absent with no empty row | Not Run |
| DET-ARR-002 | No valid applicable dates | Existing applicability preserved; no threshold Failure | Pass |
| DET-ARR-003 | Below 30% | No Arrival threshold finding | Pass |
| DET-ARR-004 | Exactly 30% / `3/10` | No Arrival threshold finding | Pass |
| DET-ARR-005 | Above 30% / `4/13` | Exactly one statistics Failure with `STAT:FAIL:ARRIVAL_OUTSIDE_FILE_MONTH` | Pass |
| DET-ARR-006 | Count changes | Finding appears, refreshes, disappears, and recurs without duplication | Pass |
| DET-STAT-001 | Total Data Rows | Existing propagation remains correct | Pass |
| DET-STAT-002 | Blank denominator | Auto/manual/reset behavior remains correct | Pass |
| DET-STAT-003 | Broken denominator | Uses applicable nonblank denominator; override/reset remains correct | Pass |
| DET-STAT-004 | Blank thresholds | 0, Warning range, and Failure range retain approved behavior | Pass |
| DET-STAT-005 | Broken thresholds | 0, Warning range, and Failure range retain approved behavior | Pass |
| DET-STAT-006 | Blank vs Broken | Blank is not Broken | Pass |
| DET-STAT-007 | Name checks | Blank names do not fail populated-name checks | Pass |

## Quick catalog and domain

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| QUICK-CAT-001 | Catalog identity | Exactly 21 stable IDs, each once, in documented order | Pass |
| QUICK-CAT-002 | Section count | 14 Raw followed by 7 Database rows | Pass |
| QUICK-CAT-003 | N/A definitions | Only currency, confirmation candidates, rejected records, and DB Email allow N/A | Pass |
| QUICK-CAT-004 | Invalid N/A | Validation rejects N/A on every other row | Not Run |
| QUICK-CAT-005 | No Quick extras | No email-presence, Arrival Month, Full/First/Last split, detailed monetary spot checks, Statistics, or File Characteristics | Pass |
| QUICK-CAT-006 | Separate status type | Direct Warning/N/A works without changing Detailed enum | Not Run |
| QUICK-NAME-001 | Separate names available | Raw Names may Pass | Pass |
| QUICK-NAME-002 | Full Name available | Raw Names may Pass | Pass |
| QUICK-NAME-003 | No usable name source | Appropriate non-pass result/finding can be recorded | Pass |
| QUICK-NAME-004 | DB correct/incorrect | Correct mapping Passes; incorrect mapping produces selected outcome | Pass |
| QUICK-MONEY-001 | One usable monetary type | Raw availability Passes | Pass |
| QUICK-MONEY-002 | Multiple usable types | Raw availability Passes without Detailed branching UI | Pass |
| QUICK-MONEY-003 | DB mapping | Correct actual type Passes; incorrect type records finding | Not Run |

## Quick custom-script and Result

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| QUICK-CS-001 | Available = No | Handled state is prevented/cleared/rejected | Pass |
| QUICK-CS-002 | Available = Yes | Each Warning/Failure may be handled independently | Not Run |
| QUICK-CS-003 | Warning handled | Severity retained; status remains `Pass with Warnings` | Not Run |
| QUICK-CS-004 | Failure handled | Severity retained; no longer Active | Pass |
| QUICK-CS-005 | Multiple Failures | Handling one does not affect another | Pass |
| QUICK-CS-006 | One Active Failure | Overall `Fail` | Pass |
| QUICK-CS-007 | All Failures handled | No Active Failure; `Pass with Warnings` | Pass |
| QUICK-STATUS-001 | Clean | `Pass` | Pass |
| QUICK-STATUS-002 | Ordinary Warning | `Pass with Warnings` | Pass |
| QUICK-STATUS-003 | Strategy Warning | `Pass with Warnings` | Pass |
| QUICK-STATUS-004 | Active Failure | `Fail` and save remains allowed when otherwise valid | Not Run |
| QUICK-STATUS-005 | Status tampering/staleness | Validation rejects mismatch with current findings | Pass |

## Summary

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| SUM-001 | Clean report | Exact `Raw file and Database QA passed successfully` | Pass |
| SUM-002 | Findings | One concise sentence per finding, one line per sentence | Pass |
| SUM-003 | Multiple findings | Multiline combined Summary | Pass |
| SUM-004 | Strategy Warning | Sentence visibly says `Strategy Warning` | Pass |
| SUM-005 | Current manual edit | Saves exact edited current text | Not Run |
| SUM-006 | Edit then QA change | Text preserved but visibly marked stale | Pass |
| SUM-007 | Stale save | Blocked | Pass |
| SUM-008 | Regenerate | Current generated text/fingerprint restored | Pass |
| SUM-009 | Edit after regenerate | New current Summary remains user-editable | Pass |
| SUM-010 | Blank non-clean Summary | Blocked | Not Run |
| SUM-011 | Privacy | No guest-level examples or copied rows generated | Not Run |

## New Surface workbook and filename safety

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| NEW-001 | Custom filename with spaces | Accepted, e.g. `Surface QA August.xlsx` | Pass |
| NEW-002 | Missing extension | `.xlsx` appended | Pass |
| NEW-003 | Invalid characters | Predictably sanitized or rejected per service contract | Pass |
| NEW-004 | Rooted/traversal/separators | Rejected | Pass |
| NEW-005 | `.` / `..` | Rejected | Pass |
| NEW-006 | Reserved device name | Rejected | Pass |
| NEW-007 | Trailing dot/space and length | Safely normalized/limited | Not Run |
| NEW-008 | Other extension | Cannot escape `.xlsx` contract | Pass |
| NEW-009 | Existing filename | Never overwritten; compatible file may be selected | Pass |
| NEW-010 | Root containment | Resolved file is a direct child of `SurfaceQA` | Pass |
| NEW-011 | Workbook structure | `Surface QA`, exact headers, `SurfaceQaTable`, filter, row-1 freeze, wrapped Summary | Pass |
| NEW-012 | Formatting | Compact readable widths, no merged data cells or hidden required data | Not Run |
| NEW-013 | Selection/preference | New workbook becomes selected and last-used | Not Run |

## Existing and legacy Surface workbooks

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| SURF-001 | Canonical load/append | Prior rows preserved; table/filter expands; exact row appended | Pass |
| SURF-002 | Formatting preservation | Existing usable client formatting remains usable | Pass |
| SURF-003 | Sample aliases | Exact seven approved legacy aliases accepted after trim/case normalization | Not Run |
| SURF-004 | Legacy canonicalization | Only seven headers canonicalized in staged copy | Pass |
| SURF-005 | Legacy old data | Row order/values/results remain unchanged | Pass |
| SURF-006 | Legacy structure repair | Table/filter/freeze/wrap established; new row appended | Pass |
| SURF-007 | Missing header | Clear failure; source byte/content state unchanged | Not Run |
| SURF-008 | Wrong/near-match header | Clear failure; no rewrite | Not Run |
| SURF-009 | Duplicate/ambiguous/reordered/extra schema | Clear failure; no rewrite | Not Run |
| SURF-010 | Corrupt `.xlsx` | Clear failure; no rewrite | Pass |
| SURF-011 | Unsupported extension metadata | Visible business data/style preserved; any dropped custom/coauthor parts assessed and recorded | Not Run |

Read-only supplied-sample observations used by fixture design:

- one sheet `Sheet1`, range `A1:G30`, 29 data rows;
- headers normalize to `Month 2026 | Hotel Name | Hotel Id | PMS | file id |
  Pass/Fail/Warning | summary`;
- no table, filter, freeze, formula, validation, conditional formatting, or
  Summary wrap;
- Hotel/File IDs were historically numeric; synthetic regression rows asserted
  new IDs as text;
- Month/Result case and whitespace varied; legacy-preservation coverage did not
  normalize old rows;
- an Excel `~$` owner file was present during inspection; this was not evidence
  of real OS-lock handling;
- automated fixtures did not exercise custom, coauthor, or other unsupported
  Open XML extension parts; `SURF-011` remains an explicit preservation risk.

## Last-used workbook and exact Surface row

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| PREF-001 | Save/select A then reopen | A selected | Not Run |
| PREF-002 | Switch/save B then reopen | B selected | Not Run |
| PREF-003 | Remembered B renamed/deleted | Safe compatible fallback or explicit selection; no crash | Not Run |
| PREF-004 | Preference payload | Stores filename only and writes atomically | Pass |
| PREF-005 | Enumeration | Ignores `~$`, temp, staging, rollback, and non-direct-child files | Not Run |
| ROW-001 | File Month | Exact text `yyyy-MM` | Pass |
| ROW-002 | Hotel Name/ID/PMS | Exact canonical metadata; Hotel ID text | Pass |
| ROW-003 | File ID | Exact text preserving leading zeroes | Pass |
| ROW-004 | Result/Summary | Exact current final values | Not Run |
| ROW-005 | Timestamp privacy | No QA Timestamp in client workbook | Pass |

## Hotel history

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| HIST-001 | First Quick QA | Creates canonical Hotel `QuickQAHistory.xlsx` | Pass |
| HIST-002 | Structure | Sheet `Quick QA History`, table `QuickQaHistoryTable`, exact 8 columns | Pass |
| HIST-003 | Timestamp | Round-trip ISO-8601 text includes offset; injectable clock used once | Pass |
| HIST-004 | Event identity | Same month/hotel/PMS/File ID/Result/Summary as client row | Pass |
| HIST-005 | Second/later QA | Appends another row; no permanent deduplication | Pass |
| HIST-006 | Boundary | No PMS history, PDF, or Detailed index update | Not Run |

## Transaction, locks, and reentrancy

The transaction suite asserted no false success, no normal one-sided event,
restoration where the injected path exercised rollback, and staging/rollback
cleanup. It did not exercise a real OS lock or displayed friendly message, the
literal history-created-then-client-fails ordering, a true rapid double-click,
or rollback failure requiring manual review.

| ID | Injected case | Expected | Status |
| --- | --- | --- | --- |
| TXN-001 | Surface workbook locked | Friendly Surface message; neither logical event committed | Not Run |
| TXN-002 | Existing Hotel history locked | Friendly history message; neither logical event committed | Not Run |
| TXN-003 | Surface staging failure | No destination commit | Pass |
| TXN-004 | History staging failure | No destination commit | Pass |
| TXN-005 | First commit succeeds, second fails | First destination rolled back | Pass |
| TXN-006 | New history then client failure | New history removed | Not Run |
| TXN-007 | Existing destination rollback | Original bytes restored | Pass |
| TXN-008 | Source changes between read/commit | Fingerprint mismatch aborts without overwrite | Pass |
| TXN-009 | Staged validation mismatch | Commit blocked | Pass |
| TXN-010A | Final verification mismatch | No success; automatic recovery state reported safely | Pass |
| TXN-010B | Recovery failure | Manual-review state and locations are reported safely | Not Run |
| TXN-011 | Rapid double click | Exactly one Surface row and one history row | Not Run |
| TXN-012 | Legitimate later repeat | Another row appends to both | Pass |

## Validation coverage

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| VAL-001 | Automated pre-save validation | Representative metadata/PMS mismatch, File Month/File ID, result/N/A/strategy, custom-script, finding/status, stale Summary, workbook/path/schema cases are rejected | Pass |
| VAL-002 | Warning saveability | Ordinary and Strategy Warnings do not block otherwise valid service-level saves | Not Run |
| VAL-003 | Final Fail saveability | An otherwise valid final `Fail` remains saveable | Not Run |
| VAL-004 | Blank non-clean Summary | A non-clean report with a blank Summary is rejected | Not Run |
| VAL-005 | Success confirmation ordering | UI confirmation includes Hotel, File ID, Result, Surface workbook, and history update before close/reset | Blocked |

`VAL-001` is Pass only for the named representative cases executed by the core,
filename/preferences, and paired-save suites; it is not exhaustive coverage of
every malformed input class listed in its compound wording.

## Detailed and V1 regression

| ID | Area | Expected | Status |
| --- | --- | --- | --- |
| REG-DET-001 | Detailed form/readiness/findings | Existing behavior retained except approved changes | Not Run |
| REG-DET-002 | Detailed PDF/paired save/index | Byte-pair, overwrite, rollback, and index behavior retained | Not Run |
| REG-DET-003 | Historical artifacts | Old IDs/index/PDFs load where applicable and remain untouched | Not Run |
| REG-V1-001 | Debugging Log | Storage, filename, ID, text, and V1 index unchanged | Pass |
| REG-V1-002 | Script Editing | Storage, filename, ID, text, and V1 index unchanged | Pass |
| REG-V1-003 | Script Creation | Storage, filename, ID, text, and V1 index unchanged | Pass |
| REG-V1-004 | Settings/actions | Change/reset/open behavior unchanged | Blocked |
| REG-V1-005 | Deferred routing | Debugging Log is not routed to Hotel/PMS folders | Pass |
| PRIV-001 | UI/generated content | Field/check-level only; no guest/payment/credential/raw-row content | Blocked |

Applicable manual items in `V1_TEST_CHECKLIST.txt` and
`docs/v2/V1-Regression-Checklist.md` were not run against a fresh published
executable because `PUB-001` did not produce one.

## Package, build, and publish gates

| ID | Gate | Expected | Status |
| --- | --- | --- | --- |
| PKG-001 | Direct packages | ClosedXML is exactly `0.105.1` and PDFsharp-MigraDoc-GDI is `6.2.4`; no additional direct spreadsheet package | Pass |
| PKG-002 | Resolved graph | Exact direct/transitive versions recorded from `project.assets.json`; license notices assessed separately | Pass |
| PKG-003 | Vulnerability check | Current transitive result recorded and dispositioned | Blocked |
| PKG-004 | Notices | `THIRD-PARTY-NOTICES.txt` matches graph and is in a fresh publish output | Not Run |
| BUILD-001 | Final Debug build | Exit 0; warnings/errors recorded | Pass |
| TEST-001 | Full Debug harness | Exit 0; all named suite/case counts recorded; individual assertion count is not instrumented | Pass |
| BUILD-002 | Final Release build | Exit 0; warnings/errors recorded | Pass |
| TEST-002 | Full Release harness | Exit 0; all named suite/case counts recorded; individual assertion count is not instrumented | Pass |
| PUB-001 | Windows publish | `win-x64`, self-contained, single-file; expected executable exists | Blocked |
| PUB-002 | Output inspection | No unexpected loose/native spreadsheet dependency or runtime installer | Blocked |
| PUB-003 | Excel independence | Published Quick workbook behavior works without Excel installed/automated | Blocked |
| PUB-004 | Published Quick create | New Surface workbook and history verified | Blocked |
| PUB-005 | Published Quick append | Existing/legacy-compatible append verified | Blocked |
| PUB-006 | Published Detailed | Detailed PDF generation regression verified | Blocked |
| PUB-007 | Published UI/startup | Exact executable and GUI scenarios run, or individually reported Not Run | Blocked |
| CLEAN-001 | Final repository scan | No generated workbook/PDF/test/bin/obj/publish artifact tracked | Pass |
| CLEAN-002 | Final Git status | Final commit/push leave no uncommitted verification changes | Not Run |
| CLEAN-003 | External temporary evidence | Final-pass temporary workbook/PDF evidence roots removed | Pass |

## Final evidence record

### Revision and worktree evidence

- Final-pass date: 2026-08-19.
- Branch: `update/quick-qa-surface-qa`.
- Reviewed implementation and pre-pass HEAD:
  `aee64206ad4346a3329c6a2cf993b47f00c4ed31`.
- Approved V2 baseline: `origin/v2-qa-reports` at
  `38422987180e124f22c8a836cc59fa858243251b`.
- `main` and `v1.0.0` both remained at
  `2f95d8a12c9b772124bd688d9a331e07730e5ab0`.
- `git fetch origin`, branch/ref inspection, ancestor check, and initial clean
  status all passed. No unrelated user edits were present and no merge, rebase,
  tag, branch deletion, or history rewrite was performed.
- The only final-pass source edits were five stale regression-suite XML comments;
  no production behavior changed. This matrix is the only final-pass design/doc
  change.

The historical pre-implementation baseline commands (`BASE-004` and
`BASE-005`) were not re-created in this final pass because the branch already
contained the reviewed implementation and no preserved contemporaneous baseline
log was available. They remain **Not Run**, rather than inferred from the final
builds.

### Automated build and harness evidence

The following commands each exited 0:

```powershell
dotnet build DocumentationLoggingDashboard.sln --configuration Debug
dotnet run --project tests\DocumentationLoggingDashboard.GeometryTests\DocumentationLoggingDashboard.GeometryTests.csproj --configuration Debug --no-build --
dotnet build DocumentationLoggingDashboard.sln --configuration Release
dotnet run --project tests\DocumentationLoggingDashboard.GeometryTests\DocumentationLoggingDashboard.GeometryTests.csproj --configuration Release --no-build --
```

- Debug build: 0 warnings, 0 errors.
- Release build: 0 warnings, 0 errors.
- Debug harness: 69/69 named scenarios passed, exit 0.
- Release harness: 69/69 named scenarios passed, exit 0, earlier in this final
  pass before the evidence-matrix edits. Only documentation changed afterward;
  behavior and compiled test code were identical.
- Named suite counts in each configuration: geometry 7/7; Blank/Broken semantic
  7/7; Detailed post-pilot 4/4; Quick core 5/5; Quick filename/preferences
  5/5; Quick workbook/save 12/12; Quick form/MainForm 4/4; Arrival Month 4/4;
  Detailed save/PDF/index 12/12; deferred text commit 8/8; V1 synthetic 1/1.
- The harness does not instrument a total count of individual assertions. No
  assertion-count claim is inferred from the 69 named scenarios.
- The Detailed harness's `[BLOCKED]` lines are expected names for negative
  validation cases that proved no PDF/index mutation; they are not failed or
  environment-blocked harness cases.
- Geometry ran off-screen at the actual 120-DPI (125%) session. This is project
  harness evidence, not a published-executable GUI run at separate 100%/150%
  system scales.

The V1 synthetic scenario passed Debugging Log, Script Editing, Script Creation,
daily IDs, filenames/text, three-entry V1 index, storage separation, and the
out-of-scope no-Hotel/PMS-routing behavior. Actual V1 buttons/settings were not
driven and remain **Blocked**. The Quick workbook/save suite passed all 12 named
cases; its transaction subset included staging failure, both commit rollback
paths, staged/final verification mismatch, concurrency detection, retained
post-backup source protection, and legitimate later repeat.

### Package, notice, and publish evidence

The restored `net10.0-windows` assets resolve:

```text
ClosedXML 0.105.1
ClosedXML.Parser 2.0.0
DocumentFormat.OpenXml 3.1.1
DocumentFormat.OpenXml.Framework 3.1.1
ExcelNumberFormat 1.1.0
Microsoft.Extensions.DependencyInjection.Abstractions 8.0.2
Microsoft.Extensions.Logging.Abstractions 8.0.3
PDFsharp-GDI 6.2.4
PDFsharp-MigraDoc-GDI 6.2.4
RBush.Signed 4.0.0
SixLabors.Fonts 1.0.0
```

Direct references remain ClosedXML `0.105.1` and PDFsharp-MigraDoc-GDI
`6.2.4`. The graph/licenses are reconciled in `THIRD-PARTY-NOTICES.txt`; no
Office Interop or Excel automation reference is present. A current
`dotnet list package --vulnerable` query was **Blocked** because access to the
user NuGet configuration/source required unavailable sandbox/network approval.
The earlier 2026-08-18 query found no vulnerable packages, but that prior result
is not promoted to a current Pass.

The intended fresh isolated publish command was:

```powershell
dotnet publish .\DocumentationLoggingDashboard\DocumentationLoggingDashboard.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true --output 'C:\Users\Gabriel Ramdeholl\AppData\Local\Temp\DocumentationLoggingDashboard-QuickQA-FinalVerify-20260819-01a0165a'
```

The network-capable invocation was rejected before execution because additional
sandbox/network approval was unavailable. The same command with `--no-restore`
ran and failed with `NETSDK1047`: the ordinary restore assets did not include
`net10.0-windows/win-x64`. The target directory remained absent. The repository
publish script was not redirected into its fixed `PublishedApp/win-x64` target
because that folder contains pre-existing July runtime/user artifacts and would
not be clean-room evidence. Consequently `PUB-001` is **Blocked**, and fresh
publish inventory/runtime/GUI gates are also **Blocked**.

A prior isolated publish from the unchanged production tree was re-inspected as
supporting evidence only; it is not the fresh final-pass publish and no GUI
claim is based on it:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `appsettings.json` | 53 | `D8907CDD3C2440BD404B7C1A23837AAAD4449F428AE64E2A1FFF038DD6FA2E03` |
| `DocumentationLoggingDashboard.exe` | 128839146 | `958F926243886FCABEC302CD118BE9F9D96F842EFF8B89877DFED3C1BDF2BB19` |
| `DocumentationLoggingDashboard.pdb` | 157188 | `DC3D7D72E040D98D7BAE0732BFDD40475A12DE9BE56AAF56FEF6ADA85E93C6F3` |
| `THIRD-PARTY-NOTICES.txt` | 17455 | `C1AD94373FE2BE094312241731040AAFCF20368673272E71490FBCB0CAF1036F` |

The published notice hash equals the source notice hash. There were no loose
spreadsheet assemblies in that prior four-file output. This supports the
configured single-file deployment shape but does not close `PUB-001` through
`PUB-007`.

### Published/manual ledger and limitations

No published-native UI scenario was claimed from the project-reference console
harness. Fresh publish/launch approval was unavailable, the repository has no
published-native UI driver or application test mode, and this machine has Excel
installed/running. Therefore the following remain **Blocked**: actual workflow
opening (`UI-001`, `UI-002`), V1 buttons/settings (`UI-004`, `REG-V1-004`),
published create/append/legacy append/history/Detailed PDF/startup
(`PUB-003` through `PUB-007`), success-dialog ordering (`VAL-005`), and the
manual privacy workflow (`PRIV-001`). Literal behavior with Excel absent and
real 100%/150% display sessions was not available in this environment.

Requirements marked **Not Run** are deliberately narrower coverage gaps, most
notably: exact empty Detailed File ID; an old schema-1 PDF; replacement index
File ID; historical Email-ID artifact; strategy alias/Average Rate
qualification; PDF proof of the retired Arrival row; every invalid Quick N/A;
handled Warning; save-allowed final Fail; exact manual Summary persistence;
blank non-clean Summary; workbook width/hidden/merged-cell formatting;
integrated preference selection/fallback/enumeration; missing/near/duplicate
Surface headers; unsupported OpenXML custom/coauthor-part preservation; the
explicit no-PDF/index Quick boundary; friendly real-lock dialog text; exact
new-history/client-failure order; true rapid double-click; rollback-failure
manual-review fallback; and the broad Detailed historical/rollback wording.
Their underlying partial source/service coverage is not promoted to Pass.

### Cleanup and artifact handling

- All semantic-harness roots were removed by the harness. The supplied July
  workbook was never copied, edited, or committed.
- An initial explicit cleanup command was rejected before execution because
  destructive-operation approval was unavailable. The final read-only existence
  scan nevertheless confirmed all three final-pass Arrival evidence roots were
  absent: `arr-24382878c5c0`, `arr-28833823fb3f`, and `arr-44fde49abeea`.
  `CLEAN-003` therefore records **Pass** from the observed final state.
- The prior isolated publish is also outside the repository and was retained as
  the explicitly labelled supporting artifact above. The stale ignored
  `PublishedApp/win-x64` contents were not modified.
- Repository scans found no tracked generated `.xlsx`, `QuickQAHistory.xlsx`,
  PDF, generated runtime-settings artifact, transaction stage/rollback, `bin`,
  `obj`, or publish artifact. The intended final diff contains only the five
  comment corrections and this evidence matrix. Because the commit/push follows
  this matrix snapshot, `CLEAN-002` remains **Not Run** here; the post-push
  status is recorded in the final handoff.
- ClosedXML can drop unsupported/custom workbook extension parts. Ordinary
  visible legacy rows/styles are covered, but `SURF-011` remains an explicitly
  accepted **Not Run** preservation risk.
