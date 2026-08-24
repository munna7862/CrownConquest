using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.World;

/// <summary>
/// Result data record produced after resolving a tactical RTS battle.
/// </summary>
public sealed class BattleResolutionResult
{
    public FactionId VictorFaction { get; init; }
    public bool AttackerWon { get; init; }
    public int AttackerInitialCount { get; init; }
    public int AttackerSurvivingCount { get; init; }
    public int AttackerCasualties => AttackerInitialCount - AttackerSurvivingCount;
    public int DefenderInitialCount { get; init; }
    public int DefenderSurvivingCount { get; init; }
    public int DefenderCasualties => DefenderInitialCount - DefenderSurvivingCount;
    public bool ProvinceCaptured { get; init; }
    public int ElapsedSimulationTicks { get; init; }
}

/// <summary>
/// Configuration for initiating a tactical battle from strategic armies and province garrisons.
/// </summary>
public sealed class BattleSetup
{
    public StrategicArmy AttackerArmy { get; }
    public StrategicArmy? DefenderArmy { get; }
    public StrategicProvince Province { get; }
    public ulong RandomSeed { get; set; } = 42;

    public BattleSetup(StrategicArmy attackerArmy, StrategicProvince province, StrategicArmy? defenderArmy = null, ulong seed = 42)
    {
        AttackerArmy = attackerArmy;
        Province = province;
        DefenderArmy = defenderArmy;
        RandomSeed = seed;
    }
}

/// <summary>
/// Authoritative simulation bridge that translates Strategic campaign forces into a tactical RTS match,
/// executes deterministic simulation ticks, and returns surviving progression back into strategic models.
/// </summary>
public static class BattleTransitionEngine
{
    public static BattleResolutionResult ExecuteBattle(
        BattleSetup setup,
        int maxTicks = 800,
        DomainEventBus? eventBus = null)
    {
        var bus = eventBus ?? new DomainEventBus();
        var config = new SimulationConfig
        {
            TicksPerSecond = 20,
            InitialRandomSeed = (int)(setup.RandomSeed % int.MaxValue),
            MapWidth = 800f,
            MapHeight = 600f
        };

        var sim = new SimulationEngine(config, bus);
        var attackerMapping = new Dictionary<EntityId, StrategicUnitSpec>();
        var defenderMapping = new Dictionary<EntityId, StrategicUnitSpec>();
        var attackerEntities = new List<UnitEntity>();
        var defenderEntities = new List<UnitEntity>();

        int nextId = 1;

        // 1. Deploy Attacker Units
        float startY = 100f;
        float ySpacing = 10f;
        int attackerInitialCount = 0;

        for (int i = 0; i < setup.AttackerArmy.Units.Count; i++)
        {
            var spec = setup.AttackerArmy.Units[i];
            if (!spec.IsAlive) continue;

            attackerInitialCount++;
            var entityId = new EntityId(nextId++);
            attackerMapping[entityId] = spec;

            var unit = new UnitEntity(
                id: entityId,
                factionId: setup.AttackerArmy.FactionId,
                unitType: spec.UnitType,
                position: new Vector2D(100f, startY + (i * ySpacing)),
                maxHealth: spec.BaseMaxHealth,
                attackDamage: spec.BaseAttackDamage,
                attackRange: MathF.Max(2.0f, spec.AttackRange),
                movementSpeed: MathF.Max(20f, spec.MoveSpeed),
                attackCooldownTicks: Math.Max(2, (int)MathF.Round(spec.AttackCooldown * 5f)),
                baseArmor: spec.Armor,
                aggroRange: 150f,
                healthPerLevelBonus: spec.HealthPerLevelBonus,
                damagePerLevelBonus: spec.DamagePerLevelBonus,
                archetype: spec.Archetype,
                initialLevel: spec.Level,
                initialXp: spec.CurrentXp,
                initialCurrentHealth: spec.CurrentHealth
            );
            unit.CurrentTerrain = setup.Province.Terrain;
            sim.State.AddUnit(unit);
            sim.SpatialGrid.Insert(unit.Id, unit.Position);
            attackerEntities.Add(unit);
        }

        // Deploy Attacker Hero if present
        UnitEntity? attackerHeroEntity = null;
        if (setup.AttackerArmy.AttachedHero != null)
        {
            var heroSpec = setup.AttackerArmy.AttachedHero;
            var heroId = new EntityId(nextId++);
            attackerInitialCount++;

            var heroState = new HeroState(
                heroClass: heroSpec.Class,
                heroName: heroSpec.HeroName,
                baseAttributes: heroSpec.BaseAttributes
            );
            heroState.CurrentLevel = heroSpec.Level;

            attackerHeroEntity = new UnitEntity(
                id: heroId,
                factionId: setup.AttackerArmy.FactionId,
                unitType: "Hero",
                position: new Vector2D(95f, startY + 5f),
                maxHealth: 300f,
                attackDamage: 30f,
                attackRange: 2.5f,
                movementSpeed: 55f,
                attackCooldownTicks: 3,
                baseArmor: 4f,
                aggroRange: 150f,
                archetype: UnitArchetype.Hero,
                heroState: heroState,
                initialLevel: heroSpec.Level,
                initialXp: heroSpec.CurrentXp
            );
            attackerHeroEntity.CurrentTerrain = setup.Province.Terrain;
            sim.State.AddUnit(attackerHeroEntity);
            sim.SpatialGrid.Insert(attackerHeroEntity.Id, attackerHeroEntity.Position);
            attackerEntities.Add(attackerHeroEntity);
        }

        // 2. Deploy Defender Units & Garrison
        int defenderInitialCount = 0;
        float garrisonDefenseMult = setup.Province.GarrisonDefenseBonus;
        FactionId defenderFaction = setup.DefenderArmy?.FactionId ?? setup.Province.OwnerFaction;

        // Stationed Army units
        if (setup.DefenderArmy != null)
        {
            for (int i = 0; i < setup.DefenderArmy.Units.Count; i++)
            {
                var spec = setup.DefenderArmy.Units[i];
                if (!spec.IsAlive) continue;

                defenderInitialCount++;
                var entityId = new EntityId(nextId++);
                defenderMapping[entityId] = spec;

                float boostedArmor = spec.Armor * garrisonDefenseMult;
                var unit = new UnitEntity(
                    id: entityId,
                    factionId: defenderFaction,
                    unitType: spec.UnitType,
                    position: new Vector2D(105f, startY + (i * ySpacing)),
                    maxHealth: spec.BaseMaxHealth,
                    attackDamage: spec.BaseAttackDamage,
                    attackRange: MathF.Max(2.0f, spec.AttackRange),
                    movementSpeed: MathF.Max(20f, spec.MoveSpeed),
                    attackCooldownTicks: Math.Max(2, (int)MathF.Round(spec.AttackCooldown * 5f)),
                    baseArmor: boostedArmor,
                    aggroRange: 150f,
                    healthPerLevelBonus: spec.HealthPerLevelBonus,
                    damagePerLevelBonus: spec.DamagePerLevelBonus,
                    archetype: spec.Archetype,
                    initialLevel: spec.Level,
                    initialXp: spec.CurrentXp,
                    initialCurrentHealth: spec.CurrentHealth
                );
                unit.CurrentTerrain = setup.Province.Terrain;
                sim.State.AddUnit(unit);
                sim.SpatialGrid.Insert(unit.Id, unit.Position);
                defenderEntities.Add(unit);
            }
        }

        // Province Garrison Units
        for (int i = 0; i < setup.Province.GarrisonUnits.Count; i++)
        {
            var spec = setup.Province.GarrisonUnits[i];
            if (!spec.IsAlive) continue;

            defenderInitialCount++;
            var entityId = new EntityId(nextId++);
            defenderMapping[entityId] = spec;

            float boostedArmor = spec.Armor * garrisonDefenseMult;
            var unit = new UnitEntity(
                id: entityId,
                factionId: defenderFaction,
                unitType: spec.UnitType,
                position: new Vector2D(106f, startY + (i * ySpacing)),
                maxHealth: spec.BaseMaxHealth,
                attackDamage: spec.BaseAttackDamage,
                attackRange: MathF.Max(2.0f, spec.AttackRange),
                movementSpeed: MathF.Max(20f, spec.MoveSpeed),
                attackCooldownTicks: Math.Max(2, (int)MathF.Round(spec.AttackCooldown * 5f)),
                baseArmor: boostedArmor,
                aggroRange: 150f,
                healthPerLevelBonus: spec.HealthPerLevelBonus,
                damagePerLevelBonus: spec.DamagePerLevelBonus,
                archetype: spec.Archetype,
                initialLevel: spec.Level,
                initialXp: spec.CurrentXp,
                initialCurrentHealth: spec.CurrentHealth
            );
            unit.CurrentTerrain = setup.Province.Terrain;
            sim.State.AddUnit(unit);
            sim.SpatialGrid.Insert(unit.Id, unit.Position);
            defenderEntities.Add(unit);
        }

        // Deploy Defender Hero if present
        UnitEntity? defenderHeroEntity = null;
        if (setup.DefenderArmy?.AttachedHero != null)
        {
            var heroSpec = setup.DefenderArmy.AttachedHero;
            var heroId = new EntityId(nextId++);
            defenderInitialCount++;

            var heroState = new HeroState(
                heroClass: heroSpec.Class,
                heroName: heroSpec.HeroName,
                baseAttributes: heroSpec.BaseAttributes
            );
            heroState.CurrentLevel = heroSpec.Level;

            defenderHeroEntity = new UnitEntity(
                id: heroId,
                factionId: defenderFaction,
                unitType: "Hero",
                position: new Vector2D(110f, startY + 5f),
                maxHealth: 300f,
                attackDamage: 30f,
                attackRange: 2.5f,
                movementSpeed: 55f,
                attackCooldownTicks: 3,
                baseArmor: 4f * garrisonDefenseMult,
                aggroRange: 150f,
                archetype: UnitArchetype.Hero,
                heroState: heroState,
                initialLevel: heroSpec.Level,
                initialXp: heroSpec.CurrentXp
            );
            defenderHeroEntity.CurrentTerrain = setup.Province.Terrain;
            sim.State.AddUnit(defenderHeroEntity);
            sim.SpatialGrid.Insert(defenderHeroEntity.Id, defenderHeroEntity.Position);
            defenderEntities.Add(defenderHeroEntity);
        }

        // If defender has 0 units (undefended province), attacker automatically wins
        if (defenderInitialCount == 0)
        {
            setup.Province.OwnerFaction = setup.AttackerArmy.FactionId;
            return new BattleResolutionResult
            {
                VictorFaction = setup.AttackerArmy.FactionId,
                AttackerWon = true,
                AttackerInitialCount = attackerInitialCount,
                AttackerSurvivingCount = attackerInitialCount,
                DefenderInitialCount = 0,
                DefenderSurvivingCount = 0,
                ProvinceCaptured = true,
                ElapsedSimulationTicks = 0
            };
        }

        // Initial attack orders targeting enemy formation
        for (int i = 0; i < attackerEntities.Count; i++)
        {
            var target = defenderEntities[i % defenderEntities.Count];
            attackerEntities[i].Attack(target.Id);
        }
        for (int i = 0; i < defenderEntities.Count; i++)
        {
            var target = attackerEntities[i % attackerEntities.Count];
            defenderEntities[i].Attack(target.Id);
        }

        // 3. Step Simulation until resolution
        int elapsedTicks = 0;
        for (int t = 0; t < maxTicks; t++)
        {
            sim.Tick();
            elapsedTicks++;

            // Count living units
            int aliveAttackers = 0;
            int aliveDefenders = 0;

            for (int i = 0; i < sim.State.ActiveUnits.Count; i++)
            {
                var u = sim.State.ActiveUnits[i];
                if (u.IsAlive)
                {
                    if (u.FactionId == setup.AttackerArmy.FactionId)
                        aliveAttackers++;
                    else
                        aliveDefenders++;
                }
            }

            if (aliveAttackers == 0 || aliveDefenders == 0)
            {
                break;
            }
        }

        // 4. Harvest Survivors and Return Progression
        int attackerSurvivingCount = 0;
        foreach (var kvp in attackerMapping)
        {
            var spec = kvp.Value;
            if (sim.State.TryGetUnit(kvp.Key, out var simUnit) && simUnit != null && simUnit.IsAlive)
            {
                spec.CurrentHealth = simUnit.CurrentHealth;
                spec.Level = simUnit.Veterancy.Level;
                spec.CurrentXp = simUnit.Veterancy.CurrentXp;
                spec.TotalKills += simUnit.Veterancy.KillCount;
                spec.Rank = simUnit.Veterancy.Rank;
                attackerSurvivingCount++;
            }
            else
            {
                spec.CurrentHealth = 0f;
            }
        }

        // Update Attacker Hero
        if (attackerHeroEntity != null && setup.AttackerArmy.AttachedHero != null)
        {
            if (attackerHeroEntity.IsAlive)
            {
                attackerSurvivingCount++;
                setup.AttackerArmy.AttachedHero.Level = attackerHeroEntity.Veterancy.Level;
                setup.AttackerArmy.AttachedHero.CurrentXp = attackerHeroEntity.Veterancy.CurrentXp;
                setup.AttackerArmy.AttachedHero.TotalKills += attackerHeroEntity.Veterancy.KillCount;
            }
            else
            {
                setup.AttackerArmy.AttachedHero = null; // Hero fallen in battle
            }
        }

        // Remove dead attacker units from army
        setup.AttackerArmy.RemoveDeadUnits();

        // Harvest Defender Survivors
        int defenderSurvivingCount = 0;
        foreach (var kvp in defenderMapping)
        {
            var spec = kvp.Value;
            if (sim.State.TryGetUnit(kvp.Key, out var simUnit) && simUnit != null && simUnit.IsAlive)
            {
                spec.CurrentHealth = simUnit.CurrentHealth;
                spec.Level = simUnit.Veterancy.Level;
                spec.CurrentXp = simUnit.Veterancy.CurrentXp;
                spec.TotalKills += simUnit.Veterancy.KillCount;
                spec.Rank = simUnit.Veterancy.Rank;
                defenderSurvivingCount++;
            }
            else
            {
                spec.CurrentHealth = 0f;
            }
        }

        if (defenderHeroEntity != null && setup.DefenderArmy?.AttachedHero != null)
        {
            if (defenderHeroEntity.IsAlive)
            {
                defenderSurvivingCount++;
                setup.DefenderArmy.AttachedHero.Level = defenderHeroEntity.Veterancy.Level;
                setup.DefenderArmy.AttachedHero.CurrentXp = defenderHeroEntity.Veterancy.CurrentXp;
                setup.DefenderArmy.AttachedHero.TotalKills += defenderHeroEntity.Veterancy.KillCount;
            }
            else
            {
                setup.DefenderArmy.AttachedHero = null;
            }
        }

        setup.DefenderArmy?.RemoveDeadUnits();
        setup.Province.GarrisonUnits.RemoveAll(u => !u.IsAlive);

        // 5. Determine Victor & Territorial Conquest
        bool attackerWon = attackerSurvivingCount > 0 && defenderSurvivingCount == 0;
        FactionId victor = attackerWon ? setup.AttackerArmy.FactionId : defenderFaction;
        bool provinceCaptured = false;

        if (attackerWon)
        {
            setup.Province.OwnerFaction = setup.AttackerArmy.FactionId;
            provinceCaptured = true;
        }

        return new BattleResolutionResult
        {
            VictorFaction = victor,
            AttackerWon = attackerWon,
            AttackerInitialCount = attackerInitialCount,
            AttackerSurvivingCount = attackerSurvivingCount,
            DefenderInitialCount = defenderInitialCount,
            DefenderSurvivingCount = defenderSurvivingCount,
            ProvinceCaptured = provinceCaptured,
            ElapsedSimulationTicks = elapsedTicks
        };
    }
}
