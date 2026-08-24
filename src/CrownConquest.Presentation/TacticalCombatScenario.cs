using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Headless match scenario orchestrating tactical combat mechanics: terrain modifiers, formations,
/// high ground elevation, forest cover, cavalry charges, spear bracing, and morale routing/rallying.
/// </summary>
public sealed class TacticalCombatScenario
{
    private readonly GameCoordinator _coordinator;
    private readonly FactionId _blueFaction = new(1);
    private readonly FactionId _redFaction = new(2);

    public GameCoordinator Coordinator => _coordinator;
    public SimulationEngine Engine => _coordinator.Simulation;
    public FactionId BlueFaction => _blueFaction;
    public FactionId RedFaction => _redFaction;

    public TacticalCombatScenario(SimulationConfig? config = null)
    {
        _coordinator = new GameCoordinator(config);
    }

    public void SetupTacticalBattlefield()
    {
        var terrainGrid = _coordinator.Simulation.State.TerrainGrid;

        // Configure terrain features:
        // Center-North: Hills (Elevation +1)
        terrainGrid.SetTerrainRect(28, 40, 16, 12, TerrainType.Hills);

        // Center-South: Marsh (Elevation -1, heavy slow)
        terrainGrid.SetTerrainRect(28, 12, 16, 12, TerrainType.Marsh);

        // East: Dense Forest (35% ranged cover)
        terrainGrid.SetTerrainRect(48, 20, 12, 24, TerrainType.Forest);

        // West: Military Road (1.25x speed)
        terrainGrid.SetTerrainRect(10, 0, 4, 64, TerrainType.Road);
    }

    public (List<EntityId> BlueSpearmen, List<EntityId> RedCavalry) SpawnChargeTestEncounter()
    {
        var blueSpearmen = new List<EntityId>();
        var redCavalry = new List<EntityId>();

        // Blue spearmen in Shield Wall facing right at (0, 0)
        for (int i = 0; i < 4; i++)
        {
            var id = _coordinator.Simulation.State.GenerateEntityId();
            var pos = new Vector2D(-2f, (i * 1.5f) - 2.25f);
            var spearman = new UnitEntity(
                id,
                _blueFaction,
                "triarius",
                pos,
                maxHealth: 120f,
                attackDamage: 14f,
                attackRange: 1.8f,
                movementSpeed: 3.5f,
                baseArmor: 2.0f,
                archetype: UnitArchetype.Spearman,
                formation: FormationType.ShieldWall);

            spearman.HeadingDirection = new Vector2D(1f, 0f);
            _coordinator.Simulation.State.AddUnit(spearman);
            _coordinator.Simulation.SpatialGrid.Insert(spearman.Id, spearman.Position);
            blueSpearmen.Add(id);
        }

        // Red cavalry charging from (20, 0) towards (-2, 0)
        for (int i = 0; i < 4; i++)
        {
            var id = _coordinator.Simulation.State.GenerateEntityId();
            var pos = new Vector2D(20f, (i * 1.5f) - 2.25f);
            var cavalry = new UnitEntity(
                id,
                _redFaction,
                "equite",
                pos,
                maxHealth: 140f,
                attackDamage: 20f,
                attackRange: 1.5f,
                movementSpeed: 5.0f,
                baseArmor: 3.0f,
                archetype: UnitArchetype.Cavalry,
                formation: FormationType.Wedge);

            cavalry.HeadingDirection = new Vector2D(-1f, 0f);
            _coordinator.Simulation.State.AddUnit(cavalry);
            _coordinator.Simulation.SpatialGrid.Insert(cavalry.Id, cavalry.Position);
            redCavalry.Add(id);
        }

        return (blueSpearmen, redCavalry);
    }

    public (EntityId HighGroundArcher, EntityId LowGroundArcher) SpawnElevationDuel()
    {
        // Set Hill at (0, 0)
        var (gx, gy) = _coordinator.Simulation.State.TerrainGrid.WorldToGrid(new Vector2D(0, 0));
        _coordinator.Simulation.State.TerrainGrid.SetTerrainRect(gx - 2, gy - 2, 5, 5, TerrainType.Hills);

        var highId = _coordinator.Simulation.State.GenerateEntityId();
        var highArcher = new UnitEntity(
            highId,
            _blueFaction,
            "roman_archer",
            new Vector2D(0, 0),
            maxHealth: 80f,
            attackDamage: 15f,
            attackRange: 7.0f,
            movementSpeed: 3.5f,
            attackType: "ranged",
            archetype: UnitArchetype.Archer);
        highArcher.CurrentTerrain = TerrainType.Hills;

        var lowId = _coordinator.Simulation.State.GenerateEntityId();
        var lowArcher = new UnitEntity(
            lowId,
            _redFaction,
            "celtic_archer",
            new Vector2D(8.0f, 0), // 8.0 units away: High ground with +2 range (total 9.0) can reach, low ground (7.0 range) cannot reach!
            maxHealth: 80f,
            attackDamage: 15f,
            attackRange: 7.0f,
            movementSpeed: 3.5f,
            attackType: "ranged",
            archetype: UnitArchetype.Archer);
        lowArcher.CurrentTerrain = TerrainType.Plains;

        _coordinator.Simulation.State.AddUnit(highArcher);
        _coordinator.Simulation.State.AddUnit(lowArcher);
        _coordinator.Simulation.SpatialGrid.Insert(highArcher.Id, highArcher.Position);
        _coordinator.Simulation.SpatialGrid.Insert(lowArcher.Id, lowArcher.Position);

        return (highId, lowId);
    }
}
