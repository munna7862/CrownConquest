using System;
using CrownConquest.Domain.Entities;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class HeroProgressionScenarioTests
{
    [Fact]
    public void Scenario_HeroProgression_FullEvolution_ExecutesSuccessfully()
    {
        // TC-S05-016: Full Headless Hero Progression E2E Scenario
        var scenario = new HeroProgressionScenario();

        // Validate initial state
        Assert.NotNull(scenario.HeroUnit);
        Assert.Equal("Brennus", scenario.Presenter.HeroName);
        Assert.Equal(HeroClass.Warlord, scenario.Presenter.Class);
        Assert.Equal(1, scenario.Presenter.Level);
        Assert.Equal(4, scenario.Presenter.AttachedSquadCount);
        Assert.True(scenario.Presenter.AttachedSquadCount <= scenario.Presenter.LeadershipCapacity);

        // Run full scenario execution
        scenario.ExecuteFullScenario();

        // Validate outcomes
        Assert.True(scenario.TotalAbilitiesCastObserved >= 1, "Expected hero abilities to be cast.");
        Assert.True(scenario.VictoryConditionAchieved, "Expected player victory over enemy warband.");
        Assert.True(scenario.HeroUnit.IsAlive, "Expected Hero to survive battle.");
        Assert.True(scenario.Presenter.Level >= 2, "Expected Hero to have leveled up from combat kills.");
    }

    [Fact]
    public void Scenario_HeroPresenter_HudSync_MatchesSimulationState()
    {
        // TC-S05-017: Hero presenter synchronizes with domain state with 0 drift
        var scenario = new HeroProgressionScenario();
        var presenter = scenario.Presenter;

        presenter.UpdateSnapshot();
        Assert.True(presenter.HasActiveHero);
        Assert.Equal(scenario.HeroUnit.CurrentHealth, presenter.CurrentHealth);
        Assert.Equal(scenario.HeroUnit.MaxHealth, presenter.MaxHealth);
        Assert.Equal(scenario.HeroUnit.HeroState!.CurrentMana, presenter.CurrentMana);
        Assert.Equal(scenario.HeroUnit.HeroState.TotalAttributes.Strength, presenter.Strength);
        Assert.Equal(scenario.HeroUnit.HeroState.TotalAttributes.Agility, presenter.Agility);
        Assert.Equal(scenario.HeroUnit.HeroState.TotalAttributes.Willpower, presenter.Willpower);
        Assert.Equal(scenario.HeroUnit.HeroState.Abilities.Count, presenter.AbilityCards.Count);
    }
}
