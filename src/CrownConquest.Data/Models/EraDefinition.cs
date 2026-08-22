using System.Collections.Generic;

namespace CrownConquest.Data.Models;

/// <summary>
/// Data-driven definition for civilization era progression.
/// </summary>
public sealed record EraDefinition
{
    public required string Id { get; init; }
    public int Era { get; init; }
    public required string DisplayName { get; init; }
    public string Description { get; init; } = string.Empty;
    public int DurationTicks { get; init; } = 100;
    public int FoodCost { get; init; } = 0;
    public int GoldCost { get; init; } = 0;
    public int WoodCost { get; init; } = 0;
    public int StoneCost { get; init; } = 0;
    public int IronCost { get; init; } = 0;
    public List<string> RequiredBuildingTypes { get; init; } = new();
}
