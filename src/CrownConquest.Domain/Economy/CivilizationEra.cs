using System;

namespace CrownConquest.Domain.Economy;

/// <summary>
/// The four authoritative civilization eras in Crown & Conquest.
/// </summary>
public enum CivilizationEra
{
    Archaic = 0,   // Tribal / Archaic: Basic wooden structures, gatherers, basic infantry
    Classical = 1, // Bronze / Classical: Barracks expansion, archers, watchtowers, blacksmith
    Imperial = 2,  // Iron / Imperial: Heavy infantry, cavalry stables, advanced metallurgy
    Feudal = 3     // Feudal / Sovereign: Master technologies, elite units, grand fortifications
}

public static class CivilizationEraExtensions
{
    public static string GetDisplayName(this CivilizationEra era) => era switch
    {
        CivilizationEra.Archaic => "Tribal / Archaic Era",
        CivilizationEra.Classical => "Bronze / Classical Era",
        CivilizationEra.Imperial => "Iron / Imperial Era",
        CivilizationEra.Feudal => "Feudal / Sovereign Era",
        _ => "Unknown Era"
    };

    public static CivilizationEra? GetNextEra(this CivilizationEra era) => era switch
    {
        CivilizationEra.Archaic => CivilizationEra.Classical,
        CivilizationEra.Classical => CivilizationEra.Imperial,
        CivilizationEra.Imperial => CivilizationEra.Feudal,
        CivilizationEra.Feudal => null,
        _ => null
    };
}
