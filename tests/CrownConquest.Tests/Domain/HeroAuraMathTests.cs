using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class HeroAuraMathTests
{
    [Fact]
    public void HeroAura_CombatDamage_CalculatesWithAuraModifiers()
    {
        // TC-S05-003 & TC-S05-005: Hero aura bonuses (+15% dmg, +2 armor)
        var tech = TechModifiers.Zero;

        // Base damage: 20 vs 3 armor = max(1, 20 - 3) = 17
        float baseDmg = CombatFormulas.CalculateCombatDamageWithAura(
            attackerArchetype: UnitArchetype.Infantry,
            attackerRawAttack: 20f,
            attackerTech: tech,
            attackerAuraDamageBonus: 0f,
            targetArchetype: UnitArchetype.Infantry,
            targetRawArmor: 3f,
            targetTech: tech,
            targetAuraArmorBonus: 0f);

        Assert.Equal(17f, baseDmg);

        // Attacker has +15% aura damage bonus: Attack = 20 * 1.15 = 23, Armor = 3 => 20
        float auraAttackDmg = CombatFormulas.CalculateCombatDamageWithAura(
            attackerArchetype: UnitArchetype.Infantry,
            attackerRawAttack: 20f,
            attackerTech: tech,
            attackerAuraDamageBonus: 0.15f,
            targetArchetype: UnitArchetype.Infantry,
            targetRawArmor: 3f,
            targetTech: tech,
            targetAuraArmorBonus: 0f);

        Assert.Equal(20f, auraAttackDmg, precision: 1);

        // Target has +2 aura armor bonus: Attack = 20, Armor = 3 + 2 = 5 => 15
        float auraDefendedDmg = CombatFormulas.CalculateCombatDamageWithAura(
            attackerArchetype: UnitArchetype.Infantry,
            attackerRawAttack: 20f,
            attackerTech: tech,
            attackerAuraDamageBonus: 0f,
            targetArchetype: UnitArchetype.Infantry,
            targetRawArmor: 3f,
            targetTech: tech,
            targetAuraArmorBonus: 2f);

        Assert.Equal(15f, auraDefendedDmg, precision: 1);
    }
}
