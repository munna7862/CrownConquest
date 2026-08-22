using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;

namespace CrownConquest.Domain.Events;

public readonly record struct TechnologyResearchStartedEvent(
    ulong SimulationTick,
    FactionId FactionId,
    EntityId BuildingId,
    string TechnologyId,
    int DurationTicks) : IDomainEvent;

public readonly record struct TechnologyResearchProgressEvent(
    ulong SimulationTick,
    FactionId FactionId,
    EntityId BuildingId,
    string TechnologyId,
    int CurrentTicks,
    int DurationTicks) : IDomainEvent;

public readonly record struct TechnologyResearchCompletedEvent(
    ulong SimulationTick,
    FactionId FactionId,
    EntityId BuildingId,
    string TechnologyId) : IDomainEvent;

public readonly record struct TechnologyResearchCancelledEvent(
    ulong SimulationTick,
    FactionId FactionId,
    EntityId BuildingId,
    string TechnologyId,
    ResourceCost RefundedCost) : IDomainEvent;
