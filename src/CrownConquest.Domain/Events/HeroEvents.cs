using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Events;

public readonly record struct HeroLevelUpEvent(
    ulong SimulationTick,
    EntityId HeroId,
    FactionId FactionId,
    int OldLevel,
    int NewLevel,
    HeroAttributes TotalAttributes) : IDomainEvent;

public readonly record struct HeroAbilityCastEvent(
    ulong SimulationTick,
    EntityId HeroId,
    FactionId FactionId,
    string AbilityId,
    EntityId TargetEntityId,
    Vector2D TargetPosition,
    float ManaCost) : IDomainEvent;

public readonly record struct HeroAttachedUnitsChangedEvent(
    ulong SimulationTick,
    EntityId HeroId,
    FactionId FactionId,
    int AttachedCount,
    int Capacity) : IDomainEvent;

public readonly record struct HeroFallenEvent(
    ulong SimulationTick,
    EntityId HeroId,
    FactionId FactionId,
    Vector2D Position) : IDomainEvent;

public readonly record struct HeroAttributeAllocatedEvent(
    ulong SimulationTick,
    EntityId HeroId,
    FactionId FactionId,
    string AttributeName,
    HeroAttributes TotalAttributes) : IDomainEvent;
