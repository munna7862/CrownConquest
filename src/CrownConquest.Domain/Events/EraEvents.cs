using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;

namespace CrownConquest.Domain.Events;

public readonly record struct EraAdvancementStartedEvent(
    ulong SimulationTick,
    FactionId FactionId,
    EntityId BuildingId,
    CivilizationEra FromEra,
    CivilizationEra TargetEra,
    int DurationTicks) : IDomainEvent;

public readonly record struct EraAdvancementProgressEvent(
    ulong SimulationTick,
    FactionId FactionId,
    CivilizationEra TargetEra,
    int CurrentTicks,
    int DurationTicks) : IDomainEvent;

public readonly record struct EraAdvancementCompletedEvent(
    ulong SimulationTick,
    FactionId FactionId,
    CivilizationEra OldEra,
    CivilizationEra NewEra) : IDomainEvent;

public readonly record struct EraAdvancementCancelledEvent(
    ulong SimulationTick,
    FactionId FactionId,
    CivilizationEra TargetEra,
    ResourceCost RefundedCost) : IDomainEvent;
