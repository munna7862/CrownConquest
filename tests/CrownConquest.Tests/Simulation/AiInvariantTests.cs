using System;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class AiInvariantTests
{
    [Fact]
    public void TC_S08_07_FogOfWarInvariant_NoTargetingUnrevealedEnemies()
    {
        var engine = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 }, bounds: new BattlefieldBounds(0, 0, 100, 100));
        var faction1 = new FactionId(1);
        var faction2 = new FactionId(2);

        var aiController = new AiFactionController(faction1, new Vector2D(10, 10));
        engine.RegisterAiController(aiController);

        // Friendly army unit at (10, 10)
        engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction1, 0, "spearman", new Vector2D(10, 10)));

        // Enemy unit hidden across the map at (90, 90)
        engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction2, 0, "spearman", new Vector2D(90, 90)));

        engine.SimulateTicks(20);

        // Verify perception does not contain the unrevealed enemy
        Assert.Empty(aiController.Perception.ActivePerceivedEnemies);
        Assert.NotEqual(AiSquadState.Attacking, aiController.ArmySquad.State);
    }

    [Fact]
    public void TC_S08_08_WorkerSelfSufficiencyInvariant_IdleWorkersGatherAutomatically()
    {
        var engine = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 }, bounds: new BattlefieldBounds(0, 0, 100, 100));
        var faction1 = new FactionId(1);

        var aiController = new AiFactionController(faction1, new Vector2D(20, 20));
        engine.RegisterAiController(aiController);

        // Spawn a resource node near the worker
        var nodeId = engine.State.GenerateEntityId();
        var node = new ResourceNodeEntity(nodeId, ResourceType.Food, new Vector2D(22, 20), maxAmount: 500);
        engine.State.AddResourceNode(node);

        // Spawn worker
        engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction1, 0, "worker", new Vector2D(20, 20)));

        // Run simulation for 25 ticks
        engine.SimulateTicks(25);

        // Worker should have transitioned out of Idle to Gathering or Moving
        var worker = engine.State.ActiveUnits[0];
        Assert.NotNull(worker.WorkerState);
        Assert.NotEqual(WorkerTaskState.None, worker.WorkerState.TaskState);
    }

    [Fact]
    public void TC_S08_09_HousingPlacementInvariant_BuildsHouseWhenNearPopCap()
    {
        var engine = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 }, bounds: new BattlefieldBounds(0, 0, 100, 100));
        var faction1 = new FactionId(1);

        var aiController = new AiFactionController(faction1, new Vector2D(20, 20));
        engine.RegisterAiController(aiController);

        var bank = engine.State.GetOrCreateResourceBank(faction1);
        bank.Deposit(ResourceType.Wood, 200, 0);

        var popManager = engine.State.GetOrCreatePopulationManager(faction1);

        // Spawn 4 idle workers (pop 4/5, available 1 <= 2)
        for (int i = 0; i < 4; i++)
        {
            engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction1, 0, "worker", new Vector2D(20 + i * 2, 20)));
        }

        engine.SimulateTicks(20);

        // Check if a house or building was placed
        bool housePlaced = false;
        var buildings = engine.State.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].BuildingType.Equals("house", StringComparison.OrdinalIgnoreCase))
            {
                housePlaced = true;
                break;
            }
        }

        Assert.True(housePlaced, "AI must place a House when population is within 2 of maximum capacity.");
    }

    [Fact]
    public void TC_S08_10_BaseDefenseInvariant_ArmyDefendsWhenBaseThreatDetected()
    {
        var engine = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 }, bounds: new BattlefieldBounds(0, 0, 100, 100));
        var faction1 = new FactionId(1);
        var faction2 = new FactionId(2);

        var aiController = new AiFactionController(faction1, new Vector2D(20, 20));
        engine.RegisterAiController(aiController);

        // Friendly army unit near base
        engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction1, 0, "spearman", new Vector2D(22, 22)));

        // Enemy invader enters base perception area (distance <= 25)
        engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction2, 0, "cavalry", new Vector2D(28, 22)));

        engine.SimulateTicks(15);

        // AI squad should transition to Defending
        Assert.Equal(AiSquadState.Defending, aiController.ArmySquad.State);
    }

    [Fact]
    public void TC_S08_11_DynamicRetreatInvariant_OvermatchedSquadRetreatsToBase()
    {
        var engine = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 }, bounds: new BattlefieldBounds(0, 0, 100, 100));
        var faction1 = new FactionId(1);
        var faction2 = new FactionId(2);

        var aiController = new AiFactionController(faction1, new Vector2D(10, 10));
        aiController.ArmySquad.SetState(AiSquadState.Attacking);
        engine.RegisterAiController(aiController);

        // Friendly spearman with low health
        var spearmanId = engine.State.GenerateEntityId();
        var spearman = new UnitEntity(
            spearmanId,
            faction1,
            "spearman",
            new Vector2D(50, 50),
            maxHealth: 100f,
            attackDamage: 10f,
            attackRange: 1.5f,
            movementSpeed: 3.5f,
            attackCooldownTicks: 20,
            killXpValue: 50);
        spearman.TakeDamage(80f, new EntityId(99), faction2, 0, new DomainEventBus(), out _); // 20% health remaining (< 30%)
        engine.State.AddUnit(spearman);
        aiController.ArmySquad.AddMember(spearman.Id);

        // Multiple enemy cavalry nearby
        for (int i = 0; i < 3; i++)
        {
            var enemy = new UnitEntity(
                engine.State.GenerateEntityId(),
                faction2,
                "cavalry",
                new Vector2D(53 + i * 2, 50),
                maxHealth: 150f,
                attackDamage: 20f,
                attackRange: 1.5f,
                movementSpeed: 5.5f,
                attackCooldownTicks: 20,
                killXpValue: 80);
            engine.State.AddUnit(enemy);
        }

        engine.SimulateTicks(10);

        // AI squad should transition to Retreating
        Assert.Equal(AiSquadState.Retreating, aiController.ArmySquad.State);
    }

    [Fact]
    public void TC_S08_12_ProductionQueueInvariant_QueuesMilitaryUnitsWithoutBankrupting()
    {
        var engine = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 }, bounds: new BattlefieldBounds(0, 0, 100, 100));
        var faction1 = new FactionId(1);

        var aiController = new AiFactionController(faction1, new Vector2D(20, 20));
        engine.RegisterAiController(aiController);

        var bank = engine.State.GetOrCreateResourceBank(faction1);
        bank.Deposit(ResourceType.Food, 150, 0);
        bank.Deposit(ResourceType.Wood, 50, 0);
        bank.Deposit(ResourceType.Gold, 50, 0);

        var popManager = engine.State.GetOrCreatePopulationManager(faction1);
        popManager.SetCurrentPopulation(1, 0);

        // Add a constructed barracks
        var barracksId = engine.State.GenerateEntityId();
        var barracks = new BuildingEntity(
            barracksId,
            faction1,
            "barracks",
            new Vector2D(25, 25),
            new Vector2D(3, 3),
            maxHealth: 800f,
            startsConstructed: true);
        engine.State.AddBuilding(barracks);

        engine.SimulateTicks(25);

        // Barracks should have unit in production queue
        Assert.False(barracks.ProductionQueue.IsEmpty);
    }
}
