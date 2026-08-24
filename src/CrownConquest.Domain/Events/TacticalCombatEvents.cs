using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Events;

public readonly record struct UnitFormationChangedEvent(
    ulong SimulationTick,
    EntityId UnitId,
    FormationType Formation) : IDomainEvent;

public readonly record struct UnitMoraleChangedEvent(
    ulong SimulationTick,
    EntityId UnitId,
    float CurrentMorale,
    MoraleLevel Level) : IDomainEvent;

public readonly record struct UnitRoutedEvent(
    ulong SimulationTick,
    EntityId UnitId,
    FactionId FactionId,
    Vector2D Position) : IDomainEvent;

public readonly record struct UnitRalliedEvent(
    ulong SimulationTick,
    EntityId UnitId,
    FactionId FactionId,
    float CurrentMorale) : IDomainEvent;

public readonly record struct CavalryChargeImpactEvent(
    ulong SimulationTick,
    EntityId AttackerId,
    EntityId TargetId,
    float DamageDealt,
    bool Braced,
    float RecoilDamage) : IDomainEvent;
