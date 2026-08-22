using System;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Application;

/// <summary>
/// Top-level coordinator that manages simulation lifecycle, fixed-timestep accumulation,
/// and command dispatching.
/// </summary>
public sealed class GameCoordinator : ICommandDispatcher
{
    private readonly SimulationEngine _simulation;
    private readonly DomainEventBus _eventBus;
    private float _timeAccumulator;

    public SimulationEngine Simulation => _simulation;
    public DomainEventBus EventBus => _eventBus;
    public ulong CurrentTick => _simulation.CurrentTick;
    public bool IsPaused { get; set; }

    public GameCoordinator(SimulationConfig? config = null, DomainEventBus? eventBus = null)
    {
        _eventBus = eventBus ?? new DomainEventBus();
        _simulation = new SimulationEngine(config, _eventBus);
        _timeAccumulator = 0f;
        IsPaused = false;
    }

    /// <summary>
    /// Updates the game coordinator with presentation frame delta time.
    /// Accumulates elapsed time and executes fixed simulation ticks accordingly.
    /// </summary>
    public int Update(float frameDeltaTime)
    {
        if (IsPaused || frameDeltaTime <= 0f)
        {
            return 0;
        }

        // Clamp maximum frame delta to prevent spiral of death
        float clampedDelta = MathF.Min(frameDeltaTime, 0.25f);
        _timeAccumulator += clampedDelta;

        float fixedDelta = _simulation.Config.DeltaTime;
        int ticksExecuted = 0;

        while (_timeAccumulator >= fixedDelta)
        {
            _simulation.Tick();
            _timeAccumulator -= fixedDelta;
            ticksExecuted++;
        }

        return ticksExecuted;
    }

    public Result DispatchCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.FactionId.IsValid)
        {
            return Result.Failure(new GameError("INVALID_FACTION", "Cannot dispatch command for invalid faction."));
        }

        _simulation.CommandQueue.Enqueue(command);
        return Result.Success();
    }

    public Result IssueGatherOrder(FactionId factionId, EntityId[] workerIds, EntityId targetNodeId)
    {
        return DispatchCommand(new GatherCommand(CurrentTick, factionId, workerIds, targetNodeId));
    }

    public Result IssuePlaceBuildingOrder(FactionId factionId, string buildingType, Vector2D position)
    {
        return DispatchCommand(new PlaceBuildingCommand(CurrentTick, factionId, buildingType, position));
    }

    public Result IssueConstructOrder(FactionId factionId, EntityId[] workerIds, EntityId buildingId)
    {
        return DispatchCommand(new ConstructBuildingCommand(CurrentTick, factionId, workerIds, buildingId));
    }

    public Result IssueQueueProductionOrder(FactionId factionId, EntityId buildingId, string unitType)
    {
        return DispatchCommand(new QueueProductionCommand(CurrentTick, factionId, buildingId, unitType));
    }

    public Result IssueCancelProductionOrder(FactionId factionId, EntityId buildingId, int queueIndex)
    {
        return DispatchCommand(new CancelProductionCommand(CurrentTick, factionId, buildingId, queueIndex));
    }

    public ResourceBank GetResourceBank(FactionId factionId)
    {
        return _simulation.State.GetOrCreateResourceBank(factionId);
    }

    public PopulationManager GetPopulationManager(FactionId factionId)
    {
        return _simulation.State.GetOrCreatePopulationManager(factionId);
    }
}
