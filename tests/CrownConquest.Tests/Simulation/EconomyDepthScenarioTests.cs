using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class EconomyDepthScenarioTests
{
    [Fact]
    public void Scenario_EconomyDepth_MultiClusterAndRepair_FullExecution()
    {
        // TC-S03-016: Full multi-cluster economy scenario with gathering across all 5 resources and building repair
        var scenario = new EconomyDepthScenario();
        var coordinator = scenario.Coordinator;
        var factionId = scenario.PlayerFaction;
        var bank = coordinator.GetResourceBank(factionId);

        int initFood = bank.Food;
        int initWood = bank.Wood;
        int initGold = bank.Gold;
        int initStone = bank.Stone;
        int initIron = bank.Iron;

        // Order workers to start economic operations and watchtower repair
        scenario.OrderStartAllEconomicGathering();
        scenario.OrderRepairWatchtower();

        // Update presentation snapshot
        scenario.Presenter.UpdateSnapshot();
        Assert.Equal(9, scenario.Presenter.Workers.TotalWorkers);
        Assert.Equal(1, scenario.Presenter.Buildings.DamagedBuildings);

        // Simulate 400 fixed ticks (e.g. 10 seconds of 40Hz simulation)
        for (int tick = 0; tick < 400; tick++)
        {
            coordinator.Simulation.Tick();
        }

        scenario.Presenter.UpdateSnapshot();

        // Verify watchtower was repaired
        bool foundTower = coordinator.Simulation.State.TryGetBuilding(scenario.DamagedWatchtowerId, out var watchtower);
        Assert.True(foundTower);
        Assert.NotNull(watchtower);
        Assert.Equal(600f, watchtower!.CurrentHealth);
        Assert.False(watchtower.IsDamaged);

        // Verify resource accumulation from specialized gathering outposts
        Assert.True(bank.Food > initFood);
        Assert.True(bank.Wood > 0); // Wood gathered and some used for repair
        Assert.True(bank.Gold > initGold);
        Assert.True(bank.Stone > 0); // Stone gathered
        Assert.True(bank.Iron > initIron);
    }

    [Fact]
    public void Scenario_EconomyDepthPresenter_DistributionSync_Accurate()
    {
        // TC-S03-017: Presenter mirrors simulation state accurately throughout scenario
        var scenario = new EconomyDepthScenario();
        var coordinator = scenario.Coordinator;
        var presenter = scenario.Presenter;

        presenter.UpdateSnapshot();
        Assert.Equal(300, presenter.Food);
        Assert.Equal(500, presenter.Wood);
        Assert.Equal(200, presenter.Gold);
        Assert.Equal(200, presenter.Stone);
        Assert.Equal(100, presenter.Iron);
        Assert.Equal(9, presenter.Workers.TotalWorkers);
        Assert.Equal(9, presenter.Workers.IdleWorkers);

        // Issue orders
        scenario.OrderStartAllEconomicGathering();
        scenario.OrderRepairWatchtower();

        // Step simulation 10 ticks
        for (int t = 0; t < 10; t++) coordinator.Simulation.Tick();
        presenter.UpdateSnapshot();

        // Active workers distributed across tasks
        Assert.True(presenter.Workers.WoodWorkers >= 2);
        Assert.True(presenter.Workers.GoldWorkers >= 1);
        Assert.True(presenter.Workers.IronWorkers >= 1);
        Assert.True(presenter.Workers.FoodWorkers >= 2);
        Assert.True(presenter.Workers.StoneWorkers >= 1);
        Assert.True(presenter.Workers.Repairers >= 2);
    }
}
