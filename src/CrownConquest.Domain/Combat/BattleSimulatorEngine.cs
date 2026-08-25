using System;
using System.Collections.Generic;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Headless, high-performance deterministic battle simulation engine.
/// Simulates isolated combat engagements between custom army rosters, gathers battle telemetry,
/// computes damage/survival metrics, and validates deterministic replay integrity.
/// </summary>
public sealed class BattleSimulatorEngine
{
    public BattleSimulatorResult ExecuteBattle(BattleSimulatorConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var simConfig = new SimulationConfig
        {
            InitialRandomSeed = config.RandomSeed,
            TicksPerSecond = 20
        };

        var eventBus = new DomainEventBus();
        var sim = new SimulationEngine(simConfig, eventBus);

        // Tracking metrics
        float totalDamageA = 0f;
        float totalDamageB = 0f;
        int totalXpA = 0;
        int totalXpB = 0;

        var damageDealtPerArchetypeA = new Dictionary<UnitArchetype, float>();
        var damageDealtPerArchetypeB = new Dictionary<UnitArchetype, float>();
        var damageTakenPerArchetypeA = new Dictionary<UnitArchetype, float>();
        var damageTakenPerArchetypeB = new Dictionary<UnitArchetype, float>();
        var killsPerArchetypeA = new Dictionary<UnitArchetype, int>();
        var killsPerArchetypeB = new Dictionary<UnitArchetype, int>();
        var deathsPerArchetypeA = new Dictionary<UnitArchetype, int>();
        var deathsPerArchetypeB = new Dictionary<UnitArchetype, int>();
        var initialCountPerArchetypeA = new Dictionary<UnitArchetype, int>();
        var initialCountPerArchetypeB = new Dictionary<UnitArchetype, int>();

        // Register domain event listeners for damage, kills, and XP
        eventBus.Subscribe<DamageDealtEvent>((in DamageDealtEvent e) =>
        {
            if (sim.State.TryGetUnit(e.TargetId, out var target) && target != null)
            {
                if (target.FactionId == config.TeamA.FactionId)
                {
                    totalDamageB += e.DamageAmount;
                    damageTakenPerArchetypeA[target.Archetype] = damageTakenPerArchetypeA.GetValueOrDefault(target.Archetype) + e.DamageAmount;
                }
                else if (target.FactionId == config.TeamB.FactionId)
                {
                    totalDamageA += e.DamageAmount;
                    damageTakenPerArchetypeB[target.Archetype] = damageTakenPerArchetypeB.GetValueOrDefault(target.Archetype) + e.DamageAmount;
                }
            }

            if (sim.State.TryGetUnit(e.AttackerId, out var attacker) && attacker != null)
            {
                if (attacker.FactionId == config.TeamA.FactionId)
                {
                    damageDealtPerArchetypeA[attacker.Archetype] = damageDealtPerArchetypeA.GetValueOrDefault(attacker.Archetype) + e.DamageAmount;
                }
                else if (attacker.FactionId == config.TeamB.FactionId)
                {
                    damageDealtPerArchetypeB[attacker.Archetype] = damageDealtPerArchetypeB.GetValueOrDefault(attacker.Archetype) + e.DamageAmount;
                }
            }
        });

        eventBus.Subscribe<UnitKilledEvent>((in UnitKilledEvent e) =>
        {
            if (sim.State.TryGetUnit(e.KillerId, out var killer) && killer != null)
            {
                if (killer.FactionId == config.TeamA.FactionId)
                {
                    killsPerArchetypeA[killer.Archetype] = killsPerArchetypeA.GetValueOrDefault(killer.Archetype) + 1;
                }
                else if (killer.FactionId == config.TeamB.FactionId)
                {
                    killsPerArchetypeB[killer.Archetype] = killsPerArchetypeB.GetValueOrDefault(killer.Archetype) + 1;
                }
            }
        });

        eventBus.Subscribe<UnitGainedXpEvent>((in UnitGainedXpEvent e) =>
        {
            if (sim.State.TryGetUnit(e.UnitId, out var unit) && unit != null)
            {
                if (unit.FactionId == config.TeamA.FactionId)
                {
                    totalXpA += e.XpGained;
                }
                else if (unit.FactionId == config.TeamB.FactionId)
                {
                    totalXpB += e.XpGained;
                }
            }
        });

        // Spawn Team A
        var spawnedA = SpawnRoster(sim, config.TeamA, initialCountPerArchetypeA);
        // Spawn Team B
        var spawnedB = SpawnRoster(sim, config.TeamB, initialCountPerArchetypeB);

        int initialUnitsA = spawnedA.Count;
        int initialUnitsB = spawnedB.Count;

        // Auto engage: units start Idle and auto-acquire targets within aggro range via SpatialGrid
        // No manual move commands needed as spatial grid target acquisition handles battle initiation

        // Simulate ticks until resolution or timeout
        ulong maxTicks = (ulong)config.MaxTicks;
        ulong tick = 0;

        while (tick < maxTicks)
        {
            sim.Tick();
            tick++;

            // Periodically refresh aggro/attack targets if needed
            if (tick % 20 == 0)
            {
                // Count alive units
                int aliveA = 0;
                int aliveB = 0;
                for (int i = 0; i < sim.State.ActiveUnits.Count; i++)
                {
                    var u = sim.State.ActiveUnits[i];
                    if (u.IsAlive)
                    {
                        if (u.FactionId == config.TeamA.FactionId) aliveA++;
                        else if (u.FactionId == config.TeamB.FactionId) aliveB++;
                    }
                }

                if (aliveA == 0 || aliveB == 0)
                {
                    break;
                }
            }
        }

        // Aggregate final metrics
        int survivingA = 0;
        int survivingB = 0;
        float survivingHpA = 0f;
        float survivingHpB = 0f;

        var survivingPerArchetypeA = new Dictionary<UnitArchetype, int>();
        var survivingPerArchetypeB = new Dictionary<UnitArchetype, int>();

        for (int i = 0; i < sim.State.ActiveUnits.Count; i++)
        {
            var u = sim.State.ActiveUnits[i];
            if (u.IsAlive)
            {
                if (u.FactionId == config.TeamA.FactionId)
                {
                    survivingA++;
                    survivingHpA += u.CurrentHealth;
                    survivingPerArchetypeA[u.Archetype] = survivingPerArchetypeA.GetValueOrDefault(u.Archetype) + 1;
                }
                else if (u.FactionId == config.TeamB.FactionId)
                {
                    survivingB++;
                    survivingHpB += u.CurrentHealth;
                    survivingPerArchetypeB[u.Archetype] = survivingPerArchetypeB.GetValueOrDefault(u.Archetype) + 1;
                }
            }
        }

        int casualtiesA = initialUnitsA - survivingA;
        int casualtiesB = initialUnitsB - survivingB;

        // Build archetype statistics
        var archetypeStatsA = new Dictionary<UnitArchetype, ArchetypeBattleMetrics>();
        foreach (var (arch, initCount) in initialCountPerArchetypeA)
        {
            int surv = survivingPerArchetypeA.GetValueOrDefault(arch, 0);
            int deaths = initCount - surv;
            archetypeStatsA[arch] = new ArchetypeBattleMetrics(
                arch,
                initCount,
                surv,
                killsPerArchetypeA.GetValueOrDefault(arch, 0),
                deaths,
                damageDealtPerArchetypeA.GetValueOrDefault(arch, 0f),
                damageTakenPerArchetypeA.GetValueOrDefault(arch, 0f),
                0);
        }

        var archetypeStatsB = new Dictionary<UnitArchetype, ArchetypeBattleMetrics>();
        foreach (var (arch, initCount) in initialCountPerArchetypeB)
        {
            int surv = survivingPerArchetypeB.GetValueOrDefault(arch, 0);
            int deaths = initCount - surv;
            archetypeStatsB[arch] = new ArchetypeBattleMetrics(
                arch,
                initCount,
                surv,
                killsPerArchetypeB.GetValueOrDefault(arch, 0),
                deaths,
                damageDealtPerArchetypeB.GetValueOrDefault(arch, 0f),
                damageTakenPerArchetypeB.GetValueOrDefault(arch, 0f),
                0);
        }

        FactionId? winner = null;
        bool isDraw = false;

        if (survivingA > 0 && survivingB == 0)
        {
            winner = config.TeamA.FactionId;
        }
        else if (survivingB > 0 && survivingA == 0)
        {
            winner = config.TeamB.FactionId;
        }
        else if (survivingA > 0 && survivingB > 0)
        {
            if (survivingHpA > survivingHpB) winner = config.TeamA.FactionId;
            else if (survivingHpB > survivingHpA) winner = config.TeamB.FactionId;
            else isDraw = true;
        }
        else
        {
            isDraw = true;
        }

        float tradeEfficiencyA = casualtiesA > 0 ? (float)casualtiesB / casualtiesA : (casualtiesB > 0 ? 10f : 1f);
        float tradeEfficiencyB = casualtiesB > 0 ? (float)casualtiesA / casualtiesB : (casualtiesA > 0 ? 10f : 1f);

        ulong finalChecksum = sim.State.ComputeStateChecksum();

        return new BattleSimulatorResult(
            winner,
            isDraw,
            tick,
            tick / 20.0f,
            initialUnitsA,
            initialUnitsB,
            survivingA,
            survivingB,
            casualtiesA,
            casualtiesB,
            survivingHpA,
            survivingHpB,
            totalDamageA,
            totalDamageB,
            totalXpA,
            totalXpB,
            tradeEfficiencyA,
            tradeEfficiencyB,
            finalChecksum,
            archetypeStatsA,
            archetypeStatsB);
    }

    /// <summary>
    /// Executes dual identical runs of the same battle and asserts 100% deterministic bit-for-bit parity.
    /// </summary>
    public bool VerifyDeterministicReplay(BattleSimulatorConfig config, out string divergenceReason)
    {
        var run1 = ExecuteBattle(config);
        var run2 = ExecuteBattle(config);

        if (run1.DurationTicks != run2.DurationTicks)
        {
            divergenceReason = $"Duration tick mismatch: Run 1 = {run1.DurationTicks}, Run 2 = {run2.DurationTicks}";
            return false;
        }

        if (run1.FinalStateChecksum != run2.FinalStateChecksum)
        {
            divergenceReason = $"Checksum mismatch: Run 1 = {run1.FinalStateChecksum}, Run 2 = {run2.FinalStateChecksum}";
            return false;
        }

        if (run1.SurvivingUnitsA != run2.SurvivingUnitsA || run1.SurvivingUnitsB != run2.SurvivingUnitsB)
        {
            divergenceReason = $"Surviving units mismatch: A({run1.SurvivingUnitsA} vs {run2.SurvivingUnitsA}), B({run1.SurvivingUnitsB} vs {run2.SurvivingUnitsB})";
            return false;
        }

        divergenceReason = string.Empty;
        return true;
    }

    private static List<UnitEntity> SpawnRoster(
        SimulationEngine sim,
        ArmyRosterConfig roster,
        Dictionary<UnitArchetype, int> initialCountMap)
    {
        var resultList = new List<UnitEntity>();

        int totalUnits = 0;
        for (int i = 0; i < roster.Units.Count; i++)
        {
            totalUnits += roster.Units[i].Count;
        }
        if (roster.AttachedHero != null) totalUnits++;

        var slots = FormationCalculator.CalculateFormationSlots(roster.Formation, roster.SpawnCenter, totalUnits, spacing: 1.8f);
        int offsetIdx = 0;

        // Spawn Hero if present
        if (roster.AttachedHero != null)
        {
            var heroEntry = roster.AttachedHero;
            var spawnPos = offsetIdx < slots.Length ? slots[offsetIdx++] : roster.SpawnCenter;

            var heroState = new HeroState(
                heroEntry.HeroClass,
                $"{heroEntry.HeroType}_Hero",
                new HeroAttributes(18, 12, 10),
                baseLeadershipCapacity: 20);

            var heroUnit = new UnitEntity(
                id: sim.State.GenerateEntityId(),
                factionId: roster.FactionId,
                unitType: heroEntry.HeroType,
                position: spawnPos,
                maxHealth: 350f,
                attackDamage: 32f,
                attackRange: 1.8f,
                movementSpeed: 4.0f,
                attackCooldownTicks: 18,
                killXpValue: 250,
                baseArmor: 4f,
                attackType: "melee",
                aggroRange: 16f,
                archetype: UnitArchetype.Hero,
                heroState: heroState,
                formation: roster.Formation,
                initialLevel: heroEntry.Level);

            sim.State.AddUnit(heroUnit);
            sim.SpatialGrid.Insert(heroUnit.Id, heroUnit.Position);
            resultList.Add(heroUnit);
            initialCountMap[UnitArchetype.Hero] = initialCountMap.GetValueOrDefault(UnitArchetype.Hero) + 1;
        }

        // Spawn Regular Units
        for (int e = 0; e < roster.Units.Count; e++)
        {
            var entry = roster.Units[e];
            var arch = UnitArchetypeExtensions.FromUnitType(entry.UnitType);
            initialCountMap[arch] = initialCountMap.GetValueOrDefault(arch) + entry.Count;

            for (int i = 0; i < entry.Count; i++)
            {
                var spawnPos = offsetIdx < slots.Length ? slots[offsetIdx++] : roster.SpawnCenter;

                float hp = entry.CustomHp > 0 ? entry.CustomHp : GetDefaultHp(arch);
                float dmg = entry.CustomDamage > 0 ? entry.CustomDamage : GetDefaultDamage(arch);
                float armor = entry.CustomArmor > 0 ? entry.CustomArmor : GetDefaultArmor(arch);
                float range = GetDefaultRange(arch);
                float speed = GetDefaultSpeed(arch);
                int cooldown = GetDefaultCooldown(arch);
                string attackType = arch == UnitArchetype.Archer ? "ranged" : (arch == UnitArchetype.Siege ? "siege" : "melee");

                var unit = new UnitEntity(
                    id: sim.State.GenerateEntityId(),
                    factionId: roster.FactionId,
                    unitType: entry.UnitType,
                    position: spawnPos,
                    maxHealth: hp,
                    attackDamage: dmg,
                    attackRange: range,
                    movementSpeed: speed,
                    attackCooldownTicks: cooldown,
                    killXpValue: 50,
                    baseArmor: armor,
                    attackType: attackType,
                    aggroRange: 14f,
                    archetype: arch,
                    formation: roster.Formation,
                    initialLevel: entry.Level);

                sim.State.AddUnit(unit);
                sim.SpatialGrid.Insert(unit.Id, unit.Position);
                resultList.Add(unit);
            }
        }

        return resultList;
    }

    private static float GetDefaultHp(UnitArchetype arch) => arch switch
    {
        UnitArchetype.Spearman => 110f,
        UnitArchetype.Infantry => 120f,
        UnitArchetype.Archer => 75f,
        UnitArchetype.Cavalry => 160f,
        UnitArchetype.Siege => 250f,
        _ => 100f
    };

    private static float GetDefaultDamage(UnitArchetype arch) => arch switch
    {
        UnitArchetype.Spearman => 14f,
        UnitArchetype.Infantry => 16f,
        UnitArchetype.Archer => 12f,
        UnitArchetype.Cavalry => 22f,
        UnitArchetype.Siege => 45f,
        _ => 10f
    };

    private static float GetDefaultArmor(UnitArchetype arch) => arch switch
    {
        UnitArchetype.Spearman => 2f,
        UnitArchetype.Infantry => 3f,
        UnitArchetype.Archer => 0f,
        UnitArchetype.Cavalry => 3f,
        UnitArchetype.Siege => 5f,
        _ => 1f
    };

    private static float GetDefaultRange(UnitArchetype arch) => arch switch
    {
        UnitArchetype.Spearman => 2.0f,
        UnitArchetype.Infantry => 1.5f,
        UnitArchetype.Archer => 8.0f,
        UnitArchetype.Cavalry => 1.6f,
        UnitArchetype.Siege => 12.0f,
        _ => 1.5f
    };

    private static float GetDefaultSpeed(UnitArchetype arch) => arch switch
    {
        UnitArchetype.Spearman => 3.2f,
        UnitArchetype.Infantry => 3.5f,
        UnitArchetype.Archer => 3.6f,
        UnitArchetype.Cavalry => 6.2f,
        UnitArchetype.Siege => 2.0f,
        _ => 3.5f
    };

    private static int GetDefaultCooldown(UnitArchetype arch) => arch switch
    {
        UnitArchetype.Spearman => 20,
        UnitArchetype.Infantry => 18,
        UnitArchetype.Archer => 24,
        UnitArchetype.Cavalry => 22,
        UnitArchetype.Siege => 50,
        _ => 20
    };
}
