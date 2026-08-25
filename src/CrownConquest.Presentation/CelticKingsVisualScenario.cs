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
/// Authoritative Celtic Kings visual scenario orchestrating multi-layered terrain tilemaps,
/// illustrated Celtic & Roman buildings, 8-directional animated units, natural resource foliage,
/// and dynamic Fog of War line-of-sight shading.
/// </summary>
public sealed class CelticKingsVisualScenario
{
    public GameCoordinator Coordinator { get; }
    public SelectionManager Selection { get; }
    public RtsCameraController Camera { get; }
    public GameViewRenderer Renderer { get; }
    public InteractiveRtsHud Hud { get; }
    public TerrainTileGrid Terrain { get; }
    public FogOfWarSystem FogOfWar { get; }

    public FactionId PlayerFaction { get; } = FactionId.Player1; // Celtic
    public FactionId EnemyFaction { get; } = FactionId.Player2;  // Roman

    public UnitEntity HeroBrennus { get; private set; } = null!;
    public BuildingEntity CelticTownCenter { get; private set; } = null!;
    public BuildingEntity CelticBarracks { get; private set; } = null!;
    public BuildingEntity CelticBlacksmith { get; private set; } = null!;
    public BuildingEntity RomanPraetorium { get; private set; } = null!;

    public List<UnitEntity> CelticArmy { get; } = new(32);
    public List<UnitEntity> RomanArmy { get; } = new(32);

    public CelticKingsVisualScenario(int seed = 1818)
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

        // 100x100 grid of 2m tiles = 200m x 200m map
        Terrain = new TerrainTileGrid(100, 100, tileSize: 2.0f, seed: seed);
        FogOfWar = new FogOfWarSystem(100, 100, cellSize: 2.0f);

        BuildTerrainFeatures();
        SetupBattlefield();
        UpdateFogOfWar();
    }

    private void BuildTerrainFeatures()
    {
        // 1. Cobblestone military road connecting Celtic Base (40, 40) to Roman Base (140, 140)
        for (int i = 15; i < 85; i++)
        {
            Terrain.SetTile(i, 25, TerrainTileType.CobblestoneRoad);
            Terrain.SetTile(25, i, TerrainTileType.CobblestoneRoad);
            if (i >= 25 && i <= 70)
            {
                Terrain.SetTile(i, i, TerrainTileType.DirtRoad);
            }
        }

        // 2. River water body (from (60, 0) down to (60, 100)) with Shallow fords
        for (int y = 0; y < 100; y++)
        {
            if (y >= 45 && y <= 55) // Shallow ford
            {
                Terrain.SetTile(58, y, TerrainTileType.ShallowWater);
                Terrain.SetTile(59, y, TerrainTileType.ShallowWater);
                Terrain.SetTile(60, y, TerrainTileType.ShallowWater);
            }
            else
            {
                Terrain.SetTile(58, y, TerrainTileType.ShallowWater);
                Terrain.SetTile(59, y, TerrainTileType.DeepWater);
                Terrain.SetTile(60, y, TerrainTileType.DeepWater);
                Terrain.SetTile(61, y, TerrainTileType.ShallowWater);
            }
        }

        // 3. Stone Cliff Elevations in North-East and South-West
        for (int cx = 75; cx <= 90; cx++)
        {
            for (int cy = 10; cy <= 25; cy++)
            {
                if (cx == 75 || cx == 90 || cy == 10 || cy == 25)
                    Terrain.SetTile(cx, cy, TerrainTileType.CliffElevation);
            }
        }

        Terrain.RecomputeAllBitmasks();
    }

    private void SetupBattlefield()
    {
        var sim = Coordinator.Simulation;
        ulong tick = Coordinator.CurrentTick;

        // 1. Player Economy Stockpile
        var playerBank = Coordinator.GetResourceBank(PlayerFaction);
        playerBank.Deposit(ResourceType.Food, 600, tick);
        playerBank.Deposit(ResourceType.Wood, 600, tick);
        playerBank.Deposit(ResourceType.Gold, 400, tick);
        playerBank.Deposit(ResourceType.Stone, 300, tick);
        playerBank.Deposit(ResourceType.Iron, 200, tick);

        // 2. Celtic Buildings
        CelticTownCenter = new BuildingEntity(
            sim.State.GenerateEntityId(),
            PlayerFaction,
            "town_center",
            new Vector2D(40f, 40f),
            new Vector2D(5f, 5f),
            maxHealth: 1600f,
            populationProvided: 15,
            startsConstructed: true);
        sim.State.AddBuilding(CelticTownCenter);

        CelticBarracks = new BuildingEntity(
            sim.State.GenerateEntityId(),
            PlayerFaction,
            "barracks",
            new Vector2D(30f, 40f),
            new Vector2D(4f, 4f),
            maxHealth: 900f,
            startsConstructed: true);
        sim.State.AddBuilding(CelticBarracks);

        CelticBlacksmith = new BuildingEntity(
            sim.State.GenerateEntityId(),
            PlayerFaction,
            "blacksmith",
            new Vector2D(30f, 30f),
            new Vector2D(3.5f, 3.5f),
            maxHealth: 700f,
            startsConstructed: true);
        sim.State.AddBuilding(CelticBlacksmith);

        // 3. Roman Fortress
        RomanPraetorium = new BuildingEntity(
            sim.State.GenerateEntityId(),
            EnemyFaction,
            "praetorium_fortress",
            new Vector2D(140f, 140f),
            new Vector2D(6f, 6f),
            maxHealth: 2200f,
            startsConstructed: true);
        sim.State.AddBuilding(RomanPraetorium);

        // 4. Natural Foliage & Resources
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Wood, new Vector2D(48f, 32f), maxAmount: 600));
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Wood, new Vector2D(52f, 35f), maxAmount: 600));
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Food, new Vector2D(35f, 48f), maxAmount: 400));
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Gold, new Vector2D(46f, 48f), maxAmount: 700));
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Stone, new Vector2D(25f, 48f), maxAmount: 500));
        sim.State.AddResourceNode(new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Iron, new Vector2D(25f, 25f), maxAmount: 400));

        // 5. Celtic Villagers
        for (int i = 0; i < 4; i++)
        {
            var vId = sim.State.GenerateEntityId();
            var villager = new UnitEntity(
                vId,
                PlayerFaction,
                "celtic_villager",
                new Vector2D(38f + (i * 1.5f), 43f),
                maxHealth: 65f,
                attackDamage: 6f,
                movementSpeed: 3.5f,
                workerState: new WorkerGatherState(carryCapacity: 10, buildPowerPerTick: 1.5f));
            sim.State.AddUnit(villager);
            sim.SpatialGrid.Insert(villager.Id, villager.Position);
            CelticArmy.Add(villager);
        }

        // 6. Hero Brennus
        var heroId = sim.State.GenerateEntityId();
        var heroState = new HeroState(
            HeroClass.Warlord,
            "Lord Brennus",
            new HeroAttributes(22, 16, 14),
            baseLeadershipCapacity: 24,
            aura: new HeroAura("Warlord's Fury", radius: 16f, damageMultiplierBonus: 0.25f, armorBonus: 3f));

        HeroBrennus = new UnitEntity(
            heroId,
            PlayerFaction,
            "celtic_hero_brennus",
            new Vector2D(40f, 52f),
            maxHealth: 400f,
            attackDamage: 35f,
            attackRange: 2.2f,
            movementSpeed: 4.2f,
            baseArmor: 5f,
            heroState: heroState);
        sim.State.AddUnit(HeroBrennus);
        sim.SpatialGrid.Insert(HeroBrennus.Id, HeroBrennus.Position);
        CelticArmy.Add(HeroBrennus);

        // 7. Celtic Swordsmen & Archers
        for (int i = 0; i < 6; i++)
        {
            var uId = sim.State.GenerateEntityId();
            var swordsman = new UnitEntity(
                uId,
                PlayerFaction,
                "celtic_swordsman",
                new Vector2D(35f + (i * 1.5f), 55f),
                maxHealth: 135f,
                attackDamage: 17f,
                attackRange: 1.8f,
                movementSpeed: 3.8f,
                baseArmor: 3f,
                killXpValue: 60);
            sim.State.AddUnit(swordsman);
            sim.SpatialGrid.Insert(swordsman.Id, swordsman.Position);
            CelticArmy.Add(swordsman);
        }

        // 8. Roman Legionary Patrol
        for (int i = 0; i < 8; i++)
        {
            var rId = sim.State.GenerateEntityId();
            var legionary = new UnitEntity(
                rId,
                EnemyFaction,
                "roman_legionary",
                new Vector2D(110f + (i * 1.5f), 110f),
                maxHealth: 145f,
                attackDamage: 16f,
                attackRange: 1.8f,
                movementSpeed: 3.4f,
                baseArmor: 4f,
                killXpValue: 75);
            sim.State.AddUnit(legionary);
            sim.SpatialGrid.Insert(legionary.Id, legionary.Position);
            RomanArmy.Add(legionary);
        }
    }

    public void UpdateFogOfWar()
    {
        var alliedUnits = new List<UnitEntity>(16);
        var alliedBuildings = new List<BuildingEntity>(8);

        var activeUnits = Coordinator.Simulation.State.ActiveUnits;
        for (int i = 0; i < activeUnits.Count; i++)
        {
            if (activeUnits[i].FactionId == PlayerFaction && activeUnits[i].IsAlive)
            {
                alliedUnits.Add(activeUnits[i]);
            }
        }

        var activeBuildings = Coordinator.Simulation.State.ActiveBuildings;
        for (int i = 0; i < activeBuildings.Count; i++)
        {
            if (activeBuildings[i].FactionId == PlayerFaction && activeBuildings[i].IsAlive)
            {
                alliedBuildings.Add(activeBuildings[i]);
            }
        }

        FogOfWar.UpdateVision(alliedUnits, alliedBuildings);
    }

    public void StepSimulation(int tickCount = 1)
    {
        for (int i = 0; i < tickCount; i++)
        {
            Coordinator.Tick();
            Terrain.UpdateWaveTicks();
            UpdateFogOfWar();
            Renderer.UpdateVfxTicks();
        }
    }
}
