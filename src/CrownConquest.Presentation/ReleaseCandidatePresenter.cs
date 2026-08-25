using System;
using System.Collections.Generic;
using CrownConquest.Domain.Shipping;

namespace CrownConquest.Presentation;

public sealed record ReleaseQualityGateItemViewModel(
    string GateName,
    bool IsPassed,
    string StatusBadge,
    string Details);

public sealed record ReleaseCandidateSummaryViewModel(
    string ApplicationTitle,
    string Version,
    string TargetPlatform,
    bool IsReadyForShipping,
    string ShippingStatusBadge,
    IReadOnlyList<ReleaseQualityGateItemViewModel> QualityGates,
    string EnvironmentSummary,
    string PerformanceSummary,
    string SmokeTestSummary,
    string ChecksumSummary);

public sealed class ReleaseCandidatePresenter
{
    public ReleaseCandidateSummaryViewModel PresentCertification(ReleaseCertificationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var gates = new List<ReleaseQualityGateItemViewModel>
        {
            new(
                "Clean-Machine Environment",
                report.EnvironmentDiagnostics.IsPassing,
                report.EnvironmentDiagnostics.IsPassing ? "[PASS]" : "[FAIL]",
                $"{report.EnvironmentDiagnostics.Architecture} on {report.EnvironmentDiagnostics.OperatingSystem} (.NET {report.EnvironmentDiagnostics.DotNetVersion})"),
            new(
                "Save/Load Compatibility & Resilience",
                report.SaveCompatibility.IsCompatible,
                report.SaveCompatibility.IsCompatible ? "[PASS]" : "[FAIL]",
                report.SaveCompatibility.Details),
            new(
                "Performance Frame Budget & Memory",
                report.PerformanceBudget.IsCertified,
                report.PerformanceBudget.IsCertified ? "[PASS]" : "[FAIL]",
                $"Mean: {report.PerformanceBudget.MeanTickDurationMs:F2}ms | Memory: {report.PerformanceBudget.MemoryFootprintMb:F1}MB | ZeroAlloc: {report.PerformanceBudget.ZeroAllocationCompliant}"),
            new(
                "Headless Smoke Test Automation",
                report.SmokeTest.IsSuccess,
                report.SmokeTest.IsSuccess ? "[PASS]" : "[FAIL]",
                $"ExitCode: {report.SmokeTest.ExitCode} | Ticks: {report.SmokeTest.TotalTicksExecuted} | Kills: {report.SmokeTest.TotalKillsAwarded}"),
            new(
                "Full Match Multi-System Regression",
                report.RegressionResult.IsSuccess,
                report.RegressionResult.IsSuccess ? "[PASS]" : "[FAIL]",
                $"Checksum: {report.RegressionResult.FinalChecksum} | Ticks: {report.RegressionResult.TotalTicksExecuted}"),
            new(
                "SHA-256 Checksum Integrity",
                report.ChecksumVerification.IsValid,
                report.ChecksumVerification.IsValid ? "[PASS]" : "[FAIL]",
                $"{report.ChecksumVerification.MatchingFiles}/{report.ChecksumVerification.TotalFilesChecked} Files Verified")
        };

        string envSummary = $"Environment: {report.EnvironmentDiagnostics.Architecture} / {report.EnvironmentDiagnostics.OperatingSystem} (RAM: {report.EnvironmentDiagnostics.TotalMemoryMb}MB, FreeDisk: {report.EnvironmentDiagnostics.AvailableDiskSpaceMb}MB)";
        string perfSummary = $"Performance: {report.PerformanceBudget.ReportSummary}";
        string smokeSummary = $"Smoke Automation: {report.SmokeTest.SummaryDetails}";
        string checksumSummary = $"Checksums: Verified {report.ChecksumVerification.MatchingFiles}/{report.ChecksumVerification.TotalFilesChecked} package components cryptographically.";

        string shippingBadge = report.IsApprovedForRelease ? "[READY FOR RELEASE]" : "[REJECTED / DEFECTS DETECTED]";

        return new ReleaseCandidateSummaryViewModel(
            "Crown & Conquest",
            report.ReleaseVersion,
            report.TargetPlatform,
            report.IsApprovedForRelease,
            shippingBadge,
            gates,
            envSummary,
            perfSummary,
            smokeSummary,
            checksumSummary);
    }
}
