using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Authoritative fresh settlement economy scenario demonstrating the full RTS economic gameplay loop:
/// Starting Town Center -> Resource gathering -> Building construction -> Population expansion -> Military unit training.
/// </summary>
public sealed class SettlementEconomyScenario
{
    private readonly GameCoordinator _coordinator;
    private readonly SelectionManager _selection;
    private readonly FactionId _factionId;

    public GameCoordinator Coordinator => _coordinator;
    public SelectionManager Selection => _selection;
    public FactionId PlayerFaction => _factionId;

    public EntityId TownCenterId { get; private set; }
    public List<EntityId> StartingVillagerIds { get; } = new(8);
    public List<EntityId> ForestTreeIds { get; } = new(8);
    public List<EntityId> BerryBushIds { get; } = new(8);
    public EntityId GoldMineId { get; private set; }
    public EntityId StoneQuarryId { get; private set; }
    public EntityId IronDepositId { get; private set; }

    public SettlementEconomyScenario(
        GameCoordinator? coordinator = null,
        FactionId? factionId = null)
    {
        _coordinator = coordinator ?? new GameCoordinator();
        _factionId = factionId ?? new FactionId(1); // Celtic Faction
        _selection = new SelectionManager(_coordinator, _factionId);

        InitializeSettlement();
    }

    private void InitializeSettlement()
    {
        var state = _coordinator.Simulation.State;
        ulong tick = _coordinator.CurrentTick;

        // 1. Initialize starting stockpile: 200 Food, 300 Wood, 100 Gold, 50 Stone, 50 Iron
        var bank = state.GetOrCreateResourceBank(_factionId);
        bank.Deposit(ResourceType.Food, 200, tick);
        bank.Deposit(ResourceType.Wood, 300, tick);
        bank.Deposit(ResourceType.Gold, 100, tick);
        bank.Deposit(ResourceType.Stone, 50, tick);
        bank.Deposit(ResourceType.Iron, 50, tick);

        // 2. Spawn constructed Town Center (4x4, +10 pop, accepts all 5 resources)
        TownCenterId = state.GenerateEntityId();
        var townCenter = new BuildingEntity(
            TownCenterId,
            _factionId,
            "town_center",
            new Vector2D(50f, 50f),
            new Vector2D(4f, 4f),
            maxHealth: 1200f,
            baseBuildTimeTicks: 200f,
            populationProvided: 10,
            acceptedDropOffTypes: new[] { ResourceType.Food, ResourceType.Wood, ResourceType.Gold, ResourceType.Stone, ResourceType.Iron },
            startsConstructed: true);
        state.AddBuilding(townCenter);

        // 3. Spawn 3 Starting Villagers
        var v1Id = state.GenerateEntityId();
        var v2Id = state.GenerateEntityId();
        var v3Id = state.GenerateEntityId();
        StartingVillagerIds.Add(v1Id);
        StartingVillagerIds.Add(v2Id);
        StartingVillagerIds.Add(v3Id);

        var v1 = new UnitEntity(v1Id, _factionId, "celtic_villager", new Vector2D(46f, 50f), maxHealth: 50f, attackDamage: 5f, movementSpeed: 3.5f, workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 0.5f, buildPowerPerTick: 1.0f));
        var v2 = new UnitEntity(v2Id, _factionId, "celtic_villager", new Vector2D(48f, 46f), maxHealth: 50f, attackDamage: 5f, movementSpeed: 3.5f, workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 0.5f, buildPowerPerTick: 1.0f));
        var v3 = new UnitEntity(v3Id, _factionId, "celtic_villager", new Vector2D(54f, 48f), maxHealth: 50f, attackDamage: 5f, movementSpeed: 3.5f, workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 0.5f, buildPowerPerTick: 1.0f));

        state.AddUnit(v1);
        state.AddUnit(v2);
        state.AddUnit(v3);

        _coordinator.Simulation.SpatialGrid.Insert(v1.Id, v1.Position);
        _coordinator.Simulation.SpatialGrid.Insert(v2.Id, v2.Position);
        _coordinator.Simulation.SpatialGrid.Insert(v3.Id, v3.Position);

        // 4. Spawn surrounding harvestable resource nodes
        // Trees
        var t1 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Wood, new Vector2D(40f, 50f), maxAmount: 300, harvestRadius: 1.8f);
        var t2 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Wood, new Vector2D(42f, 54f), maxAmount: 300, harvestRadius: 1.8f);
        var t3 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Wood, new Vector2D(38f, 46f), maxAmount: 300, harvestRadius: 1.8f);
        ForestTreeIds.Add(t1.Id);
        ForestTreeIds.Add(t2.Id);
        ForestTreeIds.Add(t3.Id);
        state.AddResourceNode(t1);
        state.AddResourceNode(t2);
        state.AddResourceNode(t3);

        // Berry bushes
        var b1 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Food, new Vector2D(50f, 60f), maxAmount: 250, harvestRadius: 1.8f);
        var b2 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Food, new Vector2D(53f, 62f), maxAmount: 250, harvestRadius: 1.8f);
        BerryBushIds.Add(b1.Id);
        BerryBushIds.Add(b2.Id);
        state.AddResourceNode(b1);
        state.AddResourceNode(b2);

        // Gold Mine
        GoldMineId = state.GenerateEntityId();
        state.AddResourceNode(new ResourceNodeEntity(GoldMineId, ResourceType.Gold, new Vector2D(62f, 50f), maxAmount: 800, harvestRadius: 2.2f));

        // Stone Quarry
        StoneQuarryId = state.GenerateEntityId();
        state.AddResourceNode(new ResourceNodeEntity(StoneQuarryId, ResourceType.Stone, new Vector2D(50f, 38f), maxAmount: 600, harvestRadius: 2.2f));

        // Iron Deposit
        IronDepositId = state.GenerateEntityId();
        state.AddResourceNode(new ResourceNodeEntity(IronDepositId, ResourceType.Iron, new Vector2D(60f, 60f), maxAmount: 500, harvestRadius: 2.2f));

        // 5. Update population manager
        var popManager = state.GetOrCreatePopulationManager(_factionId);
        popManager.SetCurrentPopulation(3, tick);
        popManager.RecalculateCapacity(state.ActiveBuildings, tick);
    }

    public void OrderGatherWood(EntityId workerId, EntityId? treeId = null)
    {
        var targetTree = treeId ?? ForestTreeIds[0];
        _coordinator.IssueGatherOrder(_factionId, new[] { workerId }, targetTree);
    }

    public void OrderGatherFood(EntityId workerId, EntityId? bushId = null)
    {
        var targetBush = bushId ?? BerryBushIds[0];
        _coordinator.IssueGatherOrder(_factionId, new[] { workerId }, targetBush);
    }

    public Result OrderPlaceBuilding(string buildingType, Vector2D position)
    {
        return _coordinator.IssuePlaceBuildingOrder(_factionId, buildingType, position);
    }

    public Result OrderConstructBuilding(EntityId[] workerIds, EntityId buildingId)
    {
        return _coordinator.IssueConstructOrder(_factionId, workerIds, buildingId);
    }

    public Result OrderTrainUnit(EntityId buildingId, string unitType)
    {
        return _coordinator.IssueQueueProductionOrder(_factionId, buildingId, unitType);
    }
}
