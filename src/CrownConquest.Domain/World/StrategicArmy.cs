using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.World;

/// <summary>
/// Domain entity representing an army maneuvered on the strategic campaign map.
/// </summary>
public sealed class StrategicArmy
{
    public StrategicArmyId Id { get; }
    public FactionId FactionId { get; }
    public string Name { get; set; }
    public ProvinceId CurrentProvinceId { get; set; }
    public ProvinceId? DestinationProvinceId { get; set; }
    public int MovementTicksRemaining { get; set; }
    public int TotalMovementTicksForEdge { get; set; }
    public Queue<ProvinceId> Waypoints { get; } = new();

    public List<StrategicUnitSpec> Units { get; } = new();
    public StrategicHeroSpec? AttachedHero { get; set; }
    public StrategicStance Stance { get; set; } = StrategicStance.Aggressive;
    public float BaseMovementSpeed { get; set; } = 50f;

    public bool IsInTransit => DestinationProvinceId.HasValue && MovementTicksRemaining > 0;
    public int UnitCount => Units.Count;
    public bool HasUnits => Units.Count > 0 || AttachedHero != null;

    public float TotalCombatPower
    {
        get
        {
            float power = 0f;
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].IsAlive)
                {
                    power += Units[i].CombatPower;
                }
            }
            if (AttachedHero != null)
            {
                power += AttachedHero.CombatPower;
            }
            return power;
        }
    }

    public StrategicArmy(
        StrategicArmyId id,
        FactionId factionId,
        string name,
        ProvinceId startingProvinceId,
        IEnumerable<StrategicUnitSpec>? units = null,
        StrategicHeroSpec? hero = null,
        StrategicStance stance = StrategicStance.Aggressive,
        float baseMovementSpeed = 50f)
    {
        Id = id;
        FactionId = factionId;
        Name = name;
        CurrentProvinceId = startingProvinceId;
        AttachedHero = hero;
        Stance = stance;
        BaseMovementSpeed = baseMovementSpeed > 0f ? baseMovementSpeed : 50f;

        if (units != null)
        {
            Units.AddRange(units);
        }
    }

    public void AddUnit(StrategicUnitSpec unit)
    {
        Units.Add(unit);
    }

    public void RemoveDeadUnits()
    {
        Units.RemoveAll(u => !u.IsAlive);
    }

    public void SetDestination(ProvinceId destination, int travelTicks)
    {
        DestinationProvinceId = destination;
        MovementTicksRemaining = Math.Max(1, travelTicks);
        TotalMovementTicksForEdge = MovementTicksRemaining;
    }

    public void ClearDestination()
    {
        DestinationProvinceId = null;
        MovementTicksRemaining = 0;
        TotalMovementTicksForEdge = 0;
    }
}
