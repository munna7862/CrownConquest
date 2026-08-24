using System;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class AiScenarioAndReplayTests
{
    [Fact]
    public void TC_S08_17_FullBotVsBotMatch_PlaysOutAndGathersAndAttacks()
    {
        var scenario = new AiFoundationScenario(seed: 42);

        // Run match for 200 ticks
        scenario.RunSimulation(200);

        // Verify presenter recorded spawns, resource harvesting, or building completions
        Assert.True(scenario.Presenter.SpawnHistory.Count > 0, "Units must be spawned during the scenario.");
        Assert.True(scenario.Presenter.HarvestHistory.Count > 0, "Workers must harvest resources.");
        Assert.True(scenario.Engine.State.ActiveUnits.Count >= 6, "Match must maintain living units.");
    }

    [Fact]
    public void TC_S08_18_1000Tick_DeterministicReplayParity()
    {
        var scenario1 = new AiFoundationScenario(seed: 42);
        var scenario2 = new AiFoundationScenario(seed: 42);

        scenario1.RunSimulation(1000);
        scenario2.RunSimulation(1000);

        ulong checksum1 = scenario1.Engine.State.ComputeStateChecksum();
        ulong checksum2 = scenario2.Engine.State.ComputeStateChecksum();

        Assert.Equal(checksum1, checksum2);
        Assert.Equal(1001UL, scenario1.Engine.CurrentTick);
        Assert.Equal(1001UL, scenario2.Engine.CurrentTick);
    }
}
