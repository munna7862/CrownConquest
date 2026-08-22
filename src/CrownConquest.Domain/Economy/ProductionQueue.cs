using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Economy;

public sealed class ProductionQueueItem
{
    public string UnitType { get; }
    public int TotalDurationTicks { get; }
    public int ProgressTicks { get; private set; }
    public ResourceCost Cost { get; }
    public int PopulationCost { get; }

    public bool IsCompleted => ProgressTicks >= TotalDurationTicks;
    public float ProgressNormalized => TotalDurationTicks > 0 ? (float)ProgressTicks / TotalDurationTicks : 1.0f;

    public ProductionQueueItem(
        string unitType,
        int totalDurationTicks,
        ResourceCost cost,
        int populationCost = 1)
    {
        UnitType = unitType;
        TotalDurationTicks = Math.Max(1, totalDurationTicks);
        ProgressTicks = 0;
        Cost = cost;
        PopulationCost = populationCost;
    }

    public void AdvanceTicks(int ticks = 1)
    {
        if (ticks > 0)
        {
            ProgressTicks = Math.Min(TotalDurationTicks, ProgressTicks + ticks);
        }
    }
}

/// <summary>
/// Authoritative production queue attached to production buildings (e.g. Town Center, Barracks).
/// </summary>
public sealed class ProductionQueue
{
    private readonly List<ProductionQueueItem> _queue;
    public int MaxQueueSize { get; }

    public int Count => _queue.Count;
    public bool IsEmpty => _queue.Count == 0;
    public bool IsFull => _queue.Count >= MaxQueueSize;
    public IReadOnlyList<ProductionQueueItem> Items => _queue;

    public ProductionQueueItem? CurrentItem => _queue.Count > 0 ? _queue[0] : null;

    public ProductionQueue(int maxQueueSize = 5)
    {
        MaxQueueSize = Math.Max(1, maxQueueSize);
        _queue = new List<ProductionQueueItem>(MaxQueueSize);
    }

    public bool TryEnqueue(ProductionQueueItem item)
    {
        if (IsFull || item == null) return false;
        _queue.Add(item);
        return true;
    }

    public ProductionQueueItem? TryDequeue()
    {
        if (IsEmpty) return null;
        var item = _queue[0];
        _queue.RemoveAt(0);
        return item;
    }

    public ProductionQueueItem? CancelAt(int index)
    {
        if (index < 0 || index >= _queue.Count) return null;
        var item = _queue[index];
        _queue.RemoveAt(index);
        return item;
    }

    public void Clear()
    {
        _queue.Clear();
    }
}
