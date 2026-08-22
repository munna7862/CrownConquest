using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Economy;

/// <summary>
/// Authoritative resource stockpile for a faction.
/// </summary>
public sealed class ResourceBank
{
    public FactionId FactionId { get; }

    public int Food { get; private set; }
    public int Wood { get; private set; }
    public int Gold { get; private set; }
    public int Stone { get; private set; }
    public int Iron { get; private set; }

    public ResourceBank(FactionId factionId, int initialFood = 0, int initialWood = 0, int initialGold = 0, int initialStone = 0, int initialIron = 0)
    {
        FactionId = factionId;
        Food = Math.Max(0, initialFood);
        Wood = Math.Max(0, initialWood);
        Gold = Math.Max(0, initialGold);
        Stone = Math.Max(0, initialStone);
        Iron = Math.Max(0, initialIron);
    }

    public int GetAmount(ResourceType type) => type switch
    {
        ResourceType.Food => Food,
        ResourceType.Wood => Wood,
        ResourceType.Gold => Gold,
        ResourceType.Stone => Stone,
        ResourceType.Iron => Iron,
        _ => 0
    };

    public void Deposit(
        ResourceType type,
        int amount,
        ulong tick,
        DomainEventBus? eventBus = null,
        EntityId? depositorId = null)
    {
        if (amount <= 0) return;

        switch (type)
        {
            case ResourceType.Food: Food += amount; break;
            case ResourceType.Wood: Wood += amount; break;
            case ResourceType.Gold: Gold += amount; break;
            case ResourceType.Stone: Stone += amount; break;
            case ResourceType.Iron: Iron += amount; break;
        }

        eventBus?.Publish(new ResourceDepositedEvent(
            tick,
            FactionId,
            depositorId ?? EntityId.None,
            type,
            amount,
            GetAmount(type)));
    }

    public bool CanAfford(ResourceCost cost)
    {
        if (cost.HasNegativeValues) return false;
        return Food >= cost.Food &&
               Wood >= cost.Wood &&
               Gold >= cost.Gold &&
               Stone >= cost.Stone &&
               Iron >= cost.Iron;
    }

    public bool TryDeduct(
        ResourceCost cost,
        ulong tick,
        DomainEventBus? eventBus = null,
        string reason = "")
    {
        if (!CanAfford(cost)) return false;

        Food -= cost.Food;
        Wood -= cost.Wood;
        Gold -= cost.Gold;
        Stone -= cost.Stone;
        Iron -= cost.Iron;

        eventBus?.Publish(new ResourceSpentEvent(
            tick,
            FactionId,
            cost,
            reason));

        return true;
    }
}
