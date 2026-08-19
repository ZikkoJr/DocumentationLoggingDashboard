# Quick QA / Surface QA Test Matrix

## Use of this matrix

This is the required verification matrix for the post-pilot Detailed/Quick QA
update. `Required` means the case must run before review; it is not a claim that
the case has passed. Record command, configuration, exit code, assertion count,
and evidence location when execution occurs. Mark unavailable GUI scenarios
`Not Run` or `Blocked` with the exact reason instead of inferring success.

Use only synthetic Hotel/report data. Create workbook fixtures under unique
system-temporary roots, never in the repository. Do not copy or commit the
user's sample workbook, generated `.xlsx`/PDF files, `bin`, `obj`, or publish
output. Protect pre-existing ignored publish/runtime content.

## Canonical commands

From the repository root:

```powershell
dotnet build DocumentationLoggingDashboard.sln -c Debug
dotnet run --project tests\DocumentationLoggingDashboard.GeometryTests\DocumentationLoggingDashboard.GeometryTests.csproj -c Debug --no-build --
dotnet build DocumentationLoggingDashboard.sln -c Release
dotnet run --project tests\DocumentationLoggingDashboard.GeometryTests\DocumentationLoggingDashboard.GeometryTests.csproj -c Release --no-build --
powershell -ExecutionPolicy Bypass -File .\publish-windows.ps1
git status --short
```

The regression project is a console harness, not a `dotnet test` test-adapter
project. Supported focused routes include `--geometry-only`, `--semantic-only`,
`--arrival-month-only`, `--deferred-only`, `--v1-only`, and bounded visible
smoke/evidence paths described by the harness `--help` output.

## Baseline and repository gates

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| BASE-001 | Fetch and inspect remote | `origin/v2-qa-reports` is explicitly fetched and exact approved baseline is recorded | Required |
| BASE-002 | Branch/tag identity | Work occurs on update branch from approved V2 commit; `main` remains V1; `v1.0.0` is unchanged | Required |
| BASE-003 | Worktree ownership | Existing user/concurrent changes are identified and preserved before edits | Required |
| BASE-004 | Baseline Debug build | Exit 0; warning/error counts recorded | Required |
| BASE-005 | Baseline full console harness | Exit 0; all existing suites recorded | Required |
| BASE-006 | No prohibited Git action | No commit, push, merge, rebase, or tag mutation | Required |

## MainForm and workflow boundaries

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| UI-001 | `Detailed QA Report` action | Opens the existing Detailed workflow | Required |
| UI-002 | `Quick QA` action | Opens the dedicated Quick form | Required |
| UI-003 | Handler inspection | Each action fires once; no duplicate handler/bootstrap path | Required |
| UI-004 | V1 actions | Preview, submit, open, folder, and settings actions remain functional | Required |
| UI-005 | Quick form shape | One scrollable page with Report Details, Raw/DB headers, custom-script controls, Result, Summary, and save | Required |
| UI-006 | Quick omissions | No Statistics/File Characteristics tabs, Detailed wizard, PDF controls, Created By, Original Filename, or QA Date input | Required |

## Detailed File ID and index

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| DET-ID-001 | Blank File ID | Readiness/save blocked | Required |
| DET-ID-002 | Whitespace-only File ID | Readiness/save blocked | Required |
| DET-ID-003 | Outer whitespace | Stored/displayed value is trimmed | Required |
| DET-ID-004 | `001234` | Remains exact text `001234` | Required |
| DET-ID-005 | Readiness edit | File ID participates in fingerprint/staleness | Required |
| DET-ID-006 | PDF | Schema-2 report information displays File ID | Required |
| DET-ID-007 | Index serialize/load | New entry contains and reloads `File ID:` | Required |
| DET-ID-008 | Filename | Existing Detailed filename remains unchanged | Required |
| DET-ID-009 | Logical key | Hotel ID + File Month overwrite identity remains unchanged | Required |
| DET-ID-010 | Schema | New rendered report schema is 2; old PDF remains schema 1 | Required |
| DET-ID-011 | Historical index only | Existing 13-line entries load without File ID | Required |
| DET-ID-012 | New index only | Label-based schema-2 entries load | Required |
| DET-ID-013 | Mixed index | Old and new entries load together | Required |
| DET-ID-014 | No migration | Loading/saving unrelated report does not add File ID to legacy entry | Required |
| DET-ID-015 | Replacement | Replacing matching logical report updates that entry with new File ID | Required |
| DET-ID-016 | Malformed/duplicate labels | Safely rejected; no partial rewrite | Required |

## Detailed Email

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| DET-EMAIL-001 | New catalog | `RAW.EMAIL.COLUMN_AVAILABLE` exists once; `RAW.REQUIRED.EMAIL_PRESENT` is absent | Required |
| DET-EMAIL-002 | Available | Pass; no generated finding | Required |
| DET-EMAIL-003 | Unavailable | Exactly one Warning; no Failure or generic duplicate | Required |
| DET-EMAIL-004 | Email unavailable alone | Overall `Pass with Warnings` | Required |
| DET-EMAIL-005 | Historical old ID | Old artifact remains readable; no migration | Required |

## Strategy matrices (run independently for Detailed and Quick)

Detailed uses `WARN:/FAIL:STRATEGY:SOURCE_RATE_MARKET`; Quick uses
`QUICK:WARN:/QUICK:FAIL:STRATEGY:SOURCE_RATE_MARKET`.

| ID suffix | Source | Rate | Market | Expected |
| --- | :---: | :---: | :---: | --- |
| 000 | Available | Available | Available | No strategy finding |
| 100 | Missing | Available | Available | Exactly one Strategy Warning naming Source |
| 010 | Available | Missing | Available | Exactly one Strategy Warning naming Rate |
| 001 | Available | Available | Missing | Exactly one Strategy Warning naming Market |
| 110 | Missing | Missing | Available | Exactly one Strategy Warning naming Source and Rate |
| 101 | Missing | Available | Missing | Exactly one Strategy Warning naming Source and Market |
| 011 | Available | Missing | Missing | Exactly one Strategy Warning naming Rate and Market |
| 111 | Missing | Missing | Missing | Exactly one aggregate Failure; no Strategy Warning |

For each of `DET-STRAT-*` and `QUICK-STRAT-*`, assert exact missing categories,
deterministic ID, no normal Warning for one/two missing, no individual Failure,
no duplicate finding, and correct overall Result. Also assert `Average Rate`
does not qualify while each approved Source/Rate/Market alias does.

## Detailed Arrival and Statistics regressions

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| DET-ARR-001 | Catalog/form/PDF | Retired Arrival checklist row is absent with no empty row | Required |
| DET-ARR-002 | No valid applicable dates | Existing applicability preserved; no threshold Failure | Required |
| DET-ARR-003 | Below 30% | No Arrival threshold finding | Required |
| DET-ARR-004 | Exactly 30% / `3/10` | No Arrival threshold finding | Required |
| DET-ARR-005 | Above 30% / `4/13` | Exactly one statistics Failure with `STAT:FAIL:ARRIVAL_OUTSIDE_FILE_MONTH` | Required |
| DET-ARR-006 | Count changes | Finding appears, refreshes, disappears, and recurs without duplication | Required |
| DET-STAT-001 | Total Data Rows | Existing propagation remains correct | Required |
| DET-STAT-002 | Blank denominator | Auto/manual/reset behavior remains correct | Required |
| DET-STAT-003 | Broken denominator | Uses applicable nonblank denominator; override/reset remains correct | Required |
| DET-STAT-004 | Blank thresholds | 0, Warning range, and Failure range retain approved behavior | Required |
| DET-STAT-005 | Broken thresholds | 0, Warning range, and Failure range retain approved behavior | Required |
| DET-STAT-006 | Blank vs Broken | Blank is not Broken | Required |
| DET-STAT-007 | Name checks | Blank names do not fail populated-name checks | Required |

## Quick catalog and domain

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| QUICK-CAT-001 | Catalog identity | Exactly 21 stable IDs, each once, in documented order | Required |
| QUICK-CAT-002 | Section count | 14 Raw followed by 7 Database rows | Required |
| QUICK-CAT-003 | N/A definitions | Only currency, confirmation candidates, rejected records, and DB Email allow N/A | Required |
| QUICK-CAT-004 | Invalid N/A | Validation rejects N/A on every other row | Required |
| QUICK-CAT-005 | No Quick extras | No email-presence, Arrival Month, Full/First/Last split, detailed monetary spot checks, Statistics, or File Characteristics | Required |
| QUICK-CAT-006 | Separate status type | Direct Warning/N/A works without changing Detailed enum | Required |
| QUICK-NAME-001 | Separate names available | Raw Names may Pass | Required |
| QUICK-NAME-002 | Full Name available | Raw Names may Pass | Required |
| QUICK-NAME-003 | No usable name source | Appropriate non-pass result/finding can be recorded | Required |
| QUICK-NAME-004 | DB correct/incorrect | Correct mapping Passes; incorrect mapping produces selected outcome | Required |
| QUICK-MONEY-001 | One usable monetary type | Raw availability Passes | Required |
| QUICK-MONEY-002 | Multiple usable types | Raw availability Passes without Detailed branching UI | Required |
| QUICK-MONEY-003 | DB mapping | Correct actual type Passes; incorrect type records finding | Required |

## Quick custom-script and Result

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| QUICK-CS-001 | Available = No | Handled state is prevented/cleared/rejected | Required |
| QUICK-CS-002 | Available = Yes | Each Warning/Failure may be handled independently | Required |
| QUICK-CS-003 | Warning handled | Severity retained; status remains `Pass with Warnings` | Required |
| QUICK-CS-004 | Failure handled | Severity retained; no longer Active | Required |
| QUICK-CS-005 | Multiple Failures | Handling one does not affect another | Required |
| QUICK-CS-006 | One Active Failure | Overall `Fail` | Required |
| QUICK-CS-007 | All Failures handled | No Active Failure; `Pass with Warnings` | Required |
| QUICK-STATUS-001 | Clean | `Pass` | Required |
| QUICK-STATUS-002 | Ordinary Warning | `Pass with Warnings` | Required |
| QUICK-STATUS-003 | Strategy Warning | `Pass with Warnings` | Required |
| QUICK-STATUS-004 | Active Failure | `Fail` and save remains allowed when otherwise valid | Required |
| QUICK-STATUS-005 | Status tampering/staleness | Validation rejects mismatch with current findings | Required |

## Summary

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| SUM-001 | Clean report | Exact `Raw file and Database QA passed successfully` | Required |
| SUM-002 | Findings | One concise sentence per finding, one line per sentence | Required |
| SUM-003 | Multiple findings | Multiline combined Summary | Required |
| SUM-004 | Strategy Warning | Sentence visibly says `Strategy Warning` | Required |
| SUM-005 | Current manual edit | Saves exact edited current text | Required |
| SUM-006 | Edit then QA change | Text preserved but visibly marked stale | Required |
| SUM-007 | Stale save | Blocked | Required |
| SUM-008 | Regenerate | Current generated text/fingerprint restored | Required |
| SUM-009 | Edit after regenerate | New current Summary remains user-editable | Required |
| SUM-010 | Blank non-clean Summary | Blocked | Required |
| SUM-011 | Privacy | No guest-level examples or copied rows generated | Required |

## New Surface workbook and filename safety

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| NEW-001 | Custom filename with spaces | Accepted, e.g. `Surface QA August.xlsx` | Required |
| NEW-002 | Missing extension | `.xlsx` appended | Required |
| NEW-003 | Invalid characters | Predictably sanitized or rejected per service contract | Required |
| NEW-004 | Rooted/traversal/separators | Rejected | Required |
| NEW-005 | `.` / `..` | Rejected | Required |
| NEW-006 | Reserved device name | Rejected | Required |
| NEW-007 | Trailing dot/space and length | Safely normalized/limited | Required |
| NEW-008 | Other extension | Cannot escape `.xlsx` contract | Required |
| NEW-009 | Existing filename | Never overwritten; compatible file may be selected | Required |
| NEW-010 | Root containment | Resolved file is a direct child of `SurfaceQA` | Required |
| NEW-011 | Workbook structure | `Surface QA`, exact headers, `SurfaceQaTable`, filter, row-1 freeze, wrapped Summary | Required |
| NEW-012 | Formatting | Compact readable widths, no merged data cells or hidden required data | Required |
| NEW-013 | Selection/preference | New workbook becomes selected and last-used | Required |

## Existing and legacy Surface workbooks

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| SURF-001 | Canonical load/append | Prior rows preserved; table/filter expands; exact row appended | Required |
| SURF-002 | Formatting preservation | Existing usable client formatting remains usable | Required |
| SURF-003 | Sample aliases | Exact seven approved legacy aliases accepted after trim/case normalization | Required |
| SURF-004 | Legacy canonicalization | Only seven headers canonicalized in staged copy | Required |
| SURF-005 | Legacy old data | Row order/values/results remain unchanged | Required |
| SURF-006 | Legacy structure repair | Table/filter/freeze/wrap established; new row appended | Required |
| SURF-007 | Missing header | Clear failure; source byte/content state unchanged | Required |
| SURF-008 | Wrong/near-match header | Clear failure; no rewrite | Required |
| SURF-009 | Duplicate/ambiguous/reordered/extra schema | Clear failure; no rewrite | Required |
| SURF-010 | Corrupt `.xlsx` | Clear failure; no rewrite | Required |
| SURF-011 | Unsupported extension metadata | Visible business data/style preserved; any dropped custom/coauthor parts assessed and recorded | Required |

Supplied-sample structural baseline for controlled fixture recreation:

- one sheet `Sheet1`, range `A1:G30`, 29 data rows;
- headers normalize to `Month 2026 | Hotel Name | Hotel Id | PMS | file id |
  Pass/Fail/Warning | summary`;
- no table, filter, freeze, formula, validation, conditional formatting, or
  Summary wrap;
- Hotel/File IDs are historically numeric; new row IDs must be text;
- Month/Result case and whitespace vary and must not be normalized;
- an Excel `~$` lock existed during audit.

## Last-used workbook and exact Surface row

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| PREF-001 | Save/select A then reopen | A selected | Required |
| PREF-002 | Switch/save B then reopen | B selected | Required |
| PREF-003 | Remembered B renamed/deleted | Safe compatible fallback or explicit selection; no crash | Required |
| PREF-004 | Preference payload | Stores filename only and writes atomically | Required |
| PREF-005 | Enumeration | Ignores `~$`, temp, staging, rollback, and non-direct-child files | Required |
| ROW-001 | File Month | Exact text `yyyy-MM` | Required |
| ROW-002 | Hotel Name/ID/PMS | Exact canonical metadata; Hotel ID text | Required |
| ROW-003 | File ID | Exact text preserving leading zeroes | Required |
| ROW-004 | Result/Summary | Exact current final values | Required |
| ROW-005 | Timestamp privacy | No QA Timestamp in client workbook | Required |

## Hotel history

| ID | Case | Expected | Status |
| --- | --- | --- | --- |
| HIST-001 | First Quick QA | Creates canonical Hotel `QuickQAHistory.xlsx` | Required |
| HIST-002 | Structure | Sheet `Quick QA History`, table `QuickQaHistoryTable`, exact 8 columns | Required |
| HIST-003 | Timestamp | Round-trip ISO-8601 text includes offset; injectable clock used once | Required |
| HIST-004 | Event identity | Same month/hotel/PMS/File ID/Result/Summary as client row | Required |
| HIST-005 | Second/later QA | Appends another row; no permanent deduplication | Required |
| HIST-006 | Boundary | No PMS history, PDF, or Detailed index update | Required |

## Transaction, locks, and reentrancy

For every failure, assert no false success, no normal one-sided event, original
bytes/data restored where practical, and staging/rollback cleanup.

| ID | Injected case | Expected | Status |
| --- | --- | --- | --- |
| TXN-001 | Surface workbook locked | Friendly Surface message; neither logical event committed | Required |
| TXN-002 | Existing Hotel history locked | Friendly history message; neither logical event committed | Required |
| TXN-003 | Surface staging failure | No destination commit | Required |
| TXN-004 | History staging failure | No destination commit | Required |
| TXN-005 | First commit succeeds, second fails | First destination rolled back | Required |
| TXN-006 | New history then client failure | New history removed | Required |
| TXN-007 | Existing destination rollback | Original bytes restored | Required |
| TXN-008 | Source changes between read/commit | Fingerprint mismatch aborts without overwrite | Required |
| TXN-009 | Staged validation mismatch | Commit blocked | Required |
| TXN-010 | Final verification mismatch | No success; recovery/manual-review state reported safely | Required |
| TXN-011 | Rapid double click | Exactly one Surface row and one history row | Required |
| TXN-012 | Legitimate later repeat | Another row appends to both | Required |

## Validation coverage

Independently verify save is blocked for: no canonical Hotel; removed Hotel
metadata; PMS mismatch; invalid File Month; blank File ID; required
`NotEvaluated`; invalid N/A; inconsistent strategy group; handled state while
Custom Script Available is No; status/finding mismatch; blank required Summary;
stale Summary; missing/unavailable Surface file; out-of-root selection;
incompatible schema; and invalid Hotel history path.

Verify save is not blocked merely by an ordinary Warning, Strategy Warning, or
final Fail. Successful UI confirmation must include Hotel, File ID, Result,
Surface filename, and Hotel history update, and must precede close/reset.

## Detailed and V1 regression

| ID | Area | Expected | Status |
| --- | --- | --- | --- |
| REG-DET-001 | Detailed form/readiness/findings | Existing behavior retained except approved changes | Required |
| REG-DET-002 | Detailed PDF/paired save/index | Byte-pair, overwrite, rollback, and index behavior retained | Required |
| REG-DET-003 | Historical artifacts | Old IDs/index/PDFs load where applicable and remain untouched | Required |
| REG-V1-001 | Debugging Log | Storage, filename, ID, text, and V1 index unchanged | Required |
| REG-V1-002 | Script Editing | Storage, filename, ID, text, and V1 index unchanged | Required |
| REG-V1-003 | Script Creation | Storage, filename, ID, text, and V1 index unchanged | Required |
| REG-V1-004 | Settings/actions | Change/reset/open behavior unchanged | Required |
| REG-V1-005 | Deferred routing | Debugging Log is not routed to Hotel/PMS folders | Required |
| PRIV-001 | UI/generated content | Field/check-level only; no guest/payment/credential/raw-row content | Required |

Run every applicable manual item in `V1_TEST_CHECKLIST.txt` and
`docs/v2/V1-Regression-Checklist.md`.

## Package, build, and publish gates

| ID | Gate | Expected | Status |
| --- | --- | --- | --- |
| PKG-001 | Direct packages | ClosedXML is exactly `0.105.1`; no other Excel library | Required |
| PKG-002 | Resolved graph | Exact transitive versions/licenses recorded from assets/list output | Required |
| PKG-003 | Vulnerability check | Transitive result recorded and dispositioned | Required |
| PKG-004 | Notices | `THIRD-PARTY-NOTICES.txt` matches graph and is in publish output | Required |
| BUILD-001 | Final Debug build | Exit 0; warnings/errors recorded | Required |
| TEST-001 | Full Debug harness | Exit 0; all assertion/suite counts recorded | Required |
| BUILD-002 | Final Release build | Exit 0; warnings/errors recorded | Required |
| TEST-002 | Full Release harness | Exit 0; all assertion/suite counts recorded | Required |
| PUB-001 | Windows publish | `win-x64`, self-contained, single-file; expected executable exists | Required |
| PUB-002 | Output inspection | No unexpected loose/native spreadsheet dependency or runtime installer | Required |
| PUB-003 | Excel independence | Published Quick workbook behavior works without Excel installed/automated | Required |
| PUB-004 | Published Quick create | New Surface workbook and history verified | Required |
| PUB-005 | Published Quick append | Existing/legacy-compatible append verified | Required |
| PUB-006 | Published Detailed | Detailed PDF generation regression verified | Required |
| PUB-007 | Published UI/startup | Exact executable and GUI scenarios run, or individually reported Not Run | Required |
| CLEAN-001 | Final repository scan | No generated workbook/PDF/test/bin/obj/publish artifact tracked | Required |
| CLEAN-002 | Final Git status | Complete `git status --short` recorded; no commit/push | Required |

## Final evidence record

Record at completion:

- branch and exact baseline;
- exact package graph and licenses;
- Debug/Release build commands and results;
- Debug/Release harness commands, suite/assertion counts, and results;
- publish command, RID/self-contained/single-file properties, file inventory,
  sizes, and hashes;
- workbook fixture roots and cleanup result;
- published-executable scenarios actually run;
- GUI/scaling/manual scenarios not run and why;
- final changed-file list and `git status --short`;
- remaining risks, including any unsupported OpenXML metadata preservation.
