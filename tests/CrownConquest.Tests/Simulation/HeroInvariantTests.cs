using System;
using System.Collections.Generic;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class HeroInvariantTests
{
    private static UnitEntity CreateTestHero(SimulationEngine sim, FactionId factionId, Vector2D pos)
    {
        var id = sim.State.GenerateEntityId();
        var state = new HeroState(
            heroClass: HeroClass.Warlord,
            heroName: "Brennus",
            baseAttributes: new HeroAttributes(18, 12, 10),
            baseLeadershipCapacity: 4,
            aura: new HeroAura("Warlord's Ferocity", 10.0f, 0.15f, 2.0f, 0.10f));

        state.AddAbility(new HeroAbilityDefinition(
            id: "heroic_strike",
            displayName: "Heroic Strike",
            description: "Melee burst",
            manaCost: 30f,
            cooldownTicks: 20,
            castRange: 2.0f,
            radius: 0f,
            AbilityTargetType.SingleTargetEnemy,
            AbilityEffectType.Damage,
            basePower: 50f));

        var unit = new UnitEntity(
            id: id,
            factionId: factionId,
            unitType: "celtic_warlord",
            position: pos,
            maxHealth: 350f,
            attackDamage: 30f,
            attackRange: 1.6f,
            movementSpeed: 4.0f,
            attackCooldownTicks: 18,
            archetype: UnitArchetype.Hero,
            heroState: state);

        sim.State.AddUnit(unit);
        return unit;
    }

    [Fact]
    public void HeroMana_ConservationAndRegen_Invariant()
    {
        // TC-S05-007: Mana consumption and per-tick regeneration invariant
        var sim = new SimulationEngine();
        var faction = new FactionId(1);
        var hero = CreateTestHero(sim, faction, new Vector2D(10f, 10f));

        float maxMana = hero.HeroState!.MaxMana;
        Assert.Equal(maxMana, hero.HeroState.CurrentMana);

        // Consume 60 mana
        Assert.True(hero.HeroState.ConsumeMana(60f));
        Assert.Equal(maxMana - 60f, hero.HeroState.CurrentMana);

        // Advance 20 ticks. Mana regen = 0.10 + 10 * 0.05 = 0.60 per tick
        // 20 * 0.60 = 12.0 mana recovered
        sim.SimulateTicks(20);

        Assert.Equal(maxMana - 60f + 12.0f, hero.HeroState.CurrentMana, precision: 2);
    }

    [Fact]
    public void HeroCooldown_FixedTickCountdown_Invariant()
    {
        // TC-S05-008: Cooldown decrements deterministically each tick
        var sim = new SimulationEngine();
        var faction = new FactionId(1);
        var hero = CreateTestHero(sim, faction, new Vector2D(10f, 10f));

        Assert.True(hero.HeroState!.TryGetAbility("heroic_strike", out var ab));
        Assert.True(ab!.IsReady);

        ab.TriggerCooldown();
        Assert.False(ab.IsReady);
        Assert.Equal(20, ab.CooldownRemainingTicks);

        sim.SimulateTicks(10);
        Assert.Equal(10, ab.CooldownRemainingTicks);
        Assert.False(ab.IsReady);

        sim.SimulateTicks(10);
        Assert.Equal(0, ab.CooldownRemainingTicks);
        Assert.True(ab.IsReady);
    }

    [Fact]
    public void HeroSquad_CapacityEnforcement_Invariant()
    {
        // TC-S05-009: Squad capacity enforcement
        var sim = new SimulationEngine();
        var faction = new FactionId(1);
        var hero = CreateTestHero(sim, faction, new Vector2D(10f, 10f));

        // Capacity: 4 + 0 + (18 / 4) = 8
        int capacity = hero.HeroState!.LeadershipCapacity;
        Assert.Equal(8, capacity);

        var squadIds = new List<EntityId>();
        for (int i = 0; i < 12; i++)
        {
            var u = new UnitEntity(sim.State.GenerateEntityId(), faction, "swordsman", new Vector2D(10f + i, 10f));
            sim.State.AddUnit(u);
            squadIds.Add(u.Id);
        }

        // Attach command with 12 units (exceeds capacity of 8)
        sim.CommandQueue.Enqueue(new AttachToHeroCommand(faction, 1UL, hero.Id, squadIds.ToArray()));
        sim.Tick();

        // Exactly 8 units attached, remaining 4 rejected
        Assert.Equal(8, hero.HeroState.AttachedUnitIds.Count);
    }

    [Fact]
    public void HeroFallen_AuraDisruption_Invariant()
    {
        // TC-S05-010: Hero death disrupts squad and clears aura
        var sim = new SimulationEngine();
        var faction = new FactionId(1);
        var hero = CreateTestHero(sim, faction, new Vector2D(10f, 10f));

        var soldier = new UnitEntity(sim.State.GenerateEntityId(), faction, "swordsman", new Vector2D(12f, 10f));
        sim.State.AddUnit(soldier);
        hero.HeroState!.AttachUnit(soldier.Id);

        sim.GetUnitAuraModifiers(soldier, out float dmgBonus, out float armorBonus, out _);
        Assert.Equal(0.15f, dmgBonus);
        Assert.Equal(2.0f, armorBonus);

        // Kill Hero directly
        hero.TakeDamage(1000f, EntityId.None, new FactionId(2), sim.CurrentTick, sim.EventBus, out bool killed);
        Assert.True(killed);
        Assert.False(hero.IsAlive);

        // Run a simulation tick for cleanup
        sim.Tick();

        // Soldier should have 0 aura bonuses
        sim.GetUnitAuraModifiers(soldier, out dmgBonus, out armorBonus, out _);
        Assert.Equal(0f, dmgBonus);
        Assert.Equal(0f, armorBonus);
    }

    [Fact]
    public void HeroProgression_DeterministicReplay_1000Ticks()
    {
        // TC-S05-011: Deterministic simulation parity test across two seeded runs
        ulong checksumA = RunHeroSimulation(seed: 9999);
        ulong checksumB = RunHeroSimulation(seed: 9999);

        Assert.Equal(checksumA, checksumB);
    }

    private static ulong RunHeroSimulation(int seed)
    {
        var config = new SimulationConfig { InitialRandomSeed = seed };
        var sim = new SimulationEngine(config);

        var playerFaction = new FactionId(1);
        var enemyFaction = new FactionId(2);

        var hero = CreateTestHero(sim, playerFaction, new Vector2D(20f, 20f));

        // Add squad units
        for (int i = 0; i < 4; i++)
        {
            var u = new UnitEntity(sim.State.GenerateEntityId(), playerFaction, "celtic_swordsman", new Vector2D(18f + i, 20f));
            sim.State.AddUnit(u);
            hero.HeroState!.AttachUnit(u.Id);
        }

        // Add enemy units
        for (int i = 0; i < 3; i++)
        {
            var e = new UnitEntity(sim.State.GenerateEntityId(), enemyFaction, "roman_legionary", new Vector2D(35f + i, 20f));
            sim.State.AddUnit(e);
        }

        // Move to engage
        sim.CommandQueue.Enqueue(new MoveCommand(playerFaction, 1UL, [hero.Id], new Vector2D(35f, 20f)));

        for (int t = 0; t < 1000; t++)
        {
            if (t == 50)
            {
                sim.CommandQueue.Enqueue(new CastHeroAbilityCommand(playerFaction, (ulong)t, hero.Id, "heroic_strike", EntityId.None, new Vector2D(35f, 20f)));
            }
            sim.Tick();
        }

        return sim.State.ComputeStateChecksum();
    }
}
