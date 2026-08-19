using System.Text.Json;
using DocumentationLoggingDashboard.QAReports.Definitions;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;
using DocumentationLoggingDashboard.QAReports.Validation;

namespace DocumentationLoggingDashboard.GeometryTests;

internal static class QaBlankBrokenStatisticsRegressionTests
{
    private static readonly QaStatisticsCalculationService CalculationService = new();
    private static readonly QaFindingSynchronizationService FindingService = new();
    private static readonly QaReportValidationService ValidationService = new();

    public static int RunAll(TextWriter output)
    {
        (string Name, Action Body)[] tests =
        [
            ("Blank and Broken threshold boundaries", TestThresholdBoundaries),
            ("Blank/Broken separation and populated-name semantics", TestSeparationAndNames),
            ("header rows remain excluded from Total Data Rows", TestHeaderRowsRemainExcluded),
            ("exact Auto/manual propagation and fresh applicability", TestPropagationAndApplicability),
            ("finding identity and resolution lifecycle", TestFindingLifecycle),
            ("validation rejects invalid counts, modes, and contradictions", TestInvalidStates),
            ("compatibility defaults, status, and mode-only staleness", TestCompatibilityStatusAndStaleness)
        ];

        int failed = 0;
        output.WriteLine($"Blank/Broken semantic harness: {tests.Length} tests");

        foreach ((string name, Action body) in tests)
        {
            output.WriteLine($"[RUN ] {name}");

            try
            {
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
                ? $"[PASS] All {tests.Length} Blank/Broken semantic tests passed."
                : $"[FAIL] {failed} of {tests.Length} Blank/Broken semantic tests failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void TestThresholdBoundaries()
    {
        foreach ((int count, QaFindingSeverity? expected) in new[]
        {
            (0, (QaFindingSeverity?)null),
            (1, QaFindingSeverity.Warning),
            (50, QaFindingSeverity.Warning),
            (51, QaFindingSeverity.Failure)
        })
        {
            QaReport blankReport = CreateCompleteReport();
            QaBlankValueStatistic blank = Blank(blankReport, QaStatisticFieldIds.Email);
            blank.BlankCount = count;
            Synchronize(blankReport);
            AssertThresholdFinding(
                blankReport,
                QaStatisticFieldIds.Email,
                isBlank: true,
                expected);

            QaReport brokenReport = CreateCompleteReport();
            QaBrokenDataStatistic broken = Broken(
                brokenReport,
                QaStatisticFieldIds.Email);
            broken.BrokenValueCount = count;
            Synchronize(brokenReport);
            AssertThresholdFinding(
                brokenReport,
                QaStatisticFieldIds.Email,
                isBlank: false,
                expected);
        }

        Check(
            QaStatisticsCalculationService.ClassifyThreshold(1, int.MaxValue)
                == QaFindingSeverity.Warning,
            "A mathematically positive rate that displays as 0.00% must remain a Warning.");
        Check(
            QaStatisticsCalculationService.CalculatePercentage(10_001, 20_001) == 50.00m,
            "The exact-boundary regression requires a displayed 50.00% value.");
        Check(
            QaStatisticsCalculationService.ClassifyThreshold(10_001, 20_001)
                == QaFindingSeverity.Failure,
            "A mathematically greater-than-50% rate must be Failure even when display rounds to 50.00%.");
    }

    private static void TestSeparationAndNames()
    {
        QaReport report = CreateCompleteReport();
        QaBlankValueStatistic emailBlank = Blank(report, QaStatisticFieldIds.Email);
        QaBrokenDataStatistic emailBroken = Broken(report, QaStatisticFieldIds.Email);
        emailBlank.BlankCount = 20;
        emailBroken.BrokenValueCount = 10;
        Synchronize(report);

        Check(emailBlank.BlankPercentage == 20m, "Expected independent Email Blank percentage 20.00%.");
        Check(emailBroken.TotalApplicableNonblankValues == 80, "Expected Email Broken denominator 80.");
        Check(emailBroken.BrokenDataPercentage == 12.5m, "Expected independent Email Broken percentage 12.50%.");
        Check(
            report.Findings.Any(finding => finding.FindingId ==
                QaFindingIds.WarningForBlankStatistic(QaStatisticFieldIds.Email)),
            "The Email Blank Warning is missing.");
        Check(
            report.Findings.Any(finding => finding.FindingId ==
                QaFindingIds.WarningForBrokenStatistic(QaStatisticFieldIds.Email)),
            "The Email Broken Warning is missing.");
        Check(
            report.Findings.All(finding => finding.Severity != QaFindingSeverity.Failure),
            "Separated 20% Blank / 12.5% Broken statistics must not create a Failure.");

        QaReport fullName = CreateCompleteReport(QaNameColumnMode.FullName);
        QaBlankValueStatistic fullBlank = Blank(fullName, QaStatisticFieldIds.FullName);
        QaBrokenDataStatistic fullBroken = Broken(fullName, QaStatisticFieldIds.FullName);
        QaCheckResult fullNameCheck = CheckResult(
            fullName,
            QaChecklistIds.Raw.FullNameValuesValid);
        fullBlank.BlankCount = 20;
        Synchronize(fullName);
        Check(fullNameCheck.Status == QaCheckStatus.Pass, "Blank Full Name cells must not Fail the populated-value check.");
        Check(
            HasFinding(fullName, QaFindingIds.WarningForBlankStatistic(QaStatisticFieldIds.FullName)),
            "Full Name Case A requires one Blank threshold Warning.");
        Check(
            !HasAnyBrokenThresholdFinding(fullName, QaStatisticFieldIds.FullName),
            "Full Name Case A must not create a Broken finding.");

        fullBroken.BrokenValueCount = 10;
        Synchronize(fullName);
        Check(fullNameCheck.Status == QaCheckStatus.Pass, "Broken Full Name at or below 50% may remain Pass.");
        Check(
            HasFinding(fullName, QaFindingIds.WarningForBrokenStatistic(QaStatisticFieldIds.FullName)),
            "Full Name Case B requires a Broken Warning.");
        QaReportValidationResult caseB = Validate(fullName);
        Check(caseB.IsReady, "Full Name Case B should be ready with a passing populated-value check.");
        Check(caseB.CalculatedStatus == QaReportStatus.PassWithWarnings, "Full Name Case B should be Pass with Warnings.");

        fullBroken.BrokenValueCount = 41;
        Synchronize(fullName);
        Check(
            HasFinding(fullName, QaFindingIds.FailureForBrokenStatistic(QaStatisticFieldIds.FullName)),
            "Full Name Case C requires the canonical Broken Failure.");
        Check(
            !Validate(fullName).IsReady,
            "Full Name Case C must reject the contradiction while the related check is Pass.");
        fullNameCheck.Status = QaCheckStatus.Fail;
        fullNameCheck.Notes = null;
        Synchronize(fullName);
        Check(
            !HasFinding(fullName, QaFindingIds.FailureForCheck(QaChecklistIds.Raw.FullNameValuesValid)),
            "The mapped Full Name threshold must suppress the duplicate generic checklist Failure.");
        Check(
            fullName.Findings.Count(finding => finding.Severity == QaFindingSeverity.Failure) == 1,
            "Full Name Case C must contain one canonical Failure for the threshold issue.");
        QaReportValidationResult caseC = Validate(fullName);
        Check(caseC.IsReady && caseC.CalculatedStatus == QaReportStatus.Fail, "Coherent Full Name Case C should be ready with Fail status.");

        QaFinding canonicalFullNameFailure = Finding(
            fullName,
            QaFindingIds.FailureForBrokenStatistic(QaStatisticFieldIds.FullName));
        fullName.FileCharacteristics.IsCustomScriptSupportAvailable = true;
        canonicalFullNameFailure.Resolution = QaFindingResolution.HandledByCustomScript;
        canonicalFullNameFailure.CustomScriptName = "SyntheticFullNameRepair";
        canonicalFullNameFailure.ResolutionNotes =
            "The aggregate threshold was reviewed independently.";
        Synchronize(fullName);
        Check(
            canonicalFullNameFailure.Resolution == QaFindingResolution.HandledByCustomScript,
            "The mapped canonical threshold resolution must survive synchronization.");
        Check(
            !HasFinding(fullName, QaFindingIds.FailureForCheck(QaChecklistIds.Raw.FullNameValuesValid)),
            "Mapped threshold de-duplication must not depend on the canonical finding resolution.");

        fullNameCheck.Notes =
            "A separate populated-value defect was confirmed independently of the threshold count.";
        Synchronize(fullName);
        Check(
            HasFinding(fullName, QaFindingIds.FailureForCheck(QaChecklistIds.Raw.FullNameValuesValid)),
            "A contextualized checklist Failure must remain separate from the canonical threshold Failure.");
        Check(
            fullName.Findings.Count(finding => finding.Severity == QaFindingSeverity.Failure) == 2,
            "A threshold Failure and a separately documented checklist Failure must both remain auditable.");
        Check(
            Validate(fullName).IsReady,
            "A documented separate checklist Failure must remain validation-ready beside the threshold Failure.");

        fullNameCheck.Notes = null;
        Synchronize(fullName);
        Check(
            !HasFinding(fullName, QaFindingIds.FailureForCheck(QaChecklistIds.Raw.FullNameValuesValid)),
            "Removing separate checklist context must restore threshold-only de-duplication.");

        QaReport firstNameEmail = CreateCompleteReport();
        QaBrokenDataStatistic firstBroken = Broken(
            firstNameEmail,
            QaStatisticFieldIds.FirstName);
        firstBroken.BrokenValueCount = 2;
        Synchronize(firstNameEmail);
        Check(
            Blank(firstNameEmail, QaStatisticFieldIds.FirstName).BlankCount == 0,
            "Synthetic email-in-name counts must not be copied into Blank statistics.");
        Check(
            HasFinding(firstNameEmail, QaFindingIds.WarningForBrokenStatistic(QaStatisticFieldIds.FirstName)),
            "A small synthetic email-in-First-Name count must create a Broken Warning.");
        Check(
            CheckResult(firstNameEmail, QaChecklistIds.Raw.FirstNameValuesValid).Status == QaCheckStatus.Pass,
            "A Broken Warning must permit the First Name validity check to remain Pass.");

        firstBroken.BrokenValueCount = 51;
        Synchronize(firstNameEmail);
        Check(
            HasFinding(firstNameEmail, QaFindingIds.FailureForBrokenStatistic(QaStatisticFieldIds.FirstName)),
            "A synthetic email-in-First-Name count above 50% must create a Broken Failure.");
        Check(
            !Validate(firstNameEmail).IsReady,
            "A mapped email-in-First-Name Broken Failure must require the related validity check to Fail.");
        QaCheckResult firstNameValidity = CheckResult(
            firstNameEmail,
            QaChecklistIds.Raw.FirstNameValuesValid);
        firstNameValidity.Status = QaCheckStatus.Fail;
        firstNameValidity.Notes = null;
        Synchronize(firstNameEmail);
        Check(
            !HasFinding(firstNameEmail, QaFindingIds.FailureForCheck(QaChecklistIds.Raw.FirstNameValuesValid)),
            "The threshold-only First Name validity Failure must not duplicate the canonical statistic Failure.");
        Check(
            Validate(firstNameEmail).IsReady,
            "A coherent synthetic email-in-First-Name Broken Failure must be ready without guest-level data.");

        QaReport lastNameEmail = CreateCompleteReport();
        QaBrokenDataStatistic lastBroken = Broken(
            lastNameEmail,
            QaStatisticFieldIds.LastName);
        lastBroken.BrokenValueCount = 51;
        QaCheckResult lastNameValidity = CheckResult(
            lastNameEmail,
            QaChecklistIds.Raw.LastNameValuesValid);
        lastNameValidity.Status = QaCheckStatus.Fail;
        lastNameValidity.Notes = null;
        Synchronize(lastNameEmail);
        Check(
            HasFinding(lastNameEmail, QaFindingIds.FailureForBrokenStatistic(QaStatisticFieldIds.LastName)),
            "A synthetic email-in-Last-Name count above 50% must create the canonical Broken Failure.");
        Check(
            !HasFinding(lastNameEmail, QaFindingIds.FailureForCheck(QaChecklistIds.Raw.LastNameValuesValid)),
            "The threshold-only Last Name validity Failure must not duplicate the canonical statistic Failure.");
        Check(
            Validate(lastNameEmail).IsReady,
            "A coherent synthetic email-in-Last-Name Broken Failure must be ready without guest-level data.");
    }

    private static void TestPropagationAndApplicability()
    {
        QaReport report = CreateCompleteReport();
        report.Statistics.FileInformation.TotalDataRows = 100;
        report.Statistics.Database.ImportedRecordCount = 100;
        report.Statistics.FileMonth.ValidArrivalDateCount = 100;
        report.Statistics.FileMonth.ArrivalDatesWithinFileMonth = 100;
        Synchronize(report);

        Check(
            report.Statistics.BlankValues.All(row =>
                row.UseAutomaticTotalApplicableRows
                && row.TotalApplicableRows == 100),
            "Step 2: every applicable Auto Blank denominator must become 100.");
        AssertAutomaticBrokenRows(report, "Step 3");

        QaBlankValueStatistic emailBlank = Blank(report, QaStatisticFieldIds.Email);
        QaBrokenDataStatistic emailBroken = Broken(report, QaStatisticFieldIds.Email);
        emailBlank.BlankCount = 12;
        Synchronize(report);
        Check(emailBroken.TotalApplicableNonblankValues == 88, "Step 5: Email Broken denominator must become 88.");

        report.Statistics.FileInformation.TotalDataRows = 120;
        report.Statistics.Database.ImportedRecordCount = 120;
        report.Statistics.FileMonth.ValidArrivalDateCount = 108;
        report.Statistics.FileMonth.ArrivalDatesWithinFileMonth = 108;
        Synchronize(report);
        Check(emailBlank.TotalApplicableRows == 120, "Step 7: Auto Blank denominator must become 120.");
        Check(emailBroken.TotalApplicableNonblankValues == 108, "Step 8: Email Broken denominator must become 108.");

        emailBlank.UseAutomaticTotalApplicableRows = false;
        emailBlank.TotalApplicableRows = 90;
        Synchronize(report);
        report.Statistics.FileInformation.TotalDataRows = 130;
        report.Statistics.Database.ImportedRecordCount = 130;
        Synchronize(report);
        Check(emailBlank.TotalApplicableRows == 90, "Steps 9-11: a manual Blank denominator must persist.");
        Check(emailBroken.TotalApplicableNonblankValues == 78, "Step 12: Auto Broken must use the manual Blank population.");

        emailBroken.UseAutomaticTotalApplicableNonblankValues = false;
        emailBroken.TotalApplicableNonblankValues = 70;
        Synchronize(report);
        report.Statistics.FileInformation.TotalDataRows = 140;
        report.Statistics.Database.ImportedRecordCount = 140;
        emailBlank.BlankCount = 15;
        Synchronize(report);
        Check(emailBroken.TotalApplicableNonblankValues == 70, "Steps 13-15: a manual Broken denominator must persist.");

        emailBlank.UseAutomaticTotalApplicableRows = true;
        emailBroken.UseAutomaticTotalApplicableNonblankValues = true;
        Synchronize(report);
        Check(emailBlank.TotalApplicableRows == 140, "Steps 16-17: Blank reset must immediately restore 140.");
        Check(emailBroken.TotalApplicableNonblankValues == 125, "Steps 16-17: Broken reset must immediately restore 125.");

        Check(
            report.Statistics.BlankValues.All(row => row.FieldId != QaStatisticFieldIds.StayValue),
            "Stay Value should initially be hidden in the one-monetary-column scenario.");
        report.FileCharacteristics.MonetaryColumnScenario =
            QaMonetaryColumnScenario.TwoMonetaryColumns;
        ApplyChecklistApplicability(report);
        Synchronize(report);
        QaBlankValueStatistic newStayBlank = Blank(report, QaStatisticFieldIds.StayValue);
        QaBrokenDataStatistic newStayBroken = Broken(report, QaStatisticFieldIds.StayValue);
        Check(
            newStayBlank.UseAutomaticTotalApplicableRows
                && newStayBlank.TotalApplicableRows == 140,
            "Steps 18-19: newly applicable Stay Value Blank row must use current Auto defaults.");
        Check(
            newStayBroken.UseAutomaticTotalApplicableNonblankValues
                && newStayBroken.TotalApplicableNonblankValues == 140,
            "Steps 18-19: newly applicable Stay Value Broken row must use current Auto defaults.");
        AssertAutomaticBrokenRows(report, "Step 20");
    }

    private static void TestHeaderRowsRemainExcluded()
    {
        QaReport report = CreateCompleteReport();
        report.Statistics.FileInformation.TotalDataRows = 119;
        report.Statistics.FileInformation.HeadersArePresent = true;
        report.Statistics.FileInformation.DataStartRow = 2;
        report.Statistics.Database.ImportedRecordCount = 119;
        report.Statistics.FileMonth.ValidArrivalDateCount = 119;
        report.Statistics.FileMonth.ArrivalDatesWithinFileMonth = 119;

        QaBlankValueStatistic emailBlank = Blank(report, QaStatisticFieldIds.Email);
        emailBlank.BlankCount = 19;
        Synchronize(report);

        Check(
            emailBlank.TotalApplicableRows == 119,
            "A data-only Total Data Rows value must propagate unchanged when row 1 is a header.");
        Check(
            Broken(report, QaStatisticFieldIds.Email).TotalApplicableNonblankValues == 100,
            "The automatic Broken denominator must use 119 data rows minus 19 blanks.");
        Check(
            report.Statistics.Database.RawMinusImportedRecordCountDifference == 0,
            "A 119-row data population and 119 imported records must have no row difference.");
        Check(
            Validate(report).IsReady,
            "Header metadata must not subtract again from the entered data-only row count.");
    }

    private static void TestFindingLifecycle()
    {
        QaReport report = CreateCompleteReport();
        report.FileCharacteristics.IsCustomScriptSupportAvailable = true;
        QaBlankValueStatistic blank = Blank(report, QaStatisticFieldIds.Email);
        blank.BlankCount = 1;
        Synchronize(report);
        QaFinding firstWarning = Finding(
            report,
            QaFindingIds.WarningForBlankStatistic(QaStatisticFieldIds.Email));
        firstWarning.Resolution = QaFindingResolution.ExplainedAndAccepted;
        firstWarning.ResolutionNotes = "Synthetic aggregate explanation.";

        report.GeneralNotes = "Unrelated synthetic edit.";
        Synchronize(report);
        QaFinding sameWarning = Finding(
            report,
            QaFindingIds.WarningForBlankStatistic(QaStatisticFieldIds.Email));
        Check(ReferenceEquals(firstWarning, sameWarning), "The same Warning ID must preserve object identity across unrelated edits.");
        Check(sameWarning.Resolution == QaFindingResolution.ExplainedAndAccepted, "The same Warning ID must preserve valid resolution.");

        blank.BlankCount = 51;
        Synchronize(report);
        Check(!report.Findings.Contains(firstWarning), "Warning to Failure must remove the old Warning object.");
        QaFinding failure = Finding(
            report,
            QaFindingIds.FailureForBlankStatistic(QaStatisticFieldIds.Email));
        Check(failure.Resolution == QaFindingResolution.Active, "Warning resolution must not transfer to the new Failure.");
        failure.Resolution = QaFindingResolution.HandledByCustomScript;
        failure.CustomScriptName = "SyntheticNormalization";
        failure.ResolutionNotes = "Synthetic aggregate handling.";

        blank.BlankCount = 50;
        Synchronize(report);
        Check(!report.Findings.Contains(failure), "Failure to Warning must remove the old Failure object.");
        QaFinding replacementWarning = Finding(
            report,
            QaFindingIds.WarningForBlankStatistic(QaStatisticFieldIds.Email));
        Check(replacementWarning.Resolution == QaFindingResolution.Active, "Failure resolution must not transfer to the replacement Warning.");

        blank.BlankCount = 0;
        Synchronize(report);
        Check(!HasAnyBlankThresholdFinding(report, QaStatisticFieldIds.Email), "Zero must remove the Blank threshold finding.");
        blank.BlankCount = 1;
        Synchronize(report);
        QaFinding recurring = Finding(
            report,
            QaFindingIds.WarningForBlankStatistic(QaStatisticFieldIds.Email));
        Check(!ReferenceEquals(recurring, replacementWarning), "A finding recurring after zero must be a fresh object.");
        Check(recurring.Resolution == QaFindingResolution.Active, "A recurring finding must be Active.");
        Check(
            report.Findings.Select(finding => finding.FindingId).Distinct(StringComparer.Ordinal).Count()
                == report.Findings.Count,
            "Finding synchronization must never create duplicate IDs.");

        QaReport brokenReport = CreateCompleteReport();
        QaBrokenDataStatistic broken = Broken(brokenReport, QaStatisticFieldIds.Email);
        broken.BrokenValueCount = 51;
        Synchronize(brokenReport);
        QaFinding brokenFailure = Finding(
            brokenReport,
            QaFindingIds.FailureForBrokenStatistic(QaStatisticFieldIds.Email));
        broken.BrokenValueCount = 50;
        Synchronize(brokenReport);
        Check(!brokenReport.Findings.Contains(brokenFailure), "Broken Failure to Warning must discard the Failure object.");
        Check(
            Finding(brokenReport, QaFindingIds.WarningForBrokenStatistic(QaStatisticFieldIds.Email)).Resolution
                == QaFindingResolution.Active,
            "Broken replacement Warning must be Active.");
    }

    private static void TestInvalidStates()
    {
        AssertInvalid(
            report =>
            {
                QaBlankValueStatistic row = Blank(report, QaStatisticFieldIds.Email);
                row.UseAutomaticTotalApplicableRows = false;
                row.TotalApplicableRows = -1;
            },
            "cannot be negative");
        AssertInvalid(
            report =>
            {
                QaBlankValueStatistic row = Blank(report, QaStatisticFieldIds.Email);
                row.UseAutomaticTotalApplicableRows = false;
                row.TotalApplicableRows = 10;
                row.BlankCount = 11;
            },
            "cannot exceed its applicable-row count");
        AssertInvalid(
            report =>
            {
                QaBlankValueStatistic row = Blank(report, QaStatisticFieldIds.Email);
                row.UseAutomaticTotalApplicableRows = false;
                row.TotalApplicableRows = 101;
            },
            "cannot exceed Total Data Rows");
        AssertInvalid(
            report =>
            {
                QaBrokenDataStatistic row = Broken(report, QaStatisticFieldIds.Email);
                row.UseAutomaticTotalApplicableNonblankValues = false;
                row.TotalApplicableNonblankValues = 10;
                row.BrokenValueCount = 11;
            },
            "cannot exceed its nonblank denominator");
        AssertInvalid(
            report =>
            {
                QaBrokenDataStatistic row = Broken(report, QaStatisticFieldIds.Email);
                row.UseAutomaticTotalApplicableNonblankValues = false;
                row.TotalApplicableNonblankValues = 0;
                row.BrokenValueCount = 1;
            },
            "cannot exceed its nonblank denominator");
        AssertInvalid(
            report =>
            {
                QaBrokenDataStatistic row = Broken(report, QaStatisticFieldIds.Email);
                row.UseAutomaticTotalApplicableNonblankValues = false;
                row.TotalApplicableNonblankValues = 0;
                row.BrokenValueCount = 0;
            },
            "is required when its blank-derived nonblank population is positive");
        AssertInvalid(
            report =>
            {
                QaBrokenDataStatistic row = Broken(report, QaStatisticFieldIds.Email);
                row.UseAutomaticTotalApplicableNonblankValues = false;
                row.TotalApplicableNonblankValues = 101;
            },
            "cannot exceed its blank-derived nonblank population");

        QaReport trueZeroPopulation = CreateCompleteReport();
        Blank(trueZeroPopulation, QaStatisticFieldIds.Email).BlankCount = 100;
        QaBrokenDataStatistic trueZeroBroken = Broken(
            trueZeroPopulation,
            QaStatisticFieldIds.Email);
        trueZeroBroken.UseAutomaticTotalApplicableNonblankValues = false;
        trueZeroBroken.TotalApplicableNonblankValues = 0;
        Synchronize(trueZeroPopulation);
        Check(
            Validate(trueZeroPopulation).IsReady,
            "A manual 0/0 Broken row must remain valid when the true derived nonblank population is zero.");

        QaReport documentedMissingField = CreateCompleteReport();
        QaBrokenDataStatistic documentedMissingBroken = Broken(
            documentedMissingField,
            QaStatisticFieldIds.Email);
        documentedMissingBroken.UseAutomaticTotalApplicableNonblankValues = false;
        documentedMissingBroken.TotalApplicableNonblankValues = 0;
        QaCheckResult emailPresence = CheckResult(
            documentedMissingField,
            QaChecklistIds.Raw.EmailColumnAvailable);
        emailPresence.Status = QaCheckStatus.Fail;
        emailPresence.Notes = "The synthetic source intentionally omits the Email field.";
        Synchronize(documentedMissingField);
        Check(
            Validate(documentedMissingField).IsReady,
            "A documented missing required field must permit a manual zero Broken denominator.");

        QaReport autoBlankMismatch = CreateCompleteReport();
        Blank(autoBlankMismatch, QaStatisticFieldIds.Email).TotalApplicableRows = 99;
        FindingService.SynchronizeFindings(autoBlankMismatch);
        AssertError(Validate(autoBlankMismatch), "must equal Total Data Rows");

        QaReport autoBrokenMismatch = CreateCompleteReport();
        Broken(autoBrokenMismatch, QaStatisticFieldIds.Email).TotalApplicableNonblankValues = 99;
        FindingService.SynchronizeFindings(autoBrokenMismatch);
        AssertError(Validate(autoBrokenMismatch), "must equal its blank-derived nonblank count");

        QaReport unsynchronized = CreateCompleteReport();
        Blank(unsynchronized, QaStatisticFieldIds.Email).BlankPercentage = 42m;
        AssertError(Validate(unsynchronized), "is not synchronized with its counts");

        QaReport duplicate = CreateCompleteReport();
        duplicate.Statistics.BlankValues.Add(new QaBlankValueStatistic
        {
            FieldId = QaStatisticFieldIds.Email,
            DisplayName = "Email",
            TotalApplicableRows = 100
        });
        AssertError(Validate(duplicate), "appears more than once");

        QaReport unknown = CreateCompleteReport();
        unknown.Statistics.BrokenData.Add(new QaBrokenDataStatistic
        {
            FieldId = "UNKNOWN_FIELD",
            DisplayName = "Unknown"
        });
        AssertError(Validate(unknown), "is not in the approved field catalog");

        QaReport notEvaluated = CreateCompleteReport();
        CheckResult(notEvaluated, QaChecklistIds.Raw.FileFormatConsistent).Status =
            QaCheckStatus.NotEvaluated;
        Synchronize(notEvaluated);
        AssertError(Validate(notEvaluated), "has not been evaluated");

        QaReport contradiction = CreateCompleteReport();
        Broken(contradiction, QaStatisticFieldIds.FirstName).BrokenValueCount = 51;
        Synchronize(contradiction);
        AssertError(Validate(contradiction), "requires checklist item");
    }

    private static void TestCompatibilityStatusAndStaleness()
    {
        Check(new QaBlankValueStatistic().UseAutomaticTotalApplicableRows, "New Blank rows must default to Auto.");
        Check(
            new QaBrokenDataStatistic().UseAutomaticTotalApplicableNonblankValues,
            "New Broken rows must default to Auto.");
        QaBlankValueStatistic? oldBlank = JsonSerializer.Deserialize<QaBlankValueStatistic>(
            "{\"FieldId\":\"EMAIL\",\"DisplayName\":\"Email\",\"TotalApplicableRows\":77}");
        QaBrokenDataStatistic? oldBroken = JsonSerializer.Deserialize<QaBrokenDataStatistic>(
            "{\"FieldId\":\"EMAIL\",\"DisplayName\":\"Email\",\"TotalApplicableNonblankValues\":70}");
        Check(oldBlank?.UseAutomaticTotalApplicableRows == true, "A missing Blank mode property must deserialize as Auto.");
        Check(oldBroken?.UseAutomaticTotalApplicableNonblankValues == true, "A missing Broken mode property must deserialize as Auto.");

        QaReportStatusService status = new();
        Check(status.CalculateStatus([], false) == QaReportStatus.Pass, "No findings must produce Pass.");
        Check(
            status.CalculateStatus(
                [new QaFinding { Severity = QaFindingSeverity.Warning }],
                false) == QaReportStatus.PassWithWarnings,
            "A Warning must produce Pass with Warnings.");
        Check(
            status.CalculateStatus(
                [new QaFinding { Severity = QaFindingSeverity.Failure }],
                false) == QaReportStatus.Fail,
            "An Active Failure must produce Fail.");
        Check(
            status.CalculateStatus(
                [new QaFinding
                {
                    Severity = QaFindingSeverity.Failure,
                    Resolution = QaFindingResolution.HandledByCustomScript
                }],
                false) == QaReportStatus.PassWithWarnings,
            "A handled Failure must retain an audit record and produce Pass with Warnings.");

        QaReport report = CreateCompleteReport();
        QaReportValidationResult ready = Validate(report);
        Check(ready.IsReady && ready.CalculatedStatus == QaReportStatus.Pass, "The baseline synthetic report must be ready Pass.");
        report.ReportStatus = ready.CalculatedStatus;
        QaBlankValueStatistic email = Blank(report, QaStatisticFieldIds.Email);
        email.UseAutomaticTotalApplicableRows = false;
        Check(email.TotalApplicableRows == 100, "Mode-only staleness test must retain the same denominator value.");

        try
        {
            _ = new QaPdfGenerationService().GeneratePdf(
                report,
                ready,
                DateTimeOffset.Parse("2026-07-17T12:00:00-04:00"));
            throw new SemanticAssertionException(
                "A mode-only edit must invalidate the readiness fingerprint before PDF generation.");
        }
        catch (QaPdfGenerationException)
        {
            // Expected stale-readiness rejection.
        }
    }

    private static void AssertInvalid(Action<QaReport> mutate, string expectedError)
    {
        QaReport report = CreateCompleteReport();
        mutate(report);
        CalculationService.Synchronize(report);
        FindingService.SynchronizeFindings(report);
        AssertError(Validate(report), expectedError);
    }

    private static void AssertThresholdFinding(
        QaReport report,
        string fieldId,
        bool isBlank,
        QaFindingSeverity? expected)
    {
        string warningId = isBlank
            ? QaFindingIds.WarningForBlankStatistic(fieldId)
            : QaFindingIds.WarningForBrokenStatistic(fieldId);
        string failureId = isBlank
            ? QaFindingIds.FailureForBlankStatistic(fieldId)
            : QaFindingIds.FailureForBrokenStatistic(fieldId);

        if (expected is null)
        {
            Check(!HasFinding(report, warningId) && !HasFinding(report, failureId),
                $"Expected no {(isBlank ? "Blank" : "Broken")} threshold finding at zero.");
            return;
        }

        string expectedId = expected == QaFindingSeverity.Warning
            ? warningId
            : failureId;
        string absentId = expected == QaFindingSeverity.Warning
            ? failureId
            : warningId;
        QaFinding finding = Finding(report, expectedId);
        Check(finding.Severity == expected, $"Finding '{expectedId}' has the wrong severity.");
        Check(!HasFinding(report, absentId), $"Mutually exclusive finding '{absentId}' should be absent.");
    }

    private static QaReport CreateCompleteReport(
        QaNameColumnMode nameMode = QaNameColumnMode.SeparateFirstAndLastName)
    {
        QaReport report = new()
        {
            ReportId = $"synthetic-{Guid.NewGuid():N}",
            FileId = "001234",
            HotelInformation = new QaHotelInformation
            {
                HotelId = "SYN-001",
                HotelName = "Synthetic Hotel",
                PmsName = "Synthetic PMS",
                FileMonth = new QaFileMonth(2026, 7)
            },
            QaDate = new DateOnly(2026, 7, 17),
            CreatedBy = "Synthetic QA",
            OriginalFileName = "synthetic-aggregate.csv",
            FileCharacteristics = new QaFileCharacteristics
            {
                NameColumnMode = nameMode,
                MonetaryColumnScenario = QaMonetaryColumnScenario.OneMonetaryColumn
            },
            Statistics = new QaStatistics
            {
                FileInformation = new QaFileInformationStatistics
                {
                    TotalDataRows = 100,
                    HeadersArePresent = true,
                    UsefulHeaders = QaUsefulHeadersResult.Yes,
                    DataStartRow = 2
                },
                FileMonth = new QaFileMonthStatistics
                {
                    ValidArrivalDateCount = 100,
                    ArrivalDatesWithinFileMonth = 100,
                    ArrivalDatesOutsideFileMonth = 0
                },
                Database = new QaDatabaseStatistics
                {
                    ImportedRecordCount = 100
                }
            }
        };

        foreach (QaCheckDefinition definition in QaChecklistCatalog.Definitions)
        {
            report.ChecklistResults.Add(new QaCheckResult
            {
                CheckId = definition.Id,
                Status = IsApplicable(definition, report.FileCharacteristics)
                    ? QaCheckStatus.Pass
                    : QaCheckStatus.NotApplicable,
                ResultSource = QaResultSource.Manual
            });
        }

        Synchronize(report);
        return report;
    }

    private static void Synchronize(QaReport report)
    {
        CalculationService.Synchronize(report);
        FindingService.SynchronizeFindings(report);
    }

    private static QaReportValidationResult Validate(QaReport report)
    {
        return ValidationService.Validate(
            report,
            [new QaHotelMetadata
            {
                HotelId = "SYN-001",
                HotelName = "Synthetic Hotel",
                PmsName = "Synthetic PMS",
                FolderName = "Synthetic Hotel - SYN-001"
            }]);
    }

    private static void ApplyChecklistApplicability(QaReport report)
    {
        foreach (QaCheckDefinition definition in QaChecklistCatalog.Definitions)
        {
            QaCheckResult result = CheckResult(report, definition.Id);
            bool applicable = IsApplicable(definition, report.FileCharacteristics);
            result.Status = applicable ? QaCheckStatus.Pass : QaCheckStatus.NotApplicable;
            if (!applicable)
            {
                result.Notes = null;
            }
        }
    }

    private static bool IsApplicable(
        QaCheckDefinition definition,
        QaFileCharacteristics characteristics)
    {
        return definition.Applicability switch
        {
            QaCheckApplicability.Always => true,
            QaCheckApplicability.SeparateNameColumns =>
                characteristics.NameColumnMode == QaNameColumnMode.SeparateFirstAndLastName,
            QaCheckApplicability.FullNameColumn =>
                characteristics.NameColumnMode == QaNameColumnMode.FullName,
            QaCheckApplicability.CurrencyColumnPresent => characteristics.HasCurrencyColumn,
            QaCheckApplicability.OneMonetaryColumn =>
                characteristics.MonetaryColumnScenario == QaMonetaryColumnScenario.OneMonetaryColumn,
            QaCheckApplicability.TwoMonetaryColumns =>
                characteristics.MonetaryColumnScenario == QaMonetaryColumnScenario.TwoMonetaryColumns,
            QaCheckApplicability.MoreThanTwoMonetaryColumns =>
                characteristics.MonetaryColumnScenario == QaMonetaryColumnScenario.MoreThanTwoMonetaryColumns,
            QaCheckApplicability.RejectedRecordsPresent =>
                characteristics.HasRejectedDatabaseRecords,
            _ => false
        };
    }

    private static void AssertAutomaticBrokenRows(QaReport report, string context)
    {
        Dictionary<string, QaBlankValueStatistic> blankById =
            report.Statistics.BlankValues.ToDictionary(row => row.FieldId, StringComparer.Ordinal);
        foreach (QaBrokenDataStatistic broken in report.Statistics.BrokenData
                     .Where(row => row.UseAutomaticTotalApplicableNonblankValues))
        {
            QaBlankValueStatistic blank = blankById[broken.FieldId];
            Check(
                broken.TotalApplicableNonblankValues
                    == blank.TotalApplicableRows - blank.BlankCount,
                $"{context}: Auto Broken denominator for '{broken.FieldId}' is stale.");
        }
    }

    private static QaBlankValueStatistic Blank(QaReport report, string fieldId) =>
        report.Statistics.BlankValues.Single(row => row.FieldId == fieldId);

    private static QaBrokenDataStatistic Broken(QaReport report, string fieldId) =>
        report.Statistics.BrokenData.Single(row => row.FieldId == fieldId);

    private static QaCheckResult CheckResult(QaReport report, string checkId) =>
        report.ChecklistResults.Single(result => result.CheckId == checkId);

    private static QaFinding Finding(QaReport report, string findingId) =>
        report.Findings.Single(finding => finding.FindingId == findingId);

    private static bool HasFinding(QaReport report, string findingId) =>
        report.Findings.Any(finding => finding.FindingId == findingId);

    private static bool HasAnyBlankThresholdFinding(QaReport report, string fieldId) =>
        HasFinding(report, QaFindingIds.WarningForBlankStatistic(fieldId))
        || HasFinding(report, QaFindingIds.FailureForBlankStatistic(fieldId));

    private static bool HasAnyBrokenThresholdFinding(QaReport report, string fieldId) =>
        HasFinding(report, QaFindingIds.WarningForBrokenStatistic(fieldId))
        || HasFinding(report, QaFindingIds.FailureForBrokenStatistic(fieldId));

    private static void AssertError(
        QaReportValidationResult result,
        string expectedSubstring)
    {
        Check(
            result.BlockingErrors.Any(error => error.Contains(
                expectedSubstring,
                StringComparison.OrdinalIgnoreCase)),
            $"Expected blocking error containing '{expectedSubstring}'. Actual: " +
            string.Join(" | ", result.BlockingErrors));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new SemanticAssertionException(message);
        }
    }

    private sealed class SemanticAssertionException(string message)
        : Exception(message);
}
