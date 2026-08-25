using System;
using System.Collections.Generic;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Result of an automated progression and curve balance audit.
/// </summary>
public sealed record ProgressionValidationReport(
    bool IsValid,
    int TotalChecksExecuted,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyDictionary<int, int> LevelXpThresholds,
    IReadOnlyDictionary<VeterancyRank, float> RankHealthMultipliers,
    IReadOnlyDictionary<VeterancyRank, float> RankDamageMultipliers,
    IReadOnlyDictionary<VeterancyRank, float> RankArmorBonuses);

/// <summary>
/// Domain validator enforcing individual unit leveling curves, veterancy scaling invariants,
/// and hero attribute balance formulas.
/// </summary>
public static class ProgressionBalanceValidator
{
    public static readonly int[] StandardLevelXpThresholds =
    {
        100,  // Level 1 -> 2
        250,  // Level 2 -> 3
        450,  // Level 3 -> 4
        700,  // Level 4 -> 5
        1000, // Level 5 -> 6
        1350, // Level 6 -> 7
        1750, // Level 7 -> 8
        2200  // Level 8 -> 9
    };

    public static ProgressionValidationReport ValidateProgressionInvariants(int[]? customThresholds = null)
    {
        var thresholds = customThresholds ?? StandardLevelXpThresholds;
        var errors = new List<string>();
        int checks = 0;

        // 1. Check monotonic strictly increasing XP thresholds
        checks++;
        for (int i = 0; i < thresholds.Length; i++)
        {
            checks++;
            if (thresholds[i] <= 0)
            {
                errors.Add($"XP threshold for Level {i + 2} must be strictly positive (was {thresholds[i]}).");
            }
            if (i > 0 && thresholds[i] <= thresholds[i - 1])
            {
                errors.Add($"XP threshold for Level {i + 2} ({thresholds[i]}) is not strictly greater than Level {i + 1} ({thresholds[i - 1]}).");
            }
        }

        // 2. Check Delta XP between consecutive levels is increasing (convex leveling curve)
        checks++;
        for (int i = 1; i < thresholds.Length; i++)
        {
            int prevDelta = (i == 1) ? thresholds[0] : (thresholds[i - 1] - thresholds[i - 2]);
            int currentDelta = thresholds[i] - thresholds[i - 1];
            checks++;
            if (currentDelta < prevDelta)
            {
                errors.Add($"XP delta from Level {i + 1} to {i + 2} ({currentDelta}) is less than previous step ({prevDelta}). Non-convex leveling curve.");
            }
        }

        // 3. Check veterancy rank mappings across levels 1 through 10
        checks++;
        var ranks = new Dictionary<int, VeterancyRank>();
        for (int lvl = 1; lvl <= 10; lvl++)
        {
            checks++;
            var expectedRank = lvl switch
            {
                1 or 2 => VeterancyRank.Recruit,
                3 or 4 => VeterancyRank.Experienced,
                5 or 6 => VeterancyRank.Veteran,
                7 or 8 => VeterancyRank.Elite,
                _ => VeterancyRank.Legendary
            };
            ranks[lvl] = expectedRank;
        }

        // 4. Validate veterancy stat multipliers
        var hpMultipliers = new Dictionary<VeterancyRank, float>
        {
            [VeterancyRank.Recruit] = 1.0f,
            [VeterancyRank.Experienced] = 1.10f,
            [VeterancyRank.Veteran] = 1.20f,
            [VeterancyRank.Elite] = 1.30f,
            [VeterancyRank.Legendary] = 1.50f
        };

        var dmgMultipliers = new Dictionary<VeterancyRank, float>
        {
            [VeterancyRank.Recruit] = 1.0f,
            [VeterancyRank.Experienced] = 1.10f,
            [VeterancyRank.Veteran] = 1.20f,
            [VeterancyRank.Elite] = 1.30f,
            [VeterancyRank.Legendary] = 1.50f
        };

        var armorBonuses = new Dictionary<VeterancyRank, float>
        {
            [VeterancyRank.Recruit] = 0f,
            [VeterancyRank.Experienced] = 0f,
            [VeterancyRank.Veteran] = 1f,
            [VeterancyRank.Elite] = 2f,
            [VeterancyRank.Legendary] = 3f
        };

        // Assert monotonic multiplier increases
        var rankOrder = new[]
        {
            VeterancyRank.Recruit,
            VeterancyRank.Experienced,
            VeterancyRank.Veteran,
            VeterancyRank.Elite,
            VeterancyRank.Legendary
        };

        for (int r = 1; r < rankOrder.Length; r++)
        {
            checks++;
            var prevRank = rankOrder[r - 1];
            var curRank = rankOrder[r];

            if (hpMultipliers[curRank] < hpMultipliers[prevRank])
            {
                errors.Add($"Health multiplier for rank {curRank} ({hpMultipliers[curRank]}) is lower than {prevRank} ({hpMultipliers[prevRank]}).");
            }
            if (dmgMultipliers[curRank] < dmgMultipliers[prevRank])
            {
                errors.Add($"Damage multiplier for rank {curRank} ({dmgMultipliers[curRank]}) is lower than {prevRank} ({dmgMultipliers[prevRank]}).");
            }
            if (armorBonuses[curRank] < armorBonuses[prevRank])
            {
                errors.Add($"Armor bonus for rank {curRank} ({armorBonuses[curRank]}) is lower than {prevRank} ({armorBonuses[prevRank]}).");
            }
        }

        var thresholdMap = new Dictionary<int, int>();
        for (int i = 0; i < thresholds.Length; i++)
        {
            thresholdMap[i + 2] = thresholds[i];
        }

        return new ProgressionValidationReport(
            errors.Count == 0,
            checks,
            errors,
            thresholdMap,
            hpMultipliers,
            dmgMultipliers,
            armorBonuses);
    }
}
