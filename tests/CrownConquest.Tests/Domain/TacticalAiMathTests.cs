using System;
using System.Collections.Generic;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class TacticalAiMathTests
{
    [Fact]
    public void TC_S09_01_FocusFire_ScoresLowHealthTargetsHigher()
    {
        var highHpTarget = new PerceivedEntityRecord(
            new EntityId(1),
            new Vector2D(10, 10),
            new FactionId(2),
            IsBuilding: false,
            UnitArchetype: UnitArchetype.Infantry,
            BuildingType: string.Empty,
            CurrentHealth: 100f,
            MaxHealth: 100f,
            Level: 1,
            LastSeenTick: 10);

        var lowHpTarget = new PerceivedEntityRecord(
            new EntityId(2),
            new Vector2D(10, 10),
            new FactionId(2),
            IsBuilding: false,
            UnitArchetype: UnitArchetype.Infantry,
            BuildingType: string.Empty,
            CurrentHealth: 20f,
            MaxHealth: 100f,
            Level: 1,
            LastSeenTick: 10);

        float scoreHighHp = AiTacticalScorer.CalculateFocusFireScore(
            UnitArchetype.Infantry,
            new Vector2D(0, 0),
            0,
            highHpTarget);

        float scoreLowHp = AiTacticalScorer.CalculateFocusFireScore(
            UnitArchetype.Infantry,
            new Vector2D(0, 0),
            0,
            lowHpTarget);

        Assert.True(scoreLowHp > scoreHighHp, $"Low HP target score ({scoreLowHp}) must exceed high HP target score ({scoreHighHp})");
    }

    [Fact]
    public void TC_S09_02_FlankingManeuver_CalculatesSideAndRearOffset()
    {
        var targetPos = new Vector2D(50, 50);
        var targetHeading = new Vector2D(0, 1); // Heading North (+Y)
        var attackerPos = new Vector2D(40, 50); // Attacker to the West (-X)

        var flankPoint = AiTacticalScorer.CalculateFlankPoint(attackerPos, targetPos, targetHeading, lateralOffset: 4f, rearOffset: 3f);

        // Target heading North -> Rear is South (Y = 47), West side is X = 46
        Assert.True(flankPoint.Y < targetPos.Y, "Flank point should be behind target heading");
        Assert.True(flankPoint.X < targetPos.X, "Flank point should be on attacker lateral side");
    }

    [Fact]
    public void TC_S09_03_DynamicFormationSelection_CountersCavalryWithSquare()
    {
        var friendly = new List<UnitEntity>
        {
            new UnitEntity(new EntityId(1), new FactionId(1), "spearman", new Vector2D(0, 0), maxHealth: 100f, attackDamage: 10, archetype: UnitArchetype.Spearman)
        };

        var perceivedEnemyCav = new List<PerceivedEntityRecord>
        {
            new PerceivedEntityRecord(new EntityId(2), new Vector2D(10, 10), new FactionId(2), false, UnitArchetype.Cavalry, "", 100, 100, 1, 0),
            new PerceivedEntityRecord(new EntityId(3), new Vector2D(12, 10), new FactionId(2), false, UnitArchetype.Cavalry, "", 100, 100, 1, 0)
        };

        var formation = AiFormationSelector.SelectOptimalFormation(friendly, perceivedEnemyCav);
        Assert.Equal(FormationType.Square, formation);
    }

    [Fact]
    public void TC_S09_04_ElevationAdvantage_ProvidesScoreBonus()
    {
        var target = new PerceivedEntityRecord(
            new EntityId(1), new Vector2D(10, 10), new FactionId(2), false, UnitArchetype.Infantry, "", 100, 100, 1, 0);

        float highGroundScore = AiTacticalScorer.CalculateFocusFireScore(
            UnitArchetype.Archer, new Vector2D(0, 0), attackerElevation: 2, target, targetElevation: 0, elevationBias: 1.0f);

        float evenGroundScore = AiTacticalScorer.CalculateFocusFireScore(
            UnitArchetype.Archer, new Vector2D(0, 0), attackerElevation: 0, target, targetElevation: 0, elevationBias: 1.0f);

        Assert.True(highGroundScore > evenGroundScore, "High ground score must exceed even ground score");
    }

    [Fact]
    public void TC_S09_05_SiegeTactics_SelectsFortificationsAndCalculatesEscorts()
    {
        var siegeUnit = new UnitEntity(new EntityId(1), new FactionId(1), "catapult", new Vector2D(10, 10), 200, 40, archetype: UnitArchetype.Siege);
        var enemies = new List<PerceivedEntityRecord>
        {
            new PerceivedEntityRecord(new EntityId(2), new Vector2D(15, 15), new FactionId(2), false, UnitArchetype.Infantry, "", 100, 100, 1, 0),
            new PerceivedEntityRecord(new EntityId(3), new Vector2D(18, 18), new FactionId(2), true, UnitArchetype.Infantry, "stone_wall", 500, 500, 1, 0),
            new PerceivedEntityRecord(new EntityId(4), new Vector2D(20, 20), new FactionId(2), true, UnitArchetype.Infantry, "fortress_gate", 800, 800, 1, 0)
        };

        var bestTarget = AiSiegeTactics.SelectSiegeTarget(siegeUnit, enemies);
        Assert.NotNull(bestTarget);
        Assert.True(bestTarget.Value.IsBuilding, "Siege engine should prioritize fortifications over units");

        var escortPos0 = AiSiegeTactics.CalculateEscortPosition(siegeUnit.Position, 0, 4, escortRadius: 3.0f);
        var escortPos2 = AiSiegeTactics.CalculateEscortPosition(siegeUnit.Position, 2, 4, escortRadius: 3.0f);
        Assert.NotEqual(escortPos0, escortPos2);
    }

    [Fact]
    public void TC_S09_07_PersonalityProfiles_InitializeWithDistinctParameters()
    {
        var agg = AiPersonalityProfile.CreateAggressive();
        var def = AiPersonalityProfile.CreateDefensive();
        var exp = AiPersonalityProfile.CreateExpansionist();
        var tac = AiPersonalityProfile.CreateTactical();

        Assert.Equal(AiPersonalityType.Aggressive, agg.PersonalityType);
        Assert.Equal(FormationType.Wedge, agg.PreferredFormation);
        Assert.True(agg.RetreatOddsThreshold < def.RetreatOddsThreshold, "Aggressive retreat threshold should be lower than defensive");

        Assert.Equal(AiPersonalityType.Defensive, def.PersonalityType);
        Assert.Equal(FormationType.Square, def.PreferredFormation);

        Assert.Equal(AiPersonalityType.Expansionist, exp.PersonalityType);
        Assert.True(exp.TargetWorkerCount > agg.TargetWorkerCount);

        Assert.Equal(AiPersonalityType.Tactical, tac.PersonalityType);
        Assert.True(tac.HeroPreservation);
    }
}
