using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class ProductionQueueTests
{
    [Fact]
    public void ProductionQueue_EnqueueAndProgressTicks()
    {
        // TC-S02-005: Enqueue production items and advance ticks
        var queue = new ProductionQueue(maxQueueSize: 3);
        Assert.True(queue.IsEmpty);
        Assert.False(queue.IsFull);

        var item1 = new ProductionQueueItem("villager", totalDurationTicks: 10, cost: new ResourceCost(Food: 50));
        var item2 = new ProductionQueueItem("swordsman", totalDurationTicks: 20, cost: new ResourceCost(Food: 60, Iron: 20));

        Assert.True(queue.TryEnqueue(item1));
        Assert.True(queue.TryEnqueue(item2));
        Assert.Equal(2, queue.Count);
        Assert.Same(item1, queue.CurrentItem);

        // Advance 5 ticks
        queue.CurrentItem!.AdvanceTicks(5);
        Assert.False(queue.CurrentItem.IsCompleted);
        Assert.Equal(0.5f, queue.CurrentItem.ProgressNormalized);

        // Advance 5 more ticks -> complete
        queue.CurrentItem.AdvanceTicks(5);
        Assert.True(queue.CurrentItem.IsCompleted);

        var completed = queue.TryDequeue();
        Assert.Same(item1, completed);
        Assert.Same(item2, queue.CurrentItem);
    }

    [Fact]
    public void PopulationManager_CapacityCalculation()
    {
        // TC-S02-006: Calculate cumulative capacity based on Town Center (+10) and Houses (+5)
        var factionId = new FactionId(1);
        var manager = new PopulationManager(factionId, baseCapacity: 5, absoluteMaxCap: 200);

        Assert.Equal(5, manager.CurrentMaxCapacity);

        var buildings = new List<BuildingEntity>
        {
            new(new EntityId(1), factionId, "town_center", new Vector2D(10f, 10f), new Vector2D(4f, 4f), populationProvided: 10, startsConstructed: true),
            new(new EntityId(2), factionId, "house", new Vector2D(20f, 10f), new Vector2D(2f, 2f), populationProvided: 5, startsConstructed: true),
            new(new EntityId(3), factionId, "house", new Vector2D(25f, 10f), new Vector2D(2f, 2f), populationProvided: 5, startsConstructed: false), // unconstructed: does not provide pop
            new(new EntityId(4), new FactionId(2), "house", new Vector2D(50f, 50f), new Vector2D(2f, 2f), populationProvided: 5, startsConstructed: true) // enemy house
        };

        manager.RecalculateCapacity(buildings, 1UL);

        // Base 5 + Town Center 10 + House 5 = 20
        Assert.Equal(20, manager.CurrentMaxCapacity);

        manager.SetCurrentPopulation(15, 1UL);
        Assert.False(manager.IsPopCapped);
        Assert.True(manager.CanTrainUnit(1));

        manager.SetCurrentPopulation(20, 2UL);
        Assert.True(manager.IsPopCapped);
        Assert.False(manager.CanTrainUnit(1));
    }
}
