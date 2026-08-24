using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.AI;

/// <summary>
/// Immutable snapshot record of an entity perceived through fog-of-war vision.
/// </summary>
public readonly record struct PerceivedEntityRecord(
    EntityId EntityId,
    Vector2D Position,
    FactionId FactionId,
    bool IsBuilding,
    UnitArchetype UnitArchetype,
    string BuildingType,
    float CurrentHealth,
    float MaxHealth,
    int Level,
    ulong LastSeenTick)
{
    public bool IsAlive => CurrentHealth > 0f;

    public static PerceivedEntityRecord FromUnit(UnitEntity unit, ulong tick)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return new PerceivedEntityRecord(
            EntityId: unit.Id,
            Position: unit.Position,
            FactionId: unit.FactionId,
            IsBuilding: false,
            UnitArchetype: unit.Archetype,
            BuildingType: string.Empty,
            CurrentHealth: unit.CurrentHealth,
            MaxHealth: unit.MaxHealth,
            Level: unit.Veterancy.Level,
            LastSeenTick: tick);
    }

    public static PerceivedEntityRecord FromBuilding(BuildingEntity building, ulong tick)
    {
        ArgumentNullException.ThrowIfNull(building);
        return new PerceivedEntityRecord(
            EntityId: building.Id,
            Position: building.Position,
            FactionId: building.FactionId,
            IsBuilding: true,
            UnitArchetype: UnitArchetype.Infantry,
            BuildingType: building.BuildingType,
            CurrentHealth: building.CurrentHealth,
            MaxHealth: building.MaxHealth,
            Level: 1,
            LastSeenTick: tick);
    }
}
