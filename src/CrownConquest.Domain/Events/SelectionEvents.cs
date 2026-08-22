using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Events;

/// <summary>
/// Emitted when one or more units are selected by a player.
/// </summary>
public readonly record struct UnitsSelectedEvent(
    ulong SimulationTick,
    FactionId FactionId,
    EntityId[] SelectedUnitIds) : IDomainEvent;

/// <summary>
/// Emitted when the unit selection is cleared for a player.
/// </summary>
public readonly record struct SelectionClearedEvent(
    ulong SimulationTick,
    FactionId FactionId) : IDomainEvent;
