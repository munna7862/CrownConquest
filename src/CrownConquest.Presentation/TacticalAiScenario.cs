using System;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Headless match scenario pitting two AI factions with distinct personalities and tactical doctrines
/// against each other (e.g. Aggressive Raider with Cavalry Flanking vs Defensive Bastion with Fortifications).
/// </summary>
public sealed class TacticalAiScenario
{
    private readonly SimulationEngine _engine;
    private readonly TacticalAiPresenter _presenter;

    public SimulationEngine Engine => _engine;
    public TacticalAiPresenter Presenter => _presenter;
    public FactionId Faction1 { get; } = new(1); // Aggressive / Tactical
    public FactionId Faction2 { get; } = new(2); // Defensive / Fortified

    public TacticalAiScenario(int seed = 42)
    {
        var config = new SimulationConfig { InitialRandomSeed = seed, TicksPerSecond = 20 };
        _engine = new SimulationEngine(config, bounds: new BattlefieldBounds(0, 0, 100, 100));
        _presenter = new TacticalAiPresenter();
        _presenter.Bind(_engine.EventBus);

        SetupScenario();
    }

    private void SetupScenario()
    {
        // 1. Initial Resources
        var bank1 = _engine.State.GetOrCreateResourceBank(Faction1);
        bank1.Deposit(ResourceType.Food, 400, 0);
        bank1.Deposit(ResourceType.Wood, 500, 0);
        bank1.Deposit(ResourceType.Gold, 300, 0);
        bank1.Deposit(ResourceType.Stone, 200, 0);

        var bank2 = _engine.State.GetOrCreateResourceBank(Faction2);
        bank2.Deposit(ResourceType.Food, 400, 0);
        bank2.Deposit(ResourceType.Wood, 500, 0);
        bank2.Deposit(ResourceType.Gold, 300, 0);
        bank2.Deposit(ResourceType.Stone, 400, 0);

        // 2. Faction 1 Setup (Aggressive Raider at 20, 20)
        var tc1Id = _engine.State.GenerateEntityId();
        var tc1 = new BuildingEntity(
            tc1Id,
            Faction1,
            "town_center",
            new Vector2D(20, 20),
            new Vector2D(4, 4),
            maxHealth: 1500f,
            populationProvided: 15,
            startsConstructed: true);
        _engine.State.AddBuilding(tc1);

        // Workers
        for (int i = 0; i < 4; i++)
        {
            var workerPos = new Vector2D(18 + i * 2, 24);
            _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction1, 0, "worker", workerPos, AttackDamage: 5));
        }

        // Army units: Cavalry for flanking + Catapult for siege + Spearmen
        _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction1, 0, "cavalry", new Vector2D(24, 24), AttackDamage: 18));
        _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction1, 0, "cavalry", new Vector2D(26, 24), AttackDamage: 18));
        _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction1, 0, "catapult", new Vector2D(22, 26), AttackDamage: 40));
        _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction1, 0, "spearman", new Vector2D(20, 26), AttackDamage: 12));

        // Resources for Faction 1
        SpawnResourceNode(ResourceType.Wood, new Vector2D(14, 20), 1000);
        SpawnResourceNode(ResourceType.Food, new Vector2D(20, 14), 800);
        SpawnResourceNode(ResourceType.Gold, new Vector2D(26, 16), 800);
        SpawnResourceNode(ResourceType.Stone, new Vector2D(16, 14), 800);

        // 3. Faction 2 Setup (Defensive Bastion at 80, 80)
        var tc2Id = _engine.State.GenerateEntityId();
        var tc2 = new BuildingEntity(
            tc2Id,
            Faction2,
            "town_center",
            new Vector2D(80, 80),
            new Vector2D(4, 4),
            maxHealth: 1500f,
            populationProvided: 15,
            startsConstructed: true);
        _engine.State.AddBuilding(tc2);

        // Defensive Wall & Tower
        var wallId = _engine.State.GenerateEntityId();
        var wall = new BuildingEntity(
            wallId,
            Faction2,
            "stone_wall",
            new Vector2D(70, 70),
            new Vector2D(2, 2),
            maxHealth: 500f,
            populationProvided: 0,
            startsConstructed: true);
        _engine.State.AddBuilding(wall);

        var towerId = _engine.State.GenerateEntityId();
        var tower = new BuildingEntity(
            towerId,
            Faction2,
            "watchtower",
            new Vector2D(74, 74),
            new Vector2D(2, 2),
            maxHealth: 600f,
            populationProvided: 0,
            startsConstructed: true);
        _engine.State.AddBuilding(tower);

        // Workers
        for (int i = 0; i < 4; i++)
        {
            var workerPos = new Vector2D(78 + i * 2, 76);
            _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction2, 0, "worker", workerPos, AttackDamage: 5));
        }

        // Defensive Spearmen & Archers
        _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction2, 0, "spearman", new Vector2D(72, 72), AttackDamage: 12));
        _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction2, 0, "spearman", new Vector2D(74, 70), AttackDamage: 12));
        _engine.CommandQueue.Enqueue(new SpawnUnitCommand(Faction2, 0, "archer", new Vector2D(76, 76), AttackDamage: 10));

        // Resources for Faction 2
        SpawnResourceNode(ResourceType.Wood, new Vector2D(86, 80), 1000);
        SpawnResourceNode(ResourceType.Food, new Vector2D(80, 86), 800);
        SpawnResourceNode(ResourceType.Gold, new Vector2D(74, 84), 800);
        SpawnResourceNode(ResourceType.Stone, new Vector2D(84, 86), 800);

        // 4. Configure AI Personalities
        var aiController1 = new AiFactionController(
            Faction1,
            new Vector2D(20, 20),
            personality: AiPersonalityProfile.CreateAggressive());

        var aiController2 = new AiFactionController(
            Faction2,
            new Vector2D(80, 80),
            personality: AiPersonalityProfile.CreateDefensive());

        _engine.RegisterAiController(aiController1);
        _engine.RegisterAiController(aiController2);

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
