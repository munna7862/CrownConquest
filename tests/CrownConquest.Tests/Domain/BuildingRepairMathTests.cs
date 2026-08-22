using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class BuildingRepairMathTests
{
    [Fact]
    public void BuildingRepair_HealthRestoration_AndEvents()
    {
        // TC-S03-003
        var eventBus = new DomainEventBus();
        var buildingId = new EntityId(201);
        var factionId = new FactionId(1);

        var building = new BuildingEntity(
            buildingId,
            factionId,
            "watchtower",
            new Vector2D(20f, 20f),
            new Vector2D(2f, 2f),
            maxHealth: 600f,
            baseBuildTimeTicks: 60f,
            startsConstructed: true,
            baseCost: new ResourceCost(Wood: 50, Stone: 125));

        Assert.False(building.IsDamaged);
        Assert.Equal(600f, building.CurrentHealth);

        // Deal 300 damage
        building.TakeDamage(300f, EntityId.None, new FactionId(2), 1, eventBus, out _);
        Assert.True(building.IsDamaged);
        Assert.Equal(300f, building.CurrentHealth);

        // Repair 100 HP
        building.Repair(100f, 2, eventBus, out bool fullyRepaired1);
        Assert.False(fullyRepaired1);
        Assert.Equal(400f, building.CurrentHealth);
        Assert.True(building.IsDamaged);

        // Repair 250 HP (exceeds max HP, clamps at 600)
        bool repairedEventFired = false;
        eventBus.Subscribe<BuildingRepairedEvent>((in BuildingRepairedEvent e) =>
        {
            if (e.BuildingId == buildingId) repairedEventFired = true;
        });

        building.Repair(250f, 3, eventBus, out bool fullyRepaired2);
        Assert.True(fullyRepaired2);
        Assert.Equal(600f, building.CurrentHealth);
        Assert.False(building.IsDamaged);
        Assert.True(repairedEventFired);
    }

    [Fact]
    public void WorkerGatherState_RepairAndFarmStates_Transitions()
    {
        // TC-S03-005
        var workerState = new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 0.5f, repairPowerPerTick: 2.0f);

        Assert.Equal(WorkerTaskState.None, workerState.TaskState);
        Assert.Equal(2.0f, workerState.RepairPowerPerTick);

        // Set repair target
        workerState.TargetBuildingId = new EntityId(55);
        workerState.TaskState = WorkerTaskState.MovingToRepair;
        Assert.Equal(WorkerTaskState.MovingToRepair, workerState.TaskState);

        workerState.TaskState = WorkerTaskState.Repairing;
        Assert.Equal(WorkerTaskState.Repairing, workerState.TaskState);

        // Add carried resource during farm harvesting
        workerState.AddCarried(ResourceType.Food, 10);
        Assert.True(workerState.HasCarriedResources);
        Assert.True(workerState.IsInventoryFull);
        Assert.Equal(ResourceType.Food, workerState.CarriedResourceType);
        Assert.Equal(10, workerState.CarriedAmount);

        // Switching task does not discard carried resources
        workerState.TaskState = WorkerTaskState.MovingToRepair;
        Assert.True(workerState.HasCarriedResources);
        Assert.Equal(10, workerState.CarriedAmount);

        var dumped = workerState.EmptyInventory();
        Assert.NotNull(dumped);
        Assert.Equal(ResourceType.Food, dumped.Value.Type);
        Assert.Equal(10, dumped.Value.Amount);
        Assert.False(workerState.HasCarriedResources);
    }
}
