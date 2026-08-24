using System;
using System.Collections.Generic;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.AI;

/// <summary>
/// Time-slicing scheduler that staggers autonomous AI decision cycles across simulation ticks
/// to prevent CPU spikes when multiple AI factions or large unit counts are simulated simultaneously.
/// </summary>
public sealed class AiUpdateScheduler
{
    private readonly List<AiFactionController> _controllers = new(8);
    private readonly Dictionary<FactionId, int> _factionOffsets = new(8);

    public const int PerceptionIntervalTicks = 5;
    public const int TacticsIntervalTicks = 5;
    public const int EconomyIntervalTicks = 10;
    public const int ProductionIntervalTicks = 10;

    public IReadOnlyList<AiFactionController> Controllers => _controllers;
    public int ScheduledFactionCount => _controllers.Count;

    public void Register(AiFactionController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (_controllers.Contains(controller)) return;

        // Auto-assign staggered offset based on registered count
        int offset = (_controllers.Count * 2) % EconomyIntervalTicks;
        _controllers.Add(controller);
        _factionOffsets[controller.FactionId] = offset;
    }

    public void RegisterWithExplicitOffset(AiFactionController controller, int offset)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (!_controllers.Contains(controller))
        {
            _controllers.Add(controller);
        }
        _factionOffsets[controller.FactionId] = Math.Abs(offset) % EconomyIntervalTicks;
    }

    public void Unregister(FactionId factionId)
    {
        for (int i = _controllers.Count - 1; i >= 0; i--)
        {
            if (_controllers[i].FactionId == factionId)
            {
                _controllers.RemoveAt(i);
            }
        }
        _factionOffsets.Remove(factionId);
    }

    public void Clear()
    {
        _controllers.Clear();
        _factionOffsets.Clear();
    }

    public int GetOffset(FactionId factionId)
    {
        return _factionOffsets.TryGetValue(factionId, out int offset) ? offset : 0;
    }

    public bool ShouldRunPerception(FactionId factionId, ulong tick)
    {
        int offset = GetOffset(factionId);
        return (tick + (ulong)offset) % PerceptionIntervalTicks == 0;
    }

    public bool ShouldRunTactics(FactionId factionId, ulong tick)
    {
        int offset = GetOffset(factionId);
        return (tick + (ulong)offset) % TacticsIntervalTicks == 0;
    }

    public bool ShouldRunEconomy(FactionId factionId, ulong tick)
    {
        int offset = GetOffset(factionId);
        return (tick + (ulong)offset) % EconomyIntervalTicks == 0;
    }

    public bool ShouldRunProduction(FactionId factionId, ulong tick)
    {
        int offset = GetOffset(factionId);
        // Half-phase offset from economy to split work across frames
        return (tick + (ulong)offset + (ulong)(EconomyIntervalTicks / 2)) % ProductionIntervalTicks == 0;
    }

    /// <summary>
    /// Executes staggered AI updates for all registered faction controllers.
    /// </summary>
    public void UpdateAll(SimulationState state, CommandQueue commandQueue, ulong currentTick)
    {
        for (int i = 0; i < _controllers.Count; i++)
        {
            var controller = _controllers[i];
            if (!controller.IsActive) continue;

            int offset = GetOffset(controller.FactionId);
            controller.UpdateScheduled(state, commandQueue, currentTick, offset);
        }
    }
}
