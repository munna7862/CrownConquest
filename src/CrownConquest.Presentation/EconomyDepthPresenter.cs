using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

public readonly record struct WorkerDistribution(
    int FoodWorkers,
    int WoodWorkers,
    int GoldWorkers,
    int StoneWorkers,
    int IronWorkers,
    int Builders,
    int Repairers,
    int IdleWorkers,
    int TotalWorkers);

public readonly record struct BuildingInventorySummary(
    int TownCenters,
    int LumberCamps,
    int MiningCamps,
    int StoneQuarryCamps,
    int Granaries,
    int Farms,
    int Barracks,
    int Houses,
    int Watchtowers,
    int DamagedBuildings);

/// <summary>
/// Presentation layer model reflecting 5-resource flows, worker task distribution,
/// specialized gathering camp statuses, and building repair health overlays.
/// </summary>
public sealed class EconomyDepthPresenter
{
    private readonly GameCoordinator _coordinator;
    private readonly FactionId _factionId;

    public int Food { get; private set; }
    public int Wood { get; private set; }
    public int Gold { get; private set; }
    public int Stone { get; private set; }
    public int Iron { get; private set; }

    public int CurrentPopulation { get; private set; }
    public int MaxPopulation { get; private set; }

    public WorkerDistribution Workers { get; private set; }
    public BuildingInventorySummary Buildings { get; private set; }

    public EconomyDepthPresenter(GameCoordinator coordinator, FactionId factionId)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _factionId = factionId;
        UpdateSnapshot();
    }

    public void UpdateSnapshot()
    {
        var bank = _coordinator.GetResourceBank(_factionId);
        Food = bank.Food;
        Wood = bank.Wood;
        Gold = bank.Gold;
        Stone = bank.Stone;
        Iron = bank.Iron;

        var pop = _coordinator.GetPopulationManager(_factionId);
        CurrentPopulation = pop.CurrentPopulation;
        MaxPopulation = pop.CurrentMaxCapacity;

        int foodW = 0, woodW = 0, goldW = 0, stoneW = 0, ironW = 0;
        int builders = 0, repairers = 0, idle = 0, total = 0;

        var units = _coordinator.Simulation.State.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var u = units[i];
            if (u.FactionId != _factionId || !u.IsAlive || !u.IsWorker) continue;

            total++;
            var ws = u.WorkerState;
            if (ws == null || u.State == UnitState.Idle)
            {
                idle++;
                continue;
            }

            if (ws.TaskState == WorkerTaskState.MovingToConstruct || ws.TaskState == WorkerTaskState.Constructing)
            {
                builders++;
            }
            else if (ws.TaskState == WorkerTaskState.MovingToRepair || ws.TaskState == WorkerTaskState.Repairing)
            {
                repairers++;
            }
            else if (ws.TaskState == WorkerTaskState.MovingToResource || ws.TaskState == WorkerTaskState.Harvesting || ws.TaskState == WorkerTaskState.ReturningToDropOff)
            {
                var rType = ws.CarriedResourceType;
                if (!rType.HasValue)
                {
                    if (_coordinator.Simulation.State.TryGetResourceNode(ws.TargetResourceNodeId, out var node) && node != null)
                    {
                        rType = node.ResourceType;
                    }
                    else if (_coordinator.Simulation.State.TryGetBuilding(ws.TargetResourceNodeId, out var farm) && farm != null && farm.IsFarm)
                    {
                        rType = ResourceType.Food;
                    }
                }

                switch (rType)
                {
                    case ResourceType.Food: foodW++; break;
                    case ResourceType.Wood: woodW++; break;
                    case ResourceType.Gold: goldW++; break;
                    case ResourceType.Stone: stoneW++; break;
                    case ResourceType.Iron: ironW++; break;
                    default: idle++; break;
                }
            }
            else
            {
                idle++;
            }
        }

        Workers = new WorkerDistribution(
            foodW,
            woodW,
            goldW,
            stoneW,
            ironW,
            builders,
            repairers,
            idle,
            total);

        int tc = 0, lc = 0, mc = 0, sqc = 0, granary = 0, farmCount = 0, barracks = 0, houses = 0, towers = 0, damaged = 0;
        var activeBuildings = _coordinator.Simulation.State.ActiveBuildings;
        for (int i = 0; i < activeBuildings.Count; i++)
        {
            var b = activeBuildings[i];
            if (b.FactionId != _factionId || !b.IsAlive) continue;

            if (b.IsDamaged) damaged++;

            switch (b.BuildingType.ToLowerInvariant())
            {
                case "town_center": tc++; break;
                case "lumber_camp": lc++; break;
                case "mining_camp": mc++; break;
                case "stone_quarry_camp":
                case "stone_quarry": sqc++; break;
                case "granary":
                case "mill": granary++; break;
                case "farm": farmCount++; break;
                case "barracks": barracks++; break;
                case "house": houses++; break;
                case "watchtower":
                case "tower": towers++; break;
            }
        }

        Buildings = new BuildingInventorySummary(
            tc,
            lc,
            mc,
            sqc,
            granary,
            farmCount,
            barracks,
            houses,
            towers,
            damaged);
    }
}
