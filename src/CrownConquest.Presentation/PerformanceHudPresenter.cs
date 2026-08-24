using System;
using System.Globalization;
using CrownConquest.Domain.Profiling;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Formatted UI view model for RTS in-game performance HUD overlay.
/// </summary>
public readonly record struct PerformanceHudViewModel(
    ulong CurrentTick,
    double EstimatedFps,
    double LastTickMs,
    double AverageTickMs,
    double PeakTickMs,
    int ActiveUnitCount,
    int ActiveBuildingCount,
    int SpatialQueriesCount,
    double MemoryAllocatedMb,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    string SubsystemBreakdownSummary,
    bool IsWithinFrameBudget);

/// <summary>
/// Presentation layer presenter for real-time simulation performance telemetry,
/// frame budget tracking, memory profiling, and bottleneck diagnosis.
/// </summary>
public sealed class PerformanceHudPresenter
{
    public const double TargetTickBudgetMs = 16.67; // 60 FPS frame budget

    public PerformanceHudViewModel GetViewModel(SimulationEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        var metrics = engine.Profiler.GetSnapshot(
            engine.CurrentTick,
            engine.State.ActiveUnits.Count,
            engine.State.ActiveBuildings.Count);

        double lastMs = metrics.LastTickDurationMs;
        double avgMs = metrics.AverageTickDurationMs;
        double peakMs = metrics.PeakTickDurationMs;

        double fps = lastMs > 0.001 ? Math.Min(60.0, 1000.0 / lastMs) : 60.0;
        double memMb = (double)metrics.AllocatedMemoryBytes / (1024.0 * 1024.0);
        bool withinBudget = avgMs <= TargetTickBudgetMs;

        string summary = string.Format(
            CultureInfo.InvariantCulture,
            "AI: {0:F2}ms | Combat: {1:F2}ms | Move: {2:F2}ms | Target: {3:F2}ms | Cmds: {4:F2}ms",
            metrics.AiPhaseMs,
            metrics.CombatPhaseMs,
            metrics.MovementPhaseMs,
            metrics.TargetAcquisitionPhaseMs,
            metrics.CommandsPhaseMs);

        return new PerformanceHudViewModel(
            CurrentTick: engine.CurrentTick,
            EstimatedFps: fps,
            LastTickMs: lastMs,
            AverageTickMs: avgMs,
            PeakTickMs: peakMs,
            ActiveUnitCount: metrics.ActiveUnitCount,
            ActiveBuildingCount: metrics.ActiveBuildingCount,
            SpatialQueriesCount: metrics.SpatialQueriesPerTick,
            MemoryAllocatedMb: memMb,
            Gen0Collections: metrics.Gen0Collections,
            Gen1Collections: metrics.Gen1Collections,
            Gen2Collections: metrics.Gen2Collections,
            SubsystemBreakdownSummary: summary,
            IsWithinFrameBudget: withinBudget);
    }
}
