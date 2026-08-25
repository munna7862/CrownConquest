using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Color representation for decoupled 2D rendering.
/// </summary>
public readonly record struct RenderColor(byte R, byte G, byte B, byte A = 255)
{
    public static readonly RenderColor White = new(255, 255, 255);
    public static readonly RenderColor Black = new(0, 0, 0);
    public static readonly RenderColor CelticBlue = new(37, 99, 235);
    public static readonly RenderColor RomanRed = new(220, 38, 38);
    public static readonly RenderColor NeutralGold = new(234, 179, 8);
    public static readonly RenderColor Green = new(34, 197, 94);
    public static readonly RenderColor DarkGreen = new(20, 83, 45);
    public static readonly RenderColor Brown = new(120, 53, 15);
    public static readonly RenderColor Gray = new(107, 114, 128);
    public static readonly RenderColor DarkSlate = new(15, 23, 42);
    public static readonly RenderColor HealthGreen = new(74, 222, 128);
    public static readonly RenderColor HealthRed = new(239, 68, 68);
    public static readonly RenderColor SelectionGreen = new(74, 222, 128, 180);
    public static readonly RenderColor DragBoxGreen = new(34, 197, 94, 60);
    public static readonly RenderColor DragBoxBorder = new(74, 222, 128, 220);
    public static readonly RenderColor BronzeRank = new(180, 83, 9);
    public static readonly RenderColor SilverRank = new(203, 213, 225);
    public static readonly RenderColor GoldRank = new(250, 204, 21);
    public static readonly RenderColor CrownLegendary = new(236, 72, 153);
}

/// <summary>
/// Render token describing a 2D unit entity on the battlefield.
/// </summary>
public readonly record struct UnitRenderToken(
    EntityId UnitId,
    FactionId Faction,
    string UnitType,
    Vector2D WorldPosition,
    Vector2D ScreenPosition,
    float Radius,
    Vector2D Heading,
    float HealthPercentage,
    bool IsSelected,
    VeterancyRank Rank,
    RenderColor FactionColor,
    RenderColor RankBadgeColor,
    string DisplayName,
    bool IsHero);

/// <summary>
/// Render token describing a structure or fortification on the battlefield.
/// </summary>
public readonly record struct BuildingRenderToken(
    EntityId BuildingId,
    FactionId Faction,
    string BuildingType,
    Vector2D WorldPosition,
    Vector2D ScreenPosition,
    Vector2D ScreenSize,
    float HealthPercentage,
    float BuildProgress,
    bool IsConstructed,
    bool IsSelected,
    RenderColor FactionColor);

/// <summary>
/// Render token describing a harvestable resource node.
/// </summary>
public readonly record struct ResourceNodeRenderToken(
    EntityId NodeId,
    ResourceType Type,
    Vector2D WorldPosition,
    Vector2D ScreenPosition,
    float Radius,
    int RemainingAmount,
    int MaxAmount,
    RenderColor Color);

/// <summary>
/// Floating text popup for combat damage and veterancy level-up announcements.
/// </summary>
public sealed class FloatingCombatText
{
    public string Text { get; }
    public Vector2D WorldPosition { get; set; }
    public RenderColor Color { get; }
    public int RemainingTicks { get; set; }
    public int TotalDurationTicks { get; }

    public float Alpha => Math.Clamp((float)RemainingTicks / TotalDurationTicks, 0f, 1f);

    public FloatingCombatText(string text, Vector2D startPos, RenderColor color, int durationTicks = 20)
    {
        Text = text;
        WorldPosition = startPos;
        Color = color;
        RemainingTicks = durationTicks;
        TotalDurationTicks = durationTicks;
    }
}

/// <summary>
/// Decoupled 2D Game View Renderer orchestrating visual battlefield snapshots,
/// coordinate conversions, unit tokens, building bounds, health bars, and HUD elements.
/// </summary>
public sealed class GameViewRenderer
{
    private readonly GameCoordinator _coordinator;
    private readonly RtsCameraController _camera;
    private readonly List<FloatingCombatText> _floatingTexts = new(64);
    private readonly List<UnitRenderToken> _cachedUnitTokens = new(256);
    private readonly List<BuildingRenderToken> _cachedBuildingTokens = new(64);
    private readonly List<ResourceNodeRenderToken> _cachedResourceTokens = new(64);

    public GameCoordinator Coordinator => _coordinator;
    public RtsCameraController Camera => _camera;
    public IReadOnlyList<FloatingCombatText> FloatingTexts => _floatingTexts;

    public GameViewRenderer(GameCoordinator coordinator, RtsCameraController? camera = null)
    {
        _coordinator = coordinator;
        _camera = camera ?? new RtsCameraController(new Vector2D(50, 50), initialZoom: 1.0f);

        _coordinator.EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        _coordinator.EventBus.Subscribe<UnitLevelUpEvent>(OnUnitLevelUp);
        _coordinator.EventBus.Subscribe<HeroLevelUpEvent>(OnHeroLevelUp);
    }

    public IReadOnlyList<UnitRenderToken> GenerateUnitTokens(Vector2D viewportSize, IReadOnlyList<EntityId>? selectedIds = null)
    {
        _cachedUnitTokens.Clear();
        var units = _coordinator.Simulation.State.ActiveUnits;

        for (int i = 0; i < units.Count; i++)
        {
            var u = units[i];
            if (!u.IsAlive) continue;

            var screenPos = _camera.WorldToScreen(u.Position, viewportSize);
            float screenRadius = 1.0f * _camera.Zoom * 16.0f;

            var factionColor = u.FactionId == FactionId.Player1 ? RenderColor.CelticBlue : RenderColor.RomanRed;
            var rank = u.Veterancy.Rank;
            var badgeColor = rank switch
            {
                VeterancyRank.Experienced => RenderColor.BronzeRank,
                VeterancyRank.Veteran => RenderColor.SilverRank,
                VeterancyRank.Elite => RenderColor.GoldRank,
                VeterancyRank.Legendary => RenderColor.CrownLegendary,
                _ => RenderColor.White
            };

            bool isSelected = false;
            if (selectedIds != null)
            {
                for (int s = 0; s < selectedIds.Count; s++)
                {
                    if (selectedIds[s] == u.Id) { isSelected = true; break; }
                }
            }

            _cachedUnitTokens.Add(new UnitRenderToken(
                UnitId: u.Id,
                Faction: u.FactionId,
                UnitType: u.UnitType,
                WorldPosition: u.Position,
                ScreenPosition: screenPos,
                Radius: Math.Max(screenRadius, 8.0f),
                Heading: u.HeadingDirection,
                HealthPercentage: u.CurrentHealth / u.MaxHealth,
                IsSelected: isSelected,
                Rank: rank,
                FactionColor: factionColor,
                RankBadgeColor: badgeColor,
                DisplayName: u.UnitType,
                IsHero: u.IsHero));
        }

        return _cachedUnitTokens;
    }

    public IReadOnlyList<BuildingRenderToken> GenerateBuildingTokens(Vector2D viewportSize, IReadOnlyList<EntityId>? selectedIds = null)
    {
        _cachedBuildingTokens.Clear();
        var buildings = _coordinator.Simulation.State.ActiveBuildings;

        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (!b.IsAlive) continue;

            var screenPos = _camera.WorldToScreen(b.Position, viewportSize);
            var screenSize = b.GridSize * _camera.Zoom * 16.0f;
            var factionColor = b.FactionId == FactionId.Player1 ? RenderColor.CelticBlue : RenderColor.RomanRed;

            bool isSelected = false;
            if (selectedIds != null)
            {
                for (int s = 0; s < selectedIds.Count; s++)
                {
                    if (selectedIds[s] == b.Id) { isSelected = true; break; }
                }
            }

            _cachedBuildingTokens.Add(new BuildingRenderToken(
                BuildingId: b.Id,
                Faction: b.FactionId,
                BuildingType: b.BuildingType,
                WorldPosition: b.Position,
                ScreenPosition: screenPos,
                ScreenSize: screenSize,
                HealthPercentage: b.CurrentHealth / b.MaxHealth,
                BuildProgress: b.BuildProgressNormalized,
                IsConstructed: b.IsConstructed,
                IsSelected: isSelected,
                FactionColor: factionColor));
        }

        return _cachedBuildingTokens;
    }

    public IReadOnlyList<ResourceNodeRenderToken> GenerateResourceTokens(Vector2D viewportSize)
    {
        _cachedResourceTokens.Clear();
        var nodes = _coordinator.Simulation.State.ActiveResourceNodes;

        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n.IsDepleted) continue;

            var screenPos = _camera.WorldToScreen(n.Position, viewportSize);
            var color = n.ResourceType switch
            {
                ResourceType.Food => RenderColor.Green,
                ResourceType.Wood => RenderColor.Brown,
                ResourceType.Gold => RenderColor.NeutralGold,
                ResourceType.Stone => RenderColor.Gray,
                ResourceType.Iron => RenderColor.DarkSlate,
                _ => RenderColor.White
            };

            _cachedResourceTokens.Add(new ResourceNodeRenderToken(
                NodeId: n.Id,
                Type: n.ResourceType,
                WorldPosition: n.Position,
                ScreenPosition: screenPos,
                Radius: n.HarvestRadius * _camera.Zoom * 10f,
                RemainingAmount: n.RemainingAmount,
                MaxAmount: n.MaxAmount,
                Color: color));
        }

        return _cachedResourceTokens;
    }

    public List<DirectionalUnitVisualState> GenerateDirectionalUnitStates(ulong currentTick)
    {
        var units = _coordinator.Simulation.State.ActiveUnits;
        var list = new List<DirectionalUnitVisualState>(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].IsAlive)
            {
                list.Add(DirectionalSpriteController.GetVisualState(units[i], currentTick));
            }
        }
        return list;
    }

    public List<BuildingSpriteDescriptor> GenerateBuildingSpriteDescriptors(ulong currentTick)
    {
        var buildings = _coordinator.Simulation.State.ActiveBuildings;
        var list = new List<BuildingSpriteDescriptor>(buildings.Count);
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].IsAlive)
            {
                list.Add(BuildingSpriteVisualMapper.GetDescriptor(buildings[i], currentTick));
            }
        }
        return list;
    }

    public List<FoliageVisualState> GenerateFoliageStates(ulong currentTick)
    {
        var nodes = _coordinator.Simulation.State.ActiveResourceNodes;
        var list = new List<FoliageVisualState>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            list.Add(FoliageResourcePresenter.GetState(nodes[i], currentTick));
        }
        return list;
    }

    public void UpdateVfxTicks()
    {
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            var ft = _floatingTexts[i];
            ft.RemainingTicks--;
            ft.WorldPosition = new Vector2D(ft.WorldPosition.X, ft.WorldPosition.Y - 0.05f);

            if (ft.RemainingTicks <= 0)
            {
                _floatingTexts.RemoveAt(i);
            }
        }
    }

    private void OnDamageDealt(in DamageDealtEvent evt)
    {
        if (_coordinator.Simulation.State.TryGetUnit(evt.TargetId, out var target) && target != null)
        {
            _floatingTexts.Add(new FloatingCombatText(
                $"-{evt.DamageAmount:F0}",
                target.Position,
                evt.IsCritical ? RenderColor.GoldRank : RenderColor.HealthRed,
                durationTicks: 25));
        }
    }

    private void OnUnitLevelUp(in UnitLevelUpEvent evt)
    {
        if (_coordinator.Simulation.State.TryGetUnit(evt.UnitId, out var unit) && unit != null)
        {
            _floatingTexts.Add(new FloatingCombatText(
                $"LEVEL UP! (Level: {evt.NewLevel})",
                unit.Position,
                RenderColor.GoldRank,
                durationTicks: 45));
        }
    }

    private void OnHeroLevelUp(in HeroLevelUpEvent evt)
    {
        if (_coordinator.Simulation.State.TryGetUnit(evt.HeroId, out var hero) && hero != null)
        {
            _floatingTexts.Add(new FloatingCombatText(
                $"HERO LEVEL {evt.NewLevel}!",
                hero.Position,
                RenderColor.CrownLegendary,
                durationTicks: 60));
        }
    }
}
