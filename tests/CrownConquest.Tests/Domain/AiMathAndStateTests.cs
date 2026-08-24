using System;
using System.Collections.Generic;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class AiMathAndStateTests
{
    [Fact]
    public void TC_S08_01_CombatPowerCalculation_ScalesWithStatsLevelAndArchetype()
    {
        var bus = new DomainEventBus();
        var unit = new UnitEntity(
            new EntityId(1),
            new FactionId(1),
            "spearman",
            new Vector2D(0, 0),
            maxHealth: 100f,
            attackDamage: 15f,
            attackRange: 1.5f,
            movementSpeed: 3.5f,
            attackCooldownTicks: 20,
            killXpValue: 50,
            baseArmor: 2f);

        float powerLvl1 = AiCombatEvaluator.CalculateUnitCombatPower(unit);
        Assert.True(powerLvl1 > 0f);

        // Advance level to 3
        unit.Veterancy.AwardXp(300, 1, bus, out bool leveledUp, out bool rankChanged);
        float powerLvl3 = AiCombatEvaluator.CalculateUnitCombatPower(unit);
        Assert.True(powerLvl3 > powerLvl1, "Combat power must increase with veterancy level.");

        // Injured unit should have proportionally lower combat power
        unit.TakeDamage(50f, new EntityId(99), new FactionId(2), 2, bus, out bool killed);
        float powerInjured = AiCombatEvaluator.CalculateUnitCombatPower(unit);
        Assert.True(powerInjured < powerLvl3, "Combat power must decrease when health is depleted.");
    }

    [Fact]
    public void TC_S08_02_CombatOddsAndRetreatThreshold_EvaluatesCorrectly()
    {
        float friendlyPower = 100f;
        float enemyPower = 100f;

        float oddsEven = AiCombatEvaluator.CalculateCombatOdds(friendlyPower, enemyPower);
        Assert.Equal(0.5f, oddsEven, 2);
        Assert.False(AiCombatEvaluator.ShouldRetreat(friendlyPower, enemyPower, squadHealthPercent: 1.0f));

        // Overwhelming enemy power (odds < 0.45)
        float highEnemyPower = 300f;
        float oddsLow = AiCombatEvaluator.CalculateCombatOdds(friendlyPower, highEnemyPower);
        Assert.True(oddsLow < 0.45f);
        Assert.True(AiCombatEvaluator.ShouldRetreat(friendlyPower, highEnemyPower, squadHealthPercent: 1.0f));

        // Severe health loss (< 30%) triggers retreat even if odds seem balanced
        Assert.True(AiCombatEvaluator.ShouldRetreat(friendlyPower, enemyPower, squadHealthPercent: 0.25f));
    }

    [Fact]
    public void TC_S08_03_AiPerceptionState_TracksSightAndFogOfWarMemory()
    {
        var state = new SimulationState();
        var faction1 = new FactionId(1);
        var faction2 = new FactionId(2);

        var friendlyUnit = new UnitEntity(
            new EntityId(1),
            faction1,
            "spearman",
            new Vector2D(10, 10),
            maxHealth: 100f,
            attackDamage: 10f,
            attackRange: 1.5f,
            movementSpeed: 3.5f,
            attackCooldownTicks: 20,
            killXpValue: 50);
        state.AddUnit(friendlyUnit);

        var enemyInRange = new UnitEntity(
            new EntityId(2),
            faction2,
            "archer",
            new Vector2D(15, 10), // Within 10 radius
            maxHealth: 80f,
            attackDamage: 12f,
            attackRange: 6f,
            movementSpeed: 3.5f,
            attackCooldownTicks: 20,
            killXpValue: 50);
        state.AddUnit(enemyInRange);

        var enemyOutOfRange = new UnitEntity(
            new EntityId(3),
            faction2,
            "cavalry",
            new Vector2D(50, 50), // Far away
            maxHealth: 150f,
            attackDamage: 20f,
            attackRange: 1.5f,
            movementSpeed: 5.5f,
            attackCooldownTicks: 20,
            killXpValue: 80);
        state.AddUnit(enemyOutOfRange);

        var perception = new AiPerceptionState(faction1);
        perception.UpdatePerception(state, currentTick: 1);

        Assert.True(perception.PerceivedEnemies.ContainsKey(enemyInRange.Id), "Enemy in range must be perceived.");
        Assert.False(perception.PerceivedEnemies.ContainsKey(enemyOutOfRange.Id), "Enemy outside fog of war must not be perceived.");

        // Threat calculation
        float threatNearFriendly = perception.GetThreatLevelNear(new Vector2D(10, 10), radius: 20f);
        Assert.True(threatNearFriendly > 0f);
    }

    [Fact]
    public void TC_S08_04_AiResourcePriority_CalculatesDynamicWeights()
    {
        var bank = new ResourceBank(new FactionId(1), initialFood: 50, initialWood: 50, initialGold: 20, initialStone: 20);
        var popManager = new PopulationManager(new FactionId(1), baseCapacity: 5);

        var weightsLowWorkers = AiResourcePriority.CalculateWeights(
            bank,
            popManager,
            activeWorkerCount: 2,
            targetWorkerCount: 15,
            isMilitaryProductionActive: false,
            isSiegeWanted: false);

        Assert.Equal(ResourceType.Food, weightsLowWorkers.PrimaryResourceDeficit);

        // Near pop cap triggers wood urgency
        popManager.SetCurrentPopulation(4, 1);
        var weightsNearPopCap = AiResourcePriority.CalculateWeights(
            bank,
            popManager,
            activeWorkerCount: 15,
            targetWorkerCount: 15,
            isMilitaryProductionActive: false,
            isSiegeWanted: false);

        Assert.Equal(ResourceType.Wood, weightsNearPopCap.PrimaryResourceDeficit);
    }

    [Fact]
    public void TC_S08_05_AiTargetingMatrix_PrefersArchetypeCounters()
    {
        // Spearmen prioritize Cavalry over Infantry
        float spearVsCav = AiTargetingMatrix.GetTargetPriority(UnitArchetype.Spearman, UnitArchetype.Cavalry);
        float spearVsInf = AiTargetingMatrix.GetTargetPriority(UnitArchetype.Spearman, UnitArchetype.Infantry);
        Assert.True(spearVsCav > spearVsInf, "Spearmen must prioritize cavalry.");

        // Cavalry prioritizes Archer over Spearman
        float cavVsArcher = AiTargetingMatrix.GetTargetPriority(UnitArchetype.Cavalry, UnitArchetype.Archer);
        float cavVsSpear = AiTargetingMatrix.GetTargetPriority(UnitArchetype.Cavalry, UnitArchetype.Spearman);
        Assert.True(cavVsArcher > cavVsSpear, "Cavalry must prioritize archers over dangerous spearmen.");

        // Siege prioritizes Gates and Towers
        float siegeVsGate = AiTargetingMatrix.GetBuildingTargetPriority(UnitArchetype.Siege, "gate");
        float siegeVsFarm = AiTargetingMatrix.GetBuildingTargetPriority(UnitArchetype.Siege, "farm");
        Assert.True(siegeVsGate > siegeVsFarm, "Siege units must prioritize fortifications.");
    }

    [Fact]
    public void TC_S08_06_AiBuildOrderPlan_ProgressesStepsAndResets()
    {
        var plan = AiBuildOrderPlan.CreateStandardPlan();
        Assert.False(plan.IsPlanFinished);
        Assert.NotNull(plan.CurrentStep);
        Assert.Equal(AiBuildStepType.TrainUnits, plan.CurrentStep.StepType);

        plan.AdvanceStep();
        Assert.Equal(AiBuildStepType.ConstructBuilding, plan.CurrentStep?.StepType);
        Assert.Equal("farm", plan.CurrentStep?.TargetIdentifier);

        plan.Reset();
        Assert.Equal(0, plan.CurrentStepIndex);
        Assert.Equal(AiBuildStepType.TrainUnits, plan.CurrentStep?.StepType);
    }
}
