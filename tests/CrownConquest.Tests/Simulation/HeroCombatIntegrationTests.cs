using System;
using System.Collections.Generic;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class HeroCombatIntegrationTests
{
    [Fact]
    public void Integration_HeroSquad_FormationAndAuraBuffs()
    {
        // TC-S05-012: Attached squad receives aura damage and armor buffs in live battle
        var sim = new SimulationEngine();
        var faction = new FactionId(1);

        var heroState = new HeroState(
            HeroClass.Warlord,
            "Brennus",
            new HeroAttributes(18, 12, 10),
            baseLeadershipCapacity: 10,
            aura: new HeroAura("Warlord's Ferocity", radius: 12.0f, damageMultiplierBonus: 0.15f, armorBonus: 2.0f, movementSpeedMultiplierBonus: 0.10f));

        var hero = new UnitEntity(
            sim.State.GenerateEntityId(),
            faction,
            "celtic_warlord",
            new Vector2D(20f, 20f),
            heroState: heroState);
        sim.State.AddUnit(hero);

        var attachedUnit = new UnitEntity(
            sim.State.GenerateEntityId(),
            faction,
            "celtic_swordsman",
            new Vector2D(22f, 20f));
        sim.State.AddUnit(attachedUnit);
        hero.HeroState!.AttachUnit(attachedUnit.Id);

        var unattachedUnit = new UnitEntity(
            sim.State.GenerateEntityId(),
            faction,
            "celtic_swordsman",
            new Vector2D(22f, 20f));
        sim.State.AddUnit(unattachedUnit);

        sim.GetUnitAuraModifiers(attachedUnit, out float dmgBonusAtt, out float armorBonusAtt, out _);
        sim.GetUnitAuraModifiers(unattachedUnit, out float dmgBonusUnatt, out float armorBonusUnatt, out _);

        Assert.Equal(0.15f, dmgBonusAtt);
        Assert.Equal(2.0f, armorBonusAtt);

        Assert.Equal(0f, dmgBonusUnatt);
        Assert.Equal(0f, armorBonusUnatt);
    }

    [Fact]
    public void Integration_HeroOffensiveAbility_AreaDamage()
    {
        // TC-S05-013: Hero casts AoE offensive ability into enemy group
        var sim = new SimulationEngine();
        var playerFaction = new FactionId(1);
        var enemyFaction = new FactionId(2);

        var heroState = new HeroState(
            HeroClass.Warlord,
            "Brennus",
            new HeroAttributes(18, 12, 10));

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

        var hero = new UnitEntity(
            sim.State.GenerateEntityId(),
            playerFaction,
            "celtic_warlord",
            new Vector2D(20f, 20f),
            heroState: heroState);
        sim.State.AddUnit(hero);

        var enemy1 = new UnitEntity(sim.State.GenerateEntityId(), enemyFaction, "roman_legionary", new Vector2D(22f, 20f), maxHealth: 100f);
        var enemy2 = new UnitEntity(sim.State.GenerateEntityId(), enemyFaction, "roman_legionary", new Vector2D(24f, 20f), maxHealth: 100f);
        sim.State.AddUnit(enemy1);
        sim.State.AddUnit(enemy2);

        float hp1Before = enemy1.CurrentHealth;
        float hp2Before = enemy2.CurrentHealth;

        bool abilityCastPublished = false;
        sim.EventBus.Subscribe<HeroAbilityCastEvent>((in HeroAbilityCastEvent e) =>
        {
            if (e.AbilityId == "war_cry") abilityCastPublished = true;
        });

        // Cast War Cry
        sim.CommandQueue.Enqueue(new CastHeroAbilityCommand(
            playerFaction,
            1UL,
            hero.Id,
            "war_cry",
            EntityId.None,
            hero.Position));

        sim.Tick();

        Assert.True(abilityCastPublished);
        Assert.True(enemy1.CurrentHealth < hp1Before);
        Assert.True(enemy2.CurrentHealth < hp2Before);
    }

    [Fact]
    public void Integration_HeroSupportAbility_AreaHeal()
    {
        // TC-S05-014: Druid hero casts Earth Mend AoE heal on damaged allies
        var sim = new SimulationEngine();
        var playerFaction = new FactionId(1);

        var druidState = new HeroState(
            HeroClass.Druid,
            "Diviciacus",
            new HeroAttributes(10, 10, 20));

        druidState.AddAbility(new HeroAbilityDefinition(
            id: "earth_mend",
            displayName: "Earth Mend",
            description: "Heals friendly units for 60 HP in 8.0 radius.",
            manaCost: 35f,
            cooldownTicks: 40,
            castRange: 8.0f,
            radius: 8.0f,
            targetType: AbilityTargetType.PointAreaAlly,
            effectType: AbilityEffectType.Heal,
            basePower: 60f));

        var druid = new UnitEntity(
            sim.State.GenerateEntityId(),
            playerFaction,
            "celtic_druid",
            new Vector2D(20f, 20f),
            heroState: druidState);
        sim.State.AddUnit(druid);

        var damagedAlly = new UnitEntity(
            sim.State.GenerateEntityId(),
            playerFaction,
            "celtic_swordsman",
            new Vector2D(22f, 20f),
            maxHealth: 120f);
        sim.State.AddUnit(damagedAlly);

        // Damage the ally
        damagedAlly.TakeDamage(80f, EntityId.None, new FactionId(2), 1UL, sim.EventBus, out _);
        Assert.Equal(40f, damagedAlly.CurrentHealth);

        // Cast Earth Mend at (22, 20)
        sim.CommandQueue.Enqueue(new CastHeroAbilityCommand(
            playerFaction,
            2UL,
            druid.Id,
            "earth_mend",
            EntityId.None,
            damagedAlly.Position));

        sim.Tick();

        // Ally should be healed (40 + 60 * 1.60 = 40 + 96 = clamped to 120 MaxHealth)
        Assert.Equal(120f, damagedAlly.CurrentHealth);
    }

    [Fact]
    public void Integration_HeroKillXP_LevelUpTrigger()
    {
        // TC-S05-015: Hero earns Kill XP, levels up, emits event, and gains attribute stats
        var sim = new SimulationEngine();
        var playerFaction = new FactionId(1);
        var enemyFaction = new FactionId(2);

        var heroState = new HeroState(
            HeroClass.Warlord,
            "Brennus",
            new HeroAttributes(18, 12, 10));

        var hero = new UnitEntity(
            sim.State.GenerateEntityId(),
            playerFaction,
            "celtic_warlord",
            new Vector2D(10f, 10f),
            xpThresholds: new[] { 0, 100, 250, 450 },
            heroState: heroState);
        sim.State.AddUnit(hero);

        Assert.Equal(1, hero.Veterancy.Level);

        bool levelUpFired = false;
        sim.EventBus.Subscribe<HeroLevelUpEvent>((in HeroLevelUpEvent e) =>
        {
            if (e.HeroId == hero.Id && e.NewLevel == 2)
            {
                levelUpFired = true;
            }
        });

        // Award 150 Kill XP to hero
        sim.AwardKillXpToHero(hero, 150, sim.CurrentTick);

        Assert.True(levelUpFired);
        Assert.Equal(2, hero.Veterancy.Level);
        Assert.Equal(2, hero.HeroState!.CurrentLevel);
        Assert.Equal(1, hero.HeroState.AvailableAttributePoints);
    }
}
