using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Events;

/// <summary>
/// Emitted when a unit gains experience points from a kill or objective.
/// </summary>
public readonly record struct UnitGainedXpEvent(
    ulong SimulationTick,
    EntityId UnitId,
    int XpGained,
    int TotalXp,
    int XpToNextLevel) : IDomainEvent;

/// <summary>
/// Emitted when a unit advances to a higher level.
/// </summary>
public readonly record struct UnitLevelUpEvent(
    ulong SimulationTick,
    EntityId UnitId,
    int OldLevel,
    int NewLevel,
    float MaxHealthIncrease,
    float AttackDamageIncrease) : IDomainEvent;

/// <summary>
/// Emitted when a unit's veterancy rank changes (e.g. Recruit -> Experienced -> Veteran).
/// </summary>
public readonly record struct VeterancyRankChangedEvent(
    ulong SimulationTick,
    EntityId UnitId,
    VeterancyRank OldRank,
    VeterancyRank NewRank) : IDomainEvent;
