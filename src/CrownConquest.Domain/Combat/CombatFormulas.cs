using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Pure combat formulas for damage calculation, armor mitigation, rock-paper-scissors archetype multipliers,
/// and range checks. Deterministic and allocation-free.
/// </summary>
public static class CombatFormulas
{
    public const float MinimumDamageFloor = 1.0f;
    public const float SpearmanVsCavalryMultiplier = 2.5f;
    public const float CavalryVsArcherMultiplier = 1.5f;
    public const float ArcherVsSpearmanMultiplier = 1.25f;

    /// <summary>
    /// Calculates archetype interaction damage multiplier based on RTS combat triangle.
    /// </summary>
    public static float GetArchetypeMultiplier(UnitArchetype attacker, UnitArchetype target)
    {
        if (attacker == UnitArchetype.Spearman && target == UnitArchetype.Cavalry)
        {
            return SpearmanVsCavalryMultiplier;
        }

        if (attacker == UnitArchetype.Cavalry && target == UnitArchetype.Archer)
        {
            return CavalryVsArcherMultiplier;
        }

        if (attacker == UnitArchetype.Archer && target == UnitArchetype.Spearman)
        {
            return ArcherVsSpearmanMultiplier;
        }

        return 1.0f;
    }

    /// <summary>
    /// Calculates effective damage dealt after armor mitigation and damage multipliers.
    /// Effective Damage = max(1.0, (RawDamage * Modifier) - Armor)
    /// </summary>
    public static float CalculateEffectiveDamage(float rawDamage, float targetArmor, float modifier = 1.0f)
    {
        float modifiedDamage = rawDamage * modifier;
        float mitigated = modifiedDamage - MathF.Max(0f, targetArmor);
        return MathF.Max(MinimumDamageFloor, mitigated);
    }

    /// <summary>
    /// Calculates full combat damage between two units factoring in veterancy, technology upgrades, and unit archetypes.
    /// </summary>
    public static float CalculateCombatDamage(
        UnitArchetype attackerArchetype,
        float attackerRawAttack,
        TechModifiers attackerTech,
        UnitArchetype targetArchetype,
        float targetRawArmor,
        TechModifiers targetTech,
        float customModifier = 1.0f)
    {
        float techAttackBonus = attackerArchetype switch
        {
            UnitArchetype.Infantry or UnitArchetype.Spearman => attackerTech.MeleeAttackBonus,
            UnitArchetype.Archer => attackerTech.RangedAttackBonus,
            UnitArchetype.Cavalry => attackerTech.CavalryAttackBonus,
            _ => 0f
        };

        float techArmorBonus = targetArchetype switch
        {
            UnitArchetype.Infantry or UnitArchetype.Spearman => targetTech.MeleeArmorBonus,
            UnitArchetype.Archer => targetTech.RangedArmorBonus,
            UnitArchetype.Cavalry => targetTech.CavalryArmorBonus,
            _ => 0f
        };

        float totalAttack = attackerRawAttack + techAttackBonus;
        float totalArmor = targetRawArmor + techArmorBonus;
        float archetypeMultiplier = GetArchetypeMultiplier(attackerArchetype, targetArchetype);
        float combinedModifier = customModifier * archetypeMultiplier;

        return CalculateEffectiveDamage(totalAttack, totalArmor, combinedModifier);
    }

    /// <summary>
    /// Evaluates if target position is within attack range of attacker factoring in range tech bonuses.
    /// </summary>
    public static bool IsInRange(Vector2D attackerPos, Vector2D targetPos, float attackRange, float rangeBonus = 0f)
    {
        float totalRange = MathF.Max(0.5f, attackRange + rangeBonus);
        float rangeWithTolerance = totalRange + 0.1f;
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
