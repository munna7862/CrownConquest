using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;

namespace CrownConquest.Domain.Events;

public readonly record struct ResourceHarvestedEvent(
    ulong SimulationTick,
    EntityId WorkerId,
    EntityId NodeId,
    ResourceType Type,
    int AmountHarvested,
    int CarriedTotal) : IDomainEvent;

public readonly record struct ResourceDepositedEvent(
    ulong SimulationTick,
    FactionId FactionId,
    EntityId WorkerId,
    ResourceType Type,
    int AmountDeposited,
    int NewBankBalance) : IDomainEvent;

public readonly record struct ResourceSpentEvent(
    ulong SimulationTick,
    FactionId FactionId,
    ResourceCost Cost,
    string Reason) : IDomainEvent;

public readonly record struct ResourceNodeDepletedEvent(
    ulong SimulationTick,
    EntityId NodeId,
    ResourceType Type,
    Vector2D Position) : IDomainEvent;

public readonly record struct BuildingPlacedEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    FactionId FactionId,
    string BuildingType,
    Vector2D Position) : IDomainEvent;

public readonly record struct BuildingConstructionProgressEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    float Progress,
    float MaxProgress) : IDomainEvent;

public readonly record struct BuildingCompletedEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    FactionId FactionId,
    string BuildingType,
    Vector2D Position) : IDomainEvent;

public readonly record struct ProductionStartedEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    FactionId FactionId,
    string UnitType,
    int TotalDurationTicks) : IDomainEvent;

public readonly record struct ProductionProgressEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    string UnitType,
    int CurrentTicks,
    int TotalDurationTicks) : IDomainEvent;

public readonly record struct ProductionCompletedEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    FactionId FactionId,
    string UnitType,
    EntityId ProducedUnitId) : IDomainEvent;

public readonly record struct ProductionCancelledEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    FactionId FactionId,
    string UnitType,
    ResourceCost RefundedCost) : IDomainEvent;

public readonly record struct PopulationCapacityChangedEvent(
    ulong SimulationTick,
    FactionId FactionId,
    int CurrentPop,
    int MaxPop) : IDomainEvent;

public readonly record struct BuildingRepairProgressEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    float CurrentHealth,
    float MaxHealth) : IDomainEvent;

public readonly record struct BuildingRepairedEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    FactionId FactionId,
    string BuildingType) : IDomainEvent;

public readonly record struct FarmHarvestedEvent(
    ulong SimulationTick,
    EntityId WorkerId,
    EntityId FarmId,
    int AmountHarvested,
    int RemainingFarmFood) : IDomainEvent;

public readonly record struct FarmDepletedEvent(
    ulong SimulationTick,
    EntityId FarmId,
    FactionId FactionId) : IDomainEvent;

public readonly record struct FarmReseededEvent(
    ulong SimulationTick,
    EntityId FarmId,
    FactionId FactionId,
    int RestoredFood) : IDomainEvent;

public readonly record struct IdleWorkersSelectedEvent(
    ulong SimulationTick,
    FactionId FactionId,
    EntityId[] WorkerIds) : IDomainEvent;

