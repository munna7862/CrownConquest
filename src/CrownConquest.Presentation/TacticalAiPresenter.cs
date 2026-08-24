using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

/// <summary>
/// Presentation layer presenter observing tactical combat AI maneuvers, dynamic formation shifts,
/// focus-fire eliminations, and siege breach progress.
/// </summary>
public sealed class TacticalAiPresenter
{
    private readonly List<UnitFormationChangedEvent> _formationHistory = new(64);
    private readonly List<UnitKilledEvent> _killHistory = new(64);
    private readonly List<WallBreachedEvent> _breachHistory = new(32);
    private readonly Dictionary<FormationType, int> _formationUsage = new();

    public IReadOnlyList<UnitFormationChangedEvent> FormationHistory => _formationHistory;
    public IReadOnlyList<UnitKilledEvent> KillHistory => _killHistory;
    public IReadOnlyList<WallBreachedEvent> BreachHistory => _breachHistory;

    public void Bind(DomainEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);

        eventBus.Subscribe<UnitFormationChangedEvent>(OnFormationChanged);
        eventBus.Subscribe<UnitKilledEvent>(OnUnitKilled);
        eventBus.Subscribe<WallBreachedEvent>(OnWallBreached);
    }

    private void OnFormationChanged(in UnitFormationChangedEvent evt)
    {
        _formationHistory.Add(evt);
        _formationUsage[evt.Formation] = _formationUsage.GetValueOrDefault(evt.Formation, 0) + 1;
    }

    private void OnUnitKilled(in UnitKilledEvent evt)
    {
        _killHistory.Add(evt);
    }

    private void OnWallBreached(in WallBreachedEvent evt)
    {
        _breachHistory.Add(evt);
    }

    public int GetFormationUsageCount(FormationType formation)
    {
        return _formationUsage.GetValueOrDefault(formation, 0);
    }

    public void Reset()
    {
        _formationHistory.Clear();
        _killHistory.Clear();
        _breachHistory.Clear();
        _formationUsage.Clear();
    }
}
