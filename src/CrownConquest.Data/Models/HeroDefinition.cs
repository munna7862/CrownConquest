using System;
using System.Collections.Generic;

namespace CrownConquest.Data.Models;

public sealed class HeroAuraDefinitionModel
{
    public string AuraName { get; set; } = string.Empty;
    public float Radius { get; set; } = 12.0f;
    public float DamageMultiplierBonus { get; set; } = 0.15f;
    public float ArmorBonus { get; set; } = 2.0f;
    public float MovementSpeedMultiplierBonus { get; set; } = 0.10f;
}

public sealed class HeroDefinition
{
    public string Id { get; set; } = string.Empty;
    public string HeroName { get; set; } = string.Empty;
    public string HeroClass { get; set; } = "Warlord";
    public string Faction { get; set; } = "celtic";
    public int BaseStrength { get; set; } = 15;
    public int BaseAgility { get; set; } = 10;
    public int BaseWillpower { get; set; } = 10;
    public int StrengthPerLevel { get; set; } = 2;
    public int AgilityPerLevel { get; set; } = 1;
    public int WillpowerPerLevel { get; set; } = 1;
    public int BaseLeadershipCapacity { get; set; } = 15;
    public List<string> AbilityIds { get; set; } = new();
    public HeroAuraDefinitionModel? Aura { get; set; }
}
