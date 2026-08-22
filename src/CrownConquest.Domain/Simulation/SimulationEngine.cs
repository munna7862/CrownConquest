using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Logging;

namespace CrownConquest.Domain.Simulation;

/// <summary>
/// Authoritative deterministic simulation engine for Crown & Conquest.
/// Fully decoupled from presentation/rendering.
/// </summary>
public sealed class SimulationEngine
{
    private readonly SimulationConfig _config;
    private readonly SimulationRandom _random;
    private readonly CommandQueue _commandQueue;
    private readonly DomainEventBus _eventBus;
    private readonly SimulationState _state;
    private readonly BattlefieldBounds _bounds;
    private readonly SpatialGrid _spatialGrid;
    private readonly List<EntityId> _queryBuffer = new(64);

    public ulong CurrentTick => _state.CurrentTick;
    public SimulationConfig Config => _config;
    public SimulationRandom Random => _random;
    public CommandQueue CommandQueue => _commandQueue;
    public DomainEventBus EventBus => _eventBus;
    public SimulationState State => _state;
    public BattlefieldBounds Bounds => _bounds;
    public SpatialGrid SpatialGrid => _spatialGrid;

    public SimulationEngine(
        SimulationConfig? config = null,
        DomainEventBus? eventBus = null,
        BattlefieldBounds? bounds = null)
    {
        _config = config ?? SimulationConfig.Default;
        _random = new SimulationRandom(_config.InitialRandomSeed);
        _commandQueue = new CommandQueue();
        _eventBus = eventBus ?? new DomainEventBus();
        _state = new SimulationState();
        _bounds = bounds ?? BattlefieldBounds.Default;
        _spatialGrid = new SpatialGrid(cellSize: 8.0f);
    }

    /// <summary>
    /// Executes a single deterministic simulation tick.
    /// </summary>
    public void Tick()
    {
        _state.CurrentTick++;
        ulong tick = _state.CurrentTick;

        // 1. Process staged commands deterministically
        ProcessCommands(tick);

        // 2. Auto-acquire targets for idle units in aggro range
        UpdateTargetAcquisition();

        // 3. Update unit movements and navigation with boundary clamping
        UpdateMovements(tick);

        // 4. Update combat engagements & cooldowns
        UpdateCombat(tick);

        // 5. Cleanup deceased entities at tick boundary
        CleanupDeadUnits();
    }

    /// <summary>
    /// Advances simulation by a specific number of fixed ticks.
    /// </summary>
    public void SimulateTicks(int tickCount)
    {
        for (int i = 0; i < tickCount; i++)
        {
            Tick();
        }
    }

    private void ProcessCommands(ulong tick)
    {
        var commands = _commandQueue.FlushForTick();
        for (int i = 0; i < commands.Length; i++)
        {
            ExecuteCommand(commands[i], tick);
        }
    }

    private void ExecuteCommand(ICommand command, ulong tick)
    {
        switch (command)
        {
            case SpawnUnitCommand spawn:
            {
                var unitId = _state.GenerateEntityId();
                var clampedPos = _bounds.Clamp(spawn.Position);
                var unit = new UnitEntity(
                    unitId,
                    spawn.FactionId,
                    spawn.UnitType,
                    clampedPos,
                    spawn.MaxHealth,
                    spawn.AttackDamage,
                    spawn.AttackRange,
                    spawn.MovementSpeed,
                    spawn.AttackCooldownTicks,
                    spawn.KillXpValue,
                    baseArmor: spawn.Armor,
                    attackType: spawn.AttackType,
                    aggroRange: spawn.AggroRange,
                    healthPerLevelBonus: spawn.HealthPerLevelBonus,
                    damagePerLevelBonus: spawn.DamagePerLevelBonus,
                    xpThresholds: spawn.XpThresholds);

                _state.AddUnit(unit);
                _spatialGrid.Insert(unit.Id, unit.Position);
                _eventBus.Publish(new UnitSpawnedEvent(tick, unitId, spawn.FactionId, spawn.UnitType, unit.Position));
                SimLogger.LogDebug("Simulation", $"Spawned {spawn.UnitType} {unitId} at {unit.Position}");
                break;
            }

            case MoveCommand move:
            {
                var dest = _bounds.Clamp(move.Destination);
                for (int i = 0; i < move.UnitIds.Length; i++)
                {
                    if (_state.TryGetUnit(move.UnitIds[i], out var unit) && unit != null && unit.FactionId == move.FactionId)
                    {
                        unit.Move(dest);
                    }
                }
                break;
            }

            case FormationMoveCommand formMove:
            {
                var destCentroid = _bounds.Clamp(formMove.DestinationCentroid);
                var slots = FormationCalculator.CalculateGridFormation(destCentroid, formMove.UnitIds.Length, formMove.Spacing);

                for (int i = 0; i < formMove.UnitIds.Length; i++)
                {
                    if (_state.TryGetUnit(formMove.UnitIds[i], out var unit) && unit != null && unit.FactionId == formMove.FactionId)
                    {
                        var clampedSlot = _bounds.Clamp(slots[i]);
                        unit.Move(clampedSlot);
                    }
                }
                break;
            }

            case AttackCommand attack:
            {
                for (int i = 0; i < attack.UnitIds.Length; i++)
                {
                    if (_state.TryGetUnit(attack.UnitIds[i], out var unit) && unit != null && unit.FactionId == attack.FactionId)
                    {
                        unit.Attack(attack.TargetEntityId);
                    }
                }
                break;
            }

            case StopCommand stop:
            {
                for (int i = 0; i < stop.UnitIds.Length; i++)
                {
                    if (_state.TryGetUnit(stop.UnitIds[i], out var unit) && unit != null && unit.FactionId == stop.FactionId)
                    {
                        unit.Stop();
                    }
                }
                break;
            }

            case SelectUnitsCommand select:
            {
                _eventBus.Publish(new UnitsSelectedEvent(tick, select.FactionId, select.UnitIds));
                break;
            }
        }
    }

    private void UpdateTargetAcquisition()
    {
        var units = _state.ActiveUnits;
        int count = units.Count;

        for (int i = 0; i < count; i++)
        {
            var unit = units[i];
            if (!unit.IsAlive || unit.State != UnitState.Idle) continue;

            // Search for nearest enemy within AggroRange
            _spatialGrid.QueryRadius(unit.Position, unit.AggroRange, id => _state.TryGetUnit(id, out var u) ? u?.Position : null, _queryBuffer);

            UnitEntity? nearestEnemy = null;
            float nearestDistSq = float.MaxValue;

            for (int q = 0; q < _queryBuffer.Count; q++)
            {
                if (_state.TryGetUnit(_queryBuffer[q], out var candidate) && candidate != null && candidate.IsAlive)
                {
                    if (candidate.FactionId != unit.FactionId)
                    {
                        float distSq = unit.Position.DistanceSquaredTo(candidate.Position);
                        if (distSq < nearestDistSq)
                        {
                            nearestDistSq = distSq;
                            nearestEnemy = candidate;
                        }
                    }
                }
            }

            if (nearestEnemy != null)
            {
                unit.Attack(nearestEnemy.Id);
            }
        }
    }

    private void UpdateMovements(ulong tick)
    {
        float dt = _config.DeltaTime;
        var units = _state.ActiveUnits;
        int count = units.Count;

        for (int i = 0; i < count; i++)
        {
            var unit = units[i];
            if (!unit.IsAlive) continue;

            if (unit.State == UnitState.Moving && unit.MoveTarget.HasValue)
            {
                var prevPos = unit.Position;
                float maxDistance = unit.MovementSpeed * dt;
                var target = _bounds.Clamp(unit.MoveTarget.Value);
                var nextPos = unit.Position.MoveTowards(target, maxDistance);
                nextPos = _bounds.Clamp(nextPos);

                unit.Position = nextPos;
                _spatialGrid.UpdatePosition(unit.Id, prevPos, unit.Position);

                if (unit.Position.DistanceSquaredTo(target) < 1e-4f)
                {
                    unit.Position = target;
                    unit.MoveTarget = null;
                    unit.State = UnitState.Idle;
                }

                _eventBus.Publish(new UnitMovedEvent(tick, unit.Id, prevPos, unit.Position));
            }
        }
    }

    private void UpdateCombat(ulong tick)
    {
        float dt = _config.DeltaTime;
        var units = _state.ActiveUnits;
        int count = units.Count;

        for (int i = 0; i < count; i++)
        {
            var unit = units[i];
            if (!unit.IsAlive) continue;

            unit.DecrementCooldown();

            if (unit.State == UnitState.Attacking && unit.AttackTargetId.IsValid)
            {
                if (!_state.TryGetUnit(unit.AttackTargetId, out var target) || target == null || !target.IsAlive)
                {
                    unit.AttackTargetId = EntityId.None;
                    unit.State = UnitState.Idle;
                    continue;
                }

                float distance = unit.Position.DistanceTo(target.Position);

                // Move in range if too far
                if (!CombatFormulas.IsInRange(unit.Position, target.Position, unit.AttackRange))
                {
                    float maxDistance = unit.MovementSpeed * dt;
                    var prevPos = unit.Position;
                    var nextPos = unit.Position.MoveTowards(target.Position, maxDistance);
                    nextPos = _bounds.Clamp(nextPos);

                    unit.Position = nextPos;
                    _spatialGrid.UpdatePosition(unit.Id, prevPos, unit.Position);
                    _eventBus.Publish(new UnitMovedEvent(tick, unit.Id, prevPos, unit.Position));
                }
                else if (unit.CooldownRemaining <= 0)
                {
                    // Attack target
                    unit.ResetCooldown();
                    target.TakeDamage(unit.AttackDamage, unit.Id, unit.FactionId, tick, _eventBus, out bool killed);

                    if (killed)
                    {
                        // Invariant: killer must be alive, and killer must not be friendly fire
                        if (unit.IsAlive && unit.FactionId != target.FactionId)
                        {
                            unit.Veterancy.RecordKill();
                            int oldLevel = unit.Veterancy.Level;
                            unit.Veterancy.AwardXp(
                                target.KillXpValue,
                                tick,
                                _eventBus,
                                out bool leveledUp,
                                out bool rankChanged);

                            if (leveledUp)
                            {
                                int levelsGained = unit.Veterancy.Level - oldLevel;
                                float healthBonus = levelsGained * unit.HealthPerLevelBonus;
                                unit.ApplyLevelUpBonus(healthBonus);
                            }

                            SimLogger.LogInfo("Combat", $"Unit {unit.Id} killed {target.Id}. Awarded {target.KillXpValue} XP. Level={unit.Veterancy.Level} ({unit.Veterancy.Rank.GetDisplayName()})");
                        }

                        unit.AttackTargetId = EntityId.None;
                        unit.State = UnitState.Idle;
                    }
                }
            }
        }
    }

    private void CleanupDeadUnits()
    {
        var units = _state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (!unit.IsAlive)
            {
                _spatialGrid.Remove(unit.Id);

                // Clear any other unit's target reference to this dead unit
                for (int j = 0; j < units.Count; j++)
                {
                    if (units[j].AttackTargetId == unit.Id)
                    {
                        units[j].AttackTargetId = EntityId.None;
                        if (units[j].State == UnitState.Attacking)
                        {
                            units[j].State = UnitState.Idle;
                        }
                    }
                }
            }
        }

        _state.RemoveDeadUnits();
    }
}
