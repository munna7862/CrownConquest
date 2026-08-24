using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class TacticalChargeMathTests
{
    [Fact]
    public void TC_S06_08_ChargeImpactFormula_DealsBonusDamageAndMoraleShockWhenUnbraced()
    {
        var charge = new ChargeState();
        Assert.False(charge.IsCharging);

        // Build momentum for 20 ticks
        for (int i = 0; i < 20; i++)
        {
            charge.IncrementMomentum();
        }

        Assert.True(charge.IsCharging);
        Assert.Equal(1.4f, charge.CurrentSpeedMultiplier, precision: 2);

        // Calculate charge damage against unbraced Swordsmen (Infantry) in Line formation
        var (damage, chargeBlocked, recoilDamage) = CombatFormulas.CalculateTacticalCombatDamage(
            UnitArchetype.Cavalry, 20f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Confident, TerrainModifiers.Plains,
            isAttackerCharging: true, isRangedAttack: false,
            UnitArchetype.Infantry, 0f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Steady, TerrainModifiers.Plains);

        Assert.False(chargeBlocked);
        Assert.Equal(0f, recoilDamage);
        // Base 20 damage * 2.0x charge damage multiplier = 40 damage
        Assert.Equal(40f, damage, precision: 1);
    }

    [Fact]
    public void TC_S06_09_SpearBracingCounter_NegatesChargeAndReflectsRecoilDamage()
    {
        // 1. Cavalry charging into Spearman (archetype bracing)
        var (spearDamage, spearChargeBlocked, spearRecoil) = CombatFormulas.CalculateTacticalCombatDamage(
            UnitArchetype.Cavalry, 20f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Confident, TerrainModifiers.Plains,
            isAttackerCharging: true, isRangedAttack: false,
            UnitArchetype.Spearman, 0f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Steady, TerrainModifiers.Plains);

        Assert.True(spearChargeBlocked);
        Assert.Equal(10f, spearRecoil, precision: 1); // 50% of 20 = 10 recoil damage
        // Charge damage multiplier is negated to 1.0x
        Assert.Equal(20f, spearDamage, precision: 1);

        // 2. Cavalry charging into Infantry in Shield Wall (formation bracing)
        var (shieldDamage, shieldChargeBlocked, shieldRecoil) = CombatFormulas.CalculateTacticalCombatDamage(
            UnitArchetype.Cavalry, 20f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Confident, TerrainModifiers.Plains,
            isAttackerCharging: true, isRangedAttack: false,
            UnitArchetype.Infantry, 0f, TechModifiers.Zero, 0f, FormationModifiers.ShieldWall, MoraleLevel.Steady, TerrainModifiers.Plains);

        Assert.True(shieldChargeBlocked);
        Assert.Equal(10f, shieldRecoil, precision: 1);
        // Target in Shield Wall has +4 Armor: (20 * 1.0) - 4 = 16
        Assert.Equal(16f, shieldDamage, precision: 1);
    }
}
