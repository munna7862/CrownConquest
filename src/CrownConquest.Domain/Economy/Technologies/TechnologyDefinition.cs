using System;
using System.Collections.Generic;

namespace CrownConquest.Domain.Economy;

/// <summary>
/// Domain definition of a researchable technology blueprint.
/// </summary>
public sealed class TechnologyDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public TechCategory Category { get; }
    public CivilizationEra RequiredEra { get; }
    public IReadOnlyList<string> RequiredTechIds { get; }
    public IReadOnlyList<string> RequiredBuildingTypes { get; }
    public ResourceCost Cost { get; }
    public int ResearchDurationTicks { get; }
    public TechModifiers Modifiers { get; }

    public TechnologyDefinition(
        string id,
        string displayName,
        string description,
        TechCategory category,
        CivilizationEra requiredEra,
        ResourceCost cost,
        int researchDurationTicks,
        TechModifiers modifiers,
        IEnumerable<string>? requiredTechIds = null,
        IEnumerable<string>? requiredBuildingTypes = null)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Category = category;
        RequiredEra = requiredEra;
        Cost = cost;
        ResearchDurationTicks = Math.Max(1, researchDurationTicks);
        Modifiers = modifiers;
        RequiredTechIds = requiredTechIds != null ? new List<string>(requiredTechIds) : Array.Empty<string>();
        RequiredBuildingTypes = requiredBuildingTypes != null ? new List<string>(requiredBuildingTypes) : Array.Empty<string>();
    }
}
