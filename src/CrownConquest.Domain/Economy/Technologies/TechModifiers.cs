using System;

namespace CrownConquest.Domain.Economy;

/// <summary>
/// Immutable modifier values granted to a faction's units, workers, and structures upon researching technology.
/// </summary>
public readonly record struct TechModifiers(
    int MeleeAttackBonus = 0,
    int MeleeArmorBonus = 0,
    int RangedAttackBonus = 0,
    int RangedArmorBonus = 0,
    float RangedRangeBonus = 0f,
    int CavalryAttackBonus = 0,
    int CavalryArmorBonus = 0,
    float CavalrySpeedBonus = 0f,
    float GatherRateBonus = 0f,
    int FarmFoodBonus = 0,
    int BuildingHealthBonus = 0,
    int BuildingArmorBonus = 0)
{
    public static readonly TechModifiers Zero = new();

    public TechModifiers Combine(in TechModifiers other)
    {
        return new TechModifiers(
            MeleeAttackBonus: MeleeAttackBonus + other.MeleeAttackBonus,
            MeleeArmorBonus: MeleeArmorBonus + other.MeleeArmorBonus,
            RangedAttackBonus: RangedAttackBonus + other.RangedAttackBonus,
            RangedArmorBonus: RangedArmorBonus + other.RangedArmorBonus,
            RangedRangeBonus: RangedRangeBonus + other.RangedRangeBonus,
            CavalryAttackBonus: CavalryAttackBonus + other.CavalryAttackBonus,
            CavalryArmorBonus: CavalryArmorBonus + other.CavalryArmorBonus,
            CavalrySpeedBonus: CavalrySpeedBonus + other.CavalrySpeedBonus,
            GatherRateBonus: GatherRateBonus + other.GatherRateBonus,
            FarmFoodBonus: FarmFoodBonus + other.FarmFoodBonus,
            BuildingHealthBonus: BuildingHealthBonus + other.BuildingHealthBonus,
            BuildingArmorBonus: BuildingArmorBonus + other.BuildingArmorBonus);
    }
}
