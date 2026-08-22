using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Pure combat formulas for damage calculation, armor mitigation, and range checks.
/// Deterministic and allocation-free.
/// </summary>
public static class CombatFormulas
{
    public const float MinimumDamageFloor = 1.0f;

    /// <summary>
    /// Calculates effective damage dealt after armor mitigation.
    /// Effective Damage = max(1.0, (RawDamage * Modifier) - Armor)
    /// </summary>
    public static float CalculateEffectiveDamage(float rawDamage, float targetArmor, float modifier = 1.0f)
    {
        float modifiedDamage = rawDamage * modifier;
        float mitigated = modifiedDamage - MathF.Max(0f, targetArmor);
        return MathF.Max(MinimumDamageFloor, mitigated);
    }

    /// <summary>
    /// Evaluates if target position is within attack range of attacker.
    /// </summary>
    public static bool IsInRange(Vector2D attackerPos, Vector2D targetPos, float attackRange)
    {
        float rangeWithTolerance = attackRange + 0.1f;
        return attackerPos.DistanceSquaredTo(targetPos) <= (rangeWithTolerance * rangeWithTolerance);
    }

    /// <summary>
    /// Calculates distance between two points.
    /// </summary>
    public static float GetDistance(Vector2D a, Vector2D b)
    {
        return a.DistanceTo(b);
    }
}
