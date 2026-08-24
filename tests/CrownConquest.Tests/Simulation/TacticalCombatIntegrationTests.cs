using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class TacticalCombatIntegrationTests
{
    [Fact]
    public void TC_S06_14_ShieldWallVsCavalryIntegration_SpearmenInShieldWallDefeatChargingCavalryDecisively()
    {
        var engine = new SimulationEngine();
        var blueFaction = new FactionId(1);
        var redFaction = new FactionId(2);

        // 3 Blue Spearmen in Shield Wall at (0, 0)
        var blueSpears = new List<UnitEntity>();
        for (int i = 0; i < 3; i++)
        {
            var spear = new UnitEntity(
                engine.State.GenerateEntityId(),
                blueFaction,
                "triarius",
                new Vector2D(0f, (i * 1.5f) - 1.5f),
                maxHealth: 120f,
                attackDamage: 15f,
                attackRange: 1.6f,
                movementSpeed: 3.5f,
                baseArmor: 2.0f,
                archetype: UnitArchetype.Spearman,
                formation: FormationType.ShieldWall);

            spear.HeadingDirection = new Vector2D(1f, 0f);
            engine.State.AddUnit(spear);
            engine.SpatialGrid.Insert(spear.Id, spear.Position);
            blueSpears.Add(spear);
        }

        // 3 Red Cavalry in Wedge starting at (20, 0) and attacking Blue Spearmen
        var redCavs = new List<UnitEntity>();
        for (int i = 0; i < 3; i++)
        {
            var cav = new UnitEntity(
                engine.State.GenerateEntityId(),
                redFaction,
                "equite",
                new Vector2D(20f, (i * 1.5f) - 1.5f),
                maxHealth: 130f,
                attackDamage: 18f,
                attackRange: 1.5f,
                movementSpeed: 5.0f,
                baseArmor: 2.0f,
                archetype: UnitArchetype.Cavalry,
                formation: FormationType.Wedge);

            cav.HeadingDirection = new Vector2D(-1f, 0f);
            engine.State.AddUnit(cav);
            engine.SpatialGrid.Insert(cav.Id, cav.Position);
            redCavs.Add(cav);
        }

        // Order cavalry to attack spearmen
        for (int i = 0; i < 3; i++)
        {
            redCavs[i].Attack(blueSpears[i].Id);
            blueSpears[i].Attack(redCavs[i].Id);
        }

        // Simulate combat encounter (150 ticks = 7.5 seconds)
        engine.SimulateTicks(150);

        // Spearmen in Shield Wall should win decisively due to charge negation, recoil reflection, and spear multiplier
        int blueLiving = 0;
        for (int i = 0; i < blueSpears.Count; i++) if (blueSpears[i].IsAlive) blueLiving++;

        int redLiving = 0;
        for (int i = 0; i < redCavs.Count; i++) if (redCavs[i].IsAlive) redLiving++;

        Assert.True(blueLiving >= 2);
        Assert.Equal(0, redLiving);
    }

    [Fact]
    public void TC_S06_15_HighGroundArcherSkirmish_HighGroundWinsDueToRangeAndDamageBonus()
    {
        var engine = new SimulationEngine();
        var blueFaction = new FactionId(1);
        var redFaction = new FactionId(2);

        // Configure Hills at (0, 0)
        var grid = engine.State.TerrainGrid;
        var (gxHill, gyHill) = grid.WorldToGrid(new Vector2D(0, 0));
        grid.SetTerrainRect(gxHill - 2, gyHill - 2, 5, 5, TerrainType.Hills);

        // Configure Marsh at (8, 0)
        var (gxMarsh, gyMarsh) = grid.WorldToGrid(new Vector2D(8.0f, 0));
        grid.SetTerrainRect(gxMarsh - 2, gyMarsh - 2, 5, 5, TerrainType.Marsh);

        var blueArcher = new UnitEntity(
            engine.State.GenerateEntityId(),
            blueFaction,
            "archer_high",
            new Vector2D(0, 0),
            maxHealth: 100f,
            attackDamage: 15f,
            attackRange: 7.0f,
            attackType: "ranged",
            archetype: UnitArchetype.Archer);

        var redArcher = new UnitEntity(
            engine.State.GenerateEntityId(),
            redFaction,
            "archer_low",
            new Vector2D(8.0f, 0),
            maxHealth: 100f,
            attackDamage: 15f,
            attackRange: 7.0f,
            attackType: "ranged",
            archetype: UnitArchetype.Archer);

        engine.State.AddUnit(blueArcher);
        engine.State.AddUnit(redArcher);
        engine.SpatialGrid.Insert(blueArcher.Id, blueArcher.Position);
        engine.SpatialGrid.Insert(redArcher.Id, redArcher.Position);

        blueArcher.Attack(redArcher.Id);
        redArcher.Attack(blueArcher.Id);

        // Blue archer on Hill has +2.0 range (total 9.0) and reaches red archer at distance 8.0 immediately!
        // Red archer has 7.0 range and cannot reach blue archer without moving uphill in marsh!
        engine.SimulateTicks(60);

        Assert.True(blueArcher.IsAlive);
        Assert.True(redArcher.CurrentHealth < blueArcher.CurrentHealth);
    }

    [Fact]
    public void TC_S06_16_FlankingAndMoraleCollapse_FlankingSquadCausesRapidMoraleDepletion()
    {
        var engine = new SimulationEngine();
        var blueFaction = new FactionId(1);
        var redFaction = new FactionId(2);

        // Engaged frontline unit facing right (heading +X)
        var targetUnit = new UnitEntity(
            engine.State.GenerateEntityId(),
            redFaction,
            "swordsman",
            new Vector2D(0, 0),
            maxHealth: 200f,
            attackDamage: 10f,
            formation: FormationType.Line);
        targetUnit.HeadingDirection = new Vector2D(1f, 0f);

        // Flanking attacker attacking from behind (position -1.5, 0)
        var flankerUnit = new UnitEntity(
            engine.State.GenerateEntityId(),
            blueFaction,
            "swordsman",
            new Vector2D(-1.5f, 0f),
            maxHealth: 100f,
            attackDamage: 15f);

        engine.State.AddUnit(targetUnit);
        engine.State.AddUnit(flankerUnit);
        engine.SpatialGrid.Insert(targetUnit.Id, targetUnit.Position);
        engine.SpatialGrid.Insert(flankerUnit.Id, flankerUnit.Position);

        flankerUnit.Attack(targetUnit.Id);

        // Simulate flanking hits
        engine.SimulateTicks(60);

        // Target morale should be heavily depleted due to flanking shock (-15 per hit)
        Assert.True(targetUnit.Morale.CurrentMorale <= 50.0f);
        Assert.True(targetUnit.Morale.Level >= MoraleLevel.Wavering);
    }
}
