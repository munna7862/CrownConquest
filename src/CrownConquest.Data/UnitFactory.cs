using System;
using System.Collections.Generic;
using CrownConquest.Data.Models;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Data;

/// <summary>
/// Factory for creating domain UnitEntity instances from loaded data definitions.
/// </summary>
public sealed class UnitFactory
{
    private readonly Dictionary<string, UnitDefinition> _unitDefinitions;
    private readonly Dictionary<string, ProgressionCurveDefinition> _progressionCurves;

    public UnitFactory(
        IEnumerable<UnitDefinition>? unitDefs = null,
        IEnumerable<ProgressionCurveDefinition>? curveDefs = null)
    {
        _unitDefinitions = new Dictionary<string, UnitDefinition>();
        _progressionCurves = new Dictionary<string, ProgressionCurveDefinition>();

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
    }

    public void RegisterUnitDefinition(UnitDefinition def)
    {
        _unitDefinitions[def.Id] = def;
    }

    public void RegisterProgressionCurve(ProgressionCurveDefinition curve)
    {
        _progressionCurves[curve.Id] = curve;
    }

    public Result<UnitEntity> CreateUnit(
        EntityId id,
        FactionId factionId,
        string unitTypeId,
        Vector2D position)
    {
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
}
