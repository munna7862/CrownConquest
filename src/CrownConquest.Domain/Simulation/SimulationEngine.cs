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
    private readonly Dictionary<string, TechnologyDefinition> _techRegistry = new(StringComparer.OrdinalIgnoreCase);

    public ulong CurrentTick => _state.CurrentTick;
    public SimulationConfig Config => _config;
    public SimulationRandom Random => _random;
    public CommandQueue CommandQueue => _commandQueue;
    public DomainEventBus EventBus => _eventBus;
    public SimulationState State => _state;
    public BattlefieldBounds Bounds => _bounds;
    public SpatialGrid SpatialGrid => _spatialGrid;
    public IReadOnlyDictionary<string, TechnologyDefinition> TechRegistry => _techRegistry;

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

        RegisterDefaultTechnologies();
    }

    public void RegisterTechnology(TechnologyDefinition tech)
    {
        ArgumentNullException.ThrowIfNull(tech);
        _techRegistry[tech.Id] = tech;
    }

    public bool TryGetTechnology(string techId, out TechnologyDefinition? tech)
    {
        return _techRegistry.TryGetValue(techId, out tech);
    }

    private void RegisterDefaultTechnologies()
    {
        RegisterTechnology(new TechnologyDefinition(
            "forging",
            "Forging",
            "Increases infantry and cavalry melee attack damage by +2.",
            TechCategory.Military,
            CivilizationEra.Classical,
            new ResourceCost(Food: 150, Gold: 50),
            researchDurationTicks: 40,
            new TechModifiers(MeleeAttackBonus: 2, CavalryAttackBonus: 2),
            requiredBuildingTypes: new[] { "blacksmith" }));

        RegisterTechnology(new TechnologyDefinition(
            "iron_weapons",
            "Iron Weapons",
            "Increases infantry and cavalry melee attack damage by an additional +3.",
            TechCategory.Military,
            CivilizationEra.Imperial,
            new ResourceCost(Food: 220, Gold: 120, Iron: 50),
            researchDurationTicks: 60,
            new TechModifiers(MeleeAttackBonus: 3, CavalryAttackBonus: 3),
            requiredTechIds: new[] { "forging" },
            requiredBuildingTypes: new[] { "blacksmith" }));

        RegisterTechnology(new TechnologyDefinition(
            "scale_armor",
            "Scale Armor",
            "Increases infantry and cavalry armor by +2.",
            TechCategory.Military,
            CivilizationEra.Classical,
            new ResourceCost(Food: 100, Gold: 100),
            researchDurationTicks: 40,
            new TechModifiers(MeleeArmorBonus: 2, CavalryArmorBonus: 2),
            requiredBuildingTypes: new[] { "blacksmith" }));

        RegisterTechnology(new TechnologyDefinition(
            "plate_armor",
            "Plate Armor",
            "Increases infantry and cavalry armor by an additional +3.",
            TechCategory.Military,
            CivilizationEra.Imperial,
            new ResourceCost(Food: 250, Gold: 200, Iron: 80),
            researchDurationTicks: 60,
            new TechModifiers(MeleeArmorBonus: 3, CavalryArmorBonus: 3),
            requiredTechIds: new[] { "scale_armor" },
            requiredBuildingTypes: new[] { "blacksmith" }));

        RegisterTechnology(new TechnologyDefinition(
            "fletching",
            "Fletching",
            "Archers gain +1 attack damage and +1.0 attack range.",
            TechCategory.Military,
            CivilizationEra.Classical,
            new ResourceCost(Food: 100, Wood: 50, Gold: 50),
            researchDurationTicks: 40,
            new TechModifiers(RangedAttackBonus: 1, RangedRangeBonus: 1.0f),
            requiredBuildingTypes: new[] { "blacksmith" }));

        RegisterTechnology(new TechnologyDefinition(
            "bodkin_arrow",
            "Bodkin Arrow",
            "Archers gain an additional +2 attack damage and +1.5 attack range.",
            TechCategory.Military,
            CivilizationEra.Imperial,
            new ResourceCost(Food: 200, Wood: 100, Gold: 100),
            researchDurationTicks: 50,
            new TechModifiers(RangedAttackBonus: 2, RangedRangeBonus: 1.5f),
            requiredTechIds: new[] { "fletching" },
            requiredBuildingTypes: new[] { "blacksmith" }));

        RegisterTechnology(new TechnologyDefinition(
            "double_bit_axe",
            "Double-Bit Axe",
            "Workers gather resources +20% faster.",
            TechCategory.Economy,
            CivilizationEra.Classical,
            new ResourceCost(Food: 100, Wood: 50),
            researchDurationTicks: 35,
            new TechModifiers(GatherRateBonus: 0.20f),
            requiredBuildingTypes: new[] { "town_center" }));

        RegisterTechnology(new TechnologyDefinition(
            "husbandry",
            "Husbandry",
            "Cavalry units move +1.0 speed faster.",
            TechCategory.Military,
            CivilizationEra.Classical,
            new ResourceCost(Food: 150),
            researchDurationTicks: 35,
            new TechModifiers(CavalrySpeedBonus: 1.0f),
            requiredBuildingTypes: new[] { "stable" }));
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

        // 2. Update hero mana regen and ability cooldowns
        UpdateHeroes(tick);

        // 3. Update worker gathering and construction state machine
        UpdateWorkerTasks(tick);

        // 4. Auto-acquire targets for idle combat units in aggro range
        UpdateTargetAcquisition();

        // 5. Update unit movements and navigation with boundary clamping
        UpdateMovements(tick);

        // 6. Update combat engagements & cooldowns
        UpdateCombat(tick);

        // 6.5. Update defensive towers autonomous defense
        UpdateTowers(tick);

        // 7. Update unit morale, hero auras, and routing state machines
        UpdateMorale(tick);

        // 8. Update building production queues
        UpdateProduction(tick);

        // 9. Update building research queues
        UpdateResearch(tick);

        // 10. Update era advancement state machines
        UpdateEraAdvancement(tick);

        // 11. Update population counts and capacities
        UpdatePopulation(tick);

        // 12. Cleanup deceased entities and depleted nodes at tick boundary
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
            case SetFormationCommand setForm:
            {
                if (_state.TryGetUnit(setForm.UnitId, out var unit) && unit != null && unit.IsAlive)
                {
                    unit.SetFormation(setForm.Formation);
                    _eventBus.Publish(new UnitFormationChangedEvent(tick, unit.Id, setForm.Formation));
                }
                break;
            }

            case SetSquadFormationCommand squadForm:
            {
                for (int i = 0; i < squadForm.UnitIds.Count; i++)
                {
                    if (_state.TryGetUnit(squadForm.UnitIds[i], out var unit) && unit != null && unit.IsAlive)
                    {
                        unit.SetFormation(squadForm.Formation);
                        _eventBus.Publish(new UnitFormationChangedEvent(tick, unit.Id, squadForm.Formation));
                    }
                }
                break;
            }

            case RallyUnitCommand rally:
            {
                if (_state.TryGetUnit(rally.UnitId, out var unit) && unit != null && unit.IsAlive)
                {
                    unit.Rally(25.0f);
                    _eventBus.Publish(new UnitRalliedEvent(tick, unit.Id, unit.FactionId, unit.Morale.CurrentMorale));
                }
                break;
            }

            case RallySquadCommand squadRally:
            {
                var activeUnits = _state.ActiveUnits;
                for (int i = 0; i < activeUnits.Count; i++)
                {
                    var unit = activeUnits[i];
                    if (unit.FactionId == squadRally.FactionId && unit.IsAlive && unit.Position.DistanceTo(squadRally.Center) <= squadRally.Radius)
                    {
                        unit.Rally(25.0f);
                        _eventBus.Publish(new UnitRalliedEvent(tick, unit.Id, unit.FactionId, unit.Morale.CurrentMorale));
                    }
                }
                break;
            }

            case AttachToHeroCommand attachHero:
            {
                ExecuteAttachToHero(attachHero, tick);
                break;
            }

            case DetachFromHeroCommand detachHero:
            {
                ExecuteDetachFromHero(detachHero, tick);
                break;
            }

            case CastHeroAbilityCommand castAbility:
            {
                ExecuteCastHeroAbility(castAbility, tick);
                break;
            }

            case AllocateHeroAttributeCommand allocAttr:
            {
                ExecuteAllocateHeroAttribute(allocAttr, tick);
                break;
            }

            case SpawnUnitCommand spawn:
            {
                var unitId = _state.GenerateEntityId();
                var clampedPos = _bounds.Clamp(spawn.Position);

                WorkerGatherState? workerState = null;
                if (spawn.UnitType.Equals("villager", StringComparison.OrdinalIgnoreCase) ||
                    spawn.UnitType.Equals("worker", StringComparison.OrdinalIgnoreCase) ||
                    spawn.UnitType.Equals("plebeian", StringComparison.OrdinalIgnoreCase))
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

            case AdvanceEraCommand advanceEra:
            {
                ExecuteAdvanceEra(advanceEra, tick);
                break;
            }

            case CancelEraAdvancementCommand cancelEra:
            {
                ExecuteCancelEraAdvancement(cancelEra, tick);
                break;
            }

            case StartResearchCommand startResearch:
            {
                ExecuteStartResearch(startResearch, tick);
                break;
            }

            case CancelResearchCommand cancelResearch:
            {
                ExecuteCancelResearch(cancelResearch, tick);
                break;
            }

            case AttackBuildingCommand attackBld:
            {
                for (int i = 0; i < attackBld.UnitIds.Length; i++)
                {
                    if (_state.TryGetUnit(attackBld.UnitIds[i], out var unit) && unit != null && unit.FactionId == attackBld.FactionId && unit.IsAlive)
                    {
                        unit.Attack(attackBld.TargetBuildingId);
                    }
                }
                break;
            }

            case ToggleGateCommand toggleGate:
            {
                if (_state.TryGetBuilding(toggleGate.GateId, out var gate) && gate != null && gate.FactionId == toggleGate.FactionId && gate.IsGate && gate.GateDefense != null)
                {
                    var oldState = gate.GateDefense.State;
                    if (toggleGate.TargetState.HasValue)
                    {
                        gate.GateDefense.TrySetState(toggleGate.TargetState.Value);
                    }
                    else
                    {
                        gate.GateDefense.Toggle();
                    }
                    _eventBus.Publish(new GateStateChangedEvent(tick, gate.Id, gate.FactionId, oldState, gate.GateDefense.State));
                }
                break;
            }

            case GarrisonTowerCommand garrison:
            {
                if (_state.TryGetBuilding(garrison.TowerId, out var tower) && tower != null && tower.FactionId == garrison.FactionId && tower.IsTower && tower.TowerDefense != null && tower.IsConstructed && tower.IsAlive)
                {
                    for (int i = 0; i < garrison.UnitIds.Length; i++)
                    {
                        var unitId = garrison.UnitIds[i];
                        if (_state.TryGetUnit(unitId, out var unit) && unit != null && unit.FactionId == garrison.FactionId && unit.IsAlive && !unit.IsHero && unit.Archetype != UnitArchetype.Siege)
                        {
                            if (tower.TowerDefense.TryGarrison(unitId))
                            {
                                unit.Stop();
                                unit.Position = tower.Position;
                                _eventBus.Publish(new UnitGarrisonedEvent(tick, tower.Id, unit.Id, tower.FactionId, tower.TowerDefense.GarrisonCount));
                            }
                        }
                    }
                }
                break;
            }

            case UngarrisonTowerCommand ungarrison:
            {
                if (_state.TryGetBuilding(ungarrison.TowerId, out var tower) && tower != null && tower.FactionId == ungarrison.FactionId && tower.IsTower && tower.TowerDefense != null)
                {
                    var egressUnits = tower.TowerDefense.UngarrisonAll();
                    for (int i = 0; i < egressUnits.Count; i++)
                    {
                        var uId = egressUnits[i];
                        if (_state.TryGetUnit(uId, out var u) && u != null && u.IsAlive)
                        {
                            var egressPos = new Vector2D(tower.Position.X + 1.5f + (i * 0.5f), tower.Position.Y + 1.5f);
                            egressPos = _bounds.Clamp(egressPos);
                            u.Position = egressPos;
                            _eventBus.Publish(new UnitUngarrisonedEvent(tick, tower.Id, u.Id, tower.FactionId, egressPos));
                        }
                    }
                }
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

    private void ExecuteAdvanceEra(AdvanceEraCommand cmd, ulong tick)
    {
        var eraState = _state.GetOrCreateEraState(cmd.FactionId);
        if (!eraState.CanAdvance(cmd.TargetEra, out string reason))
        {
            SimLogger.LogDebug("Simulation", $"Cannot advance era: {reason}");
            return;
        }

        if (!_state.TryGetBuilding(cmd.BuildingId, out var building) || building == null ||
            building.FactionId != cmd.FactionId || !building.IsConstructed || !building.IsAlive ||
            !building.BuildingType.Equals("town_center", StringComparison.OrdinalIgnoreCase))
        {
            SimLogger.LogDebug("Simulation", "Era advancement must take place at a constructed Town Center.");
            return;
        }

        var eraConfig = GetEraConfig(cmd.TargetEra);

        // Verify building prerequisites
        foreach (var reqBuilding in eraConfig.RequiredBuildingTypes)
        {
            bool hasBuilding = false;
            foreach (var b in _state.ActiveBuildings)
            {
                if (b.FactionId == cmd.FactionId && b.IsConstructed && b.IsAlive &&
                    b.BuildingType.Equals(reqBuilding, StringComparison.OrdinalIgnoreCase))
                {
                    hasBuilding = true;
                    break;
                }
            }

            if (!hasBuilding)
            {
                SimLogger.LogDebug("Simulation", $"Cannot advance era: Missing required building {reqBuilding}.");
                return;
            }
        }

        var bank = _state.GetOrCreateResourceBank(cmd.FactionId);
        if (!bank.TryDeduct(eraConfig.Cost, tick, _eventBus, $"Advance to {cmd.TargetEra}"))
        {
            SimLogger.LogDebug("Simulation", "Cannot advance era: Insufficient resources.");
            return;
        }

        eraState.TryStartAdvancement(cmd.TargetEra, eraConfig.DurationTicks, cmd.BuildingId, eraConfig.Cost, tick, _eventBus);
        SimLogger.LogInfo("Simulation", $"Faction {cmd.FactionId} started advancement to {cmd.TargetEra}.");
    }

    private void ExecuteCancelEraAdvancement(CancelEraAdvancementCommand cmd, ulong tick)
    {
        var eraState = _state.GetOrCreateEraState(cmd.FactionId);
        var refund = eraState.CancelAdvancement(tick, _eventBus);
        if (!refund.IsZero)
        {
            var bank = _state.GetOrCreateResourceBank(cmd.FactionId);
            if (refund.Food > 0) bank.Deposit(ResourceType.Food, refund.Food, tick, _eventBus);
            if (refund.Wood > 0) bank.Deposit(ResourceType.Wood, refund.Wood, tick, _eventBus);
            if (refund.Gold > 0) bank.Deposit(ResourceType.Gold, refund.Gold, tick, _eventBus);
            if (refund.Stone > 0) bank.Deposit(ResourceType.Stone, refund.Stone, tick, _eventBus);
            if (refund.Iron > 0) bank.Deposit(ResourceType.Iron, refund.Iron, tick, _eventBus);
        }
    }

    private void ExecuteStartResearch(StartResearchCommand cmd, ulong tick)
    {
        if (!_state.TryGetBuilding(cmd.BuildingId, out var building) || building == null ||
            building.FactionId != cmd.FactionId || !building.IsConstructed || !building.IsAlive ||
            building.ResearchQueue.IsFull)
        {
            return;
        }

        if (!TryGetTechnology(cmd.TechnologyId, out var tech) || tech == null)
        {
            SimLogger.LogDebug("Simulation", $"Unknown technology {cmd.TechnologyId}");
            return;
        }

        var eraState = _state.GetOrCreateEraState(cmd.FactionId);
        var techManager = _state.GetOrCreateTechManager(cmd.FactionId);

        if (!techManager.CanResearch(tech, eraState.CurrentEra, _state.ActiveBuildings, out string reason))
        {
            SimLogger.LogDebug("Simulation", $"Cannot research {tech.DisplayName}: {reason}");
            return;
        }

        // Check if already queued in this or another building
        foreach (var b in _state.ActiveBuildings)
        {
            if (b.FactionId == cmd.FactionId)
            {
                foreach (var item in b.ResearchQueue.Items)
                {
                    if (item.TechnologyId.Equals(tech.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        SimLogger.LogDebug("Simulation", $"Technology {tech.DisplayName} is already queued.");
                        return;
                    }
                }
            }
        }

        var bank = _state.GetOrCreateResourceBank(cmd.FactionId);
        if (!bank.TryDeduct(tech.Cost, tick, _eventBus, $"Research {tech.DisplayName}"))
        {
            SimLogger.LogDebug("Simulation", $"Cannot research {tech.DisplayName}: Insufficient resources.");
            return;
        }

        var queueItem = new ResearchQueueItem(tech, tech.ResearchDurationTicks, tech.Cost);
        if (building.ResearchQueue.TryEnqueue(queueItem))
        {
            _eventBus.Publish(new TechnologyResearchStartedEvent(
                tick,
                cmd.FactionId,
                building.Id,
                tech.Id,
                tech.ResearchDurationTicks));
            SimLogger.LogInfo("Simulation", $"Faction {cmd.FactionId} started research on {tech.DisplayName} at building {building.Id}.");
        }
    }

    private void ExecuteCancelResearch(CancelResearchCommand cmd, ulong tick)
    {
        if (!_state.TryGetBuilding(cmd.BuildingId, out var building) || building == null || building.FactionId != cmd.FactionId)
        {
            return;
        }

        var item = building.ResearchQueue.CancelAt(cmd.QueueIndex);
        if (item != null)
        {
            var bank = _state.GetOrCreateResourceBank(cmd.FactionId);
            if (item.Cost.Food > 0) bank.Deposit(ResourceType.Food, item.Cost.Food, tick, _eventBus);
            if (item.Cost.Wood > 0) bank.Deposit(ResourceType.Wood, item.Cost.Wood, tick, _eventBus);
            if (item.Cost.Gold > 0) bank.Deposit(ResourceType.Gold, item.Cost.Gold, tick, _eventBus);
            if (item.Cost.Stone > 0) bank.Deposit(ResourceType.Stone, item.Cost.Stone, tick, _eventBus);
            if (item.Cost.Iron > 0) bank.Deposit(ResourceType.Iron, item.Cost.Iron, tick, _eventBus);

            _eventBus.Publish(new TechnologyResearchCancelledEvent(
                tick,
                cmd.FactionId,
                building.Id,
                item.TechnologyId,
                item.Cost));
        }
    }

    private void ExecuteAttachToHero(AttachToHeroCommand attach, ulong tick)
    {
        if (!_state.TryGetUnit(attach.HeroId, out var heroUnit) || heroUnit == null || !heroUnit.IsAlive || heroUnit.HeroState == null || heroUnit.FactionId != attach.FactionId)
        {
            return;
        }

        int attachedCount = 0;
        for (int i = 0; i < attach.UnitIds.Length; i++)
        {
            var unitId = attach.UnitIds[i];
            if (unitId == attach.HeroId) continue;

            if (_state.TryGetUnit(unitId, out var unit) && unit != null && unit.IsAlive && unit.FactionId == attach.FactionId)
            {
                if (heroUnit.HeroState.AttachUnit(unitId))
                {
                    attachedCount++;
                }
            }
        }

        _eventBus.Publish(new HeroAttachedUnitsChangedEvent(
            tick,
            attach.HeroId,
            attach.FactionId,
            heroUnit.HeroState.AttachedUnitIds.Count,
            heroUnit.HeroState.LeadershipCapacity));
    }

    private void ExecuteDetachFromHero(DetachFromHeroCommand detach, ulong tick)
    {
        if (!_state.TryGetUnit(detach.HeroId, out var heroUnit) || heroUnit == null || heroUnit.HeroState == null || heroUnit.FactionId != detach.FactionId)
        {
            return;
        }

        for (int i = 0; i < detach.UnitIds.Length; i++)
        {
            heroUnit.HeroState.DetachUnit(detach.UnitIds[i]);
        }

        _eventBus.Publish(new HeroAttachedUnitsChangedEvent(
            tick,
            detach.HeroId,
            detach.FactionId,
            heroUnit.HeroState.AttachedUnitIds.Count,
            heroUnit.HeroState.LeadershipCapacity));
    }

    private void ExecuteCastHeroAbility(CastHeroAbilityCommand cast, ulong tick)
    {
        if (!_state.TryGetUnit(cast.HeroId, out var heroUnit) || heroUnit == null || !heroUnit.IsAlive || heroUnit.HeroState == null || heroUnit.FactionId != cast.FactionId)
        {
            return;
        }

        if (!heroUnit.HeroState.TryGetAbility(cast.AbilityId, out var ability) || ability == null || !ability.IsReady)
        {
            return;
        }

        if (heroUnit.HeroState.CurrentMana < ability.Definition.ManaCost)
        {
            return;
        }

        // Range checks
        Vector2D targetPos = cast.TargetPosition;
        if (cast.TargetEntityId.IsValid && _state.TryGetUnit(cast.TargetEntityId, out var targetEntity) && targetEntity != null)
        {
            targetPos = targetEntity.Position;
        }

        if (ability.Definition.CastRange > 0f)
        {
            if (heroUnit.Position.DistanceTo(targetPos) > ability.Definition.CastRange + 0.5f)
            {
                return;
            }
        }

        // Deduct mana & trigger cooldown
        heroUnit.HeroState.ConsumeMana(ability.Definition.ManaCost);
        ability.TriggerCooldown();

        // Execute effect
        switch (ability.Definition.EffectType)
        {
            case AbilityEffectType.Damage:
            {
                if (ability.Definition.TargetType == AbilityTargetType.SingleTargetEnemy)
                {
                    if (cast.TargetEntityId.IsValid && _state.TryGetUnit(cast.TargetEntityId, out var enemy) && enemy != null && enemy.IsAlive && enemy.FactionId != heroUnit.FactionId)
                    {
                        float spellDmg = CombatFormulas.CalculateHeroSpellDamage(
                            ability.Definition.BasePower,
                            heroUnit.HeroState.AbilityPotencyMultiplier,
                            enemy.Armor);

                        enemy.TakeCombatDamage(spellDmg, heroUnit.Id, heroUnit.FactionId, tick, _eventBus, out bool killed);
                        if (killed)
                        {
                            AwardKillXpToHero(heroUnit, enemy.KillXpValue, tick);
                        }
                    }
                }
                else
                {
                    // PointAreaEnemy or area damage
                    Vector2D center = ability.Definition.CastRange > 0f && targetPos != Vector2D.Zero ? targetPos : heroUnit.Position;
                    float radius = MathF.Max(1.0f, ability.Definition.Radius);

                    var activeUnits = _state.ActiveUnits;
                    for (int i = 0; i < activeUnits.Count; i++)
                    {
                        var enemy = activeUnits[i];
                        if (enemy.IsAlive && enemy.FactionId != heroUnit.FactionId)
                        {
                            float dist = enemy.Position.DistanceTo(center);
                            if (dist <= radius + 0.5f)
                            {
                                float spellDmg = CombatFormulas.CalculateHeroSpellDamage(
                                    ability.Definition.BasePower,
                                    heroUnit.HeroState.AbilityPotencyMultiplier,
                                    enemy.Armor);

                                enemy.TakeCombatDamage(spellDmg, heroUnit.Id, heroUnit.FactionId, tick, _eventBus, out bool killed);
                                if (killed)
                                {
                                    AwardKillXpToHero(heroUnit, enemy.KillXpValue, tick);
                                }
                            }
                        }
                    }
                }
                break;
            }

            case AbilityEffectType.Heal:
            {
                Vector2D center = ability.Definition.CastRange > 0f && targetPos != Vector2D.Zero ? targetPos : heroUnit.Position;
                float radius = MathF.Max(1.0f, ability.Definition.Radius);
                float healAmount = ability.Definition.BasePower * heroUnit.HeroState.AbilityPotencyMultiplier;

                var activeUnits = _state.ActiveUnits;
                for (int i = 0; i < activeUnits.Count; i++)
                {
                    var ally = activeUnits[i];
                    if (ally.IsAlive && ally.FactionId == heroUnit.FactionId)
                    {
                        float dist = ally.Position.DistanceTo(center);
                        if (dist <= radius + 0.5f)
                        {
                            ally.Heal(healAmount);
                        }
                    }
                }
                break;
            }


            case AbilityEffectType.Buff:
            case AbilityEffectType.Stun:
            {
                break;
            }
        }

        _eventBus.Publish(new HeroAbilityCastEvent(
            tick,
            heroUnit.Id,
            heroUnit.FactionId,
            cast.AbilityId,
            cast.TargetEntityId,
            targetPos,
            ability.Definition.ManaCost));
    }

    private void ExecuteAllocateHeroAttribute(AllocateHeroAttributeCommand alloc, ulong tick)
    {
        if (!_state.TryGetUnit(alloc.HeroId, out var heroUnit) || heroUnit == null || heroUnit.HeroState == null || heroUnit.FactionId != alloc.FactionId)
        {
            return;
        }

        if (heroUnit.HeroState.AllocateAttribute(alloc.AttributeName))
        {
            _eventBus.Publish(new HeroAttributeAllocatedEvent(
                tick,
                alloc.HeroId,
                alloc.FactionId,
                alloc.AttributeName,
                heroUnit.HeroState.TotalAttributes));
        }
    }

    public void AwardKillXpToHero(UnitEntity heroUnit, int xpAmount, ulong tick)
    {
        if (!heroUnit.IsAlive || heroUnit.HeroState == null) return;

        int oldLevel = heroUnit.Veterancy.Level;
        heroUnit.Veterancy.AwardXp(xpAmount, tick, _eventBus, out bool leveledUp, out _);

        if (leveledUp)
        {
            int levelsGained = heroUnit.Veterancy.Level - oldLevel;
            float healthBonus = levelsGained * heroUnit.HealthPerLevelBonus;
            heroUnit.ApplyLevelUpBonus(healthBonus);

            _eventBus.Publish(new HeroLevelUpEvent(
                tick,
                heroUnit.Id,
                heroUnit.FactionId,
                oldLevel,
                heroUnit.Veterancy.Level,
                heroUnit.HeroState.TotalAttributes));
        }
    }

    public void GetUnitAuraModifiers(UnitEntity unit, out float damageBonus, out float armorBonus, out float speedBonus)
    {
        damageBonus = 0f;
        armorBonus = 0f;
        speedBonus = 0f;

        if (!unit.IsAlive) return;

        var units = _state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var heroUnit = units[i];
            if (heroUnit.FactionId == unit.FactionId && heroUnit.IsAlive && heroUnit.HeroState?.ActiveAura != null)
            {
                var aura = heroUnit.HeroState.ActiveAura;
                bool isAttached = heroUnit.HeroState.AttachedUnitIds.Contains(unit.Id);
                bool isSelf = heroUnit.Id == unit.Id;

                if (isAttached || isSelf)
                {
                    float distSq = unit.Position.DistanceSquaredTo(heroUnit.Position);
                    if (distSq <= aura.Radius * aura.Radius)
                    {
                        damageBonus = MathF.Max(damageBonus, aura.DamageMultiplierBonus);
                        armorBonus = MathF.Max(armorBonus, aura.ArmorBonus);
                        speedBonus = MathF.Max(speedBonus, aura.MovementSpeedMultiplierBonus);
                    }
                }
            }
        }
    }

    private void UpdateHeroes(ulong tick)
    {
        var units = _state.ActiveUnits;
        int count = units.Count;
        for (int i = 0; i < count; i++)
        {
            var unit = units[i];
            if (unit.IsAlive && unit.HeroState != null)
            {
                unit.HeroState.RegenerateMana();
                unit.HeroState.TickCooldowns();
            }
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
            var tech = _state.GetOrCreateTechManager(unit.FactionId).Modifiers;

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
                        // Target node/farm is depleted or missing -> search for nearby matching resource
                        var nearestTarget = FindNearestGatherTarget(unit.Position, worker.CarriedResourceType, unit.FactionId);
                        if (nearestTarget.IsValid)
                        {
                            worker.TargetResourceNodeId = nearestTarget;
                        }
                        else
                        {
                            unit.Stop();
                        }
                    }
                    break;
                }

                case WorkerTaskState.Harvesting:
                {
                    if (_state.TryGetResourceNode(worker.TargetResourceNodeId, out var node) && node != null && !node.IsDepleted)
                    {
                        float dist = unit.Position.DistanceTo(node.Position);
                        if (dist > node.HarvestRadius + 0.5f)
                        {
                            worker.TaskState = WorkerTaskState.MovingToResource;
                            break;
                        }

                        // Apply tech gather rate modifier
                        worker.HarvestProgressAccumulator += worker.HarvestRatePerTick * (1.0f + tech.GatherRateBonus);
                        if (worker.HarvestProgressAccumulator >= 1.0f)
                        {
                            int toHarvest = (int)worker.HarvestProgressAccumulator;
                            worker.HarvestProgressAccumulator -= toHarvest;
                            int harvested = node.Harvest(toHarvest, tick, unit.Id, _eventBus);
                            if (harvested > 0)
                            {
                                worker.AddCarried(node.ResourceType, harvested);
                            }
                        }

                        if (worker.IsInventoryFull)
                        {
                            var dropOff = FindNearestDropOff(unit.FactionId, unit.Position, node.ResourceType);
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
                    }
                    else if (_state.TryGetBuilding(worker.TargetResourceNodeId, out var farm) && farm != null && farm.IsFarm && farm.IsAlive && !farm.IsFarmDepleted)
                    {
                        float farmRadius = MathF.Max(farm.GridSize.X, farm.GridSize.Y) * 0.5f + 1.5f;
                        float dist = unit.Position.DistanceTo(farm.Position);
                        if (dist > farmRadius)
                        {
                            worker.TaskState = WorkerTaskState.MovingToResource;
                            break;
                        }

                        worker.HarvestProgressAccumulator += worker.HarvestRatePerTick * (1.0f + tech.GatherRateBonus);
                        if (worker.HarvestProgressAccumulator >= 1.0f)
                        {
                            int toHarvest = (int)worker.HarvestProgressAccumulator;
                            worker.HarvestProgressAccumulator -= toHarvest;
                            int harvested = farm.HarvestFarmFood(toHarvest, tick, unit.Id, _eventBus);
                            if (harvested > 0)
                            {
                                worker.AddCarried(ResourceType.Food, harvested);
                                _eventBus.Publish(new FarmHarvestedEvent(tick, unit.Id, farm.Id, harvested, farm.FarmFoodRemaining));
                            }
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
                                unit.Stop();
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
                                unit.Stop();
                            }
                        }
                        else
                        {
                            var nextTarget = FindNearestGatherTarget(unit.Position, worker.CarriedResourceType, unit.FactionId);
                            if (nextTarget.IsValid)
                            {
                                worker.TargetResourceNodeId = nextTarget;
                                worker.TaskState = WorkerTaskState.MovingToResource;
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
                    if (_state.TryGetBuilding(worker.TargetBuildingId, out var dropOff) && dropOff != null && dropOff.IsAlive && dropOff.IsConstructed)
                    {
                        float dropOffRadius = MathF.Max(dropOff.GridSize.X, dropOff.GridSize.Y) * 0.5f + 1.2f;
                        float dist = unit.Position.DistanceTo(dropOff.Position);

                        if (dist <= dropOffRadius)
                        {
                            if (worker.HasCarriedResources && worker.CarriedResourceType.HasValue)
                            {
                                var resType = worker.CarriedResourceType.Value;
                                int amount = worker.CarriedAmount;
                                var bank = _state.GetOrCreateResourceBank(unit.FactionId);
                                bank.Deposit(resType, amount, tick, _eventBus);

                                _eventBus.Publish(new ResourceDepositedEvent(
                                    tick,
                                    unit.FactionId,
                                    unit.Id,
                                    resType,
                                    amount,
                                    bank.GetAmount(resType)));

                                worker.EmptyInventory();
                            }

                            // Return to gathering
                            if (worker.TargetResourceNodeId.IsValid)
                            {
                                worker.TaskState = WorkerTaskState.MovingToResource;
                                unit.State = UnitState.Gathering;
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
        var tech = _state.GetOrCreateTechManager(unit.FactionId).Modifiers;
        float effectiveSpeed = unit.MovementSpeed + (unit.Archetype == UnitArchetype.Cavalry ? tech.CavalrySpeedBonus : 0f);
        float maxDist = effectiveSpeed * dt;
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
                var lower = item.UnitType.ToLowerInvariant();
                if (lower.Contains("villager") || lower.Contains("worker") || lower.Contains("plebeian"))
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
                else if (lower.Contains("archer") || lower.Contains("sagittarius") || lower.Contains("bowman") || lower.Contains("veles"))
                {
                    producedUnit = new UnitEntity(
                        unitId,
                        building.FactionId,
                        item.UnitType,
                        spawnPos,
                        maxHealth: 80f,
                        attackDamage: 14f,
                        attackRange: 8.0f,
                        movementSpeed: 3.8f,
                        attackCooldownTicks: 22,
                        killXpValue: 50,
                        baseArmor: 1.0f,
                        attackType: "ranged",
                        aggroRange: 12.0f,
                        archetype: UnitArchetype.Archer);
                }
                else if (lower.Contains("spearman") || lower.Contains("triarius") || lower.Contains("hoplite"))
                {
                    producedUnit = new UnitEntity(
                        unitId,
                        building.FactionId,
                        item.UnitType,
                        spawnPos,
                        maxHealth: 100f,
                        attackDamage: 12f,
                        attackRange: 1.6f,
                        movementSpeed: 3.7f,
                        attackCooldownTicks: 18,
                        killXpValue: 50,
                        baseArmor: 2.0f,
                        attackType: "melee",
                        aggroRange: 10.0f,
                        archetype: UnitArchetype.Spearman);
                }
                else if (lower.Contains("cavalry") || lower.Contains("scout") || lower.Contains("equite") || lower.Contains("horseman"))
                {
                    bool isHeavy = lower.Contains("heavy");
                    producedUnit = new UnitEntity(
                        unitId,
                        building.FactionId,
                        item.UnitType,
                        spawnPos,
                        maxHealth: isHeavy ? 180f : 110f,
                        attackDamage: isHeavy ? 22f : 12f,
                        attackRange: 1.5f,
                        movementSpeed: isHeavy ? 4.8f : 5.5f,
                        attackCooldownTicks: isHeavy ? 20 : 16,
                        killXpValue: isHeavy ? 100 : 65,
                        baseArmor: isHeavy ? 5.0f : 2.0f,
                        attackType: "melee",
                        aggroRange: 14.0f,
                        archetype: UnitArchetype.Cavalry);
                }
                else if (lower.Contains("legionary"))
                {
                    producedUnit = new UnitEntity(
                        unitId,
                        building.FactionId,
                        item.UnitType,
                        spawnPos,
                        maxHealth: 140f,
                        attackDamage: 16f,
                        attackRange: 1.5f,
                        movementSpeed: 3.2f,
                        attackCooldownTicks: 20,
                        killXpValue: 70,
                        baseArmor: 5.0f,
                        archetype: UnitArchetype.Infantry);
                }
                else if (lower.Contains("ram"))
                {
                    producedUnit = new UnitEntity(
                        unitId,
                        building.FactionId,
                        item.UnitType,
                        spawnPos,
                        maxHealth: 250f,
                        attackDamage: 40f,
                        attackRange: 1.8f,
                        movementSpeed: 1.8f,
                        attackCooldownTicks: 30,
                        killXpValue: 80,
                        baseArmor: 8.0f,
                        attackType: "melee",
                        aggroRange: 8.0f,
                        archetype: UnitArchetype.Siege);
                }
                else if (lower.Contains("catapult") || lower.Contains("onager"))
                {
                    producedUnit = new UnitEntity(
                        unitId,
                        building.FactionId,
                        item.UnitType,
                        spawnPos,
                        maxHealth: 150f,
                        attackDamage: 35f,
                        attackRange: 12.0f,
                        movementSpeed: 2.2f,
                        attackCooldownTicks: 40,
                        killXpValue: 100,
                        baseArmor: 2.0f,
                        attackType: "ranged",
                        aggroRange: 14.0f,
                        archetype: UnitArchetype.Siege);
                }
                else if (lower.Contains("ballista") || lower.Contains("scorpion"))
                {
                    producedUnit = new UnitEntity(
                        unitId,
                        building.FactionId,
                        item.UnitType,
                        spawnPos,
                        maxHealth: 140f,
                        attackDamage: 45f,
                        attackRange: 10.0f,
                        movementSpeed: 2.6f,
                        attackCooldownTicks: 25,
                        killXpValue: 85,
                        baseArmor: 2.0f,
                        attackType: "ranged",
                        aggroRange: 12.0f,
                        archetype: UnitArchetype.Siege);
                }
                else
                {
                    // Default Swordsman
                    producedUnit = new UnitEntity(
                        unitId,
                        building.FactionId,
                        item.UnitType,
                        spawnPos,
                        maxHealth: 120f,
                        attackDamage: 18f,
                        attackRange: 1.5f,
                        movementSpeed: 3.6f,
                        attackCooldownTicks: 18,
                        killXpValue: 60,
                        baseArmor: 3.0f,
                        archetype: UnitArchetype.Infantry);
                }

                _state.AddUnit(producedUnit);
                _spatialGrid.Insert(producedUnit.Id, producedUnit.Position);

                _eventBus.Publish(new UnitSpawnedEvent(tick, unitId, building.FactionId, item.UnitType, producedUnit.Position));
                _eventBus.Publish(new ProductionCompletedEvent(tick, building.Id, building.FactionId, item.UnitType, unitId));
                SimLogger.LogInfo("Production", $"Trained unit {item.UnitType} {unitId} at {spawnPos}.");
            }
        }
    }

    private void UpdateResearch(ulong tick)
    {
        var buildings = _state.ActiveBuildings;
        int count = buildings.Count;

        for (int i = 0; i < count; i++)
        {
            var b = buildings[i];
            if (!b.IsAlive || !b.IsConstructed || b.ResearchQueue.IsEmpty) continue;

            var item = b.ResearchQueue.CurrentItem;
            if (item == null) continue;

            item.AdvanceTicks(1);
            _eventBus.Publish(new TechnologyResearchProgressEvent(
                tick,
                b.FactionId,
                b.Id,
                item.TechnologyId,
                item.ProgressTicks,
                item.TotalDurationTicks));

            if (item.IsCompleted)
            {
                b.ResearchQueue.TryDequeue();
                var techManager = _state.GetOrCreateTechManager(b.FactionId);
                techManager.TryUnlockTechnology(item.Technology, b.Id, tick, _eventBus);
                SimLogger.LogInfo("Simulation", $"Faction {b.FactionId} completed research: {item.Technology.DisplayName}");
            }
        }
    }

    private void UpdateEraAdvancement(ulong tick)
    {
        foreach (var (factionId, eraState) in _state.EraStates)
        {
            if (eraState.IsAdvancing)
            {
                eraState.AdvanceTicks(1, tick, _eventBus, out bool completed);
                if (completed)
                {
                    SimLogger.LogInfo("Simulation", $"Faction {factionId} reached {eraState.CurrentEra.GetDisplayName()}!");
                }
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

            unit.CurrentTerrain = _state.TerrainGrid.GetTerrainAt(unit.Position);

            if ((unit.State == UnitState.Moving || unit.State == UnitState.Routed) && unit.MoveTarget.HasValue)
            {
                var prevPos = unit.Position;
                var tech = _state.GetOrCreateTechManager(unit.FactionId).Modifiers;
                float effectiveSpeed = unit.MovementSpeed + (unit.Archetype == UnitArchetype.Cavalry ? tech.CavalrySpeedBonus : 0f);
                float maxDistance = effectiveSpeed * dt;
                var target = _bounds.Clamp(unit.MoveTarget.Value);
                var dir = target - unit.Position;
                if (dir.LengthSquared > 0.01f)
                {
                    unit.HeadingDirection = dir.Normalized();
                }

                if (unit.Archetype == UnitArchetype.Cavalry && unit.State == UnitState.Moving)
                {
                    unit.Charge.IncrementMomentum();
                }
                else if (unit.State != UnitState.Moving)
                {
                    unit.Charge.Reset();
                }

                var nextPos = unit.Position.MoveTowards(target, maxDistance);
                nextPos = _bounds.Clamp(nextPos);

                unit.Position = nextPos;
                _spatialGrid.UpdatePosition(unit.Id, prevPos, unit.Position);

                if (unit.Position.DistanceSquaredTo(target) < 1e-4f)
                {
                    unit.Position = target;
                    unit.MoveTarget = null;
                    if (unit.State == UnitState.Moving)
                    {
                        unit.State = UnitState.Idle;
                    }
                    unit.Charge.Reset();
                }

                _eventBus.Publish(new UnitMovedEvent(tick, unit.Id, prevPos, unit.Position));
            }
            else if (unit.State == UnitState.Idle)
            {
                unit.Charge.Reset();
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
            if (!unit.IsAlive || unit.IsRouted) continue;

            unit.DecrementCooldown();

            if (unit.State == UnitState.Attacking && unit.AttackTargetId.IsValid)
            {
                if (_state.TryGetUnit(unit.AttackTargetId, out var target) && target != null && target.IsAlive)
                {
                    var attackerTech = _state.GetOrCreateTechManager(unit.FactionId).Modifiers;
                    var targetTech = _state.GetOrCreateTechManager(target.FactionId).Modifiers;

                    float rangeBonus = unit.Archetype == UnitArchetype.Archer ? attackerTech.RangedRangeBonus : 0f;
                    int attackerElevation = unit.TerrainModifiers.ElevationLevel;
                    int targetElevation = target.TerrainModifiers.ElevationLevel;

                    bool isCatapult = unit.UnitType.Contains("catapult", StringComparison.OrdinalIgnoreCase) || unit.UnitType.Contains("onager", StringComparison.OrdinalIgnoreCase);
                    float minRange = isCatapult ? CombatFormulas.CatapultMinRange : 0f;
                    float maxRange = unit.AttackRange;

                    bool inRange = isCatapult
                        ? CombatFormulas.IsSiegeInRange(unit.Position, target.Position, minRange, maxRange, rangeBonus, attackerElevation, targetElevation)
                        : CombatFormulas.IsInRange(unit.Position, target.Position, unit.AttackRange, rangeBonus, attackerElevation, targetElevation);

                    if (!inRange)
                    {
                        var tech = _state.GetOrCreateTechManager(unit.FactionId).Modifiers;
                        float effectiveSpeed = unit.MovementSpeed + (unit.Archetype == UnitArchetype.Cavalry ? tech.CavalrySpeedBonus : 0f);
                        float maxDistance = effectiveSpeed * dt;
                        var prevPos = unit.Position;
                        var dir = target.Position - unit.Position;
                        if (dir.LengthSquared > 0.01f)
                        {
                            unit.HeadingDirection = dir.Normalized();
                        }

                        if (unit.Archetype == UnitArchetype.Cavalry)
                        {
                            unit.Charge.IncrementMomentum();
                        }

                        var nextPos = unit.Position.MoveTowards(target.Position, maxDistance);
                        nextPos = _bounds.Clamp(nextPos);

                        unit.Position = nextPos;
                        _spatialGrid.UpdatePosition(unit.Id, prevPos, unit.Position);
                        _eventBus.Publish(new UnitMovedEvent(tick, unit.Id, prevPos, unit.Position));
                    }
                    else if (unit.CooldownRemaining <= 0)
                    {
                        unit.ResetCooldown();

                        GetUnitAuraModifiers(unit, out float attackerAuraDmg, out _, out _);
                        GetUnitAuraModifiers(target, out _, out float targetAuraArmor, out _);

                        bool isCharging = unit.Archetype == UnitArchetype.Cavalry && unit.Charge.IsCharging;
                        bool isRanged = unit.AttackType.Equals("ranged", StringComparison.OrdinalIgnoreCase) || unit.Archetype == UnitArchetype.Archer || unit.Archetype == UnitArchetype.Siege;

                        bool isFlanking = CombatFormulas.IsFlankingAttack(unit.Position, target.Position, target.HeadingDirection);
                        if (isFlanking && target.Formation != FormationType.Square)
                        {
                            target.Morale.ApplyShock(15.0f);
                        }

                        var (calculatedDamage, chargeBlocked, recoilDamage) = CombatFormulas.CalculateTacticalCombatDamage(
                            unit.Archetype,
                            unit.AttackDamage,
                            attackerTech,
                            attackerAuraDmg,
                            unit.FormationModifiers,
                            unit.Morale.Level,
                            unit.TerrainModifiers,
                            isCharging,
                            isRanged,
                            target.Archetype,
                            target.Armor,
                            targetTech,
                            targetAuraArmor,
                            target.FormationModifiers,
                            target.Morale.Level,
                            target.TerrainModifiers);

                        bool isBallista = unit.UnitType.Contains("ballista", StringComparison.OrdinalIgnoreCase) || unit.UnitType.Contains("scorpion", StringComparison.OrdinalIgnoreCase);
                        if (isBallista)
                        {
                            calculatedDamage = CombatFormulas.CalculateArmorPiercingDamage(unit.AttackDamage, target.Armor, CombatFormulas.BallistaArmorPenetration);
                        }

                        if (isCharging)
                        {
                            unit.Charge.Discharge();
                            _eventBus.Publish(new CavalryChargeImpactEvent(
                                tick,
                                unit.Id,
                                target.Id,
                                calculatedDamage,
                                chargeBlocked,
                                recoilDamage));

                            if (!chargeBlocked)
                            {
                                target.Morale.ApplyShock(ChargeState.ChargeMoraleShock);
                            }

                            if (chargeBlocked && recoilDamage > 0f)
                            {
                                unit.TakeRecoilDamage(recoilDamage, target.Id, tick, _eventBus, out _);
                            }
                        }

                        target.TakeCombatDamage(calculatedDamage, unit.Id, unit.FactionId, tick, _eventBus, out bool killed);

                        if (isCatapult)
                        {
                            float splashRadius = CombatFormulas.CatapultSplashRadius;
                            float splashRadiusSq = splashRadius * splashRadius;
                            int targetsHit = 1;
                            float totalSplashDmg = calculatedDamage;

                            var unitsList = _state.ActiveUnits;
                            for (int u = 0; u < unitsList.Count; u++)
                            {
                                var splashUnit = unitsList[u];
                                if (splashUnit.FactionId != unit.FactionId && splashUnit.IsAlive && splashUnit.Id != target.Id)
                                {
                                    float distSq = target.Position.DistanceSquaredTo(splashUnit.Position);
                                    if (distSq <= splashRadiusSq)
                                    {
                                        float dist = MathF.Sqrt(distSq);
                                        float splashUnitDmg = CombatFormulas.CalculateAreaOfEffectDamage(unit.AttackDamage, dist, splashRadius);
                                        splashUnit.TakeCombatDamage(splashUnitDmg, unit.Id, unit.FactionId, tick, _eventBus, out bool killedSplash);
                                        targetsHit++;
                                        totalSplashDmg += splashUnitDmg;
                                        if (killedSplash)
                                        {
                                            ApplyNearbyCasualtyMoraleShock(splashUnit, tick);
                                        }
                                    }
                                }
                            }

                            _eventBus.Publish(new SiegeAreaOfEffectImpactEvent(tick, unit.Id, unit.FactionId, target.Position, splashRadius, targetsHit, totalSplashDmg));
                        }

                        if (killed)
                        {
                            ApplyNearbyCasualtyMoraleShock(target, tick);

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

                                    if (unit.IsHero && unit.HeroState != null)
                                    {
                                        _eventBus.Publish(new HeroLevelUpEvent(
                                            tick,
                                            unit.Id,
                                            unit.FactionId,
                                            oldLevel,
                                            unit.Veterancy.Level,
                                            unit.HeroState.TotalAttributes));
                                    }
                                }

                                // Shared squad XP to attached hero
                                if (!unit.IsHero)
                                {
                                    var unitsList = _state.ActiveUnits;
                                    for (int h = 0; h < unitsList.Count; h++)
                                    {
                                        var potentialHero = unitsList[h];
                                        if (potentialHero.FactionId == unit.FactionId && potentialHero.IsHero && potentialHero.HeroState != null && potentialHero.HeroState.AttachedUnitIds.Contains(unit.Id))
                                        {
                                            int sharedXp = Math.Max(10, target.KillXpValue / 2);
                                            AwardKillXpToHero(potentialHero, sharedXp, tick);
                                            break;
                                        }
                                    }
                                }

                                SimLogger.LogInfo("Combat", $"Unit {unit.Id} killed {target.Id}. Awarded {target.KillXpValue} XP. Level={unit.Veterancy.Level} ({unit.Veterancy.Rank.GetDisplayName()})");
                            }

                            unit.AttackTargetId = EntityId.None;
                            unit.State = UnitState.Idle;
                        }
                    }
                }
                else if (_state.TryGetBuilding(unit.AttackTargetId, out var targetBuilding) && targetBuilding != null && targetBuilding.IsAlive)
                {
                    var attackerTech = _state.GetOrCreateTechManager(unit.FactionId).Modifiers;
                    bool isCatapult = unit.UnitType.Contains("catapult", StringComparison.OrdinalIgnoreCase) || unit.UnitType.Contains("onager", StringComparison.OrdinalIgnoreCase);

                    float minRange = isCatapult ? CombatFormulas.CatapultMinRange : 0f;
                    float maxRange = unit.AttackRange;

                    bool inRange = isCatapult
                        ? CombatFormulas.IsSiegeInRange(unit.Position, targetBuilding.Position, minRange, maxRange)
                        : CombatFormulas.IsInRange(unit.Position, targetBuilding.Position, maxRange);

                    if (!inRange)
                    {
                        var tech = _state.GetOrCreateTechManager(unit.FactionId).Modifiers;
                        float effectiveSpeed = unit.MovementSpeed + (unit.Archetype == UnitArchetype.Cavalry ? tech.CavalrySpeedBonus : 0f);
                        float maxDistance = effectiveSpeed * dt;
                        var prevPos = unit.Position;
                        var dir = targetBuilding.Position - unit.Position;
                        if (dir.LengthSquared > 0.01f)
                        {
                            unit.HeadingDirection = dir.Normalized();
                        }

                        var nextPos = unit.Position.MoveTowards(targetBuilding.Position, maxDistance);
                        nextPos = _bounds.Clamp(nextPos);

                        unit.Position = nextPos;
                        _spatialGrid.UpdatePosition(unit.Id, prevPos, unit.Position);
                        _eventBus.Publish(new UnitMovedEvent(tick, unit.Id, prevPos, unit.Position));
                    }
                    else if (unit.CooldownRemaining <= 0)
                    {
                        unit.ResetCooldown();

                        float structuralDmg = CombatFormulas.CalculateStructuralCombatDamage(
                            unit.Archetype,
                            unit.UnitType,
                            unit.AttackDamage,
                            attackerTech);

                        targetBuilding.TakeDamage(structuralDmg, unit.Id, unit.FactionId, tick, _eventBus, out bool destroyed);
                        _eventBus.Publish(new BuildingAttackedEvent(tick, unit.Id, unit.FactionId, targetBuilding.Id, targetBuilding.FactionId, structuralDmg, targetBuilding.CurrentHealth));

                        if (isCatapult)
                        {
                            float splashRadius = CombatFormulas.CatapultSplashRadius;
                            float splashRadiusSq = splashRadius * splashRadius;
                            int targetsHit = 1;
                            float totalSplashDmg = structuralDmg;

                            var unitsList = _state.ActiveUnits;
                            for (int u = 0; u < unitsList.Count; u++)
                            {
                                var splashUnit = unitsList[u];
                                if (splashUnit.FactionId != unit.FactionId && splashUnit.IsAlive)
                                {
                                    float distSq = targetBuilding.Position.DistanceSquaredTo(splashUnit.Position);
                                    if (distSq <= splashRadiusSq)
                                    {
                                        float dist = MathF.Sqrt(distSq);
                                        float splashUnitDmg = CombatFormulas.CalculateAreaOfEffectDamage(unit.AttackDamage, dist, splashRadius);
                                        splashUnit.TakeCombatDamage(splashUnitDmg, unit.Id, unit.FactionId, tick, _eventBus, out bool killedSplash);
                                        targetsHit++;
                                        totalSplashDmg += splashUnitDmg;
                                        if (killedSplash)
                                        {
                                            ApplyNearbyCasualtyMoraleShock(splashUnit, tick);
                                        }
                                    }
                                }
                            }

                            _eventBus.Publish(new SiegeAreaOfEffectImpactEvent(tick, unit.Id, unit.FactionId, targetBuilding.Position, splashRadius, targetsHit, totalSplashDmg));
                        }

                        if (destroyed)
                        {
                            if (targetBuilding.IsWall)
                            {
                                var (gx, gy) = _state.TerrainGrid.WorldToGrid(targetBuilding.Position);
                                _state.TerrainGrid.SetTerrain(gx, gy, TerrainType.Rubble);
                                _state.AddBreach(new BreachEntity(targetBuilding.Id, targetBuilding.FactionId, targetBuilding.Position, targetBuilding.BuildingType, tick));
                                _eventBus.Publish(new WallBreachedEvent(tick, targetBuilding.Id, targetBuilding.FactionId, targetBuilding.Position, targetBuilding.BuildingType));
                            }

                            unit.AttackTargetId = EntityId.None;
                            unit.State = UnitState.Idle;
                        }
                    }
                }
                else
                {
                    unit.AttackTargetId = EntityId.None;
                    unit.State = UnitState.Idle;
                    unit.Charge.Reset();
                }
            }
        }
    }

    private void UpdateTowers(ulong tick)
    {
        var buildings = _state.ActiveBuildings;
        int count = buildings.Count;

        for (int i = 0; i < count; i++)
        {
            var b = buildings[i];
            if (!b.IsAlive || !b.IsConstructed || !b.IsTower || b.TowerDefense == null) continue;

            var tower = b.TowerDefense;
            tower.DecrementCooldown();

            if (tower.CooldownRemaining <= 0)
            {
                UnitEntity? nearestEnemy = null;
                float nearestDistSq = float.MaxValue;
                float rangeSq = tower.AttackRange * tower.AttackRange;

                var units = _state.ActiveUnits;
                for (int u = 0; u < units.Count; u++)
                {
                    var unit = units[u];
                    if (unit.FactionId != b.FactionId && unit.IsAlive)
                    {
                        float distSq = b.Position.DistanceSquaredTo(unit.Position);
                        if (distSq <= rangeSq && distSq < nearestDistSq)
                        {
                            nearestDistSq = distSq;
                            nearestEnemy = unit;
                        }
                    }
                }

                if (nearestEnemy != null)
                {
                    tower.ResetCooldown();
                    float damage = tower.IsBallistaTower
                        ? CombatFormulas.CalculateArmorPiercingDamage(tower.EffectiveDamage, nearestEnemy.Armor, 0.60f)
                        : CombatFormulas.CalculateEffectiveDamage(tower.EffectiveDamage, nearestEnemy.Armor);

                    nearestEnemy.TakeCombatDamage(damage, b.Id, b.FactionId, tick, _eventBus, out bool killed);
                    _eventBus.Publish(new TowerAttackEvent(tick, b.Id, b.FactionId, nearestEnemy.Id, damage, nearestEnemy.Position));

                    if (killed)
                    {
                        ApplyNearbyCasualtyMoraleShock(nearestEnemy, tick);
                    }
                }
            }
        }
    }

    private void UpdateMorale(ulong tick)
    {
        var units = _state.ActiveUnits;
        int count = units.Count;

        for (int i = 0; i < count; i++)
        {
            var unit = units[i];
            if (!unit.IsAlive) continue;

            unit.CurrentTerrain = _state.TerrainGrid.GetTerrainAt(unit.Position);

            // 1. Routing evaluation
            if (unit.Morale.IsRouted && unit.State != UnitState.Routed)
            {
                Vector2D retreatTarget = FindSafeRetreatPoint(unit);
                unit.Route(retreatTarget);
                _eventBus.Publish(new UnitRoutedEvent(tick, unit.Id, unit.FactionId, unit.Position));
            }
            else if (unit.State == UnitState.Routed && unit.Morale.CurrentMorale >= 25.0f)
            {
                unit.Rally(0f); // Resets state to Idle if >= 25.0
                _eventBus.Publish(new UnitRalliedEvent(tick, unit.Id, unit.FactionId, unit.Morale.CurrentMorale));
            }

            // 2. Passive recovery when out of active combat engagement and not routed
            if (!unit.Morale.IsRouted && unit.State != UnitState.Attacking && unit.CooldownRemaining == 0)
            {
                unit.Morale.Recover(0.05f); // +1.0 per second at 20 ticks/sec
            }

            // 3. Hero Leadership Aura recovery (+0.15/tick -> +3.0/sec)
            bool nearHero = false;
            for (int h = 0; h < count; h++)
            {
                var hero = units[h];
                if (hero.FactionId == unit.FactionId && hero.IsHero && hero.IsAlive && hero.HeroState != null)
                {
                    float radius = hero.HeroState.ActiveAura?.Radius ?? 10.0f;
                    if (hero.HeroState.AttachedUnitIds.Contains(unit.Id) || hero.Position.DistanceSquaredTo(unit.Position) <= (radius * radius))
                    {
                        nearHero = true;
                        break;
                    }
                }
            }

            if (nearHero)
            {
                unit.Morale.Recover(0.15f);
            }
        }
    }

    private void ApplyNearbyCasualtyMoraleShock(UnitEntity casualty, ulong tick)
    {
        var units = _state.ActiveUnits;
        int count = units.Count;

        float casualtyRadiusSq = 8.0f * 8.0f;
        float heroCasualtyRadiusSq = 15.0f * 15.0f;

        for (int i = 0; i < count; i++)
        {
            var friendly = units[i];
            if (friendly.FactionId == casualty.FactionId && friendly.IsAlive && friendly.Id != casualty.Id)
            {
                float distSq = friendly.Position.DistanceSquaredTo(casualty.Position);
                if (distSq <= casualtyRadiusSq)
                {
                    friendly.Morale.ApplyShock(10.0f);
                }

                if (casualty.IsHero && distSq <= heroCasualtyRadiusSq)
                {
                    friendly.Morale.ApplyShock(30.0f);
                }
            }
        }
    }

    private Vector2D FindSafeRetreatPoint(UnitEntity unit)
    {
        var buildings = _state.ActiveBuildings;
        for (int b = 0; b < buildings.Count; b++)
        {
            var building = buildings[b];
            if (building.FactionId == unit.FactionId && building.IsAlive && building.BuildingType.Equals("town_center", StringComparison.OrdinalIgnoreCase))
            {
                return building.Position;
            }
        }

        if (unit.FactionId.Value == 2)
        {
            return new Vector2D(80.0f, 50.0f);
        }

        return new Vector2D(-25.0f, -25.0f);
    }

    private void CleanupEntities()
    {
        ulong tick = _state.CurrentTick;
        // 1. Dead units
        var units = _state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (!unit.IsAlive)
            {
                if (unit.IsHero && unit.HeroState != null)
                {
                    _eventBus.Publish(new HeroFallenEvent(tick, unit.Id, unit.FactionId, unit.Position));
                    unit.HeroState.ClearAttachedUnits();
                }

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

    public static (Vector2D GridSize, float MaxHealth, float BuildTimeTicks, int PopulationProvided, ResourceCost Cost, ResourceType[] AcceptedDropOffs) GetBuildingConfig(string buildingType)
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

            "blacksmith" => (
                new Vector2D(3f, 3f),
                750f,
                80f,
                0,
                new ResourceCost(Wood: 150, Iron: 50),
                Array.Empty<ResourceType>()),

            "archery_range" or "archery" => (
                new Vector2D(3f, 3f),
                700f,
                80f,
                0,
                new ResourceCost(Wood: 175),
                Array.Empty<ResourceType>()),

            "stable" or "stables" => (
                new Vector2D(3f, 3f),
                800f,
                90f,
                0,
                new ResourceCost(Wood: 175),
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

            "guard_tower" => (
                new Vector2D(2f, 2f),
                1000f,
                80f,
                0,
                new ResourceCost(Wood: 100, Stone: 200),
                Array.Empty<ResourceType>()),

            "ballista_tower" => (
                new Vector2D(2f, 2f),
                1400f,
                100f,
                0,
                new ResourceCost(Wood: 120, Stone: 250, Iron: 50),
                Array.Empty<ResourceType>()),

            "wooden_wall" => (
                new Vector2D(1f, 1f),
                500f,
                30f,
                0,
                new ResourceCost(Wood: 20),
                Array.Empty<ResourceType>()),

            "stone_wall" => (
                new Vector2D(1f, 1f),
                1200f,
                50f,
                0,
                new ResourceCost(Stone: 30),
                Array.Empty<ResourceType>()),

            "wooden_gate" => (
                new Vector2D(2f, 1f),
                800f,
                45f,
                0,
                new ResourceCost(Wood: 50),
                Array.Empty<ResourceType>()),

            "stone_gate" => (
                new Vector2D(2f, 1f),
                2000f,
                70f,
                0,
                new ResourceCost(Stone: 100, Iron: 25),
                Array.Empty<ResourceType>()),

            "siege_workshop" => (
                new Vector2D(3f, 3f),
                900f,
                100f,
                0,
                new ResourceCost(Wood: 200, Stone: 100, Iron: 50),
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

    public static (ResourceCost Cost, int DurationTicks, int PopulationCost) GetUnitProductionConfig(string unitType)
    {
        var lower = unitType.ToLowerInvariant();
        if (lower.Contains("villager") || lower.Contains("worker") || lower.Contains("plebeian"))
        {
            return (new ResourceCost(Food: 50), 50, 1);
        }
        if (lower.Contains("celtic_archer") || lower.Contains("roman_archer") || lower.Contains("archer") || lower.Contains("sagittarius") || lower.Contains("bowman"))
        {
            return (new ResourceCost(Food: 40, Wood: 35), 60, 1);
        }
        if (lower.Contains("spearman") || lower.Contains("triarius") || lower.Contains("hoplite"))
        {
            return (new ResourceCost(Food: 50, Gold: 25), 65, 1);
        }
        if (lower.Contains("heavy_cavalry") || lower.Contains("knight"))
        {
            return (new ResourceCost(Food: 90, Gold: 100), 110, 1);
        }
        if (lower.Contains("scout_cavalry") || lower.Contains("scout"))
        {
            return (new ResourceCost(Food: 75, Gold: 50), 80, 1);
        }
        if (lower.Contains("equite"))
        {
            return (new ResourceCost(Food: 80, Gold: 80), 100, 1);
        }
        if (lower.Contains("legionary"))
        {
            return (new ResourceCost(Food: 60, Iron: 25), 65, 1);
        }
        if (lower.Contains("veles"))
        {
            return (new ResourceCost(Food: 40, Gold: 40), 70, 1);
        }
        if (lower.Contains("ram"))
        {
            return (new ResourceCost(Wood: 150, Gold: 50), 100, 2);
        }
        if (lower.Contains("catapult") || lower.Contains("onager"))
        {
            return (new ResourceCost(Wood: 200, Gold: 100, Iron: 50), 120, 3);
        }
        if (lower.Contains("ballista") || lower.Contains("scorpion"))
        {
            return (new ResourceCost(Wood: 150, Gold: 80, Iron: 40), 100, 2);
        }

        // Default Swordsman
        return (new ResourceCost(Food: 60, Iron: 20), 60, 1);
    }

    public static (ResourceCost Cost, int DurationTicks, string[] RequiredBuildingTypes) GetEraConfig(CivilizationEra targetEra)
    {
        return targetEra switch
        {
            CivilizationEra.Classical => (
                new ResourceCost(Food: 500, Gold: 200),
                100,
                new[] { "town_center", "barracks" }),

            CivilizationEra.Imperial => (
                new ResourceCost(Food: 800, Gold: 500),
                150,
                new[] { "blacksmith", "stable" }),

            CivilizationEra.Feudal => (
                new ResourceCost(Food: 1000, Gold: 800),
                200,
                new[] { "town_center" }),

            _ => (ResourceCost.Zero, 0, Array.Empty<string>())
        };
    }
}
