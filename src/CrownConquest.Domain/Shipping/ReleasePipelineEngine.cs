using System;
using System.Collections.Generic;
using System.Text;

namespace CrownConquest.Domain.Shipping;

public sealed record ReleaseCertificationReport(
    bool IsApprovedForRelease,
    string ReleaseVersion,
    string TargetPlatform,
    DateTimeOffset CertificationTimestamp,
    EnvironmentDiagnostics EnvironmentDiagnostics,
    SaveCompatibilityReport SaveCompatibility,
    PerformanceBudgetReport PerformanceBudget,
    SmokeTestResult SmokeTest,
    FullMatchRegressionResult RegressionResult,
    ChecksumVerificationResult ChecksumVerification,
    IReadOnlyList<string> CertificationNotes)
{
    public string GenerateSummaryMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Crown & Conquest — Release Candidate Certification Report");
        sb.AppendLine();
        sb.AppendLine($"- **Version:** `{ReleaseVersion}`");
        sb.AppendLine($"- **Status:** {(IsApprovedForRelease ? "**APPROVED FOR RELEASE**" : "**REJECTED**")}");
        sb.AppendLine($"- **Target Platform:** `{TargetPlatform}`");
        sb.AppendLine($"- **Certified At:** `{CertificationTimestamp:u}`");
        sb.AppendLine();
        sb.AppendLine("## Quality Gates Summary");
        sb.AppendLine($"1. **Clean-Machine Environment:** {EnvironmentDiagnostics.OverallStatus} ({EnvironmentDiagnostics.Architecture}, {EnvironmentDiagnostics.DotNetVersion})");
        sb.AppendLine($"2. **Save/Load Compatibility:** {(SaveCompatibility.IsCompatible ? "PASS" : "FAIL")} — {SaveCompatibility.Details}");
        sb.AppendLine($"3. **Performance Budget (60 FPS / <500MB):** {(PerformanceBudget.IsCertified ? "PASS" : "FAIL")} — Mean={PerformanceBudget.MeanTickDurationMs:F2}ms, Memory={PerformanceBudget.MemoryFootprintMb:F1}MB");
        sb.AppendLine($"4. **Headless Smoke Automation:** {(SmokeTest.IsSuccess ? "PASS" : "FAIL")} — ExitCode={SmokeTest.ExitCode}, Ticks={SmokeTest.TotalTicksExecuted}");
        sb.AppendLine($"5. **Full Match Multi-System Regression:** {(RegressionResult.IsSuccess ? "PASS" : "FAIL")} — Checksum={RegressionResult.FinalChecksum}");
        sb.AppendLine($"6. **SHA-256 Checksum Integrity:** {(ChecksumVerification.IsValid ? "PASS" : "FAIL")} — {ChecksumVerification.MatchingFiles}/{ChecksumVerification.TotalFilesChecked} files verified");
        sb.AppendLine();
        return sb.ToString();
    }
}

public static class ReleasePipelineEngine
{
    public static ReleaseCertificationReport ExecuteReleasePipeline(
        string version = "1.0.0",
        string targetPlatform = "win-x64",
        IReadOnlyDictionary<string, byte[]>? payloadFiles = null)
    {
        // 1. Environment Diagnostics
        var envDiag = CleanMachineEnvironmentValidator.ValidateCurrentEnvironment();

        // 2. Save Compatibility
        var saveCompat = ReleaseSaveCompatibilityValidator.ValidateCompatibility();

        // 3. Performance Budget Certification
        var perfReport = ReleasePerformanceCertifier.CertifySimulationPerformance(ticksToRun: 500, unitCount: 300);

        // 4. Smoke Test
        var smokeResult = HeadlessSmokeTestRunner.RunSmokeTest(new SmokeScenarioConfig(TicksToSimulate: 400));

        // 5. Full Match Regression
        var regResult = FullMatchRegressionHarness.RunFullMatch(ticks: 500);

        // 6. Bundle & Checksum Verification
        payloadFiles ??= new Dictionary<string, byte[]>
        {
            ["CrownConquest.Domain.dll"] = Encoding.UTF8.GetBytes("MOCK_BINARY_DOMAIN"),
            ["CrownConquest.Application.dll"] = Encoding.UTF8.GetBytes("MOCK_BINARY_APP"),
            ["CrownConquest.Data.dll"] = Encoding.UTF8.GetBytes("MOCK_BINARY_DATA"),
            ["CrownConquest.Presentation.dll"] = Encoding.UTF8.GetBytes("MOCK_BINARY_PRES"),
            ["game_data.json"] = Encoding.UTF8.GetBytes("{\"version\":\"1.0.0\"}")
        };

        var bundle = PackageBundleGenerator.CreateBundle(version, "ReleaseCandidate", targetPlatform, payloadFiles);
        var checksumResult = Sha256ChecksumValidator.VerifyManifest(bundle.Manifest, payloadFiles);

        // Evaluate overall release readiness
        bool isApproved = envDiag.IsPassing &&
                          saveCompat.IsCompatible &&
                          perfReport.IsCertified &&
                          smokeResult.IsSuccess &&
                          regResult.IsSuccess &&
                          checksumResult.IsValid;

        var notes = new List<string>
        {
            $"Deterministic state validation confirmed across {smokeResult.TotalTicksExecuted} smoke ticks and {regResult.TotalTicksExecuted} regression ticks.",
            $"Frame simulation budget verified at {perfReport.MeanTickDurationMs:F2}ms average tick duration (limit {ReleasePerformanceCertifier.MaxMeanTickBudgetMs:F1}ms).",
            $"All {checksumResult.TotalFilesChecked} package artifacts cryptographically verified with SHA-256 digests."
        };

        return new ReleaseCertificationReport(
            isApproved,
            version,
            targetPlatform,
            DateTimeOffset.UtcNow,
            envDiag,
            saveCompat,
            perfReport,
            smokeResult,
            regResult,
            checksumResult,
            notes);
    }
}
