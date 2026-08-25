using System;
using System.Diagnostics;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Simulation;

/// <summary>
/// Configuration for long-running simulation soak tests.
/// </summary>
public sealed class SoakTestConfig
{
    public int TargetTicks { get; set; } = 5000;
    public int RandomSeed { get; set; } = 42;
    public int MaxMemoryBudgetMb { get; set; } = 500;
    public int UnitSpawnCadenceTicks { get; set; } = 100;
    public int CombatEngagementCadenceTicks { get; set; } = 250;

    public static SoakTestConfig CreateFast(int ticks = 2000) => new() { TargetTicks = ticks };
    public static SoakTestConfig CreateFull(int ticks = 10000) => new() { TargetTicks = ticks };
}

/// <summary>
/// Telemetry outcome of a simulation soak test run.
/// </summary>
public sealed record SoakTestResult(
    bool IsSuccessful,
    int TotalTicksExecuted,
    long ElapsedMilliseconds,
    float TicksPerSecond,
    float PeakMemoryMb,
    float FinalMemoryMb,
    int TotalUnitsSpawned,
    int TotalUnitsKilled,
    int TotalBuildingsConstructed,
    bool IsMemoryBounded,
    bool IsSpatialGridConsistent,
    string SummaryDetails);

/// <summary>
/// High-throughput soak testing harness validating simulation stability, entity lifecycle recycling,
/// spatial partitioning consistency, and memory boundedness over thousands of ticks.
/// </summary>
public sealed class SimulationSoakHarness
{
    public SoakTestResult RunSoakTest(SoakTestConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var simConfig = new SimulationConfig
        {
            InitialRandomSeed = config.RandomSeed,
            TicksPerSecond = 20
        };

        var eventBus = new DomainEventBus();
        var sim = new SimulationEngine(simConfig, eventBus);

        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        sim.State.GetOrCreateResourceBank(f1).Deposit(ResourceType.Food, 10000, 0);
        sim.State.GetOrCreateResourceBank(f2).Deposit(ResourceType.Food, 10000, 0);
        sim.State.GetOrCreateResourceBank(f1).Deposit(ResourceType.Wood, 10000, 0);
        sim.State.GetOrCreateResourceBank(f2).Deposit(ResourceType.Wood, 10000, 0);

        int totalSpawned = 0;
        int totalKilled = 0;
        int totalBuildings = 0;

        eventBus.Subscribe<UnitKilledEvent>((in UnitKilledEvent _) => totalKilled++);

        var sw = Stopwatch.StartNew();
        long peakMemoryBytes = 0;

        for (int tick = 0; tick < config.TargetTicks; tick++)
        {
            sim.Tick();

            // Periodic unit spawning
            if (tick % config.UnitSpawnCadenceTicks == 0 && sim.State.ActiveUnits.Count < 300)
            {
                var u1 = new UnitEntity(
                    sim.State.GenerateEntityId(),
                    f1,
                    "swordsman",
                    new Vector2D(10f + (tick % 20), 10f + (tick % 20)),
                    100f,
                    12f,
                    1.5f,
                    3.5f,
                    18,
                    50,
                    2f,
                    "melee",
                    12f,
                    archetype: UnitArchetype.Infantry);
                sim.State.AddUnit(u1);
                sim.SpatialGrid.Insert(u1.Id, u1.Position);
                totalSpawned++;

                var u2 = new UnitEntity(
                    sim.State.GenerateEntityId(),
                    f2,
                    "spearman",
                    new Vector2D(50f - (tick % 20), 50f - (tick % 20)),
                    100f,
                    12f,
                    2.0f,
                    3.2f,
                    20,
                    50,
                    2f,
                    "melee",
                    12f,
                    archetype: UnitArchetype.Spearman);
                sim.State.AddUnit(u2);
                sim.SpatialGrid.Insert(u2.Id, u2.Position);
                totalSpawned++;
            }

            // Periodic combat orders
            if (tick % config.CombatEngagementCadenceTicks == 0 && sim.State.ActiveUnits.Count > 0)
            {
                var units = sim.State.ActiveUnits;
                for (int u = 0; u < units.Count; u++)
                {
                    var unit = units[u];
                    if (unit.IsAlive)
                    {
                        var targetPos = unit.FactionId == f1 ? new Vector2D(40f, 40f) : new Vector2D(20f, 20f);
                        sim.CommandQueue.Enqueue(new MoveCommand(unit.FactionId, (ulong)tick, new[] { unit.Id }, targetPos));
                    }
                }
            }

            // Memory tracking every 500 ticks
            if (tick % 500 == 0)
            {
                long currentMem = GC.GetTotalMemory(false);
                if (currentMem > peakMemoryBytes)
                {
                    peakMemoryBytes = currentMem;
                }
            }
        }

        sw.Stop();

        long finalMem = GC.GetTotalMemory(false);
        if (finalMem > peakMemoryBytes) peakMemoryBytes = finalMem;

        float peakMb = peakMemoryBytes / (1024f * 1024f);
        float finalMb = finalMem / (1024f * 1024f);
        bool memoryBounded = peakMb <= config.MaxMemoryBudgetMb;

        // Verify spatial grid consistency
        int activeCount = 0;
        for (int i = 0; i < sim.State.ActiveUnits.Count; i++)
        {
            if (sim.State.ActiveUnits[i].IsAlive) activeCount++;
        }

        bool spatialConsistent = sim.SpatialGrid.TotalIndexedEntities == activeCount;

        float tps = sw.ElapsedMilliseconds > 0
            ? (config.TargetTicks * 1000f) / sw.ElapsedMilliseconds
            : config.TargetTicks;

        bool success = memoryBounded && spatialConsistent;
        string summary = success
            ? $"Soak test completed {config.TargetTicks} ticks successfully at {tps:F1} TPS. Peak Memory: {peakMb:F1} MB."
            : $"Soak test failed. MemoryBounded={memoryBounded} ({peakMb:F1}MB / {config.MaxMemoryBudgetMb}MB), SpatialConsistent={spatialConsistent}";

        return new SoakTestResult(
            success,
            config.TargetTicks,
            sw.ElapsedMilliseconds,
            tps,
            peakMb,
            finalMb,
            totalSpawned,
            totalKilled,
            totalBuildings,
            memoryBounded,
            spatialConsistent,
            summary);
    }
}
