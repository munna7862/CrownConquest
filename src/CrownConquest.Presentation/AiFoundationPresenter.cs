using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

/// <summary>
/// Presentation layer presenter observing autonomous AI faction gameplay progression,
/// resource harvesting, unit production, building construction, combat casualties, and strategic milestones.
/// Decoupled from Godot nodes; usable in headless and UI contexts.
/// </summary>
public sealed class AiFoundationPresenter
{
    private readonly List<UnitSpawnedEvent> _spawnHistory = new(128);
    private readonly List<BuildingCompletedEvent> _completionHistory = new(64);
    private readonly List<ResourceHarvestedEvent> _harvestHistory = new(256);
    private readonly List<UnitKilledEvent> _killHistory = new(64);
    private readonly Dictionary<FactionId, int> _workersSpawned = new(8);
    private readonly Dictionary<FactionId, int> _militarySpawned = new(8);

    public IReadOnlyList<UnitSpawnedEvent> SpawnHistory => _spawnHistory;
    public IReadOnlyList<BuildingCompletedEvent> CompletionHistory => _completionHistory;
    public IReadOnlyList<ResourceHarvestedEvent> HarvestHistory => _harvestHistory;
    public IReadOnlyList<UnitKilledEvent> KillHistory => _killHistory;

    public void Bind(DomainEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);

        eventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
        eventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        eventBus.Subscribe<ResourceHarvestedEvent>(OnResourceHarvested);
        eventBus.Subscribe<UnitKilledEvent>(OnUnitKilled);
    }

    private void OnUnitSpawned(in UnitSpawnedEvent evt)
    {
        _spawnHistory.Add(evt);
        if (evt.UnitType.Equals("worker", StringComparison.OrdinalIgnoreCase) ||
            evt.UnitType.Equals("villager", StringComparison.OrdinalIgnoreCase))
        {
            _workersSpawned[evt.FactionId] = _workersSpawned.GetValueOrDefault(evt.FactionId, 0) + 1;
        }
        else
        {
            _militarySpawned[evt.FactionId] = _militarySpawned.GetValueOrDefault(evt.FactionId, 0) + 1;
        }
    }

    private void OnBuildingCompleted(in BuildingCompletedEvent evt)
    {
        _completionHistory.Add(evt);
    }

    private void OnResourceHarvested(in ResourceHarvestedEvent evt)
    {
        _harvestHistory.Add(evt);
    }

    private void OnUnitKilled(in UnitKilledEvent evt)
    {
        _killHistory.Add(evt);
    }

    public int GetWorkersSpawnedCount(FactionId factionId)
    {
        return _workersSpawned.GetValueOrDefault(factionId, 0);
    }

    public int GetMilitarySpawnedCount(FactionId factionId)
    {
        return _militarySpawned.GetValueOrDefault(factionId, 0);
    }

    public void Reset()
    {
        _spawnHistory.Clear();
        _completionHistory.Clear();
        _harvestHistory.Clear();
        _killHistory.Clear();
        _workersSpawned.Clear();
        _militarySpawned.Clear();
    }
}
