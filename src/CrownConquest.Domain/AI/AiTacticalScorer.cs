using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.AI;

/// <summary>
/// Tactical scoring algorithms evaluating focus fire priority, flanking maneuver routes,
/// and terrain elevation advantages.
/// </summary>
public static class AiTacticalScorer
{
    /// <summary>
    /// Computes tactical focus fire target score for a specific attacker against a perceived enemy.
    /// Higher values indicate higher priority focus fire target.
    /// </summary>
    public static float CalculateFocusFireScore(
        UnitArchetype attackerArchetype,
        Vector2D attackerPos,
        int attackerElevation,
        PerceivedEntityRecord target,
        int targetElevation = 0,
        float elevationBias = 1.0f)
    {
        if (!target.IsAlive)
        {
            return float.MinValue;
        }

        // 1. Archetype Rock-Paper-Scissors / Structural Target Priority
        float priorityBase = target.IsBuilding
            ? AiTargetingMatrix.GetBuildingTargetPriority(attackerArchetype, target.BuildingType)
            : AiTargetingMatrix.GetTargetPriority(attackerArchetype, target.UnitArchetype);

        float score = priorityBase * 10.0f;

        // 2. Low-Health Vulnerability Focus (Kill off wounded enemies rapidly)
        float hpRatio = (float)target.CurrentHealth / Math.Max(1, target.MaxHealth);
        float lowHealthBonus = (1.0f - Math.Clamp(hpRatio, 0f, 1.0f)) * 15.0f;
        score += lowHealthBonus;

        // 3. Threat Level Bonus (High level units or heroes are high priority)
        if (!target.IsBuilding)
        {
            if (target.UnitArchetype == UnitArchetype.Hero)
            {
                score += 12.0f;
            }
            else
            {
                score += target.Level * 2.5f;
            }
        }

        // 4. Elevation Advantage / Penalty
        if (attackerElevation > targetElevation)
        {
            score += 8.0f * Math.Max(0.5f, elevationBias);
        }
        else if (attackerElevation < targetElevation)
        {
            score -= 4.0f;
        }

        // 5. Distance Penalty (Closer targets are preferred)
        float dist = attackerPos.DistanceTo(target.Position);
        score -= dist * 0.4f;

        return score;
    }

    /// <summary>
    /// Calculates a tactical flanking position around an engaged enemy target.
    /// </summary>
    public static Vector2D CalculateFlankPoint(
        Vector2D attackerPos,
        Vector2D targetPos,
        Vector2D? targetHeading,
        float lateralOffset = 4.0f,
        float rearOffset = 3.0f)
    {
        if (!targetHeading.HasValue || targetHeading.Value.LengthSquared < 0.01f)
        {
            // Default: angle perpendicular to attacker vector
            Vector2D dirToAttacker = (attackerPos - targetPos).Normalized();
            Vector2D perp = new(-dirToAttacker.Y, dirToAttacker.X);
            return targetPos + perp * lateralOffset;
        }

        Vector2D forward = targetHeading.Value.Normalized();
        Vector2D side = new(-forward.Y, forward.X);

        // Determine if attacker is on left or right of target's heading
        Vector2D toAttacker = attackerPos - targetPos;
        float dotSide = (toAttacker.X * side.X) + (toAttacker.Y * side.Y);
        float sideSign = dotSide >= 0 ? 1.0f : -1.0f;

        // Flank point is behind and to the side of the target's heading
        return targetPos - (forward * rearOffset) + (side * (lateralOffset * sideSign));
    }

    /// <summary>
    /// Evaluates if an archetype benefits significantly from flanking maneuvers.
    /// </summary>
    public static bool IsFlankingAdvantageous(UnitArchetype archetype)
    {
        return archetype is UnitArchetype.Cavalry or UnitArchetype.Hero;
    }

    /// <summary>
    /// Selects the best tactical target from active perceived enemies for a friendly unit/squad.
    /// </summary>
    public static PerceivedEntityRecord? SelectBestTacticalTarget(
        UnitEntity leadUnit,
        IReadOnlyList<PerceivedEntityRecord> perceivedEnemies,
        int leadUnitElevation = 0,
        float elevationBias = 1.0f)
    {
        ArgumentNullException.ThrowIfNull(leadUnit);
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

            float score = CalculateFocusFireScore(
                leadUnit.Archetype,
                leadUnit.Position,
                leadUnitElevation,
                enemy,
                targetElevation: 0,
                elevationBias);

            if (score > bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }
}
