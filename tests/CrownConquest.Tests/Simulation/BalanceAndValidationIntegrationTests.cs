using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

/// <summary>
/// Tier 2, 3, and 4 Integration and Deterministic Invariant Tests for Sprint 14 Balance and Validation.
/// </summary>
public sealed class BalanceAndValidationIntegrationTests
{
    [Fact]
    public void TC_S14_11_BattleSimulator_DeterministicReplayEquality()
    {
        var config = BattleSimulatorConfig.CreateStandardMatchup("swordsman", 8, "spearman", 8, seed: 1337);
        var engine = new BattleSimulatorEngine();

        bool isDeterministic = engine.VerifyDeterministicReplay(config, out string divergenceReason);

        Assert.True(isDeterministic, $"Deterministic replay failed: {divergenceReason}");
    }

    [Fact]
    public void TC_S14_12_BattleSimulator_MatchupVarianceGeneratesDifferentOutcomes()
    {
        var engine = new BattleSimulatorEngine();

        var config1 = BattleSimulatorConfig.CreateStandardMatchup("swordsman", 8, "spearman", 8, seed: 100);
        var config2 = BattleSimulatorConfig.CreateStandardMatchup("cavalry", 6, "archer", 8, seed: 100);

        var run1 = engine.ExecuteBattle(config1);
        var run2 = engine.ExecuteBattle(config2);

        // Different army compositions produce deterministic differences in damage and checksum
        Assert.True(run1.FinalStateChecksum != run2.FinalStateChecksum);
    }

    [Fact]
    public void TC_S14_13_SaveLoadStateValidator_MidBattleParity()
    {
        var validator = new SaveLoadStateValidator();
        var result = validator.ValidateMidSimulationParity(initialTicks: 60, continuationTicks: 60, seed: 42);

        Assert.True(result.IsMatch, $"Save/load mid-simulation parity failed: {result.DivergenceDetails}");
        Assert.Equal(result.OriginalChecksum, result.RestoredChecksum);
        Assert.Equal(result.OriginalAliveUnits, result.RestoredAliveUnits);
    }

    [Fact]
    public void TC_S14_14_SimulationStateSerializer_FullRoundtripFidelity()
    {
        var state = new SimulationState { CurrentTick = 150 };
        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        var bank = state.GetOrCreateResourceBank(f1);
        bank.Deposit(CrownConquest.Domain.Economy.ResourceType.Food, 1200, 0);
        bank.Deposit(CrownConquest.Domain.Economy.ResourceType.Gold, 450, 0);

        var pop = state.GetOrCreatePopulationManager(f1);
        pop.SetCurrentPopulation(15, 0);

        var era = state.GetOrCreateEraState(f1);
        era.TryStartAdvancement(CrownConquest.Domain.Economy.CivilizationEra.Classical, 100, new EntityId(10), CrownConquest.Domain.Economy.ResourceCost.Zero, 0, null);

        var tech = state.GetOrCreateTechManager(f1);
        tech.RestoreUnlockedTech("forging");

        var heroState = new HeroState(HeroClass.Centurion, "Marcus", new HeroAttributes(20, 15, 12), baseLeadershipCapacity: 25);
        var heroUnit = new UnitEntity(
            new EntityId(1),
            f1,
            "centurion",
            new Vector2D(25f, 30f),
            maxHealth: 400f,
            attackDamage: 35f,
            attackRange: 1.8f,
            movementSpeed: 4.0f,
            attackCooldownTicks: 18,
            killXpValue: 300,
            baseArmor: 4f,
            attackType: "melee",
            aggroRange: 15f,
            archetype: UnitArchetype.Hero,
            heroState: heroState);
        state.AddUnit(heroUnit);

        var node = new ResourceNodeEntity(new EntityId(2), CrownConquest.Domain.Economy.ResourceType.Gold, new Vector2D(50f, 50f), maxAmount: 800);
        state.AddResourceNode(node);

        var building = new BuildingEntity(new EntityId(3), f1, "barracks", new Vector2D(20f, 20f), new Vector2D(3f, 3f), maxHealth: 600f, startsConstructed: true);
        state.AddBuilding(building);

        string json = SimulationStateSerializer.SerializeToJson(state, 42);
        var deserializedResult = SimulationStateSerializer.DeserializeFromJson(json);

        Assert.True(deserializedResult.IsSuccess);
        var restored = deserializedResult.Value;
        Assert.NotNull(restored);

        Assert.Equal(state.CurrentTick, restored.CurrentTick);
        Assert.Equal(1200, restored.GetOrCreateResourceBank(f1).Food);
        Assert.Equal(450, restored.GetOrCreateResourceBank(f1).Gold);
        Assert.Equal(15, restored.GetOrCreatePopulationManager(f1).CurrentPopulation);
        Assert.True(restored.GetOrCreateTechManager(f1).IsResearched("forging"));
        Assert.True(restored.TryGetUnit(new EntityId(1), out var restoredHero));
        Assert.NotNull(restoredHero?.HeroState);
        Assert.Equal("Marcus", restoredHero.HeroState.HeroName);
        Assert.True(restored.TryGetBuilding(new EntityId(3), out var restoredBuilding));
        Assert.True(restoredBuilding?.IsConstructed);
    }

    [Fact]
    public void TC_S14_15_ProgressionScaling_UnitLevelingAndVeterancyTiers()
    {
        var bus = new DomainEventBus();
        var unit = new UnitEntity(
            new EntityId(1),
            new FactionId(1),
            "swordsman",
            new Vector2D(10f, 10f),
            maxHealth: 100f,
            attackDamage: 15f,
            baseArmor: 2f,
            healthPerLevelBonus: 10f,
            damagePerLevelBonus: 2f);

        Assert.Equal(1, unit.Veterancy.Level);
        Assert.Equal(VeterancyRank.Recruit, unit.Veterancy.Rank);
        Assert.Equal(100f, unit.MaxHealth);
        Assert.Equal(15f, unit.AttackDamage);

        // Level up to 3 (Experienced)
        unit.Veterancy.AwardXp(250, 0, bus, out _, out _);
        Assert.Equal(3, unit.Veterancy.Level);
        Assert.Equal(VeterancyRank.Experienced, unit.Veterancy.Rank);
        Assert.Equal(120f, unit.MaxHealth); // 100 + 2 * 10
        Assert.Equal(19f, unit.AttackDamage); // 15 + 2 * 2

        // Level up to 5 (Veteran)
        unit.Veterancy.AwardXp(450, 0, bus, out _, out _); // total 700
        Assert.Equal(5, unit.Veterancy.Level);
        Assert.Equal(VeterancyRank.Veteran, unit.Veterancy.Rank);
        Assert.Equal(140f, unit.MaxHealth);
        Assert.Equal(23f, unit.AttackDamage);

        // Level up to 9 (Legendary)
        unit.Veterancy.AwardXp(1500, 0, bus, out _, out _); // total 2200+
        Assert.True(unit.Veterancy.Level >= 9);
        Assert.Equal(VeterancyRank.Legendary, unit.Veterancy.Rank);
    }

    [Fact]
    public void TC_S14_16_SimulationSoakHarness_Runs1000TicksWithZeroLeaks()
    {
        var harness = new SimulationSoakHarness();
        var config = SoakTestConfig.CreateFast(ticks: 1000);
        var result = harness.RunSoakTest(config);

        Assert.True(result.IsSuccessful, $"Soak test failed: {result.SummaryDetails}");
        Assert.Equal(1000, result.TotalTicksExecuted);
        Assert.True(result.IsMemoryBounded);
        Assert.True(result.PeakMemoryMb < 500f);
        Assert.True(result.IsSpatialGridConsistent);
        Assert.True(result.TotalUnitsSpawned > 0);
    }

    [Fact]
    public void TC_S14_17_CombatTriangleBalance_SpearmenVsCavalry()
    {
        // 8 Spearmen vs 6 Cavalry with counter bonuses
        var matchup = BattleSimulatorConfig.CreateStandardMatchup("spearman", 8, "cavalry", 6, seed: 100);
        var batchConfig = BatchBattleConfig.Create(matchup, iterations: 10, baseSeed: 200);

        var runner = new BatchBattleRunner();
        var result = runner.RunBatch(batchConfig);

        Assert.Equal(10, result.TotalBattles);
        Assert.True(result.WinRateA > 0.50f, $"Spearmen should reliably defeat cavalry (Win Rate: {result.WinRateA:P1})");
    }

    [Fact]
    public void TC_S14_18_CombatTriangleBalance_CavalryVsArchers()
    {
        // 6 Cavalry vs 8 Archers
        var matchup = BattleSimulatorConfig.CreateStandardMatchup("cavalry", 6, "archer", 8, seed: 300);
        var batchConfig = BatchBattleConfig.Create(matchup, iterations: 10, baseSeed: 400);

        var runner = new BatchBattleRunner();
        var result = runner.RunBatch(batchConfig);

        Assert.Equal(10, result.TotalBattles);
        Assert.True(result.WinRateA > 0.50f, $"Cavalry should flank and defeat archers (Win Rate: {result.WinRateA:P1})");
    }

    [Fact]
    public void TC_S14_19_AiDifficulty_ControllerAcceptsDifficultyModifiers()
    {
        var f1 = new FactionId(1);
        var easyAi = new AiFactionController(f1, new Vector2D(10f, 10f), difficulty: AiDifficultyConfig.CreateEasy());
        var brutalAi = new AiFactionController(f1, new Vector2D(10f, 10f), difficulty: AiDifficultyConfig.CreateBrutal());

        Assert.Equal(AiDifficultyTier.Easy, easyAi.Difficulty.Tier);
        Assert.Equal(0.75f, easyAi.Difficulty.ResourceGatherMultiplier);

        Assert.Equal(AiDifficultyTier.Brutal, brutalAi.Difficulty.Tier);
        Assert.Equal(1.50f, brutalAi.Difficulty.ResourceGatherMultiplier);
    }

    [Fact]
    public void TC_S14_20_HeroArmyAttachment_EnhancesCombatPerformance()
    {
        var engine = new BattleSimulatorEngine();

        // Standard 8v8 army
        var standardConfig = new BattleSimulatorConfig
        {
            RandomSeed = 555,
            TeamA = new ArmyRosterConfig(new FactionId(1), "Team A", new Vector2D(26f, 32f)).AddUnits("swordsman", 8),
            TeamB = new ArmyRosterConfig(new FactionId(2), "Team B", new Vector2D(38f, 32f)).AddUnits("swordsman", 8)
        };

        // Army with Attached Hero on Team A
        var heroConfig = new BattleSimulatorConfig
        {
            RandomSeed = 555,
            TeamA = new ArmyRosterConfig(new FactionId(1), "Team A", new Vector2D(26f, 32f))
                .AddUnits("swordsman", 8)
                .SetHero("warlord", HeroClass.Warlord, 1),
            TeamB = new ArmyRosterConfig(new FactionId(2), "Team B", new Vector2D(38f, 32f))
                .AddUnits("swordsman", 8)
        };

        var heroResult = engine.ExecuteBattle(heroConfig);

        Assert.Equal(new FactionId(1), heroResult.WinnerFaction);
        Assert.True(heroResult.SurvivingUnitsA > 0);
        Assert.True(heroResult.TotalDamageDealtA > heroResult.TotalDamageDealtB);
    }

    [Fact]
    public void TC_S14_21_BalanceAndValidationScenario_ExecutesSuccessfully()
    {
        var scenario = new BalanceAndValidationScenario();
        bool success = scenario.RunCompleteScenario();

        Assert.True(success, "Balance and validation scenario failed to execute completely.");
        Assert.NotNull(scenario.LatestBattleVm);
        Assert.NotNull(scenario.LatestBatchVm);
        Assert.NotNull(scenario.LatestFactionVm);
        Assert.NotNull(scenario.LatestProgressionReport);
        Assert.NotNull(scenario.LatestSaveLoadResult);
        Assert.NotNull(scenario.LatestSoakVm);

        Assert.True(scenario.LatestProgressionReport.IsValid);
        Assert.True(scenario.LatestSaveLoadResult.IsMatch);
        Assert.True(scenario.LatestSoakVm.Value.IsSuccessful);
    }

    [Fact]
    public void TC_S14_22_DeterministicReplayParity_1000TicksChecksumParity()
    {
        var config = new SimulationConfig
        {
            InitialRandomSeed = 9999,
            TicksPerSecond = 20
        };

        // Run 1
        var eventBus1 = new DomainEventBus();
        var sim1 = new SimulationEngine(config, eventBus1);
        SetupSampleWorld(sim1);
        for (int t = 0; t < 1000; t++) sim1.Tick();
        ulong checksum1 = sim1.State.ComputeStateChecksum();

        // Run 2
        var eventBus2 = new DomainEventBus();
        var sim2 = new SimulationEngine(config, eventBus2);
        SetupSampleWorld(sim2);
        for (int t = 0; t < 1000; t++) sim2.Tick();
        ulong checksum2 = sim2.State.ComputeStateChecksum();

        Assert.Equal(checksum1, checksum2);
    }

    private static void SetupSampleWorld(SimulationEngine sim)
    {
        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        sim.State.GetOrCreateResourceBank(f1).Deposit(CrownConquest.Domain.Economy.ResourceType.Food, 1000, 0);
        sim.State.GetOrCreateResourceBank(f2).Deposit(CrownConquest.Domain.Economy.ResourceType.Food, 1000, 0);

        for (int i = 0; i < 10; i++)
        {
            var u1 = new UnitEntity(
                sim.State.GenerateEntityId(),
                f1,
                "swordsman",
                new Vector2D(15f + i, 20f + (i % 3)),
                120f,
                16f,
                1.5f,
                3.5f,
                18,
                50,
                3f,
                "melee",
                14f,
                archetype: UnitArchetype.Infantry);
            sim.State.AddUnit(u1);

            var u2 = new UnitEntity(
                sim.State.GenerateEntityId(),
                f2,
                "spearman",
                new Vector2D(45f - i, 20f + (i % 3)),
                110f,
                14f,
                2.0f,
                3.2f,
                20,
                50,
                2f,
                "melee",
                14f,
                archetype: UnitArchetype.Spearman);
            sim.State.AddUnit(u2);
        }
    }
}
