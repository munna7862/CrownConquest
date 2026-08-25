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

public readonly record struct RadarBlip(
    Vector2D NormalizedPos,
    RenderColor Color,
    float Size,
    bool IsHero,
    bool IsBuilding);

public readonly record struct MinimapRadarViewModel(
    float Width,
    float Height,
    Vector2D CameraCenterNormalized,
    Vector2D CameraExtentsNormalized,
    IReadOnlyList<RadarBlip> Blips);

public readonly record struct AbilityButtonViewModel(
    string AbilityId,
    string DisplayName,
    string Hotkey,
    float CooldownRatio,
    bool CanCast,
    string Description);

public readonly record struct InteractiveRtsHudViewModel(
    ResourceBarViewModel ResourceBar,
    MinimapRadarViewModel Minimap,
    UnitStatusPanelViewModel? SingleSelection,
    UnitGroupSummaryViewModel? GroupSelection,
    IReadOnlyList<AbilityButtonViewModel> HeroAbilities,
    bool HasActiveSelection);

/// <summary>
/// Interactive RTS HUD Presenter maintaining real-time view models for the
/// Top Resource Bar, Minimap Radar, Unit Selection Card, and Command/Ability Buttons.
/// </summary>
public sealed class InteractiveRtsHud
{
    private readonly GameCoordinator _coordinator;
    private readonly SelectionManager _selection;
    private readonly RtsCameraController _camera;
    private readonly FactionId _playerFaction;
    private readonly ResourceBarHudPresenter _resBarPresenter;
    private readonly List<RadarBlip> _blipsCache = new(256);
    private readonly List<AbilityButtonViewModel> _abilitiesCache = new(8);

    public GameCoordinator Coordinator => _coordinator;
    public SelectionManager Selection => _selection;
    public RtsCameraController Camera => _camera;

    public InteractiveRtsHud(
        GameCoordinator coordinator,
        SelectionManager selection,
        RtsCameraController camera,
        FactionId? playerFaction = null)
    {
        _coordinator = coordinator;
        _selection = selection;
        _camera = camera;
        _playerFaction = playerFaction ?? FactionId.Player1;
        _resBarPresenter = new ResourceBarHudPresenter(_coordinator, _playerFaction);
    }

    public InteractiveRtsHudViewModel GenerateHudSnapshot(Vector2D viewportSize)
    {
        var state = _coordinator.Simulation.State;

        // 1. Top Resource Bar
        var resBar = _resBarPresenter.GetViewModel();

        // 2. Minimap Radar
        _blipsCache.Clear();
        var mapBounds = _camera.Bounds;
        float invWidth = 1.0f / Math.Max(mapBounds.Width, 1f);
        float invHeight = 1.0f / Math.Max(mapBounds.Height, 1f);

        for (int i = 0; i < state.ActiveUnits.Count; i++)
        {
            var u = state.ActiveUnits[i];
            if (!u.IsAlive) continue;

            var normPos = new Vector2D(u.Position.X * invWidth, u.Position.Y * invHeight);
            var color = u.FactionId == _playerFaction ? RenderColor.CelticBlue : RenderColor.RomanRed;
            _blipsCache.Add(new RadarBlip(normPos, color, u.IsHero ? 4.0f : 2.5f, u.IsHero, false));
        }

        for (int i = 0; i < state.ActiveBuildings.Count; i++)
        {
            var b = state.ActiveBuildings[i];
            if (!b.IsAlive) continue;

            var normPos = new Vector2D(b.Position.X * invWidth, b.Position.Y * invHeight);
            var color = b.FactionId == _playerFaction ? RenderColor.CelticBlue : RenderColor.RomanRed;
            _blipsCache.Add(new RadarBlip(normPos, color, 5.0f, false, true));
        }

        var camCenterNorm = new Vector2D(_camera.Position.X * invWidth, _camera.Position.Y * invHeight);
        var camExtentsNorm = new Vector2D(
            (viewportSize.X / _camera.Zoom) * invWidth * 0.5f,
            (viewportSize.Y / _camera.Zoom) * invHeight * 0.5f);

        var minimap = new MinimapRadarViewModel(160f, 160f, camCenterNorm, camExtentsNorm, _blipsCache);

        // 3. Selection Panels
        UnitStatusPanelViewModel? singleSel = null;
        UnitGroupSummaryViewModel? groupSel = null;
        _abilitiesCache.Clear();

        var selectedIds = _selection.SelectedUnitIds;
        bool hasSelection = selectedIds.Count > 0;

        if (selectedIds.Count == 1)
        {
            var unitId = selectedIds[0];
            if (state.TryGetUnit(unitId, out var unit) && unit != null && unit.IsAlive)
            {
                var rank = unit.Veterancy.Rank;
                singleSel = new UnitStatusPanelViewModel(
                    UnitId: unit.Id,
                    UnitType: unit.UnitType,
                    DisplayName: unit.UnitType,
                    CurrentHealth: unit.CurrentHealth,
                    MaxHealth: unit.MaxHealth,
                    HealthPercentage: unit.CurrentHealth / unit.MaxHealth,
                    AttackDamage: unit.AttackDamage,
                    Armor: unit.BaseArmor,
                    AttackRange: unit.AttackRange,
                    AttackType: unit.AttackType,
                    MovementSpeed: unit.MovementSpeed,
                    Level: unit.Veterancy.Level,
                    CurrentXp: unit.Veterancy.CurrentXp,
                    XpToNextLevel: unit.Veterancy.XpToNextLevel,
                    Rank: rank,
                    RankDisplayName: rank.ToString(),
                    KillCount: unit.Veterancy.KillCount,
                    State: unit.State,
                    IsHero: unit.IsHero,
                    IsWorker: unit.WorkerState != null);

                if (unit.IsHero && unit.HeroState != null)
                {
                    var abilities = unit.HeroState.Abilities;
                    for (int a = 0; a < abilities.Count; a++)
                    {
                        var abilityState = abilities[a];
                        var def = abilityState.Definition;
                        bool isReady = abilityState.CooldownRemainingTicks == 0;
                        float cdRatio = (float)abilityState.CooldownRemainingTicks / Math.Max(def.CooldownTicks, 1);

                        _abilitiesCache.Add(new AbilityButtonViewModel(
                            AbilityId: def.Id,
                            DisplayName: def.DisplayName,
                            Hotkey: $"F{a + 1}",
                            CooldownRatio: cdRatio,
                            CanCast: isReady,
                            Description: def.Description));
                    }
                }
            }
        }
        else if (selectedIds.Count > 1)
        {
            int total = 0;
            float totalHpPct = 0;
            int melee = 0, ranged = 0, cav = 0, siege = 0, worker = 0, hero = 0;

            for (int s = 0; s < selectedIds.Count; s++)
            {
                if (state.TryGetUnit(selectedIds[s], out var unit) && unit != null && unit.IsAlive)
                {
                    total++;
                    totalHpPct += (unit.CurrentHealth / unit.MaxHealth);
                    if (unit.IsHero) hero++;
                    else if (unit.WorkerState != null) worker++;
                    else if (unit.Archetype == UnitArchetype.Cavalry) cav++;
                    else if (unit.Archetype == UnitArchetype.Archer) ranged++;
                    else if (unit.Archetype == UnitArchetype.Siege) siege++;
                    else melee++;
                }
            }

            if (total > 0)
            {
                groupSel = new UnitGroupSummaryViewModel(
                    TotalCount: total,
                    AverageHealthPercentage: totalHpPct / total,
                    MeleeCount: melee,
                    RangedCount: ranged,
                    CavalryCount: cav,
                    SiegeCount: siege,
                    WorkerCount: worker,
                    HeroCount: hero,
                    PrimaryUnitType: "Squad");
            }
        }

        return new InteractiveRtsHudViewModel(
            ResourceBar: resBar,
            Minimap: minimap,
            SingleSelection: singleSel,
            GroupSelection: groupSel,
            HeroAbilities: _abilitiesCache,
            HasActiveSelection: hasSelection);
    }
}
