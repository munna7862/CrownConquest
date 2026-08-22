using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Economy;

/// <summary>
/// Authoritative population manager enforcing housing capacities and unit limits.
/// </summary>
public sealed class PopulationManager
{
    public FactionId FactionId { get; }
    public int BaseCapacity { get; }
    public int AbsoluteMaxCap { get; }

    public int CurrentPopulation { get; private set; }
    public int CurrentMaxCapacity { get; private set; }

    public bool IsPopCapped => CurrentPopulation >= CurrentMaxCapacity;

    public PopulationManager(
        FactionId factionId,
        int baseCapacity = 5,
        int absoluteMaxCap = 200)
    {
        FactionId = factionId;
        BaseCapacity = Math.Max(0, baseCapacity);
        AbsoluteMaxCap = Math.Max(BaseCapacity, absoluteMaxCap);
        CurrentPopulation = 0;
        CurrentMaxCapacity = BaseCapacity;
    }

    public void RecalculateCapacity(
        IEnumerable<BuildingEntity> buildings,
        ulong tick,
        DomainEventBus? eventBus = null)
    {
        int capacity = BaseCapacity;

        foreach (var b in buildings)
        {
            if (b.FactionId == FactionId && b.IsConstructed && b.IsAlive)
            {
                capacity += b.PopulationProvided;
            }
        }

        int clampedCap = Math.Min(AbsoluteMaxCap, capacity);
        if (clampedCap != CurrentMaxCapacity)
        {
            CurrentMaxCapacity = clampedCap;
            eventBus?.Publish(new PopulationCapacityChangedEvent(tick, FactionId, CurrentPopulation, CurrentMaxCapacity));
        }
    }

    public void SetCurrentPopulation(
        int unitCount,
        ulong tick,
        DomainEventBus? eventBus = null)
    {
        int prevPop = CurrentPopulation;
        CurrentPopulation = Math.Max(0, unitCount);

        if (CurrentPopulation != prevPop)
        {
            eventBus?.Publish(new PopulationCapacityChangedEvent(tick, FactionId, CurrentPopulation, CurrentMaxCapacity));
        }
    }

    public bool CanTrainUnit(int populationCost = 1)
    {
        return (CurrentPopulation + populationCost) <= CurrentMaxCapacity;
    }
}
