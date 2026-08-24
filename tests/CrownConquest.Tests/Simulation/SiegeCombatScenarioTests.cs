using System;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class SiegeCombatScenarioTests
{
    [Fact]
    public void TC_S07_17_FullFortressAssault_ScenarioExecutesAndBreachesFortifications()
    {
        // Arrange
        var scenario = new SiegeWarfareScenario(seed: 123);
        scenario.SetupFortressMatch();

        // Act: Run assault for 150 ticks
        scenario.RunAssault(simulationTicks: 150);

        // Assert:
        // 1. Presenter should have recorded breach and tower attacks
        Assert.NotEmpty(scenario.Presenter.TowerAttackHistory);
        Assert.NotEmpty(scenario.Presenter.BuildingAttackedHistory);

        // 2. Wall or Gate should have taken massive structural damage
        if (scenario.Engine.State.TryGetBuilding(scenario.DefenderWallId, out var wall))
        {
            Assert.True(wall == null || !wall.IsAlive || wall.CurrentHealth < 200f);
        }
    }

    [Fact]
    public void TC_S07_18_DeterministicReplayParity_1000TicksSiegeWarfare_BitForBitChecksumEquality()
    {
        const int seed = 998877;
        const int ticks = 1000;

        // Run 1
        var scenario1 = new SiegeWarfareScenario(seed: seed);
        scenario1.SetupFortressMatch();
        scenario1.RunAssault(simulationTicks: ticks);
        ulong checksum1 = scenario1.Engine.State.ComputeStateChecksum();

        // Run 2
        var scenario2 = new SiegeWarfareScenario(seed: seed);
        scenario2.SetupFortressMatch();
        scenario2.RunAssault(simulationTicks: ticks);
        ulong checksum2 = scenario2.Engine.State.ComputeStateChecksum();

        // Assert 1000-tick bit-for-bit parity
        Assert.Equal(checksum1, checksum2);
        Assert.NotEqual(0UL, checksum1);
    }
}
