using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Simulation;

/// <summary>
/// Authoritative simulation state snapshot containing all entities and global state.
/// </summary>
public sealed class SimulationState
{
    private int _nextEntityId = 1;
    private readonly Dictionary<EntityId, UnitEntity> _units = new(256);
    private readonly List<UnitEntity> _activeUnitList = new(256);

    public ulong CurrentTick { get; internal set; }
    public IReadOnlyDictionary<EntityId, UnitEntity> Units => _units;
    public IReadOnlyList<UnitEntity> ActiveUnits => _activeUnitList;

    public EntityId GenerateEntityId() => new(_nextEntityId++);

    public void AddUnit(UnitEntity unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        _units[unit.Id] = unit;
        _activeUnitList.Add(unit);
    }

    public bool TryGetUnit(EntityId id, out UnitEntity? unit)
    {
        return _units.TryGetValue(id, out unit);
    }

    public void RemoveDeadUnits()
    {
        for (int i = _activeUnitList.Count - 1; i >= 0; i--)
        {
            if (!_activeUnitList[i].IsAlive)
            {
                _units.Remove(_activeUnitList[i].Id);
                _activeUnitList.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Computes a deterministic checksum hash of the entire simulation state.
    /// Useful for detecting multiplayer/replay desynchronization.
    /// </summary>
    public ulong ComputeStateChecksum()
    {
        ulong hash = 14695981039346656037UL; // FNV offset basis
        hash = (hash ^ CurrentTick) * 1099511628211UL;

        for (int i = 0; i < _activeUnitList.Count; i++)
        {
            var unit = _activeUnitList[i];
            hash = (hash ^ (ulong)unit.Id.Value) * 1099511628211UL;
            hash = (hash ^ (ulong)unit.FactionId.Value) * 1099511628211UL;
            hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(unit.Position.X)) * 1099511628211UL;
            hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(unit.Position.Y)) * 1099511628211UL;
            hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(unit.CurrentHealth)) * 1099511628211UL;
            hash = (hash ^ (ulong)unit.Veterancy.Level) * 1099511628211UL;
            hash = (hash ^ (ulong)unit.Veterancy.CurrentXp) * 1099511628211UL;
        }

        return hash;
    }
}
