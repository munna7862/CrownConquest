using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class CivilizationProgressionScenarioTests
{
    [Fact]
    public void Scenario_CivilizationProgression_FullEvolution()
    {
        // TC-S04-016: Full scenario completes evolution, tech research, and army combat with victory
        var scenario = new CivilizationProgressionScenario();

        // Initial snapshot checks
        Assert.Equal(CivilizationEra.Archaic, scenario.Presenter.CurrentEra);
        Assert.Equal(4, scenario.Presenter.MilitaryComposition.Workers);
        Assert.Equal(0, scenario.Presenter.MilitaryComposition.Swordsmen);

        // Execute full evolution scenario
        scenario.ExecuteEvolutionScenario(out int totalTicks);

        Assert.True(totalTicks > 0, "Scenario should execute simulation ticks.");

        // Post-execution assertions
        var presenter = scenario.Presenter;
        Assert.Equal(CivilizationEra.Classical, presenter.CurrentEra);
        Assert.Contains("forging", presenter.UnlockedTechnologies);
        Assert.Contains("scale_armor", presenter.UnlockedTechnologies);
        Assert.Contains("fletching", presenter.UnlockedTechnologies);
        Assert.Contains("husbandry", presenter.UnlockedTechnologies);

        Assert.True(presenter.ActiveTechModifiers.MeleeAttackBonus >= 2);
        Assert.True(presenter.ActiveTechModifiers.MeleeArmorBonus >= 2);
        Assert.True(presenter.ActiveTechModifiers.RangedAttackBonus >= 1);
        Assert.True(presenter.ActiveTechModifiers.CavalrySpeedBonus >= 1.0f);

        // Player military defeated enemy force
        int enemyAlive = 0;
        foreach (var u in scenario.Coordinator.Simulation.State.ActiveUnits)
        {
            if (u.FactionId == scenario.EnemyFaction && u.IsAlive)
            {
                enemyAlive++;
            }
        }
        Assert.Equal(0, enemyAlive);
    }

    [Fact]
    public void Scenario_CivilizationProgressionPresenter_HudSync()
    {
        // TC-S04-017: Presenter mirrors authoritative state throughout advancement
        var scenario = new CivilizationProgressionScenario();
        var sim = scenario.Coordinator.Simulation;

        Assert.Equal(2500, scenario.Presenter.Food);
        Assert.Equal(1500, scenario.Presenter.Wood);
        Assert.Equal(1500, scenario.Presenter.Gold);

        // Start era advancement
        scenario.Coordinator.IssueAdvanceEraOrder(scenario.PlayerFaction, scenario.TownCenter.Id, CivilizationEra.Classical);
        sim.SimulateTicks(1);

        scenario.Presenter.UpdateSnapshot();
        Assert.True(scenario.Presenter.IsAdvancingEra);
        Assert.Equal(CivilizationEra.Classical, scenario.Presenter.TargetEra);
        Assert.True(scenario.Presenter.Food < 2500, "Advancement cost should be deducted.");

        // Advance halfway (50 ticks)
        sim.SimulateTicks(50);
        scenario.Presenter.UpdateSnapshot();
        Assert.True(scenario.Presenter.EraAdvancementProgressNormalized > 0.4f);
        Assert.True(scenario.Presenter.EraAdvancementProgressNormalized < 0.6f);

        // Complete advancement
        sim.SimulateTicks(55);
        scenario.Presenter.UpdateSnapshot();
        Assert.False(scenario.Presenter.IsAdvancingEra);
        Assert.Equal(CivilizationEra.Classical, scenario.Presenter.CurrentEra);
        Assert.Equal("Bronze / Classical Era", scenario.Presenter.EraDisplayName);
    }
}
