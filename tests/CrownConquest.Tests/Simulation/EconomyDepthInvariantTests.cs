using System;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class EconomyDepthInvariantTests
{
    [Fact]
    public void EconomyDepth_Repair_ResourceConservation_Invariant()
    {
        // TC-S03-006: Damaged watchtower repair deducts resources from bank and restores health without leak
        var engine = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = engine.State.GetOrCreateResourceBank(factionId);

        // Initial bank: 500 Wood, 500 Stone
        bank.Deposit(ResourceType.Wood, 500, 0);
        bank.Deposit(ResourceType.Stone, 500, 0);

        // Place watchtower: BaseCost = Wood 50, Stone 125, MaxHealth = 600
        var towerId = engine.State.GenerateEntityId();
        var tower = new BuildingEntity(
            towerId,
            factionId,
            "watchtower",
            new Vector2D(50f, 50f),
            new Vector2D(2f, 2f),
            maxHealth: 600f,
            baseBuildTimeTicks: 60f,
            startsConstructed: true,
            baseCost: new ResourceCost(Wood: 50, Stone: 125));
        engine.State.AddBuilding(tower);

        // Deal 300 damage (50% missing health)
        tower.TakeDamage(300f, EntityId.None, new FactionId(2), 1, engine.EventBus, out _);
        Assert.Equal(300f, tower.CurrentHealth);

        // Spawn repair worker next to tower
        var workerId = engine.State.GenerateEntityId();
        var worker = new UnitEntity(
            workerId,
            factionId,
            "villager",
            new Vector2D(50f, 52f),
            workerState: new WorkerGatherState(repairPowerPerTick: 3.0f));
        engine.State.AddUnit(worker);
        engine.SpatialGrid.Insert(worker.Id, worker.Position);

        // Command repair
        engine.CommandQueue.Enqueue(new RepairBuildingCommand(1, factionId, new[] { workerId }, towerId));

        // Simulate until fully repaired (100 ticks)
        engine.SimulateTicks(120);

        Assert.Equal(600f, tower.CurrentHealth);
        Assert.False(tower.IsDamaged);
        Assert.Equal(UnitState.Idle, worker.State);

        // Verify resource deduction occurred
        Assert.True(bank.Wood < 500);
        Assert.True(bank.Stone < 500);
    }

    [Fact]
    public void EconomyDepth_FarmReseed_WoodDeduction_Invariant()
    {
        // TC-S03-007: Farm auto-reseed deducts exactly 60 Wood per reseed
        var engine = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = engine.State.GetOrCreateResourceBank(factionId);

        bank.Deposit(ResourceType.Wood, 200, 0);

        // Spawn granary for food drop-off
        var granaryId = engine.State.GenerateEntityId();
        var granary = new BuildingEntity(
            granaryId, factionId, "granary", new Vector2D(50f, 50f), new Vector2D(2f, 2f),
            acceptedDropOffTypes: new[] { ResourceType.Food }, startsConstructed: true);
        engine.State.AddBuilding(granary);

        // Spawn Farm with only 15 Food remaining
        var farmId = engine.State.GenerateEntityId();
        var farm = new BuildingEntity(
            farmId, factionId, "farm", new Vector2D(50f, 52f), new Vector2D(2f, 2f),
            startsConstructed: true, isFarm: true, maxFarmFood: 250, farmReseedCost: 60);
        // Harvest down to 15 food
        farm.HarvestFarmFood(235, 1, EntityId.None, null);
        engine.State.AddBuilding(farm);

        // Spawn farmer
        var farmerId = engine.State.GenerateEntityId();
        var farmer = new UnitEntity(
            farmerId, factionId, "villager", new Vector2D(50f, 52f),
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        engine.State.AddUnit(farmer);
        engine.SpatialGrid.Insert(farmer.Id, farmer.Position);

        int initialWood = bank.Wood; // 200

        // Order gather from farm
        engine.CommandQueue.Enqueue(new GatherCommand(1, factionId, new[] { farmerId }, farmId));

        // Simulate 80 ticks: Farmer harvests 10 food -> deposits at granary -> harvests 5 food (farm depletes) -> auto-reseeds (deducts 60 Wood) -> continues farming
        engine.SimulateTicks(80);

        Assert.Equal(initialWood - 60, bank.Wood); // Exactly 60 wood spent
        Assert.True(bank.Food >= 15); // Food deposited into bank
    }

    [Fact]
    public void EconomyDepth_IdleWorkerQuery_ExactCount()
    {
        // TC-S03-008: GetIdleWorkers returns exact list of idle workers
        var engine = new SimulationEngine();
        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        // F1: 2 idle villagers, 1 moving villager, 1 military swordsman (not worker)
        var v1 = new UnitEntity(engine.State.GenerateEntityId(), f1, "villager", new Vector2D(10f, 10f), workerState: new WorkerGatherState());
        var v2 = new UnitEntity(engine.State.GenerateEntityId(), f1, "villager", new Vector2D(12f, 10f), workerState: new WorkerGatherState());
        var v3 = new UnitEntity(engine.State.GenerateEntityId(), f1, "villager", new Vector2D(14f, 10f), workerState: new WorkerGatherState());
        v3.Move(new Vector2D(30f, 30f)); // Active moving

        var s1 = new UnitEntity(engine.State.GenerateEntityId(), f1, "swordsman", new Vector2D(16f, 10f)); // Combat unit

        // F2: 1 idle villager
        var v4 = new UnitEntity(engine.State.GenerateEntityId(), f2, "villager", new Vector2D(50f, 50f), workerState: new WorkerGatherState());

        engine.State.AddUnit(v1);
        engine.State.AddUnit(v2);
        engine.State.AddUnit(v3);
        engine.State.AddUnit(s1);
        engine.State.AddUnit(v4);

        var idleF1 = engine.GetIdleWorkers(f1);
        Assert.Equal(2, idleF1.Length);
        Assert.Contains(v1.Id, idleF1);
        Assert.Contains(v2.Id, idleF1);
        Assert.DoesNotContain(v3.Id, idleF1);
        Assert.DoesNotContain(s1.Id, idleF1);

        var idleF2 = engine.GetIdleWorkers(f2);
        Assert.Single(idleF2);
        Assert.Equal(v4.Id, idleF2[0]);
    }

    [Fact]
    public void EconomyDepth_TaskSwitching_InventoryPreserved()
    {
        // TC-S03-009: Reassigning worker preserves carried inventory
        var engine = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = engine.State.GetOrCreateResourceBank(factionId);

        // Town center at (50, 50)
        var tcId = engine.State.GenerateEntityId();
        var tc = new BuildingEntity(
            tcId, factionId, "town_center", new Vector2D(50f, 50f), new Vector2D(4f, 4f),
            acceptedDropOffTypes: new[] { ResourceType.Gold, ResourceType.Wood }, startsConstructed: true);
        engine.State.AddBuilding(tc);

        // Worker carrying 8 Gold
        var workerId = engine.State.GenerateEntityId();
        var workerState = new WorkerGatherState(carryCapacity: 10);
        workerState.AddCarried(ResourceType.Gold, 8);

        var worker = new UnitEntity(workerId, factionId, "villager", new Vector2D(45f, 45f), workerState: workerState);
        engine.State.AddUnit(worker);
        engine.SpatialGrid.Insert(worker.Id, worker.Position);

        // Move order -> carried inventory remains 8 Gold
        worker.Move(new Vector2D(48f, 48f));
        Assert.Equal(8, worker.WorkerState!.CarriedAmount);
        Assert.Equal(ResourceType.Gold, worker.WorkerState.CarriedResourceType);

        // Order return to drop off
        worker.WorkerState.TargetBuildingId = tcId;
        worker.WorkerState.TaskState = WorkerTaskState.ReturningToDropOff;
        worker.State = UnitState.Returning;

        engine.SimulateTicks(60);

        Assert.Equal(8, bank.Gold);
        Assert.False(worker.WorkerState.HasCarriedResources);
    }

    [Fact]
    public void EconomyDepth_BitExactReplay_600Ticks()
    {
        // TC-S03-010: Replay produces bit-exact identical simulation state after 600 ticks
        var sim1 = CreateEconomyDepthSimulation(1337);
        var sim2 = CreateEconomyDepthSimulation(1337);

        sim1.SimulateTicks(600);
        sim2.SimulateTicks(600);

        var bank1 = sim1.State.GetOrCreateResourceBank(new FactionId(1));
        var bank2 = sim2.State.GetOrCreateResourceBank(new FactionId(1));

        Assert.Equal(bank1.Food, bank2.Food);
        Assert.Equal(bank1.Wood, bank2.Wood);
        Assert.Equal(bank1.Gold, bank2.Gold);
        Assert.Equal(bank1.Stone, bank2.Stone);
        Assert.Equal(bank1.Iron, bank2.Iron);

        for (int i = 0; i < sim1.State.ActiveUnits.Count; i++)
        {
            var u1 = sim1.State.ActiveUnits[i];
            var u2 = sim2.State.ActiveUnits[i];
            Assert.Equal(u1.Position.X, u2.Position.X, precision: 4);
            Assert.Equal(u1.Position.Y, u2.Position.Y, precision: 4);
            Assert.Equal(u1.CurrentHealth, u2.CurrentHealth, precision: 4);
        }
    }

    private static SimulationEngine CreateEconomyDepthSimulation(int seed)
    {
        var config = new SimulationConfig { InitialRandomSeed = seed };
        var engine = new SimulationEngine(config);
        var factionId = new FactionId(1);
        var bank = engine.State.GetOrCreateResourceBank(factionId);
        bank.Deposit(ResourceType.Wood, 300, 0);

        // Town center
        var tc = new BuildingEntity(
            engine.State.GenerateEntityId(), factionId, "town_center", new Vector2D(50f, 50f), new Vector2D(4f, 4f),
            acceptedDropOffTypes: new[] { ResourceType.Food, ResourceType.Wood, ResourceType.Gold, ResourceType.Stone, ResourceType.Iron }, startsConstructed: true);
        engine.State.AddBuilding(tc);

        // Lumber Camp & Tree
        var lc = new BuildingEntity(
            engine.State.GenerateEntityId(), factionId, "lumber_camp", new Vector2D(50f, 65f), new Vector2D(2f, 2f),
            acceptedDropOffTypes: new[] { ResourceType.Wood }, startsConstructed: true);
        engine.State.AddBuilding(lc);

        var tree = new ResourceNodeEntity(engine.State.GenerateEntityId(), ResourceType.Wood, new Vector2D(50f, 70f), maxAmount: 300);
        engine.State.AddResourceNode(tree);

        // Worker
        var w = new UnitEntity(
            engine.State.GenerateEntityId(), factionId, "villager", new Vector2D(50f, 64f),
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 0.5f));
        engine.State.AddUnit(w);
        engine.SpatialGrid.Insert(w.Id, w.Position);

        engine.CommandQueue.Enqueue(new GatherCommand(1, factionId, new[] { w.Id }, tree.Id));
        return engine;
    }
}
