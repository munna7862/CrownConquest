using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class HeroAttributesMathTests
{
    [Fact]
    public void HeroAttributes_DerivedStats_CalculateCorrectly()
    {
        // TC-S05-001: Strength, Agility, Willpower scaling formulas
        var attrs = new HeroAttributes(Strength: 20, Agility: 15, Willpower: 10);

        // Strength: +20 HP, +1.5 Dmg per point
        Assert.Equal(400f, attrs.BonusHealth);
        Assert.Equal(30f, attrs.BonusAttackDamage);

        // Agility: +0.2 Armor, +0.05 Speed, 0.5 Cooldown reduction
        Assert.Equal(3.0f, attrs.BonusArmor, precision: 2);
        Assert.Equal(0.75f, attrs.BonusMovementSpeed, precision: 2);
        Assert.Equal(7, attrs.CooldownReductionTicks);

        // Willpower: MaxMana = 50 + (WIL * 15), ManaRegen = 0.10 + (WIL * 0.05), Potency = 1.0 + (WIL * 0.03)
        Assert.Equal(200f, attrs.MaxMana);
        Assert.Equal(0.60f, attrs.ManaRegenPerTick, precision: 2);
        Assert.Equal(1.30f, attrs.AbilityPotencyMultiplier, precision: 2);
    }

    [Fact]
    public void HeroState_LevelUp_ScalesAttributesAndDerivedStats()
    {
        // TC-S05-004: Hero level-up attribute progression
        var state = new HeroState(
            heroClass: HeroClass.Warlord,
            heroName: "Brennus",
            baseAttributes: new HeroAttributes(18, 12, 10),
            baseLeadershipCapacity: 15,
            strengthPerLevel: 3,
            agilityPerLevel: 1,
            willpowerPerLevel: 1);

        Assert.Equal(1, state.CurrentLevel);
        Assert.Equal(18, state.TotalAttributes.Strength);
        Assert.Equal(0, state.AvailableAttributePoints);

        // Level up to 3
        state.OnLevelUp(3);

        Assert.Equal(3, state.CurrentLevel);
        // Strength should be 18 + (3 * 2) = 24
        Assert.Equal(24, state.TotalAttributes.Strength);
        // Agility should be 12 + (1 * 2) = 14
        Assert.Equal(14, state.TotalAttributes.Agility);
        // Willpower should be 10 + (1 * 2) = 12
        Assert.Equal(12, state.TotalAttributes.Willpower);
        // Available points should be 2
        Assert.Equal(2, state.AvailableAttributePoints);

        // Allocate points
        Assert.True(state.AllocateAttribute("strength"));
        Assert.Equal(1, state.AvailableAttributePoints);
        Assert.Equal(25, state.TotalAttributes.Strength);

        Assert.True(state.AllocateAttribute("agility"));
        Assert.Equal(0, state.AvailableAttributePoints);
        Assert.Equal(15, state.TotalAttributes.Agility);

        // Attempting to allocate with 0 points fails
        Assert.False(state.AllocateAttribute("willpower"));
    }

    [Fact]
    public void HeroLeadership_Capacity_ScalesWithLevelAndStrength()
    {
        // TC-S05-006: Capacity = Base + (Level - 1) * 2 + (STR / 4)
        var state = new HeroState(
            heroClass: HeroClass.Centurion,
            heroName: "Marcus",
            baseAttributes: new HeroAttributes(16, 14, 12),
            baseLeadershipCapacity: 18);

        // Level 1, STR 16: 18 + 0 + 4 = 22
        Assert.Equal(22, state.LeadershipCapacity);

        // Level Up to 4 (LevelDelta = 3): STR becomes 16 + (2 * 3) = 22
        state.OnLevelUp(4);
        // Capacity: 18 + (3 * 2) + (22 / 4) = 18 + 6 + 5 = 29
        Assert.Equal(29, state.LeadershipCapacity);
    }
}
