using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// AI evaluation hooks for siege warfare, fortification targeting, threat detection, and breach pathfinding.
/// Deterministic and allocation-free in high-frequency queries.
/// </summary>
public static class SiegeAiHooks
{
    /// <summary>
    /// Evaluates and selects the highest-priority target building or unit for a siege engine (Ram, Catapult, Ballista).
    /// Priority: Gates -> Defensive Towers -> Walls -> Town Centers / Military Buildings -> Units.
    /// </summary>
    public static EntityId SelectOptimalSiegeTarget(UnitEntity siegeUnit, SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(siegeUnit);
        ArgumentNullException.ThrowIfNull(state);

        if (!siegeUnit.IsAlive) return EntityId.None;

        float searchRadius = MathF.Max(siegeUnit.AggroRange, siegeUnit.AttackRange + 4.0f);
        float searchRadiusSq = searchRadius * searchRadius;

        BuildingEntity? bestGate = null;
        float bestGateDistSq = float.MaxValue;

        BuildingEntity? bestTower = null;
        float bestTowerDistSq = float.MaxValue;

        BuildingEntity? bestWall = null;
        float bestWallDistSq = float.MaxValue;

        BuildingEntity? bestOtherBuilding = null;
        float bestOtherDistSq = float.MaxValue;

        var buildings = state.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (b.FactionId == siegeUnit.FactionId || !b.IsAlive) continue;

            float distSq = siegeUnit.Position.DistanceSquaredTo(b.Position);
            if (distSq > searchRadiusSq) continue;

            if (b.IsGate)
            {
                if (distSq < bestGateDistSq)
                {
                    bestGateDistSq = distSq;
                    bestGate = b;
                }
            }
            else if (b.IsTower)
            {
                if (distSq < bestTowerDistSq)
                {
                    bestTowerDistSq = distSq;
                    bestTower = b;
                }
            }
            else if (b.IsWall)
            {
                if (distSq < bestWallDistSq)
                {
                    bestWallDistSq = distSq;
                    bestWall = b;
                }
            }
            else
            {
                if (distSq < bestOtherDistSq)
                {
                    bestOtherDistSq = distSq;
                    bestOtherBuilding = b;
                }
            }
        }

        if (bestGate != null) return bestGate.Id;
        if (bestTower != null) return bestTower.Id;
        if (bestWall != null) return bestWall.Id;
        if (bestOtherBuilding != null) return bestOtherBuilding.Id;

        // Fallback: enemy units in range
        UnitEntity? bestEnemyUnit = null;
        float bestUnitDistSq = float.MaxValue;
        var units = state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var u = units[i];
            if (u.FactionId == siegeUnit.FactionId || !u.IsAlive) continue;

            float distSq = siegeUnit.Position.DistanceSquaredTo(u.Position);
            if (distSq <= searchRadiusSq && distSq < bestUnitDistSq)
            {
                bestUnitDistSq = distSq;
                bestEnemyUnit = u;
            }
        }

        return bestEnemyUnit != null ? bestEnemyUnit.Id : EntityId.None;
    }

    /// <summary>
    /// Finds the nearest active breach in fortifications relative to a position.
    /// </summary>
    public static BreachEntity? FindNearestBreach(Vector2D searchPos, SimulationState state, FactionId? defendingFaction = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        BreachEntity? nearest = null;
        float nearestDistSq = float.MaxValue;

        var breaches = state.Breaches;
        for (int i = 0; i < breaches.Count; i++)
        {
            var breach = breaches[i];
            if (defendingFaction.HasValue && breach.DefendingFactionId != defendingFaction.Value) continue;

            float distSq = searchPos.DistanceSquaredTo(breach.Position);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = breach;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Evaluates if an attacking unit or siege engine is threatening a perimeter fortification.
    /// </summary>
    public static bool IsFortificationUnderThreat(BuildingEntity building, SimulationState state, float threatRange = 12.0f)
    {
        ArgumentNullException.ThrowIfNull(building);
        ArgumentNullException.ThrowIfNull(state);

        if (!building.IsAlive) return false;

        float threatRangeSq = threatRange * threatRange;
        var units = state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var u = units[i];
            if (u.FactionId != building.FactionId && u.IsAlive)
            {
                if (u.Position.DistanceSquaredTo(building.Position) <= threatRangeSq)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
