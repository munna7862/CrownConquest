using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class TacticalFormationMathTests
{
    [Fact]
    public void TC_S06_04_FormationOffsets_CalculatesCorrectSlotCountsAndSpacing()
    {
        var centroid = new Vector2D(10f, 20f);

        // Line formation: 6 units
        var lineSlots = FormationCalculator.CalculateFormationSlots(FormationType.Line, centroid, 6, spacing: 2.0f);
        Assert.Equal(6, lineSlots.Length);
        Assert.True(lineSlots[0].X < lineSlots[^1].X); // Line spreads horizontally

        // Shield Wall: 6 units
        var shieldSlots = FormationCalculator.CalculateFormationSlots(FormationType.ShieldWall, centroid, 6, spacing: 2.0f);
        Assert.Equal(6, shieldSlots.Length);

        // Wedge: 6 units (Rank 0: 1, Rank 1: 2, Rank 2: 3 = 6 total)
        var wedgeSlots = FormationCalculator.CalculateFormationSlots(FormationType.Wedge, centroid, 6, spacing: 2.0f);
        Assert.Equal(6, wedgeSlots.Length);

        // Square: 4 units (2x2)
        var squareSlots = FormationCalculator.CalculateFormationSlots(FormationType.Square, centroid, 4, spacing: 2.0f);
        Assert.Equal(4, squareSlots.Length);

        // Column: 6 units (2x3 file)
        var colSlots = FormationCalculator.CalculateFormationSlots(FormationType.Column, centroid, 6, spacing: 2.0f);
        Assert.Equal(6, colSlots.Length);
    }

    [Fact]
    public void TC_S06_05_FormationModifiers_MatchCombatSpecifications()
    {
        var line = FormationModifiers.GetDefault(FormationType.Line);
        Assert.Equal(1.00f, line.MeleeDamageMultiplier);
        Assert.Equal(0.0f, line.ArmorBonus);
        Assert.Equal(1.0f, line.MovementSpeedMultiplier);
        Assert.False(line.CanBraceCavalry);

        var shieldWall = FormationModifiers.GetDefault(FormationType.ShieldWall);
        Assert.Equal(0.95f, shieldWall.MeleeDamageMultiplier);
        Assert.Equal(4.0f, shieldWall.ArmorBonus);
        Assert.Equal(0.70f, shieldWall.MovementSpeedMultiplier);
        Assert.Equal(0.50f, shieldWall.RangedDamageMitigation);
        Assert.True(shieldWall.CanBraceCavalry);

        var wedge = FormationModifiers.GetDefault(FormationType.Wedge);
        Assert.Equal(1.0f, wedge.MeleeDamageMultiplier);
        Assert.Equal(-2.0f, wedge.ArmorBonus);
        Assert.Equal(1.15f, wedge.MovementSpeedMultiplier);
        Assert.Equal(1.30f, wedge.ChargeDamageMultiplier);
        Assert.False(wedge.CanBraceCavalry);

        var square = FormationModifiers.GetDefault(FormationType.Square);
        Assert.Equal(0.90f, square.MeleeDamageMultiplier);
        Assert.Equal(2.0f, square.ArmorBonus);
        Assert.Equal(0.80f, square.MovementSpeedMultiplier);
        Assert.True(square.CanBraceCavalry);
    }
}
