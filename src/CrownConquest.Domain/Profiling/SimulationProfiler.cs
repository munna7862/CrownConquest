using System;
using System.Diagnostics;

namespace CrownConquest.Domain.Profiling;

/// <summary>
/// Subsystem phase identifier for profiling breakdown.
/// </summary>
public enum SimulationPhase : byte
{
    Commands = 0,
    Ai = 1,
    Heroes = 2,
    Workers = 3,
    TargetAcquisition = 4,
    Movement = 5,
    Combat = 6,
    Towers = 7,
    Morale = 8,
    Production = 9,
    Research = 10,
    EraAdvancement = 11,
    Population = 12,
    Cleanup = 13,
    SpatialIndexing = 14,
    TotalTick = 15
}

/// <summary>
/// Telemetry metrics snapshot for simulation performance monitoring.
/// </summary>
public readonly record struct PerformanceMetrics(
    ulong CurrentTick,
    double LastTickDurationMs,
    double AverageTickDurationMs,
    double PeakTickDurationMs,
    double MinTickDurationMs,
    int ActiveUnitCount,
    int ActiveBuildingCount,
    int SpatialQueriesPerTick,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    long AllocatedMemoryBytes,
    double CommandsPhaseMs,
    double AiPhaseMs,
    double CombatPhaseMs,
    double MovementPhaseMs,
    double TargetAcquisitionPhaseMs,
    double SpatialIndexingPhaseMs);

/// <summary>
/// Zero-allocation ref struct timer scope for recording subsystem durations.
/// </summary>
public readonly ref struct ProfileScope
{
    private readonly SimulationProfiler _profiler;
    private readonly SimulationPhase _phase;
    private readonly long _startTimestamp;

    public ProfileScope(SimulationProfiler profiler, SimulationPhase phase)
    {
        _profiler = profiler;
        _phase = phase;
        _startTimestamp = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        long elapsed = Stopwatch.GetTimestamp() - _startTimestamp;
        _profiler.RecordPhaseDuration(_phase, elapsed);
    }
}

/// <summary>
/// High-performance, zero-allocation simulation profiler.
/// Records exact high-resolution timestamps per simulation phase and maintains rolling telemetry metrics.
/// </summary>
public sealed class SimulationProfiler
{
    private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;
    private const int PhaseCount = 16;
    private const int HistoryCapacity = 120; // 4 seconds of 30Hz history

    private readonly long[] _phaseTicks = new long[PhaseCount];
    private readonly double[] _phaseDurationsMs = new double[PhaseCount];
    private readonly double[] _tickDurationHistory = new double[HistoryCapacity];
    private int _historyIndex;
    private int _historyCount;

    private double _peakTickMs;
    private double _minTickMs = double.MaxValue;
    private double _totalTickTimeAccumulator;
    private ulong _totalTicksRecorded;
    private int _spatialQueriesThisTick;

    public bool IsEnabled { get; set; } = true;
    public double PeakTickDurationMs => _peakTickMs;
    public double MinTickDurationMs => _historyCount > 0 ? _minTickMs : 0.0;
    public double LastTickDurationMs => _phaseDurationsMs[(int)SimulationPhase.TotalTick];

    public SimulationProfiler()
    {
        Reset();
    }

    public void Reset()
    {
        Array.Clear(_phaseTicks, 0, _phaseTicks.Length);
        Array.Clear(_phaseDurationsMs, 0, _phaseDurationsMs.Length);
        Array.Clear(_tickDurationHistory, 0, _tickDurationHistory.Length);
        _historyIndex = 0;
        _historyCount = 0;
        _peakTickMs = 0.0;
        _minTickMs = double.MaxValue;
        _totalTickTimeAccumulator = 0.0;
        _totalTicksRecorded = 0;
        _spatialQueriesThisTick = 0;
    }

    public void BeginTick()
    {
        if (!IsEnabled) return;
        Array.Clear(_phaseTicks, 0, _phaseTicks.Length);
        _spatialQueriesThisTick = 0;
    }

    public ProfileScope Measure(SimulationPhase phase)
    {
        if (!IsEnabled) return default;
        return new ProfileScope(this, phase);
    }

    internal void RecordPhaseDuration(SimulationPhase phase, long stopwatchTicks)
    {
        if (!IsEnabled) return;
        _phaseTicks[(int)phase] += stopwatchTicks;
    }

    public void RecordSpatialQuery()
    {
        if (!IsEnabled) return;
        _spatialQueriesThisTick++;
    }

    public void EndTick(ulong currentTick, int activeUnits, int activeBuildings)
    {
        if (!IsEnabled) return;

        for (int i = 0; i < PhaseCount; i++)
        {
            _phaseDurationsMs[i] = _phaseTicks[i] * TicksToMs;
        }

        double totalMs = _phaseDurationsMs[(int)SimulationPhase.TotalTick];
        if (totalMs <= 0.0)
        {
            // Compute sum of phases if total tick wasn't measured directly
            double sum = 0;
            for (int i = 0; i < (int)SimulationPhase.TotalTick; i++)
            {
                sum += _phaseDurationsMs[i];
            }
            totalMs = sum;
            _phaseDurationsMs[(int)SimulationPhase.TotalTick] = totalMs;
        }

        _tickDurationHistory[_historyIndex] = totalMs;
        _historyIndex = (_historyIndex + 1) % HistoryCapacity;
        if (_historyCount < HistoryCapacity) _historyCount++;

        _totalTickTimeAccumulator += totalMs;
        _totalTicksRecorded++;

        if (totalMs > _peakTickMs) _peakTickMs = totalMs;
        if (totalMs < _minTickMs) _minTickMs = totalMs;
    }

    public double GetPhaseDurationMs(SimulationPhase phase)
    {
        return _phaseDurationsMs[(int)phase];
    }

    public double GetAverageTickDurationMs()
    {
        if (_historyCount == 0) return 0.0;
        double sum = 0;
        for (int i = 0; i < _historyCount; i++)
        {
            sum += _tickDurationHistory[i];
        }
        return sum / _historyCount;
    }

    public PerformanceMetrics GetSnapshot(ulong currentTick, int activeUnits, int activeBuildings)
    {
        return new PerformanceMetrics(
            CurrentTick: currentTick,
            LastTickDurationMs: LastTickDurationMs,
            AverageTickDurationMs: GetAverageTickDurationMs(),
            PeakTickDurationMs: _peakTickMs,
            MinTickDurationMs: _historyCount > 0 ? _minTickMs : 0.0,
            ActiveUnitCount: activeUnits,
            ActiveBuildingCount: activeBuildings,
            SpatialQueriesPerTick: _spatialQueriesThisTick,
            Gen0Collections: GC.CollectionCount(0),
            Gen1Collections: GC.CollectionCount(1),
            Gen2Collections: GC.CollectionCount(2),
            AllocatedMemoryBytes: GC.GetTotalMemory(false),
            CommandsPhaseMs: _phaseDurationsMs[(int)SimulationPhase.Commands],
            AiPhaseMs: _phaseDurationsMs[(int)SimulationPhase.Ai],
            CombatPhaseMs: _phaseDurationsMs[(int)SimulationPhase.Combat],
            MovementPhaseMs: _phaseDurationsMs[(int)SimulationPhase.Movement],
            TargetAcquisitionPhaseMs: _phaseDurationsMs[(int)SimulationPhase.TargetAcquisition],
            SpatialIndexingPhaseMs: _phaseDurationsMs[(int)SimulationPhase.SpatialIndexing]);
    }
}
