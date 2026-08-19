# Pilot Correction - Arrival Month Threshold

> **Evidence status (2026-07-23):** The focused implementation, baseline/final solution builds, publish, initial four-test Debug suite, representative PDF rendering, and paired-save/index evidence completed successfully. After review added explicit literal-zero, unrelated-edit, and evidence-path-guard assertions, the expanded test project still built, but Windows Smart App Control blocked the newly unsigned test DLL from loading. The final expanded Debug/Release reruns and the attempted V1 executable rerun are therefore `Blocked`, not `Pass`. No commit, push, corrected pilot package, pilot resumption, merge, tag, or release is claimed.

## 1. Previous behavior

The active Raw File catalog included:

```text
RAW.DATES.ARRIVAL_WITHIN_FILE_MONTH
```

Its display name was `Arrival Dates are within the selected File Month`. Every new report therefore received a manual `QaCheckResult` and Pass/Fail row for the condition. Validation coupled any positive outside count to that checklist result: outside dates required the checklist item to be Fail, and checklist finding generation supplied the Failure. File Month statistics were already visible and used Valid Arrival Date Count for both percentages.

This made a single outside date a manual checklist failure and did not express the approved tolerance threshold.

## 2. Corrected behavior

The Arrival Month condition is no longer an active checklist item. New reports do not create or require its result; the form creates no row, hidden control, or empty spacing; checklist finding generation does not receive it; and newly generated PDF checklist tables do not render it.

File Month statistics remain visible and editable. Internally valid statistics create exactly one Failure only when more than 30% of valid nonblank Arrival Dates are outside the selected File Month. Exactly 30% and every lower ratio create no Arrival/File Month Failure or Warning.

## 3. Retired checklist ID

The retired stable identifier is:

```text
RAW.DATES.ARRIVAL_WITHIN_FILE_MONTH
```

`QaChecklistIds.Raw.ArrivalWithinFileMonth` is retained as a retired compatibility constant. It is excluded from `QaChecklistCatalog.Definitions`, is not reused, and is not used to derive the new finding ID.

## 4. Active-catalog count change

| Catalog measure | Before | Corrected |
| --- | ---: | ---: |
| Raw File definitions | 21 | 20 |
| Database definitions | 7 | 7 |
| Total active definitions | 28 | 27 |
| Default applicable results | 22 | 21 |
| Default Not Applicable results | 6 | 6 |

Every remaining active definition must still have exactly one result in a new report. The recently corrected monetary spot-check applicability remains unchanged.

## 5. Compatibility decision

Repository inspection found no persisted `QaReport` draft serialization or reload path. JSON persistence is limited to Hotel/PMS metadata; saved QA output is PDF plus the dedicated index, and existing-report discovery reconstructs logical keys from filenames/index data rather than deserializing checklist results. Incomplete forms are explicitly in-memory only and are discarded on close.

Therefore no broad legacy-result tolerance or retired-ID deserialization boundary is added. The retired constant is retained to avoid reusing or obscuring the historical stable ID, but active report validation and PDF validation continue to require exactly one result for each active definition and reject duplicate or arbitrary unknown IDs.

Previously generated PDFs are immutable historical files and are not changed by this correction.

## 6. Valid-date denominator

The denominator is:

```text
QaFileMonthStatistics.ValidArrivalDateCount
```

It represents valid nonblank Arrival Dates. It is not Total Data Rows, total applicable file rows, the Arrival Date blank-statistics denominator, or all Arrival Date cells including blank/invalid values.

The File Month categories must satisfy:

```text
ValidArrivalDateCount >= 0
ArrivalDatesWithinFileMonth >= 0
ArrivalDatesOutsideFileMonth >= 0
ArrivalDatesWithinFileMonth <= ValidArrivalDateCount
ArrivalDatesOutsideFileMonth <= ValidArrivalDateCount
ArrivalDatesWithinFileMonth + ArrivalDatesOutsideFileMonth
    == ValidArrivalDateCount
```

Valid Arrival Date Count also remains bounded by the derived nonblank Arrival Date count.

## 7. Exact greater-than-30-percent implementation

The authoritative decision is made from counts, not a rounded display string:

```text
decimal outsideScaled =
    (decimal)ArrivalDatesOutsideFileMonth * 100m;
decimal thresholdScaled =
    (decimal)ValidArrivalDateCount * 30m;

failure = outsideScaled > thresholdScaled;
```

This preserves the strict contract:

```text
Outside percentage <= 30% -> no threshold Failure
Outside percentage > 30%  -> one threshold Failure
```

Examples:

- `3 / 10` is exactly 30% and does not fail.
- `4 / 13` is approximately 30.769230...% and fails.
- Display rounding never changes the decision.

## 8. Zero-denominator convention

For `ValidArrivalDateCount == 0`, both category counts must also be zero. The existing Phase 7 percentage convention remains `0`; calculation produces no exception, `NaN`, or infinity, and synchronization creates no threshold finding.

A positive inside or outside count with zero valid dates is a blocking validation error. Invalid statistics are workflow errors, not QA findings.

## 9. Finding ID and source

The deterministic finding ID is:

```text
STAT:FAIL:ARRIVAL_OUTSIDE_FILE_MONTH
```

The finding contract is:

```text
Severity = Failure
Resolution = Active when newly created
Source = Statistic
RelatedCheckId = null
```

Suggested title:

```text
More than 30% of Arrival Dates are outside the selected File Month
```

The description includes the selected File Month, valid count, inside count, outside count, and an unrounded or sufficiently precise outside percentage. It contains no guest data or source-file contents.

## 10. Finding lifecycle and custom-script behavior

The existing `QaFindingSynchronizationService` remains the only generated-finding reconciler.

- At/below 30% to above 30%: create a new Active Failure.
- Above 30% to at/below 30%: remove the desired finding and obsolete custom-script resolution state.
- Above 30% continuously: preserve the valid resolution/custom-script details, update the contextual description, and do not duplicate the finding.
- Above to below to above: the recurrence is a new Active Failure; the earlier handled state is not restored.

Handling by Custom Script never changes Failure severity. With no other Active Failure, the existing status rule produces Pass with Warnings; an Active occurrence produces Fail.

## 11. Production files changed

The final focused production diff contains seven files:

- `DocumentationLoggingDashboard/QAReports/Definitions/QaChecklistCatalog.cs` - remove the retired definition from the active catalog.
- `DocumentationLoggingDashboard/QAReports/Definitions/QaChecklistIds.cs` - retain and mark the stable identifier as retired without disruptive warnings.
- `DocumentationLoggingDashboard/QAReports/Definitions/QaFindingIds.cs` - add the deterministic statistics Failure ID.
- `DocumentationLoggingDashboard/QAReports/Forms/QaReportForm.cs` - update the explicit active-definition count guard from 28 to 27 and immediately refresh month-context finding descriptions when File Month changes; checklist construction remains catalog-driven.
- `DocumentationLoggingDashboard/QAReports/Services/QaFindingSynchronizationService.cs` - create and lifecycle-manage the exact threshold Failure through the existing desired-finding merge.
- `DocumentationLoggingDashboard/QAReports/Services/QaReportValidationService.cs` - remove checklist coupling and enforce File Month count integrity/finding consistency.
- `DocumentationLoggingDashboard/QAReports/Services/QaStatisticsCalculationService.cs` - centralize internally valid File Month categorization and the exact unrounded greater-than-30-percent comparison for synchronization and validation.

The focused test diff contains:

- `tests/DocumentationLoggingDashboard.GeometryTests/Program.cs` - add the isolated `--arrival-month-only` suite route.
- `tests/DocumentationLoggingDashboard.GeometryTests/QaArrivalMonthRegressionTests.cs` - add catalog, threshold, validation, lifecycle, form, PDF, paired-save, and index coverage.

The form and PDF checklist consume `QaChecklistCatalog.Definitions`; no hidden form row or PDF redesign was added.

## 12. Documentation files changed

The correction updates:

- `README.txt`
- `RELEASE_INSTRUCTIONS.txt`
- `docs/v2/Phase-2-Domain-Contract.md`
- `docs/v2/Phase-5-QA-Report-Form.md`
- `docs/v2/Phase-6-Findings-And-Custom-Script.md`
- `docs/v2/Phase-7-Statistics-Validation-Status.md`
- `docs/v2/Phase-8-PDF-Generation.md`
- `docs/v2/Phase-9-Paired-Saving-And-Index.md`
- `docs/v2/Phase-10-Final-Validation-And-Release-Readiness.md`
- `docs/v2/V2-End-to-End-Test-Matrix.md`
- `docs/v2/V2-Pilot-Guide.md`
- `docs/v2/V2-Production-Readiness.md`
- `docs/v2/V2-Release-Checklist.md`
- `docs/v2/V2-Release-Notes-Draft.md`
- this correction record

`RELEASE_INSTRUCTIONS.txt` contained no old rule/count statement, but received the required hold notice so the prior `3842298` package is not mistaken for corrected evidence. `V1_TEST_CHECKLIST.txt` was inspected and contained no Arrival Month rule or active-checklist-count statement requiring a focused edit.

## 13. Baseline and final build results

| Gate | Command | Result |
| --- | --- | --- |
| Baseline Debug at `3842298` | `dotnet build DocumentationLoggingDashboard.sln -c Debug` | Pass; exit 0; 0 warnings; 0 errors |
| Final Debug | `dotnet build DocumentationLoggingDashboard.sln -c Debug` | Pass; exit 0; 0 warnings; 0 errors |
| Final Release | `dotnet build DocumentationLoggingDashboard.sln -c Release` | Pass; exit 0; 0 warnings; 0 errors |
| Windows publish | `powershell -ExecutionPolicy Bypass -File .\publish-windows.ps1` | Pass after approved NuGet access; output `PublishedApp\win-x64`; `win-x64`, self-contained, and `PublishSingleFile` confirmed |
| Expanded focused-test build | Debug and Release test-project builds | Pass; the later runtime loads were blocked by Smart App Control as recorded below |

Published inventory:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `appsettings.json` | 53 | `D8907CDD3C2440BD404B7C1A23837AAAD4449F428AE64E2A1FFF038DD6FA2E03` |
| `DocumentationLoggingDashboard.exe` | 118,648,213 | `DF76E602B7FC4EB0FA7B7422E25D40584DDDCF1C849B6BA9FDE210074386E016` |
| `DocumentationLoggingDashboard.pdb` | 122,704 | `8DC185A24F57F4668B98DC1504768E5BBE92E1EF7B4A6B379392D483D603C976` |

The publish output contained no loose DLL, `.deps.json`, or `.runtimeconfig.json`. The project/package diff was empty, and `dotnet list DocumentationLoggingDashboard/DocumentationLoggingDashboard.csproj package` still listed only direct package `PDFsharp-MigraDoc-GDI` `6.2.4`.

## 14. Tests executed

The first Debug `--arrival-month-only` run completed all four grouped tests:

```text
[PASS] All 4 Arrival Month correction tests passed.
```

It preserved synthetic evidence at:

```text
C:\Users\Gabriel Ramdeholl\AppData\Local\Temp\arr-24de4736d3bb
```

That successful run covered:

- 29 unique active definitions, 22 Raw and 7 DB, all retired IDs absent, and monetary spot-check applicability unchanged;
- new-report result/control coverage and retired-row absence;
- 0%, 20%, exactly 30%, 31%, `3/10`, and `4/13`, including sufficiently precise fractional description text;
- zero denominator and negative/category-above-valid/category-sum invalid states;
- retired/unknown result rejection at readiness and PDF boundaries;
- Active/handled status, continuous above-threshold count changes, description refresh, removal, recurrence, and no duplication;
- four ready PDF/save/index scenarios and byte-identical Hotel/PMS copies.

Review then requested additional literal-zero, unrelated-report-edit, and external evidence-path-guard assertions. Those additions compile in the expanded Debug and Release test builds. Their runtime reruns did not start: Windows Smart App Control Code Integrity policy `{0283ac0f-fff1-49ae-ada1-8a933130cad6}` blocked loading the newly unsigned test DLL, with Code Integrity events `3033` and `3077`. The same block occurred on escalated Debug/Release attempts.

Therefore:

- the earlier 4/4 execution is valid evidence for the source/test state that ran;
- the reviewer-added assertions are build/source-inspection evidence only and are not claimed as executed;
- there is no final expanded-suite rerun pass;
- the V1 executable regression retry was blocked by the same policy after the test DLL hash changed. Production V1 files have no diff and both solution builds pass, but this correction does not claim a fresh executable V1 regression pass.

## 15. Representative PDF verification

The four representative Hotel PDFs produced 2, 2, 3, and 3 pages. All 10 pages were rendered and visually inspected.

| File Month / case | Status | Pages | Bytes | SHA-256 |
| --- | --- | ---: | ---: | --- |
| `2026-01` / 20% outside | Pass | 2 | 99,257 | `E7C832043A6F33A2B56791361DA37CA6F84BA388E278686BB4CD43646BF14493` |
| `2026-02` / exactly 30% | Pass | 2 | 98,789 | `34DA14DB93C590B98D261614DFDEED2DE22CBC25308CDA17CAC06FE9E2FE1953` |
| `2026-03` / 31% Active | Fail | 3 | 101,622 | `5FEC73919A5143FE8799E8131A89B7281A242D826C10801663CE66657B50649D` |
| `2026-04` / 31% handled | Pass with Warnings | 3 | 102,407 | `B408673728A32005FDA56CFACE3C223E2B8939FD0DD38F7273DA2D44A53E51A2` |

Inspection confirmed:

- File Month Statistics retained the correct valid/inside/outside counts and percentages.
- The retired checklist row was absent with no empty row.
- No Arrival/File Month finding appeared at 20% or exactly 30%.
- Each above-threshold PDF contained exactly one Statistic-sourced Failed Check.
- The handled PDF retained Failure severity and displayed the custom-script details.
- There was no clipping, overlap, black box, blank trailing page, or layout redesign.

## 16. Paired-save and QA-index verification

The preserved synthetic root contains four Hotel PDFs and four PMS PDFs. For each case, both destination files exactly matched the generated payload and shared the size/hash in the table above.

`QAReportIndex.txt` contained exactly four entries:

| File Month | Index status |
| --- | --- |
| `2026-01` | Pass |
| `2026-02` | Pass |
| `2026-03` | Fail |
| `2026-04` | Pass with Warnings |

The paired destinations contained exactly one current copy per logical key, all statuses matched the validated/saved status, and the synthetic root contained zero `.tmp`, `.qa-tmp`, `.qa-rollback`, `.bak`, or partial transaction artifacts.

The focused Arrival Month run used four fresh logical keys; it did not re-execute overwrite cancellation/confirmation. Existing overwrite production code has no focused diff, but overwrite behavior remains a required pilot repetition rather than a newly claimed test result.

Expected status solely from this condition:

| Scenario | Expected status |
| --- | --- |
| 0%, 20%, or exactly 30%; no other findings | Pass |
| Above 30%; Active Failure | Fail |
| Above 30%; handled Failure and no other Active Failure | Pass with Warnings |

## 17. Pilot scenarios requiring repetition

Do not resume pilot execution until the correction is committed with explicit authorization, independently reviewed, packaged with separate authorization, and pilot resumption is separately authorized.

When authorized, repeat:

1. 20% outside: no finding, Pass if otherwise clean.
2. Exactly 30% outside: no finding, Pass if otherwise clean.
3. 31% outside: one Active statistics Failure, Fail.
4. Above threshold handled by Custom Script: Failure severity retained, Pass with Warnings when otherwise clean.
5. `31% -> 30% -> 31%`: removal clears handled state and recurrence starts Active.
6. Continuous `31% -> 35%`: resolution preserved, description updated, no duplicate.
7. Zero denominator and invalid totals: safe zero behavior and readiness blocking.
8. Newly generated checklist/PDF inspection: retired row absent and remaining conditional rows, including the monetary spot check, unchanged.
9. Paired-save, overwrite, byte identity, and QA-index status verification.
10. Rerun the reviewer-expanded focused Debug and Release suites and the correction-specific V1 executable regression in an approved environment where the test DLL is trusted; retain the Smart App Control block as a limitation until that occurs.

## 18. Starting pilot baseline

The actual starting source commit is:

```text
38422987180e124f22c8a836cc59fa858243251b
```

It is `v2-qa-reports` commit `Clarify final artifact hash authority` and was observed at `0 behind / 0 ahead` of `origin/v2-qa-reports` before this correction. The external production manifest at:

```text
C:\Users\Gabriel Ramdeholl\Desktop\DocumentationLoggingDashboard-V2-Production-3842298\production-manifest.txt
```

records that exact full/short commit on lines 9-13. Its observed SHA-256 during the baseline audit was:

```text
C16DEA7EBD5FC13E190135916A65D1010A483CF386056C1A0040F8B42A2B3F26
```

The previously approved frozen-pilot repository baseline remains:

```text
d8f2eeeab6d7dea83c9c2924ccb2987645fc69cc
```

History from that checkpoint is linear:

```text
f6d7653  Fix V2 QA statistics and deferred text commits
124df57  Prepare V2 production release candidate
3842298  Clarify final artifact hash authority
```

The prior external production candidate awaits independent review and does not authorize pilot resumption. It also predates this Arrival Month correction and must not be used as corrected evidence.

## 19. Proposed new pilot baseline

```text
PENDING - no proposed new pilot-baseline commit exists.
```

No commit or push is authorized or claimed. After implementation, verification, explicit commit authorization, commit creation, independent review of the exact commit, separate package authorization, and separate pilot-resumption authorization, record the proposed full SHA here. Until then, `3842298` is only the correction's starting source commit and `d8f2eee` remains the previous frozen-pilot baseline.

Recommended future commit message, if later authorized:

```text
Retire arrival-month check and enforce 30 percent failure threshold
```

## Locked correction statements

```text
RAW.DATES.ARRIVAL_WITHIN_FILE_MONTH is retired from the active checklist.

File Month statistics remain visible and editable.

The denominator is Valid Arrival Date Count.

Exactly 30% outside does not fail.

More than 30% outside creates exactly one statistics-sourced Failure.

No checklist Failure is created for this condition.

Handled custom-script Failures retain Failure severity and follow the approved status rules.
```
