using System.Collections.Generic;

namespace CrownConquest.Data.Models;

public sealed class BuildingDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Faction { get; set; } = "neutral";
    public float GridWidth { get; set; } = 2.0f;
    public float GridHeight { get; set; } = 2.0f;
    public float MaxHealth { get; set; } = 500.0f;
    public int BuildTimeTicks { get; set; } = 100;
    public int PopulationProvided { get; set; } = 0;
    public int FoodCost { get; set; } = 0;
    public int WoodCost { get; set; } = 0;
    public int GoldCost { get; set; } = 0;
    public int StoneCost { get; set; } = 0;
    public int IronCost { get; set; } = 0;
    public List<string> AcceptedDropOffs { get; set; } = new();
}
