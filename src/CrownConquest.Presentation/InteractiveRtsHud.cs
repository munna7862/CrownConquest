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

public readonly record struct BuildingProductionOptionViewModel(
    string ActionId,
    string DisplayName,
    string Hotkey,
    ResourceCost Cost,
    int TrainTimeTicks,
    int PopulationCost,
    bool CanAfford,
    bool CanTrainPop,
    string Description);

public readonly record struct BuildingQueuedItemViewModel(
    int Index,
    string UnitType,
    string DisplayName,
    float ProgressNormalized,
    int RemainingTicks);

public readonly record struct BuildingProductionCardViewModel(
    EntityId BuildingId,
    string BuildingType,
    string DisplayName,
    float Health,
    float MaxHealth,
    float HealthPercentage,
    bool IsConstructed,
    float BuildProgressPercentage,
    Vector2D RallyPoint,
    IReadOnlyList<BuildingProductionOptionViewModel> ProductionOptions,
    IReadOnlyList<BuildingQueuedItemViewModel> QueuedItems,
    int QueueCount,
    int MaxQueueSize);

public readonly record struct PopulationBreakdownViewModel(
    int TotalOccupied,
    int MilitaryCount,
    int WorkerCount,
    int CurrentMaxCapacity,
    int AbsoluteMaxCap,
    bool IsPopCapped,
    string Tooltip);

public readonly record struct InteractiveRtsHudViewModel(
    ResourceBarViewModel ResourceBar,
    MinimapRadarViewModel Minimap,
    UnitStatusPanelViewModel? SingleSelection,
    UnitGroupSummaryViewModel? GroupSelection,
    BuildingProductionCardViewModel? BuildingSelection,
    PopulationBreakdownViewModel PopulationBreakdown,
    IReadOnlyList<AbilityButtonViewModel> HeroAbilities,
    bool HasActiveSelection);

/// <summary>
/// Interactive RTS HUD Presenter maintaining real-time view models for the
/// Top Resource Bar, Minimap Radar, Unit Selection Card, Building Production Card, and Command/Ability Buttons.
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
    private readonly List<BuildingProductionOptionViewModel> _prodOptionsCache = new(8);
    private readonly List<BuildingQueuedItemViewModel> _queuedItemsCache = new(8);

    public GameCoordinator Coordinator => _coordinator;
    public SelectionManager Selection => _selection;
    public RtsCameraController Camera => _camera;

    public InteractiveRtsHud(
        GameCoordinator coordinator,
        SelectionManager selection,
        RtsCameraController camera,
        FactionId? playerFaction = null)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _playerFaction = playerFaction ?? FactionId.Player1;
        _resBarPresenter = new ResourceBarHudPresenter(_coordinator, _playerFaction);
    }

    public InteractiveRtsHudViewModel GenerateHudSnapshot(Vector2D viewportSize)
    {
        var state = _coordinator.Simulation.State;
        var bank = _coordinator.GetResourceBank(_playerFaction);
        var popManager = state.GetOrCreatePopulationManager(_playerFaction);

        // 1. Top Resource Bar
        var resBar = _resBarPresenter.GetViewModel();

        // 2. Population Breakdown
        int totalOccupied = 0;
        int workerCount = 0;
        int militaryCount = 0;

        for (int i = 0; i < state.ActiveUnits.Count; i++)
        {
            var u = state.ActiveUnits[i];
            if (u.FactionId == _playerFaction && u.IsAlive)
            {
                totalOccupied++;
                if (u.WorkerState != null)
                {
                    workerCount++;
                }
                else
                {
                    militaryCount++;
                }
            }
        }

        popManager.SetCurrentPopulation(totalOccupied, _coordinator.CurrentTick);
        popManager.RecalculateCapacity(state.ActiveBuildings, _coordinator.CurrentTick);

        var popBreakdown = new PopulationBreakdownViewModel(
            TotalOccupied: totalOccupied,
            MilitaryCount: militaryCount,
            WorkerCount: workerCount,
            CurrentMaxCapacity: popManager.CurrentMaxCapacity,
            AbsoluteMaxCap: popManager.AbsoluteMaxCap,
            IsPopCapped: popManager.IsPopCapped,
            Tooltip: $"Occupied: {totalOccupied} (Military: {militaryCount}, Workers: {workerCount}) | Capacity: {popManager.CurrentMaxCapacity} / Max: {popManager.AbsoluteMaxCap}");

        // 3. Minimap Radar
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

        // 4. Selection Panels (Unit Single / Squad or Building)
        UnitStatusPanelViewModel? singleSel = null;
        UnitGroupSummaryViewModel? groupSel = null;
        BuildingProductionCardViewModel? buildingSel = null;
        _abilitiesCache.Clear();

        var selectedIds = _selection.SelectedUnitIds;
        bool hasSelection = selectedIds.Count > 0 || _selection.SelectedBuildingId.HasValue;

        if (selectedIds.Count == 1)
        {
            var unitId = selectedIds[0];
            if (state.TryGetUnit(unitId, out var unit) && unit != null && unit.IsAlive)
            {
                var rank = unit.Veterancy.Rank;
                singleSel = new UnitStatusPanelViewModel(
                    UnitId: unit.Id,
                    UnitType: unit.UnitType,
                    DisplayName: FormatDisplayName(unit.UnitType),
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
        else if (_selection.SelectedBuildingId.HasValue)
        {
            if (state.TryGetBuilding(_selection.SelectedBuildingId.Value, out var building) && building != null && building.IsAlive)
            {
                _prodOptionsCache.Clear();
                _queuedItemsCache.Clear();

                var lowerType = building.BuildingType.ToLowerInvariant();
                if (lowerType.Contains("town_center"))
                {
                    var villagerCost = new ResourceCost(Food: 50);
                    _prodOptionsCache.Add(new BuildingProductionOptionViewModel(
                        ActionId: "celtic_villager",
                        DisplayName: "Train Celtic Villager",
                        Hotkey: "[V]",
                        Cost: villagerCost,
                        TrainTimeTicks: 200,
                        PopulationCost: 1,
                        CanAfford: bank.CanAfford(villagerCost),
                        CanTrainPop: popManager.CanTrainUnit(1),
                        Description: "Trains a worker unit for harvesting resources and building settlements."));

                    var eraCost = new ResourceCost(Food: 500, Gold: 300);
                    _prodOptionsCache.Add(new BuildingProductionOptionViewModel(
                        ActionId: "advance_era",
                        DisplayName: "Advance Civilization Era",
                        Hotkey: "[E]",
                        Cost: eraCost,
                        TrainTimeTicks: 0,
                        PopulationCost: 0,
                        CanAfford: bank.CanAfford(eraCost),
                        CanTrainPop: true,
                        Description: "Advances your civilization to the next age, unlocking advanced buildings and units."));
                }
                else if (lowerType.Contains("barracks"))
                {
                    var swordsmanCost = new ResourceCost(Food: 60, Wood: 20);
                    _prodOptionsCache.Add(new BuildingProductionOptionViewModel(
                        ActionId: "celtic_swordsman",
                        DisplayName: "Train Celtic Swordsman",
                        Hotkey: "[S]",
                        Cost: swordsmanCost,
                        TrainTimeTicks: 250,
                        PopulationCost: 1,
                        CanAfford: bank.CanAfford(swordsmanCost),
                        CanTrainPop: popManager.CanTrainUnit(1),
                        Description: "Frontline melee warrior armed with broadsword and Celtic shield."));

                    var archerCost = new ResourceCost(Food: 50, Wood: 40);
                    _prodOptionsCache.Add(new BuildingProductionOptionViewModel(
                        ActionId: "celtic_archer",
                        DisplayName: "Train Celtic Archer",
                        Hotkey: "[A]",
                        Cost: archerCost,
                        TrainTimeTicks: 250,
                        PopulationCost: 1,
                        CanAfford: bank.CanAfford(archerCost),
                        CanTrainPop: popManager.CanTrainUnit(1),
                        Description: "Ranged skirmisher firing rapid arrows from longbows."));
                }
                else if (lowerType.Contains("blacksmith"))
                {
                    var bladesCost = new ResourceCost(Wood: 100, Gold: 50);
                    _prodOptionsCache.Add(new BuildingProductionOptionViewModel(
                        ActionId: "upgrade_forged_blades",
                        DisplayName: "Forged Blades (+2 Melee Damage)",
                        Hotkey: "[F]",
                        Cost: bladesCost,
                        TrainTimeTicks: 0,
                        PopulationCost: 0,
                        CanAfford: bank.CanAfford(bladesCost),
                        CanTrainPop: true,
                        Description: "Hones swords and axes with hardened steel, increasing melee attack power by +2."));

                    var armorCost = new ResourceCost(Wood: 75, Iron: 75);
                    _prodOptionsCache.Add(new BuildingProductionOptionViewModel(
                        ActionId: "upgrade_scale_armor",
                        DisplayName: "Scale Armor (+2 Armor)",
                        Hotkey: "[R]",
                        Cost: armorCost,
                        TrainTimeTicks: 0,
                        PopulationCost: 0,
                        CanAfford: bank.CanAfford(armorCost),
                        CanTrainPop: true,
                        Description: "Equips warriors with reinforced scale armor, increasing protection by +2."));
                }
                else if (lowerType.Contains("stable") || lowerType.Contains("stables"))
                {
                    var cavCost = new ResourceCost(Food: 70, Gold: 45);
                    _prodOptionsCache.Add(new BuildingProductionOptionViewModel(
                        ActionId: "celtic_cavalry",
                        DisplayName: "Train Celtic Cavalry",
                        Hotkey: "[C]",
                        Cost: cavCost,
                        TrainTimeTicks: 300,
                        PopulationCost: 2,
                        CanAfford: bank.CanAfford(cavCost),
                        CanTrainPop: popManager.CanTrainUnit(2),
                        Description: "Fast mounted horseman with charge momentum bonus."));
                }

                var qItems = building.ProductionQueue.Items;
                for (int q = 0; q < qItems.Count; q++)
                {
                    var item = qItems[q];
                    _queuedItemsCache.Add(new BuildingQueuedItemViewModel(
                        Index: q,
                        UnitType: item.UnitType,
                        DisplayName: FormatDisplayName(item.UnitType),
                        ProgressNormalized: item.ProgressNormalized,
                        RemainingTicks: item.TotalDurationTicks - item.ProgressTicks));
                }

                buildingSel = new BuildingProductionCardViewModel(
                    BuildingId: building.Id,
                    BuildingType: building.BuildingType,
                    DisplayName: FormatDisplayName(building.BuildingType),
                    Health: building.CurrentHealth,
                    MaxHealth: building.MaxHealth,
                    HealthPercentage: building.CurrentHealth / building.MaxHealth,
                    IsConstructed: building.IsConstructed,
                    BuildProgressPercentage: building.BuildProgressNormalized,
                    RallyPoint: building.RallyPoint,
                    ProductionOptions: _prodOptionsCache.ToArray(),
                    QueuedItems: _queuedItemsCache.ToArray(),
                    QueueCount: building.ProductionQueue.Count,
                    MaxQueueSize: building.ProductionQueue.MaxQueueSize);
            }
        }

        return new InteractiveRtsHudViewModel(
            ResourceBar: resBar,
            Minimap: minimap,
            SingleSelection: singleSel,
            GroupSelection: groupSel,
            BuildingSelection: buildingSel,
            PopulationBreakdown: popBreakdown,
            HeroAbilities: _abilitiesCache,
            HasActiveSelection: hasSelection);
    }

    private static string FormatDisplayName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return string.Empty;
        var parts = rawName.Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..].ToLowerInvariant();
            }
        }
        return string.Join(" ", parts);
    }
}
