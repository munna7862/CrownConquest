using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class SettlementEconomyScenarioTests
{
    [Fact]
    public void Scenario_SettlementBootstrap_ToMilitaryProduction()
    {
        // TC-S02-018: End-to-end headless fresh settlement bootstrap to military unit production
        var scenario = new SettlementEconomyScenario();
        var coordinator = scenario.Coordinator;
        var factionId = scenario.PlayerFaction;

        // 1. Order 2 villagers to gather Wood from ForestTree[0], 1 villager to gather Food from BerryBush[0]
        scenario.OrderGatherWood(scenario.StartingVillagerIds[0], scenario.ForestTreeIds[0]);
        scenario.OrderGatherWood(scenario.StartingVillagerIds[1], scenario.ForestTreeIds[0]);
        scenario.OrderGatherFood(scenario.StartingVillagerIds[2], scenario.BerryBushIds[0]);

        // Simulate 40 ticks for gathering
        coordinator.Update(40 * coordinator.Simulation.Config.DeltaTime);

        var bank = coordinator.GetResourceBank(factionId);
        Assert.True(bank.Wood >= 300, "Starting wood + gathered wood should be ample.");

        // 2. Place a House at (44, 42) and assign villager 0 to construct it
        var housePos = new Vector2D(44f, 42f);
        var placeHouseResult = scenario.OrderPlaceBuilding("house", housePos);
        Assert.True(placeHouseResult.IsSuccess);

        // Advance 1 tick so building entity is created in simulation
        coordinator.Update(coordinator.Simulation.Config.DeltaTime);

        // Find the placed house
        BuildingEntity? placedHouse = null;
        foreach (var b in coordinator.Simulation.State.ActiveBuildings)
        {
            if (b.BuildingType == "house")
            {
                placedHouse = b;
                break;
            }
        }
        Assert.NotNull(placedHouse);

        // Assign worker 0 to construct house
        scenario.OrderConstructBuilding(new[] { scenario.StartingVillagerIds[0] }, placedHouse.Id);

        // Simulate up to 100 ticks until house is constructed
        for (int i = 0; i < 100; i++)
        {
            coordinator.Update(coordinator.Simulation.Config.DeltaTime);
            if (placedHouse.IsConstructed) break;
        }

        Assert.True(placedHouse.IsConstructed, "House should be fully constructed.");
        // Population capacity should now be 5 (base) + 10 (Town Center) + 5 (House) = 20
        var popManager = coordinator.GetPopulationManager(factionId);
        Assert.Equal(20, popManager.CurrentMaxCapacity);

        // 3. Place a Barracks at (58, 42) and assign all 3 villagers to construct it
        var barracksPos = new Vector2D(58f, 42f);
        var placeBarracksResult = scenario.OrderPlaceBuilding("barracks", barracksPos);
        Assert.True(placeBarracksResult.IsSuccess);

        coordinator.Update(coordinator.Simulation.Config.DeltaTime);

        BuildingEntity? placedBarracks = null;
        foreach (var b in coordinator.Simulation.State.ActiveBuildings)
        {
            if (b.BuildingType == "barracks")
            {
                placedBarracks = b;
                break;
            }
        }
        Assert.NotNull(placedBarracks);

        // Assign 3 villagers to construct Barracks
        scenario.OrderConstructBuilding(scenario.StartingVillagerIds.ToArray(), placedBarracks.Id);

        // Simulate until Barracks completes
        for (int i = 0; i < 160; i++)
        {
            coordinator.Update(coordinator.Simulation.Config.DeltaTime);
            if (placedBarracks.IsConstructed) break;
        }
        Assert.True(placedBarracks.IsConstructed, "Barracks should be fully constructed.");

        // 4. Train 2 Swordsmen in the Barracks
        var trainResult1 = scenario.OrderTrainUnit(placedBarracks.Id, "swordsman");
        var trainResult2 = scenario.OrderTrainUnit(placedBarracks.Id, "swordsman");
        Assert.True(trainResult1.IsSuccess);
        Assert.True(trainResult2.IsSuccess);

        // Track completed units
        var producedMilitaryUnits = new List<EntityId>();
        coordinator.EventBus.Subscribe<ProductionCompletedEvent>((in ProductionCompletedEvent e) =>
        {
            if (e.UnitType == "swordsman")
            {
                producedMilitaryUnits.Add(e.ProducedUnitId);
            }
        });

        // Simulate 150 ticks for military training
        for (int i = 0; i < 150; i++)
        {
            coordinator.Update(coordinator.Simulation.Config.DeltaTime);
            if (producedMilitaryUnits.Count >= 2) break;
        }

        // 5. Verification of Sprint Exit Criteria
        Assert.Equal(2, producedMilitaryUnits.Count);

        foreach (var unitId in producedMilitaryUnits)
        {
            Assert.True(coordinator.Simulation.State.TryGetUnit(unitId, out var unit));
            Assert.NotNull(unit);
            Assert.Equal("swordsman", unit!.UnitType);
            Assert.Equal(factionId, unit.FactionId);
            Assert.True(unit.IsAlive);
        }

        // Total population should be 3 villagers + 2 swordsmen = 5
        Assert.Equal(5, popManager.CurrentPopulation);
    }

    [Fact]
    public void Scenario_ResourceBarAndPlacementPresenter_Sync()
    {
        // TC-S02-019: Presentation HUD & Preview reflect simulation truth
        var presenter = new SettlementEconomyPresenter();
        var scenario = presenter.Scenario;

        // Resource Bar View Model
        var hudModel = presenter.ResourceBar.GetViewModel();
        Assert.Equal(200, hudModel.Food);
        Assert.Equal(300, hudModel.Wood);
        Assert.Equal(100, hudModel.Gold);
        Assert.Equal(50, hudModel.Stone);
        Assert.Equal(50, hudModel.Iron);
        Assert.Equal(3, hudModel.CurrentPopulation);
        Assert.Equal(15, hudModel.MaxPopulation); // 5 base + 10 TC
        Assert.False(hudModel.IsPopCapped);

        // Placement Preview Evaluation
        var preview = presenter.PlacementPreview.Evaluate(
            scenario.PlayerFaction,
            "house",
            new Vector2D(44f, 42f),
            new Vector2D(2f, 2f),
            new ResourceCost(Wood: 50));

        Assert.True(preview.IsValid);
        Assert.True(preview.CanAfford);
        Assert.True(preview.IsGridValid);

        // Overlapping Placement Preview Evaluation
        var invalidPreview = presenter.PlacementPreview.Evaluate(
            scenario.PlayerFaction,
            "house",
            new Vector2D(50f, 50f), // directly on Town Center
            new Vector2D(2f, 2f),
            new ResourceCost(Wood: 50));

        Assert.False(invalidPreview.IsValid);
        Assert.False(invalidPreview.IsGridValid);

        // Worker View Models
        var workers = presenter.GetWorkerViewModels();
        Assert.Equal(3, workers.Count);

        // Building View Models
        var buildings = presenter.GetBuildingViewModels();
        Assert.Single(buildings);
        Assert.Equal("town_center", buildings[0].BuildingType);
        Assert.True(buildings[0].IsConstructed);

        // Resource Node View Models
        var nodes = presenter.GetResourceNodeViewModels();
        Assert.Equal(8, nodes.Count); // 3 trees, 2 bushes, 1 gold, 1 stone, 1 iron
    }
}
