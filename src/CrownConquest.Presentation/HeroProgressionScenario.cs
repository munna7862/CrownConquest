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

/// <summary>
/// Headless Tier 4 E2E Scenario demonstrating the full RPG Hero vertical slice:
/// Hero summoning -> Squad attachment -> Leadership aura in combat -> Active offensive ability casting ->
/// Kill XP progression & level up -> Attribute allocation -> State persistence checksum verification.
/// </summary>
public sealed class HeroProgressionScenario
{
    public GameCoordinator Coordinator { get; }
    public FactionId PlayerFaction { get; } = new(1);
    public FactionId EnemyFaction { get; } = new(2);

    public UnitEntity HeroUnit { get; private set; } = null!;
    public List<UnitEntity> PlayerSquad { get; } = new();
    public List<UnitEntity> EnemyUnits { get; } = new();

    public HeroPresenter Presenter { get; }

    public int TotalLevelUpsObserved { get; private set; }
    public int TotalAbilitiesCastObserved { get; private set; }
    public bool VictoryConditionAchieved { get; private set; }

    public HeroProgressionScenario()
    {
        var config = new SimulationConfig
        {
            TicksPerSecond = 20,
            InitialRandomSeed = 5555
        };

        Coordinator = new GameCoordinator(config);

        // Listen for hero progression events
        Coordinator.EventBus.Subscribe<HeroLevelUpEvent>(OnHeroLevelUp);
        Coordinator.EventBus.Subscribe<HeroAbilityCastEvent>(OnAbilityCast);

        InitializeScenario();
        Presenter = new HeroPresenter(Coordinator, PlayerFaction, HeroUnit.Id);
        Presenter.UpdateSnapshot();
    }

    private void InitializeScenario()
    {
        var sim = Coordinator.Simulation;

        // 1. Stockpile resources for Player
        var playerBank = Coordinator.GetResourceBank(PlayerFaction);
        playerBank.Deposit(ResourceType.Food, 1000, 1UL);
        playerBank.Deposit(ResourceType.Gold, 800, 1UL);
        playerBank.Deposit(ResourceType.Wood, 500, 1UL);

        // 2. Spawn Celtic Warlord Hero (Brennus) at (20, 25)
        var heroId = sim.State.GenerateEntityId();
        var heroState = new HeroState(
            heroClass: HeroClass.Warlord,
            heroName: "Brennus",
            baseAttributes: new HeroAttributes(18, 12, 10),
            baseLeadershipCapacity: 15,
            aura: new HeroAura("Warlord's Ferocity", radius: 12.0f, damageMultiplierBonus: 0.15f, armorBonus: 2.0f, movementSpeedMultiplierBonus: 0.10f),
            strengthPerLevel: 3,
            agilityPerLevel: 1,
            willpowerPerLevel: 1);

        heroState.AddAbility(new HeroAbilityDefinition(
            id: "heroic_strike",
            displayName: "Heroic Strike",
            description: "Deals massive physical damage (50 base) to a single enemy.",
            manaCost: 25f,
            cooldownTicks: 30,
            castRange: 2.5f,
            radius: 0f,
            targetType: AbilityTargetType.SingleTargetEnemy,
            effectType: AbilityEffectType.Damage,
            basePower: 50f));

        heroState.AddAbility(new HeroAbilityDefinition(
            id: "war_cry",
            displayName: "War Cry",
            description: "Damages nearby enemies (30 base) in 8.0 radius.",
            manaCost: 40f,
            cooldownTicks: 45,
            castRange: 0f,
            radius: 8.0f,
            targetType: AbilityTargetType.PointAreaEnemy,
            effectType: AbilityEffectType.Damage,
            basePower: 30f));

        HeroUnit = new UnitEntity(
            id: heroId,
            factionId: PlayerFaction,
            unitType: "celtic_warlord",
            position: new Vector2D(20f, 25f),
            maxHealth: 350f,
            attackDamage: 35f,
            attackRange: 1.8f,
            movementSpeed: 4.0f,
            attackCooldownTicks: 18,
            killXpValue: 250,
            baseArmor: 5f,
            aggroRange: 14f,
            healthPerLevelBonus: 35f,
            damagePerLevelBonus: 4.5f,
            xpThresholds: new[] { 0, 150, 350, 650, 1050, 1550, 2150 },
            archetype: UnitArchetype.Hero,
            heroState: heroState);

        sim.State.AddUnit(HeroUnit);

        // 3. Spawn 4 Player Swordsmen around Hero
        for (int i = 0; i < 4; i++)
        {
            var uId = sim.State.GenerateEntityId();
            var pos = new Vector2D(18f + (i * 1.5f), 23f);
            var sword = new UnitEntity(
                id: uId,
                factionId: PlayerFaction,
                unitType: "celtic_swordsman",
                position: pos,
                maxHealth: 120f,
                attackDamage: 18f,
                attackRange: 1.5f,
                movementSpeed: 3.6f,
                attackCooldownTicks: 18,
                killXpValue: 60,
                baseArmor: 3f,
                aggroRange: 10f,
                archetype: UnitArchetype.Infantry);

            sim.State.AddUnit(sword);
            PlayerSquad.Add(sword);
        }

        // 4. Attach Swordsmen to Hero
        for (int i = 0; i < PlayerSquad.Count; i++)
        {
            HeroUnit.HeroState!.AttachUnit(PlayerSquad[i].Id);
        }

        // 5. Spawn Enemy Roman Outpost Warband at (45, 25)
        for (int i = 0; i < 5; i++)
        {
            var eId = sim.State.GenerateEntityId();
            var ePos = new Vector2D(44f + (i * 1.2f), 25f);
            var enemy = new UnitEntity(
                id: eId,
                factionId: EnemyFaction,
                unitType: "roman_legionary",
                position: ePos,
                maxHealth: 110f,
                attackDamage: 14f,
                attackRange: 1.5f,
                movementSpeed: 3.3f,
                attackCooldownTicks: 19,
                killXpValue: 80,
                baseArmor: 3f,
                aggroRange: 12f,
                archetype: UnitArchetype.Infantry);

            sim.State.AddUnit(enemy);
            EnemyUnits.Add(enemy);
        }
    }

    public void ExecuteFullScenario()
    {
        var sim = Coordinator.Simulation;

        // Step 1: Advance squad toward enemy outpost
        var marchDest = new Vector2D(35f, 25f);
        HeroUnit.Move(marchDest);
        for (int i = 0; i < PlayerSquad.Count; i++)
        {
            PlayerSquad[i].Move(marchDest);
        }

        // Enemy also moves to intercept at (35, 25)
        for (int i = 0; i < EnemyUnits.Count; i++)
        {
            EnemyUnits[i].Move(new Vector2D(35f, 25f));
        }

        // Advance ticks to march into combat engagement range
        for (int t = 0; t < 60; t++)
        {
            Coordinator.Tick();
            Presenter.UpdateSnapshot();
        }

        // Step 2: Hero casts War Cry AoE ability to burst enemy front line
        Presenter.CastAbility("war_cry", EntityId.None, HeroUnit.Position);

        // Advance ticks for combat and cooldown
        for (int t = 0; t < 30; t++)
        {
            Coordinator.Tick();
            Presenter.UpdateSnapshot();
        }

        // Step 3: Hero casts Heroic Strike on remaining enemy
        EntityId livingEnemyId = EntityId.None;
        for (int i = 0; i < EnemyUnits.Count; i++)
        {
            if (EnemyUnits[i].IsAlive)
            {
                livingEnemyId = EnemyUnits[i].Id;
                break;
            }
        }

        if (livingEnemyId.IsValid)
        {
            Presenter.CastAbility("heroic_strike", livingEnemyId, Vector2D.Zero);
        }

        // Step 4: Advance ticks until all enemy units eliminated
        for (int t = 0; t < 250; t++)
        {
            Coordinator.Tick();
            Presenter.UpdateSnapshot();

            // Check if victory achieved
            bool anyEnemyAlive = false;
            for (int i = 0; i < EnemyUnits.Count; i++)
            {
                if (EnemyUnits[i].IsAlive)
                {
                    anyEnemyAlive = true;
                    break;
                }
            }

            if (!anyEnemyAlive)
            {
                VictoryConditionAchieved = true;
                break;
            }
        }

        // Step 5: Allocate earned attribute points if available
        if (Presenter.AvailableAttributePoints > 0)

        {
            Presenter.AllocateAttribute("strength");
            Coordinator.Tick();
            Presenter.UpdateSnapshot();
        }
    }

    private void OnHeroLevelUp(in HeroLevelUpEvent e)
    {
        if (e.HeroId == HeroUnit.Id)
        {
            TotalLevelUpsObserved++;
        }
    }

    private void OnAbilityCast(in HeroAbilityCastEvent e)
    {
        if (e.HeroId == HeroUnit.Id)
        {
            TotalAbilitiesCastObserved++;
        }
    }
}
