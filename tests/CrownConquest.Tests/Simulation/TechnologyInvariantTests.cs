using System;
using System.Collections.Generic;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class TechnologyInvariantTests
{
    [Fact]
    public void TechResearch_ResourceConservation_Invariant()
    {
        // TC-S04-007: Start research deducts exact cost; cancel refunds exact cost
        var sim = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = sim.State.GetOrCreateResourceBank(factionId);
        bank.Deposit(ResourceType.Food, 500, 1UL);
        bank.Deposit(ResourceType.Gold, 300, 1UL);

        // Advance to Classical so tech is valid
        var eraState = sim.State.GetOrCreateEraState(factionId);
        eraState.TryStartAdvancement(CivilizationEra.Classical, 1, new EntityId(1), ResourceCost.Zero, 1UL, null);
        eraState.AdvanceTicks(1, 2UL, null, out _);

        // Construct Blacksmith
        var blacksmith = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "blacksmith",
            new Vector2D(20f, 20f),
            new Vector2D(3f, 3f),
            startsConstructed: true);
        sim.State.AddBuilding(blacksmith);

        int initialFood = bank.Food; // 500
        int initialGold = bank.Gold; // 300

        // Forging costs 150 Food, 50 Gold
        sim.CommandQueue.Enqueue(new StartResearchCommand(1UL, factionId, blacksmith.Id, "forging"));
        sim.SimulateTicks(1);

        Assert.Equal(initialFood - 150, bank.Food);
        Assert.Equal(initialGold - 50, bank.Gold);
        Assert.Equal(1, blacksmith.ResearchQueue.Count);

        // Cancel research
        sim.CommandQueue.Enqueue(new CancelResearchCommand(2UL, factionId, blacksmith.Id, 0));
        sim.SimulateTicks(1);

        Assert.Equal(initialFood, bank.Food);
        Assert.Equal(initialGold, bank.Gold);
        Assert.True(blacksmith.ResearchQueue.IsEmpty);
    }

    [Fact]
    public void EraAdvancement_TownCenter_LockoutAndCompletionInvariant()
    {
        // TC-S04-008: Advance era deducts costs and transitions era deterministically
        var sim = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = sim.State.GetOrCreateResourceBank(factionId);
        bank.Deposit(ResourceType.Food, 1000, 1UL);
        bank.Deposit(ResourceType.Gold, 500, 1UL);

        var tc = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "town_center",
            new Vector2D(20f, 20f),
            new Vector2D(4f, 4f),
            startsConstructed: true);
        sim.State.AddBuilding(tc);

        var barracks = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "barracks",
            new Vector2D(20f, 26f),
            new Vector2D(3f, 3f),
            startsConstructed: true);
        sim.State.AddBuilding(barracks);

        // Advance to Classical (500 Food, 200 Gold, 100 ticks)
        sim.CommandQueue.Enqueue(new AdvanceEraCommand(1UL, factionId, tc.Id, CivilizationEra.Classical));
        sim.SimulateTicks(1);

        var eraState = sim.State.GetOrCreateEraState(factionId);
        Assert.True(eraState.IsAdvancing);
        Assert.Equal(500, bank.Food);
        Assert.Equal(300, bank.Gold);

        // Duplicate advance command while advancing should be ignored
        sim.CommandQueue.Enqueue(new AdvanceEraCommand(2UL, factionId, tc.Id, CivilizationEra.Classical));
        sim.SimulateTicks(1);
        Assert.Equal(500, bank.Food); // No extra deduction

        // Simulate until completion (100 ticks total)
        sim.SimulateTicks(102);

        Assert.False(eraState.IsAdvancing);
        Assert.Equal(CivilizationEra.Classical, eraState.CurrentEra);
    }

    [Fact]
    public void TechTree_PrerequisiteLock_Invariant()
    {
        // TC-S04-009: Tech commands without prerequisites are rejected without resource deductions
        var sim = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = sim.State.GetOrCreateResourceBank(factionId);
        bank.Deposit(ResourceType.Food, 500, 1UL);
        bank.Deposit(ResourceType.Gold, 500, 1UL);

        var blacksmith = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "blacksmith",
            new Vector2D(20f, 20f),
            new Vector2D(3f, 3f),
            startsConstructed: true);
        sim.State.AddBuilding(blacksmith);

        // Still in Archaic Era -> attempting to research "forging" (requires Classical)
        sim.CommandQueue.Enqueue(new StartResearchCommand(1UL, factionId, blacksmith.Id, "forging"));
        sim.SimulateTicks(1);

        Assert.Equal(500, bank.Food);
        Assert.Equal(500, bank.Gold);
        Assert.True(blacksmith.ResearchQueue.IsEmpty);
    }

    [Fact]
    public void CombatTriangle_RockPaperScissors_Determinism()
    {
        // TC-S04-010: Spearmen counter Cavalry in simulation combat
        var sim = new SimulationEngine();
        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        // F1: 3 Spearmen
        for (int i = 0; i < 3; i++)
        {
            var sp = new UnitEntity(
                sim.State.GenerateEntityId(),
                f1,
                "spearman",
                new Vector2D(20f, 20f + (i * 2f)),
                maxHealth: 100f,
                attackDamage: 12f,
                archetype: UnitArchetype.Spearman);
            sim.State.AddUnit(sp);
            sim.SpatialGrid.Insert(sp.Id, sp.Position);
        }

        // F2: 2 Cavalry
        for (int i = 0; i < 2; i++)
        {
            var cav = new UnitEntity(
                sim.State.GenerateEntityId(),
                f2,
                "cavalry",
                new Vector2D(25f, 20f + (i * 2f)),
                maxHealth: 130f,
                attackDamage: 15f,
                archetype: UnitArchetype.Cavalry);
            sim.State.AddUnit(cav);
            sim.SpatialGrid.Insert(cav.Id, cav.Position);
        }

        // Simulate battle
        sim.SimulateTicks(120);

        int f1Living = 0, f2Living = 0;
        foreach (var u in sim.State.ActiveUnits)
        {
            if (u.IsAlive && u.FactionId == f1) f1Living++;
            if (u.IsAlive && u.FactionId == f2) f2Living++;
        }

        // Spearmen win against Cavalry
        Assert.True(f1Living > 0, "Spearmen should survive.");
        Assert.Equal(0, f2Living);
    }

    [Fact]
    public void Civilization_DeterministicReplay_1000Ticks()
    {
        // TC-S04-011: Two simulation engines with identical commands produce bit-exact state checksums
        var sim1 = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 777 });
        var sim2 = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 777 });

        var f1 = new FactionId(1);
        sim1.State.GetOrCreateResourceBank(f1).Deposit(ResourceType.Food, 1000, 1UL);
        sim2.State.GetOrCreateResourceBank(f1).Deposit(ResourceType.Food, 1000, 1UL);
        sim1.State.GetOrCreateResourceBank(f1).Deposit(ResourceType.Gold, 500, 1UL);
        sim2.State.GetOrCreateResourceBank(f1).Deposit(ResourceType.Gold, 500, 1UL);

        var tc1 = new BuildingEntity(sim1.State.GenerateEntityId(), f1, "town_center", new Vector2D(20f, 20f), new Vector2D(4f, 4f), startsConstructed: true);
        var tc2 = new BuildingEntity(sim2.State.GenerateEntityId(), f1, "town_center", new Vector2D(20f, 20f), new Vector2D(4f, 4f), startsConstructed: true);
        sim1.State.AddBuilding(tc1);
        sim2.State.AddBuilding(tc2);

        var b1 = new BuildingEntity(sim1.State.GenerateEntityId(), f1, "barracks", new Vector2D(20f, 26f), new Vector2D(3f, 3f), startsConstructed: true);
        var b2 = new BuildingEntity(sim2.State.GenerateEntityId(), f1, "barracks", new Vector2D(20f, 26f), new Vector2D(3f, 3f), startsConstructed: true);
        sim1.State.AddBuilding(b1);
        sim2.State.AddBuilding(b2);

        // Enqueue advancement
        sim1.CommandQueue.Enqueue(new AdvanceEraCommand(1UL, f1, tc1.Id, CivilizationEra.Classical));
        sim2.CommandQueue.Enqueue(new AdvanceEraCommand(1UL, f1, tc2.Id, CivilizationEra.Classical));

        for (int step = 0; step < 10; step++)
        {
            sim1.SimulateTicks(50);
            sim2.SimulateTicks(50);

            Assert.Equal(sim1.State.ComputeStateChecksum(), sim2.State.ComputeStateChecksum());
        }
    }
}
