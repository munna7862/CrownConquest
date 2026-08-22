using System;
using System.Collections.Generic;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class EconomyIntegrationTests
{
    [Fact]
    public void Gathering_FullWorkerCycle_MoveHarvestReturnDeposit()
    {
        // TC-S02-013: Full worker gathering cycle from Gold mine to Town Center
        var sim = new SimulationEngine();
        var factionId = new FactionId(1);

        var tc = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "town_center",
            new Vector2D(50f, 50f),
            new Vector2D(4f, 4f),
            startsConstructed: true,
            acceptedDropOffTypes: new[] { ResourceType.Gold });
        sim.State.AddBuilding(tc);

        var goldMine = new ResourceNodeEntity(
            sim.State.GenerateEntityId(),
            ResourceType.Gold,
            new Vector2D(58f, 50f),
            maxAmount: 500,
            harvestRadius: 2.0f);
        sim.State.AddResourceNode(goldMine);

        var worker = new UnitEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "villager",
            new Vector2D(50f, 50f),
            movementSpeed: 4.0f,
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        sim.State.AddUnit(worker);

        int depositEvents = 0;
        sim.EventBus.Subscribe<ResourceDepositedEvent>((in ResourceDepositedEvent e) =>
        {
            if (e.Type == ResourceType.Gold) depositEvents++;
        });

        // Issue gather order
        sim.CommandQueue.Enqueue(new GatherCommand(1UL, factionId, new[] { worker.Id }, goldMine.Id));

        // Simulate 80 ticks (travel to mine, harvest 10 gold, travel back to TC, deposit)
        sim.SimulateTicks(80);

        Assert.True(depositEvents >= 1, "Expected at least 1 deposit event.");
        Assert.True(sim.State.GetOrCreateResourceBank(factionId).Gold >= 10, "Expected at least 10 gold in bank.");
    }

    [Fact]
    public void Construction_SingleAndMultiWorkerBuilding()
    {
        // TC-S02-014: Single worker vs multi-worker construction acceleration
        void TestConstruction(int workerCount, out ulong completionTick)
        {
            var sim = new SimulationEngine();
            var factionId = new FactionId(1);

            var barracks = new BuildingEntity(
                sim.State.GenerateEntityId(),
                factionId,
                "barracks",
                new Vector2D(50f, 50f),
                new Vector2D(3f, 3f),
                baseBuildTimeTicks: 60f,
                startsConstructed: false);
            sim.State.AddBuilding(barracks);

            var workerIds = new List<EntityId>();
            for (int i = 0; i < workerCount; i++)
            {
                var w = new UnitEntity(
                    sim.State.GenerateEntityId(),
                    factionId,
                    "villager",
                    new Vector2D(50f + i, 48f),
                    workerState: new WorkerGatherState(buildPowerPerTick: 1.0f));
                sim.State.AddUnit(w);
                workerIds.Add(w.Id);
            }

            ulong finishedTick = 0;
            sim.EventBus.Subscribe<BuildingCompletedEvent>((in BuildingCompletedEvent e) =>
            {
                finishedTick = e.SimulationTick;
            });

            sim.CommandQueue.Enqueue(new ConstructBuildingCommand(1UL, factionId, workerIds.ToArray(), barracks.Id));

            for (int t = 0; t < 100; t++)
            {
                sim.Tick();
                if (barracks.IsConstructed) break;
            }

            completionTick = finishedTick;
        }

        TestConstruction(1, out ulong singleWorkerFinish);
        TestConstruction(3, out ulong multiWorkerFinish);

        Assert.True(singleWorkerFinish > 0, "Single worker failed to complete construction.");
        Assert.True(multiWorkerFinish > 0, "Multi workers failed to complete construction.");
        Assert.True(multiWorkerFinish < singleWorkerFinish, $"Expected 3 workers ({multiWorkerFinish} ticks) to finish faster than 1 worker ({singleWorkerFinish} ticks).");
    }

    [Fact]
    public void Production_TownCenter_VillagerTraining()
    {
        // TC-S02-015: Town Center trains Villager with food deduction and pop cap increment
        var sim = new SimulationEngine();
        var factionId = new FactionId(1);

        var tc = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "town_center",
            new Vector2D(50f, 50f),
            new Vector2D(4f, 4f),
            populationProvided: 10,
            startsConstructed: true);
        sim.State.AddBuilding(tc);

        var bank = sim.State.GetOrCreateResourceBank(factionId);
        bank.Deposit(ResourceType.Food, 100, 1UL);

        int spawnedEvents = 0;
        sim.EventBus.Subscribe<UnitSpawnedEvent>((in UnitSpawnedEvent e) =>
        {
            if (e.UnitType.Equals("villager", StringComparison.OrdinalIgnoreCase)) spawnedEvents++;
        });

        // Queue Villager (Cost: 50 Food, Duration: 50 Ticks)
        sim.CommandQueue.Enqueue(new QueueProductionCommand(1UL, factionId, tc.Id, "villager"));
        sim.SimulateTicks(1);

        Assert.Equal(50, bank.Food); // 100 - 50 = 50 Food remaining
        Assert.Equal(1, tc.ProductionQueue.Count);

        // Simulate 60 ticks
        sim.SimulateTicks(60);

        Assert.True(tc.ProductionQueue.IsEmpty);
        Assert.Equal(1, spawnedEvents);
        Assert.Equal(1, sim.State.GetOrCreatePopulationManager(factionId).CurrentPopulation);
    }

    [Fact]
    public void Production_Barracks_SwordsmanTraining()
    {
        // TC-S02-016: Barracks produces Swordsman at rally point
        var sim = new SimulationEngine();
        var factionId = new FactionId(1);

        var barracks = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "barracks",
            new Vector2D(50f, 50f),
            new Vector2D(3f, 3f),
            startsConstructed: true,
            rallyPoint: new Vector2D(55f, 55f));
        sim.State.AddBuilding(barracks);

        var bank = sim.State.GetOrCreateResourceBank(factionId);
        bank.Deposit(ResourceType.Food, 200, 1UL);
        bank.Deposit(ResourceType.Iron, 100, 1UL);

        EntityId trainedUnitId = EntityId.None;
        sim.EventBus.Subscribe<ProductionCompletedEvent>((in ProductionCompletedEvent e) =>
        {
            trainedUnitId = e.ProducedUnitId;
        });

        // Queue Swordsman
        sim.CommandQueue.Enqueue(new QueueProductionCommand(1UL, factionId, barracks.Id, "swordsman"));

        // Simulate 70 ticks
        sim.SimulateTicks(70);

        Assert.True(trainedUnitId.IsValid, "Expected swordsman to finish training.");
        Assert.True(sim.State.TryGetUnit(trainedUnitId, out var swordsman) && swordsman != null);
        Assert.Equal("swordsman", swordsman!.UnitType);
        Assert.Equal(55f, swordsman.Position.X, 0.1f);
        Assert.Equal(55f, swordsman.Position.Y, 0.1f);
    }

    [Fact]
    public void DropOff_StoragePit_ResourceFiltering()
    {
        // TC-S02-017: Storage Pit accepts Wood/Stone/Iron/Gold but not Food
        var sim = new SimulationEngine();
        var factionId = new FactionId(1);

        // Distant Town Center at (80, 80)
        var tc = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "town_center",
            new Vector2D(80f, 80f),
            new Vector2D(4f, 4f),
            startsConstructed: true,
            acceptedDropOffTypes: new[] { ResourceType.Food, ResourceType.Wood });
        sim.State.AddBuilding(tc);

        // Nearby Storage Pit at (48f, 50f)
        var storagePit = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "storage_pit",
            new Vector2D(48f, 50f),
            new Vector2D(2f, 2f),
            startsConstructed: true,
            acceptedDropOffTypes: new[] { ResourceType.Wood, ResourceType.Gold, ResourceType.Stone, ResourceType.Iron });
        sim.State.AddBuilding(storagePit);

        // Tree at (44f, 50f)
        var tree = new ResourceNodeEntity(
            sim.State.GenerateEntityId(),
            ResourceType.Wood,
            new Vector2D(44f, 50f),
            maxAmount: 500,
            harvestRadius: 1.8f);
        sim.State.AddResourceNode(tree);

        // Worker at (44f, 50f)
        var worker = new UnitEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "villager",
            new Vector2D(44f, 50f),
            movementSpeed: 4.0f,
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        sim.State.AddUnit(worker);

        sim.CommandQueue.Enqueue(new GatherCommand(1UL, factionId, new[] { worker.Id }, tree.Id));

        // Simulate 30 ticks (gather 10 wood -> deposit at nearest drop-off: Storage Pit)
        sim.SimulateTicks(30);

        Assert.Equal(10, sim.State.GetOrCreateResourceBank(factionId).Wood);
        // Worker deposited at Storage Pit (around (48, 50)), NOT distant TC (80, 80)
        Assert.True(worker.Position.X < 60f, $"Worker should have stayed near Storage Pit, but was at {worker.Position}");
    }
}
