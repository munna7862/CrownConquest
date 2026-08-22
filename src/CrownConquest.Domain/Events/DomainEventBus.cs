using System.Collections.Concurrent;

namespace CrownConquest.Domain.Events;

/// <summary>
/// Delegate definition for strongly-typed domain event handlers.
/// Passing readonly ref for struct events or in-memory instances guarantees zero boxing.
/// </summary>
public delegate void DomainEventHandler<TEvent>(in TEvent domainEvent) where TEvent : struct, IDomainEvent;

/// <summary>
/// High-throughput, zero-allocation Domain Event Bus.
/// Allows presentation nodes, audio controllers, and QA harnesses to observe simulation state
/// changes without introducing coupling to simulation entities.
/// </summary>
public sealed class DomainEventBus
{
    private readonly ConcurrentDictionary<Type, object> _handlers = new();

    private sealed class HandlerList<TEvent> where TEvent : struct, IDomainEvent
    {
        private readonly List<DomainEventHandler<TEvent>> _list = new();
        private readonly object _lock = new();

        public void Add(DomainEventHandler<TEvent> handler)
        {
            lock (_lock)
            {
                if (!_list.Contains(handler))
                {
                    _list.Add(handler);
                }
            }
        }

        public void Remove(DomainEventHandler<TEvent> handler)
        {
            lock (_lock)
            {
                _list.Remove(handler);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _list.Clear();
            }
        }

        public void Invoke(in TEvent domainEvent)
        {
            // Snapshot or direct index-based iteration without IEnumerator allocation
            lock (_lock)
            {
                int count = _list.Count;
                for (int i = 0; i < count; i++)
                {
                    _list[i](in domainEvent);
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _list.Count;
                }
            }
        }
    }

    /// <summary>
    /// Subscribe a strongly-typed callback to receive domain events of type TEvent.
    /// </summary>
    public void Subscribe<TEvent>(DomainEventHandler<TEvent> handler) where TEvent : struct, IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        var handlerList = (HandlerList<TEvent>)_handlers.GetOrAdd(
            typeof(TEvent),
            _ => new HandlerList<TEvent>());
        handlerList.Add(handler);
    }

    /// <summary>
    /// Unsubscribe a previously registered callback.
    /// </summary>
    public void Unsubscribe<TEvent>(DomainEventHandler<TEvent> handler) where TEvent : struct, IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (_handlers.TryGetValue(typeof(TEvent), out var obj) && obj is HandlerList<TEvent> handlerList)
        {
            handlerList.Remove(handler);
        }
    }

    /// <summary>
    /// Publish a domain event to all registered subscribers with zero heap allocations.
    /// </summary>
    public void Publish<TEvent>(in TEvent domainEvent) where TEvent : struct, IDomainEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var obj) && obj is HandlerList<TEvent> handlerList)
        {
            handlerList.Invoke(in domainEvent);
        }
    }

    /// <summary>
    /// Clear all registered handlers across all event types.
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
    }

    /// <summary>
    /// Get count of subscribers for a specific event type.
    /// </summary>
    public int GetSubscriberCount<TEvent>() where TEvent : struct, IDomainEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var obj) && obj is HandlerList<TEvent> handlerList)
        {
            return handlerList.Count;
        }
        return 0;
    }
}
