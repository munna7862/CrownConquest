using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

public sealed class CivilizationProgressionScenario
{
    public GameCoordinator Coordinator { get; }
    public FactionId PlayerFaction { get; } = new(1);
    public FactionId EnemyFaction { get; } = new(2);

    public BuildingEntity TownCenter { get; private set; } = null!;
    public BuildingEntity Barracks { get; private set; } = null!;
    public BuildingEntity? Blacksmith { get; private set; }
    public BuildingEntity? ArcheryRange { get; private set; }
    public BuildingEntity? Stable { get; private set; }

    public CivilizationProgressionPresenter Presenter { get; }

    public CivilizationProgressionScenario()
    {
        var config = new SimulationConfig
        {
            TicksPerSecond = 20,
            InitialRandomSeed = 4242
        };

        Coordinator = new GameCoordinator(config);
        Presenter = new CivilizationProgressionPresenter(Coordinator, PlayerFaction);

        InitializeScenario();
        Presenter.UpdateSnapshot();
    }

    private void InitializeScenario()
    {
        var sim = Coordinator.Simulation;

        // Stockpile resources for civilization advancement
        var playerBank = Coordinator.GetResourceBank(PlayerFaction);
        playerBank.Deposit(ResourceType.Food, 2500, 1UL);
        playerBank.Deposit(ResourceType.Wood, 1500, 1UL);
        playerBank.Deposit(ResourceType.Gold, 1500, 1UL);
        playerBank.Deposit(ResourceType.Stone, 500, 1UL);
        playerBank.Deposit(ResourceType.Iron, 500, 1UL);

        // Player Town Center at (30, 30)
        TownCenter = new BuildingEntity(
            sim.State.GenerateEntityId(),
            PlayerFaction,
            "town_center",
            new Vector2D(30f, 30f),
            new Vector2D(4f, 4f),
            startsConstructed: true,
            rallyPoint: new Vector2D(36f, 30f));
        sim.State.AddBuilding(TownCenter);

        // Player Barracks at (30, 20)
        Barracks = new BuildingEntity(
            sim.State.GenerateEntityId(),
            PlayerFaction,
            "barracks",
            new Vector2D(30f, 20f),
            new Vector2D(3f, 3f),
            startsConstructed: true,
            rallyPoint: new Vector2D(35f, 20f));
        sim.State.AddBuilding(Barracks);

        // Houses for population capacity
        var house1 = new BuildingEntity(
            sim.State.GenerateEntityId(),
            PlayerFaction,
            "house",
            new Vector2D(22f, 30f),
            new Vector2D(2f, 2f),
            populationProvided: 10,
            startsConstructed: true);
        sim.State.AddBuilding(house1);

        var house2 = new BuildingEntity(
            sim.State.GenerateEntityId(),
            PlayerFaction,
            "house",
            new Vector2D(22f, 25f),
            new Vector2D(2f, 2f),
            populationProvided: 10,
            startsConstructed: true);
        sim.State.AddBuilding(house2);

        // Spawn 4 Player Builders/Workers
        for (int i = 0; i < 4; i++)
        {
            var workerId = sim.State.GenerateEntityId();
            var worker = new UnitEntity(
                workerId,
                PlayerFaction,
                "celtic_villager",
                new Vector2D(32f + (i * 2f), 32f),
                workerState: new WorkerGatherState(carryCapacity: 10, buildPowerPerTick: 2.0f));
            sim.State.AddUnit(worker);
            sim.SpatialGrid.Insert(worker.Id, worker.Position);
        }

        // Spawn Enemy Raiding Outpost & Units at (80, 50)
        var enemyBank = Coordinator.GetResourceBank(EnemyFaction);
        enemyBank.Deposit(ResourceType.Food, 500, 1UL);

        // 3 Roman Equites (Cavalry)
        for (int i = 0; i < 3; i++)
        {
            var cavId = sim.State.GenerateEntityId();
            var cav = new UnitEntity(
                cavId,
                EnemyFaction,
                "roman_equite",
                new Vector2D(78f + (i * 2f), 48f),
                maxHealth: 160f,
                attackDamage: 20f,
                movementSpeed: 5.0f,
                baseArmor: 4.0f,
                archetype: UnitArchetype.Cavalry);
            sim.State.AddUnit(cav);
            sim.SpatialGrid.Insert(cav.Id, cav.Position);
        }

        // 3 Roman Legionaries (Infantry)
        for (int i = 0; i < 3; i++)
        {
            var infId = sim.State.GenerateEntityId();
            var inf = new UnitEntity(
                infId,
                EnemyFaction,
                "roman_legionary",
                new Vector2D(78f + (i * 2f), 52f),
                maxHealth: 140f,
                attackDamage: 16f,
                movementSpeed: 3.2f,
                baseArmor: 5.0f,
                archetype: UnitArchetype.Infantry);
            sim.State.AddUnit(inf);
            sim.SpatialGrid.Insert(inf.Id, inf.Position);
        }
    }

    /// <summary>
    /// Executes the full evolution scenario: Advance Era -> Build Blacksmith, Archery Range, Stable ->
    /// Research Upgrades -> Train Mixed Force -> Eliminate Enemy Threat.
    /// </summary>
    public void ExecuteEvolutionScenario(out int totalTicksTaken)
    {
        var sim = Coordinator.Simulation;

        // Phase 1: Advance to Classical Era (100 ticks)
        Coordinator.IssueAdvanceEraOrder(PlayerFaction, TownCenter.Id, CivilizationEra.Classical);
        sim.SimulateTicks(105);

        // Phase 2: Place & Construct Blacksmith, Archery Range, Stable
        var blacksmithPos = new Vector2D(40f, 30f);
        var archeryPos = new Vector2D(40f, 20f);
        var stablePos = new Vector2D(40f, 40f);

        Coordinator.IssuePlaceBuildingOrder(PlayerFaction, "blacksmith", blacksmithPos);
        Coordinator.IssuePlaceBuildingOrder(PlayerFaction, "archery_range", archeryPos);
        Coordinator.IssuePlaceBuildingOrder(PlayerFaction, "stable", stablePos);
        sim.SimulateTicks(2);

        foreach (var b in sim.State.ActiveBuildings)
        {
            if (b.FactionId == PlayerFaction && !b.IsConstructed)
            {
                if (b.BuildingType.Equals("blacksmith", StringComparison.OrdinalIgnoreCase)) Blacksmith = b;
                if (b.BuildingType.Equals("archery_range", StringComparison.OrdinalIgnoreCase)) ArcheryRange = b;
                if (b.BuildingType.Equals("stable", StringComparison.OrdinalIgnoreCase)) Stable = b;

                // Fast construct for scenario demo
                b.Construct(100f, sim.CurrentTick, sim.EventBus, out _);
            }
        }

        // Phase 3: Research Forging (+2 Melee), Scale Armor (+2 Armor), and Fletching (+1 Dmg, +1 Range)
        if (Blacksmith != null)
        {
            Coordinator.IssueStartResearchOrder(PlayerFaction, Blacksmith.Id, "forging");
            Coordinator.IssueStartResearchOrder(PlayerFaction, Blacksmith.Id, "scale_armor");
            Coordinator.IssueStartResearchOrder(PlayerFaction, Blacksmith.Id, "fletching");
        }
        if (Stable != null)
        {
            Coordinator.IssueStartResearchOrder(PlayerFaction, Stable.Id, "husbandry");
        }

        sim.SimulateTicks(130); // Advance until research completes

        // Phase 4: Train mixed military units (3 Spearmen, 3 Archers, 3 Cavalry)
        Coordinator.IssueQueueProductionOrder(PlayerFaction, Barracks.Id, "celtic_spearman");
        Coordinator.IssueQueueProductionOrder(PlayerFaction, Barracks.Id, "celtic_spearman");
        Coordinator.IssueQueueProductionOrder(PlayerFaction, Barracks.Id, "celtic_spearman");

        if (ArcheryRange != null)
        {
            Coordinator.IssueQueueProductionOrder(PlayerFaction, ArcheryRange.Id, "celtic_archer");
            Coordinator.IssueQueueProductionOrder(PlayerFaction, ArcheryRange.Id, "celtic_archer");
            Coordinator.IssueQueueProductionOrder(PlayerFaction, ArcheryRange.Id, "celtic_archer");
        }

        if (Stable != null)
        {
            Coordinator.IssueQueueProductionOrder(PlayerFaction, Stable.Id, "celtic_scout_cavalry");
            Coordinator.IssueQueueProductionOrder(PlayerFaction, Stable.Id, "celtic_scout_cavalry");
            Coordinator.IssueQueueProductionOrder(PlayerFaction, Stable.Id, "celtic_scout_cavalry");
        }

        sim.SimulateTicks(250); // Production completes for all buildings

        // Phase 5: Order Player Army and Enemy Army to engage
        var playerMilitary = new List<UnitEntity>();
        foreach (var u in sim.State.ActiveUnits)
        {
            if (u.FactionId == PlayerFaction && u.Archetype != UnitArchetype.Worker && u.IsAlive)
            {
                playerMilitary.Add(u);
            }
        }

        var enemyMilitary = new List<UnitEntity>();
        foreach (var u in sim.State.ActiveUnits)
        {
            if (u.FactionId == EnemyFaction && u.IsAlive)
            {
                enemyMilitary.Add(u);
            }
        }

        // Phase 6: Run Combat until victory with battlefield target re-acquisition
        for (int step = 0; step < 40; step++)
        {
            var livingEnemies = new List<UnitEntity>();
            foreach (var u in sim.State.ActiveUnits)
            {
                if (u.FactionId == EnemyFaction && u.IsAlive)
                {
                    livingEnemies.Add(u);
                }
            }

            if (livingEnemies.Count == 0) break;

            foreach (var u in sim.State.ActiveUnits)
            {
                if (u.FactionId == PlayerFaction && u.Archetype != UnitArchetype.Worker && u.IsAlive && u.State == UnitState.Idle)
                {
                    UnitEntity? nearest = null;
                    float minDist = float.MaxValue;
                    for (int e = 0; e < livingEnemies.Count; e++)
                    {
                        float dist = u.Position.DistanceTo(livingEnemies[e].Position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            nearest = livingEnemies[e];
                        }
                    }
                    if (nearest != null)
                    {
                        u.Attack(nearest.Id);
                    }
                }
            }

            sim.SimulateTicks(30);
        }

        Presenter.UpdateSnapshot();
        totalTicksTaken = (int)sim.CurrentTick;
    }
}
