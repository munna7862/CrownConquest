using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Events;

/// <summary>
/// Emitted when a unit is spawned into the simulation.
/// </summary>
public readonly record struct UnitSpawnedEvent(
    ulong SimulationTick,
    EntityId UnitId,
    FactionId FactionId,
    string UnitType,
    Vector2D Position) : IDomainEvent;

/// <summary>
/// Emitted when a unit moves to a new position.
/// </summary>
public readonly record struct UnitMovedEvent(
    ulong SimulationTick,
    EntityId UnitId,
    Vector2D PreviousPosition,
    Vector2D NewPosition) : IDomainEvent;

/// <summary>
/// Emitted when damage is dealt from an attacker to a target entity.
/// </summary>
public readonly record struct DamageDealtEvent(
    ulong SimulationTick,
    EntityId AttackerId,
    EntityId TargetId,
    float DamageAmount,
    float RemainingHealth,
    bool IsCritical) : IDomainEvent;

/// <summary>
/// Emitted when a unit is killed in combat.
/// </summary>
public readonly record struct UnitKilledEvent(
    ulong SimulationTick,
    EntityId CasualtyId,
    EntityId KillerId,
    FactionId CasualtyFaction,
    FactionId KillerFaction,
    Vector2D DeathPosition) : IDomainEvent;
