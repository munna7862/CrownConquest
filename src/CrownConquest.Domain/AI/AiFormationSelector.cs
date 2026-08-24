using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.AI;

/// <summary>
/// AI subsystem for dynamic formation selection based on army composition, enemy threats,
/// and personality preferences.
/// </summary>
public static class AiFormationSelector
{
    /// <summary>
    /// Evaluates friendly squad composition and perceived enemy threats to choose the optimal formation.
    /// </summary>
    public static FormationType SelectOptimalFormation(
        IReadOnlyList<UnitEntity> friendlySquad,
        IReadOnlyList<PerceivedEntityRecord> perceivedEnemies,
        AiPersonalityProfile? personality = null)
    {
        if (friendlySquad == null || friendlySquad.Count == 0)
        {
            return personality?.PreferredFormation ?? FormationType.Line;
        }

        // 1. Analyze Enemy Composition
        int enemyCavalryCount = 0;
        int enemySiegeCount = 0;
        int totalEnemyUnits = 0;

        if (perceivedEnemies != null)
        {
            for (int i = 0; i < perceivedEnemies.Count; i++)
            {
                var enemy = perceivedEnemies[i];
                if (!enemy.IsAlive || enemy.IsBuilding) continue;

                totalEnemyUnits++;
                if (enemy.UnitArchetype == UnitArchetype.Cavalry)
                {
                    enemyCavalryCount++;
                }
                else if (enemy.UnitArchetype == UnitArchetype.Siege)
                {
                    enemySiegeCount++;
                }
            }
        }

        // 2. Counter Heavy Enemy Cavalry -> Square or ShieldWall (Anti-Charge & Armor)
        if (enemyCavalryCount >= 2 || (totalEnemyUnits > 0 && (float)enemyCavalryCount / totalEnemyUnits >= 0.35f))
        {
            return FormationType.Square;
        }

        // 3. Counter Enemy Siege / Catapults -> Loose / Skirmish (Splash Damage Mitigation)
        if (enemySiegeCount >= 1)
        {
            return FormationType.Loose;
        }

        // 4. Analyze Friendly Squad Composition
        int friendlyCavalryCount = 0;
        int friendlySpearCount = 0;
        int friendlyArcherCount = 0;
        int totalFriendly = 0;

        for (int i = 0; i < friendlySquad.Count; i++)
        {
            var unit = friendlySquad[i];
            if (unit == null || !unit.IsAlive) continue;

            totalFriendly++;
            if (unit.Archetype == UnitArchetype.Cavalry) friendlyCavalryCount++;
            else if (unit.Archetype == UnitArchetype.Spearman) friendlySpearCount++;
            else if (unit.Archetype == UnitArchetype.Archer) friendlyArcherCount++;
        }

        // 5. Friendly Cavalry Heavy -> Wedge Formation (Max Charge Damage)
        if (friendlyCavalryCount >= 3 || (totalFriendly > 0 && (float)friendlyCavalryCount / totalFriendly >= 0.50f))
        {
            return FormationType.Wedge;
        }

        // 6. Friendly Spearman Heavy Defensive -> Shield Wall
        if (friendlySpearCount >= 3 && personality?.PersonalityType == AiPersonalityType.Defensive)
        {
            return FormationType.ShieldWall;
        }

        // 7. Personality Preferred Formation Fallback
        if (personality != null)
        {
            return personality.PreferredFormation;
        }

        return FormationType.Line;
    }
}
