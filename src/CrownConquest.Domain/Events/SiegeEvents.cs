using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Events;

/// <summary>
/// Published when a wall segment is destroyed and transformed into a breach.
/// </summary>
public readonly record struct WallBreachedEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    FactionId FactionId,
    Vector2D Position,
    string WallType) : IDomainEvent;

/// <summary>
/// Published when a fortress gate changes its state (Open, Closed, Locked).
/// </summary>
public readonly record struct GateStateChangedEvent(
    ulong SimulationTick,
    EntityId GateId,
    FactionId FactionId,
    GateState OldState,
    GateState NewState) : IDomainEvent;

/// <summary>
/// Published when a defensive tower fires at an enemy target.
/// </summary>
public readonly record struct TowerAttackEvent(
    ulong SimulationTick,
    EntityId TowerId,
    FactionId FactionId,
    EntityId TargetId,
    float DamageDealt,
    Vector2D TargetPosition) : IDomainEvent;

/// <summary>
/// Published when a unit garrisons into a defensive tower.
/// </summary>
public readonly record struct UnitGarrisonedEvent(
    ulong SimulationTick,
    EntityId TowerId,
    EntityId UnitId,
    FactionId FactionId,
    int GarrisonCount) : IDomainEvent;

/// <summary>
/// Published when a unit exits a tower.
/// </summary>
public readonly record struct UnitUngarrisonedEvent(
    ulong SimulationTick,
    EntityId TowerId,
    EntityId UnitId,
    FactionId FactionId,
    Vector2D EgressPosition) : IDomainEvent;

/// <summary>
/// Published when a siege engine (Catapult) hits an area with splash damage.
/// </summary>
public readonly record struct SiegeAreaOfEffectImpactEvent(
    ulong SimulationTick,
    EntityId AttackerId,
    FactionId FactionId,
    Vector2D ImpactCenter,
    float Radius,
    int TargetsHit,
    float TotalDamage) : IDomainEvent;

/// <summary>
/// Published when a building or fortification takes damage from an attack.
/// </summary>
public readonly record struct BuildingAttackedEvent(
    ulong SimulationTick,
    EntityId AttackerId,
    FactionId AttackerFaction,
    EntityId BuildingId,
    FactionId BuildingFaction,
    float DamageDealt,
    float RemainingHealth) : IDomainEvent;
