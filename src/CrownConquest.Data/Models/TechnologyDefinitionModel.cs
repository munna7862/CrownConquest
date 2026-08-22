using System.Collections.Generic;

namespace CrownConquest.Data.Models;

/// <summary>
/// Data-driven definition for technology tree research.
/// </summary>
public sealed record TechnologyDefinitionModel
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = "Military";
    public int RequiredEra { get; init; } = 1;
    public List<string> RequiredTechIds { get; init; } = new();
    public List<string> RequiredBuildingTypes { get; init; } = new();
    public int FoodCost { get; init; } = 0;
    public int GoldCost { get; init; } = 0;
    public int WoodCost { get; init; } = 0;
    public int StoneCost { get; init; } = 0;
    public int IronCost { get; init; } = 0;
    public int ResearchDurationTicks { get; init; } = 40;
    public int MeleeAttackBonus { get; init; } = 0;
    public int MeleeArmorBonus { get; init; } = 0;
    public int RangedAttackBonus { get; init; } = 0;
    public int RangedArmorBonus { get; init; } = 0;
    public float RangedRangeBonus { get; init; } = 0f;
    public int CavalryAttackBonus { get; init; } = 0;
    public int CavalryArmorBonus { get; init; } = 0;
    public float CavalrySpeedBonus { get; init; } = 0f;
    public float GatherRateBonus { get; init; } = 0f;
    public int FarmFoodBonus { get; init; } = 0;
    public int BuildingHealthBonus { get; init; } = 0;
    public int BuildingArmorBonus { get; init; } = 0;
}
