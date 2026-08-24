using System.Collections.Generic;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class TacticalAiInvariantTests
{
    [Fact]
    public void TC_S09_08_FocusFireInvariant_AiTargetsLowestHealthUnitFirst()
    {
        var config = new SimulationConfig { InitialRandomSeed = 42 };
        var engine = new SimulationEngine(config, bounds: new BattlefieldBounds(0, 0, 100, 100));

        var friendlyFaction = new FactionId(1);
        var enemyFaction = new FactionId(2);

        var friendlyUnit = new UnitEntity(engine.State.GenerateEntityId(), friendlyFaction, "archer", new Vector2D(50, 50), 100, 15, archetype: UnitArchetype.Archer);
        engine.State.AddUnit(friendlyUnit);

        var enemyFullHp = new UnitEntity(engine.State.GenerateEntityId(), enemyFaction, "spearman", new Vector2D(55, 50), 100, 10, archetype: UnitArchetype.Spearman);
        var enemyLowHp = new UnitEntity(engine.State.GenerateEntityId(), enemyFaction, "spearman", new Vector2D(55, 52), 100, 10, archetype: UnitArchetype.Spearman);
        enemyLowHp.TakeDamage(80f, friendlyUnit.Id, friendlyFaction, 0, engine.EventBus, out _); // 20 HP remaining

        engine.State.AddUnit(enemyFullHp);
        engine.State.AddUnit(enemyLowHp);

        var aiController = new AiFactionController(friendlyFaction, new Vector2D(50, 50));
        engine.RegisterAiController(aiController);

        // Update perception and tactics
        aiController.Perception.UpdatePerception(engine.State, 0);

        var bestTarget = AiTacticalScorer.SelectBestTacticalTarget(friendlyUnit, aiController.Perception.ActivePerceivedEnemies);
        Assert.NotNull(bestTarget);
        Assert.Equal(enemyLowHp.Id, bestTarget.Value.EntityId);
    }

    [Fact]
    public void TC_S09_09_FlankingInvariant_CavalryCalculatesFlankOffsets()
    {
        var cavalryPos = new Vector2D(40, 45);
        var engagedEnemyPos = new Vector2D(50, 50);
        var enemyHeading = new Vector2D(0, 1); // Facing North

        var flankPoint = AiTacticalScorer.CalculateFlankPoint(cavalryPos, engagedEnemyPos, enemyHeading, lateralOffset: 4f, rearOffset: 3f);

        Assert.True(AiTacticalScorer.IsFlankingAdvantageous(UnitArchetype.Cavalry));
        Assert.True(flankPoint.Y < engagedEnemyPos.Y);
    }

    [Fact]
    public void TC_S09_10_FormationInvariant_AiSwitchesToSquareWhenFacingCavalry()
    {
        var config = new SimulationConfig { InitialRandomSeed = 42 };
        var engine = new SimulationEngine(config, bounds: new BattlefieldBounds(0, 0, 100, 100));

        var faction1 = new FactionId(1);
        var faction2 = new FactionId(2);

        var spearman = new UnitEntity(engine.State.GenerateEntityId(), faction1, "spearman", new Vector2D(20, 20), 100, 10, archetype: UnitArchetype.Spearman);
        engine.State.AddUnit(spearman);

        var cav1 = new UnitEntity(engine.State.GenerateEntityId(), faction2, "cavalry", new Vector2D(25, 20), 120, 18, archetype: UnitArchetype.Cavalry);
        var cav2 = new UnitEntity(engine.State.GenerateEntityId(), faction2, "cavalry", new Vector2D(26, 22), 120, 18, archetype: UnitArchetype.Cavalry);
        engine.State.AddUnit(cav1);
        engine.State.AddUnit(cav2);

        var aiController = new AiFactionController(faction1, new Vector2D(20, 20));
        engine.RegisterAiController(aiController);

        engine.SimulateTicks(5); // Runs AI perception and tactics

        Assert.Equal(FormationType.Square, aiController.CurrentFormation);
    }

    [Fact]
    public void TC_S09_11_HighGroundInvariant_ElevationBoostsTacticalScore()
    {
        var record = new PerceivedEntityRecord(
            new EntityId(10), new Vector2D(30, 30), new FactionId(2), false, UnitArchetype.Infantry, "", 100, 100, 1, 0);

        float highScore = AiTacticalScorer.CalculateFocusFireScore(UnitArchetype.Archer, new Vector2D(25, 25), attackerElevation: 2, record, targetElevation: 0);
        float lowScore = AiTacticalScorer.CalculateFocusFireScore(UnitArchetype.Archer, new Vector2D(25, 25), attackerElevation: 0, record, targetElevation: 2);

        Assert.True(highScore > lowScore);
    }

    [Fact]
    public void TC_S09_12_SiegeEscortInvariant_CalculatesUniformPerimeter()
    {
        var siegePos = new Vector2D(50, 50);
        var slot0 = AiSiegeTactics.CalculateEscortPosition(siegePos, 0, 4, 3.0f);
        var slot1 = AiSiegeTactics.CalculateEscortPosition(siegePos, 1, 4, 3.0f);
        var slot2 = AiSiegeTactics.CalculateEscortPosition(siegePos, 2, 4, 3.0f);
        var slot3 = AiSiegeTactics.CalculateEscortPosition(siegePos, 3, 4, 3.0f);

        Assert.Equal(3.0f, siegePos.DistanceTo(slot0), precision: 2);
        Assert.Equal(3.0f, siegePos.DistanceTo(slot1), precision: 2);
        Assert.Equal(3.0f, siegePos.DistanceTo(slot2), precision: 2);
        Assert.Equal(3.0f, siegePos.DistanceTo(slot3), precision: 2);
    }
}
