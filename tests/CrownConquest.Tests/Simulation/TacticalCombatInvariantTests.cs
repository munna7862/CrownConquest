using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class TacticalCombatInvariantTests
{
    [Fact]
    public void TC_S06_10_RoutingInvariant_UnitWithZeroMoraleEntersRoutedAndCannotAttack()
    {
        var engine = new SimulationEngine();
        var faction1 = new FactionId(1);
        var faction2 = new FactionId(2);

        var id1 = engine.State.GenerateEntityId();
        var swordsman = new UnitEntity(
            id1,
            faction1,
            "swordsman",
            new Vector2D(0, 0),
            maxHealth: 100f,
            attackDamage: 20f);

        var id2 = engine.State.GenerateEntityId();
        var enemy = new UnitEntity(
            id2,
            faction2,
            "swordsman",
            new Vector2D(1.5f, 0),
            maxHealth: 100f,
            attackDamage: 20f);

        engine.State.AddUnit(swordsman);
        engine.State.AddUnit(enemy);
        engine.SpatialGrid.Insert(swordsman.Id, swordsman.Position);
        engine.SpatialGrid.Insert(enemy.Id, enemy.Position);

        swordsman.Attack(enemy.Id);

        // Apply total morale collapse
        swordsman.Morale.SetMorale(0f);
        Assert.True(swordsman.IsRouted);

        // Advance tick
        engine.SimulateTicks(1);

        // Unit must enter Routed state and drop attack target
        Assert.Equal(UnitState.Routed, swordsman.State);
        Assert.Equal(EntityId.None, swordsman.AttackTargetId);
        Assert.True(swordsman.MoveTarget.HasValue); // Fleeing towards safe retreat
    }

    [Fact]
    public void TC_S06_11_HeroRallyInvariant_RalliesRoutedUnitBackToControllableState()
    {
        var engine = new SimulationEngine();
        var faction1 = new FactionId(1);

        var unitId = engine.State.GenerateEntityId();
        var unit = new UnitEntity(
            unitId,
            faction1,
            "swordsman",
            new Vector2D(0, 0),
            maxHealth: 100f,
            attackDamage: 20f);

        engine.State.AddUnit(unit);
        engine.SpatialGrid.Insert(unit.Id, unit.Position);

        // Force into routed state
        unit.Morale.SetMorale(0f);
        engine.SimulateTicks(1);
        Assert.Equal(UnitState.Routed, unit.State);

        // Dispatch RallyUnitCommand
        engine.CommandQueue.Enqueue(new RallyUnitCommand(faction1, unit.Id));
        engine.SimulateTicks(1);

        // Unit morale is restored >= 25, unit returns to Idle
        Assert.True(unit.Morale.CurrentMorale >= 25.0f);
        Assert.Equal(UnitState.Idle, unit.State);
        Assert.False(unit.IsRouted);
    }

    [Fact]
    public void TC_S06_12_TerrainMovementSimulation_AdjustsMovementSpeedDeterministically()
    {
        var engine = new SimulationEngine();
        var faction1 = new FactionId(1);

        // Set up Road on Left (X: -20 to -10), Forest in Middle (X: -5 to 5), Marsh on Right (X: 10 to 20)
        var grid = engine.State.TerrainGrid;
        var (gxRoad, gyRoad) = grid.WorldToGrid(new Vector2D(-15f, 0f));
        grid.SetTerrainRect(gxRoad - 2, gyRoad - 2, 5, 5, TerrainType.Road);

        var (gxForest, gyForest) = grid.WorldToGrid(new Vector2D(0f, 0f));
        grid.SetTerrainRect(gxForest - 2, gyForest - 2, 5, 5, TerrainType.Forest);

        var (gxMarsh, gyMarsh) = grid.WorldToGrid(new Vector2D(15f, 0f));
        grid.SetTerrainRect(gxMarsh - 2, gyMarsh - 2, 5, 5, TerrainType.Marsh);

        var uRoad = new UnitEntity(engine.State.GenerateEntityId(), faction1, "scout", new Vector2D(-15f, 0f), movementSpeed: 4.0f);
        var uForest = new UnitEntity(engine.State.GenerateEntityId(), faction1, "scout", new Vector2D(0f, 0f), movementSpeed: 4.0f);
        var uMarsh = new UnitEntity(engine.State.GenerateEntityId(), faction1, "scout", new Vector2D(15f, 0f), movementSpeed: 4.0f);

        engine.State.AddUnit(uRoad);
        engine.State.AddUnit(uForest);
        engine.State.AddUnit(uMarsh);

        engine.SimulateTicks(1);

        // Verify effective movement speed according to terrain
        Assert.Equal(4.0f * 1.25f, uRoad.EffectiveMovementSpeed, precision: 2); // Road: 5.0
        Assert.Equal(4.0f * 0.80f, uForest.EffectiveMovementSpeed, precision: 2); // Forest: 3.2
        Assert.Equal(4.0f * 0.60f, uMarsh.EffectiveMovementSpeed, precision: 2); // Marsh: 2.4
    }

    [Fact]
    public void TC_S06_13_Deterministic1000TickReplay_StateChecksumBitEquality()
    {
        ulong RunSimulation(int seed)
        {
            var config = new SimulationConfig { InitialRandomSeed = seed };
            var engine = new SimulationEngine(config);

            var blueFaction = new FactionId(1);
            var redFaction = new FactionId(2);

            // Configure terrain features
            var grid = engine.State.TerrainGrid;
            grid.SetTerrainRect(28, 40, 16, 12, TerrainType.Hills);
            grid.SetTerrainRect(28, 12, 16, 12, TerrainType.Marsh);
            grid.SetTerrainRect(48, 20, 12, 24, TerrainType.Forest);

            // Blue army: 3 spearmen in Shield Wall, 2 archers on hills
            for (int i = 0; i < 3; i++)
            {
                var spear = new UnitEntity(
                    engine.State.GenerateEntityId(),
                    blueFaction,
                    "spearman",
                    new Vector2D(-5f, (i * 2f) - 2f),
                    maxHealth: 100f,
                    attackDamage: 14f,
                    movementSpeed: 3.5f,
                    archetype: UnitArchetype.Spearman,
                    formation: FormationType.ShieldWall);
                engine.State.AddUnit(spear);
                engine.SpatialGrid.Insert(spear.Id, spear.Position);
            }

            for (int i = 0; i < 2; i++)
            {
                var archer = new UnitEntity(
                    engine.State.GenerateEntityId(),
                    blueFaction,
                    "archer",
                    new Vector2D(-8f, (i * 2f) - 1f),
                    maxHealth: 80f,
                    attackDamage: 12f,
                    attackRange: 7.0f,
                    attackType: "ranged",
                    archetype: UnitArchetype.Archer);
                engine.State.AddUnit(archer);
                engine.SpatialGrid.Insert(archer.Id, archer.Position);
            }

            // Red army: 4 cavalry in Wedge
            for (int i = 0; i < 4; i++)
            {
                var cav = new UnitEntity(
                    engine.State.GenerateEntityId(),
                    redFaction,
                    "cavalry",
                    new Vector2D(15f, (i * 2f) - 3f),
                    maxHealth: 120f,
                    attackDamage: 18f,
                    movementSpeed: 4.5f,
                    archetype: UnitArchetype.Cavalry,
                    formation: FormationType.Wedge);
                engine.State.AddUnit(cav);
                engine.SpatialGrid.Insert(cav.Id, cav.Position);
            }

            // Advance 1,000 fixed simulation ticks
            for (int t = 0; t < 1000; t++)
            {
                engine.Tick();
            }

            return engine.State.ComputeStateChecksum();
        }

        ulong run1 = RunSimulation(42);
        ulong run2 = RunSimulation(42);

        Assert.Equal(run1, run2);
    }
}
