using System;
using System.Collections.Generic;
using System.Text;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Shipping;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class ReleaseShippingIntegrationTests
{
    [Fact]
    public void TC_S15_011_HeadlessSmokeTestRunner_600Ticks_ExecutesCleanlyWithExitCodeZero()
    {
        // Arrange
        var config = new SmokeScenarioConfig(TicksToSimulate: 600, RandomSeed: 42, UnitsPerFaction: 6);

        // Act
        var result = HeadlessSmokeTestRunner.RunSmokeTest(config);

        // Assert
        Assert.True(result.IsSuccess, result.SummaryDetails);
        Assert.Equal(HeadlessSmokeTestRunner.ExitCodeSuccess, result.ExitCode);
        Assert.Equal(600, result.TotalTicksExecuted);
        Assert.True(result.SaveLoadParityConfirmed);
        Assert.True(result.TotalKillsAwarded > 0 || result.TotalResourcesHarvested > 0);
    }

    [Fact]
    public void TC_S15_012_SmokeTest_MidSimulationSaveReload_MaintainsStateChecksumParity()
    {
        // Arrange
        var config = new SmokeScenarioConfig(TicksToSimulate: 300, MidSimulationSaveTick: 150);

        // Act
        var result = HeadlessSmokeTestRunner.RunSmokeTest(config);

        // Assert
        Assert.True(result.SaveLoadParityConfirmed);
        Assert.NotEqual(0UL, result.PreSaveChecksum);
        Assert.Equal(result.PreSaveChecksum, result.PostReloadChecksum);
    }

    [Fact]
    public void TC_S15_013_SmokeTest_CompletesWithinStrictTimeBudget()
    {
        // Arrange
        var config = new SmokeScenarioConfig(TicksToSimulate: 400);

        // Act
        var result = HeadlessSmokeTestRunner.RunSmokeTest(config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.DurationMs < 5000.0, $"Smoke test took {result.DurationMs}ms (budget < 5000ms)");
    }

    [Fact]
    public void TC_S15_014_ReleasePerformanceCertifier_1000Ticks500Units_PassesFrameBudget()
    {
        // Act
        var report = ReleasePerformanceCertifier.CertifySimulationPerformance(
            ticksToRun: 500,
            unitCount: 200,
            seed: 42);

        // Assert
        Assert.True(report.IsCertified, report.ReportSummary);
        Assert.True(report.MeanTickDurationMs <= ReleasePerformanceCertifier.MaxMeanTickBudgetMs,
            $"Mean tick was {report.MeanTickDurationMs:F2}ms (Budget <= {ReleasePerformanceCertifier.MaxMeanTickBudgetMs}ms)");
        Assert.True(report.MemoryFootprintMb < ReleasePerformanceCertifier.MaxMemoryFootprintMb,
            $"Memory was {report.MemoryFootprintMb:F1}MB (Budget < {ReleasePerformanceCertifier.MaxMemoryFootprintMb}MB)");
        Assert.True(report.ZeroAllocationCompliant);
    }

    [Fact]
    public void TC_S15_015_FullMatchRegression_ExecutesAllSystemsCleanly()
    {
        // Act
        var result = FullMatchRegressionHarness.RunFullMatch(ticks: 600, seed: 1337);

        // Assert
        Assert.True(result.IsSuccess, result.Summary);
        Assert.Equal(600, result.TotalTicksExecuted);
        Assert.NotEqual(0UL, result.FinalChecksum);
        Assert.True(result.FinalActiveUnits > 0);
        Assert.True(result.FinalActiveBuildings > 0);
    }

    [Fact]
    public void TC_S15_016_DeterministicReplayParity_1000TicksDualSeededRuns_ExactChecksumMatch()
    {
        // Arrange
        int seed = 777;
        int ticks = 1000;

        var sim1 = new SimulationEngine(new SimulationConfig { InitialRandomSeed = seed }, new DomainEventBus());
        var sim2 = new SimulationEngine(new SimulationConfig { InitialRandomSeed = seed }, new DomainEventBus());

        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        for (int i = 0; i < 10; i++)
        {
            var u1A = new UnitEntity(sim1.State.GenerateEntityId(), f1, "swordsman", new Vector2D(10f + i, 20f), 120f, 15f, 1.5f, 3.5f, 18, 50, 3f);
            var u1B = new UnitEntity(sim2.State.GenerateEntityId(), f1, "swordsman", new Vector2D(10f + i, 20f), 120f, 15f, 1.5f, 3.5f, 18, 50, 3f);
            sim1.State.AddUnit(u1A);
            sim2.State.AddUnit(u1B);
            sim1.SpatialGrid.Insert(u1A.Id, u1A.Position);
            sim2.SpatialGrid.Insert(u1B.Id, u1B.Position);

            var u2A = new UnitEntity(sim1.State.GenerateEntityId(), f2, "spearman", new Vector2D(30f - i, 20f), 110f, 14f, 2.0f, 3.2f, 20, 50, 2f);
            var u2B = new UnitEntity(sim2.State.GenerateEntityId(), f2, "spearman", new Vector2D(30f - i, 20f), 110f, 14f, 2.0f, 3.2f, 20, 50, 2f);
            sim1.State.AddUnit(u2A);
            sim2.State.AddUnit(u2B);
            sim1.SpatialGrid.Insert(u2A.Id, u2A.Position);
            sim2.SpatialGrid.Insert(u2B.Id, u2B.Position);
        }

        // Act
        for (int t = 0; t < ticks; t++)
        {
            sim1.Tick();
            sim2.Tick();
        }

        ulong checksum1 = sim1.State.ComputeStateChecksum();
        ulong checksum2 = sim2.State.ComputeStateChecksum();

        // Assert
        Assert.Equal(checksum1, checksum2);
    }

    [Fact]
    public void TC_S15_017_ReleasePipelineEngine_ExecuteReleasePipeline_ReturnsApprovedReport()
    {
        // Act
        var report = ReleasePipelineEngine.ExecuteReleasePipeline(version: "1.0.0", targetPlatform: "win-x64");

        // Assert
        Assert.True(report.IsApprovedForRelease);
        Assert.Equal("1.0.0", report.ReleaseVersion);
        Assert.Equal("win-x64", report.TargetPlatform);
        Assert.True(report.SaveCompatibility.IsCompatible);
        Assert.True(report.PerformanceBudget.IsCertified);
        Assert.True(report.SmokeTest.IsSuccess);
        Assert.True(report.RegressionResult.IsSuccess);
        Assert.True(report.ChecksumVerification.IsValid);

        string markdown = report.GenerateSummaryMarkdown();
        Assert.Contains("APPROVED FOR RELEASE", markdown);
    }

    [Fact]
    public void TC_S15_018_ReleaseCandidatePresenter_BuildsCompleteViewModel()
    {
        // Arrange
        var report = ReleasePipelineEngine.ExecuteReleasePipeline("1.0.0", "win-x64");
        var presenter = new ReleaseCandidatePresenter();

        // Act
        var vm = presenter.PresentCertification(report);

        // Assert
        Assert.NotNull(vm);
        Assert.True(vm.IsReadyForShipping);
        Assert.Equal("[READY FOR RELEASE]", vm.ShippingStatusBadge);
        Assert.Equal(6, vm.QualityGates.Count);
        Assert.All(vm.QualityGates, g => Assert.True(g.IsPassed));
    }

    [Fact]
    public void TC_S15_019_ReleaseCandidateScenario_ExecutesFullCertificationWorkflow()
    {
        // Arrange
        var scenario = new ReleaseCandidateScenario();

        // Act
        bool passed = scenario.RunReleaseCertification("1.0.0", "win-x64");

        // Assert
        Assert.True(passed);
        Assert.NotNull(scenario.LatestReport);
        Assert.NotNull(scenario.LatestSummaryVm);
        Assert.True(scenario.LatestSummaryVm.IsReadyForShipping);
    }
}
