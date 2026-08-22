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

    public ulong CurrentTick => _state.CurrentTick;
    public SimulationConfig Config => _config;
    public SimulationRandom Random => _random;
    public CommandQueue CommandQueue => _commandQueue;
    public DomainEventBus EventBus => _eventBus;
    public SimulationState State => _state;

    public SimulationEngine(SimulationConfig? config = null, DomainEventBus? eventBus = null)
    {
        _config = config ?? SimulationConfig.Default;
        _random = new SimulationRandom(_config.InitialRandomSeed);
        _commandQueue = new CommandQueue();
        _eventBus = eventBus ?? new DomainEventBus();
        _state = new SimulationState();
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

        // 2. Update unit movements and navigation
        UpdateMovements();

        // 3. Update combat engagements & cooldowns
        UpdateCombat(tick);

        // 4. Cleanup deceased entities at tick boundary
        _state.RemoveDeadUnits();
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
                var unit = new UnitEntity(
                    unitId,
                    spawn.FactionId,
                    spawn.UnitType,
                    spawn.Position,
                    spawn.MaxHealth,
                    spawn.AttackDamage,
                    spawn.AttackRange,
                    spawn.MovementSpeed,
                    spawn.AttackCooldownTicks,
                    spawn.KillXpValue);

                _state.AddUnit(unit);
                _eventBus.Publish(new UnitSpawnedEvent(tick, unitId, spawn.FactionId, spawn.UnitType, spawn.Position));
                SimLogger.LogDebug("Simulation", $"Spawned {spawn.UnitType} {unitId} at {spawn.Position}");
                break;
            }

            case MoveCommand move:
            {
                for (int i = 0; i < move.UnitIds.Length; i++)
                {
                    if (_state.TryGetUnit(move.UnitIds[i], out var unit) && unit != null && unit.FactionId == move.FactionId)
                    {
                        unit.Move(move.Destination);
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
        }
    }

    private void UpdateMovements()
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
                unit.Position = unit.Position.MoveTowards(unit.MoveTarget.Value, maxDistance);

                if (unit.Position.DistanceSquaredTo(unit.MoveTarget.Value) < 1e-4f)
                {
                    unit.Position = unit.MoveTarget.Value;
                    unit.MoveTarget = null;
                    unit.State = UnitState.Idle;
                }

                _eventBus.Publish(new UnitMovedEvent(_state.CurrentTick, unit.Id, prevPos, unit.Position));
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
                if (distance > unit.AttackRange)
                {
                    float maxDistance = unit.MovementSpeed * dt;
                    var prevPos = unit.Position;
                    unit.Position = unit.Position.MoveTowards(target.Position, maxDistance);
                    _eventBus.Publish(new UnitMovedEvent(tick, unit.Id, prevPos, unit.Position));
                }
                else if (unit.CooldownRemaining <= 0)
                {
                    // Attack target
                    unit.ResetCooldown();
                    target.TakeDamage(unit.AttackDamage, unit.Id, unit.FactionId, tick, _eventBus, out bool killed);

                    if (killed)
                    {
                        // Signature Gameplay Mechanic: Award Kill XP to killer and evaluate immediate level-up
                        unit.Veterancy.RecordKill();
                        unit.Veterancy.AwardXp(
                            target.KillXpValue,
                            tick,
                            _eventBus,
                            out bool leveledUp,
                            out bool rankChanged);

                        SimLogger.LogInfo("Combat", $"Unit {unit.Id} killed {target.Id}. Awarded {target.KillXpValue} XP. Level={unit.Veterancy.Level} ({unit.Veterancy.Rank.GetDisplayName()})");

                        unit.AttackTargetId = EntityId.None;
                        unit.State = UnitState.Idle;
                    }
                }
            }
        }
    }
}
