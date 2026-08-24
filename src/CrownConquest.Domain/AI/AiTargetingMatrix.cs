using System;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.AI;

/// <summary>
/// Domain priority matrix evaluating tactical target suitability based on unit archetypes and structures.
/// </summary>
public static class AiTargetingMatrix
{
    /// <summary>
    /// Computes numerical preference score for an attacker archetype engaging a target unit archetype.
    /// Higher values indicate higher priority target.
    /// </summary>
    public static float GetTargetPriority(UnitArchetype attacker, UnitArchetype target)
    {
        return attacker switch
        {
            // Siege units prioritize structural and high armor targets
            UnitArchetype.Siege => target switch
            {
                UnitArchetype.Archer => 4.5f,
                UnitArchetype.Infantry => 4.0f,
                UnitArchetype.Spearman => 3.8f,
                _ => 2.0f
            },

            // Cavalry specializes in hunting archers, siege, and unarmored targets
            UnitArchetype.Cavalry => target switch
            {
                UnitArchetype.Siege => 5.0f,
                UnitArchetype.Archer => 4.8f,
                UnitArchetype.Worker => 4.0f,
                UnitArchetype.Infantry => 2.5f,
                UnitArchetype.Spearman => 0.5f, // Avoid spearmen
                _ => 2.0f
            },

            // Spearmen counter cavalry
            UnitArchetype.Spearman => target switch
            {
                UnitArchetype.Cavalry => 5.0f,
                UnitArchetype.Siege => 3.5f,
                UnitArchetype.Hero => 3.0f,
                UnitArchetype.Infantry => 2.5f,
                _ => 2.0f
            },

            // Archers counter unarmored infantry and spearmen
            UnitArchetype.Archer => target switch
            {
                UnitArchetype.Worker => 4.5f,
                UnitArchetype.Spearman => 4.2f,
                UnitArchetype.Infantry => 3.8f,
                UnitArchetype.Hero => 3.0f,
                UnitArchetype.Cavalry => 2.0f,
                _ => 2.0f
            },

            // Melee Infantry (Swordsmen)
            UnitArchetype.Infantry => target switch
            {
                UnitArchetype.Spearman => 4.5f,
                UnitArchetype.Archer => 4.0f,
                UnitArchetype.Worker => 3.5f,
                UnitArchetype.Siege => 3.0f,
                _ => 2.5f
            },

            // Hero
            UnitArchetype.Hero => target switch
            {
                UnitArchetype.Hero => 4.5f,
                UnitArchetype.Cavalry => 4.0f,
                _ => 3.0f
            },

            _ => 1.0f
        };
    }

    /// <summary>
    /// Computes preference score for attacking specific building structures.
    /// </summary>
    public static float GetBuildingTargetPriority(UnitArchetype attacker, string buildingType)
    {
        string type = buildingType.ToLowerInvariant();
        bool isSiege = attacker == UnitArchetype.Siege;

        if (isSiege)
        {
            if (type.Contains("gate")) return 5.5f;
            if (type.Contains("tower")) return 5.0f;
            if (type.Contains("town_center") || type.Contains("fortress")) return 4.5f;
            if (type.Contains("wall")) return 4.0f;
            if (type.Contains("barracks") || type.Contains("range") || type.Contains("stable")) return 3.5f;
            return 2.0f;
        }

        // Standard infantry/ranged target priorities against buildings
        if (type.Contains("tower")) return 1.5f; // Dangerous to melee without siege
        if (type.Contains("town_center") || type.Contains("fortress")) return 3.5f;
        if (type.Contains("barracks") || type.Contains("farm") || type.Contains("house")) return 4.0f;
        if (type.Contains("gate")) return 3.0f;
        if (type.Contains("wall")) return 1.0f;

        return 2.0f;
    }
}
