using System;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class AiIntegrationTests
{
    [Fact]
    public void TC_S08_13_EndToEndEconomicAi_GathersAndExpands()
    {
        var engine = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 }, bounds: new BattlefieldBounds(0, 0, 100, 100));
        var faction1 = new FactionId(1);

        var aiController = new AiFactionController(faction1, new Vector2D(20, 20));
        engine.RegisterAiController(aiController);

        var bank = engine.State.GetOrCreateResourceBank(faction1);
        bank.Deposit(ResourceType.Food, 100, 0);
        bank.Deposit(ResourceType.Wood, 100, 0);

        // Town Center
        var tc = new BuildingEntity(
            engine.State.GenerateEntityId(),
            faction1,
            "town_center",
            new Vector2D(20, 20),
            new Vector2D(4, 4),
            maxHealth: 1500f,
            populationProvided: 10,
            startsConstructed: true);
        engine.State.AddBuilding(tc);

        // Resource Node
        var foodNode = new ResourceNodeEntity(engine.State.GenerateEntityId(), ResourceType.Food, new Vector2D(22, 16), maxAmount: 800);
        engine.State.AddResourceNode(foodNode);

        // Spawn 2 workers
        for (int i = 0; i < 2; i++)
        {
            engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction1, 0, "worker", new Vector2D(20 + i * 2, 22), AttackDamage: 5));
        }

        // Run simulation for 50 ticks
        engine.SimulateTicks(50);

        // Verify food node was harvested
        Assert.True(foodNode.RemainingAmount < 800, "Workers must have harvested food from the node.");
    }

    [Fact]
    public void TC_S08_14_MilitaryAssemblyAndStaging_GroupsUnitsAtRallyPoint()
    {
        var engine = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 }, bounds: new BattlefieldBounds(0, 0, 100, 100));
        var faction1 = new FactionId(1);

        var aiController = new AiFactionController(faction1, new Vector2D(20, 20));
        aiController.ArmySquad.RallyPoint = new Vector2D(30, 30);
        engine.RegisterAiController(aiController);

        // Spawn military units scattered around
        engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction1, 0, "spearman", new Vector2D(15, 15)));
        engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction1, 0, "archer", new Vector2D(12, 18)));

        engine.SimulateTicks(30);

        // Squad members should be moving towards the rally point (30, 30)
        var aliveUnits = aiController.ArmySquad.GetAliveUnits(engine.State);
        Assert.Equal(2, aliveUnits.Count);
        Assert.Equal(AiSquadState.Assembling, aiController.ArmySquad.State);
    }

    [Fact]
    public void TC_S08_15_CombinedArmsArmyComposition_RecruitsMixedForces()
    {
        var engine = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 }, bounds: new BattlefieldBounds(0, 0, 100, 100));
        var faction1 = new FactionId(1);

        var aiController = new AiFactionController(faction1, new Vector2D(20, 20));
        engine.RegisterAiController(aiController);

        var bank = engine.State.GetOrCreateResourceBank(faction1);
        bank.Deposit(ResourceType.Food, 500, 0);
        bank.Deposit(ResourceType.Wood, 500, 0);
        bank.Deposit(ResourceType.Gold, 500, 0);
        bank.Deposit(ResourceType.Iron, 200, 0);

        var popManager = engine.State.GetOrCreatePopulationManager(faction1);
        popManager.SetCurrentPopulation(0, 0);

        // Create barracks, archery range, stable, siege workshop
        var barracks = new BuildingEntity(engine.State.GenerateEntityId(), faction1, "barracks", new Vector2D(25, 20), new Vector2D(3, 3), maxHealth: 800f, startsConstructed: true);
        var range = new BuildingEntity(engine.State.GenerateEntityId(), faction1, "archery_range", new Vector2D(25, 25), new Vector2D(3, 3), maxHealth: 800f, startsConstructed: true);
        var stable = new BuildingEntity(engine.State.GenerateEntityId(), faction1, "stable", new Vector2D(20, 25), new Vector2D(3, 3), maxHealth: 800f, startsConstructed: true);
        var workshop = new BuildingEntity(engine.State.GenerateEntityId(), faction1, "siege_workshop", new Vector2D(20, 30), new Vector2D(3, 3), maxHealth: 800f, startsConstructed: true);

        engine.State.AddBuilding(barracks);
        engine.State.AddBuilding(range);
        engine.State.AddBuilding(stable);
        engine.State.AddBuilding(workshop);

        engine.SimulateTicks(30);

        // Verify production queues are populated across multiple buildings
        Assert.False(barracks.ProductionQueue.IsEmpty, "Barracks should produce infantry.");
        Assert.False(range.ProductionQueue.IsEmpty, "Archery Range should produce archers.");
        Assert.False(stable.ProductionQueue.IsEmpty, "Stable should produce cavalry.");
        Assert.False(workshop.ProductionQueue.IsEmpty, "Siege Workshop should produce siege.");
    }

    [Fact]
    public void TC_S08_16_AutonomousAttackRun_AttacksAndEngagesEnemyTargets()
    {
        var engine = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 42 }, bounds: new BattlefieldBounds(0, 0, 100, 100));
        var faction1 = new FactionId(1);
        var faction2 = new FactionId(2);

        var aiController = new AiFactionController(faction1, new Vector2D(20, 20));
        aiController.ArmySquad.AttackThreshold = 2; // Low threshold for test
        engine.RegisterAiController(aiController);

        // Spawn 2 friendly military units
        engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction1, 0, "cavalry", new Vector2D(22, 22), AttackDamage: 25, MovementSpeed: 6f));
        engine.CommandQueue.Enqueue(new SpawnUnitCommand(faction1, 0, "cavalry", new Vector2D(24, 22), AttackDamage: 25, MovementSpeed: 6f));

        // Spawn an enemy archer within perception zone
        var enemyId = engine.State.GenerateEntityId();
        var enemyArcher = new UnitEntity(
            enemyId,
            faction2,
            "archer",
            new Vector2D(32, 22),
            maxHealth: 60f,
            attackDamage: 8f,
            attackRange: 6f,
            movementSpeed: 3.5f,
            attackCooldownTicks: 20,
            killXpValue: 50);
        engine.State.AddUnit(enemyArcher);

        engine.SimulateTicks(40);

        // Friendly army should have transitioned to Attacking and engaged enemy
        Assert.Equal(AiSquadState.Attacking, aiController.ArmySquad.State);
        Assert.True(enemyArcher.CurrentHealth < 60f || !enemyArcher.IsAlive, "Enemy archer must take damage from AI attack run.");
    }
}
