using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Economy;

public enum WorkerTaskState
{
    None,
    MovingToResource,
    Harvesting,
    ReturningToDropOff,
    MovingToConstruct,
    Constructing,
    MovingToRepair,
    Repairing
}

/// <summary>
/// State tracking carried inventory, target resource nodes, construction tasks, and repair tasks for worker units.
/// </summary>
public sealed class WorkerGatherState
{
    public WorkerTaskState TaskState { get; set; }
    public ResourceType? CarriedResourceType { get; private set; }
    public int CarriedAmount { get; private set; }
    public int CarryCapacity { get; }

    public EntityId TargetResourceNodeId { get; set; }
    public EntityId TargetBuildingId { get; set; }

    public float HarvestRatePerTick { get; }
    public float HarvestProgressAccumulator { get; set; }
    public float BuildPowerPerTick { get; }
    public float RepairPowerPerTick { get; }

    public bool HasCarriedResources => CarriedAmount > 0 && CarriedResourceType.HasValue;
    public bool IsInventoryFull => CarriedAmount >= CarryCapacity;

    public WorkerGatherState(
        int carryCapacity = 10,
        float harvestRatePerTick = 0.5f,
        float buildPowerPerTick = 1.0f,
        float repairPowerPerTick = 1.5f)
    {
        CarryCapacity = Math.Max(1, carryCapacity);
        HarvestRatePerTick = Math.Max(0.01f, harvestRatePerTick);
        BuildPowerPerTick = Math.Max(0.01f, buildPowerPerTick);
        RepairPowerPerTick = Math.Max(0.01f, repairPowerPerTick);
        TaskState = WorkerTaskState.None;
        TargetResourceNodeId = EntityId.None;
        TargetBuildingId = EntityId.None;
        CarriedResourceType = null;
        CarriedAmount = 0;
        HarvestProgressAccumulator = 0f;
    }

    public void AddCarried(ResourceType type, int amount)
    {
        if (amount <= 0) return;

        // If carrying different resource, replace or take
        if (CarriedResourceType != type)
        {
            CarriedResourceType = type;
            CarriedAmount = Math.Min(CarryCapacity, amount);
        }
        else
        {
            CarriedAmount = Math.Min(CarryCapacity, CarriedAmount + amount);
        }
    }

    public (ResourceType Type, int Amount)? EmptyInventory()
    {
        if (!HasCarriedResources || !CarriedResourceType.HasValue)
        {
            return null;
        }

        var result = (CarriedResourceType.Value, CarriedAmount);
        CarriedResourceType = null;
        CarriedAmount = 0;
        return result;
    }

    public void ResetTask()
    {
        TaskState = WorkerTaskState.None;
        TargetResourceNodeId = EntityId.None;
        TargetBuildingId = EntityId.None;
        HarvestProgressAccumulator = 0f;
    }
}
