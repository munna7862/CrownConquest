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
/// Authored historical battle scenario: Gauls (Celtic Village) vs Romans (Roman Fort).
/// Features a river crossing, fortified outposts, hero leadership, and match end triggers.
/// </summary>
public sealed class HistoricalBattleScenario
{
    public GameCoordinator Coordinator { get; }
    public TerrainTileGrid Terrain { get; }
    public FogOfWarSystem Fog { get; }
    public UnitVoiceBarkPresenter VoiceBarks { get; }
    public PositionalCombatAudioSystem AudioSystem { get; }
    public ProjectilePhysicsSystem Projectiles { get; }
    public CombatVfxPresenter VfxPresenter { get; }

    public BuildingEntity CelticTownCenter { get; private set; } = null!;
    public BuildingEntity RomanTownCenter { get; private set; } = null!;
    public UnitEntity CelticHeroBrennus { get; private set; } = null!;
    public UnitEntity RomanCenturionLeader { get; private set; } = null!;

    public MatchOutcome Outcome { get; private set; } = MatchOutcome.Ongoing;
    public int TicksExecuted { get; private set; }
    public int CelticKills { get; private set; }
    public int RomanKills { get; private set; }
    public int CelticCasualties { get; private set; }
    public int RomanCasualties { get; private set; }
    public int UnitsTrained { get; private set; }
    public int ResourcesHarvested { get; private set; }

    public HistoricalBattleScenario(int seed = 1904)
    {
        var config = new SimulationConfig
        {
            InitialRandomSeed = seed,
            TicksPerSecond = 20
        };

        Coordinator = new GameCoordinator(config);
        Terrain = new TerrainTileGrid(64, 64, 50f, seed);
        Fog = new FogOfWarSystem(64, 64, 50f);
        VoiceBarks = new UnitVoiceBarkPresenter();
        AudioSystem = new PositionalCombatAudioSystem();
        Projectiles = new ProjectilePhysicsSystem();
        VfxPresenter = new CombatVfxPresenter();

        SubscribeEvents();
        SetupBattlefield();
    }

    private void SubscribeEvents()
    {
        Coordinator.EventBus.Subscribe<UnitKilledEvent>(OnUnitKilled);
        Coordinator.EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        Coordinator.EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        Coordinator.EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
        Coordinator.EventBus.Subscribe<UnitLevelUpEvent>(OnLevelUp);
    }

    private void SetupBattlefield()
    {
        var sim = Coordinator.Simulation;

        // 1. Initialize Terrain & River
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                if (x == 32 || x == 33)
                {
                    if (y >= 26 && y <= 38)
                        Terrain.SetTile(x, y, TerrainTileType.ShallowWater); // Shallow river ford
                    else
                        Terrain.SetTile(x, y, TerrainTileType.DeepWater);
                }
                else if ((x >= 8 && x <= 56) && (y == 16 || y == 48))
                {
                    Terrain.SetTile(x, y, TerrainTileType.CobblestoneRoad);
                }
                else
                {
                    Terrain.SetTile(x, y, (x + y) % 7 == 0 ? TerrainTileType.FlowerGrass : TerrainTileType.Grass);
                }
            }
        }

        // 2. Setup Celtic Settlement (North-West)
        var bankCeltic = sim.State.GetOrCreateResourceBank(FactionId.Player1);
        bankCeltic.Deposit(ResourceType.Food, 600, 0);
        bankCeltic.Deposit(ResourceType.Wood, 600, 0);
        bankCeltic.Deposit(ResourceType.Gold, 400, 0);

        CelticTownCenter = new BuildingEntity(
            sim.State.GenerateEntityId(),
            FactionId.Player1,
            "town_center",
            new Vector2D(600f, 600f),
            new Vector2D(6f, 6f),
            maxHealth: 2400f,
            populationProvided: 15,
            startsConstructed: true);
        sim.State.AddBuilding(CelticTownCenter);

        var celticBarracks = new BuildingEntity(
            sim.State.GenerateEntityId(),
            FactionId.Player1,
            "barracks",
            new Vector2D(750f, 650f),
            new Vector2D(4f, 4f),
            maxHealth: 900f,
            startsConstructed: true);
        sim.State.AddBuilding(celticBarracks);

        var celticTower = new BuildingEntity(
            sim.State.GenerateEntityId(),
            FactionId.Player1,
            "watchtower",
            new Vector2D(900f, 800f),
            new Vector2D(3f, 3f),
            maxHealth: 550f,
            startsConstructed: true);
        sim.State.AddBuilding(celticTower);

        // Celtic Army
        var heroStateCeltic = new HeroState(HeroClass.Warlord, "Lord Brennus", new HeroAttributes(22, 16, 14));
        CelticHeroBrennus = new UnitEntity(
            sim.State.GenerateEntityId(),
            FactionId.Player1,
            "celtic_warlord",
            new Vector2D(800f, 750f),
            maxHealth: 450f,
            attackDamage: 38f,
            baseArmor: 8f,
            movementSpeed: 4.5f,
            attackRange: 2.5f,
            attackCooldownTicks: 16,
            heroState: heroStateCeltic);
        sim.State.AddUnit(CelticHeroBrennus);

        for (int i = 0; i < 6; i++)
        {
            var swordsman = new UnitEntity(
                sim.State.GenerateEntityId(),
                FactionId.Player1,
                "celtic_swordsman",
                new Vector2D(850f + (i * 25f), 700f + (i * 20f)),
                maxHealth: 130f,
                attackDamage: 16f,
                baseArmor: 3f,
                movementSpeed: 3.8f,
                attackRange: 2.0f,
                attackCooldownTicks: 16);
            sim.State.AddUnit(swordsman);
        }

        for (int i = 0; i < 4; i++)
        {
            var archer = new UnitEntity(
                sim.State.GenerateEntityId(),
                FactionId.Player1,
                "celtic_archer",
                new Vector2D(700f + (i * 30f), 800f + (i * 15f)),
                maxHealth: 85f,
                attackDamage: 15f,
                baseArmor: 1f,
                movementSpeed: 4.0f,
                attackRange: 12.0f,
                attackCooldownTicks: 24,
                attackType: "ranged");
            sim.State.AddUnit(archer);
        }

        for (int i = 0; i < 3; i++)
        {
            var villager = new UnitEntity(
                sim.State.GenerateEntityId(),
                FactionId.Player1,
                "celtic_villager",
                new Vector2D(550f + (i * 25f), 550f + (i * 25f)),
                maxHealth: 65f,
                attackDamage: 6f,
                baseArmor: 0f,
                movementSpeed: 3.5f,
                attackRange: 1.5f,
                attackCooldownTicks: 20,
                workerState: new WorkerGatherState());
            sim.State.AddUnit(villager);
        }

        // 3. Setup Roman Fort (South-East)
        var bankRoman = sim.State.GetOrCreateResourceBank(FactionId.Player2);
        bankRoman.Deposit(ResourceType.Food, 800, 0);
        bankRoman.Deposit(ResourceType.Wood, 800, 0);
        bankRoman.Deposit(ResourceType.Gold, 600, 0);

        RomanTownCenter = new BuildingEntity(
            sim.State.GenerateEntityId(),
            FactionId.Player2,
            "praetorium_fortress",
            new Vector2D(2400f, 2400f),
            new Vector2D(6f, 6f),
            maxHealth: 2600f,
            populationProvided: 15,
            startsConstructed: true);
        sim.State.AddBuilding(RomanTownCenter);

        var romanBarracks = new BuildingEntity(
            sim.State.GenerateEntityId(),
            FactionId.Player2,
            "legion_barracks",
            new Vector2D(2250f, 2350f),
            new Vector2D(4.5f, 4.5f),
            maxHealth: 1000f,
            startsConstructed: true);
        sim.State.AddBuilding(romanBarracks);

        var romanTower = new BuildingEntity(
            sim.State.GenerateEntityId(),
            FactionId.Player2,
            "ballista_tower",
            new Vector2D(2100f, 2200f),
            new Vector2D(3.5f, 3.5f),
            maxHealth: 650f,
            startsConstructed: true);
        sim.State.AddBuilding(romanTower);

        // Roman Army
        var heroStateRoman = new HeroState(HeroClass.Centurion, "Centurion Leader", new HeroAttributes(24, 14, 16));
        RomanCenturionLeader = new UnitEntity(
            sim.State.GenerateEntityId(),
            FactionId.Player2,
            "roman_centurion",
            new Vector2D(2200f, 2250f),
            maxHealth: 480f,
            attackDamage: 36f,
            baseArmor: 10f,
            movementSpeed: 3.8f,
            attackRange: 2.5f,
            attackCooldownTicks: 18,
            heroState: heroStateRoman);
        sim.State.AddUnit(RomanCenturionLeader);

        for (int i = 0; i < 6; i++)
        {
            var legionary = new UnitEntity(
                sim.State.GenerateEntityId(),
                FactionId.Player2,
                "roman_legionary",
                new Vector2D(2150f - (i * 25f), 2200f - (i * 20f)),
                maxHealth: 145f,
                attackDamage: 16f,
                baseArmor: 4f,
                movementSpeed: 3.6f,
                attackRange: 2.0f,
                attackCooldownTicks: 16);
            sim.State.AddUnit(legionary);
        }

        for (int i = 0; i < 4; i++)
        {
            var archer = new UnitEntity(
                sim.State.GenerateEntityId(),
                FactionId.Player2,
                "roman_sagittarius",
                new Vector2D(2300f - (i * 30f), 2100f - (i * 15f)),
                maxHealth: 90f,
                attackDamage: 14f,
                baseArmor: 2f,
                movementSpeed: 3.8f,
                attackRange: 12.0f,
                attackCooldownTicks: 24,
                attackType: "ranged");
            sim.State.AddUnit(archer);
        }

        for (int i = 0; i < 2; i++)
        {
            var catapult = new UnitEntity(
                sim.State.GenerateEntityId(),
                FactionId.Player2,
                "roman_catapult",
                new Vector2D(2450f - (i * 40f), 2150f),
                maxHealth: 220f,
                attackDamage: 45f,
                baseArmor: 2f,
                movementSpeed: 2.2f,
                attackRange: 20.0f,
                attackCooldownTicks: 40,
                attackType: "siege");
            sim.State.AddUnit(catapult);
        }

        // Initial Fog Update for Player 1
        Fog.UpdateVision(sim.State.ActiveUnits, sim.State.ActiveBuildings);
    }

    /// <summary>
    /// Simulates fixed ticks and updates presentation systems.
    /// </summary>
    public void SimulateTicks(int tickCount)
    {
        for (int t = 0; t < tickCount; t++)
        {
            Coordinator.Simulation.Tick();
            TicksExecuted++;

            // Step Projectiles
            Projectiles.Tick(OnProjectileImpact);

            // Update Fog of War
            Fog.UpdateVision(Coordinator.Simulation.State.ActiveUnits, Coordinator.Simulation.State.ActiveBuildings);

            // Evaluate Win/Loss Conditions
            EvaluateMatchOutcome();

            if (Outcome != MatchOutcome.Ongoing)
            {
                break;
            }
        }
    }

    private void EvaluateMatchOutcome()
    {
        if (RomanTownCenter.CurrentHealth <= 0 || !RomanTownCenter.IsAlive)
        {
            Outcome = MatchOutcome.Victory;
        }
        else if (CelticTownCenter.CurrentHealth <= 0 || !CelticTownCenter.IsAlive)
        {
            Outcome = MatchOutcome.Defeat;
        }
    }

    public MatchResultSummaryViewModel GetMatchSummary()
    {
        return MatchResultPresenter.CreateSummary(
            playerFaction: FactionId.Player1,
            outcome: Outcome,
            totalTicks: TicksExecuted,
            kills: CelticKills,
            casualties: CelticCasualties,
            unitsTrained: UnitsTrained,
            resourcesHarvested: ResourcesHarvested,
            mvpHeroName: "Brennus, Chieftain of the Senones",
            mvpHeroLevel: CelticHeroBrennus.Veterancy.Level,
            mvpHeroKills: CelticHeroBrennus.Veterancy.KillCount);
    }

    private void OnUnitKilled(in UnitKilledEvent evt)
    {
        if (evt.CasualtyFaction == FactionId.Player1)
        {
            CelticCasualties++;
            RomanKills++;
        }
        else
        {
            RomanCasualties++;
            CelticKills++;
        }

        VfxPresenter.PushParticle(CombatVfxPresenter.CreateBloodSplashDescriptor(evt.DeathPosition, evt.SimulationTick));
        AudioSystem.TryQueueAudioCue("sfx_unit_death", evt.DeathPosition, new Vector2D(800f, 800f), 0.8f, 1.0f, evt.SimulationTick);
    }

    private void OnDamageDealt(in DamageDealtEvent evt)
    {
        VfxPresenter.PushParticle(CombatVfxPresenter.CreateHitSparkDescriptor(new Vector2D(800f, 800f), new Vector2D(1f, 0f), evt.DamageAmount, 42, evt.SimulationTick));
        AudioSystem.TryQueueAudioCue("sfx_sword_clash", new Vector2D(800f, 800f), new Vector2D(800f, 800f), 0.7f, 1.0f, evt.SimulationTick);
    }

    private void OnBuildingCompleted(in BuildingCompletedEvent evt)
    {
        AudioSystem.TryQueueAudioCue("sfx_building_complete", evt.Position, new Vector2D(800f, 800f), 0.8f, 1.0f, evt.SimulationTick);
    }

    private void OnUnitSpawned(in UnitSpawnedEvent evt)
    {
        if (evt.FactionId == FactionId.Player1) UnitsTrained++;
    }

    private void OnLevelUp(in UnitLevelUpEvent evt)
    {
        VfxPresenter.PushParticle(CombatVfxPresenter.CreateLevelUpRuneDescriptor(new Vector2D(800f, 800f), evt.NewLevel, evt.SimulationTick));
        AudioSystem.TryQueueAudioCue("sfx_levelup_fanfare", new Vector2D(800f, 800f), new Vector2D(800f, 800f), 0.9f, 1.0f, evt.SimulationTick);
    }

    private void OnProjectileImpact(ActiveProjectile projectile)
    {
        VfxPresenter.PushParticle(CombatVfxPresenter.CreateImpactDebrisDescriptor(projectile.Target, projectile.ApexHeight / 40f, (ulong)TicksExecuted));
        AudioSystem.TryQueueAudioCue("sfx_catapult_impact", projectile.Target, new Vector2D(800f, 800f), 0.9f, 1.0f, (ulong)TicksExecuted);
    }
}
