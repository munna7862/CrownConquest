using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class FarmMathTests
{
    [Fact]
    public void Farm_FoodCapacityAndHarvest_Deduction_Accurate()
    {
        // TC-S03-001
        var eventBus = new DomainEventBus();
        var farmId = new EntityId(101);
        var factionId = new FactionId(1);
        var workerId = new EntityId(1);

        var farm = new BuildingEntity(
            farmId,
            factionId,
            "farm",
            new Vector2D(10f, 10f),
            new Vector2D(2f, 2f),
            maxHealth: 200f,
            baseBuildTimeTicks: 30f,
            startsConstructed: true,
            isFarm: true,
            maxFarmFood: 250,
            farmReseedCost: 60);

        Assert.True(farm.IsFarm);
        Assert.Equal(250, farm.FarmFoodRemaining);
        Assert.False(farm.IsFarmDepleted);

        // Harvest 10 food
        int harvested = farm.HarvestFarmFood(10, 1, workerId, eventBus);
        Assert.Equal(10, harvested);
        Assert.Equal(240, farm.FarmFoodRemaining);

        // Harvest 240 food (depletes farm)
        bool depletedFired = false;
        eventBus.Subscribe<FarmDepletedEvent>((in FarmDepletedEvent e) =>
        {
            if (e.FarmId == farmId) depletedFired = true;
        });

        int remainingHarvest = farm.HarvestFarmFood(300, 2, workerId, eventBus);
        Assert.Equal(240, remainingHarvest);
        Assert.Equal(0, farm.FarmFoodRemaining);
        Assert.True(farm.IsFarmDepleted);
        Assert.True(depletedFired);
    }

    [Fact]
    public void Farm_Reseeding_ReplenishesFood_FullCapacity()
    {
        // TC-S03-002
        var eventBus = new DomainEventBus();
        var farmId = new EntityId(102);
        var factionId = new FactionId(1);
        var workerId = new EntityId(1);

        var farm = new BuildingEntity(
            farmId,
            factionId,
            "farm",
            new Vector2D(10f, 10f),
            new Vector2D(2f, 2f),
            startsConstructed: true,
            isFarm: true,
            maxFarmFood: 250,
            farmReseedCost: 60);

        // Deplete farm
        farm.HarvestFarmFood(250, 1, workerId, eventBus);
        Assert.True(farm.IsFarmDepleted);

        // Reseed farm
        bool reseededFired = false;
        eventBus.Subscribe<FarmReseededEvent>((in FarmReseededEvent e) =>
        {
            if (e.FarmId == farmId && e.RestoredFood == 250) reseededFired = true;
        });

        farm.ReseedFarm(2, eventBus);
        Assert.False(farm.IsFarmDepleted);
        Assert.Equal(250, farm.FarmFoodRemaining);
        Assert.True(reseededFired);
    }

    [Fact]
    public void SpecializedCamps_AcceptedDropOffFiltering_Correct()
    {
        // TC-S03-004
        var lumberCamp = new BuildingEntity(
            new EntityId(1), new FactionId(1), "lumber_camp", Vector2D.Zero, new Vector2D(2f, 2f),
            acceptedDropOffTypes: new[] { ResourceType.Wood }, startsConstructed: true);

        var miningCamp = new BuildingEntity(
            new EntityId(2), new FactionId(1), "mining_camp", Vector2D.Zero, new Vector2D(2f, 2f),
            acceptedDropOffTypes: new[] { ResourceType.Gold, ResourceType.Iron }, startsConstructed: true);

        var stoneCamp = new BuildingEntity(
            new EntityId(3), new FactionId(1), "stone_quarry_camp", Vector2D.Zero, new Vector2D(2f, 2f),
            acceptedDropOffTypes: new[] { ResourceType.Stone }, startsConstructed: true);

        var granary = new BuildingEntity(
            new EntityId(4), new FactionId(1), "granary", Vector2D.Zero, new Vector2D(2f, 2f),
            acceptedDropOffTypes: new[] { ResourceType.Food }, startsConstructed: true);

        // Lumber camp only accepts Wood
        Assert.True(lumberCamp.AcceptsDropOff(ResourceType.Wood));
        Assert.False(lumberCamp.AcceptsDropOff(ResourceType.Food));
        Assert.False(lumberCamp.AcceptsDropOff(ResourceType.Gold));
        Assert.False(lumberCamp.AcceptsDropOff(ResourceType.Stone));
        Assert.False(lumberCamp.AcceptsDropOff(ResourceType.Iron));

        // Mining camp accepts Gold and Iron
        Assert.True(miningCamp.AcceptsDropOff(ResourceType.Gold));
        Assert.True(miningCamp.AcceptsDropOff(ResourceType.Iron));
        Assert.False(miningCamp.AcceptsDropOff(ResourceType.Wood));
        Assert.False(miningCamp.AcceptsDropOff(ResourceType.Food));
        Assert.False(miningCamp.AcceptsDropOff(ResourceType.Stone));

        // Stone quarry camp accepts Stone only
        Assert.True(stoneCamp.AcceptsDropOff(ResourceType.Stone));
        Assert.False(stoneCamp.AcceptsDropOff(ResourceType.Wood));
        Assert.False(stoneCamp.AcceptsDropOff(ResourceType.Gold));

        // Granary accepts Food only
        Assert.True(granary.AcceptsDropOff(ResourceType.Food));
        Assert.False(granary.AcceptsDropOff(ResourceType.Wood));
    }
}
