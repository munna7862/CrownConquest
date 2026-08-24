using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class TacticalTerrainMathTests
{
    [Fact]
    public void TC_S06_01_TerrainModifiers_MatchSpecification()
    {
        var plains = TerrainModifiers.GetDefault(TerrainType.Plains);
        Assert.Equal(1.0f, plains.MovementSpeedMultiplier);
        Assert.Equal(0, plains.ElevationLevel);
        Assert.Equal(0.0f, plains.RangedCoverMitigation);
        Assert.Equal(1.0f, plains.ChargeSpeedMultiplier);

        var forest = TerrainModifiers.GetDefault(TerrainType.Forest);
        Assert.Equal(0.8f, forest.MovementSpeedMultiplier);
        Assert.Equal(0, forest.ElevationLevel);
        Assert.Equal(0.35f, forest.RangedCoverMitigation);
        Assert.Equal(0.6f, forest.ChargeSpeedMultiplier);

        var hills = TerrainModifiers.GetDefault(TerrainType.Hills);
        Assert.Equal(0.85f, hills.MovementSpeedMultiplier);
        Assert.Equal(1, hills.ElevationLevel);
        Assert.Equal(0.15f, hills.RangedCoverMitigation);
        Assert.Equal(0.8f, hills.ChargeSpeedMultiplier);

        var marsh = TerrainModifiers.GetDefault(TerrainType.Marsh);
        Assert.Equal(0.6f, marsh.MovementSpeedMultiplier);
        Assert.Equal(-1, marsh.ElevationLevel);
        Assert.Equal(0.0f, marsh.RangedCoverMitigation);
        Assert.Equal(0.4f, marsh.ChargeSpeedMultiplier);

        var road = TerrainModifiers.GetDefault(TerrainType.Road);
        Assert.Equal(1.25f, road.MovementSpeedMultiplier);
        Assert.Equal(0, road.ElevationLevel);
        Assert.Equal(0.0f, road.RangedCoverMitigation);
        Assert.Equal(1.1f, road.ChargeSpeedMultiplier);

        var water = TerrainModifiers.GetDefault(TerrainType.Water);
        Assert.Equal(0.0f, water.MovementSpeedMultiplier);
    }

    [Fact]
    public void TC_S06_02_ElevationMath_GrantsHighGroundRangeAndDamageBonus()
    {
        // Elevation Range Bonus: +2.0 when attacker elevation > target elevation
        float rangeBonusHigh = CombatFormulas.GetElevationRangeBonus(attackerElevation: 1, targetElevation: 0);
        Assert.Equal(2.0f, rangeBonusHigh);

        float rangeBonusEqual = CombatFormulas.GetElevationRangeBonus(attackerElevation: 0, targetElevation: 0);
        Assert.Equal(0.0f, rangeBonusEqual);

        float rangeBonusLow = CombatFormulas.GetElevationRangeBonus(attackerElevation: -1, targetElevation: 0);
        Assert.Equal(0.0f, rangeBonusLow);

        // Elevation Damage Multiplier: 1.25 downhill (+25%), 0.85 uphill (-15%), 1.00 flat
        float dmgMultDownhill = CombatFormulas.GetElevationDamageMultiplier(attackerElevation: 1, targetElevation: 0);
        Assert.Equal(1.25f, dmgMultDownhill);

        float dmgMultUphill = CombatFormulas.GetElevationDamageMultiplier(attackerElevation: 0, targetElevation: 1);
        Assert.Equal(0.85f, dmgMultUphill);

        float dmgMultFlat = CombatFormulas.GetElevationDamageMultiplier(attackerElevation: 0, targetElevation: 0);
        Assert.Equal(1.00f, dmgMultFlat);
    }

    [Fact]
    public void TC_S06_03_ForestCover_MitigatesRangedDamageWithoutImpactingMelee()
    {
        var plainsTerrain = TerrainModifiers.Plains;
        var forestTerrain = TerrainModifiers.Forest;

        // Ranged attack against plains target vs forest target
        var (rangedDmgPlains, _, _) = CombatFormulas.CalculateTacticalCombatDamage(
            UnitArchetype.Archer, 20f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Steady, plainsTerrain, false, true,
            UnitArchetype.Infantry, 0f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Steady, plainsTerrain);

        var (rangedDmgForest, _, _) = CombatFormulas.CalculateTacticalCombatDamage(
            UnitArchetype.Archer, 20f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Steady, plainsTerrain, false, true,
            UnitArchetype.Infantry, 0f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Steady, forestTerrain);

        Assert.Equal(20f, rangedDmgPlains);
        // Forest mitigates 35% -> 20 * (1 - 0.35) = 13.0
        Assert.Equal(13f, rangedDmgForest, precision: 2);

        // Melee attack is NOT mitigated by forest cover
        var (meleeDmgPlains, _, _) = CombatFormulas.CalculateTacticalCombatDamage(
            UnitArchetype.Infantry, 20f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Steady, plainsTerrain, false, false,
            UnitArchetype.Infantry, 0f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Steady, plainsTerrain);

        var (meleeDmgForest, _, _) = CombatFormulas.CalculateTacticalCombatDamage(
            UnitArchetype.Infantry, 20f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Steady, plainsTerrain, false, false,
            UnitArchetype.Infantry, 0f, TechModifiers.Zero, 0f, FormationModifiers.Line, MoraleLevel.Steady, forestTerrain);

        Assert.Equal(meleeDmgPlains, meleeDmgForest);
    }
}
