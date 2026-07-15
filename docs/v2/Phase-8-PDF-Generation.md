# Phase 8 PDF Report Generation

## Purpose and boundaries

Phase 8 adds a pure presentation layer that renders one complete, ready, in-memory `QaReport` into one reusable in-memory PDF `byte[]`. It uses the approved Phase 2 report model and the approved Phase 7 `QaReportValidationResult`; it does not introduce a report DTO, a second validation/status system, or a second findings collection.

The renderer does not save a file, select a path or final filename, show UI, access Hotel/PMS storage, append an index, decide overwrite behavior, or write paired destination copies. It does not recalculate status, create findings, change severity/resolution, or mutate the report. Those save and destination concerns remain Phase 9 work.

Phase 9 must call the renderer exactly once for a ready report and reuse the same returned byte array for both destination writes. It must not render one PDF independently for each destination.

## Repository baseline

- Required and active branch: `v2-qa-reports`.
- Approved Phase 7 implementation: `f61a5264336cb969b5310a2d6ba61434149f9bc6`.
- Approved Phase 8 starting tip: `305629412297469cd26000e8bfe1989747fa359f`.
- Actual starting and current working-tree commit: `305629412297469cd26000e8bfe1989747fa359f`.
- Approved Phase 2 commit: `7d94a27a4569f8ca521aeaa6c08da0e510fc4dc7`.
- Approved Phase 1 commit: `49beaa1f45700725d328ade215419254560cb406`.

The Phase 7, Phase 2, and Phase 1 ancestry checks passed before editing. The worktree was initially clean and recent history was inspected. Nothing was committed or pushed during Phase 8.

## PDF library decision

The only direct package added is:

```xml
<PackageReference Include="PDFsharp-MigraDoc-GDI" Version="6.2.4" />
```

Decision record:

- Library: PDFsharp and MigraDoc.
- Direct package/version: `PDFsharp-MigraDoc-GDI` `6.2.4`.
- License: MIT.
- Intended use: an internal commercial application.
- Platform: Windows-only GDI build in the existing Windows-only WinForms application.
- Framework evidence: the package exposes a `net10.0-windows7.0` target; the official PDFsharp documentation identifies the GDI build as Windows-only and able to use Windows-installed fonts; the PDFsharp release notes identify .NET 9 and .NET 10 support in 6.2.3 and later.
- Package behavior: the GDI package relies on Windows Forms/GDI+, which is acceptable at the package/runtime layer for this application. The QA PDF service and its PDF helpers do not reference WinForms controls, forms, `MessageBox`, or dialogs.
- External requirements: no browser engine, HTML renderer, Office/LibreOffice installation, external executable, server, online conversion service, separate font asset, or runtime download is part of PDF generation.

Official evidence reviewed:

- [NuGet: PDFsharp-MigraDoc-GDI 6.2.4](https://www.nuget.org/packages/PDFsharp-MigraDoc-GDI/6.2.4)
- [PDFsharp: choosing the GDI build](https://docs.pdfsharp.net/General/Overview/Choose-PDFsharp-version.html)
- [PDFsharp: version 6.2 compatibility notes](https://docs.pdfsharp.net/General/Overview/Whats-New.html)
- [Official PDFsharp repository and MIT license](https://github.com/empira/PDFsharp)

This is a technical assessment that the official MIT license is suitable for internal commercial use; it is not a legal review.

NuGet's package page exposes these direct package dependencies across its framework information: `PDFsharp-GDI`, `Microsoft.Extensions.Logging.Abstractions`, and `System.Security.Cryptography.Pkcs`. The actual restored `net10.0-windows` asset graph contained `PDFsharp-MigraDoc-GDI/6.2.4`, `PDFsharp-GDI/6.2.4`, `Microsoft.Extensions.Logging.Abstractions/8.0.3`, `Microsoft.Extensions.DependencyInjection.Abstractions/8.0.2`, and the SDK publish/build package `Microsoft.NET.ILLink.Tasks/10.0.9`. `System.Security.Cryptography.Pkcs` was not selected into this target's restored asset graph.

Rejected alternatives:

- QuestPDF: rejected because free commercial-use eligibility is revenue-dependent.
- iText: rejected because closed-source proprietary use requires AGPL compliance or commercial licensing.
- IronPDF: rejected because production use requires a commercial license and adds a heavier HTML/Chrome-oriented stack.
- PDFsharp/MigraDoc Core: rejected because this Windows-only application can use the GDI build and Windows-installed fonts without explicit font resolvers or bundled font binaries.

## Source organization and public API

PDF implementation details are under `QAReports/Pdf`; the reusable public service and exception are under `QAReports/Services`; readiness evidence is under `QAReports/Validation`.

The public rendering API is:

```csharp
public sealed class QaPdfGenerationService
{
    public byte[] GeneratePdf(
        QaReport report,
        QaReportValidationResult validationResult,
        DateTimeOffset generatedAt);
}
```

The public actionable exception API is:

```csharp
public sealed class QaPdfGenerationException : Exception
{
    public QaPdfGenerationException(string message);
    public QaPdfGenerationException(string message, Exception innerException);
}
```

The service renders through MigraDoc/PDFsharp into a `MemoryStream`, returns `stream.ToArray()`, and validates a `%PDF-` signature plus a plausible trailing `%%EOF` marker. It accepts the generation instant from the caller; it does not read the system clock. The same instant is used in the visible header and normalized to UTC for PDF creation/modification metadata.

There is no path parameter, stream owned by a caller, save dialog, `File.WriteAllBytes`, storage dependency, index dependency, or production filesystem side effect. The temporary harness alone wrote returned payloads outside the repository for verification.

## Readiness boundary and stale-readiness evidence

`QaReportValidationService` now creates a deterministic SHA-256 readiness fingerprint only when its existing validation has no blocking error. `QaReportValidationResult` retains its original public constructor for compatibility and adds an overload/property carrying `ValidatedReportFingerprint`. Results made through the older constructor intentionally contain no readiness evidence and cannot authorize PDF rendering.

The fingerprint uses invariant, length-delimited values and stable catalog ordering. It covers report identity/schema, Hotel/PMS/File Month, QA Date, Created By, original filename, notes, characteristics, every checklist value including notes/source/evaluation time, all statistics and explanations, and every finding field. `ReportStatus` is deliberately excluded because the Phase 7 form assigns it after validation.

Before rendering, the service rejects:

- null report or readiness result;
- an unready result, blocking errors, or missing calculated status;
- null or mismatched report status;
- blank Effective Created By;
- missing or stale readiness fingerprint;
- invalid characteristic/check/finding enum values;
- Not Evaluated, duplicate, unknown, missing, or applicability-inconsistent checklist results;
- blank or duplicate finding IDs; and
- missing, duplicate, unknown, or applicability-inconsistent statistics rows and missing required statistics groups.

The renderer does not call the status service or validation service and does not repair input. The caller must synchronize, validate, and assign the calculated status through the existing Phase 7 workflow. A relevant edit changes the fingerprint and requires fresh validation. Blank Created By continues to use the approved non-mutating Effective Created By fallback `InnoVarxi QA Team` from the readiness result.

## PDF structure and content rules

The document is US Letter portrait with compact grayscale tables, Arial typography, stable margins, and these sections in order:

1. Report header and summary
2. Information and Statistics
3. Raw File QA
4. DB QA
5. Warnings, only when Warning-severity findings exist
6. Failed Checks, only when Failure-severity findings exist
7. Notes, only when General Notes is nonblank

The header contains Overall Status, Hotel Name, Hotel ID, PMS, File Month, QA Date, Effective Created By, safe Original Filename when available, the caller-supplied PDF Generated instant, and Schema Version. QA Date and File Month remain distinct. Worksheet Name is not displayed.

Original filename display is deliberately non-storage-oriented. Leading/trailing whitespace is removed, both slash styles are treated as path separators, and only the final nonblank component is displayed. Null, blank, separator-only, or trailing-separator values omit the row. No directory portion, destination, or sanitized save filename is created.

Information and Statistics contains:

- File Information: Total Data Rows, Headers Present, Useful Headers, and Data Start Row.
- Warning Summary: Warning-severity findings only, with total count and ordered title/resolution rows; Failures are never folded into this summary.
- Blank Value Statistics and Broken Data Statistics as separate catalog-ordered tables with independent counts, denominators, percentages, and broken explanations.
- Name Statistics only for Separate First/Last Name mode.
- File Month Statistics with valid/inside/outside counts and stored percentages.
- Monetary Statistics with applicable Average Rate, Stay Value, and high-value details.
- Database Statistics with imported count and absolute raw/DB difference, plus a DB Issues table only when rejected or missing-required-value counts are positive. Existing checklist/finding context is displayed rather than inventing new conclusions.

Percentages use the stored Phase 7 values and render as `0.00%`; the PDF layer does not recalculate them.

Raw File QA and DB QA iterate the actual immutable `QaChecklistCatalog` in catalog and section order. The input gate requires exactly one result for every definition. Pass and Fail render directly, Not Applicable renders as `N/A` with an explanatory detail, and Not Evaluated is refused. Table heading rows repeat after page breaks.

Warnings and Failed Checks are filtered strictly by original severity, then deterministically ordered. Resolution never changes placement: handled Warnings remain under Warnings, while handled Failures remain under Failed Checks even when the overall status is Pass with Warnings. Finding blocks show severity/resolution, title, description, related check where available, source, resolution, optional script name, and optional resolution notes.

Finding order is catalog-related findings first in checklist order, then statistics findings by stable statistic group/field order, then other generated findings, then manual findings, with stable finding ID tie-breaking.

General Notes are added as literal plain text only. Markup-looking input is not interpreted. CR/LF variants are normalized only for display. Long unbroken runs are split at a stable 42-character display boundary to prevent clipping.

Every page footer contains Hotel ID, File Month, `Page x of y`, and this privacy reminder:

```text
Do not include guest names, guest emails, payment data, credentials, or full hotel-file contents in QA reports.
```

## Font, metadata, layout, and error handling

Arial is selected by family name through the Windows GDI build. No machine-specific font path, custom resolver, downloaded font, or font binary is included. `PredefinedFontsAndChars.ErrorFontName` is also set to Arial inside a small serialized rendering boundary because the library's font state is process-wide. A font/typeface/glyph resolution failure becomes an actionable `QaPdfGenerationException` naming the required font; other library failures retain their inner exception.

PDF metadata is intentionally limited to:

- Title: `Hotel QA Report`
- Subject: `Internal Hotel File QA`
- Author: Effective Created By
- Keywords: `Hotel QA, PMS, File Month`
- Creation/Modification date: caller-supplied generation instant normalized to UTC

The readiness fingerprint, report ID, original path components, guest/reservation data, and destination details are not metadata fields.

Section and subsection headings keep with following content. Short key/value and finding tables stay together where possible. Finding blocks do not split during the tested long-report layout. Checklist header rows repeat on continuation pages. Long text wraps, and the generated test set had no clipped text, overlap, missing section, blank trailing page, or incorrect page total.

## Verification record

### Builds and package restore

The untouched baseline command was:

```powershell
dotnet build DocumentationLoggingDashboard.sln
```

Result: exit code 0, build succeeded, 0 warnings, and 0 errors in 9.02 seconds.

The approved package restore succeeded after the workspace sandbox's inability to read the user-level NuGet configuration was handled with approved access. No other direct package was added.

Post-edit Debug and Release gates used:

```powershell
dotnet build DocumentationLoggingDashboard.sln
dotnet build DocumentationLoggingDashboard.sln -c Release
```

Both succeeded with exit code 0, 0 warnings, and 0 errors. The recorded Release run completed in 2.54 seconds. An earlier in-sandbox Release attempt failed only because access to the user-level `NuGet.Config` was denied; it was not treated as a successful build.

### Publish and published execution

The repository command was:

```powershell
powershell -ExecutionPolicy Bypass -File .\publish-windows.ps1
```

Its effective self-contained Windows x64 single-file publish succeeded with exit code 0. The first sandboxed attempt could not read `NuGet.Config`; the script prints its completion text even when its internal `dotnet publish` fails, so success was accepted only after an approved-access rerun showed restore, build, and publish output paths.

`PublishedApp\win-x64` existed before Phase 8. The current publish produced/retained:

- `DocumentationLoggingDashboard.exe` (single-file application)
- `DocumentationLoggingDashboard.pdb`
- `appsettings.json`
- the pre-existing `DocumentationLogs` directory

There was no loose PDFsharp/MigraDoc assembly, font file, PDF-related native library, browser executable, external renderer, or runtime download. The published executable launched hidden, remained running after four seconds, and was stopped after the smoke test. The pre-existing ignored publish baseline was backed up before this verification and restored afterward; publish output is not tracked or committed.

A second temporary self-contained Windows x64 single-file harness was published and executed. It built against the current production project, exercised the renderer, and completed all 110 assertions. This verified no missing PDFsharp/MigraDoc assembly, native dependency, font, external runtime, browser engine, or external executable in the deployed model.

### Disposable synthetic harness

The temporary harness was created under:

```text
%LOCALAPPDATA%\Temp\Phase8PdfVerification-019f66463fdf
```

It used the actual production models, catalogs, calculation/synchronization/validation services, and PDF service. It completed:

```text
PASS assertions=110
PASS extracted-pdf checks=191
```

The scenarios covered Pass; three Warning resolution states; active Failure plus a separate Warning; handled Failure; blank Created By; present, absent, long, Windows-path, Unix-path, mixed-path, separator-only, and trailing-separator original filenames; full statistics with and without DB Issues; N/A checklist output; long notes; special characters; 18 additional findings; and long pagination.

Refusal coverage included null report/result, blocking/unready result, null/mismatched status, Not Evaluated, duplicate/unknown/missing checklist results, duplicate finding ID, missing/stale fingerprint, and structurally invalid statistics. Successful payloads were nonempty, began with `%PDF-`, contained a trailing `%%EOF`, did not mutate JSON snapshots of the reports, and produced byte-identical temporary copies from the same returned byte array. No production file or QA index was written.

Text and metadata extraction performed 191 independent checks covering section order/inclusion, severity separation, handled-Failure placement, safe filenames, omitted paths, exact metadata, footer totals/privacy text on every page, percentage formatting, literal notes, repeated checklist headers, N/A output, and absence of report ID/fingerprint/private paths.

### Visual verification

Fourteen temporary PDFs totaling 37 pages were rendered with Poppler at 120 DPI and every rendered page was inspected. The set included all filename variants and every status/finding/long-report scenario. Inspection checked margins, headings, grayscale tables, line wrapping, safe long words, repeated checklist headers, Warning/Failure separation, handled Failure placement, footer text and page totals, finding-block pagination, notes, clipping/overlap, and blank trailing pages.

An initial visual pass found a File Month table and some finding blocks splitting poorly. Row padding/keep behavior was adjusted, the harness was rerun, all PDFs were rerendered, and the final pages were inspected again. The final rendered set showed no clipping, overlap, missing section, split finding block, incorrect page count, or blank trailing page.

### Regression evidence and limitations

Executed evidence:

- the whole solution built in Debug and Release with no warnings/errors;
- the self-contained published app launched successfully;
- the actual Phase 2 through Phase 7 model/service path generated and refused PDFs as documented;
- generation did not mutate the report or write a production PDF; and
- the published single-file harness exercised PDF generation successfully.

Source/final-diff inspection verifies that Program/MainForm startup, protected V1 models/services, V1 log definitions/storage/settings, Phase 3 metadata, Phase 4 Hotel/PMS behavior, Phase 5 form opening and dynamic checklists, Phase 6 resolution semantics, Phase 7 statistic calculations/findings/status, form-close behavior, and automatic-save behavior remain outside the changed file set. The only Phase 7 behavior extension is ready-result fingerprint evidence; existing completion/status rules are not replaced.

The human-interactive items in `V1-Regression-Checklist.md`, visible navigation into the QA Report form, metadata-management UI, dynamic control interaction, V1 Preview/Submit and shell-opening workflows, keyboard/accessibility behavior, multi-monitor DPI, and other-machine font availability were not manually exercised. They are supported here only by unchanged-source inspection and the full build. No claim is made that those interactive workflows were tested.

## Deferred Phase 9 work

Phase 8 intentionally adds no final filename, Save/Export button, save dialog, overwrite rule, atomic or paired write, Hotel/PMS destination, metadata path, QA index entry, retention policy, or production PDF output. Phase 9 owns those decisions and must render once, retain the returned byte array, and write that identical payload to both approved destinations.
