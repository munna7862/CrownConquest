using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class DeterministicSimulationTests
{
    [Fact]
    public void Simulation_BitExactReplay_IdenticalSeed_ShouldProduceIdenticalChecksums()
    {
        var config1 = new SimulationConfig { InitialRandomSeed = 12345 };
        var config2 = new SimulationConfig { InitialRandomSeed = 12345 };

        var sim1 = new SimulationEngine(config1);
        var sim2 = new SimulationEngine(config2);

        // Spawn identical units in both simulations
        var spawnCmd1 = new SpawnUnitCommand(FactionId.Player1, 0, "celtic_swordsman", new Vector2D(10f, 10f));
        var spawnCmd2 = new SpawnUnitCommand(FactionId.Player2, 0, "roman_legionary", new Vector2D(20f, 10f));

        sim1.CommandQueue.Enqueue(spawnCmd1);
        sim1.CommandQueue.Enqueue(spawnCmd2);

        sim2.CommandQueue.Enqueue(spawnCmd1);
        sim2.CommandQueue.Enqueue(spawnCmd2);

        // Run 100 ticks
        sim1.SimulateTicks(100);
        sim2.SimulateTicks(100);

        // Order Unit 1 to move to (15, 10) in both
        var moveCmd = new MoveCommand(FactionId.Player1, 100, [new EntityId(1)], new Vector2D(15f, 10f));
        sim1.CommandQueue.Enqueue(moveCmd);
        sim2.CommandQueue.Enqueue(moveCmd);

        // Run another 100 ticks
        sim1.SimulateTicks(100);
        sim2.SimulateTicks(100);

        ulong hash1 = sim1.State.ComputeStateChecksum();
        ulong hash2 = sim2.State.ComputeStateChecksum();

        Assert.Equal(hash1, hash2);
        Assert.Equal(200UL, sim1.CurrentTick);
        Assert.Equal(200UL, sim2.CurrentTick);
    }

    [Fact]
    public void Simulation_RandomGenerator_ShouldBeDeterministic()
    {
        var rng1 = new SimulationRandom(42);
        var rng2 = new SimulationRandom(42);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(rng1.NextUInt(), rng2.NextUInt());
            Assert.Equal(rng1.NextFloat(), rng2.NextFloat());
            Assert.Equal(rng1.NextRange(10, 50), rng2.NextRange(10, 50));
        }
    }
}
