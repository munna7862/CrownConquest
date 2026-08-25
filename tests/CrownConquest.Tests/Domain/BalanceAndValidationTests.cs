using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Domain;

/// <summary>
/// Tier 1 Unit and Math Tests for Sprint 14 Balance and Validation systems.
/// </summary>
public sealed class BalanceAndValidationTests
{
    [Fact]
    public void TC_S14_01_BattleSimulatorConfig_CreatesValidRosterAndMatchup()
    {
        var config = BattleSimulatorConfig.CreateStandardMatchup("spearman", 12, "cavalry", 8, seed: 777);

        Assert.Equal(777, config.RandomSeed);
        Assert.Equal(12, config.TeamA.Units[0].Count);
        Assert.Equal(8, config.TeamB.Units[0].Count);
        Assert.Equal("spearman", config.TeamA.Units[0].UnitType);
        Assert.Equal("cavalry", config.TeamB.Units[0].UnitType);
        Assert.True(config.AutoEngage);
    }

    [Fact]
    public void TC_S14_02_ArchetypeBattleMetrics_CalculatesRatesAccurately()
    {
        var metrics = new ArchetypeBattleMetrics(
            UnitArchetype.Infantry,
            InitialCount: 20,
            SurvivingCount: 15,
            Kills: 10,
            Deaths: 5,
            DamageDealt: 500f,
            DamageTaken: 250f,
            XpGained: 300);

        Assert.Equal(0.25f, metrics.CasualtyRate);
        Assert.Equal(0.75f, metrics.SurvivalRate);
        Assert.Equal(2.0f, metrics.KillDeathRatio);
    }

    [Fact]
    public void TC_S14_03_BatchBattleRunner_AggregatesStatisticsCorrectly()
    {
        var matchup = BattleSimulatorConfig.CreateStandardMatchup("spearman", 6, "swordsman", 6, seed: 100);
        var batchConfig = BatchBattleConfig.Create(matchup, iterations: 10, baseSeed: 500);

        var runner = new BatchBattleRunner();
        var result = runner.RunBatch(batchConfig);

        Assert.Equal(10, result.TotalBattles);
        Assert.Equal(10, result.BattleResults.Count);
        Assert.True(result.WinRateA >= 0f && result.WinRateA <= 1f);
        Assert.True(result.WinRateB >= 0f && result.WinRateB <= 1f);
        Assert.True(result.MeanDurationTicks > 0);
        Assert.True(result.StdDevDurationTicks >= 0);
        Assert.True(result.MaxDurationTicks >= result.MinDurationTicks);
    }

    [Fact]
    public void TC_S14_04_BatchBattleRunner_ThrowsOnZeroOrNegativeIterations()
    {
        var matchup = new BattleSimulatorConfig();
        var invalidConfig = new BatchBattleConfig { Iterations = 0, BaseMatchup = matchup };
        var runner = new BatchBattleRunner();

        Assert.Throws<ArgumentOutOfRangeException>(() => runner.RunBatch(invalidConfig));
    }

    [Fact]
    public void TC_S14_05_FactionBalanceReport_GeneratesFormattedReportAndMatrix()
    {
        var generator = new FactionBalanceReportGenerator();
        var report = generator.GenerateReport(battlesPerMatchup: 2, baseSeed: 100);

        Assert.Equal(10, report.TotalMatchups);
        Assert.Equal(10, report.Matchups.Count);
        Assert.Equal(5, report.FactionOverallWinRates.Count);
        Assert.True(report.OverallAsymmetryScore >= 0f);

        string text = report.GenerateFormattedReport();
        Assert.Contains("FACTION BALANCE & ASYMMETRY REPORT", text);
        Assert.Contains("Kingdom", text);
        Assert.Contains("Imperium", text);
        Assert.Contains("Caliphate", text);
        Assert.Contains("Horde", text);
        Assert.Contains("Republic", text);
    }

    [Fact]
    public void TC_S14_06_ProgressionBalanceValidator_ValidatesMonotonicLevelingCurve()
    {
        var report = ProgressionBalanceValidator.ValidateProgressionInvariants();

        Assert.True(report.IsValid);
        Assert.Empty(report.ValidationErrors);
        Assert.True(report.TotalChecksExecuted > 20);

        // Check veterancy rank multipliers
        Assert.Equal(1.0f, report.RankHealthMultipliers[VeterancyRank.Recruit]);
        Assert.Equal(1.10f, report.RankHealthMultipliers[VeterancyRank.Experienced]);
        Assert.Equal(1.20f, report.RankHealthMultipliers[VeterancyRank.Veteran]);
        Assert.Equal(1.30f, report.RankHealthMultipliers[VeterancyRank.Elite]);
        Assert.Equal(1.50f, report.RankHealthMultipliers[VeterancyRank.Legendary]);

        Assert.Equal(0f, report.RankArmorBonuses[VeterancyRank.Recruit]);
        Assert.Equal(1f, report.RankArmorBonuses[VeterancyRank.Veteran]);
        Assert.Equal(2f, report.RankArmorBonuses[VeterancyRank.Elite]);
        Assert.Equal(3f, report.RankArmorBonuses[VeterancyRank.Legendary]);
    }

    [Fact]
    public void TC_S14_07_ProgressionBalanceValidator_DetectsNonMonotonicCurves()
    {
        int[] invalidCurve = { 100, 250, 200, 500 }; // 200 < 250 violation
        var report = ProgressionBalanceValidator.ValidateProgressionInvariants(invalidCurve);

        Assert.False(report.IsValid);
        Assert.NotEmpty(report.ValidationErrors);
        Assert.Contains(report.ValidationErrors, e => e.Contains("not strictly greater"));
    }

    [Fact]
    public void TC_S14_08_AiDifficultyConfig_TiersMapToExpectedModifiers()
    {
        var easy = AiDifficultyConfig.CreateEasy();
        var normal = AiDifficultyConfig.CreateNormal();
        var hard = AiDifficultyConfig.CreateHard();
        var brutal = AiDifficultyConfig.CreateBrutal();

        Assert.Equal(0.75f, easy.ResourceGatherMultiplier);
        Assert.Equal(1.00f, normal.ResourceGatherMultiplier);
        Assert.Equal(1.25f, hard.ResourceGatherMultiplier);
        Assert.Equal(1.50f, brutal.ResourceGatherMultiplier);

        Assert.Equal(0.60f, easy.AggressionFactor);
        Assert.Equal(1.00f, normal.AggressionFactor);
        Assert.Equal(1.30f, hard.AggressionFactor);
        Assert.Equal(1.60f, brutal.AggressionFactor);

        Assert.Equal(2, easy.DecisionIntervalMultiplier);
        Assert.Equal(1, normal.DecisionIntervalMultiplier);
    }

    [Fact]
    public void TC_S14_09_AiDifficultyConfig_CustomClampsInvalidValues()
    {
        var custom = AiDifficultyConfig.CreateCustom(
            gatherMult: -5.0f,
            buildSpeedMult: 100f,
            decisionIntervalMult: -1,
            targetSwitchThreshold: 0f,
            aggression: -10f,
            perceptionMult: 20f);

        Assert.Equal(AiDifficultyTier.Custom, custom.Tier);
        Assert.Equal(0.1f, custom.ResourceGatherMultiplier);
        Assert.Equal(5.0f, custom.BuildSpeedMultiplier);
        Assert.Equal(1, custom.DecisionIntervalMultiplier);
        Assert.Equal(0.1f, custom.TargetSwitchingThreshold);
        Assert.Equal(0.1f, custom.AggressionFactor);
        Assert.Equal(5.0f, custom.PerceptionRangeMultiplier);
    }

    [Fact]
    public void TC_S14_10_BalanceAndValidationPresenter_ProjectsViewModelsAccurately()
    {
        var coordinator = new BalanceValidationCoordinator();
        var presenter = new BalanceAndValidationPresenter(coordinator);

        var config = BattleSimulatorConfig.CreateStandardMatchup("swordsman", 5, "spearman", 5, seed: 42);
        var result = coordinator.RunSingleBattle(config);

        var battleVm = presenter.BuildBattleViewModel(result, "Swordsmen", "Spearmen");
        Assert.Equal(5, battleVm.InitialA);
        Assert.Equal(5, battleVm.InitialB);
        Assert.True(battleVm.DurationTicks > 0);
        Assert.False(string.IsNullOrEmpty(battleVm.StatusSummary));

        var batchConfig = BatchBattleConfig.Create(config, iterations: 5, baseSeed: 100);
        var batchResult = coordinator.RunBatchBattles(batchConfig);

        var batchVm = presenter.BuildBatchViewModel(batchResult, "Swordsmen", "Spearmen");
        Assert.Equal(5, batchVm.TotalBattles);
        Assert.Contains("battles evaluated", batchVm.SummaryText);
    }
}
