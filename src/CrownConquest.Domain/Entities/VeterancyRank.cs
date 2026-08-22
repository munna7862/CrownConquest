namespace CrownConquest.Domain.Entities;

/// <summary>
/// Battlefield veterancy ranks matching Crown & Conquest signature progression:
/// Level 1–2: Recruit
/// Level 3–4: Experienced
/// Level 5–6: Veteran
/// Level 7–8: Elite
/// Level 9+:  Legendary
/// </summary>
public enum VeterancyRank
{
    Recruit = 1,
    Experienced = 2,
    Veteran = 3,
    Elite = 4,
    Legendary = 5
}

public static class VeterancyRankExtensions
{
    public static VeterancyRank GetRankForLevel(int level) => level switch
    {
        <= 2 => VeterancyRank.Recruit,
        <= 4 => VeterancyRank.Experienced,
        <= 6 => VeterancyRank.Veteran,
        <= 8 => VeterancyRank.Elite,
        _ => VeterancyRank.Legendary
    };

    public static string GetDisplayName(this VeterancyRank rank) => rank switch
    {
        VeterancyRank.Recruit => "Recruit",
        VeterancyRank.Experienced => "Experienced",
        VeterancyRank.Veteran => "Veteran",
        VeterancyRank.Elite => "Elite",
        VeterancyRank.Legendary => "Legendary",
        _ => "Unknown"
    };
}
