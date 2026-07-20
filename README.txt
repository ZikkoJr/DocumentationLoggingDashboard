Documentation Logging Dashboard
===============================

Documentation Logging Dashboard is a C# WinForms desktop application for the
three established V1 plain-text documentation logs and the V2 QA Report
workflow. V1 entries retain their daily .txt files and LogIndex.txt. V2 creates
paired Hotel/PMS PDF reports and a separate QA Report index.

V2 QA Reports
-------------

Create QA Report supports Hotel/PMS metadata, a 28-item checklist, separate
Blank and Broken statistics, findings, readiness validation, paired PDF saving,
logical-report overwrite handling, and a dedicated QAReportIndex.txt.

Blank and Broken thresholds are independent:

- 0% creates no threshold finding.
- Above 0% through 50%, inclusive, creates a Warning.
- Above 50% creates a Failure.

Blank counts describe missing cells. Broken counts describe invalid populated
values, so blank cells do not fail populated-value checks. Automatic Blank
denominators follow Total Data Rows. Automatic Broken denominators follow the
matching Blank denominator minus Blank Count. Each denominator can be manually
overridden and reset to Auto.

Warning-only and handled-Failure reports save as Pass with Warnings. Active
Failures save as Fail. A handled Failure retains Failure severity and remains in
Failed Checks. Pending notes and other free text commit at validation/action
boundaries, including before readiness and save. The Hotel and PMS copies are
written from the same PDF bytes, and the QA index records the final status.

The planned V2.1 enhancement "Route Debugging Logs into the applicable Hotel
and PMS folders" is deferred. V2 does not change V1 Debugging Log routing.

Supported log types
-------------------

The preserved V1 workflow supports exactly three log types:

- Debugging Log
- Script Editing Log
- Script Creation Log

Required fields
---------------

Debugging Log:

- Hotel Name
- Hotel ID
- PMS
- Error shown on ticket
- Root cause
- Fix applied

Script Editing Log:

- Script Name
- Reason for edit
- Changes made
- Hotel Applied To

Script Creation Log:

- Script Name
- Reason for creation
- Script purpose / what it does
- Hotels that use this script

Optional fields
---------------

These fields can be left blank for any log type:

- Created By
- Notes / Follow-up

Blank optional fields are saved as N/A.

Automatic fields
----------------

The app automatically includes:

- Log ID
- Date/Time
- Log Type

Log ID format
-------------

Log IDs use the log type prefix, date, and a daily sequence number:

- DEBUG-yyyyMMdd-###
- EDIT-yyyyMMdd-###
- CREATE-yyyyMMdd-###

Examples:

- DEBUG-20260618-001
- EDIT-20260618-001
- CREATE-20260618-001

V1 output files
---------------

The V1 workflow writes plain .txt files only. Daily log files use:

yyyy-MM-dd_FileNameSuffix.txt

Examples:

- 2026-06-18_DebuggingLog.txt
- 2026-06-18_ScriptEditingLog.txt
- 2026-06-18_ScriptCreationLog.txt

Output folder structure
-----------------------

The selected documentation root folder contains this structure:

DocumentationLogs/
|
+-- DebuggingLogs/
+-- ScriptEditingLogs/
+-- ScriptCreationLogs/
+-- Index/
    +-- LogIndex.txt

Missing folders are created automatically when logs are saved or when the logs
folder is opened.

LogIndex.txt
------------

Each successful save appends one pipe-separated summary line to:

DocumentationLogs/Index/LogIndex.txt

Format:

Date | Time | Log ID | Log Type | Primary Item | Secondary Item | Saved File

Summary field mapping:

- Debugging Log: Hotel: {Hotel Name} / {Hotel ID} | PMS: {PMS}
- Script Editing Log: Script: {Script Name} | Hotel Applied To: {Hotel Applied To}
- Script Creation Log: Script: {Script Name} | Hotels That Use This Script: {Hotels that use this script}

Examples:

- 2026-06-18 | 10:24 AM | DEBUG-20260618-001 | Debugging Log | Hotel: Example Hotel / 12345 | PMS: Mews | C:\CompanyDocs\DocumentationLogs\DebuggingLogs\2026-06-18_DebuggingLog.txt
- 2026-06-18 | 10:31 AM | EDIT-20260618-001 | Script Editing Log | Script: MewsPMS.cs | Hotel Applied To: Example Hotel / 12345 | C:\CompanyDocs\DocumentationLogs\ScriptEditingLogs\2026-06-18_ScriptEditingLog.txt
- 2026-06-18 | 10:42 AM | CREATE-20260618-001 | Script Creation Log | Script: CustomHotelRevenueParser.cs | Hotels That Use This Script: Example Hotel / 12345 | C:\CompanyDocs\DocumentationLogs\ScriptCreationLogs\2026-06-18_ScriptCreationLog.txt

Changing the logs folder
------------------------

The dashboard shows the current documentation root folder.

Use Change Logs Folder to select a new root folder for future saves. The app
creates the expected folder structure in the selected location. Existing logs are
not moved when the folder changes.

Use Reset to Default Folder to return to the default DocumentationLogs location.

Settings priority
-----------------

The documentation root folder is resolved in this order:

1. user-settings.json
2. appsettings.json
3. DocumentationLogs default

Runtime folder changes are saved to user-settings.json beside the built app
instead of modifying appsettings.json. Relative paths are resolved from the
application base directory. Absolute paths are used as-is.

Privacy reminder
----------------

Do not enter guest names, emails, payment data, credentials, or full hotel files
into logs. Use ticket IDs, hotel IDs, script names, and summarized issues
instead.

How to build and run for development
------------------------------------

From the repository root, build the solution:

dotnet build DocumentationLoggingDashboard.sln

Run the app from Visual Studio, or run the built executable for development
only from:

DocumentationLoggingDashboard/bin/Debug/net10.0-windows/DocumentationLoggingDashboard.exe

Development publishing
----------------------

The script below creates a development publish. It is not the reviewed V2
production-candidate package.

From the repository root, run:

powershell -ExecutionPolicy Bypass -File .\publish-windows.ps1

The published app is created under:

PublishedApp\win-x64

The development executable can be launched from:

PublishedApp\win-x64\DocumentationLoggingDashboard.exe

For production-candidate use, follow the START-HERE.txt file in the separately
reviewed external folder named
DocumentationLoggingDashboard-V2-Production-<SHORTSHA>. Do not use the
repository, bin output, old V1 PublishedApp folder, or frozen pilot package as
the production working application folder. Release details are in
RELEASE_INSTRUCTIONS.txt.
