using System;
using System.Collections.Generic;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Application;

/// <summary>
/// Manages player unit selection state, point clicks, and marquee drag-box selection queries.
/// </summary>
public sealed class SelectionManager
{
    private readonly GameCoordinator _coordinator;
    private readonly FactionId _localPlayerFaction;
    private readonly List<EntityId> _selectedUnitIds = new(64);
    private readonly List<EntityId> _queryBuffer = new(64);

    public IReadOnlyList<EntityId> SelectedUnitIds => _selectedUnitIds;
    public FactionId LocalPlayerFaction => _localPlayerFaction;
    public EntityId? SelectedBuildingId { get; private set; }
    public BuildingEntity? SelectedBuilding => SelectedBuildingId.HasValue && _coordinator.Simulation.State.TryGetBuilding(SelectedBuildingId.Value, out var b) ? b : null;

    public SelectionManager(GameCoordinator coordinator, FactionId localPlayerFaction)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _localPlayerFaction = localPlayerFaction;

        // Listen for unit deaths to remove dead units from selection
        _coordinator.EventBus.Subscribe<UnitKilledEvent>(OnUnitKilled);
    }

    /// <summary>
    /// Selects a single unit or building near the given world coordinate.
    /// </summary>
    public bool SelectPoint(Vector2D worldPos, float clickRadius = 1.0f)
    {
        _selectedUnitIds.Clear();
        _queryBuffer.Clear();

        var sim = _coordinator.Simulation;
        sim.SpatialGrid.QueryRadius(
            worldPos,
            clickRadius,
            id => sim.State.TryGetUnit(id, out var u) ? u?.Position : null,
            _queryBuffer);

        UnitEntity? bestCandidate = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < _queryBuffer.Count; i++)
        {
            if (sim.State.TryGetUnit(_queryBuffer[i], out var unit) && unit != null && unit.IsAlive)
            {
                if (unit.FactionId == _localPlayerFaction)
                {
                    float distSq = unit.Position.DistanceSquaredTo(worldPos);
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestCandidate = unit;
                    }
                }
            }
        }

        if (bestCandidate != null)
        {
            SelectedBuildingId = null;
            _selectedUnitIds.Add(bestCandidate.Id);
            _coordinator.DispatchCommand(new SelectUnitsCommand(
                _localPlayerFaction,
                _coordinator.CurrentTick,
                [bestCandidate.Id],
                ClearPrevious: true));
            return true;
        }

        // Check if a friendly building was clicked
        var buildings = sim.State.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (b.FactionId == _localPlayerFaction && b.IsAlive)
            {
                var expandedBox = Rect2D.FromCenterAndExtents(b.Position, (b.GridSize.X * 0.5f) + 0.5f, (b.GridSize.Y * 0.5f) + 0.5f);
                if (expandedBox.Contains(worldPos) || b.Position.DistanceSquaredTo(worldPos) <= ((b.GridSize.X * 0.5f) * (b.GridSize.X * 0.5f)))
                {
                    SelectedBuildingId = b.Id;
                    _coordinator.DispatchCommand(new SelectUnitsCommand(
                        _localPlayerFaction,
                        _coordinator.CurrentTick,
                        Array.Empty<EntityId>(),
                        ClearPrevious: true));
                    return true;
                }
            }
        }

        SelectedBuildingId = null;
        _coordinator.DispatchCommand(new SelectUnitsCommand(
            _localPlayerFaction,
            _coordinator.CurrentTick,
            Array.Empty<EntityId>(),
            ClearPrevious: true));
        return false;
    }

    public void SelectBuilding(EntityId buildingId)
    {
        _selectedUnitIds.Clear();
        var sim = _coordinator.Simulation;
        if (sim.State.TryGetBuilding(buildingId, out var b) && b != null && b.FactionId == _localPlayerFaction && b.IsAlive)
        {
            SelectedBuildingId = buildingId;
        }
        else
        {
            SelectedBuildingId = null;
        }

        _coordinator.DispatchCommand(new SelectUnitsCommand(
            _localPlayerFaction,
            _coordinator.CurrentTick,
            Array.Empty<EntityId>(),
            ClearPrevious: true));
    }

    public void SetBuildingRallyPoint(Vector2D rallyPos)
    {
        if (SelectedBuildingId.HasValue)
        {
            _coordinator.DispatchCommand(new SetRallyPointCommand(
                _coordinator.CurrentTick,
                _localPlayerFaction,
                SelectedBuildingId.Value,
                rallyPos));
        }
    }

    /// <summary>
    /// Selects all friendly units intersecting the 2D bounding marquee box.
    /// </summary>
    public int SelectBox(Rect2D box)
    {
        SelectedBuildingId = null;
        _selectedUnitIds.Clear();
        _queryBuffer.Clear();

        var sim = _coordinator.Simulation;
        sim.SpatialGrid.QueryBox(
            box,
            id => sim.State.TryGetUnit(id, out var u) ? u?.Position : null,
            _queryBuffer);

        for (int i = 0; i < _queryBuffer.Count; i++)
        {
            if (sim.State.TryGetUnit(_queryBuffer[i], out var unit) && unit != null && unit.IsAlive)
            {
                if (unit.FactionId == _localPlayerFaction)
                {
                    _selectedUnitIds.Add(unit.Id);
                }
            }
        }

        _coordinator.DispatchCommand(new SelectUnitsCommand(
            _localPlayerFaction,
            _coordinator.CurrentTick,
            _selectedUnitIds.ToArray(),
            ClearPrevious: true));

        return _selectedUnitIds.Count;
    }

    /// <summary>
    /// Issues a formation move order to all currently selected units.
    /// </summary>
    public void IssueMoveOrder(Vector2D destination)
    {
        if (_selectedUnitIds.Count == 0) return;

        if (_selectedUnitIds.Count == 1)
        {
            _coordinator.DispatchCommand(new MoveCommand(
                _localPlayerFaction,
                _coordinator.CurrentTick,
                _selectedUnitIds.ToArray(),
                destination));
        }
        else
        {
            _coordinator.DispatchCommand(new FormationMoveCommand(
                _localPlayerFaction,
                _coordinator.CurrentTick,
                _selectedUnitIds.ToArray(),
                destination,
                Spacing: 2.0f));
        }
    }

    /// <summary>
    /// Issues an attack order to all currently selected units.
    /// </summary>
    public void IssueAttackOrder(EntityId targetId)
    {
        if (_selectedUnitIds.Count == 0 || !targetId.IsValid) return;

        _coordinator.DispatchCommand(new AttackCommand(
            _localPlayerFaction,
            _coordinator.CurrentTick,
            _selectedUnitIds.ToArray(),
            targetId));
    }

    /// <summary>
    /// Contextual right click order: if clicked on an enemy, attack; otherwise move in formation.
    /// </summary>
    public void IssueContextualOrder(Vector2D targetWorldPos, float clickRadius = 1.0f)
    {
        if (_selectedUnitIds.Count == 0) return;

        var sim = _coordinator.Simulation;
        sim.SpatialGrid.QueryRadius(
            targetWorldPos,
            clickRadius,
            id => sim.State.TryGetUnit(id, out var u) ? u?.Position : null,
            _queryBuffer);

        UnitEntity? targetEnemy = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < _queryBuffer.Count; i++)
        {
            if (sim.State.TryGetUnit(_queryBuffer[i], out var unit) && unit != null && unit.IsAlive)
            {
                if (unit.FactionId != _localPlayerFaction)
                {
                    float distSq = unit.Position.DistanceSquaredTo(targetWorldPos);
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        targetEnemy = unit;
                    }
                }
            }
        }

        if (targetEnemy != null)
        {
            IssueAttackOrder(targetEnemy.Id);
        }
        else
        {
            IssueMoveOrder(targetWorldPos);
        }
    }

    public void ClearSelection()
    {
        SelectedBuildingId = null;
        _selectedUnitIds.Clear();
        _coordinator.DispatchCommand(new SelectUnitsCommand(
            _localPlayerFaction,
            _coordinator.CurrentTick,
            Array.Empty<EntityId>(),
            ClearPrevious: true));
    }

    private void OnUnitKilled(in UnitKilledEvent e)
    {
        _selectedUnitIds.Remove(e.CasualtyId);
    }
}
