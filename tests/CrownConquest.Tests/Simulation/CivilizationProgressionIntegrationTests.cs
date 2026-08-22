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

public sealed class CivilizationProgressionIntegrationTests
{
    [Fact]
    public void Integration_Blacksmith_UpgradeApplication_ActiveUnits()
    {
        // TC-S04-012: Completing Forging at Blacksmith boosts melee damage on existing active units
        var sim = new SimulationEngine();
        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        var bank = sim.State.GetOrCreateResourceBank(f1);
        bank.Deposit(ResourceType.Food, 500, 1UL);
        bank.Deposit(ResourceType.Gold, 200, 1UL);

        // Put F1 in Classical Era
        var eraState = sim.State.GetOrCreateEraState(f1);
        eraState.TryStartAdvancement(CivilizationEra.Classical, 1, new EntityId(1), ResourceCost.Zero, 1UL, null);
        eraState.AdvanceTicks(1, 2UL, null, out _);

        // Spawn Blacksmith
        var blacksmith = new BuildingEntity(
            sim.State.GenerateEntityId(),
            f1,
            "blacksmith",
            new Vector2D(10f, 10f),
            new Vector2D(3f, 3f),
            startsConstructed: true);
        sim.State.AddBuilding(blacksmith);

        // Spawn F1 Swordsman and F2 Enemy Swordsman (high health to measure hit)
        var friendlySwordsman = new UnitEntity(
            sim.State.GenerateEntityId(),
            f1,
            "celtic_swordsman",
            new Vector2D(20f, 20f),
            maxHealth: 200f,
            attackDamage: 18f,
            baseArmor: 0f,
            attackCooldownTicks: 10,
            aggroRange: 0f);
        sim.State.AddUnit(friendlySwordsman);
        sim.SpatialGrid.Insert(friendlySwordsman.Id, friendlySwordsman.Position);

        var enemy = new UnitEntity(
            sim.State.GenerateEntityId(),
            f2,
            "enemy_dummy",
            new Vector2D(21f, 20f),
            maxHealth: 200f,
            attackDamage: 0f,
            baseArmor: 0f,
            attackCooldownTicks: 100,
            aggroRange: 0f);
        sim.State.AddUnit(enemy);
        sim.SpatialGrid.Insert(enemy.Id, enemy.Position);

        // 1. Attack once without tech -> Deal 18 damage -> Enemy HP = 182
        friendlySwordsman.Attack(enemy.Id);
        sim.SimulateTicks(1);
        Assert.Equal(182f, enemy.CurrentHealth, 0.1f);

        // Stop attack while research is running
        friendlySwordsman.Stop();

        // Order Forging research (duration 40 ticks, +2 melee damage)
        sim.CommandQueue.Enqueue(new StartResearchCommand(2UL, f1, blacksmith.Id, "forging"));
        sim.SimulateTicks(45);

        var techManager = sim.State.GetOrCreateTechManager(f1);
        Assert.True(techManager.IsResearched("forging"));
        Assert.Equal(2, techManager.Modifiers.MeleeAttackBonus);

        // 2. Attack with Forging active -> Deal 18 + 2 = 20 damage -> Enemy HP = 182 - 20 = 162
        friendlySwordsman.Attack(enemy.Id);
        sim.SimulateTicks(2);
        Assert.Equal(162f, enemy.CurrentHealth, 0.1f);
    }

    [Fact]
    public void Integration_ArcheryRange_TrainingAndRangedCombat()
    {
        // TC-S04-013: Train Archer at Archery Range and attack melee target from standoff range
        var sim = new SimulationEngine();
        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        var bank = sim.State.GetOrCreateResourceBank(f1);
        bank.Deposit(ResourceType.Food, 200, 1UL);
        bank.Deposit(ResourceType.Wood, 100, 1UL);

        var archeryRange = new BuildingEntity(
            sim.State.GenerateEntityId(),
            f1,
            "archery_range",
            new Vector2D(20f, 20f),
            new Vector2D(3f, 3f),
            startsConstructed: true,
            rallyPoint: new Vector2D(25f, 20f));
        sim.State.AddBuilding(archeryRange);

        EntityId trainedArcherId = EntityId.None;
        sim.EventBus.Subscribe<ProductionCompletedEvent>((in ProductionCompletedEvent e) =>
        {
            trainedArcherId = e.ProducedUnitId;
        });

        // Queue Archer (60 ticks)
        sim.CommandQueue.Enqueue(new QueueProductionCommand(1UL, f1, archeryRange.Id, "celtic_archer"));
        sim.SimulateTicks(65);

        Assert.True(trainedArcherId.IsValid);
        Assert.True(sim.State.TryGetUnit(trainedArcherId, out var archer) && archer != null);
        Assert.Equal(UnitArchetype.Archer, archer!.Archetype);
        Assert.Equal(8.0f, archer.AttackRange);

        // Spawn stationary enemy at distance 7.0 units
        var enemy = new UnitEntity(
            sim.State.GenerateEntityId(),
            f2,
            "enemy_soldier",
            new Vector2D(32f, 20f), // Distance 7.0 from (25, 20)
            maxHealth: 100f,
            attackDamage: 10f,
            attackRange: 1.5f);
        sim.State.AddUnit(enemy);
        sim.SpatialGrid.Insert(enemy.Id, enemy.Position);

        // Archer attacks from range
        archer.Attack(enemy.Id);
        sim.SimulateTicks(1);

        // Archer stays in place (doesn't need to move) and deals damage
        Assert.Equal(25f, archer.Position.X, 0.1f);
        Assert.True(enemy.CurrentHealth < 100f, "Enemy should have taken ranged arrow damage.");
    }

    [Fact]
    public void Integration_Stable_CavalryTrainingAndHighSpeedFlank()
    {
        // TC-S04-014: Stable trains Cavalry with high mobility
        var sim = new SimulationEngine();
        var f1 = new FactionId(1);
        var bank = sim.State.GetOrCreateResourceBank(f1);
        bank.Deposit(ResourceType.Food, 200, 1UL);
        bank.Deposit(ResourceType.Gold, 100, 1UL);

        var stable = new BuildingEntity(
            sim.State.GenerateEntityId(),
            f1,
            "stable",
            new Vector2D(20f, 20f),
            new Vector2D(3f, 3f),
            startsConstructed: true,
            rallyPoint: new Vector2D(25f, 20f));
        sim.State.AddBuilding(stable);

        EntityId trainedCavId = EntityId.None;
        sim.EventBus.Subscribe<ProductionCompletedEvent>((in ProductionCompletedEvent e) =>
        {
            trainedCavId = e.ProducedUnitId;
        });

        sim.CommandQueue.Enqueue(new QueueProductionCommand(1UL, f1, stable.Id, "scout_cavalry"));
        sim.SimulateTicks(85);

        Assert.True(trainedCavId.IsValid);
        Assert.True(sim.State.TryGetUnit(trainedCavId, out var cav) && cav != null);
        Assert.Equal(UnitArchetype.Cavalry, cav!.Archetype);
        Assert.Equal(5.5f, cav.MovementSpeed);

        // Move cavalry 55 units -> at 5.5 speed, takes exactly 10 seconds / 200 ticks
        cav.Move(new Vector2D(80f, 20f));
        sim.SimulateTicks(200);

        Assert.Equal(80f, cav.Position.X, 0.1f);
    }

    [Fact]
    public void Integration_EraProgression_UnlocksNewBuildingsAndUnits()
    {
        // TC-S04-015: Faction advances from Archaic -> Classical -> Imperial unlocking tech & infrastructure
        var sim = new SimulationEngine();
        var f1 = new FactionId(1);
        var bank = sim.State.GetOrCreateResourceBank(f1);
        bank.Deposit(ResourceType.Food, 2000, 1UL);
        bank.Deposit(ResourceType.Gold, 1000, 1UL);
        bank.Deposit(ResourceType.Iron, 500, 1UL);

        var tc = new BuildingEntity(sim.State.GenerateEntityId(), f1, "town_center", new Vector2D(30f, 30f), new Vector2D(4f, 4f), startsConstructed: true);
        var barracks = new BuildingEntity(sim.State.GenerateEntityId(), f1, "barracks", new Vector2D(30f, 20f), new Vector2D(3f, 3f), startsConstructed: true);
        sim.State.AddBuilding(tc);
        sim.State.AddBuilding(barracks);

        var eraState = sim.State.GetOrCreateEraState(f1);
        Assert.Equal(CivilizationEra.Archaic, eraState.CurrentEra);

        // Advance to Classical Era
        sim.CommandQueue.Enqueue(new AdvanceEraCommand(1UL, f1, tc.Id, CivilizationEra.Classical));
        sim.SimulateTicks(105);

        Assert.Equal(CivilizationEra.Classical, eraState.CurrentEra);

        // Place and construct Blacksmith & Stable
        var blacksmith = new BuildingEntity(sim.State.GenerateEntityId(), f1, "blacksmith", new Vector2D(40f, 30f), new Vector2D(3f, 3f), startsConstructed: true);
        var stable = new BuildingEntity(sim.State.GenerateEntityId(), f1, "stable", new Vector2D(40f, 40f), new Vector2D(3f, 3f), startsConstructed: true);
        sim.State.AddBuilding(blacksmith);
        sim.State.AddBuilding(stable);

        // Research Forging in Classical Era
        sim.CommandQueue.Enqueue(new StartResearchCommand(106UL, f1, blacksmith.Id, "forging"));
        sim.SimulateTicks(45);

        var techManager = sim.State.GetOrCreateTechManager(f1);
        Assert.True(techManager.IsResearched("forging"));

        // Advance to Imperial Era (requires Blacksmith + Stable)
        sim.CommandQueue.Enqueue(new AdvanceEraCommand(155UL, f1, tc.Id, CivilizationEra.Imperial));
        sim.SimulateTicks(155);

        Assert.Equal(CivilizationEra.Imperial, eraState.CurrentEra);

        // In Imperial Era, research Iron Weapons (Tier 2)
        sim.CommandQueue.Enqueue(new StartResearchCommand(315UL, f1, blacksmith.Id, "iron_weapons"));
        sim.SimulateTicks(65);

        Assert.True(techManager.IsResearched("iron_weapons"));
        Assert.Equal(5, techManager.Modifiers.MeleeAttackBonus); // 2 (Forging) + 3 (Iron Weapons)
    }
}
