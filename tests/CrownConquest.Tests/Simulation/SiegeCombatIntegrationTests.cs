using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class SiegeCombatIntegrationTests
{
    [Fact]
    public void TC_S07_13_SiegeWorkshop_QueuesAndProducesSiegeEngines()
    {
        // Arrange
        var engine = new SimulationEngine();
        var factionId = new FactionId(1);
        var bank = engine.State.GetOrCreateResourceBank(factionId);
        bank.Deposit(ResourceType.Wood, 1000, 0);
        bank.Deposit(ResourceType.Gold, 500, 0);
        bank.Deposit(ResourceType.Iron, 300, 0);

        // Add town center for population capacity
        var tcId = engine.State.GenerateEntityId();
        var tc = new BuildingEntity(
            tcId,
            factionId,
            "town_center",
            new Vector2D(0f, 0f),
            new Vector2D(4f, 4f),
            startsConstructed: true);
        engine.State.AddBuilding(tc);

        // Place Siege Workshop
        var workshopId = engine.State.GenerateEntityId();
        var workshop = new BuildingEntity(
            workshopId,
            factionId,
            "siege_workshop",
            new Vector2D(5f, 0f),
            new Vector2D(3f, 3f),
            startsConstructed: true);
        engine.State.AddBuilding(workshop);

        bool productionCompleted = false;
        engine.EventBus.Subscribe<ProductionCompletedEvent>((in ProductionCompletedEvent evt) =>
        {
            if (evt.UnitType.Contains("ram"))
            {
                productionCompleted = true;
            }
        });

        // Act: Queue Battering Ram
        engine.CommandQueue.Enqueue(new QueueProductionCommand(0, factionId, workshopId, "celtic_battering_ram"));
        engine.SimulateTicks(105); // Production duration is 100 ticks

        // Assert
        Assert.True(productionCompleted, "Battering ram production should have completed.");
        Assert.Contains(engine.State.ActiveUnits, u => u.UnitType.Contains("ram") && u.Archetype == UnitArchetype.Siege);
    }

    [Fact]
    public void TC_S07_14_TowerGarrison_IncreasesFirepowerAndUngarrisonsSafely()
    {
        // Arrange
        var engine = new SimulationEngine();
        var factionId = new FactionId(1);

        var towerId = engine.State.GenerateEntityId();
        var tower = new BuildingEntity(
            towerId,
            factionId,
            "guard_tower",
            new Vector2D(0f, 0f),
            new Vector2D(2f, 2f),
            startsConstructed: true);
        engine.State.AddBuilding(tower);

        var archer1 = new UnitEntity(engine.State.GenerateEntityId(), factionId, "celtic_archer", new Vector2D(0f, 0f), archetype: UnitArchetype.Archer);
        var archer2 = new UnitEntity(engine.State.GenerateEntityId(), factionId, "celtic_archer", new Vector2D(0f, 0f), archetype: UnitArchetype.Archer);
        engine.State.AddUnit(archer1);
        engine.State.AddUnit(archer2);

        Assert.Equal(0, tower.TowerDefense?.GarrisonCount);

        // Act: Garrison 2 archers
        engine.CommandQueue.Enqueue(new GarrisonTowerCommand(factionId, towerId, new[] { archer1.Id, archer2.Id }));
        engine.SimulateTicks(2);

        // Assert garrisoned
        Assert.Equal(2, tower.TowerDefense?.GarrisonCount);
        Assert.True(tower.TowerDefense?.EffectiveDamage > tower.TowerDefense?.BaseAttackDamage);

        // Act: Ungarrison
        engine.CommandQueue.Enqueue(new UngarrisonTowerCommand(factionId, towerId));
        engine.SimulateTicks(2);

        // Assert ungarrisoned
        Assert.Equal(0, tower.TowerDefense?.GarrisonCount);
    }

    [Fact]
    public void TC_S07_15_SiegeAi_PrioritizesGatesAndTowersOverStandardBuildings()
    {
        // Arrange
        var engine = new SimulationEngine();
        var factionAtk = new FactionId(1);
        var factionDef = new FactionId(2);

        var ram = new UnitEntity(
            engine.State.GenerateEntityId(),
            factionAtk,
            "celtic_battering_ram",
            new Vector2D(0f, 0f),
            aggroRange: 15.0f,
            archetype: UnitArchetype.Siege);
        engine.State.AddUnit(ram);

        // Add regular house at dist 3
        var house = new BuildingEntity(
            engine.State.GenerateEntityId(),
            factionDef,
            "house",
            new Vector2D(3f, 0f),
            new Vector2D(2f, 2f),
            startsConstructed: true);
        engine.State.AddBuilding(house);

        // Add gate at dist 5
        var gate = new BuildingEntity(
            engine.State.GenerateEntityId(),
            factionDef,
            "wooden_gate",
            new Vector2D(5f, 0f),
            new Vector2D(2f, 1f),
            startsConstructed: true);
        engine.State.AddBuilding(gate);

        // Act: Select target using SiegeAiHooks
        var targetId = SiegeAiHooks.SelectOptimalSiegeTarget(ram, engine.State);

        // Assert: Gate should be prioritized over house even though house is closer
        Assert.Equal(gate.Id, targetId);
    }

    [Fact]
    public void TC_S07_16_SiegeAi_LocatesNearestPassableBreach()
    {
        // Arrange
        var engine = new SimulationEngine();
        var factionDef = new FactionId(2);

        var breachPos1 = new Vector2D(10f, 0f);
        var breachPos2 = new Vector2D(25f, 0f);

        engine.State.AddBreach(new BreachEntity(new EntityId(101), factionDef, breachPos1, "wooden_wall", 50));
        engine.State.AddBreach(new BreachEntity(new EntityId(102), factionDef, breachPos2, "stone_wall", 70));

        var queryPos = new Vector2D(8f, 0f); // Closer to breachPos1

        // Act
        var nearest = SiegeAiHooks.FindNearestBreach(queryPos, engine.State);

        // Assert
        Assert.NotNull(nearest);
        Assert.Equal(new EntityId(101), nearest.WallEntityId);
        Assert.Equal(breachPos1, nearest.Position);
    }
}
