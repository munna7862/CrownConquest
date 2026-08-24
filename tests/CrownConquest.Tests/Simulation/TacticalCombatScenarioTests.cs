using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class TacticalCombatScenarioTests
{
    [Fact]
    public void TC_S06_17_TacticalCombatScenario_ExecutesFullHeadlessMatchAndPresenter()
    {
        var scenario = new TacticalCombatScenario();
        scenario.SetupTacticalBattlefield();

        // 1. Verify terrain grid initialized
        var grid = scenario.Coordinator.Simulation.State.TerrainGrid;
        var centerNorth = grid.GridToWorld(30, 42);
        Assert.Equal(TerrainType.Hills, grid.GetTerrainAt(centerNorth));

        // 2. Spawn encounters
        var (blueSpears, redCavs) = scenario.SpawnChargeTestEncounter();
        Assert.Equal(4, blueSpears.Count);
        Assert.Equal(4, redCavs.Count);

        // 3. Test TacticalCombatPresenter view models
        var presenter = new TacticalCombatPresenter(scenario.Coordinator, scenario.BlueFaction);
        presenter.SelectUnits(blueSpears);

        Assert.Equal(4, presenter.SelectedCount);
        Assert.Equal(FormationType.ShieldWall, presenter.ActiveFormation);
        Assert.Equal(MoraleLevel.Confident, presenter.PrimaryMoraleLevel);
        Assert.Equal(6, presenter.FormationOptions.Count);
        Assert.Equal(4, presenter.UnitCards.Count);

        // 4. Change formation via presenter
        presenter.SetFormation(FormationType.Square);
        scenario.Coordinator.Update(0.05f); // 1 tick

        if (scenario.Coordinator.Simulation.State.TryGetUnit(blueSpears[0], out var spearUnit))
        {
            Assert.NotNull(spearUnit);
            Assert.Equal(FormationType.Square, spearUnit.Formation);
        }

        // Change back to Shield Wall
        presenter.SetFormation(FormationType.ShieldWall);
        scenario.Coordinator.Update(0.05f);

        // 5. Order Cavalry to attack Spearmen
        for (int i = 0; i < redCavs.Count; i++)
        {
            if (scenario.Coordinator.Simulation.State.TryGetUnit(redCavs[i], out var cav) && cav != null)
            {
                cav.Attack(blueSpears[i % blueSpears.Count]);
            }
        }

        // 6. Simulate battle until cavalry is eliminated
        for (int step = 0; step < 200; step++)
        {
            scenario.Coordinator.Update(0.05f);
        }

        presenter.UpdateSnapshot();

        // Blue spearmen in shield wall withstand cavalry charge
        int survivingBlue = 0;
        for (int i = 0; i < blueSpears.Count; i++)
        {
            if (scenario.Coordinator.Simulation.State.TryGetUnit(blueSpears[i], out var u) && u != null && u.IsAlive)
            {
                survivingBlue++;
            }
        }

        int survivingRed = 0;
        for (int i = 0; i < redCavs.Count; i++)
        {
            if (scenario.Coordinator.Simulation.State.TryGetUnit(redCavs[i], out var u) && u != null && u.IsAlive)
            {
                survivingRed++;
            }
        }

        Assert.True(survivingBlue >= 2);
        Assert.Equal(0, survivingRed);
    }
}
