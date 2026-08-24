using System.Collections.Generic;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class TacticalAiIntegrationTests
{
    [Fact]
    public void TC_S09_13_AggressiveRaiderAi_AdoptsOffensiveProfileAndWedge()
    {
        var config = new SimulationConfig { InitialRandomSeed = 42 };
        var engine = new SimulationEngine(config, bounds: new BattlefieldBounds(0, 0, 100, 100));

        var factionId = new FactionId(1);
        var controller = new AiFactionController(
            factionId,
            new Vector2D(20, 20),
            personality: AiPersonalityProfile.CreateAggressive());

        engine.RegisterAiController(controller);

        Assert.Equal(AiPersonalityType.Aggressive, controller.Personality.PersonalityType);
        Assert.Equal(FormationType.Wedge, controller.Personality.PreferredFormation);
        Assert.Equal(6, controller.Personality.AttackSquadThreshold);
        Assert.Equal(0.25f, controller.Personality.RetreatOddsThreshold);
    }

    [Fact]
    public void TC_S09_14_DefensiveBastionAi_GuardsWidePerimeterAndHasHighRetreatThreshold()
    {
        var config = new SimulationConfig { InitialRandomSeed = 42 };
        var engine = new SimulationEngine(config, bounds: new BattlefieldBounds(0, 0, 100, 100));

        var factionId = new FactionId(1);
        var controller = new AiFactionController(
            factionId,
            new Vector2D(20, 20),
            personality: AiPersonalityProfile.CreateDefensive());

        engine.RegisterAiController(controller);

        Assert.Equal(AiPersonalityType.Defensive, controller.Personality.PersonalityType);
        Assert.Equal(45.0f, controller.Personality.BaseDefenseRadius);
        Assert.Equal(0.55f, controller.Personality.RetreatOddsThreshold);
        Assert.Equal(FormationType.Square, controller.Personality.PreferredFormation);
    }

    [Fact]
    public void TC_S09_15_ExpansionistImperialAi_SetsHighWorkerTarget()
    {
        var config = new SimulationConfig { InitialRandomSeed = 42 };
        var engine = new SimulationEngine(config, bounds: new BattlefieldBounds(0, 0, 100, 100));

        var factionId = new FactionId(1);
        var controller = new AiFactionController(
            factionId,
            new Vector2D(20, 20),
            personality: AiPersonalityProfile.CreateExpansionist());

        engine.RegisterAiController(controller);

        Assert.Equal(AiPersonalityType.Expansionist, controller.Personality.PersonalityType);
        Assert.Equal(24, controller.TargetWorkerCount);
        Assert.Equal(16, controller.Personality.AttackSquadThreshold);
    }

    [Fact]
    public void TC_S09_16_TacticalHeroCentricAi_RetreatsWhenHeroHealthIsCritical()
    {
        var config = new SimulationConfig { InitialRandomSeed = 42 };
        var engine = new SimulationEngine(config, bounds: new BattlefieldBounds(0, 0, 100, 100));

        var faction1 = new FactionId(1);
        var faction2 = new FactionId(2);

        var hero = new UnitEntity(engine.State.GenerateEntityId(), faction1, "hero_commander", new Vector2D(50, 50), maxHealth: 500f, attackDamage: 30, archetype: UnitArchetype.Hero);
        engine.State.AddUnit(hero);

        var spearman = new UnitEntity(engine.State.GenerateEntityId(), faction1, "spearman", new Vector2D(51, 50), 100, 10, archetype: UnitArchetype.Spearman);
        engine.State.AddUnit(spearman);

        var enemy = new UnitEntity(engine.State.GenerateEntityId(), faction2, "infantry", new Vector2D(55, 50), 100, 10, archetype: UnitArchetype.Infantry);
        engine.State.AddUnit(enemy);

        var controller = new AiFactionController(
            faction1,
            new Vector2D(20, 20),
            personality: AiPersonalityProfile.CreateTactical());

        controller.ArmySquad.SetState(AiSquadState.Attacking);
        engine.RegisterAiController(controller);

        // Damage hero to 10% health (< 30% threshold)
        hero.TakeDamage(450f, enemy.Id, faction2, 0, engine.EventBus, out _);

        engine.SimulateTicks(5); // Runs AI decision cycle

        Assert.Equal(AiSquadState.Retreating, controller.ArmySquad.State);
    }
}
