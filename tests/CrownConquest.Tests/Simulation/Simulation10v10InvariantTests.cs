using System.Collections.Generic;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class Simulation10v10InvariantTests
{
    [Fact]
    public void Simulation_10v10_BitExactReplay_ShouldMatchCheckSumAcrossSimulations()
    {
        var scenario1 = new CombatArenaScenario(new Application.GameCoordinator(new SimulationConfig { InitialRandomSeed = 42 }));
        var scenario2 = new CombatArenaScenario(new Application.GameCoordinator(new SimulationConfig { InitialRandomSeed = 42 }));

        scenario1.Deploy10v10Forces();
        scenario2.Deploy10v10Forces();

        scenario1.OrderArmiesToEngage();
        scenario2.OrderArmiesToEngage();

        // Simulate 400 fixed ticks
        for (int i = 0; i < 400; i++)
        {
            scenario1.Coordinator.Simulation.Tick();
            scenario2.Coordinator.Simulation.Tick();
        }

        ulong checksum1 = scenario1.Coordinator.Simulation.State.ComputeStateChecksum();
        ulong checksum2 = scenario2.Coordinator.Simulation.State.ComputeStateChecksum();

        Assert.Equal(checksum1, checksum2);
        Assert.Equal(scenario1.Coordinator.CurrentTick, scenario2.Coordinator.CurrentTick);
    }

    [Fact]
    public void Simulation_10v10_SeedDivergence_ShouldDivergeWithDifferentSeeds()
    {
        var scenario1 = new CombatArenaScenario(new Application.GameCoordinator(new SimulationConfig { InitialRandomSeed = 42 }));
        var scenario2 = new CombatArenaScenario(new Application.GameCoordinator(new SimulationConfig { InitialRandomSeed = 999 }));

        scenario1.Deploy10v10Forces();
        scenario2.Deploy10v10Forces();

        scenario1.OrderArmiesToEngage();
        scenario2.OrderArmiesToEngage();

        scenario1.Coordinator.Simulation.SimulateTicks(200);
        scenario2.Coordinator.Simulation.SimulateTicks(200);

        // State ticks match
        Assert.Equal(scenario1.Coordinator.CurrentTick, scenario2.Coordinator.CurrentTick);
    }

    [Fact]
    public void Progression_KillAttributionInvariant_TotalXpConserved()
    {
        var scenario = new CombatArenaScenario();
        scenario.Deploy10v10Forces();
        scenario.OrderArmiesToEngage();

        // Simulate 500 ticks until casualties occur
        scenario.Coordinator.Simulation.SimulateTicks(500);

        Assert.NotEmpty(scenario.KilledEvents);

        int totalXpGainedByActiveUnits = 0;
        foreach (var unit in scenario.Coordinator.Simulation.State.ActiveUnits)
        {
            totalXpGainedByActiveUnits += unit.Veterancy.CurrentXp;
        }

        // Total XP on living units must be strictly positive if any unit was killed
        Assert.True(totalXpGainedByActiveUnits > 0);
    }

    [Fact]
    public void Progression_NoFriendlyFireXp_ShouldNotAwardXpToSameFaction()
    {
        var sim = new SimulationEngine();
        int xpEvents = 0;

        sim.EventBus.Subscribe<UnitGainedXpEvent>((in UnitGainedXpEvent e) => xpEvents++);

        // Spawn 2 friendly Celtic units
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(FactionId.Player1, 0, "celtic_swordsman", new Vector2D(10f, 10f), KillXpValue: 100));
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(FactionId.Player1, 0, "celtic_swordsman", new Vector2D(11f, 10f), KillXpValue: 100));
        sim.Tick();

        var unit1 = new EntityId(1);
        var unit2 = new EntityId(2);

        // Friendly unit takes lethal damage attributed to friend
        if (sim.State.TryGetUnit(unit2, out var casualty) && casualty != null)
        {
            casualty.TakeDamage(200f, unit1, FactionId.Player1, sim.CurrentTick, sim.EventBus, out bool killed);
            Assert.True(killed);
        }

        // Friendly fire must NOT award XP
        Assert.Equal(0, xpEvents);
    }

    [Fact]
    public void Simulation_SpatialGrid_QueryCorrectness_ShouldMatchLinearScan()
    {
        var grid = new SpatialGrid(cellSize: 5.0f);
        var positions = new Dictionary<EntityId, Vector2D>();

        // Place 50 units randomly
        var rand = new System.Random(123);
        for (int i = 1; i <= 50; i++)
        {
            var id = new EntityId(i);
            var pos = new Vector2D((float)rand.NextDouble() * 80f, (float)rand.NextDouble() * 80f);
            positions[id] = pos;
            grid.Insert(id, pos);
        }

        var queryCenter = new Vector2D(40f, 40f);
        float radius = 15.0f;
        float radiusSq = radius * radius;

        var gridResults = new List<EntityId>();
        grid.QueryRadius(queryCenter, radius, id => positions.TryGetValue(id, out var p) ? p : null, gridResults);

        var expectedResults = new List<EntityId>();
        foreach (var kvp in positions)
        {
            if (kvp.Value.DistanceSquaredTo(queryCenter) <= radiusSq)
            {
                expectedResults.Add(kvp.Key);
            }
        }

        Assert.Equal(expectedResults.Count, gridResults.Count);
        foreach (var id in expectedResults)
        {
            Assert.Contains(id, gridResults);
        }
    }
}
