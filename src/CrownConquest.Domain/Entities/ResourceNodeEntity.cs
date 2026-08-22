using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Entities;

/// <summary>
/// Authoritative domain entity representing a harvestable resource node in the world.
/// </summary>
public sealed class ResourceNodeEntity
{
    public EntityId Id { get; }
    public ResourceType ResourceType { get; }
    public Vector2D Position { get; }
    public int MaxAmount { get; }
    public int RemainingAmount { get; private set; }
    public float HarvestRadius { get; }
    public bool IsDepleted => RemainingAmount <= 0;

    public ResourceNodeEntity(
        EntityId id,
        ResourceType resourceType,
        Vector2D position,
        int maxAmount = 500,
        float harvestRadius = 1.8f)
    {
        Id = id;
        ResourceType = resourceType;
        Position = position;
        MaxAmount = Math.Max(1, maxAmount);
        RemainingAmount = MaxAmount;
        HarvestRadius = harvestRadius;
    }

    /// <summary>
    /// Extracts up to requestedAmount from the node.
    /// </summary>
    public int Harvest(int requestedAmount, ulong tick, EntityId harvesterId, DomainEventBus? eventBus = null)
    {
        if (requestedAmount <= 0 || IsDepleted) return 0;

        int extracted = Math.Min(requestedAmount, RemainingAmount);
        RemainingAmount -= extracted;

        if (IsDepleted)
        {
            eventBus?.Publish(new ResourceNodeDepletedEvent(tick, Id, ResourceType, Position));
        }

        return extracted;
    }
}
