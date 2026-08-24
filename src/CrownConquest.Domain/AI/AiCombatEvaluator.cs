using System;
using System.Collections.Generic;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.AI;

/// <summary>
/// Domain combat strength, combat odds, and retreat evaluation math formulas.
/// </summary>
public static class AiCombatEvaluator
{
    public const float DefaultRetreatOddsThreshold = 0.45f;
    public const float DefaultSquadHealthRetreatThreshold = 0.30f;

    /// <summary>
    /// Computes the individual combat power metric for a unit based on current/max HP, base stats, level, and archetype.
    /// </summary>
    public static float CalculateUnitCombatPower(UnitEntity unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (!unit.IsAlive)
        {
            return 0f;
        }

        float hpRatio = (float)unit.CurrentHealth / Math.Max(1f, unit.MaxHealth);
        float levelMultiplier = 1.0f + 0.15f * (unit.Veterancy.Level - 1);
        float archetypeMultiplier = unit.Archetype switch
        {
            UnitArchetype.Hero => 2.5f,
            UnitArchetype.Cavalry => 1.4f,
            UnitArchetype.Siege => 1.5f,
            UnitArchetype.Spearman => 1.0f,
            UnitArchetype.Archer => 1.1f,
            UnitArchetype.Infantry => 1.0f,
            _ => 0.5f
        };

        return (unit.AttackDamage * 3.0f + unit.MaxHealth * 0.1f + unit.Armor * 2.0f) * hpRatio * levelMultiplier * archetypeMultiplier;
    }

    /// <summary>
    /// Computes total combat power for a squad/collection of friendly living units.
    /// </summary>
    public static float CalculateSquadCombatPower(IReadOnlyList<UnitEntity> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        float totalPower = 0f;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit != null && unit.IsAlive)
            {
                totalPower += CalculateUnitCombatPower(unit);
            }
        }
        return totalPower;
    }

    /// <summary>
    /// Computes total perceived threat combat power from perceived enemy records.
    /// </summary>
    public static float CalculatePerceivedThreat(IReadOnlyList<PerceivedEntityRecord> perceivedEnemies)
    {
        ArgumentNullException.ThrowIfNull(perceivedEnemies);
        float totalPower = 0f;
        for (int i = 0; i < perceivedEnemies.Count; i++)
        {
            var enemy = perceivedEnemies[i];
            if (!enemy.IsAlive)
            {
                continue;
            }

            if (enemy.IsBuilding)
            {
                bool isDefensive = enemy.BuildingType.Contains("tower", StringComparison.OrdinalIgnoreCase) ||
                                  enemy.BuildingType.Contains("fortress", StringComparison.OrdinalIgnoreCase) ||
                                  enemy.BuildingType.Contains("gate", StringComparison.OrdinalIgnoreCase);
                totalPower += isDefensive ? 35f : 10f;
            }
            else
            {
                float levelMult = 1.0f + 0.15f * Math.Max(0, enemy.Level - 1);
                float hpRatio = (float)enemy.CurrentHealth / Math.Max(1f, enemy.MaxHealth);
                float power = (20f + enemy.CurrentHealth * 0.1f) * levelMult * hpRatio;
                totalPower += power;
            }
        }
        return totalPower;
    }

    /// <summary>
    /// Calculates combat odds ratio: Friendly Power / (Friendly Power + Perceived Enemy Power).
    /// Returns value between 0.0 and 1.0. (0.50 means evenly matched).
    /// </summary>
    public static float CalculateCombatOdds(float friendlyPower, float enemyPower)
    {
        if (friendlyPower <= 0f)
        {
            return 0f;
        }
        if (enemyPower <= 0f)
        {
            return 1.0f;
        }

        return friendlyPower / (friendlyPower + enemyPower);
    }

    /// <summary>
    /// Evaluates whether a friendly squad should disengage and retreat.
    /// </summary>
    public static bool ShouldRetreat(
        float friendlyPower,
        float enemyPower,
        float squadHealthPercent,
        float retreatOddsThreshold = DefaultRetreatOddsThreshold,
        float squadHealthRetreatThreshold = DefaultSquadHealthRetreatThreshold)
    {
        if (enemyPower <= 0f)
        {
            return false;
        }

        if (squadHealthPercent < squadHealthRetreatThreshold)
        {
            return true;
        }

        float odds = CalculateCombatOdds(friendlyPower, enemyPower);
        return odds < retreatOddsThreshold;
    }
}
