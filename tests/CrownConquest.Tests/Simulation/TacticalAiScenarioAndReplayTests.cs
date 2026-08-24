using CrownConquest.Domain.Common;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class TacticalAiScenarioAndReplayTests
{
    [Fact]
    public void TC_S09_17_TacticalAiScenario_ExecutesAutonomousMatch()
    {
        var scenario = new TacticalAiScenario(seed: 42);

        // Run 200 simulation ticks
        scenario.RunSimulation(200);

        Assert.True(scenario.Engine.CurrentTick >= 200);
        Assert.True(scenario.Presenter.FormationHistory.Count > 0, "Formation changes should be recorded by presenter");
    }

    [Fact]
    public void TC_S09_18_ReplayParity_1000Ticks_BitExactStateChecksumEquality()
    {
        const int seed = 12345;
        const int totalTicks = 1000;

        var scenario1 = new TacticalAiScenario(seed);
        var scenario2 = new TacticalAiScenario(seed);

        scenario1.RunSimulation(totalTicks);
        scenario2.RunSimulation(totalTicks);

        ulong checksum1 = scenario1.Engine.State.ComputeStateChecksum();
        ulong checksum2 = scenario2.Engine.State.ComputeStateChecksum();

        Assert.Equal(checksum1, checksum2);
        Assert.Equal((ulong)(totalTicks + 1), scenario1.Engine.CurrentTick);
        Assert.Equal((ulong)(totalTicks + 1), scenario2.Engine.CurrentTick);
    }
}
