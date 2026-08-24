using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Entities;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class HeroAbilityMathTests
{
    [Fact]
    public void HeroAbility_CooldownTracking_WorksCorrectly()
    {
        // TC-S05-002: Ability cooldown and ready state
        var def = new HeroAbilityDefinition(
            "heroic_strike",
            "Heroic Strike",
            "High melee burst",
            manaCost: 25f,
            cooldownTicks: 30,
            castRange: 2.5f,
            radius: 0f,
            AbilityTargetType.SingleTargetEnemy,
            AbilityEffectType.Damage,
            basePower: 50f);

        var ability = new HeroAbilityState(def);

        Assert.True(ability.IsReady);
        Assert.Equal(0, ability.CooldownRemainingTicks);
        Assert.Equal(0f, ability.CooldownNormalized);

        ability.TriggerCooldown();
        Assert.False(ability.IsReady);
        Assert.Equal(30, ability.CooldownRemainingTicks);
        Assert.Equal(1.0f, ability.CooldownNormalized);

        for (int i = 0; i < 15; i++)
        {
            ability.DecrementCooldown();
        }

        Assert.Equal(15, ability.CooldownRemainingTicks);
        Assert.Equal(0.5f, ability.CooldownNormalized, precision: 2);
        Assert.False(ability.IsReady);

        for (int i = 0; i < 15; i++)
        {
            ability.DecrementCooldown();
        }

        Assert.Equal(0, ability.CooldownRemainingTicks);
        Assert.True(ability.IsReady);
    }

    [Fact]
    public void CombatFormulas_CalculateHeroSpellDamage_ScalesWithPotencyAndPenetration()
    {
        // Spell damage formula: rawPower * potency - (targetArmor * (1 - armorPenetration))
        // BasePower = 60, Potency = 1.30 (from 10 WIL), TargetArmor = 6, Penetration = 0.5 (effective armor = 3)
        // Expected: 60 * 1.30 - 3 = 78 - 3 = 75
        float dmg = CombatFormulas.CalculateHeroSpellDamage(
            baseAbilityPower: 60f,
            abilityPotencyMultiplier: 1.30f,
            targetArmor: 6f,
            armorPenetration: 0.5f);

        Assert.Equal(75.0f, dmg, precision: 1);
    }
}
