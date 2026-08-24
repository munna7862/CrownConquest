using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.AI;

/// <summary>
/// Authoritative AI perception state tracking fog-of-war vision, discovered enemy units/structures,
/// known resource nodes, and threat heatmaps for an AI faction.
/// </summary>
public sealed class AiPerceptionState
{
    private readonly Dictionary<EntityId, PerceivedEntityRecord> _perceivedEnemies = new(128);
    private readonly List<PerceivedEntityRecord> _activePerceivedEnemyList = new(128);
    private readonly List<Vector2D> _knownEnemyBases = new(16);
    private readonly List<ResourceNodeEntity> _knownResourceNodes = new(64);

    public FactionId FactionId { get; }

    public IReadOnlyDictionary<EntityId, PerceivedEntityRecord> PerceivedEnemies => _perceivedEnemies;
    public IReadOnlyList<PerceivedEntityRecord> ActivePerceivedEnemies => _activePerceivedEnemyList;
    public IReadOnlyList<Vector2D> KnownEnemyBases => _knownEnemyBases;
    public IReadOnlyList<ResourceNodeEntity> KnownResourceNodes => _knownResourceNodes;

    public AiPerceptionState(FactionId factionId)
    {
        FactionId = factionId;
    }

    /// <summary>
    /// Updates fog-of-war perception by scanning friendly unit/building line-of-sight against all simulation entities.
    /// </summary>
    public void UpdatePerception(SimulationState state, ulong currentTick)
    {
        ArgumentNullException.ThrowIfNull(state);

        _activePerceivedEnemyList.Clear();

        // 1. Scan enemy units in sight of friendly units or buildings
        var units = state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var targetUnit = units[i];
            if (!targetUnit.IsAlive || targetUnit.FactionId == FactionId)
            {
                continue;
            }

            if (IsPositionVisible(state, targetUnit.Position))
            {
                var record = PerceivedEntityRecord.FromUnit(targetUnit, currentTick);
                _perceivedEnemies[targetUnit.Id] = record;
                _activePerceivedEnemyList.Add(record);
            }
            else if (_perceivedEnemies.TryGetValue(targetUnit.Id, out var existing))
            {
                _activePerceivedEnemyList.Add(existing);
            }
        }

        // 2. Scan enemy buildings in sight
        var buildings = state.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            var targetBuilding = buildings[i];
            if (!targetBuilding.IsAlive || targetBuilding.FactionId == FactionId)
            {
                continue;
            }

            if (IsPositionVisible(state, targetBuilding.Position))
            {
                var record = PerceivedEntityRecord.FromBuilding(targetBuilding, currentTick);
                _perceivedEnemies[targetBuilding.Id] = record;
                _activePerceivedEnemyList.Add(record);

                // Check if this is an enemy Town Center or fortress base
                if (targetBuilding.BuildingType.Equals("town_center", StringComparison.OrdinalIgnoreCase) ||
                    targetBuilding.BuildingType.Equals("fortress", StringComparison.OrdinalIgnoreCase))
                {
                    bool baseKnown = false;
                    for (int b = 0; b < _knownEnemyBases.Count; b++)
                    {
                        if (_knownEnemyBases[b].DistanceTo(targetBuilding.Position) < 5.0f)
                        {
                            baseKnown = true;
                            break;
                        }
                    }
                    if (!baseKnown)
                    {
                        _knownEnemyBases.Add(targetBuilding.Position);
                    }
                }
            }
            else if (_perceivedEnemies.TryGetValue(targetBuilding.Id, out var existing))
            {
                _activePerceivedEnemyList.Add(existing);
            }
        }

        // 3. Scan resource nodes in sight
        var nodes = state.ActiveResourceNodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.IsDepleted)
            {
                continue;
            }

            if (IsPositionVisible(state, node.Position))
            {
                if (!_knownResourceNodes.Contains(node))
                {
                    _knownResourceNodes.Add(node);
                }
            }
        }

        // Remove depleted nodes
        for (int i = _knownResourceNodes.Count - 1; i >= 0; i--)
        {
            if (_knownResourceNodes[i].IsDepleted)
            {
                _knownResourceNodes.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Checks whether a given spatial position is within sight of any friendly living unit or building.
    /// </summary>
    public bool IsPositionVisible(SimulationState state, Vector2D position)
    {
        ArgumentNullException.ThrowIfNull(state);

        var units = state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit.IsAlive && unit.FactionId == FactionId)
            {
                float sightRadius = GetUnitSightRadius(unit.Archetype);
                if (unit.Position.DistanceTo(position) <= sightRadius)
                {
                    return true;
                }
            }
        }

        var buildings = state.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            var building = buildings[i];
            if (building.IsAlive && building.FactionId == FactionId)
            {
                float sightRadius = GetBuildingSightRadius(building.BuildingType);
                if (building.Position.DistanceTo(position) <= sightRadius)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Calculates total perceived enemy threat level around a specific coordinate within radius.
    /// </summary>
    public float GetThreatLevelNear(Vector2D position, float radius)
    {
        float totalThreat = 0f;
        for (int i = 0; i < _activePerceivedEnemyList.Count; i++)
        {
            var enemy = _activePerceivedEnemyList[i];
            if (enemy.IsAlive && enemy.Position.DistanceTo(position) <= radius)
            {
                totalThreat += enemy.IsBuilding ? 10f : (enemy.Level * 5f + enemy.CurrentHealth * 0.1f);
            }
        }
        return totalThreat;
    }

    public static float GetUnitSightRadius(UnitArchetype archetype)
    {
        return archetype switch
        {
            UnitArchetype.Cavalry => 14.0f,
            UnitArchetype.Archer => 12.0f,
            UnitArchetype.Hero => 14.0f,
            UnitArchetype.Siege => 12.0f,
            _ => 10.0f
        };
    }

    public static float GetBuildingSightRadius(string buildingType)
    {
        return buildingType.ToLowerInvariant() switch
        {
            "town_center" => 16.0f,
            "watchtower" => 16.0f,
            "guard_tower" => 18.0f,
            "ballista_tower" => 18.0f,
            "cannon_tower" => 18.0f,
            "fortress" => 20.0f,
            _ => 8.0f
        };
    }
}
