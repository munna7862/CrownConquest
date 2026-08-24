using System.Collections.Generic;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.World;

/// <summary>
/// Persistent state snapshot of an RPG Hero attached to a strategic army.
/// </summary>
public sealed class StrategicHeroSpec
{
    public string HeroName { get; set; } = string.Empty;
    public HeroClass Class { get; set; } = HeroClass.Warlord;
    public HeroAttributes BaseAttributes { get; set; } = new(10, 10, 10);
    public int Level { get; set; } = 1;
    public int CurrentXp { get; set; } = 0;
    public int TotalKills { get; set; } = 0;
    public List<string> UnlockedAbilities { get; set; } = new();
    public List<string> EquippedItems { get; set; } = new();

    public float CombatPower => (Level * 50f) + (BaseAttributes.Strength * 5f) + (BaseAttributes.Agility * 5f) + (BaseAttributes.Willpower * 5f);

    public StrategicHeroSpec Clone()
    {
        return new StrategicHeroSpec
        {
            HeroName = HeroName,
            Class = Class,
            BaseAttributes = BaseAttributes,
            Level = Level,
            CurrentXp = CurrentXp,
            TotalKills = TotalKills,
            UnlockedAbilities = new List<string>(UnlockedAbilities),
            EquippedItems = new List<string>(EquippedItems)
        };
    }
}
