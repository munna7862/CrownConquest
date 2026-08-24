using System;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Playable and headless scenario orchestrating autonomous AI vs AI gameplay.
/// Factions independently gather resources, expand infrastructure, train armies, coordinate attacks,
/// execute tactical retreats, and wage warfare.
/// </summary>
public sealed class AiFoundationScenario
{
    private readonly SimulationEngine _engine;
    private readonly AiFoundationPresenter _presenter;

    public SimulationEngine Engine => _engine;
    public AiFoundationPresenter Presenter => _presenter;
    public FactionId Faction1 { get; } = new(1);
    public FactionId Faction2 { get; } = new(2);

    public AiFoundationScenario(int seed = 42)
    {
        var config = new SimulationConfig { InitialRandomSeed = seed, TicksPerSecond = 20 };
        _engine = new SimulationEngine(config, bounds: new BattlefieldBounds(0, 0, 100, 100));
        _presenter = new AiFoundationPresenter();
        _presenter.Bind(_engine.EventBus);

        SetupScenario();
    }

    private void SetupScenario()
    {
        // 1. Initial Resources for both factions
        var bank1 = _engine.State.GetOrCreateResourceBank(Faction1);
        bank1.Deposit(ResourceType.Food, 200, 0);
        bank1.Deposit(ResourceType.Wood, 300, 0);
        bank1.Deposit(ResourceType.Gold, 100, 0);
        bank1.Deposit(ResourceType.Stone, 100, 0);

        var bank2 = _engine.State.GetOrCreateResourceBank(Faction2);
        bank2.Deposit(ResourceType.Food, 200, 0);
        bank2.Deposit(ResourceType.Wood, 300, 0);
        bank2.Deposit(ResourceType.Gold, 100, 0);
        bank2.Deposit(ResourceType.Stone, 100, 0);

        // 2. Faction 1 Base Setup at (20, 20)
        var tc1Id = _engine.State.GenerateEntityId();
        var tc1 = new BuildingEntity(
            tc1Id,
            Faction1,
            "town_center",
            new Vector2D(20, 20),
            new Vector2D(4, 4),
            maxHealth: 1500f,
            populationProvided: 10,
            startsConstructed: true);
        _engine.State.AddBuilding(tc1);

        // Spawn 3 workers for Faction 1
        for (int i = 0; i < 3; i++)
        {
            var workerPos = new Vector2D(18 + i * 2, 24);
            _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction1, 0, "worker", workerPos, AttackDamage: 5));
        }

        // Faction 1 Resource Nodes
        SpawnResourceNode(ResourceType.Wood, new Vector2D(14, 20), 1000);
        SpawnResourceNode(ResourceType.Wood, new Vector2D(14, 24), 1000);
        SpawnResourceNode(ResourceType.Food, new Vector2D(20, 14), 800);
        SpawnResourceNode(ResourceType.Gold, new Vector2D(26, 16), 800);
        SpawnResourceNode(ResourceType.Stone, new Vector2D(16, 14), 800);

        // 3. Faction 2 Base Setup at (80, 80)
        var tc2Id = _engine.State.GenerateEntityId();
        var tc2 = new BuildingEntity(
            tc2Id,
            Faction2,
            "town_center",
            new Vector2D(80, 80),
            new Vector2D(4, 4),
            maxHealth: 1500f,
            populationProvided: 10,
            startsConstructed: true);
        _engine.State.AddBuilding(tc2);

        // Spawn 3 workers for Faction 2
        for (int i = 0; i < 3; i++)
        {
            var workerPos = new Vector2D(78 + i * 2, 76);
            _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction2, 0, "worker", workerPos, AttackDamage: 5));
        }

        // Faction 2 Resource Nodes
        SpawnResourceNode(ResourceType.Wood, new Vector2D(86, 80), 1000);
        SpawnResourceNode(ResourceType.Wood, new Vector2D(86, 76), 1000);
        SpawnResourceNode(ResourceType.Food, new Vector2D(80, 86), 800);
        SpawnResourceNode(ResourceType.Gold, new Vector2D(74, 84), 800);
        SpawnResourceNode(ResourceType.Stone, new Vector2D(84, 86), 800);

        // 4. Register Autonomous AI Controllers
        var aiController1 = new AiFactionController(Faction1, new Vector2D(20, 20));
        var aiController2 = new AiFactionController(Faction2, new Vector2D(80, 80));

        _engine.RegisterAiController(aiController1);
        _engine.RegisterAiController(aiController2);

        // Process initial spawns
        _engine.Tick();
    }

    private void SpawnResourceNode(ResourceType type, Vector2D position, int amount)
    {
        var nodeId = _engine.State.GenerateEntityId();
        var node = new ResourceNodeEntity(nodeId, type, position, maxAmount: amount);
        _engine.State.AddResourceNode(node);
    }

    public void RunSimulation(int tickCount)
    {
        _engine.SimulateTicks(tickCount);
    }
}
