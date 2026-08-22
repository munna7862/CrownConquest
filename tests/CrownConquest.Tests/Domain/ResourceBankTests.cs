using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Events;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class ResourceBankTests
{
    [Fact]
    public void ResourceBank_DepositAndDeduct_CorrectBalances()
    {
        // TC-S02-001: Deposit various amounts into bank and deduct costs
        var factionId = new FactionId(1);
        var bank = new ResourceBank(factionId, initialFood: 50, initialWood: 100);
        var eventBus = new DomainEventBus();

        int depositedEventCount = 0;
        eventBus.Subscribe<ResourceDepositedEvent>((in ResourceDepositedEvent e) =>
        {
            depositedEventCount++;
        });

        Assert.Equal(50, bank.Food);
        Assert.Equal(100, bank.Wood);
        Assert.Equal(0, bank.Gold);

        // Deposit
        bank.Deposit(ResourceType.Food, 25, 10UL, eventBus);
        bank.Deposit(ResourceType.Gold, 100, 11UL, eventBus);
        bank.Deposit(ResourceType.Stone, 40, 12UL, eventBus);
        bank.Deposit(ResourceType.Iron, 30, 13UL, eventBus);

        Assert.Equal(75, bank.Food);
        Assert.Equal(100, bank.Wood);
        Assert.Equal(100, bank.Gold);
        Assert.Equal(40, bank.Stone);
        Assert.Equal(30, bank.Iron);
        Assert.Equal(4, depositedEventCount);

        // Deduct
        var cost = new ResourceCost(Food: 25, Wood: 50, Gold: 30);
        bool deducted = bank.TryDeduct(cost, 14UL, eventBus, "Unit Training");

        Assert.True(deducted);
        Assert.Equal(50, bank.Food);
        Assert.Equal(50, bank.Wood);
        Assert.Equal(70, bank.Gold);
    }

    [Fact]
    public void ResourceBank_CanAfford_InsufficientResources()
    {
        // TC-S02-002: Test CanAfford check when funds are insufficient
        var bank = new ResourceBank(new FactionId(1), initialFood: 40, initialWood: 100);

        var affordableCost = new ResourceCost(Food: 30, Wood: 80);
        var unaffordableFood = new ResourceCost(Food: 50, Wood: 50);
        var unaffordableGold = new ResourceCost(Gold: 10);

        Assert.True(bank.CanAfford(affordableCost));
        Assert.False(bank.CanAfford(unaffordableFood));
        Assert.False(bank.CanAfford(unaffordableGold));

        // Attempting to deduct unaffordable should fail and not mutate bank
        bool deducted = bank.TryDeduct(unaffordableFood, 1UL);
        Assert.False(deducted);
        Assert.Equal(40, bank.Food);
        Assert.Equal(100, bank.Wood);
    }

    [Fact]
    public void ResourceCost_ZeroAndMultiResourceChecks()
    {
        // TC-S02-003: Test ResourceCost creation and arithmetic operations
        var zero = ResourceCost.Zero;
        Assert.True(zero.IsZero);
        Assert.False(zero.HasNegativeValues);

        var costA = new ResourceCost(Food: 50, Wood: 100, Gold: 20);
        var costB = new ResourceCost(Food: 30, Wood: 50, Iron: 15);
        var combined = costA + costB;

        Assert.Equal(80, combined.Food);
        Assert.Equal(150, combined.Wood);
        Assert.Equal(20, combined.Gold);
        Assert.Equal(0, combined.Stone);
        Assert.Equal(15, combined.Iron);

        var negativeCost = new ResourceCost(Food: -10);
        Assert.True(negativeCost.HasNegativeValues);
    }
}
