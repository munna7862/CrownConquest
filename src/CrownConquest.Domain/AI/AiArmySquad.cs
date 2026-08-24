using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.AI;

public enum AiSquadState
{
    Assembling,
    Attacking,
    Defending,
    Retreating,
    Patrolling
}

/// <summary>
/// Authoritative AI squad coordinating military staging, attack runs, defensive maneuvers, and tactical retreats.
/// </summary>
public sealed class AiArmySquad
{
    private readonly List<EntityId> _memberIds = new(64);
    private readonly List<UnitEntity> _cachedAliveUnits = new(64);

    public FactionId FactionId { get; }
    public AiSquadState State { get; private set; } = AiSquadState.Assembling;
    public Vector2D RallyPoint { get; set; }
    public Vector2D TargetPosition { get; set; }
    public EntityId? TargetEntityId { get; set; }
    public int AttackThreshold { get; set; } = 6;

    public IReadOnlyList<EntityId> MemberIds => _memberIds;

    public AiArmySquad(FactionId factionId, Vector2D initialRallyPoint)
    {
        FactionId = factionId;
        RallyPoint = initialRallyPoint;
        TargetPosition = initialRallyPoint;
    }

    public void AddMember(EntityId unitId)
    {
        if (!_memberIds.Contains(unitId))
        {
            _memberIds.Add(unitId);
        }
    }

    public void RemoveMember(EntityId unitId)
    {
        _memberIds.Remove(unitId);
    }

    public void SetState(AiSquadState newState)
    {
        State = newState;
    }

    public IReadOnlyList<UnitEntity> GetAliveUnits(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _cachedAliveUnits.Clear();

        for (int i = _memberIds.Count - 1; i >= 0; i--)
        {
            var id = _memberIds[i];
            if (state.TryGetUnit(id, out var unit) && unit != null && unit.IsAlive)
            {
                _cachedAliveUnits.Add(unit);
            }
            else
            {
                _memberIds.RemoveAt(i);
            }
        }

        return _cachedAliveUnits;
    }

    public float CalculateTotalHealthPercent(SimulationState state)
    {
        var alive = GetAliveUnits(state);
        if (alive.Count == 0) return 0f;

        float current = 0f;
        float max = 0f;
        for (int i = 0; i < alive.Count; i++)
        {
            current += alive[i].CurrentHealth;
            max += alive[i].MaxHealth;
        }

        return max > 0f ? current / max : 0f;
    }
}
