using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// Building Visual State Presenter
// ─────────────────────────────────────────────────

/// <summary>
/// Visual phase of a building during its lifecycle.
/// </summary>
public enum BuildingVisualPhase
{
    Placement,
    UnderConstruction,
    Completed,
    Damaged,
    Destroyed
}

/// <summary>
/// Describes the visual state of a building for rendering.
/// </summary>
public readonly record struct BuildingVisualState(
    EntityId BuildingId,
    FactionId FactionId,
    string BuildingType,
    Vector2D Position,
    float ConstructionProgress,
    float HealthPercentage,
    BuildingVisualPhase VisualPhase,
    int FactionColorIndex,
    bool ShowConstructionAnimation,
    bool ShowDamageOverlay,
    bool ShowCompletionFlash);

/// <summary>
/// Presenter that generates building visual state descriptors.
/// </summary>
public sealed class BuildingVisualPresenter
{
    private readonly BuildingVisualState[] _states;
    private int _activeCount;
    private readonly int _maxBuildings;

    public int ActiveCount => _activeCount;

    public BuildingVisualPresenter(int maxBuildings = 128)
    {
        _maxBuildings = maxBuildings;
        _states = new BuildingVisualState[maxBuildings];
        _activeCount = 0;
    }

    /// <summary>
    /// Updates all building visual states from current simulation state.
    /// </summary>
    public void UpdateStates(BuildingEntity[] buildings, int count)
    {
        _activeCount = 0;

        for (int i = 0; i < count && _activeCount < _maxBuildings; i++)
        {
            var building = buildings[i];
            float healthPct = building.MaxHealth > 0f ? building.CurrentHealth / building.MaxHealth : 0f;
            var phase = DetermineVisualPhase(building, healthPct);
            int colorIndex = SelectionFeedbackPresenter.GetFactionColorIndex(building.FactionId);

            _states[_activeCount++] = new BuildingVisualState(
                BuildingId: building.Id,
                FactionId: building.FactionId,
                BuildingType: building.BuildingType,
                Position: building.Position,
                ConstructionProgress: building.BuildProgressNormalized,
                HealthPercentage: healthPct,
                VisualPhase: phase,
                FactionColorIndex: colorIndex,
                ShowConstructionAnimation: !building.IsConstructed,
                ShowDamageOverlay: healthPct < 0.5f && building.IsConstructed,
                ShowCompletionFlash: false);
        }
    }

    /// <summary>
    /// Generates a single building visual state descriptor.
    /// </summary>
    public static BuildingVisualState GetVisualState(BuildingEntity building)
    {
        ArgumentNullException.ThrowIfNull(building);

        float healthPct = building.MaxHealth > 0f ? building.CurrentHealth / building.MaxHealth : 0f;
        var phase = DetermineVisualPhase(building, healthPct);
        int colorIndex = SelectionFeedbackPresenter.GetFactionColorIndex(building.FactionId);

        return new BuildingVisualState(
            BuildingId: building.Id,
            FactionId: building.FactionId,
            BuildingType: building.BuildingType,
            Position: building.Position,
            ConstructionProgress: building.BuildProgressNormalized,
            HealthPercentage: healthPct,
            VisualPhase: phase,
            FactionColorIndex: colorIndex,
            ShowConstructionAnimation: !building.IsConstructed,
            ShowDamageOverlay: healthPct < 0.5f && building.IsConstructed,
            ShowCompletionFlash: false);
    }

    public BuildingVisualState GetState(int index)
    {
        if (index < 0 || index >= _activeCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _states[index];
    }

    private static BuildingVisualPhase DetermineVisualPhase(BuildingEntity building, float healthPct)
    {
        if (healthPct <= 0f) return BuildingVisualPhase.Destroyed;
        if (!building.IsConstructed) return BuildingVisualPhase.UnderConstruction;
        if (healthPct < 0.5f) return BuildingVisualPhase.Damaged;
        return BuildingVisualPhase.Completed;
    }
}
