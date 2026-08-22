using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Presenter bridging domain simulation state to visual HUD, unit selection panels, and camera.
/// </summary>
public sealed class CombatArenaPresenter
{
    private readonly CombatArenaScenario _scenario;
    private readonly RtsCameraController _camera;
    private readonly List<string> _combatLog = new(128);
    private readonly List<string> _celebrationToasts = new(32);

    public CombatArenaScenario Scenario => _scenario;
    public GameCoordinator Coordinator => _scenario.Coordinator;
    public SelectionManager Selection => _scenario.Selection;
    public RtsCameraController Camera => _camera;
    public IReadOnlyList<string> CombatLog => _combatLog;
    public IReadOnlyList<string> CelebrationToasts => _celebrationToasts;

    public CombatArenaPresenter(CombatArenaScenario? scenario = null, RtsCameraController? camera = null)
    {
        _scenario = scenario ?? new CombatArenaScenario();
        _camera = camera ?? new RtsCameraController();

        // Subscribe to presentation events
        Coordinator.EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        Coordinator.EventBus.Subscribe<UnitKilledEvent>(OnUnitKilled);
        Coordinator.EventBus.Subscribe<UnitLevelUpEvent>(OnUnitLevelUp);
        Coordinator.EventBus.Subscribe<VeterancyRankChangedEvent>(OnRankChanged);
    }

    public List<UnitPresentationViewModel> GetUnitViewModels()
    {
        var activeUnits = Coordinator.Simulation.State.ActiveUnits;
        var selectedIds = Selection.SelectedUnitIds;
        var viewModels = new List<UnitPresentationViewModel>(activeUnits.Count);

        for (int i = 0; i < activeUnits.Count; i++)
        {
            var u = activeUnits[i];
            bool isSelected = false;
            for (int s = 0; s < selectedIds.Count; s++)
            {
                if (selectedIds[s] == u.Id)
                {
                    isSelected = true;
                    break;
                }
            }

            viewModels.Add(new UnitPresentationViewModel(
                Id: u.Id,
                FactionId: u.FactionId,
                UnitType: u.UnitType,
                Position: u.Position,
                CurrentHealth: u.CurrentHealth,
                MaxHealth: u.MaxHealth,
                HealthPercentage: u.MaxHealth > 0f ? u.CurrentHealth / u.MaxHealth : 0f,
                Armor: u.Armor,
                AttackDamage: u.AttackDamage,
                AttackRange: u.AttackRange,
                AttackType: u.AttackType,
                Level: u.Veterancy.Level,
                CurrentXp: u.Veterancy.CurrentXp,
                XpToNextLevel: u.Veterancy.XpToNextLevel,
                KillCount: u.Veterancy.KillCount,
                Rank: u.Veterancy.Rank,
                RankName: u.Veterancy.Rank.GetDisplayName(),
                State: u.State,
                IsSelected: isSelected));
        }

        return viewModels;
    }

    public UnitPresentationViewModel? GetPrimarySelectedUnitViewModel()
    {
        if (Selection.SelectedUnitIds.Count == 0) return null;

        var primaryId = Selection.SelectedUnitIds[0];
        if (Coordinator.Simulation.State.TryGetUnit(primaryId, out var unit) && unit != null)
        {
            return new UnitPresentationViewModel(
                Id: unit.Id,
                FactionId: unit.FactionId,
                UnitType: unit.UnitType,
                Position: unit.Position,
                CurrentHealth: unit.CurrentHealth,
                MaxHealth: unit.MaxHealth,
                HealthPercentage: unit.MaxHealth > 0f ? unit.CurrentHealth / unit.MaxHealth : 0f,
                Armor: unit.Armor,
                AttackDamage: unit.AttackDamage,
                AttackRange: unit.AttackRange,
                AttackType: unit.AttackType,
                Level: unit.Veterancy.Level,
                CurrentXp: unit.Veterancy.CurrentXp,
                XpToNextLevel: unit.Veterancy.XpToNextLevel,
                KillCount: unit.Veterancy.KillCount,
                Rank: unit.Veterancy.Rank,
                RankName: unit.Veterancy.Rank.GetDisplayName(),
                State: unit.State,
                IsSelected: true);
        }

        return null;
    }

    private void OnDamageDealt(in DamageDealtEvent e)
    {
        _combatLog.Add($"[Tick {e.SimulationTick}] Unit {e.AttackerId} dealt {e.DamageAmount:F1} dmg to {e.TargetId} (Remaining HP: {e.RemainingHealth:F1})");
    }

    private void OnUnitKilled(in UnitKilledEvent e)
    {
        _combatLog.Add($"[Tick {e.SimulationTick}] Unit {e.CasualtyId} (Faction {e.CasualtyFaction}) slain by Unit {e.KillerId} (Faction {e.KillerFaction})");
    }

    private void OnUnitLevelUp(in UnitLevelUpEvent e)
    {
        string toast = $"⭐ LEVEL UP! Unit {e.UnitId} reached Level {e.NewLevel}! (+{e.MaxHealthIncrease:F0} HP, +{e.AttackDamageIncrease:F1} DMG)";
        _celebrationToasts.Add(toast);
        _combatLog.Add($"[Tick {e.SimulationTick}] {toast}");
    }

    private void OnRankChanged(in VeterancyRankChangedEvent e)
    {
        string toast = $"🏆 RANK PROMOTION! Unit {e.UnitId} promoted to {e.NewRank.GetDisplayName()}!";
        _celebrationToasts.Add(toast);
        _combatLog.Add($"[Tick {e.SimulationTick}] {toast}");
    }
}
