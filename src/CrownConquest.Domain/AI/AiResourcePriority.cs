using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;

namespace CrownConquest.Domain.AI;

/// <summary>
/// Dynamic resource priority weightings calculated to balance worker assignments and production needs.
/// </summary>
public readonly record struct ResourceWeights(
    float FoodWeight,
    float WoodWeight,
    float GoldWeight,
    float StoneWeight,
    float IronWeight)
{
    public ResourceType PrimaryResourceDeficit
    {
        get
        {
            float max = FoodWeight;
            ResourceType primary = ResourceType.Food;

            if (WoodWeight > max)
            {
                max = WoodWeight;
                primary = ResourceType.Wood;
            }
            if (GoldWeight > max)
            {
                max = GoldWeight;
                primary = ResourceType.Gold;
            }
            if (StoneWeight > max)
            {
                max = StoneWeight;
                primary = ResourceType.Stone;
            }
            if (IronWeight > max)
            {
                primary = ResourceType.Iron;
            }

            return primary;
        }
    }
}

public static class AiResourcePriority
{
    /// <summary>
    /// Computes dynamic resource weights based on current stockpile, population capacity, and queued military needs.
    /// </summary>
    public static ResourceWeights CalculateWeights(
        ResourceBank bank,
        PopulationManager popManager,
        int activeWorkerCount,
        int targetWorkerCount,
        bool isMilitaryProductionActive,
        bool isSiegeWanted)
    {
        ArgumentNullException.ThrowIfNull(bank);
        ArgumentNullException.ThrowIfNull(popManager);

        int availableCap = popManager.CurrentMaxCapacity - popManager.CurrentPopulation;

        // 1. Food Weight: needed for workers, infantry, archers, cavalry
        float foodWeight = 2.0f;
        if (activeWorkerCount < targetWorkerCount)
        {
            foodWeight += 3.0f; // Urgently need workers
        }
        if (bank.Food < 100)
        {
            foodWeight += 2.0f;
        }
        else if (bank.Food > 500)
        {
            foodWeight = Math.Max(0.5f, foodWeight - 1.5f);
        }

        // 2. Wood Weight: needed for farms, houses, barracks, archery range, siege workshop
        float woodWeight = 2.0f;
        if (availableCap <= 2 && popManager.CurrentPopulation < popManager.AbsoluteMaxCap)
        {
            woodWeight += 4.0f; // Urgently need housing
        }
        if (bank.Wood < 100)
        {
            woodWeight += 2.0f;
        }
        else if (bank.Wood > 500)
        {
            woodWeight = Math.Max(0.5f, woodWeight - 1.5f);
        }

        // 3. Gold Weight: needed for cavalry, archers, heroes, technology
        float goldWeight = 1.0f;
        if (isMilitaryProductionActive)
        {
            goldWeight += 2.0f;
        }
        if (bank.Gold < 50)
        {
            goldWeight += 1.5f;
        }

        // 4. Stone Weight: needed for towers, walls, gates, town centers
        float stoneWeight = isSiegeWanted ? 2.5f : 0.8f;
        if (bank.Stone < 50 && isSiegeWanted)
        {
            stoneWeight += 2.0f;
        }

        // 5. Iron Weight: advanced weapons and heavy armor
        float ironWeight = isMilitaryProductionActive ? 1.5f : 0.5f;

        return new ResourceWeights(foodWeight, woodWeight, goldWeight, stoneWeight, ironWeight);
    }
}
