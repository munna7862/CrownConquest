using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

public readonly record struct FormationOptionPresentation(
    FormationType Formation,
    string DisplayName,
    string Description,
    bool IsActive,
    float MeleeDamageMultiplier,
    float ArmorBonus,
    float MovementSpeedMultiplier,
    float RangedDamageMitigation,
    bool CanBraceCavalry);

public readonly record struct UnitTacticalCardPresentation(
    EntityId UnitId,
    string UnitType,
    UnitArchetype Archetype,
    FormationType Formation,
    float CurrentMorale,
    float MaxMorale,
    MoraleLevel MoraleLevel,
    TerrainType Terrain,
    int ElevationLevel,
    float ChargeMomentumProgress,
    bool IsCharging,
    bool IsRouted);

/// <summary>
/// Presentation layer view model providing tactical combat telemetry, formation selection,
/// morale gauges, terrain analysis, and charge momentum tracking.
/// </summary>
public sealed class TacticalCombatPresenter
{
    private readonly GameCoordinator _coordinator;
    private readonly FactionId _factionId;
    private readonly List<EntityId> _selectedUnitIds = new();

    public IReadOnlyList<EntityId> SelectedUnitIds => _selectedUnitIds;
    public int SelectedCount => _selectedUnitIds.Count;
    public bool HasSelection => _selectedUnitIds.Count > 0;

    public FormationType ActiveFormation { get; private set; } = FormationType.Line;
    public float AverageMorale { get; private set; } = 100.0f;
    public MoraleLevel PrimaryMoraleLevel { get; private set; } = MoraleLevel.Confident;
    public bool HasRoutedUnits { get; private set; }
    public bool AllUnitsRouted { get; private set; }
    public TerrainType PrimaryTerrain { get; private set; } = TerrainType.Plains;
    public int PrimaryElevation { get; private set; } = 0;
    public float PrimaryChargeProgress { get; private set; } = 0f;
    public bool IsAnyUnitCharging { get; private set; }

    public List<FormationOptionPresentation> FormationOptions { get; } = new();
    public List<UnitTacticalCardPresentation> UnitCards { get; } = new();

    public TacticalCombatPresenter(GameCoordinator coordinator, FactionId factionId)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _factionId = factionId;
        UpdateSnapshot();
    }

    public void SelectUnits(IEnumerable<EntityId> unitIds)
    {
        _selectedUnitIds.Clear();
        foreach (var id in unitIds)
        {
            if (_coordinator.Simulation.State.TryGetUnit(id, out var unit) && unit != null && unit.FactionId == _factionId && unit.IsAlive)
            {
                _selectedUnitIds.Add(id);
            }
        }
        UpdateSnapshot();
    }

    public void ClearSelection()
    {
        _selectedUnitIds.Clear();
        UpdateSnapshot();
    }

    public void SetFormation(FormationType formation)
    {
        if (_selectedUnitIds.Count == 0) return;

        if (_selectedUnitIds.Count == 1)
        {
            _coordinator.DispatchCommand(new SetFormationCommand(_factionId, _selectedUnitIds[0], formation));
        }
        else
        {
            _coordinator.DispatchCommand(new SetSquadFormationCommand(_factionId, _selectedUnitIds.ToArray(), formation));
        }

        ActiveFormation = formation;
        UpdateSnapshot();
    }

    public void RallySelectedUnits()
    {
        if (_selectedUnitIds.Count == 0) return;

        for (int i = 0; i < _selectedUnitIds.Count; i++)
        {
            _coordinator.DispatchCommand(new RallyUnitCommand(_factionId, _selectedUnitIds[i]));
        }
        UpdateSnapshot();
    }

    public void UpdateSnapshot()
    {
        UnitCards.Clear();
        FormationOptions.Clear();

        if (_selectedUnitIds.Count == 0)
        {
            AverageMorale = 100.0f;
            PrimaryMoraleLevel = MoraleLevel.Confident;
            HasRoutedUnits = false;
            AllUnitsRouted = false;
            PrimaryTerrain = TerrainType.Plains;
            PrimaryElevation = 0;
            PrimaryChargeProgress = 0f;
            IsAnyUnitCharging = false;
            PopulateFormationOptions(FormationType.Line);
            return;
        }

        float totalMorale = 0f;
        int livingCount = 0;
        int routedCount = 0;
        float maxChargeProgress = 0f;
        bool anyCharging = false;
        FormationType dominantFormation = FormationType.Line;
        TerrainType dominantTerrain = TerrainType.Plains;
        int dominantElevation = 0;

        for (int i = 0; i < _selectedUnitIds.Count; i++)
        {
            if (_coordinator.Simulation.State.TryGetUnit(_selectedUnitIds[i], out var unit) && unit != null && unit.IsAlive)
            {
                livingCount++;
                totalMorale += unit.Morale.CurrentMorale;
                if (unit.IsRouted) routedCount++;

                dominantFormation = unit.Formation;
                dominantTerrain = unit.CurrentTerrain;
                dominantElevation = unit.TerrainModifiers.ElevationLevel;

                if (unit.Charge.MomentumProgress > maxChargeProgress)
                {
                    maxChargeProgress = unit.Charge.MomentumProgress;
                }
                if (unit.Charge.IsCharging)
                {
                    anyCharging = true;
                }

                UnitCards.Add(new UnitTacticalCardPresentation(
                    unit.Id,
                    unit.UnitType,
                    unit.Archetype,
                    unit.Formation,
                    unit.Morale.CurrentMorale,
                    unit.Morale.MaxMorale,
                    unit.Morale.Level,
                    unit.CurrentTerrain,
                    unit.TerrainModifiers.ElevationLevel,
                    unit.Charge.MomentumProgress,
                    unit.Charge.IsCharging,
                    unit.IsRouted));
            }
        }

        if (livingCount > 0)
        {
            AverageMorale = totalMorale / livingCount;
            HasRoutedUnits = routedCount > 0;
            AllUnitsRouted = routedCount == livingCount;
            ActiveFormation = dominantFormation;
            PrimaryTerrain = dominantTerrain;
            PrimaryElevation = dominantElevation;
            PrimaryChargeProgress = maxChargeProgress;
            IsAnyUnitCharging = anyCharging;

            PrimaryMoraleLevel = AverageMorale switch
            {
                <= 0.001f => MoraleLevel.Routed,
                < 25.0f => MoraleLevel.Breaking,
                < 50.0f => MoraleLevel.Wavering,
                < 80.0f => MoraleLevel.Steady,
                _ => MoraleLevel.Confident
            };
        }
        else
        {
            AverageMorale = 0f;
            PrimaryMoraleLevel = MoraleLevel.Routed;
            HasRoutedUnits = false;
            AllUnitsRouted = true;
        }

        PopulateFormationOptions(ActiveFormation);
    }

    private void PopulateFormationOptions(FormationType active)
    {
        FormationOptions.Add(CreateFormationOption(FormationType.Line, "Line", "Maximizes melee engagement front.", active));
        FormationOptions.Add(CreateFormationOption(FormationType.ShieldWall, "Shield Wall", "+4 Armor, +50% Ranged mitigation, Braces vs Cavalry.", active));
        FormationOptions.Add(CreateFormationOption(FormationType.Wedge, "Wedge", "+30% Charge Damage, +15% Speed, Arrowhead shock.", active));
        FormationOptions.Add(CreateFormationOption(FormationType.Square, "Square", "+2 Armor, All-round defense, Prevents flanking.", active));
        FormationOptions.Add(CreateFormationOption(FormationType.Loose, "Loose", "-40% Ranged/AoE damage, Dispersed spacing.", active));
        FormationOptions.Add(CreateFormationOption(FormationType.Column, "Column", "+25% Move speed, Rapid road transit.", active));
    }

    private static FormationOptionPresentation CreateFormationOption(FormationType type, string name, string description, FormationType active)
    {
        var mod = FormationModifiers.GetDefault(type);
        return new FormationOptionPresentation(
            type,
            name,
            description,
            type == active,
            mod.MeleeDamageMultiplier,
            mod.ArmorBonus,
            mod.MovementSpeedMultiplier,
            mod.RangedDamageMitigation,
            mod.CanBraceCavalry);
    }
}
