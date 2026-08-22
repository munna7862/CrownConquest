using System;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class EconomyInvariantTests
{
    [Fact]
    public void Economy_ConservationOfResources_Invariant()
    {
        // TC-S02-008: Total initial resources == Remaining in node + Carried by workers + Deposited in bank
        var sim = new SimulationEngine();
        var factionId = new FactionId(1);
        int initialNodeWood = 100;

        // Town Center at (50, 50)
        var tc = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "town_center",
            new Vector2D(50f, 50f),
            new Vector2D(4f, 4f),
            startsConstructed: true,
            acceptedDropOffTypes: new[] { ResourceType.Wood, ResourceType.Food, ResourceType.Gold, ResourceType.Stone, ResourceType.Iron });
        sim.State.AddBuilding(tc);

        // Tree at (45, 50) with 100 Wood
        var tree = new ResourceNodeEntity(
            sim.State.GenerateEntityId(),
            ResourceType.Wood,
            new Vector2D(45f, 50f),
            maxAmount: initialNodeWood,
            harvestRadius: 1.8f);
        sim.State.AddResourceNode(tree);

        // 2 Workers at (47, 50)
        var w1 = new UnitEntity(sim.State.GenerateEntityId(), factionId, "villager", new Vector2D(47f, 50f), workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        var w2 = new UnitEntity(sim.State.GenerateEntityId(), factionId, "villager", new Vector2D(47f, 50f), workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        sim.State.AddUnit(w1);
        sim.State.AddUnit(w2);

        // Order workers to gather
        sim.CommandQueue.Enqueue(new GatherCommand(1UL, factionId, new[] { w1.Id, w2.Id }, tree.Id));

        // Simulate 150 ticks (should fully harvest the tree and deposit into bank)
        for (int t = 0; t < 150; t++)
        {
            sim.Tick();

            // Invariant check at EVERY simulation tick:
            int remainingInNode = sim.State.TryGetResourceNode(tree.Id, out var n) && n != null ? n.RemainingAmount : 0;
            int carriedByWorkers = (w1.WorkerState?.CarriedAmount ?? 0) + (w2.WorkerState?.CarriedAmount ?? 0);
            int depositedInBank = sim.State.GetOrCreateResourceBank(factionId).Wood;

            int totalWoodInSystem = remainingInNode + carriedByWorkers + depositedInBank;
            Assert.Equal(initialNodeWood, totalWoodInSystem);
        }

        // After completion: all 100 wood must be in bank
        Assert.Equal(100, sim.State.GetOrCreateResourceBank(factionId).Wood);
    }

    [Fact]
    public void Economy_WorkerInterruption_NoResourceLoss()
    {
        // TC-S02-009: Interrupted worker retains carried resources
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
            new Vector2D(48f, 50f),
            maxAmount: 500,
            harvestRadius: 2.0f);
        sim.State.AddResourceNode(goldMine);

        var worker = new UnitEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "villager",
            new Vector2D(48f, 50f),
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        sim.State.AddUnit(worker);

        // Gather gold
        sim.CommandQueue.Enqueue(new GatherCommand(1UL, factionId, new[] { worker.Id }, goldMine.Id));
        sim.SimulateTicks(7);

        int carriedBefore = worker.WorkerState!.CarriedAmount;
        Assert.True(carriedBefore > 0);
        Assert.Equal(ResourceType.Gold, worker.WorkerState.CarriedResourceType);

        // Interrupt worker with a move order away
        sim.CommandQueue.Enqueue(new MoveCommand(factionId, sim.CurrentTick, new[] { worker.Id }, new Vector2D(80f, 80f)));
        sim.SimulateTicks(10);

        // Worker must still possess the carried gold
        Assert.Equal(carriedBefore, worker.WorkerState.CarriedAmount);
        Assert.Equal(ResourceType.Gold, worker.WorkerState.CarriedResourceType);

        // Now order worker to gather from mine again -> should return and deposit
        sim.CommandQueue.Enqueue(new GatherCommand(sim.CurrentTick, factionId, new[] { worker.Id }, goldMine.Id));
        sim.SimulateTicks(60);

        // Bank should have at least the deposited gold
        Assert.True(sim.State.GetOrCreateResourceBank(factionId).Gold >= 6);
    }

    [Fact]
    public void Economy_PopulationCap_StrictlyEnforced()
    {
        // TC-S02-010: Pop cap prevents unit training
        var sim = new SimulationEngine();
        var factionId = new FactionId(1);

        var tc = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "town_center",
            new Vector2D(50f, 50f),
            new Vector2D(4f, 4f),
            populationProvided: 2, // Town Center provides 2 pop (Base 5 + 2 = 7)
            startsConstructed: true);
        sim.State.AddBuilding(tc);

        // Give plenty of food
        sim.State.GetOrCreateResourceBank(factionId).Deposit(ResourceType.Food, 1000, 1UL);

        // Spawn 7 living units to reach pop cap (5 base + 2 TC = 7)
        for (int i = 0; i < 7; i++)
        {
            sim.State.AddUnit(new UnitEntity(sim.State.GenerateEntityId(), factionId, "villager", new Vector2D(50f, 50f)));
        }

        sim.SimulateTicks(1);

        var popManager = sim.State.GetOrCreatePopulationManager(factionId);
        Assert.True(popManager.IsPopCapped);

        // Attempt to queue unit
        sim.CommandQueue.Enqueue(new QueueProductionCommand(sim.CurrentTick, factionId, tc.Id, "villager"));
        sim.SimulateTicks(1);

        // Queue must be empty because pop cap was reached
        Assert.True(tc.ProductionQueue.IsEmpty);
    }

    [Fact]
    public void Economy_NodeDepletion_AutoRetargetNearestNode()
    {
        // TC-S02-011: Worker auto-retargets nearest node upon depletion
        var sim = new SimulationEngine();
        var factionId = new FactionId(1);

        var tc = new BuildingEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "town_center",
            new Vector2D(50f, 50f),
            new Vector2D(4f, 4f),
            startsConstructed: true,
            acceptedDropOffTypes: new[] { ResourceType.Wood });
        sim.State.AddBuilding(tc);

        // Tree 1 (small, 10 wood)
        var tree1 = new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Wood, new Vector2D(46f, 50f), maxAmount: 10, harvestRadius: 1.8f);
        // Tree 2 (larger, 200 wood)
        var tree2 = new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Wood, new Vector2D(44f, 50f), maxAmount: 200, harvestRadius: 1.8f);

        sim.State.AddResourceNode(tree1);
        sim.State.AddResourceNode(tree2);

        var worker = new UnitEntity(
            sim.State.GenerateEntityId(),
            factionId,
            "villager",
            new Vector2D(46f, 50f),
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 1.0f));
        sim.State.AddUnit(worker);

        sim.CommandQueue.Enqueue(new GatherCommand(1UL, factionId, new[] { worker.Id }, tree1.Id));

        // Simulate 100 ticks
        sim.SimulateTicks(100);

        // Tree 1 is depleted
        Assert.True(tree1.IsDepleted);
        // Tree 2 has been harvested from
        Assert.True(tree2.RemainingAmount < 200);
        // Bank received deposits from both trees
        Assert.True(sim.State.GetOrCreateResourceBank(factionId).Wood > 10);
    }

    [Fact]
    public void Economy_BitExactReplay_500Ticks()
    {
        // TC-S02-012: Deterministic bit-exact state checksum across 2 runs
        var sim1 = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 });
        var sim2 = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 });

        var factionId = new FactionId(1);

        void SetupSim(SimulationEngine sim)
        {
            var tc = new BuildingEntity(
                new EntityId(1),
                factionId,
                "town_center",
                new Vector2D(50f, 50f),
                new Vector2D(4f, 4f),
                startsConstructed: true,
                acceptedDropOffTypes: new[] { ResourceType.Wood, ResourceType.Food });
            sim.State.AddBuilding(tc);

            var tree = new ResourceNodeEntity(new EntityId(2), ResourceType.Wood, new Vector2D(45f, 50f), maxAmount: 500);
            sim.State.AddResourceNode(tree);

            var w1 = new UnitEntity(new EntityId(3), factionId, "villager", new Vector2D(48f, 50f), workerState: new WorkerGatherState());
            sim.State.AddUnit(w1);

            sim.CommandQueue.Enqueue(new GatherCommand(1UL, factionId, new[] { w1.Id }, tree.Id));
        }

        SetupSim(sim1);
        SetupSim(sim2);

        sim1.SimulateTicks(500);
        sim2.SimulateTicks(500);

        ulong checksum1 = sim1.State.ComputeStateChecksum();
        ulong checksum2 = sim2.State.ComputeStateChecksum();

        Assert.Equal(checksum1, checksum2);
        Assert.Equal(sim1.State.GetOrCreateResourceBank(factionId).Wood, sim2.State.GetOrCreateResourceBank(factionId).Wood);
    }
}
