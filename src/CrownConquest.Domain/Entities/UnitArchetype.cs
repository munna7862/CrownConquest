using System;

namespace CrownConquest.Domain.Entities;

public enum UnitArchetype
{
    Worker,
    Infantry,
    Spearman,
    Archer,
    Cavalry,
    Siege
}

public static class UnitArchetypeExtensions
{
    public static UnitArchetype FromUnitType(string unitType)
    {
        var lower = unitType.ToLowerInvariant();
        if (lower.Contains("villager") || lower.Contains("worker") || lower.Contains("plebeian"))
        {
            return UnitArchetype.Worker;
        }
        if (lower.Contains("spearman") || lower.Contains("hoplite") || lower.Contains("pikeman"))
        {
            return UnitArchetype.Spearman;
        }
        if (lower.Contains("archer") || lower.Contains("bowman") || lower.Contains("veles") || lower.Contains("skirmisher"))
        {
            return UnitArchetype.Archer;
        }
        if (lower.Contains("cavalry") || lower.Contains("scout") || lower.Contains("equite") || lower.Contains("knight") || lower.Contains("horseman"))
        {
            return UnitArchetype.Cavalry;
        }
        if (lower.Contains("ram") || lower.Contains("catapult") || lower.Contains("ballista") || lower.Contains("trebuchet"))
        {
            return UnitArchetype.Siege;
        }

        return UnitArchetype.Infantry;
    }
}
