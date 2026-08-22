using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class CombatTriangleMathTests
{
    [Fact]
    public void CombatFormulas_SpearmanVsCavalry_BonusDamage()
    {
        // TC-S04-005: Spearman gets 2.5x damage multiplier against Cavalry
        float rawAttack = 12f;
        float targetArmor = 2f;
        var tech = TechModifiers.Zero;

        // Spearman attacking Cavalry
        float dmgVsCavalry = CombatFormulas.CalculateCombatDamage(
            UnitArchetype.Spearman,
            rawAttack,
            tech,
            UnitArchetype.Cavalry,
            targetArmor,
            tech);

        // Raw damage modified = 12 * 2.5 = 30.0 -> Mitigated by 2 armor = 28.0
        Assert.Equal(28.0f, dmgVsCavalry, 0.01f);

        // Spearman attacking Infantry (standard 1.0x)
        float dmgVsInfantry = CombatFormulas.CalculateCombatDamage(
            UnitArchetype.Spearman,
            rawAttack,
            tech,
            UnitArchetype.Infantry,
            targetArmor,
            tech);

        // 12 - 2 = 10.0
        Assert.Equal(10.0f, dmgVsInfantry, 0.01f);
    }

    [Fact]
    public void CombatFormulas_ArcherRanged_TechModifierScaling()
    {
        // TC-S04-006: Archer damage and range scale with tech upgrades
        float baseArcherAttack = 14f;
        float targetArmor = 3f;

        var baseTech = TechModifiers.Zero;
        var fletchingTech = new TechModifiers(RangedAttackBonus: 1, RangedRangeBonus: 1.0f);
        var bodkinTech = new TechModifiers(RangedAttackBonus: 3, RangedRangeBonus: 2.5f);

        float baseDmg = CombatFormulas.CalculateCombatDamage(
            UnitArchetype.Archer,
            baseArcherAttack,
            baseTech,
            UnitArchetype.Infantry,
            targetArmor,
            baseTech);
        Assert.Equal(11.0f, baseDmg, 0.01f); // 14 - 3 = 11

        float fletchingDmg = CombatFormulas.CalculateCombatDamage(
            UnitArchetype.Archer,
            baseArcherAttack,
            fletchingTech,
            UnitArchetype.Infantry,
            targetArmor,
            baseTech);
        Assert.Equal(12.0f, fletchingDmg, 0.01f); // 15 - 3 = 12

        float bodkinDmg = CombatFormulas.CalculateCombatDamage(
            UnitArchetype.Archer,
            baseArcherAttack,
            bodkinTech,
            UnitArchetype.Infantry,
            targetArmor,
            baseTech);
        Assert.Equal(14.0f, bodkinDmg, 0.01f); // 17 - 3 = 14

        // Range checks with bonus
        var archerPos = new Vector2D(10f, 10f);
        var targetPos = new Vector2D(19f, 10f); // Distance = 9.0

        // Base range 8.0 -> Out of range
        Assert.False(CombatFormulas.IsInRange(archerPos, targetPos, 8.0f, baseTech.RangedRangeBonus));

        // Range 8.0 + 1.0 (Fletching) = 9.0 -> In range
        Assert.True(CombatFormulas.IsInRange(archerPos, targetPos, 8.0f, fletchingTech.RangedRangeBonus));
    }

    [Fact]
    public void CombatFormulas_CavalryVsArcher_FlankingMultiplier()
    {
        float rawAttack = 20f;
        float archerArmor = 1f;
        var tech = TechModifiers.Zero;

        // Cavalry vs Archer gets 1.5x multiplier
        float dmg = CombatFormulas.CalculateCombatDamage(
            UnitArchetype.Cavalry,
            rawAttack,
            tech,
            UnitArchetype.Archer,
            archerArmor,
            tech);

        // 20 * 1.5 = 30 -> 30 - 1 = 29.0
        Assert.Equal(29.0f, dmg, 0.01f);
    }
}
