using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Presentation;

public sealed class GraphicalPresentationTests
{
    [Fact]
    public void TC_S16_001_Camera_ScreenToWorld_WorldToScreen_InversionParity()
    {
        var bounds = new BattlefieldBounds(0, 0, 200, 200);
        var camera = new RtsCameraController(new Vector2D(50, 50), initialZoom: 1.5f, bounds);
        var vpSize = new Vector2D(1920, 1080);

        var originalWorldPos = new Vector2D(75.5f, 62.3f);
        var screenPos = camera.WorldToScreen(originalWorldPos, vpSize);
        var invertedWorldPos = camera.ScreenToWorld(screenPos, vpSize);

        Assert.Equal(originalWorldPos.X, invertedWorldPos.X, precision: 3);
        Assert.Equal(originalWorldPos.Y, invertedWorldPos.Y, precision: 3);
    }

    [Fact]
    public void TC_S16_002_Camera_BoundsClamping_EnforcesMapLimits()
    {
        var bounds = new BattlefieldBounds(0, 0, 100, 100);
        var camera = new RtsCameraController(new Vector2D(50, 50), initialZoom: 1.0f, bounds);

        camera.SetPosition(new Vector2D(150, -50));
        Assert.Equal(99.5f, camera.Position.X);
        Assert.Equal(0.5f, camera.Position.Y);
    }

    [Fact]
    public void TC_S16_003_Camera_ZoomClamping_StaysWithinDefinedLimits()
    {
        var camera = new RtsCameraController(new Vector2D(50, 50), initialZoom: 1.0f);

        for (int i = 0; i < 20; i++) camera.ZoomIn();
        Assert.Equal(camera.MaxZoom, camera.Zoom);

        for (int i = 0; i < 40; i++) camera.ZoomOut();
        Assert.Equal(camera.MinZoom, camera.Zoom);
    }

    [Fact]
    public void TC_S16_004_GameViewRenderer_GeneratesUnitTokens_WithFactionColorsAndBadges()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var tokens = scenario.Renderer.GenerateUnitTokens(new Vector2D(1920, 1080));

        Assert.NotEmpty(tokens);
        var heroToken = Assert.Single(tokens, t => t.IsHero);
        Assert.Equal(RenderColor.CelticBlue, heroToken.FactionColor);
        Assert.True(heroToken.HealthPercentage > 0.99f);
    }

    [Fact]
    public void TC_S16_005_InteractiveRtsHud_GeneratesAccurateResourceSnapshot()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var hudSnapshot = scenario.Hud.GenerateHudSnapshot(new Vector2D(1920, 1080));

        Assert.Equal(500, hudSnapshot.ResourceBar.Food);
        Assert.Equal(500, hudSnapshot.ResourceBar.Wood);
        Assert.Equal(300, hudSnapshot.ResourceBar.Gold);
        Assert.Equal(200, hudSnapshot.ResourceBar.Stone);
        Assert.Equal(150, hudSnapshot.ResourceBar.Iron);
        Assert.NotEmpty(hudSnapshot.Minimap.Blips);
    }

    [Fact]
    public void TC_S16_006_HeroProgression_GeneratesAbilityButtons_WithCooldownTracking()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        scenario.Selection.SelectPoint(scenario.HeroUnit.Position);

        var hudSnapshot = scenario.Hud.GenerateHudSnapshot(new Vector2D(1920, 1080));
        Assert.NotNull(hudSnapshot.SingleSelection);
        Assert.True(hudSnapshot.SingleSelection.Value.IsHero);
        Assert.Equal(2, hudSnapshot.HeroAbilities.Count);
        Assert.Equal("war_cry", hudSnapshot.HeroAbilities[0].AbilityId);
        Assert.Equal("heroic_strike", hudSnapshot.HeroAbilities[1].AbilityId);
    }

    [Fact]
    public void TC_S16_007_FloatingCombatText_FadesAndExpiresCleanly()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        scenario.Coordinator.EventBus.Publish(new DamageDealtEvent(
            SimulationTick: 1,
            AttackerId: scenario.HeroUnit.Id,
            TargetId: scenario.HeroUnit.Id,
            DamageAmount: 45f,
            RemainingHealth: 305f,
            IsCritical: true));

        Assert.NotEmpty(scenario.Renderer.FloatingTexts);
        Assert.Equal("-45", scenario.Renderer.FloatingTexts[0].Text);

        scenario.StepSimulation(30);
        Assert.Empty(scenario.Renderer.FloatingTexts);
    }

    [Fact]
    public void TC_S16_008_GraphicalScenario_FullSimulationStep_AdvancesTicksDeterministically()
    {
        var scenario1 = new GraphicalGameScenario(seed: 999);
        var scenario2 = new GraphicalGameScenario(seed: 999);

        scenario1.StepSimulation(50);
        scenario2.StepSimulation(50);

        Assert.Equal(scenario1.Coordinator.CurrentTick, scenario2.Coordinator.CurrentTick);
        Assert.Equal(scenario1.PlayerArmy.Count, scenario2.PlayerArmy.Count);
    }
}
