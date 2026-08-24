using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// Veterancy Presentation View Models & Presenter
// ─────────────────────────────────────────────────

/// <summary>
/// Describes the visual badge and rank overlay for a unit's veterancy.
/// </summary>
public readonly record struct VeterancyBadgeDescriptor(
    EntityId UnitId,
    VeterancyRank Rank,
    string RankDisplayName,
    int BadgeIconIndex,
    int Level,
    int ChevronCount,
    bool ShowLevelUpEffect);

/// <summary>
/// Presenter that tracks veterancy visual state and level-up VFX triggers.
/// Subscribes to domain events for real-time level-up effects.
/// </summary>
public sealed class VeterancyPresenter
{
    private readonly DomainEventBus _eventBus;

    // Pre-allocated buffers for active level-up effects
    private readonly EntityId[] _levelUpEffectUnits;
    private int _levelUpEffectCount;
    private readonly int _maxEffects;

    public int PendingLevelUpEffectCount => _levelUpEffectCount;

    public VeterancyPresenter(DomainEventBus eventBus, int maxEffects = 64)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _maxEffects = maxEffects;
        _levelUpEffectUnits = new EntityId[maxEffects];
        _levelUpEffectCount = 0;

        _eventBus.Subscribe<UnitLevelUpEvent>(OnUnitLevelUp);
        _eventBus.Subscribe<VeterancyRankChangedEvent>(OnVeterancyRankChanged);
    }

    /// <summary>
    /// Generates a badge descriptor for a unit's current veterancy state.
    /// </summary>
    public VeterancyBadgeDescriptor GetBadgeDescriptor(UnitEntity unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var rank = unit.Veterancy.Rank;
        bool showLevelUp = HasPendingLevelUpEffect(unit.Id);

        return new VeterancyBadgeDescriptor(
            UnitId: unit.Id,
            Rank: rank,
            RankDisplayName: rank.GetDisplayName(),
            BadgeIconIndex: GetBadgeIconIndex(rank),
            Level: unit.Veterancy.Level,
            ChevronCount: GetChevronCount(rank),
            ShowLevelUpEffect: showLevelUp);
    }

    /// <summary>
    /// Consumes a pending level-up effect for a unit, returning true if one was pending.
    /// </summary>
    public bool ConsumeLevelUpEffect(EntityId unitId)
    {
        for (int i = 0; i < _levelUpEffectCount; i++)
        {
            if (_levelUpEffectUnits[i] == unitId)
            {
                // Remove by swapping with last
                _levelUpEffectUnits[i] = _levelUpEffectUnits[_levelUpEffectCount - 1];
                _levelUpEffectCount--;
                return true;
            }
        }
        return false;
    }

    public EntityId GetPendingLevelUpUnit(int index)
    {
        if (index < 0 || index >= _levelUpEffectCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _levelUpEffectUnits[index];
    }

    public void ClearPendingEffects() => _levelUpEffectCount = 0;

    public void Unregister()
    {
        _eventBus.Unsubscribe<UnitLevelUpEvent>(OnUnitLevelUp);
        _eventBus.Unsubscribe<VeterancyRankChangedEvent>(OnVeterancyRankChanged);
    }

    private void OnUnitLevelUp(in UnitLevelUpEvent evt)
    {
        if (_levelUpEffectCount < _maxEffects)
        {
            _levelUpEffectUnits[_levelUpEffectCount++] = evt.UnitId;
        }
    }

    private void OnVeterancyRankChanged(in VeterancyRankChangedEvent evt)
    {
        // Rank changes also trigger level-up effects (stronger visual)
        if (_levelUpEffectCount < _maxEffects)
        {
            // Ensure no duplicate for same unit
            if (!HasPendingLevelUpEffect(evt.UnitId))
            {
                _levelUpEffectUnits[_levelUpEffectCount++] = evt.UnitId;
            }
        }
    }

    private bool HasPendingLevelUpEffect(EntityId unitId)
    {
        for (int i = 0; i < _levelUpEffectCount; i++)
        {
            if (_levelUpEffectUnits[i] == unitId) return true;
        }
        return false;
    }

    /// <summary>
    /// Maps veterancy rank to a badge icon index for the sprite atlas.
    /// </summary>
    public static int GetBadgeIconIndex(VeterancyRank rank) => rank switch
    {
        VeterancyRank.Recruit => 0,
        VeterancyRank.Experienced => 1,
        VeterancyRank.Veteran => 2,
        VeterancyRank.Elite => 3,
        VeterancyRank.Legendary => 4,
        _ => 0
    };

    /// <summary>
    /// Maps veterancy rank to chevron/star count for visual display.
    /// </summary>
    public static int GetChevronCount(VeterancyRank rank) => rank switch
    {
        VeterancyRank.Recruit => 0,
        VeterancyRank.Experienced => 1,
        VeterancyRank.Veteran => 2,
        VeterancyRank.Elite => 3,
        VeterancyRank.Legendary => 5,
        _ => 0
    };
}
