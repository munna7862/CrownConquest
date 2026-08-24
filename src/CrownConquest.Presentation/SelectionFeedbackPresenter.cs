using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// Selection Feedback View Models & Presenter
// ─────────────────────────────────────────────────

/// <summary>
/// Describes a selection ring overlay for a unit on the battlefield.
/// </summary>
public readonly record struct SelectionRingDescriptor(
    EntityId UnitId,
    Vector2D Position,
    float Radius,
    int FactionColorIndex,
    bool IsSelected,
    bool IsHovered,
    float HealthPercentage);

/// <summary>
/// Describes a health bar overlay positioned relative to a unit.
/// </summary>
public readonly record struct HealthBarDescriptor(
    EntityId UnitId,
    Vector2D Position,
    float CurrentHealth,
    float MaxHealth,
    float HealthPercentage,
    int FactionColorIndex,
    bool IsVisible);

/// <summary>
/// Presenter that generates selection feedback descriptors for all visible units.
/// Completely stateless — derives all data from simulation state each frame.
/// </summary>
public sealed class SelectionFeedbackPresenter
{
    // Pre-allocated descriptor buffers to avoid per-frame allocations
    private readonly SelectionRingDescriptor[] _selectionRings;
    private readonly HealthBarDescriptor[] _healthBars;
    private int _activeRingCount;
    private int _activeHealthBarCount;
    private readonly int _maxDescriptors;

    public int ActiveRingCount => _activeRingCount;
    public int ActiveHealthBarCount => _activeHealthBarCount;

    public SelectionFeedbackPresenter(int maxDescriptors = 512)
    {
        _maxDescriptors = maxDescriptors;
        _selectionRings = new SelectionRingDescriptor[maxDescriptors];
        _healthBars = new HealthBarDescriptor[maxDescriptors];
        _activeRingCount = 0;
        _activeHealthBarCount = 0;
    }

    /// <summary>
    /// Updates all selection ring and health bar descriptors from current unit state.
    /// </summary>
    public void UpdateDescriptors(
        UnitEntity[] visibleUnits,
        int visibleCount,
        EntityId[] selectedUnitIds,
        int selectedCount,
        EntityId hoveredUnitId)
    {
        _activeRingCount = 0;
        _activeHealthBarCount = 0;

        for (int i = 0; i < visibleCount && i < _maxDescriptors; i++)
        {
            var unit = visibleUnits[i];
            if (!unit.IsAlive) continue;

            bool isSelected = IsInArray(unit.Id, selectedUnitIds, selectedCount);
            bool isHovered = unit.Id == hoveredUnitId;
            float healthPct = unit.MaxHealth > 0f ? unit.CurrentHealth / unit.MaxHealth : 0f;
            int colorIndex = GetFactionColorIndex(unit.FactionId);
            float radius = GetSelectionRadius(unit.Archetype);

            if (isSelected || isHovered)
            {
                _selectionRings[_activeRingCount++] = new SelectionRingDescriptor(
                    unit.Id, unit.Position, radius, colorIndex,
                    isSelected, isHovered, healthPct);
            }

            // Health bars visible for damaged or selected units
            if (healthPct < 1.0f || isSelected)
            {
                _healthBars[_activeHealthBarCount++] = new HealthBarDescriptor(
                    unit.Id, unit.Position,
                    unit.CurrentHealth, unit.MaxHealth, healthPct,
                    colorIndex, IsVisible: true);
            }
        }
    }

    public SelectionRingDescriptor GetSelectionRing(int index)
    {
        if (index < 0 || index >= _activeRingCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _selectionRings[index];
    }

    public HealthBarDescriptor GetHealthBar(int index)
    {
        if (index < 0 || index >= _activeHealthBarCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _healthBars[index];
    }

    private static bool IsInArray(EntityId id, EntityId[] array, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (array[i] == id) return true;
        }
        return false;
    }

    public static int GetFactionColorIndex(FactionId factionId)
    {
        if (factionId == FactionId.Player1) return 0; // Blue
        if (factionId == FactionId.Player2) return 1; // Red
        return 2;                                      // Neutral / Gray
    }

    private static float GetSelectionRadius(UnitArchetype archetype) => archetype switch
    {
        UnitArchetype.Siege => 1.8f,
        UnitArchetype.Cavalry => 1.4f,
        UnitArchetype.Hero => 1.6f,
        _ => 1.0f
    };
}
