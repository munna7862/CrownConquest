using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Events;

/// <summary>
/// Emitted when a building's construction progress changes visually.
/// </summary>
public readonly record struct BuildingVisualProgressEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    FactionId FactionId,
    string BuildingType,
    float Progress,
    float MaxProgress) : IDomainEvent;

/// <summary>
/// Emitted when a building takes damage and its visual damage state changes.
/// </summary>
public readonly record struct BuildingDamageVisualEvent(
    ulong SimulationTick,
    EntityId BuildingId,
    float HealthPercentage) : IDomainEvent;
