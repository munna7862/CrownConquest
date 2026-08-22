namespace CrownConquest.Domain.Events;

/// <summary>
/// Marker interface for all strongly-typed domain events.
/// Events represent immutable state transitions that occurred in the simulation.
/// </summary>
public interface IDomainEvent
{
    ulong SimulationTick { get; }
}
