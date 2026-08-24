using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class SiegeCombatMathTests
{
    [Fact]
    public void TC_S07_01_BatteringRam_CalculatesStructuralDamageMultiplier()
    {
        // Arrange: Ram with 40 base attack vs building with 0 armor
        float rawAttack = 40f;
        var tech = new TechModifiers();

        // Act
        float structuralDamage = CombatFormulas.CalculateStructuralCombatDamage(
            UnitArchetype.Siege,
            "celtic_battering_ram",
            rawAttack,
            tech,
            buildingArmor: 0f);

        // Assert: 40 * 5.0 = 200 damage
        Assert.Equal(200f, structuralDamage);
    }

    [Fact]
    public void TC_S07_02_BatteringRam_AppliesPierceMitigationAgainstRangedAttacks()
    {
        // Arrange: Archer attacking Ram with 80% ranged pierce mitigation
        float archerAttack = 20f;
        float ramArmor = 8f;
        var tech = new TechModifiers();

        // Act
        var (damage, _, _) = CombatFormulas.CalculateTacticalCombatDamage(
            attackerArchetype: UnitArchetype.Archer,
            attackerRawAttack: archerAttack,
            attackerTech: tech,
            attackerAuraDamageBonus: 0f,
            attackerFormation: FormationModifiers.Line,
            attackerMorale: MoraleLevel.Steady,
            attackerTerrain: TerrainModifiers.Plains,
            isAttackerCharging: false,
            isRangedAttack: true,
            targetArchetype: UnitArchetype.Siege,
            targetRawArmor: ramArmor,
            targetTech: tech,
            targetAuraArmorBonus: 0f,
            targetFormation: FormationModifiers.Line,
            targetMorale: MoraleLevel.Steady,
            targetTerrain: TerrainModifiers.Plains);

        // Assert: (20 - 8) * (1 - 0.80) = 12 * 0.20 = 2.4 damage
        Assert.True(damage <= 3.0f, $"Damage {damage} should be heavily mitigated by ram pierce armor.");
        Assert.True(damage >= 1.0f);
    }

    [Fact]
    public void TC_S07_03_Catapult_CalculatesAoESplashDamageFalloff()
    {
        // Arrange: Catapult base splash damage 40, radius 2.5
        float baseDamage = 40f;
        float radius = 2.5f;

        // Act: Center impact (dist 0), half distance (dist 1.25), edge (dist 2.5), beyond edge (dist 3.0)
        float centerDmg = CombatFormulas.CalculateAreaOfEffectDamage(baseDamage, 0f, radius);
        float midDmg = CombatFormulas.CalculateAreaOfEffectDamage(baseDamage, 1.25f, radius);
        float edgeDmg = CombatFormulas.CalculateAreaOfEffectDamage(baseDamage, 2.5f, radius);
        float outsideDmg = CombatFormulas.CalculateAreaOfEffectDamage(baseDamage, 3.0f, radius);

        // Assert: 100% at center, 75% at mid, 50% at edge, 0 beyond
        Assert.Equal(40f, centerDmg);
        Assert.Equal(30f, midDmg);
        Assert.Equal(20f, edgeDmg);
        Assert.Equal(0f, outsideDmg);
    }

    [Fact]
    public void TC_S07_04_Catapult_ValidatesMinAndMaxRangeBoundaries()
    {
        var attackerPos = new Vector2D(0f, 0f);
        var tooCloseTarget = new Vector2D(2f, 0f);     // Dist 2 < MinRange 3
        var inRangeTarget = new Vector2D(6f, 0f);      // Dist 6 (3 <= 6 <= 12)
        var tooFarTarget = new Vector2D(15f, 0f);     // Dist 15 > MaxRange 12

        Assert.False(CombatFormulas.IsSiegeInRange(attackerPos, tooCloseTarget, 3.0f, 12.0f));
        Assert.True(CombatFormulas.IsSiegeInRange(attackerPos, inRangeTarget, 3.0f, 12.0f));
        Assert.False(CombatFormulas.IsSiegeInRange(attackerPos, tooFarTarget, 3.0f, 12.0f));
    }

    [Fact]
    public void TC_S07_05_Ballista_CalculatesArmorPenetration()
    {
        // Arrange: Ballista raw attack 50 vs heavily armored unit (Armor 10)
        float rawAttack = 50f;
        float targetArmor = 10f;

        // Act: 60% armor ignored -> effective armor = 4 -> damage = 50 - 4 = 46
        float damage = CombatFormulas.CalculateArmorPiercingDamage(rawAttack, targetArmor, armorPenetration: 0.60f);

        // Assert
        Assert.Equal(46f, damage);
    }

    [Fact]
    public void TC_S07_06_Tower_DamageScalesWithGarrisonCount()
    {
        // Arrange: Tower base damage 12, garrison damage bonus 0.20 per unit
        var tower = new TowerDefenseState(baseAttackDamage: 12f, maxGarrisonCapacity: 4, garrisonDamageBonusPerUnit: 0.20f);

        Assert.Equal(12f, tower.EffectiveDamage);

        // Act: Garrison 2 units (+40% -> 12 * 1.4 = 16.8)
        tower.TryGarrison(new EntityId(101));
        tower.TryGarrison(new EntityId(102));

        // Assert
        Assert.Equal(16.8f, tower.EffectiveDamage, precision: 1);
        Assert.Equal(2, tower.GarrisonCount);
    }

    [Fact]
    public void TC_S07_07_GateState_TransitionsCorrectly()
    {
        var gate = new GateDefenseState(GateState.Closed);
        Assert.False(gate.IsPassableForEnemies);
        Assert.True(gate.IsPassableForFriendlies);

        gate.Toggle();
        Assert.Equal(GateState.Open, gate.State);
        Assert.True(gate.IsPassableForEnemies);

        gate.TrySetState(GateState.Locked);
        Assert.Equal(GateState.Locked, gate.State);
        Assert.False(gate.IsPassableForEnemies);
    }

    [Fact]
    public void TC_S07_08_RubbleTerrain_ProvidesCorrectModifiers()
    {
        var rubble = TerrainModifiers.GetDefault(TerrainType.Rubble);

        Assert.Equal(0.75f, rubble.MovementSpeedMultiplier);
        Assert.Equal(0.20f, rubble.RangedCoverMitigation);
        Assert.Equal(0.5f, rubble.ChargeSpeedMultiplier);
        Assert.Equal(0, rubble.ElevationLevel);
    }
}
