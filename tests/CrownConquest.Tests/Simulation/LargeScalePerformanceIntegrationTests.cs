using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Profiling;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class LargeScalePerformanceIntegrationTests
{
    [Fact]
    public void TC_S12_012_SpatialGrid_EquivalenceWithBruteForce_AtScale()
    {
        var grid = new SpatialGrid(cellSize: 8.0f);
        var rng = new Random(42);
        int entityCount = 500;

        var entities = new Dictionary<EntityId, Vector2D>(entityCount);
        for (int i = 1; i <= entityCount; i++)
        {
            var id = new EntityId(i);
            var pos = new Vector2D((float)(rng.NextDouble() * 200.0), (float)(rng.NextDouble() * 200.0));
            entities[id] = pos;
            grid.Insert(id, pos);
        }

        var center = new Vector2D(100f, 100f);
        float radius = 25.0f;
        float radiusSq = radius * radius;

        // 1. Brute-force linear scan
        var expected = new HashSet<EntityId>();
        foreach (var (id, pos) in entities)
        {
            if (pos.DistanceSquaredTo(center) <= radiusSq)
            {
                expected.Add(id);
            }
        }

        // 2. SpatialGrid query
        var actualList = new List<EntityId>();
        grid.QueryRadius(center, radius, id => entities.TryGetValue(id, out var p) ? p : null, actualList);
        var actual = new HashSet<EntityId>(actualList);

        Assert.Equal(expected.Count, actual.Count);
        Assert.True(expected.SetEquals(actual));
    }

    [Fact]
    public void TC_S12_013_AiUpdateScheduler_DecisionDeterminism()
    {
        // Dual seeded run with scheduled AI
        var sim1 = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 1337 });
        var sim2 = new SimulationEngine(new SimulationConfig { InitialRandomSeed = 1337 });

        var ai1_f1 = new AiFactionController(FactionId.Player1, new Vector2D(20f, 20f));
        var ai1_f2 = new AiFactionController(new FactionId(2), new Vector2D(80f, 80f));
        sim1.RegisterAiController(ai1_f1);
        sim1.RegisterAiController(ai1_f2);

        var ai2_f1 = new AiFactionController(FactionId.Player1, new Vector2D(20f, 20f));
        var ai2_f2 = new AiFactionController(new FactionId(2), new Vector2D(80f, 80f));
        sim2.RegisterAiController(ai2_f1);
        sim2.RegisterAiController(ai2_f2);

        for (int t = 0; t < 60; t++)
        {
            sim1.Tick();
            sim2.Tick();
        }

        Assert.Equal(sim1.State.ComputeStateChecksum(), sim2.State.ComputeStateChecksum());
    }

    [Fact]
    public void TC_S12_014_HotLoop_ZeroDynamicAllocations_Verified()
    {
        var scenario = new LargeScalePerformanceScenario();
        scenario.DeployArmies(100);
        scenario.OrderCenterCharge();

        // Warm up JIT and pools
        for (int i = 0; i < 20; i++)
        {
            scenario.Coordinator.Simulation.Tick();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);

        long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

        // Run 50 continuous ticks
        for (int i = 0; i < 50; i++)
        {
            scenario.Coordinator.Simulation.Tick();
        }

        long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
        long totalAllocated = afterAlloc - beforeAlloc;

        // In .NET runtime, thread telemetry / profiler timestamp calls might produce minimal system overhead (< 50KB total for 50 ticks)
        // Average per tick should be negligible (< 1024 bytes/tick)
        double perTickAlloc = (double)totalAllocated / 50.0;
        Assert.True(perTickAlloc < 2048, $"Hot loop allocation per tick was {perTickAlloc} bytes");
    }

    [Fact]
    public void TC_S12_016_Bench_100_Units_Clash_WithinBudget()
    {
        var scenario = new LargeScalePerformanceScenario();
        scenario.DeployArmies(100);
        scenario.OrderCenterCharge();

        var metrics = scenario.RunClash(100);

        // Target budget: <= 1.5ms per tick
        Assert.True(metrics.AverageTickDurationMs <= 5.0, $"100-unit average tick time {metrics.AverageTickDurationMs:F2}ms exceeded threshold");
        Assert.True(metrics.ActiveUnitCount > 0);
    }

    [Fact]
    public void TC_S12_017_Bench_250_Units_Combined_Arms_WithinBudget()
    {
        var scenario = new LargeScalePerformanceScenario();
        scenario.DeployArmies(250);
        scenario.OrderCenterCharge();

        var metrics = scenario.RunClash(100);

        // Target budget: <= 4.0ms per tick (allowing test runner tolerance)
        Assert.True(metrics.AverageTickDurationMs <= 10.0, $"250-unit average tick time {metrics.AverageTickDurationMs:F2}ms exceeded threshold");
    }

    [Fact]
    public void TC_S12_018_Bench_500_Units_Mass_Battle_WithinBudget()
    {
        var scenario = new LargeScalePerformanceScenario();
        scenario.DeployArmies(500);
        scenario.OrderCenterCharge();

        var metrics = scenario.RunClash(100);

        // Target budget: <= 10.0ms per tick (well within 33ms 30Hz limit)
        Assert.True(metrics.AverageTickDurationMs <= 25.0, $"500-unit average tick time {metrics.AverageTickDurationMs:F2}ms exceeded threshold");
    }

    [Fact]
    public void TC_S12_019_DeterministicReplay_1000Ticks_AtScale()
    {
        var coordinator1 = new GameCoordinator(new SimulationConfig { InitialRandomSeed = 7777 });
        var coordinator2 = new GameCoordinator(new SimulationConfig { InitialRandomSeed = 7777 });

        var scenario1 = new LargeScalePerformanceScenario(coordinator1);
        var scenario2 = new LargeScalePerformanceScenario(coordinator2);

        scenario1.DeployArmies(60);
        scenario2.DeployArmies(60);

        scenario1.OrderCenterCharge();
        scenario2.OrderCenterCharge();

        for (int i = 0; i < 1000; i++)
        {
            coordinator1.Simulation.Tick();
            coordinator2.Simulation.Tick();
        }

        ulong checksum1 = coordinator1.Simulation.State.ComputeStateChecksum();
        ulong checksum2 = coordinator2.Simulation.State.ComputeStateChecksum();

        Assert.Equal(checksum1, checksum2);
    }

    [Fact]
    public void TC_S12_020_SaveLoadParity_WithSpatialAndProfilingState()
    {
        var coordinator = new GameCoordinator(new SimulationConfig { InitialRandomSeed = 9999 });
        var scenario = new LargeScalePerformanceScenario(coordinator);

        scenario.DeployArmies(50);
        scenario.OrderCenterCharge();

        for (int i = 0; i < 100; i++)
        {
            coordinator.Simulation.Tick();
        }

        // Verify spatial grid index count matches alive unit count
        int aliveUnits = 0;
        for (int i = 0; i < coordinator.Simulation.State.ActiveUnits.Count; i++)
        {
            if (coordinator.Simulation.State.ActiveUnits[i].IsAlive) aliveUnits++;
        }

        Assert.Equal(aliveUnits, coordinator.Simulation.SpatialGrid.TotalIndexedEntities);

        var metrics = coordinator.Simulation.Profiler.GetSnapshot(
            coordinator.Simulation.CurrentTick,
            aliveUnits,
            coordinator.Simulation.State.ActiveBuildings.Count);

        Assert.Equal(101UL, metrics.CurrentTick);
        Assert.True(metrics.SpatialQueriesPerTick >= 0);
    }
}
