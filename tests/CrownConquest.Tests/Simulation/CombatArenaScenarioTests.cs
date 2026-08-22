using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class CombatArenaScenarioTests
{
    [Fact]
    public void Scenario_10v10_FullBattleResolution_ShouldSimulateCleanlyToCasualtiesAndLevelUps()
    {
        var presenter = new CombatArenaPresenter();

        // 1. Deploy 10 Celtic vs 10 Roman forces
        presenter.Scenario.Deploy10v10Forces();

        var initialViewModels = presenter.GetUnitViewModels();
        Assert.Equal(20, initialViewModels.Count);

        // 2. Order forces to engage
        presenter.Scenario.OrderArmiesToEngage();

        // 3. Simulate battle for 1000 ticks
        presenter.Coordinator.Simulation.SimulateTicks(1000);

        // 4. Verify battle resolution
        Assert.NotEmpty(presenter.Scenario.KilledEvents);
        Assert.NotEmpty(presenter.Scenario.LevelUpEvents);
        Assert.NotEmpty(presenter.CombatLog);

        // Verify surviving units have gained experience and kills
        var survivors = presenter.GetUnitViewModels();
        Assert.True(survivors.Count < 20); // Casualties occurred

        bool anyLeveledUp = false;
        for (int i = 0; i < survivors.Count; i++)
        {
            if (survivors[i].Level > 1)
            {
                anyLeveledUp = true;
                Assert.True(survivors[i].KillCount > 0);
                Assert.True(survivors[i].CurrentXp > 0);
                Assert.True(survivors[i].MaxHealth > 80f);
            }
        }

        Assert.True(anyLeveledUp, "Expected at least one surviving unit to have scored a kill and leveled up.");
    }

    [Fact]
    public void Scenario_UnitRoster_StatsReflection_ShouldMatchSimulationTruth()
    {
        var presenter = new CombatArenaPresenter();
        presenter.Scenario.Deploy10v10Forces();

        // Select the first Celtic swordsman (ID 1)
        presenter.Selection.SelectPoint(new Vector2D(25f, 35f));

        var primary = presenter.GetPrimarySelectedUnitViewModel();
        Assert.NotNull(primary);
        Assert.Equal(new EntityId(1), primary.Value.Id);
        Assert.Equal(FactionId.Player1, primary.Value.FactionId);
        Assert.Equal("celtic_swordsman", primary.Value.UnitType);
        Assert.Equal(1, primary.Value.Level);
        Assert.Equal(VeterancyRank.Recruit, primary.Value.Rank);
        Assert.True(primary.Value.IsSelected);
    }
}
