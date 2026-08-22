using System;
using System.Collections.Generic;

namespace CrownConquest.Domain.Economy;

public sealed class ResearchQueueItem
{
    public TechnologyDefinition Technology { get; }
    public string TechnologyId => Technology.Id;
    public int TotalDurationTicks { get; }
    public int ProgressTicks { get; private set; }
    public ResourceCost Cost { get; }

    public bool IsCompleted => ProgressTicks >= TotalDurationTicks;
    public float ProgressNormalized => TotalDurationTicks > 0 ? (float)ProgressTicks / TotalDurationTicks : 1.0f;

    public ResearchQueueItem(
        TechnologyDefinition technology,
        int totalDurationTicks,
        ResourceCost cost)
    {
        Technology = technology;
        TotalDurationTicks = Math.Max(1, totalDurationTicks);
        ProgressTicks = 0;
        Cost = cost;
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
/// Authoritative research queue attached to buildings (e.g. Blacksmith, Town Center).
/// </summary>
public sealed class ResearchQueue
{
    private readonly List<ResearchQueueItem> _queue;
    public int MaxQueueSize { get; }

    public int Count => _queue.Count;
    public bool IsEmpty => _queue.Count == 0;
    public bool IsFull => _queue.Count >= MaxQueueSize;
    public IReadOnlyList<ResearchQueueItem> Items => _queue;

    public ResearchQueueItem? CurrentItem => _queue.Count > 0 ? _queue[0] : null;

    public ResearchQueue(int maxQueueSize = 5)
    {
        MaxQueueSize = Math.Max(1, maxQueueSize);
        _queue = new List<ResearchQueueItem>(MaxQueueSize);
    }

    public bool TryEnqueue(ResearchQueueItem item)
    {
        if (IsFull || item == null) return false;
        _queue.Add(item);
        return true;
    }

    public ResearchQueueItem? TryDequeue()
    {
        if (IsEmpty) return null;
        var item = _queue[0];
        _queue.RemoveAt(0);
        return item;
    }

    public ResearchQueueItem? CancelAt(int index)
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
