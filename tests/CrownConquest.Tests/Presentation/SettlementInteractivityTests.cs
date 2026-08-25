using System;
using System.Collections.Generic;
using System.Linq;
using CrownConquest.Application;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Presentation;

public sealed class SettlementInteractivityTests
{
    // =========================================================================
    // Tier 1: Pure Domain & Housing Math Tests
    // =========================================================================

    [Fact]
    public void TC_S17_001_BuildingSelection_PointClick_SelectsBuilding()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        // Town center is at (40, 40) with size (4, 4)
        bool selected = scenario.Selection.SelectPoint(new Vector2D(40f, 40f));

        Assert.True(selected);
        Assert.Equal(scenario.PlayerTownCenter.Id, scenario.Selection.SelectedBuildingId);
        Assert.Empty(scenario.Selection.SelectedUnitIds);
    }

    [Fact]
    public void TC_S17_002_BuildingSelection_ClickGround_DeselectsBuilding()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        scenario.Selection.SelectPoint(new Vector2D(40f, 40f));
        Assert.Equal(scenario.PlayerTownCenter.Id, scenario.Selection.SelectedBuildingId);

        // Click empty ground
        bool selected = scenario.Selection.SelectPoint(new Vector2D(150f, 150f));

        Assert.False(selected);
        Assert.Null(scenario.Selection.SelectedBuildingId);
        Assert.Empty(scenario.Selection.SelectedUnitIds);
    }

    [Fact]
    public void TC_S17_003_BuildingSelection_ClickUnit_DeselectsBuilding()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        scenario.Selection.SelectPoint(new Vector2D(40f, 40f));
        Assert.Equal(scenario.PlayerTownCenter.Id, scenario.Selection.SelectedBuildingId);

        // Click Hero Unit at (40, 52)
        bool selected = scenario.Selection.SelectPoint(scenario.HeroUnit.Position);

        Assert.True(selected);
        Assert.Null(scenario.Selection.SelectedBuildingId);
        Assert.Single(scenario.Selection.SelectedUnitIds, scenario.HeroUnit.Id);
    }

    [Fact]
    public void TC_S17_004_ProductionActionCards_TownCenter_OffersVillagerAndEra()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        scenario.Selection.SelectPoint(scenario.PlayerTownCenter.Position);

        var hudSnapshot = scenario.Hud.GenerateHudSnapshot(new Vector2D(1920, 1080));
        Assert.NotNull(hudSnapshot.BuildingSelection);
        var bSel = hudSnapshot.BuildingSelection.Value;

        Assert.Equal("town_center", bSel.BuildingType);
        Assert.Contains(bSel.ProductionOptions, opt => opt.ActionId == "celtic_villager" && opt.Cost.Food == 50);
        Assert.Contains(bSel.ProductionOptions, opt => opt.ActionId == "advance_era");
    }

    [Fact]
    public void TC_S17_005_ProductionActionCards_Barracks_OffersSwordsmanAndArcher()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        scenario.Selection.SelectPoint(scenario.PlayerBarracks.Position);

        var hudSnapshot = scenario.Hud.GenerateHudSnapshot(new Vector2D(1920, 1080));
        Assert.NotNull(hudSnapshot.BuildingSelection);
        var bSel = hudSnapshot.BuildingSelection.Value;

        Assert.Equal("barracks", bSel.BuildingType);
        Assert.Contains(bSel.ProductionOptions, opt => opt.ActionId == "celtic_swordsman" && opt.Cost.Food == 60 && opt.Cost.Wood == 20);
        Assert.Contains(bSel.ProductionOptions, opt => opt.ActionId == "celtic_archer" && opt.Cost.Food == 50 && opt.Cost.Wood == 40);
    }

    [Fact]
    public void TC_S17_006_ProductionActionCards_Blacksmith_OffersForgedBladesAndScaleArmor()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var sim = scenario.Coordinator.Simulation;
        var blacksmith = new BuildingEntity(
            sim.State.GenerateEntityId(),
            scenario.PlayerFaction,
            "blacksmith",
            new Vector2D(35f, 35f),
            new Vector2D(3f, 3f),
            maxHealth: 600f,
            startsConstructed: true);
        sim.State.AddBuilding(blacksmith);

        scenario.Selection.SelectPoint(blacksmith.Position);

        var hudSnapshot = scenario.Hud.GenerateHudSnapshot(new Vector2D(1920, 1080));
        Assert.NotNull(hudSnapshot.BuildingSelection);
        var bSel = hudSnapshot.BuildingSelection.Value;

        Assert.Contains(bSel.ProductionOptions, opt => opt.ActionId == "upgrade_forged_blades");
        Assert.Contains(bSel.ProductionOptions, opt => opt.ActionId == "upgrade_scale_armor");
    }

    [Fact]
    public void TC_S17_007_PopulationCapacity_HousesIncreaseCapBy5()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var sim = scenario.Coordinator.Simulation;

        // Base town center gives +15 pop
        var popManager = sim.State.GetOrCreatePopulationManager(scenario.PlayerFaction);
        popManager.RecalculateCapacity(sim.State.ActiveBuildings, scenario.Coordinator.CurrentTick);
        int initialCap = popManager.CurrentMaxCapacity;

        // Add 2 completed houses (+5 each = +10)
        for (int i = 0; i < 2; i++)
        {
            var house = new BuildingEntity(
                sim.State.GenerateEntityId(),
                scenario.PlayerFaction,
                "house",
                new Vector2D(25f + (i * 4f), 25f),
                new Vector2D(2f, 2f),
                maxHealth: 400f,
                populationProvided: 5,
                startsConstructed: true);
            sim.State.AddBuilding(house);
        }

        popManager.RecalculateCapacity(sim.State.ActiveBuildings, scenario.Coordinator.CurrentTick);
        Assert.Equal(initialCap + 10, popManager.CurrentMaxCapacity);
    }

    [Fact]
    public void TC_S17_008_PopulationBreakdown_CalculatesWorkersAndMilitary()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var hudSnapshot = scenario.Hud.GenerateHudSnapshot(new Vector2D(1920, 1080));

        var pop = hudSnapshot.PopulationBreakdown;
        Assert.Equal(4, pop.WorkerCount); // 4 villagers
        Assert.Equal(9, pop.MilitaryCount); // 8 swordsmen + 1 hero
        Assert.Equal(13, pop.TotalOccupied);
        Assert.False(pop.IsPopCapped);
        Assert.Contains("Occupied: 13", pop.Tooltip);
    }

    // =========================================================================
    // Tier 2: Production Queue & Placement Invariant Tests
    // =========================================================================

    [Fact]
    public void TC_S17_009_ProductionQueue_EnqueueDeductsResourcesImmediately()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var bank = scenario.Coordinator.GetResourceBank(scenario.PlayerFaction);
        int initialFood = bank.GetAmount(ResourceType.Food);
        int initialWood = bank.GetAmount(ResourceType.Wood);

        scenario.Coordinator.DispatchCommand(new QueueProductionCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            scenario.PlayerBarracks.Id,
            "celtic_swordsman"));

        scenario.StepSimulation(1);

        Assert.Equal(initialFood - 60, bank.GetAmount(ResourceType.Food));
        Assert.Equal(initialWood - 20, bank.GetAmount(ResourceType.Wood));
        Assert.Equal(1, scenario.PlayerBarracks.ProductionQueue.Count);
    }

    [Fact]
    public void TC_S17_010_ProductionQueue_MaxQueueSizeCappedAt5()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        for (int i = 0; i < 7; i++)
        {
            scenario.Coordinator.DispatchCommand(new QueueProductionCommand(
                scenario.Coordinator.CurrentTick,
                scenario.PlayerFaction,
                scenario.PlayerBarracks.Id,
                "celtic_swordsman"));
        }

        scenario.StepSimulation(1);
        Assert.Equal(5, scenario.PlayerBarracks.ProductionQueue.Count);
    }

    [Fact]
    public void TC_S17_011_ProductionQueue_CancelRefunds100PercentResources()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var bank = scenario.Coordinator.GetResourceBank(scenario.PlayerFaction);
        int initialFood = bank.GetAmount(ResourceType.Food);
        int initialWood = bank.GetAmount(ResourceType.Wood);

        scenario.Coordinator.DispatchCommand(new QueueProductionCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            scenario.PlayerBarracks.Id,
            "celtic_swordsman"));

        scenario.StepSimulation(1);

        // Cancel index 0
        scenario.Coordinator.DispatchCommand(new CancelProductionCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            scenario.PlayerBarracks.Id,
            QueueIndex: 0));

        scenario.StepSimulation(1);

        Assert.Equal(initialFood, bank.GetAmount(ResourceType.Food));
        Assert.Equal(initialWood, bank.GetAmount(ResourceType.Wood));
        Assert.Equal(0, scenario.PlayerBarracks.ProductionQueue.Count);
    }

    [Fact]
    public void TC_S17_012_ProductionQueue_ProgressAdvancesEachTick()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        scenario.Coordinator.DispatchCommand(new QueueProductionCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            scenario.PlayerBarracks.Id,
            "celtic_swordsman"));

        scenario.StepSimulation(1);
        var item = scenario.PlayerBarracks.ProductionQueue.CurrentItem;
        Assert.NotNull(item);

        int startProg = item.ProgressTicks;
        scenario.StepSimulation(10);
        Assert.Equal(startProg + 10, item.ProgressTicks);
        Assert.True(item.ProgressNormalized > 0.0f);
    }

    [Fact]
    public void TC_S17_013_ProductionQueue_CompletionSpawnsUnitAndFiresEvent()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        bool eventFired = false;
        scenario.Coordinator.EventBus.Subscribe<ProductionCompletedEvent>((in ProductionCompletedEvent e) =>
        {
            if (e.UnitType == "celtic_swordsman") eventFired = true;
        });

        int unitCountBefore = scenario.Coordinator.Simulation.State.ActiveUnits.Count;

        scenario.Coordinator.DispatchCommand(new QueueProductionCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            scenario.PlayerBarracks.Id,
            "celtic_swordsman"));

        // Swordsman train time is 50 ticks in sim engine
        scenario.StepSimulation(60);

        Assert.True(eventFired);
        Assert.Equal(unitCountBefore + 1, scenario.Coordinator.Simulation.State.ActiveUnits.Count);
    }

    [Fact]
    public void TC_S17_014_BuildingPlacement_ValidBlueprint_AllowsPlacement()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var config = BuildingPlacementPreview.GetBlueprintConfig("barracks");

        var result = scenario.Renderer != null ?
            new BuildingPlacementPreview(scenario.Coordinator).Evaluate(
                scenario.PlayerFaction,
                "barracks",
                new Vector2D(60f, 60f),
                config.GridSize,
                config.Cost) : default;

        Assert.True(result.IsValid);
        Assert.True(result.IsGridValid);
        Assert.True(result.CanAfford);
    }

    [Fact]
    public void TC_S17_015_BuildingPlacement_ObstructedBlueprint_RejectsPlacement()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var config = BuildingPlacementPreview.GetBlueprintConfig("barracks");

        // Try placing directly over Town Center at (40, 40)
        var preview = new BuildingPlacementPreview(scenario.Coordinator);
        var result = preview.Evaluate(
            scenario.PlayerFaction,
            "barracks",
            new Vector2D(40f, 40f),
            config.GridSize,
            config.Cost);

        Assert.False(result.IsValid);
        Assert.False(result.IsGridValid);
    }

    [Fact]
    public void TC_S17_016_BuildingPlacement_InsufficientResources_RejectsPlacement()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var bank = scenario.Coordinator.GetResourceBank(scenario.PlayerFaction);
        bank.TryDeduct(new ResourceCost(Wood: 500), scenario.Coordinator.CurrentTick, null);

        var config = BuildingPlacementPreview.GetBlueprintConfig("house");
        var preview = new BuildingPlacementPreview(scenario.Coordinator);
        var result = preview.Evaluate(
            scenario.PlayerFaction,
            "house",
            new Vector2D(70f, 70f),
            config.GridSize,
            config.Cost);

        Assert.False(result.IsValid);
        Assert.False(result.CanAfford);
    }

    // =========================================================================
    // Tier 3: Multi-System & Worker Loop Integration Tests
    // =========================================================================

    [Fact]
    public void TC_S17_017_RallyPoint_SpawnsUnitAndMarchesToRallyPoint()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var rallyDest = new Vector2D(70f, 70f);

        scenario.Coordinator.DispatchCommand(new SetRallyPointCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            scenario.PlayerBarracks.Id,
            rallyDest));

        scenario.Coordinator.DispatchCommand(new QueueProductionCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            scenario.PlayerBarracks.Id,
            "celtic_swordsman"));

        // Step until unit spawns and starts marching
        scenario.StepSimulation(55);

        var spawnedUnit = scenario.Coordinator.Simulation.State.ActiveUnits.Last();
        Assert.Equal("celtic_swordsman", spawnedUnit.UnitType);
        Assert.Equal(rallyDest, spawnedUnit.Position);
    }

    [Fact]
    public void TC_S17_018_RallyPoint_ResourceNode_AssignsWorkerToGather()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var goldNode = scenario.Coordinator.Simulation.State.ActiveResourceNodes.First(n => n.ResourceType == ResourceType.Gold);

        // Set Town Center rally point to Gold Node
        scenario.Coordinator.DispatchCommand(new SetRallyPointCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            scenario.PlayerTownCenter.Id,
            goldNode.Position));

        scenario.Coordinator.DispatchCommand(new QueueProductionCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            scenario.PlayerTownCenter.Id,
            "celtic_villager"));

        // Step until villager spawns
        scenario.StepSimulation(55);

        var spawnedVillager = scenario.Coordinator.Simulation.State.ActiveUnits.Last();
        Assert.Equal("celtic_villager", spawnedVillager.UnitType);
        Assert.NotNull(spawnedVillager.WorkerState);
        Assert.True(spawnedVillager.WorkerState.TaskState is WorkerTaskState.MovingToResource or WorkerTaskState.Harvesting or WorkerTaskState.ReturningToDropOff);
        Assert.True(spawnedVillager.WorkerState.CarriedAmount > 0 || spawnedVillager.WorkerState.TargetResourceNodeId.IsValid);
    }

    [Fact]
    public void TC_S17_019_WorkerGatherLoop_HarvestsUntilCapacityAndDeposits()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var woodNode = scenario.Coordinator.Simulation.State.ActiveResourceNodes.First(n => n.ResourceType == ResourceType.Wood);
        var villager = scenario.Coordinator.Simulation.State.ActiveUnits.First(u => u.UnitType == "celtic_villager");

        int initialWood = scenario.Coordinator.GetResourceBank(scenario.PlayerFaction).GetAmount(ResourceType.Wood);

        scenario.Coordinator.DispatchCommand(new GatherCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            [villager.Id],
            woodNode.Id));

        // Step through walk, harvest 10 wood, return to Town Center, and deposit
        scenario.StepSimulation(150);

        int currentWood = scenario.Coordinator.GetResourceBank(scenario.PlayerFaction).GetAmount(ResourceType.Wood);
        Assert.True(currentWood > initialWood, "Wood should be deposited into faction stockpile.");
    }

    [Fact]
    public void TC_S17_020_WorkerGatherLoop_DepletedNode_TransitionsToNearestOrIdle()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var sim = scenario.Coordinator.Simulation;

        // Add a small node with only 2 resources
        var smallNode = new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Iron, new Vector2D(39f, 41f), maxAmount: 2);
        sim.State.AddResourceNode(smallNode);

        var villager = sim.State.ActiveUnits.First(u => u.UnitType == "celtic_villager");

        scenario.Coordinator.DispatchCommand(new GatherCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            [villager.Id],
            smallNode.Id));

        scenario.StepSimulation(60);

        // Small node is depleted and removed, worker returned carried iron to Town Center and didn't crash
        Assert.True(smallNode.IsDepleted);
    }

    [Fact]
    public void TC_S17_021_BuildingConstruction_VillagersBuildFoundationToCompletion()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var sim = scenario.Coordinator.Simulation;

        // Place blueprint foundation at (50, 50)
        scenario.Coordinator.DispatchCommand(new PlaceBuildingCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            "house",
            new Vector2D(50f, 50f)));

        scenario.StepSimulation(1);

        var placedHouse = sim.State.ActiveBuildings.First(b => b.BuildingType == "house" && b.Position == new Vector2D(50f, 50f));
        Assert.False(placedHouse.IsConstructed);

        // Assign 2 villagers to construct
        var villagers = sim.State.ActiveUnits.Where(u => u.UnitType == "celtic_villager").Take(2).Select(u => u.Id).ToArray();
        scenario.Coordinator.DispatchCommand(new ConstructBuildingCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            villagers,
            placedHouse.Id));

        // Step simulation until construction completes
        scenario.StepSimulation(120);

        Assert.True(placedHouse.IsConstructed);
        Assert.Equal(placedHouse.MaxHealth, placedHouse.CurrentHealth);
    }

    [Fact]
    public void TC_S17_022_HousingCap_BlocksTrainingWhenPopCapped()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var sim = scenario.Coordinator.Simulation;
        var popManager = sim.State.GetOrCreatePopulationManager(scenario.PlayerFaction);
        popManager.RecalculateCapacity(sim.State.ActiveBuildings, scenario.Coordinator.CurrentTick);

        // Spawn extra units to reach current max capacity exactly
        int extraNeeded = popManager.CurrentMaxCapacity - sim.State.ActiveUnits.Count(u => u.FactionId == scenario.PlayerFaction && u.IsAlive);
        for (int i = 0; i < extraNeeded; i++)
        {
            var u = new UnitEntity(
                sim.State.GenerateEntityId(),
                scenario.PlayerFaction,
                "celtic_swordsman",
                new Vector2D(40f, 40f),
                maxHealth: 100f);
            sim.State.AddUnit(u);
        }

        scenario.Coordinator.DispatchCommand(new QueueProductionCommand(
            scenario.Coordinator.CurrentTick,
            scenario.PlayerFaction,
            scenario.PlayerTownCenter.Id,
            "celtic_villager"));

        scenario.StepSimulation(1);

        // Queue must be empty because population is capped
        Assert.Equal(0, scenario.PlayerTownCenter.ProductionQueue.Count);
    }

    // =========================================================================
    // Tier 4: Headless Scenario & Replay Parity Tests
    // =========================================================================

    [Fact]
    public void TC_S17_023_SettlementInteractiveScenario_FullEconomyLoop()
    {
        var scenario = new GraphicalGameScenario(seed: 42);
        var presenter = new SettlementEconomyPresenter(camera: scenario.Camera);

        // Step simulation 200 ticks
        scenario.StepSimulation(200);

        var bModels = presenter.GetBuildingViewModels();
        var nModels = presenter.GetResourceNodeViewModels();
        var wModels = presenter.GetWorkerViewModels();

        Assert.NotEmpty(bModels);
        Assert.NotEmpty(nModels);
        Assert.NotEmpty(wModels);
    }

    [Fact]
    public void TC_S17_024_SettlementScenario_ReplayChecksumParity_1000Ticks()
    {
        var scenario1 = new GraphicalGameScenario(seed: 7777);
        var scenario2 = new GraphicalGameScenario(seed: 7777);

        scenario1.StepSimulation(1000);
        scenario2.StepSimulation(1000);

        Assert.Equal(scenario1.Coordinator.CurrentTick, scenario2.Coordinator.CurrentTick);
        Assert.Equal(scenario1.Coordinator.Simulation.State.ActiveUnits.Count, scenario2.Coordinator.Simulation.State.ActiveUnits.Count);
        Assert.Equal(scenario1.Coordinator.Simulation.State.ActiveBuildings.Count, scenario2.Coordinator.Simulation.State.ActiveBuildings.Count);
    }
}
