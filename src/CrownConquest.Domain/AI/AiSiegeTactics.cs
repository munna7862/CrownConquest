using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.AI;

/// <summary>
/// AI tactics for siege weapon targeting, escort protection rings, and wall breach exploitation.
/// </summary>
public static class AiSiegeTactics
{
    /// <summary>
    /// Selects primary fortification or high-value structural target for siege engines.
    /// </summary>
    public static PerceivedEntityRecord? SelectSiegeTarget(
        UnitEntity siegeUnit,
        IReadOnlyList<PerceivedEntityRecord> perceivedEnemies)
    {
        ArgumentNullException.ThrowIfNull(siegeUnit);
        ArgumentNullException.ThrowIfNull(perceivedEnemies);

        if (perceivedEnemies.Count == 0)
        {
            return null;
        }

        PerceivedEntityRecord? best = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < perceivedEnemies.Count; i++)
        {
            var enemy = perceivedEnemies[i];
            if (!enemy.IsAlive) continue;

            float score;
            if (enemy.IsBuilding)
            {
                string bld = enemy.BuildingType.ToLowerInvariant();
                if (bld.Contains("gate")) score = 100.0f;
                else if (bld.Contains("tower")) score = 90.0f;
                else if (bld.Contains("town_center") || bld.Contains("fortress")) score = 85.0f;
                else if (bld.Contains("wall")) score = 80.0f;
                else score = 60.0f;
            }
            else
            {
                // Secondary target: clustered units
                score = 20.0f + (enemy.UnitArchetype == UnitArchetype.Archer ? 10.0f : 0f);
            }

            float dist = siegeUnit.Position.DistanceTo(enemy.Position);
            score -= dist * 0.5f;

            if (score > bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }

    /// <summary>
    /// Calculates orbital escort slot positions around a siege machine to protect it from melee rushes.
    /// </summary>
    public static Vector2D CalculateEscortPosition(
        Vector2D siegePosition,
        int escortIndex,
        int totalEscorts,
        float escortRadius = 3.0f)
    {
        if (totalEscorts <= 0)
        {
            return siegePosition;
        }

        float angle = (MathF.PI * 2.0f * escortIndex) / totalEscorts;
        return new Vector2D(
            siegePosition.X + MathF.Cos(angle) * escortRadius,
            siegePosition.Y + MathF.Sin(angle) * escortRadius);
    }

    /// <summary>
    /// Checks for a destroyed wall or gate near an enemy perimeter to coordinate infantry breach assaults.
    /// </summary>
    public static Vector2D? FindWallBreach(IReadOnlyList<PerceivedEntityRecord> structures, Vector2D enemyBasePos)
    {
        if (structures == null || structures.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < structures.Count; i++)
        {
            var s = structures[i];
            if (!s.IsAlive && s.IsBuilding)
            {
                string bld = s.BuildingType.ToLowerInvariant();
                if (bld.Contains("wall") || bld.Contains("gate"))
                {
                    return s.Position;
                }
            }
        }

        return null;
    }
}
