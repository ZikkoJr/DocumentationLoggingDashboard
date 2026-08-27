# Excel Documentation Logs Test Matrix

## Status convention

This matrix records the final verification status for the Excel documentation-log
update. `Pass` is used only where the named automated command, isolated harness,
or repository inspection produced evidence. `Manual Required` identifies an
interactive observation that was not made, and `Not Run` remains explicit where
a dedicated fault case was not executed.

## Executed evidence — 2026-08-27

| Verification surface | Final status | Evidence |
|---|---|---|
| Exact-baseline ancestry and update branch | Pass | `merge-base --is-ancestor` succeeded from `439cd1e725a738f7678351f03ccf4a432800bf50`; work remained on `update/excel-documentation-logs`. |
| Post-change Debug build | Pass | `dotnet build DocumentationLoggingDashboard.sln -c Debug`; 0 warnings, 0 errors. |
| Full Debug regression harness | Pass | Required unfiltered `dotnet run ... -c Debug --no-build --` exited 0. |
| Post-change Release build | Pass | `dotnet build DocumentationLoggingDashboard.sln -c Release`; 0 warnings, 0 errors. |
| Full Release regression harness | Pass | Required unfiltered `dotnet run ... -c Release --no-build --` exited 0. |
| Focused documentation-log suites | Pass | 33 named cases passed: parser/routing 5, workbook/schema/preferences 4, sequence/index 6, transactions 14, MainForm 4. |
| Existing Quick/Detailed QA and V1 compatibility | Pass | Both full configurations passed Quick QA workbook/transaction/form, Detailed PDF/index, Arrival Month, geometry, deferred-text, and updated V1 coverage. |
| Workbook/schema/parser/routing/sequence/index/rollback rows below | Pass where the row names an automated assertion | The focused suites use isolated synthetic roots and cover all three log types, typed workbook rows, strict incompatibility rejection, legacy preservation, concurrent allocation, routing fan-out, rollback, locks, and normal residue cleanup. |
| Windows publish | Pass | `publish-windows.ps1` produced Release `win-x64`, self-contained, single-file output and the expected executable. |
| Published executable process start | Pass (process smoke only) | The exact executable remained running for a bounded five-second hidden launch and was then stopped cleanly. No visual assertion was made. |
| Visual MainForm walkthrough and minimum-size review | Manual Required | WinForms layout contracts and event/state behavior passed automated checks, but no human visual pass was performed. |
| Open Selected Workbook / Open Logs Folder / Open Log Index shell launch | Manual Required | Labels, enablement, and exactly-one handler routing passed; external shell associations were not invoked during automation. |
| Published-GUI isolated workbook create/append | Manual Required | The published app has no noninteractive command-line workflow. Equivalent service behavior passed in both Debug and Release synthetic-root harnesses. |
| Successful-save cleanup-warning injection | Not Run | The warning/result/UI path exists. Ordinary success/failure residue cleanup and forced rollback/manual-review behavior passed, but a cleanup-only deletion failure was not separately injected. |
| Repository/documentation review | Pass | Generated workbooks/PDFs/temp/backups/settings and `PublishedApp` are ignored; README, V1 checklist, design, matrix, and legacy-compatibility notes were reviewed. |

## Repository, build, and packaging

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| BUILD-01 | Work branch is descended from exact baseline `439cd1e725a738f7678351f03ccf4a432800bf50`. | Commit/merge-base output required | Pass |
| BUILD-02 | No unrelated local changes were overwritten; generated workbook/PDF/temp/backup/settings/publish evidence is absent from the review diff. | `git status -sb`, file review | Pass |
| BUILD-03 | Debug solution build completes with no errors. | `dotnet build DocumentationLoggingDashboard.sln -c Debug` | Pass |
| BUILD-04 | Full Debug regression harness exits successfully. | Required `dotnet run ... -c Debug --no-build --` output | Pass |
| BUILD-05 | Release solution build completes with no errors. | `dotnet build DocumentationLoggingDashboard.sln -c Release` | Pass |
| BUILD-06 | Full Release regression harness exits successfully. | Required `dotnet run ... -c Release --no-build --` output | Pass |
| BUILD-07 | Windows publish completes as Release, `win-x64`, self-contained, single-file. | `publish-windows.ps1` output and artifact inspection | Pass |
| BUILD-08 | Published application starts where environment policy permits. | Bounded hidden launch of the exact published executable | Pass (process smoke only) |
| BUILD-09 | Published application completes an isolated workbook create/append smoke flow where practical. | Synthetic-root observation | Manual Required |
| BUILD-10 | Any GUI or published-build step the environment cannot execute is explicitly recorded as Blocked/Manual Required rather than passed. | End-of-work report | Pass |

## Workbook creation, formatting, and discovery

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| WB-01 | New Debugging Running workbook uses the exact Debugging title and ordered ten-column schema. | Automated workbook inspection | Pass |
| WB-02 | New Script Editing Running workbook uses the exact Editing title and ordered eight-column schema. | Automated workbook inspection | Pass |
| WB-03 | New Script Creation Running workbook uses the exact Creation title and ordered eight-column schema. | Automated workbook inspection | Pass |
| WB-04 | Every new workbook has title row 1, blank row 2, table headers row 3, and rows beginning at row 4. | Automated workbook inspection | Pass |
| WB-05 | Data range is a real Excel Table with filtering enabled and rows frozen through the header. | Automated workbook inspection | Pass |
| WB-06 | Long text wraps, widths are reasonable, and no cells inside the table are merged. | Automated plus rendered/manual inspection | Pass (automated structure); Manual visual |
| WB-07 | Date/Time is a real Excel date/time value. | ClosedXML cell-type assertion | Pass |
| WB-08 | Log ID and Hotel ID/Hotel IDs are explicitly text and leading zeroes survive round trip. | ClosedXML cell-type/value assertion | Pass |
| WB-09 | `Log Type` is absent from every table schema. | Exact header assertion | Pass |
| WB-10 | Hidden `__DLD_Metadata` contains schema name/version, log type, scope type, and scope ID. | Automated metadata inspection | Pass |
| WB-11 | Hotel history title includes canonical Hotel Name and Hotel ID. | Automated workbook inspection | Pass |
| WB-12 | PMS history title includes canonical PMS Name. | Automated workbook inspection | Pass |
| WB-13 | Editing/Creation history rows retain the event's complete normalized Hotel-ID list. | Cross-destination row comparison | Pass |
| WB-14 | Compatible Running discovery returns only valid `.xlsx` files for the selected log type and Running scope. | Synthetic-directory test | Pass |
| WB-15 | A valid workbook appends one row without resetting or overwriting historical rows. | Before/after workbook assertion | Pass |
| WB-16 | A missing deterministic Hotel/PMS history workbook is correctly created in staging and receives the event. | Transaction test | Pass |

## Filename safety and preferences

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| PREF-01 | Filename without an extension receives `.xlsx`. | Unit test | Pass |
| PREF-02 | Existing `.xlsx` extension is accepted case-insensitively and canonicalized. | Unit test | Pass |
| PREF-03 | Rooted paths, separators, and traversal are rejected. | Unit test | Pass |
| PREF-04 | Unsupported extensions are rejected. | Unit test | Pass |
| PREF-05 | Invalid characters, reserved Windows device names, terminal dots/spaces, empty names, and excessive length are rejected. | Unit test | Pass |
| PREF-06 | Create New cannot escape the selected type's Running directory. | Path-containment test | Pass |
| PREF-07 | Existing file or directory collision is rejected and never overwritten. | Synthetic-directory test | Pass |
| PREF-08 | Newly created workbook is immediately selected. | MainForm/UI test | Pass |
| PREF-09 | Debugging, Editing, and Creation remember independent leaf filenames. | Preferences regression | Pass |
| PREF-10 | Selecting one type does not overwrite either other type's preference. | Preferences regression | Pass |
| PREF-11 | Settings writes are atomic and versioned in `Index/documentation-log-settings.json`. | Fault-injection/file inspection | Pass |
| PREF-12 | Missing/renamed remembered workbook is handled without crash and permits reselection/creation. | Preferences/MainForm regression | Pass |
| PREF-13 | Corrupt, locked, unsafe, or incompatible remembered choice is handled without crash. | Preferences/MainForm regression | Pass |
| PREF-14 | Clearing fields or switching types does not clear persistent workbook preferences. | MainForm regression | Pass |

## Debugging metadata, validation, and routing

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| DBG-01 | Hotel search finds a canonical record by Hotel ID. | Selector regression | Pass |
| DBG-02 | Hotel search finds a canonical record by Hotel Name. | Selector regression | Pass |
| DBG-03 | Selection shows canonical Hotel Name, Hotel ID, and PMS. | MainForm/selector regression | Pass |
| DBG-04 | PMS is read-only/canonical and cannot be manually mismatched. | MainForm regression | Pass |
| DBG-05 | Missing Hotel selection blocks Preview/Submit as applicable. | Validation test | Pass |
| DBG-06 | Selected Hotel removed before save is blocked after fresh metadata load. | Stale-metadata test | Pass |
| DBG-07 | Changed Hotel Name/PMS/folder routing is blocked and requires refresh/reselection. | Stale-metadata test | Pass |
| DBG-08 | Missing Error Shown On Ticket blocks the save. | Validation test | Pass |
| DBG-09 | Missing Root Cause blocks the save. | Validation test | Pass |
| DBG-10 | Missing Fix Applied blocks the save. | Validation test | Pass |
| DBG-11 | Created By remains optional and blank saves as `N/A`. | Workbook assertion | Pass |
| DBG-12 | Notes / Follow-up remains optional and blank saves as `N/A`. | Workbook assertion | Pass |
| DBG-13 | Existing compatible Running workbook can be selected and used. | UI plus save test | Pass |
| DBG-14 | Create New produces and selects a valid Debugging Running workbook. | UI plus workbook test | Pass |
| DBG-15 | Locked selected Running workbook gives the focused operator message and no success. | Lock/failure test | Pass |
| DBG-16 | One save writes exactly one Running, one Hotel-history, one PMS-history, and one index row. | Cross-file transaction assertion | Pass |
| DBG-17 | All three workbook rows have identical ID, timestamp, canonical metadata, business data, and optional values. | Cross-destination comparison | Pass |

## Hotel ID parser and canonical resolution

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| PARSE-01 | `1953` resolves as one text ID. | Parser test | Pass |
| PARSE-02 | `1953, 2093` resolves in entered order. | Parser test | Pass |
| PARSE-03 | `1953;2093` resolves in entered order. | Parser test | Pass |
| PARSE-04 | CR, LF, and CR/LF multiline input splits correctly. | Parser test | Pass |
| PARSE-05 | Mixed comma, semicolon, and newline delimiters split correctly. | Parser test | Pass |
| PARSE-06 | Leading/trailing whitespace and empty tokens are removed. | Parser test | Pass |
| PARSE-07 | Duplicate IDs are removed case-insensitively and first-entered order remains stable. | Parser test | Pass |
| PARSE-08 | Leading zeroes survive parsing, resolution, canonical string creation, and workbook serialization. | Parser/workbook test | Pass |
| PARSE-09 | One unknown ID blocks the entire event and identifies that ID. | Parser/routing test | Pass |
| PARSE-10 | Multiple unknown IDs block the entire event and list them concisely. | Parser/routing test | Pass |
| PARSE-11 | Mixed valid/invalid IDs write no Running, history, index, or state artifact. | Transaction assertion | Pass |
| PARSE-12 | Resolved Hotel Name, PMS, and folder values come from current canonical metadata rather than input text. | Routing test | Pass |
| PARSE-13 | Canonical workbook representation is `ID1; ID2; ID3`. | Parser/event assertion | Pass |

## Script Editing routing and content

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| EDIT-01 | Manual Hotel ID(s), Script Name, Reason For Edit, and Changes Made are required. | Validation test | Pass |
| EDIT-02 | Editing presents no Hotel picker and no manual Hotel Name/PMS inputs. | MainForm regression | Pass |
| EDIT-03 | Two Hotels on one PMS produce Running once, each Hotel once, and that PMS once. | Routing transaction test | Pass |
| EDIT-04 | Hotels on different PMS systems produce each unique PMS once. | Routing transaction test | Pass |
| EDIT-05 | Duplicate Hotel IDs do not duplicate Hotel or PMS destinations. | Routing transaction test | Pass |
| EDIT-06 | Script Name survives identically in every destination. | Cross-destination comparison | Pass |
| EDIT-07 | Reason For Edit survives identically in every destination. | Cross-destination comparison | Pass |
| EDIT-08 | Changes Made survives identically in every destination. | Cross-destination comparison | Pass |
| EDIT-09 | Created By and Notes / Follow-up remain optional with `N/A` semantics. | Workbook assertion | Pass |
| EDIT-10 | Exactly one index entry points to the selected Editing Running workbook and summarizes canonical Hotel IDs. | Index assertion | Pass |

## Script Creation routing and content

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| CREATE-01 | Manual Hotel ID(s), Script Name, Reason For Creation, and Script Purpose / What It Does are required. | Validation test | Pass |
| CREATE-02 | Creation presents no Hotel picker and no manual Hotel Name/PMS inputs. | MainForm regression | Pass |
| CREATE-03 | Two Hotels on one PMS produce Running once, each Hotel once, and that PMS once. | Routing transaction test | Pass |
| CREATE-04 | Hotels on different PMS systems produce each unique PMS once. | Routing transaction test | Pass |
| CREATE-05 | Duplicate Hotel IDs do not duplicate Hotel or PMS destinations. | Routing transaction test | Pass |
| CREATE-06 | Script Name survives identically in every destination. | Cross-destination comparison | Pass |
| CREATE-07 | Reason For Creation survives identically in every destination. | Cross-destination comparison | Pass |
| CREATE-08 | Script Purpose / What It Does survives identically in every destination. | Cross-destination comparison | Pass |
| CREATE-09 | Created By and Notes / Follow-up remain optional with `N/A` semantics. | Workbook assertion | Pass |
| CREATE-10 | Exactly one index entry points to the selected Creation Running workbook and summarizes canonical Hotel IDs. | Index assertion | Pass |

## Schema rejection

Each row below must be repeated for Debugging, Script Editing, and Script
Creation and, where applicable, Running, Hotel, and PMS scopes.

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| SCHEMA-01 | Valid expected type/scope workbook is accepted. | Parameterized schema test | Pass |
| SCHEMA-02 | Wrong log type is rejected without alteration. | Parameterized schema test plus byte/fingerprint comparison | Pass |
| SCHEMA-03 | Wrong scope type is rejected without alteration. | Parameterized schema test plus byte/fingerprint comparison | Pass |
| SCHEMA-04 | Wrong scope ID is rejected without alteration. | Parameterized schema test plus byte/fingerprint comparison | Pass |
| SCHEMA-05 | Missing required header is rejected. | Parameterized schema test | Pass |
| SCHEMA-06 | Changed or reordered header is rejected. | Parameterized schema test | Pass |
| SCHEMA-07 | Metadata sheet/marker missing is rejected even when visible headers match. | Parameterized schema test | Pass |
| SCHEMA-08 | Unsupported schema version is rejected. | Parameterized schema test | Pass |
| SCHEMA-09 | Corrupt/non-workbook content is rejected. | Parameterized schema test | Pass |
| SCHEMA-10 | Missing/renamed/incompatible table or sheet structure is rejected. | Parameterized schema test | Pass |
| SCHEMA-11 | Existing incompatible history is never silently repaired, reset, or replaced. | Transaction plus fingerprint assertion | Pass |

## Log ID, state, and index

The ID cases repeat for `DEBUG`, `EDIT`, and `CREATE` unless otherwise stated.

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| SEQ-01 | First event of the day receives `001`. | Parameterized sequence test | Pass |
| SEQ-02 | Second event of the day receives `002`. | Parameterized sequence test | Pass |
| SEQ-03 | Switching Running workbook does not reset or reuse the sequence. | Parameterized sequence test | Pass |
| SEQ-04 | Creating a new Running workbook mid-day does not reset or reuse the sequence. | Parameterized sequence test | Pass |
| SEQ-05 | New service instance/app restart continues from durable state. | Parameterized sequence test | Pass |
| SEQ-06 | Multiple Hotel/PMS destinations consume one sequence only. | Parameterized transaction test | Pass |
| SEQ-07 | Current-date legacy TXT ID raises the next candidate above its maximum. | Transition sequence test | Pass |
| SEQ-08 | Existing LogIndex Excel ID raises the next candidate above its maximum. | Transition sequence test | Pass |
| SEQ-09 | Dedicated state, TXT, and index disagreement uses the highest observed value. | Sequence reconciliation test | Pass |
| SEQ-10 | Next calendar day begins its independent date sequence. | Parameterized sequence test | Pass |
| SEQ-11 | Different log types retain independent prefixes and sequences. | Sequence test | Pass |
| SEQ-12 | Preview does not create/update sequence state or reserve an ID. | Preview/state fingerprint test | Pass |
| SEQ-13 | Submit recomputes the actual ID when preview has become stale. | Overlapping-preview test | Pass |
| SEQ-14 | Reentrant/overlapping submissions cannot silently commit the same ID. | Synchronization test | Pass |
| SEQ-15 | Sequence-state JSON version/schema corruption produces a focused failure without reset. | State test | Pass |
| INDEX-01 | One logical event creates exactly one documentation index line. | Index count assertion | Pass |
| INDEX-02 | New line retains the seven pipe-separated fields. | Index format assertion | Pass |
| INDEX-03 | Saved File points to the selected Running `.xlsx`, not a history workbook. | Index path assertion | Pass |
| INDEX-04 | Existing historical index bytes/lines remain unchanged. | Prefix-byte comparison | Pass |
| INDEX-05 | Editing/Creation summary uses canonical `Hotel IDs` wording. | Index content assertion | Pass |
| INDEX-06 | No documentation event is added to `QAReportIndex.txt`. | QA-index fingerprint assertion | Pass |

## Mixed history and legacy preservation

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| LEGACY-01 | Synthetic historical Debugging TXT is byte-for-byte unchanged after a new Excel save. | Before/after byte comparison | Pass |
| LEGACY-02 | Synthetic historical Editing TXT is byte-for-byte unchanged after a new Excel save. | Before/after byte comparison | Pass |
| LEGACY-03 | Synthetic historical Creation TXT is byte-for-byte unchanged after a new Excel save. | Before/after byte comparison | Pass |
| LEGACY-04 | Old index lines remain byte-for-byte unchanged while one new line is appended. | Prefix-byte comparison | Pass |
| LEGACY-05 | New event creates no daily TXT file and adds no bytes to an existing one. | File enumeration/fingerprint assertion | Pass |
| LEGACY-06 | New Excel save succeeds in a root containing old TXT/index history. | Mixed-history transaction test | Pass |
| LEGACY-07 | Sequence continues above an applicable legacy TXT ID whose old index entry is absent. | Transition sequence test | Pass |
| LEGACY-08 | No TXT-to-Excel migration, rename, move, or deletion occurs. | Tree and byte comparison | Pass |

## Transaction, locking, rollback, and concurrency

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| TX-01 | Selected Running workbook locked/open: no artifact commits and focused Running message is returned. | Lock injection plus fingerprint comparison | Pass |
| TX-02 | Hotel history locked/open: no artifact commits and affected Hotel history is identified. | Lock injection plus fingerprint comparison | Pass |
| TX-03 | PMS history locked/open: no artifact commits and affected PMS history is identified. | Lock injection plus fingerprint comparison | Pass |
| TX-04 | Failure among multiple Hotel histories rolls back every already committed destination. | Fault injection | Pass |
| TX-05 | Index stage/write failure leaves workbooks and sequence state unchanged. | Fault injection | Pass |
| TX-06 | Sequence-state stage/write failure leaves workbooks and index unchanged. | Fault injection | Pass |
| TX-07 | Commit failure after at least one replacement restores all ordinary prior state. | Fault injection plus byte comparison | Pass |
| TX-08 | Existing source changed between staging and commit is detected; no silent overwrite occurs. | Fingerprint/concurrency injection | Pass |
| TX-09 | New destination appears between staging and commit; create-new collision is detected and not overwritten. | Concurrency injection | Pass |
| TX-10 | Staged workbooks are reopened and verified before commit. | Verification fault injection | Pass |
| TX-11 | Ordinary failure returns no success result/dialog. | SaveService/MainForm test | Pass |
| TX-12 | Rollback failure identifies manual-review paths and does not claim restoration. | Rollback fault injection | Pass |
| TX-13 | Cleanup warning is explicit without converting an otherwise complete commit into false failure/success ambiguity. | Cleanup fault injection | Not Run |
| TX-14 | Successful transaction leaves no normal temp or backup residue. | Directory enumeration | Pass |
| TX-15 | Failed transaction cleans normal temp/backups when safe and preserves evidence when manual review is required. | Directory enumeration | Pass |
| TX-16 | Running, all history rows, index, and state reflect one identical event after success. | Cross-artifact assertion | Pass |
| TX-17 | Submit double-click/reentrancy guard permits only one save call and one event. | MainForm/save fake test | Pass |

## MainForm behavior

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| UI-01 | Existing three-item log-type selector remains. | MainForm reflection/UI test | Pass |
| UI-02 | One reusable Running selector follows the active log type. | MainForm reflection/UI test | Pass |
| UI-03 | Debugging shows QA-style Hotel selector and canonical context. | MainForm reflection/UI test | Pass |
| UI-04 | Debugging shows exactly Error, Root Cause, Fix Applied, Created By, and Notes business/common inputs. | MainForm reflection/UI test | Pass |
| UI-05 | Editing shows manual Hotel ID(s), Script Name, Reason For Edit, Changes Made, and common inputs. | MainForm reflection/UI test | Pass |
| UI-06 | Creation shows manual Hotel ID(s), Script Name, Reason For Creation, Script Purpose, and common inputs. | MainForm reflection/UI test | Pass |
| UI-07 | Editing and Creation do not show the Hotel picker. | MainForm reflection/UI test | Pass |
| UI-08 | Switching types clears stale Hotel selection, raw Hotel IDs, business values, and preview. | MainForm state test | Pass |
| UI-09 | Switching types does not clear or cross-write workbook preferences. | MainForm/preferences test | Pass |
| UI-10 | Dynamic controls/event subscriptions do not accumulate across repeated switches. | Handler/control lifecycle test | Pass |
| UI-11 | Submit handler is wired exactly once. | Handler inspection | Pass |
| UI-12 | Submit is disabled and guarded while a save is active. | Reentrancy test | Pass |
| UI-13 | Failed save preserves useful form values for correction/retry. | MainForm test | Pass |
| UI-14 | Preview candidate does not imply reservation; actual Submit result displays the committed ID. | MainForm/service test | Pass |
| UI-15 | `Open Selected Log Workbook` opens the selected compatible Running workbook or gives a friendly unavailable message. | Manual/UI test | Manual Required |
| UI-16 | Open Logs Folder opens the configured documentation root. | Manual/UI test | Manual Required |
| UI-17 | Open Log Index opens the documentation `LogIndex.txt` or gives a friendly missing message. | Manual/UI test | Manual Required |
| UI-18 | Changing/resetting root refreshes root-bound metadata, workbook choices, and preferences without moving old files. | MainForm/root test | Manual Required |
| UI-19 | Privacy reminder remains present and unchanged in intent. | UI assertion | Pass |
| UI-20 | MainForm remains usable at its minimum size; dynamic fields scroll and the bottom action row remains legible. | Geometry/manual test | Manual Required |

## Approved Quick QA, Detailed QA, and legacy regressions

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| QA-QUICK-01 | Surface workbook create/append output and schema are unchanged. | Existing Quick QA regression | Pass |
| QA-QUICK-02 | Remembered Surface workbook behavior is unchanged. | Existing Quick QA regression | Pass |
| QA-QUICK-03 | Hotel `QuickQAHistory.xlsx` behavior is unchanged. | Existing Quick QA regression | Pass |
| QA-QUICK-04 | Quick QA staged transaction/rollback behavior is unchanged. | Existing Quick QA regression | Pass |
| QA-QUICK-05 | Quick findings, status, summary, and formatting are unchanged. | Full Quick QA regression | Pass |
| QA-DETAIL-01 | Detailed form workflow/readiness remains valid. | Existing Detailed QA regression | Pass |
| QA-DETAIL-02 | Detailed PDF generation remains valid. | Existing PDF regression/evidence | Pass |
| QA-DETAIL-03 | Paired Hotel/PMS save and rollback remain valid. | Existing Detailed save regression | Pass |
| QA-DETAIL-04 | Detailed QA index behavior remains valid. | Existing Detailed index regression | Pass |
| QA-DETAIL-05 | File ID behavior remains valid. | Existing Detailed regression | Pass |
| QA-DETAIL-06 | Email warning behavior remains unchanged. | Existing Detailed regression | Pass |
| QA-DETAIL-07 | Source/Rate/Market strategy behavior remains unchanged. | Existing Detailed regression | Pass |
| QA-DETAIL-08 | Detailed findings/statistics remain unchanged. | Full Detailed regression | Pass |
| QA-LEGACY-01 | Existing V1/legacy suite is updated only for intentionally retired new-TXT behavior and still protects prefixes, folders, old TXT readability, and index compatibility. | Updated legacy regression | Pass |
| QA-LEGACY-02 | Documentation saves do not contaminate Quick or Detailed QA indexes/settings/artifacts. | Isolation regression | Pass |

## Documentation and review completeness

| ID | Case and expected result | Evidence | Status |
|---|---|---|---|
| DOC-01 | README describes Excel Running/history behavior and historical TXT validity without rewriting release history. | Documentation review | Pass |
| DOC-02 | Design documents exact schemas, metadata, paths, parser/routing, sequence/index, transaction, locks, crash boundary, and QA isolation. | Documentation review | Pass |
| DOC-03 | Legacy compatibility document states byte-for-byte preservation and mixed-history behavior. | Documentation review | Pass |
| DOC-04 | Final report records commands/results accurately and makes no unsupported manual pass claim. | End-of-work review | Pass |
| DOC-05 | `git diff --stat` and `git status -sb` are recorded; no commit or push occurs without explicit authorization. | End-of-work review | Pass |
