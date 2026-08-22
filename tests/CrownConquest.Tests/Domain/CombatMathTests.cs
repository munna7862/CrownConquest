using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class CombatMathTests
{
    [Fact]
    public void CombatMath_ArmorMitigation_Standard_ShouldSubtractArmorFromDamage()
    {
        // 20 raw damage - 5 armor = 15 effective damage
        float effective = CombatFormulas.CalculateEffectiveDamage(rawDamage: 20f, targetArmor: 5f);
        Assert.Equal(15f, effective);
    }

    [Fact]
    public void CombatMath_ArmorMitigation_MinimumFloor_ShouldFloorAtOne()
    {
        // 10 raw damage - 15 armor -> should floor at 1.0f
        float effective = CombatFormulas.CalculateEffectiveDamage(rawDamage: 10f, targetArmor: 15f);
        Assert.Equal(1.0f, effective);
    }

    [Fact]
    public void Rect2D_ContainsAndIntersects_ShouldAccuratelyEvaluateBounds()
    {
        var box = new Rect2D(10f, 10f, 30f, 40f);

        Assert.True(box.Contains(new Vector2D(15f, 25f)));
        Assert.True(box.Contains(new Vector2D(10f, 10f)));
        Assert.True(box.Contains(new Vector2D(30f, 40f)));

        Assert.False(box.Contains(new Vector2D(5f, 25f)));
        Assert.False(box.Contains(new Vector2D(35f, 25f)));
        Assert.False(box.Contains(new Vector2D(15f, 45f)));

        var intersectingBox = new Rect2D(25f, 35f, 50f, 50f);
        var disjointBox = new Rect2D(35f, 45f, 60f, 60f);

        Assert.True(box.Intersects(intersectingBox));
        Assert.False(box.Intersects(disjointBox));
    }

    [Fact]
    public void BattlefieldBounds_ClampPosition_ShouldKeepUnitsInsidePerimeter()
    {
        var bounds = new BattlefieldBounds(0f, 0f, 100f, 100f);

        var clamped1 = bounds.Clamp(new Vector2D(-10f, 50f), margin: 1.0f);
        Assert.Equal(1.0f, clamped1.X);
        Assert.Equal(50.0f, clamped1.Y);

        var clamped2 = bounds.Clamp(new Vector2D(150f, 200f), margin: 1.0f);
        Assert.Equal(99.0f, clamped2.X);
        Assert.Equal(99.0f, clamped2.Y);
    }

    [Fact]
    public void RtsCamera_ScreenToWorldProjection_ShouldTransformCoordinatesCorrectly()
    {
        var bounds = new BattlefieldBounds(0f, 0f, 100f, 100f);
        var camera = new RtsCameraController(new Vector2D(50f, 50f), initialZoom: 2.0f, bounds);
        var viewport = new Vector2D(800f, 600f);

        // Center screen pixel (400, 300) should map to camera world pos (50, 50)
        var worldCenter = camera.ScreenToWorld(new Vector2D(400f, 300f), viewport);
        Assert.Equal(50.0f, worldCenter.X, precision: 4);
        Assert.Equal(50.0f, worldCenter.Y, precision: 4);

        // Screen (600, 300) -> offset (+200, 0) / zoom 2 = (+100, 0) world offset -> world (150, 50)
        var worldOffset = camera.ScreenToWorld(new Vector2D(600f, 300f), viewport);
        Assert.Equal(150.0f, worldOffset.X, precision: 4);
        Assert.Equal(50.0f, worldOffset.Y, precision: 4);

        // Roundtrip projection
        var screenProjected = camera.WorldToScreen(worldOffset, viewport);
        Assert.Equal(600.0f, screenProjected.X, precision: 4);
        Assert.Equal(300.0f, screenProjected.Y, precision: 4);
    }

    [Fact]
    public void Veterancy_MultiLevelRollover_ShouldAdvanceThroughMultipleLevelsSequentially()
    {
        var bus = new DomainEventBus();
        var state = new VeterancyState(new EntityId(1), initialLevel: 1);

        // Award 1000 XP in one go (Level 1 -> 6 requires 1000 XP in default curve)
        state.AwardXp(1000, 10, bus, out bool leveledUp, out bool rankChanged);

        Assert.True(leveledUp);
        Assert.True(rankChanged);
        Assert.Equal(6, state.Level);
        Assert.Equal(VeterancyRank.Veteran, state.Rank);
    }
}
