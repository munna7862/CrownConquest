using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

/// <summary>
/// Subscribes to domain events and coordinates presentation effects (audio, visual, HUD).
/// Completely decouples Godot scenes from the simulation logic.
/// </summary>
public sealed class PresentationEventBridge
{
    private readonly DomainEventBus _eventBus;
    private int _spawnedEventCount;
    private int _levelUpEventCount;
    private int _killEventCount;

    public int SpawnedEventCount => _spawnedEventCount;
    public int LevelUpEventCount => _levelUpEventCount;
    public int KillEventCount => _killEventCount;

    public PresentationEventBridge(DomainEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
        _eventBus.Subscribe<UnitLevelUpEvent>(OnUnitLevelUp);
        _eventBus.Subscribe<UnitKilledEvent>(OnUnitKilled);
    }

    public void Unregister()
    {
        _eventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
        _eventBus.Unsubscribe<UnitLevelUpEvent>(OnUnitLevelUp);
        _eventBus.Unsubscribe<UnitKilledEvent>(OnUnitKilled);
    }

    private void OnUnitSpawned(in UnitSpawnedEvent evt)
    {
        _spawnedEventCount++;
    }

    private void OnUnitLevelUp(in UnitLevelUpEvent evt)
    {
        _levelUpEventCount++;
    }

    private void OnUnitKilled(in UnitKilledEvent evt)
    {
        _killEventCount++;
    }
}
