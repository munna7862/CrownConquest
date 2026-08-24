using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.World;

/// <summary>
/// Authoritative manager of diplomatic reputations and standing state machine between player and world factions.
/// Enforces standing thresholds and trade multipliers.
/// </summary>
public sealed class FactionDiplomacyManager
{
    private readonly Dictionary<string, FactionDefinition> _factions = new();
    private readonly List<string> _factionOrder = new();
    private readonly Dictionary<string, int> _reputations = new();
    private readonly DomainEventBus? _eventBus;

    public IReadOnlyList<string> FactionIds => _factionOrder;
    public int FactionCount => _factions.Count;

    public FactionDiplomacyManager(DomainEventBus? eventBus = null)
    {
        _eventBus = eventBus;
    }

    public void RegisterFaction(FactionDefinition faction)
    {
        if (!_factions.ContainsKey(faction.Id))
        {
            _factions[faction.Id] = faction;
            _factionOrder.Add(faction.Id);
            _reputations[faction.Id] = Math.Clamp(faction.InitialReputation, -100, 100);
        }
    }

    public bool TryGetFaction(string factionId, out FactionDefinition? faction)
    {
        return _factions.TryGetValue(factionId, out faction);
    }

    public FactionDefinition? GetFaction(string factionId)
    {
        _factions.TryGetValue(factionId, out var faction);
        return faction;
    }

    public IEnumerable<FactionDefinition> GetAllFactions()
    {
        for (int i = 0; i < _factionOrder.Count; i++)
        {
            yield return _factions[_factionOrder[i]];
        }
    }

    public int GetReputation(string factionId)
    {
        if (_reputations.TryGetValue(factionId, out int rep))
        {
            return rep;
        }
        return 0;
    }

    public void SetReputation(string factionId, int reputation, ulong simulationTick = 0)
    {
        int oldRep = GetReputation(factionId);
        int newRep = Math.Clamp(reputation, -100, 100);
        var oldStanding = CalculateStanding(oldRep);
        var newStanding = CalculateStanding(newRep);

        _reputations[factionId] = newRep;

        if (oldRep != newRep && _eventBus != null)
        {
            _eventBus.Publish(new FactionReputationChangedEvent(simulationTick, factionId, oldRep, newRep, newRep - oldRep));
        }

        if (oldStanding != newStanding && _eventBus != null)
        {
            _eventBus.Publish(new FactionStandingChangedEvent(simulationTick, factionId, oldStanding, newStanding));
        }
    }

    public Result ModifyReputation(string factionId, int delta, ulong simulationTick = 0)
    {
        if (!_factions.ContainsKey(factionId) && !_reputations.ContainsKey(factionId))
        {
            return Result.Failure(new GameError("FACTION_NOT_FOUND", $"Faction {factionId} is not registered."));
        }

        int current = GetReputation(factionId);
        SetReputation(factionId, current + delta, simulationTick);
        return Result.Success();
    }

    public DiplomacyStanding GetStanding(string factionId)
    {
        return CalculateStanding(GetReputation(factionId));
    }

    public static DiplomacyStanding CalculateStanding(int reputation)
    {
        if (reputation <= -60) return DiplomacyStanding.AtWar;
        if (reputation <= -20) return DiplomacyStanding.Hostile;
        if (reputation < 20) return DiplomacyStanding.Neutral;
        if (reputation < 60) return DiplomacyStanding.Friendly;
        return DiplomacyStanding.Allied;
    }

    public double GetTradeBonusModifier(string factionId)
    {
        var standing = GetStanding(factionId);
        return standing switch
        {
            DiplomacyStanding.Allied => 1.25,
            DiplomacyStanding.Friendly => 1.10,
            DiplomacyStanding.Neutral => 1.00,
            DiplomacyStanding.Hostile => 0.85,
            DiplomacyStanding.AtWar => 0.00,
            _ => 1.00
        };
    }

    public bool IsAtWar(string factionId) => GetStanding(factionId) == DiplomacyStanding.AtWar;
    public bool IsAllied(string factionId) => GetStanding(factionId) == DiplomacyStanding.Allied;
}
