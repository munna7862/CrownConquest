using System;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class EconomyDepthIntegrationTests
{
    [Fact]
    public void Integration_LumberCamp_DropOffRouting_UsesNearestCamp()
    {
        // TC-S03-011: Lumberjack drops off at nearby Lumber Camp instead of distant Town Center
        var engine = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = engine.State.GetOrCreateResourceBank(factionId);

        // Town Center at (10, 10)
        var tc = new BuildingEntity(
            engine.State.GenerateEntityId(), factionId, "town_center", new Vector2D(10f, 10f), new Vector2D(4f, 4f),
            acceptedDropOffTypes: new[] { ResourceType.Wood }, startsConstructed: true);
        engine.State.AddBuilding(tc);

        // Lumber Camp at (50, 50)
        var lcId = engine.State.GenerateEntityId();
        var lc = new BuildingEntity(
            lcId, factionId, "lumber_camp", new Vector2D(50f, 50f), new Vector2D(2f, 2f),
            acceptedDropOffTypes: new[] { ResourceType.Wood }, startsConstructed: true);
        engine.State.AddBuilding(lc);

        // Tree at (52, 50)
        var tree = new ResourceNodeEntity(engine.State.GenerateEntityId(), ResourceType.Wood, new Vector2D(52f, 50f), maxAmount: 300, harvestRadius: 1.8f);
        engine.State.AddResourceNode(tree);

        // Lumberjack at (51, 50)
        var worker = new UnitEntity(
            engine.State.GenerateEntityId(), factionId, "villager", new Vector2D(51f, 50f),
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        engine.State.AddUnit(worker);
        engine.SpatialGrid.Insert(worker.Id, worker.Position);

        // Order gather
        engine.CommandQueue.Enqueue(new GatherCommand(1, factionId, new[] { worker.Id }, tree.Id));

        // Simulate 40 ticks: Worker gathers wood -> returns to Lumber Camp (NOT Town Center) -> deposits
        engine.SimulateTicks(40);

        Assert.True(bank.Wood >= 10);
        // Distance to Lumber Camp should be very small
        Assert.True(worker.Position.DistanceTo(lc.Position) < 5.0f);
    }

    [Fact]
    public void Integration_MiningCamp_DualResourceRouting_GoldAndIron()
    {
        // TC-S03-012: Both Gold miner and Iron miner deposit at the shared Mining Camp
        var engine = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = engine.State.GetOrCreateResourceBank(factionId);

        // Mining Camp at (50, 50) accepting Gold & Iron
        var mc = new BuildingEntity(
            engine.State.GenerateEntityId(), factionId, "mining_camp", new Vector2D(50f, 50f), new Vector2D(2f, 2f),
            acceptedDropOffTypes: new[] { ResourceType.Gold, ResourceType.Iron }, startsConstructed: true);
        engine.State.AddBuilding(mc);

        // Gold mine at (52, 48) and Iron deposit at (52, 52)
        var goldNode = new ResourceNodeEntity(engine.State.GenerateEntityId(), ResourceType.Gold, new Vector2D(52f, 48f), maxAmount: 800, harvestRadius: 2.0f);
        var ironNode = new ResourceNodeEntity(engine.State.GenerateEntityId(), ResourceType.Iron, new Vector2D(52f, 52f), maxAmount: 500, harvestRadius: 2.0f);
        engine.State.AddResourceNode(goldNode);
        engine.State.AddResourceNode(ironNode);

        // Workers
        var goldWorker = new UnitEntity(
            engine.State.GenerateEntityId(), factionId, "villager", new Vector2D(51f, 48f),
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        var ironWorker = new UnitEntity(
            engine.State.GenerateEntityId(), factionId, "villager", new Vector2D(51f, 52f),
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        engine.State.AddUnit(goldWorker);
        engine.State.AddUnit(ironWorker);
        engine.SpatialGrid.Insert(goldWorker.Id, goldWorker.Position);
        engine.SpatialGrid.Insert(ironWorker.Id, ironWorker.Position);

        engine.CommandQueue.Enqueue(new GatherCommand(1, factionId, new[] { goldWorker.Id }, goldNode.Id));
        engine.CommandQueue.Enqueue(new GatherCommand(1, factionId, new[] { ironWorker.Id }, ironNode.Id));

        engine.SimulateTicks(40);

        Assert.True(bank.Gold >= 10);
        Assert.True(bank.Iron >= 10);
    }

    [Fact]
    public void Integration_Granary_FarmAndBerryDropOff_IncreasesStockpile()
    {
        // TC-S03-013: Farmer and Berry gatherer deposit food at Granary
        var engine = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = engine.State.GetOrCreateResourceBank(factionId);

        var granary = new BuildingEntity(
            engine.State.GenerateEntityId(), factionId, "granary", new Vector2D(50f, 50f), new Vector2D(2f, 2f),
            acceptedDropOffTypes: new[] { ResourceType.Food }, startsConstructed: true);
        engine.State.AddBuilding(granary);

        var farm = new BuildingEntity(
            engine.State.GenerateEntityId(), factionId, "farm", new Vector2D(48f, 50f), new Vector2D(2f, 2f),
            startsConstructed: true, isFarm: true, maxFarmFood: 250);
        engine.State.AddBuilding(farm);

        var berry = new ResourceNodeEntity(engine.State.GenerateEntityId(), ResourceType.Food, new Vector2D(52f, 50f), maxAmount: 250, harvestRadius: 1.8f);
        engine.State.AddResourceNode(berry);

        var w1 = new UnitEntity(engine.State.GenerateEntityId(), factionId, "villager", new Vector2D(48f, 50f), workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        var w2 = new UnitEntity(engine.State.GenerateEntityId(), factionId, "villager", new Vector2D(52f, 50f), workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        engine.State.AddUnit(w1);
        engine.State.AddUnit(w2);
        engine.SpatialGrid.Insert(w1.Id, w1.Position);
        engine.SpatialGrid.Insert(w2.Id, w2.Position);

        engine.CommandQueue.Enqueue(new GatherCommand(1, factionId, new[] { w1.Id }, farm.Id));
        engine.CommandQueue.Enqueue(new GatherCommand(1, factionId, new[] { w2.Id }, berry.Id));

        engine.SimulateTicks(40);

        Assert.True(bank.Food >= 20); // Food deposited from both sources
    }

    [Fact]
    public void Integration_BuildingRepair_MultiWorker_AcceleratesRepair()
    {
        // TC-S03-014: 3 workers repair damaged building 3x faster
        var engine = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = engine.State.GetOrCreateResourceBank(factionId);
        bank.Deposit(ResourceType.Wood, 500, 0);

        var building = new BuildingEntity(
            engine.State.GenerateEntityId(), factionId, "barracks", new Vector2D(50f, 50f), new Vector2D(3f, 3f),
            maxHealth: 800f, startsConstructed: true, baseCost: new ResourceCost(Wood: 150));
        building.TakeDamage(600f, EntityId.None, new FactionId(2), 1, null, out _);
        engine.State.AddBuilding(building);
        Assert.Equal(200f, building.CurrentHealth);

        var w1 = new UnitEntity(engine.State.GenerateEntityId(), factionId, "villager", new Vector2D(49f, 50f), workerState: new WorkerGatherState(repairPowerPerTick: 2.0f));
        var w2 = new UnitEntity(engine.State.GenerateEntityId(), factionId, "villager", new Vector2D(51f, 50f), workerState: new WorkerGatherState(repairPowerPerTick: 2.0f));
        var w3 = new UnitEntity(engine.State.GenerateEntityId(), factionId, "villager", new Vector2D(50f, 49f), workerState: new WorkerGatherState(repairPowerPerTick: 2.0f));
        engine.State.AddUnit(w1);
        engine.State.AddUnit(w2);
        engine.State.AddUnit(w3);
        engine.SpatialGrid.Insert(w1.Id, w1.Position);
        engine.SpatialGrid.Insert(w2.Id, w2.Position);
        engine.SpatialGrid.Insert(w3.Id, w3.Position);

        // Assign all 3 workers to repair (6.0 HP / tick combined)
        engine.CommandQueue.Enqueue(new RepairBuildingCommand(1, factionId, new[] { w1.Id, w2.Id, w3.Id }, building.Id));

        // In 100 ticks, 600 missing HP should be fully restored
        engine.SimulateTicks(105);

        Assert.Equal(800f, building.CurrentHealth);
        Assert.False(building.IsDamaged);
    }

    [Fact]
    public void Integration_FarmDepletionAndAutoReseed_Seamless()
    {
        // TC-S03-015: Farmer harvests farm, farm exhausts, auto-reseeds with wood, farmer continues
        var engine = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = engine.State.GetOrCreateResourceBank(factionId);
        bank.Deposit(ResourceType.Wood, 100, 0);

        var granary = new BuildingEntity(
            engine.State.GenerateEntityId(), factionId, "granary", new Vector2D(50f, 50f), new Vector2D(2f, 2f),
            acceptedDropOffTypes: new[] { ResourceType.Food }, startsConstructed: true);
        engine.State.AddBuilding(granary);

        var farm = new BuildingEntity(
            engine.State.GenerateEntityId(), factionId, "farm", new Vector2D(50f, 52f), new Vector2D(2f, 2f),
            startsConstructed: true, isFarm: true, maxFarmFood: 250, farmReseedCost: 60);
        farm.HarvestFarmFood(245, 1, EntityId.None, null); // 5 food remaining
        engine.State.AddBuilding(farm);

        var farmer = new UnitEntity(
            engine.State.GenerateEntityId(), factionId, "villager", new Vector2D(50f, 52f),
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        engine.State.AddUnit(farmer);
        engine.SpatialGrid.Insert(farmer.Id, farmer.Position);

        engine.CommandQueue.Enqueue(new GatherCommand(1, factionId, new[] { farmer.Id }, farm.Id));

        // Run 80 ticks
        engine.SimulateTicks(80);

        // Farm should have reseeded and bank wood decreased by 60
        Assert.Equal(40, bank.Wood);
        Assert.True(farm.FarmFoodRemaining > 0);
        Assert.True(bank.Food >= 10);
    }
}
