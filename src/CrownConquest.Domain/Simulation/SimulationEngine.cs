using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
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

        // 2. Update worker gathering and construction state machine
        UpdateWorkerTasks(tick);

        // 3. Auto-acquire targets for idle combat units in aggro range
        UpdateTargetAcquisition();

        // 4. Update unit movements and navigation with boundary clamping
        UpdateMovements(tick);

        // 5. Update combat engagements & cooldowns
        UpdateCombat(tick);

        // 6. Update building production queues
        UpdateProduction(tick);

        // 7. Update population counts and capacities
        UpdatePopulation(tick);

        // 8. Cleanup deceased entities and depleted nodes at tick boundary
        CleanupEntities();
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

                WorkerGatherState? workerState = null;
                if (spawn.UnitType.Equals("villager", StringComparison.OrdinalIgnoreCase) ||
                    spawn.UnitType.Equals("worker", StringComparison.OrdinalIgnoreCase))
                {
                    workerState = new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 0.5f, buildPowerPerTick: 1.0f);
                }

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
                    xpThresholds: spawn.XpThresholds,
                    workerState: workerState);

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

            case GatherCommand gather:
            {
                for (int i = 0; i < gather.WorkerIds.Length; i++)
                {
                    if (_state.TryGetUnit(gather.WorkerIds[i], out var unit) && unit != null && unit.FactionId == gather.FactionId)
                    {
                        unit.AssignGather(gather.TargetNodeId);
                    }
                }
                break;
            }

            case PlaceBuildingCommand place:
            {
                ExecutePlaceBuilding(place, tick);
                break;
            }

            case ConstructBuildingCommand construct:
            {
                for (int i = 0; i < construct.WorkerIds.Length; i++)
                {
                    if (_state.TryGetUnit(construct.WorkerIds[i], out var unit) && unit != null && unit.FactionId == construct.FactionId)
                    {
                        unit.AssignConstruct(construct.BuildingId);
                    }
                }
                break;
            }

            case QueueProductionCommand queueProd:
            {
                ExecuteQueueProduction(queueProd, tick);
                break;
            }

            case CancelProductionCommand cancelProd:
            {
                ExecuteCancelProduction(cancelProd, tick);
                break;
            }

            case SetRallyPointCommand setRally:
            {
                if (_state.TryGetBuilding(setRally.BuildingId, out var building) && building != null && building.FactionId == setRally.FactionId)
                {
                    building.RallyPoint = _bounds.Clamp(setRally.RallyPoint);
                }
                break;
            }

            case RepairBuildingCommand repair:
            {
                for (int i = 0; i < repair.WorkerIds.Length; i++)
                {
                    if (_state.TryGetUnit(repair.WorkerIds[i], out var unit) && unit != null && unit.FactionId == repair.FactionId)
                    {
                        unit.AssignRepair(repair.BuildingId);
                    }
                }
                break;
            }

            case ReseedFarmCommand reseed:
            {
                if (_state.TryGetBuilding(reseed.FarmId, out var farm) && farm != null && farm.FactionId == reseed.FactionId && farm.IsFarm)
                {
                    var bank = _state.GetOrCreateResourceBank(reseed.FactionId);
                    if (bank.TryDeduct(new ResourceCost(Wood: farm.FarmReseedCost), tick, _eventBus, "Reseed Farm"))
                    {
                        farm.ReseedFarm(tick, _eventBus);
                    }
                }
                break;
            }

            case SelectIdleWorkersCommand selectIdle:
            {
                var idleIds = GetIdleWorkers(selectIdle.FactionId);
                _eventBus.Publish(new IdleWorkersSelectedEvent(tick, selectIdle.FactionId, idleIds));
                _eventBus.Publish(new UnitsSelectedEvent(tick, selectIdle.FactionId, idleIds));
                break;
            }
        }
    }

    private void ExecutePlaceBuilding(PlaceBuildingCommand place, ulong tick)
    {
        var config = GetBuildingConfig(place.BuildingType);
        var snappedPos = _state.PlacementGrid.SnapToGrid(place.Position);

        // 1. Validate placement on grid
        if (!_state.PlacementGrid.CanPlace(snappedPos, config.GridSize, _state.ActiveBuildings, _state.ActiveResourceNodes, _bounds))
        {
            SimLogger.LogDebug("Simulation", $"Cannot place building {place.BuildingType} at {snappedPos}: Placement invalid.");
            return;
        }

        // 2. Validate and deduct resource cost
        var bank = _state.GetOrCreateResourceBank(place.FactionId);
        if (!bank.TryDeduct(config.Cost, tick, _eventBus, $"Construct {place.BuildingType}"))
        {
            SimLogger.LogDebug("Simulation", $"Cannot place building {place.BuildingType}: Insufficient resources.");
            return;
        }

        // 3. Create building entity
        var buildingId = _state.GenerateEntityId();
        var building = new BuildingEntity(
            buildingId,
            place.FactionId,
            place.BuildingType,
            snappedPos,
            config.GridSize,
            maxHealth: config.MaxHealth,
            baseBuildTimeTicks: config.BuildTimeTicks,
            populationProvided: config.PopulationProvided,
            acceptedDropOffTypes: config.AcceptedDropOffs,
            startsConstructed: false,
            baseCost: config.Cost,
            isFarm: place.BuildingType.Equals("farm", StringComparison.OrdinalIgnoreCase));

        _state.AddBuilding(building);
        _eventBus.Publish(new BuildingPlacedEvent(tick, buildingId, place.FactionId, place.BuildingType, snappedPos));
        SimLogger.LogInfo("Simulation", $"Placed building {place.BuildingType} {buildingId} at {snappedPos}.");
    }

    private void ExecuteQueueProduction(QueueProductionCommand queueProd, ulong tick)
    {
        if (!_state.TryGetBuilding(queueProd.BuildingId, out var building) || building == null || !building.IsAlive || !building.IsConstructed)
        {
            return;
        }

        if (building.FactionId != queueProd.FactionId || building.ProductionQueue.IsFull)
        {
            return;
        }

        var prodConfig = GetUnitProductionConfig(queueProd.UnitType);

        // Check population cap
        var popManager = _state.GetOrCreatePopulationManager(queueProd.FactionId);
        int livingCount = 0;
        for (int u = 0; u < _state.ActiveUnits.Count; u++)
        {
            if (_state.ActiveUnits[u].FactionId == queueProd.FactionId && _state.ActiveUnits[u].IsAlive)
            {
                livingCount++;
            }
        }
        popManager.SetCurrentPopulation(livingCount, tick);
        popManager.RecalculateCapacity(_state.ActiveBuildings, tick);

        if (!popManager.CanTrainUnit(prodConfig.PopulationCost))
        {
            SimLogger.LogDebug("Simulation", $"Cannot train {queueProd.UnitType}: Population cap reached.");
            return;
        }

        // Deduct cost
        var bank = _state.GetOrCreateResourceBank(queueProd.FactionId);
        if (!bank.TryDeduct(prodConfig.Cost, tick, _eventBus, $"Train {queueProd.UnitType}"))
        {
            SimLogger.LogDebug("Simulation", $"Cannot train {queueProd.UnitType}: Insufficient resources.");
            return;
        }

        var item = new ProductionQueueItem(
            queueProd.UnitType,
            prodConfig.DurationTicks,
            prodConfig.Cost,
            prodConfig.PopulationCost);

        if (building.ProductionQueue.TryEnqueue(item))
        {
            _eventBus.Publish(new ProductionStartedEvent(tick, building.Id, queueProd.FactionId, queueProd.UnitType, prodConfig.DurationTicks));
        }
    }

    private void ExecuteCancelProduction(CancelProductionCommand cancelProd, ulong tick)
    {
        if (!_state.TryGetBuilding(cancelProd.BuildingId, out var building) || building == null || building.FactionId != cancelProd.FactionId)
        {
            return;
        }

        var item = building.ProductionQueue.CancelAt(cancelProd.QueueIndex);
        if (item != null)
        {
            var bank = _state.GetOrCreateResourceBank(cancelProd.FactionId);
            if (item.Cost.Food > 0) bank.Deposit(ResourceType.Food, item.Cost.Food, tick, _eventBus);
            if (item.Cost.Wood > 0) bank.Deposit(ResourceType.Wood, item.Cost.Wood, tick, _eventBus);
            if (item.Cost.Gold > 0) bank.Deposit(ResourceType.Gold, item.Cost.Gold, tick, _eventBus);
            if (item.Cost.Stone > 0) bank.Deposit(ResourceType.Stone, item.Cost.Stone, tick, _eventBus);
            if (item.Cost.Iron > 0) bank.Deposit(ResourceType.Iron, item.Cost.Iron, tick, _eventBus);

            _eventBus.Publish(new ProductionCancelledEvent(tick, building.Id, cancelProd.FactionId, item.UnitType, item.Cost));
        }
    }

    private void UpdateWorkerTasks(ulong tick)
    {
        float dt = _config.DeltaTime;
        var units = _state.ActiveUnits;
        int count = units.Count;

        for (int i = 0; i < count; i++)
        {
            var unit = units[i];
            if (!unit.IsAlive || unit.WorkerState == null) continue;

            var worker = unit.WorkerState;

            switch (worker.TaskState)
            {
                case WorkerTaskState.MovingToResource:
                {
                    if (_state.TryGetResourceNode(worker.TargetResourceNodeId, out var node) && node != null && !node.IsDepleted)
                    {
                        float dist = unit.Position.DistanceTo(node.Position);
                        if (dist <= node.HarvestRadius)
                        {
                            worker.TaskState = WorkerTaskState.Harvesting;
                            unit.State = UnitState.Gathering;
                        }
                        else
                        {
                            MoveUnitTowards(unit, node.Position, dt, tick);
                        }
                    }
                    else if (_state.TryGetBuilding(worker.TargetResourceNodeId, out var farm) && farm != null && farm.IsFarm && farm.IsAlive && !farm.IsFarmDepleted)
                    {
                        float farmRadius = MathF.Max(farm.GridSize.X, farm.GridSize.Y) * 0.5f + 1.2f;
                        float dist = unit.Position.DistanceTo(farm.Position);
                        if (dist <= farmRadius)
                        {
                            worker.TaskState = WorkerTaskState.Harvesting;
                            unit.State = UnitState.Gathering;
                        }
                        else
                        {
                            MoveUnitTowards(unit, farm.Position, dt, tick);
                        }
                    }
                    else
                    {
                        // Target depleted/missing -> check if farm can be reseeded
                        if (_state.TryGetBuilding(worker.TargetResourceNodeId, out var depFarm) && depFarm != null && depFarm.IsFarm && depFarm.IsAlive)
                        {
                            var bank = _state.GetOrCreateResourceBank(unit.FactionId);
                            if (bank.TryDeduct(new ResourceCost(Wood: depFarm.FarmReseedCost), tick, _eventBus, "Auto-Reseed Farm"))
                            {
                                depFarm.ReseedFarm(tick, _eventBus);
                                worker.TaskState = WorkerTaskState.Harvesting;
                                unit.State = UnitState.Gathering;
                                break;
                            }
                        }

                        if (worker.HasCarriedResources && worker.CarriedResourceType.HasValue)
                        {
                            var dropOff = FindNearestDropOff(unit.FactionId, unit.Position, worker.CarriedResourceType.Value);
                            if (dropOff != null)
                            {
                                worker.TargetBuildingId = dropOff.Id;
                                worker.TaskState = WorkerTaskState.ReturningToDropOff;
                                unit.State = UnitState.Returning;
                            }
                            else
                            {
                                unit.Stop();
                            }
                        }
                        else
                        {
                            var nextTarget = FindNearestGatherTarget(unit.Position, null, unit.FactionId);
                            if (nextTarget.IsValid)
                            {
                                worker.TargetResourceNodeId = nextTarget;
                                worker.TaskState = WorkerTaskState.MovingToResource;
                                unit.State = UnitState.Gathering;
                            }
                            else
                            {
                                unit.Stop();
                            }
                        }
                    }
                    break;
                }

                case WorkerTaskState.Harvesting:
                {
                    if (_state.TryGetResourceNode(worker.TargetResourceNodeId, out var node) && node != null && !node.IsDepleted)
                    {
                        worker.HarvestProgressAccumulator += worker.HarvestRatePerTick;
                        while (worker.HarvestProgressAccumulator >= 1.0f && !worker.IsInventoryFull && !node.IsDepleted)
                        {
                            int request = Math.Min((int)MathF.Floor(worker.HarvestProgressAccumulator), worker.CarryCapacity - worker.CarriedAmount);
                            if (request <= 0) break;

                            int harvested = node.Harvest(request, tick, unit.Id, _eventBus);
                            worker.AddCarried(node.ResourceType, harvested);
                            worker.HarvestProgressAccumulator -= harvested;

                            _eventBus.Publish(new ResourceHarvestedEvent(
                                tick,
                                unit.Id,
                                node.Id,
                                node.ResourceType,
                                harvested,
                                worker.CarriedAmount));
                        }

                        if (worker.IsInventoryFull || node.IsDepleted)
                        {
                            var dropOff = FindNearestDropOff(unit.FactionId, unit.Position, worker.CarriedResourceType ?? node.ResourceType);
                            if (dropOff != null)
                            {
                                worker.TargetBuildingId = dropOff.Id;
                                worker.TaskState = WorkerTaskState.ReturningToDropOff;
                                unit.State = UnitState.Returning;
                            }
                            else
                            {
                                unit.State = UnitState.Idle;
                            }
                        }
                    }
                    else if (_state.TryGetBuilding(worker.TargetResourceNodeId, out var farm) && farm != null && farm.IsFarm && farm.IsAlive && !farm.IsFarmDepleted)
                    {
                        worker.HarvestProgressAccumulator += worker.HarvestRatePerTick;
                        while (worker.HarvestProgressAccumulator >= 1.0f && !worker.IsInventoryFull && !farm.IsFarmDepleted)
                        {
                            int request = Math.Min((int)MathF.Floor(worker.HarvestProgressAccumulator), worker.CarryCapacity - worker.CarriedAmount);
                            if (request <= 0) break;

                            int harvested = farm.HarvestFarmFood(request, tick, unit.Id, _eventBus);
                            worker.AddCarried(ResourceType.Food, harvested);
                            worker.HarvestProgressAccumulator -= harvested;

                            _eventBus.Publish(new FarmHarvestedEvent(
                                tick,
                                unit.Id,
                                farm.Id,
                                harvested,
                                farm.FarmFoodRemaining));

                            _eventBus.Publish(new ResourceHarvestedEvent(
                                tick,
                                unit.Id,
                                farm.Id,
                                ResourceType.Food,
                                harvested,
                                worker.CarriedAmount));
                        }

                        if (worker.IsInventoryFull)
                        {
                            var dropOff = FindNearestDropOff(unit.FactionId, unit.Position, ResourceType.Food);
                            if (dropOff != null)
                            {
                                worker.TargetBuildingId = dropOff.Id;
                                worker.TaskState = WorkerTaskState.ReturningToDropOff;
                                unit.State = UnitState.Returning;
                            }
                            else
                            {
                                unit.State = UnitState.Idle;
                            }
                        }
                        else if (farm.IsFarmDepleted)
                        {
                            var bank = _state.GetOrCreateResourceBank(unit.FactionId);
                            if (bank.TryDeduct(new ResourceCost(Wood: farm.FarmReseedCost), tick, _eventBus, "Auto-Reseed Farm"))
                            {
                                farm.ReseedFarm(tick, _eventBus);
                            }
                            else if (worker.HasCarriedResources)
                            {
                                var dropOff = FindNearestDropOff(unit.FactionId, unit.Position, ResourceType.Food);
                                if (dropOff != null)
                                {
                                    worker.TargetBuildingId = dropOff.Id;
                                    worker.TaskState = WorkerTaskState.ReturningToDropOff;
                                    unit.State = UnitState.Returning;
                                }
                                else
                                {
                                    unit.State = UnitState.Idle;
                                }
                            }
                            else
                            {
                                var nextTarget = FindNearestGatherTarget(unit.Position, ResourceType.Food, unit.FactionId);
                                if (nextTarget.IsValid)
                                {
                                    worker.TargetResourceNodeId = nextTarget;
                                    worker.TaskState = WorkerTaskState.MovingToResource;
                                    unit.State = UnitState.Gathering;
                                }
                                else
                                {
                                    unit.Stop();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Target depleted
                        if (_state.TryGetBuilding(worker.TargetResourceNodeId, out var depFarm) && depFarm != null && depFarm.IsFarm && depFarm.IsAlive)
                        {
                            var bank = _state.GetOrCreateResourceBank(unit.FactionId);
                            if (bank.TryDeduct(new ResourceCost(Wood: depFarm.FarmReseedCost), tick, _eventBus, "Auto-Reseed Farm"))
                            {
                                depFarm.ReseedFarm(tick, _eventBus);
                                break;
                            }
                        }

                        if (worker.HasCarriedResources && worker.CarriedResourceType.HasValue)
                        {
                            var dropOff = FindNearestDropOff(unit.FactionId, unit.Position, worker.CarriedResourceType.Value);
                            if (dropOff != null)
                            {
                                worker.TargetBuildingId = dropOff.Id;
                                worker.TaskState = WorkerTaskState.ReturningToDropOff;
                                unit.State = UnitState.Returning;
                            }
                            else
                            {
                                unit.State = UnitState.Idle;
                            }
                        }
                        else
                        {
                            var nextTarget = FindNearestGatherTarget(unit.Position, null, unit.FactionId);
                            if (nextTarget.IsValid)
                            {
                                worker.TargetResourceNodeId = nextTarget;
                                worker.TaskState = WorkerTaskState.MovingToResource;
                                unit.State = UnitState.Gathering;
                            }
                            else
                            {
                                unit.Stop();
                            }
                        }
                    }
                    break;
                }

                case WorkerTaskState.ReturningToDropOff:
                {
                    if (_state.TryGetBuilding(worker.TargetBuildingId, out var dropOff) &&
                        dropOff != null && dropOff.IsAlive &&
                        worker.CarriedResourceType.HasValue &&
                        dropOff.AcceptsDropOff(worker.CarriedResourceType.Value))
                    {
                        float dropRadius = MathF.Max(dropOff.GridSize.X, dropOff.GridSize.Y) * 0.5f + 1.2f;
                        float dist = unit.Position.DistanceTo(dropOff.Position);

                        if (dist <= dropRadius)
                        {
                            // Deposit resources into faction bank
                            var carried = worker.EmptyInventory();
                            if (carried.HasValue)
                            {
                                var bank = _state.GetOrCreateResourceBank(unit.FactionId);
                                bank.Deposit(carried.Value.Type, carried.Value.Amount, tick, _eventBus, unit.Id);
                            }

                            // Return to resource gathering
                            if (_state.TryGetResourceNode(worker.TargetResourceNodeId, out var prevNode) && prevNode != null && !prevNode.IsDepleted)
                            {
                                worker.TaskState = WorkerTaskState.MovingToResource;
                                unit.State = UnitState.Gathering;
                            }
                            else if (_state.TryGetBuilding(worker.TargetResourceNodeId, out var prevFarm) && prevFarm != null && prevFarm.IsFarm && prevFarm.IsAlive && (!prevFarm.IsFarmDepleted || _state.GetOrCreateResourceBank(unit.FactionId).CanAfford(new ResourceCost(Wood: prevFarm.FarmReseedCost))))
                            {
                                worker.TaskState = WorkerTaskState.MovingToResource;
                                unit.State = UnitState.Gathering;
                            }
                            else
                            {
                                var nextTarget = FindNearestGatherTarget(unit.Position, carried?.Type, unit.FactionId);
                                if (nextTarget.IsValid)
                                {
                                    worker.TargetResourceNodeId = nextTarget;
                                    worker.TaskState = WorkerTaskState.MovingToResource;
                                    unit.State = UnitState.Gathering;
                                }
                                else
                                {
                                    unit.Stop();
                                }
                            }
                        }
                        else
                        {
                            MoveUnitTowards(unit, dropOff.Position, dt, tick);
                        }
                    }
                    else
                    {
                        // Drop-off invalid or destroyed -> seek new drop-off
                        if (worker.HasCarriedResources && worker.CarriedResourceType.HasValue)
                        {
                            var newDropOff = FindNearestDropOff(unit.FactionId, unit.Position, worker.CarriedResourceType.Value);
                            if (newDropOff != null)
                            {
                                worker.TargetBuildingId = newDropOff.Id;
                            }
                            else
                            {
                                unit.Stop();
                            }
                        }
                        else
                        {
                            unit.Stop();
                        }
                    }
                    break;
                }

                case WorkerTaskState.MovingToConstruct:
                {
                    if (_state.TryGetBuilding(worker.TargetBuildingId, out var building) && building != null && building.IsAlive && !building.IsConstructed)
                    {
                        float constructRadius = MathF.Max(building.GridSize.X, building.GridSize.Y) * 0.5f + 1.2f;
                        float dist = unit.Position.DistanceTo(building.Position);

                        if (dist <= constructRadius)
                        {
                            worker.TaskState = WorkerTaskState.Constructing;
                            unit.State = UnitState.Constructing;
                        }
                        else
                        {
                            MoveUnitTowards(unit, building.Position, dt, tick);
                        }
                    }
                    else
                    {
                        unit.Stop();
                    }
                    break;
                }

                case WorkerTaskState.Constructing:
                {
                    if (_state.TryGetBuilding(worker.TargetBuildingId, out var building) && building != null && building.IsAlive && !building.IsConstructed)
                    {
                        float constructRadius = MathF.Max(building.GridSize.X, building.GridSize.Y) * 0.5f + 1.5f;
                        float dist = unit.Position.DistanceTo(building.Position);

                        if (dist <= constructRadius)
                        {
                            building.Construct(worker.BuildPowerPerTick, tick, _eventBus, out bool completedJustNow);
                            if (completedJustNow)
                            {
                                var popManager = _state.GetOrCreatePopulationManager(building.FactionId);
                                popManager.RecalculateCapacity(_state.ActiveBuildings, tick, _eventBus);
                                unit.Stop();
                            }
                        }
                        else
                        {
                            worker.TaskState = WorkerTaskState.MovingToConstruct;
                        }
                    }
                    else
                    {
                        unit.Stop();
                    }
                    break;
                }

                case WorkerTaskState.MovingToRepair:
                {
                    if (_state.TryGetBuilding(worker.TargetBuildingId, out var building) && building != null && building.IsAlive && building.IsDamaged)
                    {
                        float repairRadius = MathF.Max(building.GridSize.X, building.GridSize.Y) * 0.5f + 1.2f;
                        float dist = unit.Position.DistanceTo(building.Position);

                        if (dist <= repairRadius)
                        {
                            worker.TaskState = WorkerTaskState.Repairing;
                            unit.State = UnitState.Repairing;
                        }
                        else
                        {
                            MoveUnitTowards(unit, building.Position, dt, tick);
                        }
                    }
                    else
                    {
                        unit.Stop();
                    }
                    break;
                }

                case WorkerTaskState.Repairing:
                {
                    if (_state.TryGetBuilding(worker.TargetBuildingId, out var building) && building != null && building.IsAlive && building.IsDamaged)
                    {
                        float repairRadius = MathF.Max(building.GridSize.X, building.GridSize.Y) * 0.5f + 1.5f;
                        float dist = unit.Position.DistanceTo(building.Position);

                        if (dist <= repairRadius)
                        {
                            float missingHp = building.MaxHealth - building.CurrentHealth;
                            float repairAmount = MathF.Min(worker.RepairPowerPerTick, missingHp);
                            float costRatio = (repairAmount / building.MaxHealth) * 0.5f;
                            int woodCost = (int)MathF.Ceiling(building.BaseCost.Wood * costRatio);
                            int stoneCost = (int)MathF.Ceiling(building.BaseCost.Stone * costRatio);

                            var bank = _state.GetOrCreateResourceBank(unit.FactionId);
                            if (bank.CanAfford(new ResourceCost(Wood: woodCost, Stone: stoneCost)))
                            {
                                if (woodCost > 0 || stoneCost > 0)
                                {
                                    bank.TryDeduct(new ResourceCost(Wood: woodCost, Stone: stoneCost), tick, _eventBus, "Building Repair");
                                }

                                building.Repair(repairAmount, tick, _eventBus, out bool fullyRepaired);
                                if (fullyRepaired)
                                {
                                    unit.Stop();
                                }
                            }
                            else
                            {
                                unit.Stop();
                            }
                        }
                        else
                        {
                            worker.TaskState = WorkerTaskState.MovingToRepair;
                        }
                    }
                    else
                    {
                        unit.Stop();
                    }
                    break;
                }
            }
        }
    }

    private void MoveUnitTowards(UnitEntity unit, Vector2D destination, float dt, ulong tick)
    {
        var prevPos = unit.Position;
        float maxDist = unit.MovementSpeed * dt;
        var nextPos = unit.Position.MoveTowards(destination, maxDist);
        nextPos = _bounds.Clamp(nextPos);

        unit.Position = nextPos;
        _spatialGrid.UpdatePosition(unit.Id, prevPos, unit.Position);
        _eventBus.Publish(new UnitMovedEvent(tick, unit.Id, prevPos, unit.Position));
    }

    private ResourceNodeEntity? FindNearestResourceNode(Vector2D position, ResourceType? typeFilter)
    {
        ResourceNodeEntity? nearest = null;
        float nearestDistSq = float.MaxValue;

        var nodes = _state.ActiveResourceNodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.IsDepleted) continue;
            if (typeFilter.HasValue && node.ResourceType != typeFilter.Value) continue;

            float distSq = position.DistanceSquaredTo(node.Position);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = node;
            }
        }

        return nearest;
    }

    private EntityId FindNearestGatherTarget(Vector2D position, ResourceType? typeFilter, FactionId factionId)
    {
        EntityId nearestId = EntityId.None;
        float nearestDistSq = float.MaxValue;

        var nodes = _state.ActiveResourceNodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.IsDepleted) continue;
            if (typeFilter.HasValue && node.ResourceType != typeFilter.Value) continue;

            float distSq = position.DistanceSquaredTo(node.Position);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearestId = node.Id;
            }
        }

        if (!typeFilter.HasValue || typeFilter.Value == ResourceType.Food)
        {
            var buildings = _state.ActiveBuildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                if (b.FactionId != factionId || !b.IsAlive || !b.IsConstructed || !b.IsFarm || b.IsFarmDepleted) continue;

                float distSq = position.DistanceSquaredTo(b.Position);
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearestId = b.Id;
                }
            }
        }

        return nearestId;
    }

    private BuildingEntity? FindNearestDropOff(FactionId factionId, Vector2D position, ResourceType resourceType)
    {
        BuildingEntity? nearest = null;
        float nearestDistSq = float.MaxValue;

        var buildings = _state.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (b.FactionId != factionId || !b.IsAlive || !b.IsConstructed) continue;
            if (!b.AcceptsDropOff(resourceType)) continue;

            float distSq = position.DistanceSquaredTo(b.Position);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = b;
            }
        }

        return nearest;
    }

    private void UpdateProduction(ulong tick)
    {
        var buildings = _state.ActiveBuildings;
        int count = buildings.Count;

        for (int i = 0; i < count; i++)
        {
            var building = buildings[i];
            if (!building.IsAlive || !building.IsConstructed || building.ProductionQueue.IsEmpty) continue;

            var item = building.ProductionQueue.CurrentItem;
            if (item == null) continue;

            item.AdvanceTicks(1);
            _eventBus.Publish(new ProductionProgressEvent(tick, building.Id, item.UnitType, item.ProgressTicks, item.TotalDurationTicks));

            if (item.IsCompleted)
            {
                building.ProductionQueue.TryDequeue();

                var spawnPos = _bounds.Clamp(building.RallyPoint);
                var unitId = _state.GenerateEntityId();

                UnitEntity producedUnit;
                if (item.UnitType.Equals("villager", StringComparison.OrdinalIgnoreCase) ||
                    item.UnitType.Equals("worker", StringComparison.OrdinalIgnoreCase))
                {
                    producedUnit = new UnitEntity(
                        unitId,
                        building.FactionId,
                        item.UnitType,
                        spawnPos,
                        maxHealth: 50f,
                        attackDamage: 5f,
                        attackRange: 1.0f,
                        movementSpeed: 3.5f,
                        attackCooldownTicks: 25,
                        killXpValue: 20,
                        workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 0.5f, buildPowerPerTick: 1.0f));
                }
                else
                {
                    // Swordsman / Military unit
                    producedUnit = new UnitEntity(
                        unitId,
                        building.FactionId,
                        item.UnitType,
                        spawnPos,
                        maxHealth: 100f,
                        attackDamage: 15f,
                        attackRange: 1.5f,
                        movementSpeed: 3.5f,
                        attackCooldownTicks: 20,
                        killXpValue: 50);
                }

                _state.AddUnit(producedUnit);
                _spatialGrid.Insert(producedUnit.Id, producedUnit.Position);

                _eventBus.Publish(new UnitSpawnedEvent(tick, unitId, building.FactionId, item.UnitType, producedUnit.Position));
                _eventBus.Publish(new ProductionCompletedEvent(tick, building.Id, building.FactionId, item.UnitType, unitId));
                SimLogger.LogInfo("Production", $"Trained unit {item.UnitType} {unitId} at {spawnPos}.");
            }
        }
    }

    private void UpdatePopulation(ulong tick)
    {
        for (int i = 0; i < _state.ActiveUnits.Count; i++)
        {
            _state.GetOrCreatePopulationManager(_state.ActiveUnits[i].FactionId);
        }
        for (int i = 0; i < _state.ActiveBuildings.Count; i++)
        {
            _state.GetOrCreatePopulationManager(_state.ActiveBuildings[i].FactionId);
        }

        foreach (var (factionId, popManager) in _state.PopulationManagers)
        {
            int livingUnits = 0;
            var units = _state.ActiveUnits;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].FactionId == factionId && units[i].IsAlive)
                {
                    livingUnits++;
                }
            }

            popManager.SetCurrentPopulation(livingUnits, tick, _eventBus);
            popManager.RecalculateCapacity(_state.ActiveBuildings, tick, _eventBus);
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
                    unit.ResetCooldown();
                    target.TakeDamage(unit.AttackDamage, unit.Id, unit.FactionId, tick, _eventBus, out bool killed);

                    if (killed)
                    {
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

    private void CleanupEntities()
    {
        // 1. Dead units
        var units = _state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (!unit.IsAlive)
            {
                _spatialGrid.Remove(unit.Id);

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

        // 2. Depleted resource nodes
        _state.RemoveDepletedNodes();

        // 3. Destroyed buildings
        _state.RemoveDeadBuildings();
    }

    public EntityId[] GetIdleWorkers(FactionId factionId)
    {
        var list = new List<EntityId>();
        var units = _state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var u = units[i];
            if (u.FactionId == factionId && u.IsIdleWorker)
            {
                list.Add(u.Id);
            }
        }
        return list.ToArray();
    }

    private static (Vector2D GridSize, float MaxHealth, float BuildTimeTicks, int PopulationProvided, ResourceCost Cost, ResourceType[] AcceptedDropOffs) GetBuildingConfig(string buildingType)
    {
        return buildingType.ToLowerInvariant() switch
        {
            "town_center" => (
                new Vector2D(4f, 4f),
                1200f,
                200f,
                10,
                new ResourceCost(Wood: 275, Stone: 100),
                new[] { ResourceType.Food, ResourceType.Wood, ResourceType.Gold, ResourceType.Stone, ResourceType.Iron }),

            "house" => (
                new Vector2D(2f, 2f),
                300f,
                40f,
                5,
                new ResourceCost(Wood: 50),
                Array.Empty<ResourceType>()),

            "barracks" => (
                new Vector2D(3f, 3f),
                800f,
                80f,
                0,
                new ResourceCost(Wood: 150),
                Array.Empty<ResourceType>()),

            "storage_pit" => (
                new Vector2D(2f, 2f),
                400f,
                50f,
                0,
                new ResourceCost(Wood: 100),
                new[] { ResourceType.Wood, ResourceType.Gold, ResourceType.Stone, ResourceType.Iron }),

            "lumber_camp" => (
                new Vector2D(2f, 2f),
                400f,
                50f,
                0,
                new ResourceCost(Wood: 100),
                new[] { ResourceType.Wood }),

            "mining_camp" => (
                new Vector2D(2f, 2f),
                400f,
                50f,
                0,
                new ResourceCost(Wood: 100),
                new[] { ResourceType.Gold, ResourceType.Iron }),

            "stone_quarry_camp" or "stone_quarry" => (
                new Vector2D(2f, 2f),
                400f,
                50f,
                0,
                new ResourceCost(Wood: 100),
                new[] { ResourceType.Stone }),

            "granary" or "mill" => (
                new Vector2D(2f, 2f),
                400f,
                50f,
                0,
                new ResourceCost(Wood: 100),
                new[] { ResourceType.Food }),

            "farm" => (
                new Vector2D(2f, 2f),
                200f,
                30f,
                0,
                new ResourceCost(Wood: 60),
                Array.Empty<ResourceType>()),

            "watchtower" or "tower" => (
                new Vector2D(2f, 2f),
                600f,
                60f,
                0,
                new ResourceCost(Wood: 50, Stone: 125),
                Array.Empty<ResourceType>()),

            _ => (
                new Vector2D(2f, 2f),
                300f,
                50f,
                0,
                new ResourceCost(Wood: 100),
                Array.Empty<ResourceType>())
        };
    }

    private static (ResourceCost Cost, int DurationTicks, int PopulationCost) GetUnitProductionConfig(string unitType)
    {
        return unitType.ToLowerInvariant() switch
        {
            "villager" or "worker" => (
                new ResourceCost(Food: 50),
                50,
                1),

            "swordsman" => (
                new ResourceCost(Food: 60, Iron: 20),
                60,
                1),

            "archer" => (
                new ResourceCost(Food: 40, Wood: 35),
                60,
                1),

            "legionary" => (
                new ResourceCost(Food: 60, Iron: 25),
                65,
                1),

            _ => (
                new ResourceCost(Food: 50),
                50,
                1)
        };
    }
}
