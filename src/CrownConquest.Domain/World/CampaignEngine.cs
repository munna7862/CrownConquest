using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.World;

/// <summary>
/// Authoritative simulation coordinator for the persistent strategic campaign world map.
/// Manages campaign ticks, multi-province army movements, territorial economies, and tactical battle transitions.
/// </summary>
public sealed class CampaignEngine
{
    private readonly Dictionary<StrategicArmyId, StrategicArmy> _armies = new();
    private readonly List<StrategicArmyId> _armyOrder = new();
    private readonly Dictionary<FactionId, ResourceCost> _treasuries = new();

    public StrategicMap Map { get; }
    public StrategicTerritoryManager TerritoryManager { get; }
    public DomainEventBus EventBus { get; }

    public int SimulationTick { get; private set; }
    public int CampaignTurn { get; private set; } = 1;
    public int TicksPerTurn { get; set; } = 100;

    public IReadOnlyList<StrategicArmyId> ArmyIds => _armyOrder;
    public int ArmyCount => _armies.Count;

    public CampaignEngine(
        StrategicMap map,
        DomainEventBus? eventBus = null,
        int ticksPerTurn = 100)
    {
        Map = map;
        TerritoryManager = new StrategicTerritoryManager(map);
        EventBus = eventBus ?? new DomainEventBus();
        TicksPerTurn = Math.Max(10, ticksPerTurn);
    }

    public void RestoreTickState(int tick, int turn)
    {
        SimulationTick = Math.Max(0, tick);
        CampaignTurn = Math.Max(1, turn);
    }

    public void RegisterArmy(StrategicArmy army)
    {
        if (!_armies.ContainsKey(army.Id))
        {
            _armies[army.Id] = army;
            _armyOrder.Add(army.Id);

            if (Map.TryGetProvince(army.CurrentProvinceId, out var prov) && prov != null)
            {
                if (!prov.StationedArmyIds.Contains(army.Id))
                {
                    prov.StationedArmyIds.Add(army.Id);
                }
            }
        }
    }

    public bool TryGetArmy(StrategicArmyId id, out StrategicArmy? army)
    {
        return _armies.TryGetValue(id, out army);
    }

    public StrategicArmy? GetArmy(StrategicArmyId id)
    {
        _armies.TryGetValue(id, out var army);
        return army;
    }

    public IEnumerable<StrategicArmy> GetAllArmies()
    {
        for (int i = 0; i < _armyOrder.Count; i++)
        {
            yield return _armies[_armyOrder[i]];
        }
    }

    public ResourceCost GetTreasury(FactionId faction)
    {
        if (!_treasuries.TryGetValue(faction, out var treasury))
        {
            treasury = new ResourceCost(Food: 100, Wood: 100, Gold: 100, Stone: 100, Iron: 50);
            _treasuries[faction] = treasury;
        }
        return treasury;
    }

    public void SetTreasury(FactionId faction, ResourceCost inventory)
    {
        _treasuries[faction] = inventory;
    }

    public Result OrderArmyMove(StrategicArmyId armyId, ProvinceId destinationProvinceId)
    {
        if (!TryGetArmy(armyId, out var army) || army == null)
        {
            return Result.Failure(new GameError("ARMY_NOT_FOUND", $"Army {armyId} not found."));
        }

        if (!Map.TryGetProvince(destinationProvinceId, out var destProv) || destProv == null)
        {
            return Result.Failure(new GameError("PROVINCE_NOT_FOUND", $"Destination province {destinationProvinceId} not found."));
        }

        if (army.CurrentProvinceId == destinationProvinceId && !army.IsInTransit)
        {
            return Result.Success();
        }

        var path = Map.FindPath(army.CurrentProvinceId, destinationProvinceId);
        if (path.Count == 0)
        {
            return Result.Failure(new GameError("NO_PATH_FOUND", $"No connected path from {army.CurrentProvinceId} to {destinationProvinceId}."));
        }

        army.Waypoints.Clear();
        for (int i = 0; i < path.Count; i++)
        {
            army.Waypoints.Enqueue(path[i]);
        }

        // Start first hop
        StartNextWaypointHop(army);
        return Result.Success();
    }

    private void StartNextWaypointHop(StrategicArmy army)
    {
        if (army.Waypoints.Count == 0)
        {
            army.ClearDestination();
            return;
        }

        var nextProvinceId = army.Waypoints.Dequeue();
        if (Map.TryGetProvince(army.CurrentProvinceId, out var currentProv) &&
            Map.TryGetProvince(nextProvinceId, out var nextProv) &&
            currentProv != null && nextProv != null)
        {
            int travelTicks = StrategicMovementCalculator.CalculateTravelTicks(
                currentProv.Position,
                nextProv.Position,
                nextProv.Terrain,
                army.BaseMovementSpeed
            );

            army.SetDestination(nextProvinceId, travelTicks);
            EventBus.Publish(new ArmyMovedOnMapEvent((ulong)SimulationTick, army.Id, army.CurrentProvinceId, nextProvinceId));
        }
    }

    public void AdvanceTick()
    {
        SimulationTick++;

        // Process army movements
        for (int i = 0; i < _armyOrder.Count; i++)
        {
            var armyId = _armyOrder[i];
            if (!_armies.TryGetValue(armyId, out var army) || army == null) continue;

            if (army.IsInTransit && army.DestinationProvinceId.HasValue)
            {
                army.MovementTicksRemaining--;
                if (army.MovementTicksRemaining <= 0)
                {
                    // Arrived at destination province
                    var oldProvinceId = army.CurrentProvinceId;
                    var arrivedProvinceId = army.DestinationProvinceId.Value;

                    if (Map.TryGetProvince(oldProvinceId, out var oldProv) && oldProv != null)
                    {
                        oldProv.StationedArmyIds.Remove(army.Id);
                    }

                    army.CurrentProvinceId = arrivedProvinceId;
                    if (Map.TryGetProvince(arrivedProvinceId, out var arrivedProv) && arrivedProv != null)
                    {
                        if (!arrivedProv.StationedArmyIds.Contains(army.Id))
                        {
                            arrivedProv.StationedArmyIds.Add(army.Id);
                        }
                    }

                    EventBus.Publish(new ArmyArrivedAtProvinceEvent((ulong)SimulationTick, army.Id, arrivedProvinceId));

                    // Check for hostile engagement in province
                    bool engagementTriggered = CheckAndResolveEngagement(army, arrivedProvinceId);

                    // If army survived and not locked in battle, continue to next waypoint
                    if (!engagementTriggered && army.HasUnits && army.Waypoints.Count > 0)
                    {
                        StartNextWaypointHop(army);
                    }
                    else
                    {
                        army.ClearDestination();
                    }
                }
            }
        }

        // Check for turn boundary
        if (SimulationTick % TicksPerTurn == 0)
        {
            AdvanceTurn();
        }
    }

    private bool CheckAndResolveEngagement(StrategicArmy army, ProvinceId provinceId)
    {
        if (!Map.TryGetProvince(provinceId, out var province) || province == null)
            return false;

        // Check if hostile: province owner is different, or hostile armies stationed
        bool isHostileProvince = province.OwnerFaction != army.FactionId && province.OwnerFaction != FactionId.Neutral;
        StrategicArmy? enemyArmy = null;

        for (int i = 0; i < province.StationedArmyIds.Count; i++)
        {
            var stationedId = province.StationedArmyIds[i];
            if (stationedId != army.Id && _armies.TryGetValue(stationedId, out var stationedArmy) && stationedArmy != null)
            {
                if (stationedArmy.FactionId != army.FactionId && stationedArmy.FactionId != FactionId.Neutral)
                {
                    enemyArmy = stationedArmy;
                    break;
                }
            }
        }

        if (isHostileProvince || enemyArmy != null || province.GarrisonUnits.Count > 0)
        {
            EventBus.Publish(new BattleEngagementStartedEvent((ulong)SimulationTick, army.Id, provinceId, enemyArmy?.Id));

            var setup = new BattleSetup(army, province, enemyArmy, (ulong)(SimulationTick + 42));
            var result = BattleTransitionEngine.ExecuteBattle(setup, eventBus: EventBus);

            EventBus.Publish(new BattleEngagementResolvedEvent(
                (ulong)SimulationTick,
                army.Id,
                provinceId,
                result.VictorFaction,
                result.AttackerCasualties,
                result.DefenderCasualties
            ));

            if (result.ProvinceCaptured)
            {
                EventBus.Publish(new ProvinceCapturedEvent((ulong)SimulationTick, provinceId, province.OwnerFaction, army.FactionId));
            }

            // Cleanup destroyed armies
            if (!army.HasUnits)
            {
                RemoveArmy(army.Id);
                EventBus.Publish(new ArmyDestroyedEvent((ulong)SimulationTick, army.Id, army.FactionId, provinceId));
            }

            if (enemyArmy != null && !enemyArmy.HasUnits)
            {
                RemoveArmy(enemyArmy.Id);
                EventBus.Publish(new ArmyDestroyedEvent((ulong)SimulationTick, enemyArmy.Id, enemyArmy.FactionId, provinceId));
            }

            return true;
        }

        return false;
    }

    public void RemoveArmy(StrategicArmyId armyId)
    {
        if (_armies.TryGetValue(armyId, out var army) && army != null)
        {
            if (Map.TryGetProvince(army.CurrentProvinceId, out var prov) && prov != null)
            {
                prov.StationedArmyIds.Remove(armyId);
            }
            _armies.Remove(armyId);
            _armyOrder.Remove(armyId);
        }
    }

    public void AdvanceTurn()
    {
        CampaignTurn++;

        // Collect province yields into faction treasuries
        var yieldsByFaction = new Dictionary<FactionId, ResourceCost>();

        foreach (var province in Map.GetAllProvinces())
        {
            if (province.OwnerFaction == FactionId.Neutral) continue;

            if (!yieldsByFaction.TryGetValue(province.OwnerFaction, out var current))
            {
                current = ResourceCost.Zero;
            }

            current = current + province.ResourceYields;
            yieldsByFaction[province.OwnerFaction] = current;
        }

        foreach (var kvp in yieldsByFaction)
        {
            var currentTreasury = GetTreasury(kvp.Key);
            var newTreasury = currentTreasury + kvp.Value;
            SetTreasury(kvp.Key, newTreasury);

            EventBus.Publish(new CampaignResourceYieldCollectedEvent((ulong)SimulationTick, kvp.Key, kvp.Value));
        }

        EventBus.Publish(new CampaignTurnAdvancedEvent((ulong)SimulationTick, CampaignTurn));
    }
}
