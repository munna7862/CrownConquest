using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class SiegeCombatInvariantTests
{
    [Fact]
    public void TC_S07_09_WallDestruction_CreatesRubbleTerrainAndPublishesWallBreachedEvent()
    {
        // Arrange
        var engine = new SimulationEngine();
        var factionDef = new FactionId(1);
        var factionAtk = new FactionId(2);

        var wallId = engine.State.GenerateEntityId();
        var wallPos = new Vector2D(5f, 5f);
        var wall = new BuildingEntity(
            wallId,
            factionDef,
            "wooden_wall",
            wallPos,
            new Vector2D(1f, 1f),
            maxHealth: 50f, // Low HP for instant breach
            startsConstructed: true);
        engine.State.AddBuilding(wall);

        var ramId = engine.State.GenerateEntityId();
        var ram = new UnitEntity(
            ramId,
            factionAtk,
            "roman_battering_ram",
            new Vector2D(5f, 4f),
            attackDamage: 50f,
            attackRange: 1.8f,
            archetype: UnitArchetype.Siege);
        engine.State.AddUnit(ram);

        bool breachedEventReceived = false;
        WallBreachedEvent capturedEvent = default;
        engine.EventBus.Subscribe<WallBreachedEvent>((in WallBreachedEvent evt) =>
        {
            breachedEventReceived = true;
            capturedEvent = evt;
        });

        // Act: Order Ram to attack the wall
        engine.CommandQueue.Enqueue(new AttackBuildingCommand(factionAtk, new[] { ramId }, wallId));
        engine.SimulateTicks(5);

        // Assert: Wall should be destroyed, breached event emitted, terrain set to Rubble
        Assert.True(breachedEventReceived, "WallBreachedEvent should have been published.");
        Assert.Equal(wallId, capturedEvent.BuildingId);
        Assert.Equal(factionDef, capturedEvent.FactionId);

        var (gx, gy) = engine.State.TerrainGrid.WorldToGrid(wallPos);
        Assert.Equal(TerrainType.Rubble, engine.State.TerrainGrid.GetTerrain(gx, gy));
        Assert.Single(engine.State.Breaches);
    }

    [Fact]
    public void TC_S07_10_GatePassability_EvaluatesCorrectlyForFriendliesAndEnemies()
    {
        // Arrange
        var gate = new GateDefenseState(GateState.Closed);

        // Assert Closed state
        Assert.True(gate.IsPassableForFriendlies);
        Assert.False(gate.IsPassableForEnemies);

        // Toggle to Open
        gate.Toggle();
        Assert.True(gate.IsPassableForFriendlies);
        Assert.True(gate.IsPassableForEnemies);

        // Lock gate
        gate.TrySetState(GateState.Locked);
        Assert.False(gate.IsPassableForEnemies);
    }

    [Fact]
    public void TC_S07_11_Tower_AutonomousDefenseAcquiresAndDamagesEnemyTargets()
    {
        // Arrange
        var engine = new SimulationEngine();
        var factionDef = new FactionId(1);
        var factionAtk = new FactionId(2);

        var towerId = engine.State.GenerateEntityId();
        var tower = new BuildingEntity(
            towerId,
            factionDef,
            "watchtower",
            new Vector2D(0f, 0f),
            new Vector2D(2f, 2f),
            maxHealth: 600f,
            startsConstructed: true);
        engine.State.AddBuilding(tower);

        var enemyId = engine.State.GenerateEntityId();
        var enemy = new UnitEntity(
            enemyId,
            factionAtk,
            "roman_legionary",
            new Vector2D(3f, 0f), // Within range (8.0)
            maxHealth: 140f,
            baseArmor: 0f);
        engine.State.AddUnit(enemy);

        bool towerFired = false;
        engine.EventBus.Subscribe<TowerAttackEvent>((in TowerAttackEvent evt) =>
        {
            towerFired = true;
        });

        // Act: Advance simulation by 5 ticks
        engine.SimulateTicks(5);

        // Assert: Tower should have fired and dealt damage to enemy
        Assert.True(towerFired, "Tower should have fired at enemy in range.");
        Assert.True(enemy.CurrentHealth < 140f, $"Enemy health ({enemy.CurrentHealth}) should be reduced by tower arrow damage.");
    }

    [Fact]
    public void TC_S07_12_Catapult_DealsAreaOfEffectDamageToClusteredTargets()
    {
        // Arrange
        var engine = new SimulationEngine();
        var factionAtk = new FactionId(1);
        var factionDef = new FactionId(2);

        var catapultId = engine.State.GenerateEntityId();
        var catapult = new UnitEntity(
            catapultId,
            factionAtk,
            "celtic_catapult",
            new Vector2D(0f, 0f),
            attackDamage: 40f,
            attackRange: 12.0f,
            archetype: UnitArchetype.Siege);
        engine.State.AddUnit(catapult);

        // Target wall at (6, 0)
        var wallId = engine.State.GenerateEntityId();
        var wall = new BuildingEntity(
            wallId,
            factionDef,
            "wooden_wall",
            new Vector2D(6f, 0f),
            new Vector2D(1f, 1f),
            maxHealth: 500f,
            startsConstructed: true);
        engine.State.AddBuilding(wall);

        // Defender unit standing near wall at (6.5, 0.5) (dist ~0.7 < splash 2.5)
        var defenderNearId = engine.State.GenerateEntityId();
        var defenderNear = new UnitEntity(
            defenderNearId,
            factionDef,
            "roman_legionary",
            new Vector2D(6.5f, 0.5f),
            maxHealth: 100f);
        engine.State.AddUnit(defenderNear);

        bool aoeEventPublished = false;
        engine.EventBus.Subscribe<SiegeAreaOfEffectImpactEvent>((in SiegeAreaOfEffectImpactEvent evt) =>
        {
            aoeEventPublished = true;
        });

        // Act: Catapult attacks wall
        engine.CommandQueue.Enqueue(new AttackBuildingCommand(factionAtk, new[] { catapultId }, wallId));
        engine.SimulateTicks(5);

        // Assert: Both wall and nearby unit should have taken damage
        Assert.True(aoeEventPublished, "AoE splash impact event should have been published.");
        Assert.True(wall.CurrentHealth < 500f, "Wall should have taken primary siege damage.");
        Assert.True(defenderNear.CurrentHealth < 100f, $"Nearby defender ({defenderNear.CurrentHealth}) should have taken splash damage.");
    }
}
