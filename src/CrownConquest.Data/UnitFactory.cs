using System;
using System.Collections.Generic;
using CrownConquest.Data.Models;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Data;

/// <summary>
/// Factory for creating domain UnitEntity and Hero instances from loaded data definitions.
/// </summary>
public sealed class UnitFactory
{
    private readonly Dictionary<string, UnitDefinition> _unitDefinitions;
    private readonly Dictionary<string, ProgressionCurveDefinition> _progressionCurves;
    private readonly Dictionary<string, HeroDefinition> _heroDefinitions;
    private readonly Dictionary<string, AbilityDefinitionModel> _abilityDefinitions;

    public UnitFactory(
        IEnumerable<UnitDefinition>? unitDefs = null,
        IEnumerable<ProgressionCurveDefinition>? curveDefs = null,
        IEnumerable<HeroDefinition>? heroDefs = null,
        IEnumerable<AbilityDefinitionModel>? abilityDefs = null)
    {
        _unitDefinitions = new Dictionary<string, UnitDefinition>(StringComparer.OrdinalIgnoreCase);
        _progressionCurves = new Dictionary<string, ProgressionCurveDefinition>(StringComparer.OrdinalIgnoreCase);
        _heroDefinitions = new Dictionary<string, HeroDefinition>(StringComparer.OrdinalIgnoreCase);
        _abilityDefinitions = new Dictionary<string, AbilityDefinitionModel>(StringComparer.OrdinalIgnoreCase);

        if (unitDefs != null)
        {
            foreach (var def in unitDefs)
            {
                _unitDefinitions[def.Id] = def;
            }
        }

        if (curveDefs != null)
        {
            foreach (var curve in curveDefs)
            {
                _progressionCurves[curve.Id] = curve;
            }
        }

        if (heroDefs != null)
        {
            foreach (var hero in heroDefs)
            {
                _heroDefinitions[hero.Id] = hero;
            }
        }

        if (abilityDefs != null)
        {
            foreach (var ability in abilityDefs)
            {
                _abilityDefinitions[ability.Id] = ability;
            }
        }
    }

    public void RegisterUnitDefinition(UnitDefinition def)
    {
        _unitDefinitions[def.Id] = def;
    }

    public void RegisterProgressionCurve(ProgressionCurveDefinition curve)
    {
        _progressionCurves[curve.Id] = curve;
    }

    public void RegisterHeroDefinition(HeroDefinition hero)
    {
        _heroDefinitions[hero.Id] = hero;
    }

    public void RegisterAbilityDefinition(AbilityDefinitionModel ability)
    {
        _abilityDefinitions[ability.Id] = ability;
    }

    public Result<UnitEntity> CreateUnit(
        EntityId id,
        FactionId factionId,
        string unitTypeId,
        Vector2D position)
    {
        if (_heroDefinitions.TryGetValue(unitTypeId, out var heroDef))
        {
            return CreateHeroUnit(id, factionId, unitTypeId, position);
        }

        if (!_unitDefinitions.TryGetValue(unitTypeId, out var def))
        {
            return Result<UnitEntity>.Failure(new GameError("UNKNOWN_UNIT_TYPE", $"Unit type '{unitTypeId}' not found in registered definitions."));
        }

        int[]? xpThresholds = null;
        float hpBonus = 15.0f;
        float dmgBonus = 2.5f;

        if (_progressionCurves.TryGetValue(def.XpCurveId, out var curve))
        {
            xpThresholds = curve.LevelXpThresholds;
            hpBonus = curve.HealthPerLevelBonus;
            dmgBonus = curve.DamagePerLevelBonus;
        }

        WorkerGatherState? workerState = null;
        if (def.Id.Contains("villager", StringComparison.OrdinalIgnoreCase) ||
            def.Id.Contains("worker", StringComparison.OrdinalIgnoreCase))
        {
            workerState = new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 0.5f, buildPowerPerTick: 1.0f);
        }

        var unit = new UnitEntity(
            id: id,
            factionId: factionId,
            unitType: def.Id,
            position: position,
            maxHealth: def.MaxHealth,
            attackDamage: def.AttackDamage,
            attackRange: def.AttackRange,
            movementSpeed: def.MovementSpeed,
            attackCooldownTicks: def.AttackCooldownTicks,
            killXpValue: def.KillXpValue,
            baseArmor: def.Armor,
            attackType: def.AttackType,
            aggroRange: def.AggroRange,
            healthPerLevelBonus: hpBonus,
            damagePerLevelBonus: dmgBonus,
            xpThresholds: xpThresholds,
            workerState: workerState);

        return Result<UnitEntity>.Success(unit);
    }

    public Result<UnitEntity> CreateHeroUnit(
        EntityId id,
        FactionId factionId,
        string heroTypeId,
        Vector2D position)
    {
        if (!_heroDefinitions.TryGetValue(heroTypeId, out var heroDef))
        {
            return Result<UnitEntity>.Failure(new GameError("UNKNOWN_HERO_TYPE", $"Hero definition '{heroTypeId}' not found."));
        }

        if (!_unitDefinitions.TryGetValue(heroTypeId, out var unitDef))
        {
            // Fallback base stats if not in unit definitions
            unitDef = new UnitDefinition
            {
                Id = heroDef.Id,
                DisplayName = heroDef.HeroName,
                Faction = heroDef.Faction,
                MaxHealth = 300f,
                AttackDamage = 30f,
                Armor = 4f,
                AttackRange = 1.6f,
                AttackType = "melee",
                MovementSpeed = 4.0f,
                AttackCooldownTicks = 18,
                KillXpValue = 250,
                AggroRange = 14f,
                XpCurveId = "hero_warlord_curve"
            };

        }

        int[]? xpThresholds = null;
        float hpBonus = 30.0f;
        float dmgBonus = 4.0f;

        if (_progressionCurves.TryGetValue(unitDef.XpCurveId, out var curve))
        {
            xpThresholds = curve.LevelXpThresholds;
            hpBonus = curve.HealthPerLevelBonus;
            dmgBonus = curve.DamagePerLevelBonus;
        }

        HeroClass heroClass = Enum.TryParse<HeroClass>(heroDef.HeroClass, true, out var parsedClass)
            ? parsedClass
            : HeroClass.Warlord;

        HeroAura? aura = null;
        if (heroDef.Aura != null)
        {
            aura = new HeroAura(
                heroDef.Aura.AuraName,
                heroDef.Aura.Radius,
                heroDef.Aura.DamageMultiplierBonus,
                heroDef.Aura.ArmorBonus,
                heroDef.Aura.MovementSpeedMultiplierBonus);
        }

        var heroState = new HeroState(
            heroClass: heroClass,
            heroName: heroDef.HeroName,
            baseAttributes: new HeroAttributes(heroDef.BaseStrength, heroDef.BaseAgility, heroDef.BaseWillpower),
            baseLeadershipCapacity: heroDef.BaseLeadershipCapacity,
            aura: aura,
            strengthPerLevel: heroDef.StrengthPerLevel,
            agilityPerLevel: heroDef.AgilityPerLevel,
            willpowerPerLevel: heroDef.WillpowerPerLevel);

        // Bind abilities
        foreach (var abilityId in heroDef.AbilityIds)
        {
            if (_abilityDefinitions.TryGetValue(abilityId, out var abilityModel))
            {
                var targetType = Enum.TryParse<AbilityTargetType>(abilityModel.TargetType, true, out var tt) ? tt : AbilityTargetType.SingleTargetEnemy;
                var effectType = Enum.TryParse<AbilityEffectType>(abilityModel.EffectType, true, out var et) ? et : AbilityEffectType.Damage;

                heroState.AddAbility(new HeroAbilityDefinition(
                    abilityModel.Id,
                    abilityModel.DisplayName,
                    abilityModel.Description,
                    abilityModel.ManaCost,
                    abilityModel.CooldownTicks,
                    abilityModel.CastRange,
                    abilityModel.Radius,
                    targetType,
                    effectType,
                    abilityModel.BasePower));
            }
        }

        var heroUnit = new UnitEntity(
            id: id,
            factionId: factionId,
            unitType: heroDef.Id,
            position: position,
            maxHealth: unitDef.MaxHealth,
            attackDamage: unitDef.AttackDamage,
            attackRange: unitDef.AttackRange,
            movementSpeed: unitDef.MovementSpeed,
            attackCooldownTicks: unitDef.AttackCooldownTicks,
            killXpValue: unitDef.KillXpValue,
            baseArmor: unitDef.Armor,
            attackType: unitDef.AttackType,
            aggroRange: unitDef.AggroRange,
            healthPerLevelBonus: hpBonus,
            damagePerLevelBonus: dmgBonus,
            xpThresholds: xpThresholds,
            archetype: UnitArchetype.Hero,
            heroState: heroState);

        return Result<UnitEntity>.Success(heroUnit);
    }
}
