# Version 2 QA Reports - Release Notes Draft

> **Draft only:** Version 2 has not been merged, tagged, published, or released. These notes describe the current `v2-qa-reports` candidate and must be updated from the final approved commit before release.

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
- Use safe percentage and denominator calculations, including zero-denominator cases.

The application does not read a spreadsheet, parse a hotel file, query a database, or populate checklist results automatically.

### Warnings, Failures, and custom-script handling

- Show Warnings and Failed Checks in separate areas.
- Create deterministic findings from failed checks, passed-check notes, and approved characteristic/statistics rules.
- Resolve each finding independently as Explained and Accepted or, where applicable, Handled by Custom Script.
- Preserve the finding's original severity. Handling a Failure does not downgrade it or change a Fail report to a warning status.
- Remove stale findings and reset a resolution if its underlying condition disappears and later returns.

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

Offscreen WinForms construction passed, but that evidence is not a direct visible-GUI test. Visible operation, 125%/150% scaling, prompts and dialogs, keyboard navigation, and a completed controlled pilot remain pending and block a broad release claim.

The final production rebuild without the temporary friend harness and the exact Debug and Release builds passed with 0 warnings and 0 errors. The publish wrapper succeeded and produced only `DocumentationLoggingDashboard.exe`, `DocumentationLoggingDashboard.pdb`, and `appsettings.json`; the published executable also remained running through its hidden smoke interval. The pre-existing ignored publish directory was restored byte-for-byte after verification.

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
- Visible GUI, Windows 125%/150% scaling, keyboard navigation, message boxes, overwrite-dialog behavior, and double-click behavior still require direct pilot verification.
- The published application is Windows x64, self-contained, and single-file, and PDF rendering relies on the Windows GDI build and an available Arial typeface.
- Phase 10 used a disposable friend harness, not a permanent automated test project.
- Existing unrelated V1 technical debt is not redesigned by Version 2.

## Future direction

Future phases may consider separately approved automation for safe file parsing, checklist assistance, integrations, history, or richer test infrastructure. None of those capabilities is part of this candidate. Any future automation must preserve manual review, privacy restrictions, V1 isolation, approved checklist meanings, and transactional storage guarantees.

## Release-readiness note

The current package is technically build/publish ready for a controlled pilot with the guardrails in `V2-Pilot-Guide.md`. It does not support stating that Version 2 is released. Merge, tag, and release remain subject to completed direct GUI checks, a successful controlled pilot unless explicitly waived, review of known issues, a clean final branch, and separate explicit approval.
