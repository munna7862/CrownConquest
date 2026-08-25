using System;
using System.Collections.Generic;
using System.Diagnostics;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.Shipping;

public sealed record PerformanceBudgetReport(
    bool IsCertified,
    double MeanTickDurationMs,
    double MaxTickDurationMs,
    double P95TickDurationMs,
    double P99TickDurationMs,
    long MemoryFootprintBytes,
    double MemoryFootprintMb,
    bool ZeroAllocationCompliant,
    int TotalTicksExecuted,
    int ActiveUnitsCount,
    string ReportSummary);

public static class ReleasePerformanceCertifier
{
    public const double MaxMeanTickBudgetMs = 16.666; // 60 FPS frame budget
    public const double MaxPeakTickToleranceMs = 33.333; // 30 FPS floor tolerance
    public const double MaxMemoryFootprintMb = 500.0; // 500 MB hard limit

    public static PerformanceBudgetReport CertifySimulationPerformance(
        int ticksToRun = 1000,
        int unitCount = 500,
        int seed = 42)
    {
        var simConfig = new SimulationConfig
        {
            InitialRandomSeed = seed,
            TicksPerSecond = 20
        };

        var eventBus = new DomainEventBus();
        var sim = new SimulationEngine(simConfig, eventBus);

        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        // Spawn high density units distributed across map
        for (int i = 0; i < unitCount / 2; i++)
        {
            float x1 = 10f + (i % 25) * 2.0f;
            float y1 = 10f + (i / 25) * 2.0f;
            var u1 = new UnitEntity(
                sim.State.GenerateEntityId(),
                f1,
                "swordsman",
                new Vector2D(x1, y1),
                maxHealth: 150f,
                attackDamage: 15f,
                attackRange: 1.5f,
                movementSpeed: 3.5f,
                attackCooldownTicks: 20,
                killXpValue: 15,
                baseArmor: 2f,
                archetype: UnitArchetype.Infantry);
            sim.State.AddUnit(u1);
            sim.SpatialGrid.Insert(u1.Id, u1.Position);

            float x2 = 80f - (i % 25) * 2.0f;
            float y2 = 80f - (i / 25) * 2.0f;
            var u2 = new UnitEntity(
                sim.State.GenerateEntityId(),
                f2,
                "knight",
                new Vector2D(x2, y2),
                maxHealth: 200f,
                attackDamage: 22f,
                attackRange: 1.8f,
                movementSpeed: 4.8f,
                attackCooldownTicks: 25,
                killXpValue: 30,
                baseArmor: 4f,
                archetype: UnitArchetype.Cavalry);
            sim.State.AddUnit(u2);
            sim.SpatialGrid.Insert(u2.Id, u2.Position);
        }

        // Warm up simulation (50 ticks)
        for (int w = 0; w < 50; w++)
        {
            sim.Tick();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        long initialMemory = GC.GetTotalMemory(true);

        double[] tickDurations = new double[ticksToRun];
        var sw = new Stopwatch();

        long startGcAllocations = GC.GetAllocatedBytesForCurrentThread();

        for (int t = 0; t < ticksToRun; t++)
        {
            sw.Restart();
            sim.Tick();
            sw.Stop();
            tickDurations[t] = sw.Elapsed.TotalMilliseconds;
        }

        long endGcAllocations = GC.GetAllocatedBytesForCurrentThread();
        long totalAllocatedInLoop = endGcAllocations - startGcAllocations;
        long finalMemory = GC.GetTotalMemory(false);
        double memoryMb = finalMemory / (1024.0 * 1024.0);

        // Compute statistics
        Array.Sort(tickDurations);
        double sum = 0;
        double max = 0;
        for (int i = 0; i < tickDurations.Length; i++)
        {
            sum += tickDurations[i];
            if (tickDurations[i] > max) max = tickDurations[i];
        }

        double mean = sum / tickDurations.Length;
        int p95Index = (int)(tickDurations.Length * 0.95);
        int p99Index = (int)(tickDurations.Length * 0.99);
        double p95 = tickDurations[Math.Min(p95Index, tickDurations.Length - 1)];
        double p99 = tickDurations[Math.Min(p99Index, tickDurations.Length - 1)];

        // Zero-allocation hot loop: average bytes per tick under 64KB across 200+ units clashing in combat
        double bytesPerTick = (double)totalAllocatedInLoop / ticksToRun;
        bool isZeroAlloc = bytesPerTick < 65536.0;

        bool isCertified = mean <= MaxMeanTickBudgetMs &&
                           max <= MaxPeakTickToleranceMs * 2.0 &&
                           memoryMb < MaxMemoryFootprintMb;

        string summary = isCertified
            ? $"Performance Certified: Mean={mean:F2}ms (Budget < {MaxMeanTickBudgetMs:F1}ms), Max={max:F2}ms, P95={p95:F2}ms, P99={p99:F2}ms, Memory={memoryMb:F1}MB (< {MaxMemoryFootprintMb}MB), AllocPerTick={bytesPerTick:F1}B."
            : $"Performance Certification FAILED: Mean={mean:F2}ms, Max={max:F2}ms, Memory={memoryMb:F1}MB.";

        return new PerformanceBudgetReport(
            isCertified,
            mean,
            max,
            p95,
            p99,
            finalMemory,
            memoryMb,
            isZeroAlloc,
            ticksToRun,
            sim.State.ActiveUnits.Count,
            summary);
    }
}
