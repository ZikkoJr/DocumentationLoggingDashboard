# Version 2 QA Reports - Release Notes Draft

> **Production-candidate update (2026-07-20):** Final cleanup and synthetic regression now cover Blank/Broken semantics, denominator propagation, deferred text, layout at actual 125% scaling, blocked saves, overwrite replacement, byte-identical paired PDFs, QA index status, and protected V1 output. Real 100% and 150% DPI were unavailable and are not claimed. The separate immutable candidate is for independent review; `main` is not merged and no V2 tag or release exists. Debugging Log Hotel/PMS routing is deferred to V2.1.

> **Draft only:** Version 2 has not been merged, tagged, published, or released. These notes describe the current `v2-qa-reports` candidate and must be updated from the final approved commit before release.

> **Controlled-pilot boundary:** Final semantic, deferred-text, geometry, save/PDF/index, blocked-save, overwrite, actual-125% visible, and protected V1 regressions pass. Real 100%/150% DPI remains open. Do not use this draft to resume Scenario 3, replace the frozen package, modify the live Retry-01 root, merge, tag, or release.

> **Deferred-text correction:** V2 defers QA report free-text model commits until validation/action boundaries and batches the resulting refresh. Focused 8/8, synthetic PDF/index, actual-125% long-note, and protected V1 evidence pass. Real 100%/150% DPI and independent production-candidate review remain open. See `Pilot-Correction-Deferred-Text-Commit.md`.

## Overview

Version 2 adds a manual QA Report workflow alongside the existing V1 documentation-log workflows. It guides an operator through Hotel/PMS selection, file characteristics, Raw File QA and DB QA checklists, statistics, findings, validation, PDF generation, dual-location saving, and a dedicated QA Report index.

V1 Debugging Log, Script Editing Log, and Script Creation Log workflows remain available and keep their existing text files, daily naming, automatic IDs, settings, and `LogIndex.txt`. V2 uses its own `QAReports` storage tree and does not write QA report entries to the V1 index.

## What's included

### Hotel and PMS management

- Add and search PMS systems and Hotels using locally stored metadata.
- Maintain each Hotel's canonical PMS relationship and safe generated folder names.
- Refuse trimmed, case-insensitive duplicates, sanitization collisions, unsafe names, and Hotels that reference a missing PMS.
- Preserve corrupt or unsupported metadata for explicit review; normal loading does not silently reset it.

Metadata editing and deletion are not included in this version.

### Manual QA entry

- Complete the approved 28-item Raw File QA and DB QA catalog.
- Apply checklist items dynamically for separate-name columns, a full-name column, currency, one or two monetary columns, rejected records, and always-applicable checks.
- Record manual statistics, including separate Blank Value Statistics and Broken Data Statistics.
- Treat Total Data Rows as the operator-entered count of data-bearing rows, excluding headers/preamble; Headers Present and Data Start Row do not cause another subtraction. For 120 occupied physical rows with a row-1 header and data starting on row 2, enter `119`.
- Default each newly applicable Blank denominator automatically from Total Data Rows while permitting a per-row manual override and visible Auto reset.
- Default each newly applicable Broken denominator automatically from the matching Blank denominator minus Blank Count while permitting a separate per-row manual override and Auto reset.
- Keep Blank (missing cell) separate from Broken (invalid populated value). A positive exact percentage through and including 50% is a Warning; above 50% is a Failure; zero numerator or denominator produces no threshold finding.
- Use safe percentage and denominator validation without clamping entered counts, including zero-denominator and impossible Auto/manual states.
- Keep finding/checklist/report/statistics free text responsive as a local draft while typing; commit on focus validation or readiness/save/workflow boundaries, with one batched refresh where multiple drafts are flushed.

The application does not read a spreadsheet, parse a hotel file, query a database, or populate checklist results automatically.

### Warnings, Failures, and custom-script handling

- Show Warnings and Failed Checks in separate areas.
- Create deterministic findings from failed checks, passed-check notes, and approved characteristic/statistics rules.
- Resolve each finding independently as Explained and Accepted or, where applicable, Handled by Custom Script.
- Preserve the finding's original severity. A handled Failure remains under Failed Checks; when no Active Failure remains, the existing status service calculates Pass with Warnings.
- Remove stale findings and reset a resolution if its underlying condition disappears and later returns.
- Use separate deterministic Warning and Failure families for Blank and Broken thresholds. For a mapped Broken result above 50%, blank Notes on the current failing checklist row identify a threshold-only condition and suppress its generic generated checklist Failure. Nonblank checklist Notes document a separate contextual defect, so both the canonical statistics Failure and contextual checklist Failure remain; resolution state alone does not control this decision.

### Validation and status

- Block generation and saving until required report details, applicable checklist results, statistics, and finding resolutions are complete.
- Use `InnoVarxi QA Team` as the effective Created By value when the field is blank.
- Calculate one of three report statuses: Pass, Pass with Warnings, or Fail.
- Invalidate stale readiness after relevant report edits and revalidate before generation and saving.

### PDF output

- Generate one validated PDF payload in memory and reuse the same bytes for both destinations.
- Include report details, file characteristics, Raw File QA, DB QA, statistics, Warnings, Failed Checks, finding resolutions, notes, privacy text, and page numbering as applicable.
- Keep Blank Value Statistics and Broken Data Statistics in distinct tables.
- Wrap and paginate long checklist details and finding narratives using continuation rows.
- Exclude private local paths and full source-file contents from the report.

### Paired storage and overwrite protection

- Save identical PDF copies under the canonical Hotel and PMS folders.
- Build the logical report key from Hotel ID and File Month; QA Date is not part of overwrite identity.
- Detect existing structured filenames without substring matches between similar Hotel IDs.
- Require confirmation before replacing an existing logical report. Cancellation writes nothing.
- Commit the Hotel copy, PMS copy, and dedicated QA index as one logical transaction, with rollback and explicit manual-review reporting when recovery cannot complete.
- Maintain one current `QAReports\Index\QAReportIndex.txt` entry per logical key, with relative Hotel/PMS paths.

There is no cross-process save lock. Only one application process should write to a documentation root at a time.

## Phase 10 hardening

Final regression identified and corrected four narrowly scoped defects:

- the publish wrapper now returns a failing native publish exit code instead of printing false completion, and verifies that the expected executable was produced;
- QA index single-line fields now collapse all whitespace runs consistently;
- paired-save rollback restores moved Hotel and PMS backups in the true reverse of backup order; and
- oversized checklist details split into bounded continuation rows instead of entering the footer area.

Post-fix service/harness validation completed 368 assertions with 0 failures. Structural, extraction, and visual checks covered a 2-page Pass PDF, 4-page Pass with Warnings PDF, 3-page Fail PDF, 3-page custom-script-handled PDF, and 24-page long-content PDF. All 900 indexed long-content tokens were present; the lowest checklist content ended at 716.29 points while the footer began around 770.84 points, and no private local path appeared.

Those are historical Phase 10 results. Assertions and artifacts whose expected outcome depended on the prior Blank/Broken thresholds, denominator behavior, finding IDs, checklist Notes context, readiness/status, or affected PDF/index content were superseded as evidence for the P1 correction; replacement evidence is recorded below. Unaffected pagination, transaction, path, metadata, privacy, and V1 evidence may remain useful after explicit regression review.

Actual-Windows-125% visible no-save evidence now covers initial, maximized, 880x600, maximum-applicability, header-row, manual/Auto, and font-pressure states; permanent STA suites cover containment, mouse hit-testing, forward/reverse keyboard navigation, and scrolling. Real Windows 100% and 150% remain Not Run and continue to block a complete scaling claim.

For the correction, final Debug and Release solution and permanent-test builds passed with 0 warnings/errors. The isolated output's EXE, PDB, `appsettings.json`, and isolated `user-settings.json` were hashed; the visible application responded and closed normally with exit 0 without saving. A final audit found the frozen package, live Retry-01 root, V1 data, Git history, `main`, tag inventory, dependencies, and deployment model unchanged. The verification output is not an approved pilot package.

## Controlled-pilot P1 correction reviewed locally

The correction propagates the operator-entered data-only Total Data Rows through automatic Blank denominators and derives automatic Broken nonblank denominators from the matching Blank values, while preserving valid manual overrides. Header/data-start metadata do not cause an additional subtraction. It keeps the existing status calculator, save transaction, PDF structure, QA index format, storage paths, dependency set, and V1 contracts.

Current evidence includes semantic 7/7, deferred-text 8/8, STA geometry 7/7, actual-125% visible no-save, blocked-save, overwrite, paired PDF/index, protected V1, and rendered PDF review. The P2 Notes discriminator, direct PDF-order assertion, and real 100%/150% DPI remain open. The permanent test project is included in the solution.

## Privacy and local storage

V2 is locally stored. It does not add database access, email, upload, cloud storage, background monitoring, or automatic extraction from hotel files.

Enter only Hotel/PMS identifiers, aggregate statistics, and non-sensitive QA summaries. Do not enter guest names, guest contact information, payment information, credentials, production reservation records, raw rows, or full hotel-file contents. QA index paths are relative, and reusable documentation examples use synthetic data.

## Known limitations

- No automatic hotel-file parsing, spreadsheet reading, database integration, checklist population, QA automation, email, or upload.
- No metadata edit/delete interface, report-history browser, or incomplete-draft persistence.
- No cross-process save coordination; concurrent application processes must not share a writable documentation root.
- No automatic repair of malformed QA metadata or `QAReportIndex.txt`.
- An abnormal process or machine termination may leave a backup that requires manual review; every abandoned backup is not automatically cleaned.
- Real Windows permission/ACL behavior has not received complete environment-level coverage; artificial fault injection covered the transaction branches.
- Actual-125% visible no-save and STA mouse/keyboard behavior passed. Real 100%/150%, message boxes, overwrite-dialog behavior, and double-click behavior still require direct verification.
- The published application is Windows x64, self-contained, and single-file, and PDF rendering relies on the Windows GDI build and an available Arial typeface.
- Phase 10 used a disposable friend harness. The correction adds permanent focused semantic, STA geometry/interaction, and synthetic save/PDF/index coverage that passed in Debug and Release; the project is not included in the solution and must be run explicitly.
- Existing unrelated V1 technical debt is not redesigned by Version 2.

## Future direction

Future phases may consider separately approved automation for safe file parsing, checklist assistance, integrations, history, or richer test infrastructure. None of those capabilities is part of this candidate. Any future automation must preserve manual review, privacy restrictions, V1 isolation, approved checklist meanings, and transactional storage guarantees.

## Release-readiness note

The frozen package and current local correction are **not ready to resume the controlled pilot**. Replacement of the package and Scenario 3 resumption remain separately blocked after correction implementation, fresh evidence, independent review, and commit authorization. Merge, tag, and release additionally require a successful authorized pilot unless explicitly waived, a clean final approved branch, post-merge verification, and separate explicit approvals.
