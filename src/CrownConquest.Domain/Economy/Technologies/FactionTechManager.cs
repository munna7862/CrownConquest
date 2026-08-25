using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Economy;

/// <summary>
/// Authoritative manager tracking a faction's researched technologies and cumulative stat modifiers.
/// </summary>
public sealed class FactionTechManager
{
    public FactionId FactionId { get; }
    private readonly HashSet<string> _unlockedTechIds = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<string> UnlockedTechIds => _unlockedTechIds;

    public TechModifiers Modifiers { get; private set; } = TechModifiers.Zero;

    public FactionTechManager(FactionId factionId)
    {
        FactionId = factionId;
    }

    public bool IsResearched(string techId)
    {
        if (string.IsNullOrWhiteSpace(techId)) return false;
        return _unlockedTechIds.Contains(techId);
    }

    public bool CanResearch(
        TechnologyDefinition tech,
        CivilizationEra currentEra,
        IReadOnlyCollection<BuildingEntity> factionBuildings,
        out string reason)
    {
        if (tech == null)
        {
            reason = "Technology definition cannot be null.";
            return false;
        }

        if (IsResearched(tech.Id))
        {
            reason = $"Technology '{tech.DisplayName}' is already researched.";
            return false;
        }

        if (currentEra < tech.RequiredEra)
        {
            reason = $"Requires {tech.RequiredEra.GetDisplayName()} (Current: {currentEra.GetDisplayName()}).";
            return false;
        }

        for (int i = 0; i < tech.RequiredTechIds.Count; i++)
        {
            var reqTechId = tech.RequiredTechIds[i];
            if (!IsResearched(reqTechId))
            {
                reason = $"Missing prerequisite technology '{reqTechId}'.";
                return false;
            }
        }

        for (int i = 0; i < tech.RequiredBuildingTypes.Count; i++)
        {
            var reqBuilding = tech.RequiredBuildingTypes[i];
            bool found = false;
            foreach (var b in factionBuildings)
            {
                if (b.FactionId == FactionId && b.IsConstructed && b.IsAlive &&
                    b.BuildingType.Equals(reqBuilding, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                reason = $"Requires constructed building '{reqBuilding}'.";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    public bool TryUnlockTechnology(
        TechnologyDefinition tech,
        EntityId buildingId,
        ulong tick,
        DomainEventBus? eventBus)
    {
        if (tech == null || _unlockedTechIds.Contains(tech.Id)) return false;

        _unlockedTechIds.Add(tech.Id);
        Modifiers = Modifiers.Combine(tech.Modifiers);

        eventBus?.Publish(new TechnologyResearchCompletedEvent(
            tick,
            FactionId,
            buildingId,
            tech.Id));

        return true;
    }

    public void RestoreUnlockedTech(string techId)
    {
        if (!string.IsNullOrWhiteSpace(techId))
        {
            _unlockedTechIds.Add(techId);
        }
    }
}
