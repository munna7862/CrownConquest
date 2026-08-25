using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Authoritative interactive scenario setting up a complete 2D RTS battlefield:
/// Celtic Player Base -> 5-Resource Economy -> Barracks & Blacksmith -> Army Formations -> Hero Brennus -> Roman Opposition.
/// </summary>
public sealed class GraphicalGameScenario
{
    public GameCoordinator Coordinator { get; }
    public SelectionManager Selection { get; }
    public RtsCameraController Camera { get; }
    public GameViewRenderer Renderer { get; }
    public InteractiveRtsHud Hud { get; }

    public FactionId PlayerFaction { get; } = FactionId.Player1;
    public FactionId EnemyFaction { get; } = FactionId.Player2;

    public UnitEntity HeroUnit { get; private set; } = null!;
    public BuildingEntity PlayerTownCenter { get; private set; } = null!;
    public BuildingEntity PlayerBarracks { get; private set; } = null!;
    public List<UnitEntity> PlayerArmy { get; } = new(16);
    public List<UnitEntity> EnemyArmy { get; } = new(16);

    public GraphicalGameScenario(int seed = 1337)
    {
        var config = new SimulationConfig
        {
            InitialRandomSeed = seed,
            TicksPerSecond = 20
        };

        var bounds = new BattlefieldBounds(0, 0, 200, 200);
        Coordinator = new GameCoordinator(config);
        Selection = new SelectionManager(Coordinator, PlayerFaction);
        Camera = new RtsCameraController(new Vector2D(50, 50), initialZoom: 1.0f, bounds);
        Renderer = new GameViewRenderer(Coordinator, Camera);
        Hud = new InteractiveRtsHud(Coordinator, Selection, Camera, PlayerFaction);

        SetupBattlefield();
    }

    private void SetupBattlefield()
    {
        var sim = Coordinator.Simulation;
        ulong tick = Coordinator.CurrentTick;

        // 1. Initial Resource Stockpile
        var playerBank = Coordinator.GetResourceBank(PlayerFaction);
        playerBank.Deposit(ResourceType.Food, 500, tick);
        playerBank.Deposit(ResourceType.Wood, 500, tick);
        playerBank.Deposit(ResourceType.Gold, 300, tick);
        playerBank.Deposit(ResourceType.Stone, 200, tick);
        playerBank.Deposit(ResourceType.Iron, 150, tick);

        // 2. Player Town Center at (40, 40)
        PlayerTownCenter = new BuildingEntity(
            sim.State.GenerateEntityId(),
            PlayerFaction,
            "town_center",
            new Vector2D(40f, 40f),
            new Vector2D(4f, 4f),
            maxHealth: 1500f,
            populationProvided: 15,
            startsConstructed: true);
        sim.State.AddBuilding(PlayerTownCenter);

        // 3. Player Barracks at (32, 40)
        PlayerBarracks = new BuildingEntity(
            sim.State.GenerateEntityId(),
            PlayerFaction,
            "barracks",
            new Vector2D(32f, 40f),
            new Vector2D(3f, 3f),
            maxHealth: 800f,
            startsConstructed: true);
        sim.State.AddBuilding(PlayerBarracks);

        // 4. Resource Nodes surrounding base
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Wood, new Vector2D(48f, 35f), maxAmount: 500));
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Wood, new Vector2D(52f, 38f), maxAmount: 500));
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Food, new Vector2D(35f, 48f), maxAmount: 400));
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Gold, new Vector2D(46f, 48f), maxAmount: 600));
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Stone, new Vector2D(28f, 48f), maxAmount: 400));
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Iron, new Vector2D(28f, 32f), maxAmount: 350));

        // 5. Spawn 4 Celtic Villagers
        for (int i = 0; i < 4; i++)
        {
            var vId = sim.State.GenerateEntityId();
            var villager = new UnitEntity(
                vId,
                PlayerFaction,
                "celtic_villager",
                new Vector2D(38f + (i * 1.5f), 44f),
                maxHealth: 60f,
                attackDamage: 5f,
                movementSpeed: 3.5f,
                workerState: new WorkerGatherState(carryCapacity: 10, buildPowerPerTick: 1.5f));
            sim.State.AddUnit(villager);
            sim.SpatialGrid.Insert(villager.Id, villager.Position);
        }

        // 6. Spawn Celtic Hero (Brennus) at (40, 52)
        var heroId = sim.State.GenerateEntityId();
        var heroState = new HeroState(
            HeroClass.Warlord,
            "Brennus",
            new HeroAttributes(20, 14, 12),
            baseLeadershipCapacity: 20,
            aura: new HeroAura("Warlord's Might", radius: 14f, damageMultiplierBonus: 0.20f, armorBonus: 2f));

        heroState.AddAbility(new HeroAbilityDefinition(
            "war_cry", "War Cry", "Roars battle cry dealing 40 damage in radius.", 30f, 40, 0f, 10f, AbilityTargetType.PointAreaEnemy, AbilityEffectType.Damage, 40f));
        heroState.AddAbility(new HeroAbilityDefinition(
            "heroic_strike", "Heroic Strike", "Crushing strike dealing 75 single-target damage.", 25f, 25, 2.5f, 0f, AbilityTargetType.SingleTargetEnemy, AbilityEffectType.Damage, 75f));

        HeroUnit = new UnitEntity(
            heroId,
            PlayerFaction,
            "celtic_hero",
            new Vector2D(40f, 52f),
            maxHealth: 350f,
            attackDamage: 30f,
            attackRange: 2.0f,
            movementSpeed: 4.2f,
            baseArmor: 4f,
            heroState: heroState);
        sim.State.AddUnit(HeroUnit);
        sim.SpatialGrid.Insert(HeroUnit.Id, HeroUnit.Position);

        // 7. Spawn Celtic Swordsmen Army (8 units) in Line formation
        for (int i = 0; i < 8; i++)
        {
            var uId = sim.State.GenerateEntityId();
            var pos = new Vector2D(35f + (i * 1.5f), 55f);
            var swordsman = new UnitEntity(
                uId,
                PlayerFaction,
                "celtic_swordsman",
                pos,
                maxHealth: 130f,
                attackDamage: 16f,
                attackRange: 1.8f,
                movementSpeed: 3.8f,
                baseArmor: 3f,
                killXpValue: 60,
                formation: FormationType.Line);
            sim.State.AddUnit(swordsman);
            sim.SpatialGrid.Insert(swordsman.Id, swordsman.Position);
            PlayerArmy.Add(swordsman);
            HeroUnit.HeroState?.AttachUnit(swordsman.Id);
        }

        // 8. Spawn Roman Patrol Army (8 Legionaries + 2 Equites) at (80, 55)
        for (int i = 0; i < 8; i++)
        {
            var rId = sim.State.GenerateEntityId();
            var pos = new Vector2D(75f + (i * 1.5f), 55f);
            var legionary = new UnitEntity(
                rId,
                EnemyFaction,
                "roman_legionary",
                pos,
                maxHealth: 140f,
                attackDamage: 15f,
                attackRange: 1.8f,
                movementSpeed: 3.4f,
                baseArmor: 4f,
                killXpValue: 75,
                formation: FormationType.ShieldWall);
            sim.State.AddUnit(legionary);
            sim.SpatialGrid.Insert(legionary.Id, legionary.Position);
            EnemyArmy.Add(legionary);
        }
    }

    public void StepSimulation(int tickCount = 1)
    {
        for (int i = 0; i < tickCount; i++)
        {
            Coordinator.Tick();
            Renderer.UpdateVfxTicks();
        }
    }
}
