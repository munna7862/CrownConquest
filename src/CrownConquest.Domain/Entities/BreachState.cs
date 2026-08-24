using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Entities;

/// <summary>
/// Authoritative representation of a breached wall fortification segment.
/// </summary>
public sealed record BreachEntity
{
    public EntityId WallEntityId { get; init; }
    public FactionId DefendingFactionId { get; init; }
    public Vector2D Position { get; init; }
    public string WallType { get; init; } = string.Empty;
    public ulong BreachedAtTick { get; init; }
    public float BreachRadius { get; init; } = 1.5f;

    public BreachEntity(
        EntityId wallEntityId,
        FactionId defendingFactionId,
        Vector2D position,
        string wallType,
        ulong breachedAtTick,
        float breachRadius = 1.5f)
    {
        WallEntityId = wallEntityId;
        DefendingFactionId = defendingFactionId;
        Position = position;
        WallType = wallType;
        BreachedAtTick = breachedAtTick;
        BreachRadius = breachRadius;
    }
}
