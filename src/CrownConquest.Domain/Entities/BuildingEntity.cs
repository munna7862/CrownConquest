using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Entities;

/// <summary>
/// Authoritative domain entity representing a settlement building (Town Center, House, Barracks, Storage Pit).
/// Decoupled from Godot nodes.
/// </summary>
public sealed class BuildingEntity
{
    public EntityId Id { get; }
    public FactionId FactionId { get; }
    public string BuildingType { get; }
    public Vector2D Position { get; }
    public Vector2D GridSize { get; }

    public float MaxHealth { get; }
    public float CurrentHealth { get; private set; }
    public float BaseBuildTimeTicks { get; }
    public float CurrentBuildProgress { get; private set; }

    public bool IsConstructed => CurrentBuildProgress >= BaseBuildTimeTicks;
    public float BuildProgressNormalized => BaseBuildTimeTicks > 0 ? Math.Clamp(CurrentBuildProgress / BaseBuildTimeTicks, 0f, 1f) : 1.0f;
    public bool IsAlive => CurrentHealth > 0f;

    public int PopulationProvided { get; }
    private readonly HashSet<ResourceType> _acceptedDropOffTypes;
    public IReadOnlyCollection<ResourceType> AcceptedDropOffTypes => _acceptedDropOffTypes;

    public ResourceCost BaseCost { get; }
    public bool IsFarm { get; }
    public int MaxFarmFood { get; }
    public int FarmFoodRemaining { get; private set; }
    public int FarmReseedCost { get; }
    public bool IsFarmDepleted => IsFarm && FarmFoodRemaining <= 0;

    public ProductionQueue ProductionQueue { get; }
    public Vector2D RallyPoint { get; set; }

    public Rect2D BoundingBox => Rect2D.FromCenterAndExtents(Position, GridSize.X * 0.5f, GridSize.Y * 0.5f);
    public bool IsDamaged => IsConstructed && IsAlive && CurrentHealth < MaxHealth;

    public BuildingEntity(
        EntityId id,
        FactionId factionId,
        string buildingType,
        Vector2D position,
        Vector2D gridSize,
        float maxHealth = 500f,
        float baseBuildTimeTicks = 100f,
        int populationProvided = 0,
        IEnumerable<ResourceType>? acceptedDropOffTypes = null,
        bool startsConstructed = false,
        Vector2D? rallyPoint = null,
        ResourceCost? baseCost = null,
        bool isFarm = false,
        int maxFarmFood = 250,
        int farmReseedCost = 60)
    {
        Id = id;
        FactionId = factionId;
        BuildingType = buildingType;
        Position = position;
        GridSize = gridSize;
        MaxHealth = Math.Max(1f, maxHealth);
        BaseBuildTimeTicks = Math.Max(1f, baseBuildTimeTicks);
        PopulationProvided = populationProvided;
        _acceptedDropOffTypes = acceptedDropOffTypes != null ? new HashSet<ResourceType>(acceptedDropOffTypes) : new HashSet<ResourceType>();
        BaseCost = baseCost ?? new ResourceCost(Wood: 100);
        IsFarm = isFarm || buildingType.Equals("farm", StringComparison.OrdinalIgnoreCase);
        MaxFarmFood = maxFarmFood;
        FarmFoodRemaining = IsFarm ? maxFarmFood : 0;
        FarmReseedCost = farmReseedCost;
        ProductionQueue = new ProductionQueue(maxQueueSize: 5);
        RallyPoint = rallyPoint ?? new Vector2D(position.X + (gridSize.X * 0.5f) + 1.5f, position.Y);

        if (startsConstructed)
        {
            CurrentBuildProgress = BaseBuildTimeTicks;
            CurrentHealth = MaxHealth;
        }
        else
        {
            CurrentBuildProgress = 0f;
            CurrentHealth = Math.Max(1f, MaxHealth * 0.1f); // 10% starting foundation health
        }
    }

    public bool AcceptsDropOff(ResourceType type)
    {
        return IsConstructed && IsAlive && _acceptedDropOffTypes.Contains(type);
    }

    public void Construct(
        float buildPower,
        ulong tick,
        DomainEventBus? eventBus,
        out bool completedJustNow)
    {
        completedJustNow = false;
        if (IsConstructed || !IsAlive || buildPower <= 0f) return;

        bool wasConstructed = IsConstructed;
        CurrentBuildProgress = Math.Min(BaseBuildTimeTicks, CurrentBuildProgress + buildPower);

        // Scale health up with construction progress
        float targetHealth = Math.Max(1f, (CurrentBuildProgress / BaseBuildTimeTicks) * MaxHealth);
        if (targetHealth > CurrentHealth)
        {
            CurrentHealth = targetHealth;
        }

        eventBus?.Publish(new BuildingConstructionProgressEvent(tick, Id, CurrentBuildProgress, BaseBuildTimeTicks));

        if (!wasConstructed && IsConstructed)
        {
            CurrentHealth = MaxHealth;
            completedJustNow = true;
            eventBus?.Publish(new BuildingCompletedEvent(tick, Id, FactionId, BuildingType, Position));
        }
    }

    public void Repair(
        float repairAmount,
        ulong tick,
        DomainEventBus? eventBus,
        out bool fullyRepaired)
    {
        fullyRepaired = false;
        if (!IsConstructed || !IsAlive || repairAmount <= 0f || CurrentHealth >= MaxHealth)
        {
            if (CurrentHealth >= MaxHealth) fullyRepaired = true;
            return;
        }

        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + repairAmount);
        eventBus?.Publish(new BuildingRepairProgressEvent(tick, Id, CurrentHealth, MaxHealth));

        if (CurrentHealth >= MaxHealth)
        {
            CurrentHealth = MaxHealth;
            fullyRepaired = true;
            eventBus?.Publish(new BuildingRepairedEvent(tick, Id, FactionId, BuildingType));
        }
    }

    public int HarvestFarmFood(
        int request,
        ulong tick,
        EntityId workerId,
        DomainEventBus? eventBus)
    {
        if (!IsFarm || !IsConstructed || !IsAlive || FarmFoodRemaining <= 0 || request <= 0)
        {
            return 0;
        }

        int harvested = Math.Min(request, FarmFoodRemaining);
        FarmFoodRemaining -= harvested;

        if (FarmFoodRemaining <= 0)
        {
            FarmFoodRemaining = 0;
            eventBus?.Publish(new FarmDepletedEvent(tick, Id, FactionId));
        }

        return harvested;
    }

    public void ReseedFarm(ulong tick, DomainEventBus? eventBus)
    {
        if (!IsFarm || !IsAlive) return;

        FarmFoodRemaining = MaxFarmFood;
        eventBus?.Publish(new FarmReseededEvent(tick, Id, FactionId, MaxFarmFood));
    }

    public void TakeDamage(
        float damage,
        EntityId attackerId,
        FactionId attackerFaction,
        ulong tick,
        DomainEventBus? eventBus,
        out bool destroyed)
    {
        destroyed = false;
        if (!IsAlive) return;

        CurrentHealth = MathF.Max(0f, CurrentHealth - damage);
        if (CurrentHealth <= 0f)
        {
            destroyed = true;
            // Additional building destroyed event can be emitted if needed
        }
    }
}
