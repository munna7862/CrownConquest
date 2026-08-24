using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

/// <summary>
/// Presentation layer presenter observing siege warfare simulation events (wall breaches, gate states, tower attacks, AoE impacts).
/// Decoupled from Godot nodes; usable in headless and UI contexts.
/// </summary>
public sealed class SiegeWarfarePresenter
{
    private readonly List<WallBreachedEvent> _breachHistory = new(32);
    private readonly List<GateStateChangedEvent> _gateStateHistory = new(32);
    private readonly List<TowerAttackEvent> _towerAttackHistory = new(64);
    private readonly List<SiegeAreaOfEffectImpactEvent> _aoeImpactHistory = new(64);
    private readonly List<BuildingAttackedEvent> _buildingAttackedHistory = new(64);
    private readonly Dictionary<EntityId, GateState> _gateStates = new(16);
    private readonly Dictionary<EntityId, int> _towerGarrisonCounts = new(16);

    public IReadOnlyList<WallBreachedEvent> BreachHistory => _breachHistory;
    public IReadOnlyList<GateStateChangedEvent> GateStateHistory => _gateStateHistory;
    public IReadOnlyList<TowerAttackEvent> TowerAttackHistory => _towerAttackHistory;
    public IReadOnlyList<SiegeAreaOfEffectImpactEvent> AoeImpactHistory => _aoeImpactHistory;
    public IReadOnlyList<BuildingAttackedEvent> BuildingAttackedHistory => _buildingAttackedHistory;
    public IReadOnlyDictionary<EntityId, GateState> GateStates => _gateStates;
    public IReadOnlyDictionary<EntityId, int> TowerGarrisonCounts => _towerGarrisonCounts;

    public void Bind(DomainEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);

        eventBus.Subscribe<WallBreachedEvent>(OnWallBreached);
        eventBus.Subscribe<GateStateChangedEvent>(OnGateStateChanged);
        eventBus.Subscribe<TowerAttackEvent>(OnTowerAttack);
        eventBus.Subscribe<UnitGarrisonedEvent>(OnUnitGarrisoned);
        eventBus.Subscribe<UnitUngarrisonedEvent>(OnUnitUngarrisoned);
        eventBus.Subscribe<SiegeAreaOfEffectImpactEvent>(OnAoeImpact);
        eventBus.Subscribe<BuildingAttackedEvent>(OnBuildingAttacked);
    }

    private void OnWallBreached(in WallBreachedEvent evt)
    {
        _breachHistory.Add(evt);
    }

    private void OnGateStateChanged(in GateStateChangedEvent evt)
    {
        _gateStateHistory.Add(evt);
        _gateStates[evt.GateId] = evt.NewState;
    }

    private void OnTowerAttack(in TowerAttackEvent evt)
    {
        _towerAttackHistory.Add(evt);
    }

    private void OnUnitGarrisoned(in UnitGarrisonedEvent evt)
    {
        _towerGarrisonCounts[evt.TowerId] = evt.GarrisonCount;
    }

    private void OnUnitUngarrisoned(in UnitUngarrisonedEvent evt)
    {
        if (_towerGarrisonCounts.TryGetValue(evt.TowerId, out int count))
        {
            _towerGarrisonCounts[evt.TowerId] = Math.Max(0, count - 1);
        }
    }

    private void OnAoeImpact(in SiegeAreaOfEffectImpactEvent evt)
    {
        _aoeImpactHistory.Add(evt);
    }

    private void OnBuildingAttacked(in BuildingAttackedEvent evt)
    {
        _buildingAttackedHistory.Add(evt);
    }

    public GateState GetGateState(EntityId gateId)
    {
        return _gateStates.TryGetValue(gateId, out var state) ? state : GateState.Closed;
    }

    public int GetTowerGarrisonCount(EntityId towerId)
    {
        return _towerGarrisonCounts.TryGetValue(towerId, out int count) ? count : 0;
    }

    public void Reset()
    {
        _breachHistory.Clear();
        _gateStateHistory.Clear();
        _towerAttackHistory.Clear();
        _aoeImpactHistory.Clear();
        _buildingAttackedHistory.Clear();
        _gateStates.Clear();
        _towerGarrisonCounts.Clear();
    }
}
