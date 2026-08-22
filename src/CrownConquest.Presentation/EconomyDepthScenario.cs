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
/// Authoritative economy depth scenario showcasing multiple specialized gathering clusters:
/// 1. Forest Outpost (Lumber Camp + dense forest trees)
/// 2. Mining Outpost (Mining Camp + Gold Mines & Iron Deposits)
/// 3. Farmstead (Granary + renewable Farms & Berry Bushes)
/// 4. Stone Quarry Outpost (Stone Quarry Camp + Stone Quarries)
/// 5. Damaged Watchtower fortification demonstrating worker building repair.
/// </summary>
public sealed class EconomyDepthScenario
{
    private readonly GameCoordinator _coordinator;
    private readonly SelectionManager _selection;
    private readonly FactionId _factionId;
    private readonly EconomyDepthPresenter _presenter;

    public GameCoordinator Coordinator => _coordinator;
    public SelectionManager Selection => _selection;
    public FactionId PlayerFaction => _factionId;
    public EconomyDepthPresenter Presenter => _presenter;

    public EntityId TownCenterId { get; private set; }
    public EntityId LumberCampId { get; private set; }
    public EntityId MiningCampId { get; private set; }
    public EntityId StoneQuarryCampId { get; private set; }
    public EntityId GranaryId { get; private set; }
    public EntityId DamagedWatchtowerId { get; private set; }

    public List<EntityId> FarmIds { get; } = new(4);
    public List<EntityId> ForestTreeIds { get; } = new(8);
    public List<EntityId> BerryBushIds { get; } = new(4);
    public List<EntityId> GoldMineIds { get; } = new(4);
    public List<EntityId> IronDepositIds { get; } = new(4);
    public List<EntityId> StoneQuarryIds { get; } = new(4);

    public List<EntityId> LumberjackIds { get; } = new(4);
    public List<EntityId> GoldMinerIds { get; } = new(4);
    public List<EntityId> IronMinerIds { get; } = new(4);
    public List<EntityId> FarmerIds { get; } = new(4);
    public List<EntityId> StoneMinerIds { get; } = new(4);
    public List<EntityId> RepairCrewIds { get; } = new(4);

    public EconomyDepthScenario(
        GameCoordinator? coordinator = null,
        FactionId? factionId = null)
    {
        _coordinator = coordinator ?? new GameCoordinator();
        _factionId = factionId ?? new FactionId(1);
        _selection = new SelectionManager(_coordinator, _factionId);
        _presenter = new EconomyDepthPresenter(_coordinator, _factionId);

        InitializeScenario();
    }

    private void InitializeScenario()
    {
        var state = _coordinator.Simulation.State;
        ulong tick = _coordinator.CurrentTick;

        // 1. Starting stockpile
        var bank = state.GetOrCreateResourceBank(_factionId);
        bank.Deposit(ResourceType.Food, 300, tick);
        bank.Deposit(ResourceType.Wood, 500, tick);
        bank.Deposit(ResourceType.Gold, 200, tick);
        bank.Deposit(ResourceType.Stone, 200, tick);
        bank.Deposit(ResourceType.Iron, 100, tick);

        // 2. Central Town Center (50, 50)
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
            startsConstructed: true,
            baseCost: new ResourceCost(Wood: 275, Stone: 100));
        state.AddBuilding(townCenter);

        // 3. Outpost 1: Lumber Camp & Forest (50, 75)
        LumberCampId = state.GenerateEntityId();
        var lumberCamp = new BuildingEntity(
            LumberCampId,
            _factionId,
            "lumber_camp",
            new Vector2D(50f, 75f),
            new Vector2D(2f, 2f),
            maxHealth: 400f,
            baseBuildTimeTicks: 50f,
            acceptedDropOffTypes: new[] { ResourceType.Wood },
            startsConstructed: true,
            baseCost: new ResourceCost(Wood: 100));
        state.AddBuilding(lumberCamp);

        var tree1 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Wood, new Vector2D(48f, 80f), maxAmount: 300, harvestRadius: 1.8f);
        var tree2 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Wood, new Vector2D(52f, 80f), maxAmount: 300, harvestRadius: 1.8f);
        ForestTreeIds.Add(tree1.Id);
        ForestTreeIds.Add(tree2.Id);
        state.AddResourceNode(tree1);
        state.AddResourceNode(tree2);

        // 4. Outpost 2: Mining Camp & Gold / Iron Veins (75, 50)
        MiningCampId = state.GenerateEntityId();
        var miningCamp = new BuildingEntity(
            MiningCampId,
            _factionId,
            "mining_camp",
            new Vector2D(75f, 50f),
            new Vector2D(2f, 2f),
            maxHealth: 400f,
            baseBuildTimeTicks: 50f,
            acceptedDropOffTypes: new[] { ResourceType.Gold, ResourceType.Iron },
            startsConstructed: true,
            baseCost: new ResourceCost(Wood: 100));
        state.AddBuilding(miningCamp);

        var gold1 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Gold, new Vector2D(80f, 48f), maxAmount: 800, harvestRadius: 2.2f);
        var iron1 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Iron, new Vector2D(80f, 52f), maxAmount: 500, harvestRadius: 2.2f);
        GoldMineIds.Add(gold1.Id);
        IronDepositIds.Add(iron1.Id);
        state.AddResourceNode(gold1);
        state.AddResourceNode(iron1);

        // 5. Outpost 3: Granary & Farms (50, 25)
        GranaryId = state.GenerateEntityId();
        var granary = new BuildingEntity(
            GranaryId,
            _factionId,
            "granary",
            new Vector2D(50f, 25f),
            new Vector2D(2f, 2f),
            maxHealth: 400f,
            baseBuildTimeTicks: 50f,
            acceptedDropOffTypes: new[] { ResourceType.Food },
            startsConstructed: true,
            baseCost: new ResourceCost(Wood: 100));
        state.AddBuilding(granary);

        var farm1Id = state.GenerateEntityId();
        var farm1 = new BuildingEntity(
            farm1Id,
            _factionId,
            "farm",
            new Vector2D(47f, 20f),
            new Vector2D(2f, 2f),
            maxHealth: 200f,
            baseBuildTimeTicks: 30f,
            startsConstructed: true,
            baseCost: new ResourceCost(Wood: 60),
            isFarm: true,
            maxFarmFood: 250,
            farmReseedCost: 60);
        FarmIds.Add(farm1Id);
        state.AddBuilding(farm1);

        var berry1 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Food, new Vector2D(53f, 20f), maxAmount: 250, harvestRadius: 1.8f);
        BerryBushIds.Add(berry1.Id);
        state.AddResourceNode(berry1);

        // 6. Outpost 4: Stone Quarry Camp & Stone Deposits (25, 50)
        StoneQuarryCampId = state.GenerateEntityId();
        var stoneCamp = new BuildingEntity(
            StoneQuarryCampId,
            _factionId,
            "stone_quarry_camp",
            new Vector2D(25f, 50f),
            new Vector2D(2f, 2f),
            maxHealth: 400f,
            baseBuildTimeTicks: 50f,
            acceptedDropOffTypes: new[] { ResourceType.Stone },
            startsConstructed: true,
            baseCost: new ResourceCost(Wood: 100));
        state.AddBuilding(stoneCamp);

        var stone1 = new ResourceNodeEntity(state.GenerateEntityId(), ResourceType.Stone, new Vector2D(20f, 50f), maxAmount: 600, harvestRadius: 2.2f);
        StoneQuarryIds.Add(stone1.Id);
        state.AddResourceNode(stone1);

        // 7. Damaged Watchtower (50, 65) - 200 / 600 HP (Damaged)
        DamagedWatchtowerId = state.GenerateEntityId();
        var watchtower = new BuildingEntity(
            DamagedWatchtowerId,
            _factionId,
            "watchtower",
            new Vector2D(50f, 65f),
            new Vector2D(2f, 2f),
            maxHealth: 600f,
            baseBuildTimeTicks: 60f,
            startsConstructed: true,
            baseCost: new ResourceCost(Wood: 50, Stone: 125));
        // Apply damage down to 200 HP
        watchtower.TakeDamage(400f, EntityId.None, new FactionId(2), tick, null, out _);
        state.AddBuilding(watchtower);

        // 8. Spawn Specialized Villagers
        // Lumberjacks
        SpawnWorker(new Vector2D(49f, 76f), LumberjackIds);
        SpawnWorker(new Vector2D(51f, 76f), LumberjackIds);

        // Miners (Gold & Iron)
        SpawnWorker(new Vector2D(76f, 49f), GoldMinerIds);
        SpawnWorker(new Vector2D(76f, 51f), IronMinerIds);

        // Farmers & Foragers
        SpawnWorker(new Vector2D(48f, 24f), FarmerIds);
        SpawnWorker(new Vector2D(52f, 24f), FarmerIds);

        // Stone Miners
        SpawnWorker(new Vector2D(24f, 49f), StoneMinerIds);

        // Repair Crew near Watchtower
        SpawnWorker(new Vector2D(48f, 64f), RepairCrewIds);
        SpawnWorker(new Vector2D(52f, 64f), RepairCrewIds);

        // 9. Update population
        var popManager = state.GetOrCreatePopulationManager(_factionId);
        popManager.SetCurrentPopulation(state.ActiveUnits.Count, tick);
        popManager.RecalculateCapacity(state.ActiveBuildings, tick);

        _presenter.UpdateSnapshot();
    }

    private void SpawnWorker(Vector2D position, List<EntityId> list)
    {
        var id = _coordinator.Simulation.State.GenerateEntityId();
        var worker = new UnitEntity(
            id,
            _factionId,
            "celtic_villager",
            position,
            maxHealth: 50f,
            attackDamage: 5f,
            movementSpeed: 3.5f,
            workerState: new WorkerGatherState(carryCapacity: 10, harvestRatePerTick: 0.5f, buildPowerPerTick: 1.0f, repairPowerPerTick: 2.0f));

        _coordinator.Simulation.State.AddUnit(worker);
        _coordinator.Simulation.SpatialGrid.Insert(worker.Id, worker.Position);
        list.Add(id);
    }

    public void OrderStartAllEconomicGathering()
    {
        // Lumberjacks -> Tree 1
        _coordinator.IssueGatherOrder(_factionId, LumberjackIds.ToArray(), ForestTreeIds[0]);

        // Gold Miner -> Gold 1
        _coordinator.IssueGatherOrder(_factionId, GoldMinerIds.ToArray(), GoldMineIds[0]);

        // Iron Miner -> Iron 1
        _coordinator.IssueGatherOrder(_factionId, IronMinerIds.ToArray(), IronDepositIds[0]);

        // Farmers -> Farm 1 & Berry Bush 1
        _coordinator.IssueGatherOrder(_factionId, new[] { FarmerIds[0] }, FarmIds[0]);
        _coordinator.IssueGatherOrder(_factionId, new[] { FarmerIds[1] }, BerryBushIds[0]);

        // Stone Miner -> Stone 1
        _coordinator.IssueGatherOrder(_factionId, StoneMinerIds.ToArray(), StoneQuarryIds[0]);
    }

    public void OrderRepairWatchtower()
    {
        _coordinator.IssueRepairOrder(_factionId, RepairCrewIds.ToArray(), DamagedWatchtowerId);
    }
}
