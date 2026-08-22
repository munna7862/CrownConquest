using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

public readonly record struct MilitaryDistribution(
    int Workers,
    int Swordsmen,
    int Spearmen,
    int Archers,
    int Cavalry,
    int TotalUnits);

public readonly record struct ResearchCardPresentation(
    string TechnologyId,
    string DisplayName,
    string Description,
    TechCategory Category,
    ResourceCost Cost,
    int DurationTicks,
    bool IsResearched,
    bool CanResearch,
    string RequirementTooltip);

/// <summary>
/// Presentation layer model reflecting Civilization Era advancement, technology research command cards,
/// active faction modifiers, and mixed-arms military composition.
/// </summary>
public sealed class CivilizationProgressionPresenter
{
    private readonly GameCoordinator _coordinator;
    private readonly FactionId _factionId;

    public CivilizationEra CurrentEra { get; private set; }
    public string EraDisplayName { get; private set; } = string.Empty;
    public bool IsAdvancingEra { get; private set; }
    public float EraAdvancementProgressNormalized { get; private set; }
    public CivilizationEra? TargetEra { get; private set; }

    public IReadOnlyCollection<string> UnlockedTechnologies { get; private set; } = Array.Empty<string>();
    public TechModifiers ActiveTechModifiers { get; private set; } = TechModifiers.Zero;

    public List<ResearchCardPresentation> AvailableResearch { get; } = new();
    public MilitaryDistribution MilitaryComposition { get; private set; }

    public int Food { get; private set; }
    public int Wood { get; private set; }
    public int Gold { get; private set; }
    public int Stone { get; private set; }
    public int Iron { get; private set; }

    public CivilizationProgressionPresenter(GameCoordinator coordinator, FactionId factionId)
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

        var eraState = _coordinator.GetEraState(_factionId);
        CurrentEra = eraState.CurrentEra;
        EraDisplayName = eraState.CurrentEra.GetDisplayName();
        IsAdvancingEra = eraState.IsAdvancing;
        EraAdvancementProgressNormalized = eraState.ProgressNormalized;
        TargetEra = eraState.TargetEra;

        var techManager = _coordinator.GetTechManager(_factionId);
        UnlockedTechnologies = techManager.UnlockedTechIds;
        ActiveTechModifiers = techManager.Modifiers;

        // Populate available research cards
        AvailableResearch.Clear();
        var activeBuildings = _coordinator.Simulation.State.ActiveBuildings;
        foreach (var (techId, tech) in _coordinator.Simulation.TechRegistry)
        {
            bool isResearched = techManager.IsResearched(techId);
            bool canResearch = techManager.CanResearch(tech, CurrentEra, activeBuildings, out string reason);

            AvailableResearch.Add(new ResearchCardPresentation(
                tech.Id,
                tech.DisplayName,
                tech.Description,
                tech.Category,
                tech.Cost,
                tech.ResearchDurationTicks,
                isResearched,
                canResearch,
                reason));
        }

        // Calculate military distribution
        int workers = 0, swordsmen = 0, spearmen = 0, archers = 0, cavalry = 0, total = 0;
        var activeUnits = _coordinator.Simulation.State.ActiveUnits;
        for (int i = 0; i < activeUnits.Count; i++)
        {
            var u = activeUnits[i];
            if (u.FactionId != _factionId || !u.IsAlive) continue;

            total++;
            switch (u.Archetype)
            {
                case UnitArchetype.Worker: workers++; break;
                case UnitArchetype.Spearman: spearmen++; break;
                case UnitArchetype.Archer: archers++; break;
                case UnitArchetype.Cavalry: cavalry++; break;
                default: swordsmen++; break;
            }
        }

        MilitaryComposition = new MilitaryDistribution(
            workers,
            swordsmen,
            spearmen,
            archers,
            cavalry,
            total);
    }
}
