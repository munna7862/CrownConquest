using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Simulation;

/// <summary>
/// Authoritative simulation state snapshot containing all entities, economies, eras, tech trees, and world state.
/// </summary>
public sealed class SimulationState
{
    private int _nextEntityId = 1;
    private readonly Dictionary<EntityId, UnitEntity> _units = new(256);
    private readonly List<UnitEntity> _activeUnitList = new(256);

    private readonly Dictionary<EntityId, ResourceNodeEntity> _resourceNodes = new(256);
    private readonly List<ResourceNodeEntity> _activeNodeList = new(256);

    private readonly Dictionary<EntityId, BuildingEntity> _buildings = new(128);
    private readonly List<BuildingEntity> _activeBuildingList = new(128);

    private readonly Dictionary<FactionId, ResourceBank> _resourceBanks = new(8);
    private readonly Dictionary<FactionId, PopulationManager> _populationManagers = new(8);
    private readonly Dictionary<FactionId, EraState> _eraStates = new(8);
    private readonly Dictionary<FactionId, FactionTechManager> _techManagers = new(8);

    public PlacementGrid PlacementGrid { get; } = new(cellSize: 1.0f);
    public Combat.TerrainGrid TerrainGrid { get; } = new(64, 64, 1.0f);

    public ulong CurrentTick { get; internal set; }
    public IReadOnlyDictionary<EntityId, UnitEntity> Units => _units;
    public IReadOnlyList<UnitEntity> ActiveUnits => _activeUnitList;

    public IReadOnlyDictionary<EntityId, ResourceNodeEntity> ResourceNodes => _resourceNodes;
    public IReadOnlyList<ResourceNodeEntity> ActiveResourceNodes => _activeNodeList;

    public IReadOnlyDictionary<EntityId, BuildingEntity> Buildings => _buildings;
    public IReadOnlyList<BuildingEntity> ActiveBuildings => _activeBuildingList;

    public IReadOnlyDictionary<FactionId, ResourceBank> ResourceBanks => _resourceBanks;
    public IReadOnlyDictionary<FactionId, PopulationManager> PopulationManagers => _populationManagers;
    public IReadOnlyDictionary<FactionId, EraState> EraStates => _eraStates;
    public IReadOnlyDictionary<FactionId, FactionTechManager> TechManagers => _techManagers;

    public EntityId GenerateEntityId() => new(_nextEntityId++);

    public ResourceBank GetOrCreateResourceBank(FactionId factionId)
    {
        if (!_resourceBanks.TryGetValue(factionId, out var bank))
        {
            bank = new ResourceBank(factionId);
            _resourceBanks[factionId] = bank;
        }
        return bank;
    }

    public PopulationManager GetOrCreatePopulationManager(FactionId factionId)
    {
        if (!_populationManagers.TryGetValue(factionId, out var manager))
        {
            manager = new PopulationManager(factionId);
            _populationManagers[factionId] = manager;
        }
        return manager;
    }

    public EraState GetOrCreateEraState(FactionId factionId)
    {
        if (!_eraStates.TryGetValue(factionId, out var eraState))
        {
            eraState = new EraState(factionId);
            _eraStates[factionId] = eraState;
        }
        return eraState;
    }

    public FactionTechManager GetOrCreateTechManager(FactionId factionId)
    {
        if (!_techManagers.TryGetValue(factionId, out var techManager))
        {
            techManager = new FactionTechManager(factionId);
            _techManagers[factionId] = techManager;
        }
        return techManager;
    }

    public void AddUnit(UnitEntity unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        _units[unit.Id] = unit;
        _activeUnitList.Add(unit);
    }

    public bool TryGetUnit(EntityId id, out UnitEntity? unit)
    {
        return _units.TryGetValue(id, out unit);
    }

    public void RemoveDeadUnits()
    {
        for (int i = _activeUnitList.Count - 1; i >= 0; i--)
        {
            if (!_activeUnitList[i].IsAlive)
            {
                _units.Remove(_activeUnitList[i].Id);
                _activeUnitList.RemoveAt(i);
            }
        }
    }

    public void AddResourceNode(ResourceNodeEntity node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _resourceNodes[node.Id] = node;
        _activeNodeList.Add(node);
    }

    public bool TryGetResourceNode(EntityId id, out ResourceNodeEntity? node)
    {
        return _resourceNodes.TryGetValue(id, out node);
    }

    public void RemoveDepletedNodes()
    {
        for (int i = _activeNodeList.Count - 1; i >= 0; i--)
        {
            if (_activeNodeList[i].IsDepleted)
            {
                _resourceNodes.Remove(_activeNodeList[i].Id);
                _activeNodeList.RemoveAt(i);
            }
        }
    }

    public void AddBuilding(BuildingEntity building)
    {
        ArgumentNullException.ThrowIfNull(building);
        _buildings[building.Id] = building;
        _activeBuildingList.Add(building);
    }

    public bool TryGetBuilding(EntityId id, out BuildingEntity? building)
    {
        return _buildings.TryGetValue(id, out building);
    }

    public void RemoveDeadBuildings()
    {
        for (int i = _activeBuildingList.Count - 1; i >= 0; i--)
        {
            if (!_activeBuildingList[i].IsAlive)
            {
                _buildings.Remove(_activeBuildingList[i].Id);
                _activeBuildingList.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Computes a deterministic checksum hash of the entire simulation state.
    /// </summary>
    public ulong ComputeStateChecksum()
    {
        ulong hash = 14695981039346656037UL; // FNV offset basis
        hash = (hash ^ CurrentTick) * 1099511628211UL;

        // Units checksum
        for (int i = 0; i < _activeUnitList.Count; i++)
        {
            var unit = _activeUnitList[i];
            hash = (hash ^ (ulong)unit.Id.Value) * 1099511628211UL;
            hash = (hash ^ (ulong)unit.FactionId.Value) * 1099511628211UL;
            hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(unit.Position.X)) * 1099511628211UL;
            hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(unit.Position.Y)) * 1099511628211UL;
            hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(unit.CurrentHealth)) * 1099511628211UL;
            hash = (hash ^ (ulong)unit.Veterancy.Level) * 1099511628211UL;
            hash = (hash ^ (ulong)unit.Veterancy.CurrentXp) * 1099511628211UL;
            hash = (hash ^ (ulong)unit.Formation) * 1099511628211UL;
            hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(unit.Morale.CurrentMorale)) * 1099511628211UL;
            hash = (hash ^ (ulong)unit.Charge.MomentumTicks) * 1099511628211UL;
            hash = (hash ^ (ulong)unit.CurrentTerrain) * 1099511628211UL;

            if (unit.WorkerState != null)
            {
                hash = (hash ^ (ulong)unit.WorkerState.CarriedAmount) * 1099511628211UL;
                hash = (hash ^ (ulong)(unit.WorkerState.CarriedResourceType.HasValue ? (int)unit.WorkerState.CarriedResourceType.Value + 1 : 0)) * 1099511628211UL;
            }
            if (unit.HeroState != null)
            {
                hash = (hash ^ (ulong)unit.HeroState.TotalAttributes.Strength) * 1099511628211UL;
                hash = (hash ^ (ulong)unit.HeroState.TotalAttributes.Agility) * 1099511628211UL;
                hash = (hash ^ (ulong)unit.HeroState.TotalAttributes.Willpower) * 1099511628211UL;
                hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(unit.HeroState.CurrentMana)) * 1099511628211UL;
                hash = (hash ^ (ulong)unit.HeroState.AttachedUnitIds.Count) * 1099511628211UL;
                for (int a = 0; a < unit.HeroState.Abilities.Count; a++)
                {
                    hash = (hash ^ (ulong)unit.HeroState.Abilities[a].CooldownRemainingTicks) * 1099511628211UL;
                }
            }
        }


        // Buildings checksum
        for (int i = 0; i < _activeBuildingList.Count; i++)
        {
            var b = _activeBuildingList[i];
            hash = (hash ^ (ulong)b.Id.Value) * 1099511628211UL;
            hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(b.CurrentHealth)) * 1099511628211UL;
            hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(b.CurrentBuildProgress)) * 1099511628211UL;
            hash = (hash ^ (ulong)b.ProductionQueue.Count) * 1099511628211UL;
            hash = (hash ^ (ulong)b.ResearchQueue.Count) * 1099511628211UL;
        }

        // Resource nodes checksum
        for (int i = 0; i < _activeNodeList.Count; i++)
        {
            var node = _activeNodeList[i];
            hash = (hash ^ (ulong)node.Id.Value) * 1099511628211UL;
            hash = (hash ^ (ulong)node.RemainingAmount) * 1099511628211UL;
        }

        // Banks checksum
        foreach (var (factionId, bank) in _resourceBanks)
        {
            hash = (hash ^ (ulong)factionId.Value) * 1099511628211UL;
            hash = (hash ^ (ulong)bank.Food) * 1099511628211UL;
            hash = (hash ^ (ulong)bank.Wood) * 1099511628211UL;
            hash = (hash ^ (ulong)bank.Gold) * 1099511628211UL;
            hash = (hash ^ (ulong)bank.Stone) * 1099511628211UL;
            hash = (hash ^ (ulong)bank.Iron) * 1099511628211UL;
        }

        // Eras checksum
        foreach (var (factionId, eraState) in _eraStates)
        {
            hash = (hash ^ (ulong)factionId.Value) * 1099511628211UL;
            hash = (hash ^ (ulong)eraState.CurrentEra) * 1099511628211UL;
            hash = (hash ^ (ulong)eraState.ProgressTicks) * 1099511628211UL;
        }

        // Tech checksum
        foreach (var (factionId, techManager) in _techManagers)
        {
            hash = (hash ^ (ulong)factionId.Value) * 1099511628211UL;
            hash = (hash ^ (ulong)techManager.UnlockedTechIds.Count) * 1099511628211UL;
        }

        return hash;
    }
}
