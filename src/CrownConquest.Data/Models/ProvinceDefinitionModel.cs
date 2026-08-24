using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;

namespace CrownConquest.Data.Models;

/// <summary>
/// Serializable data definition for a province on the strategic world map.
/// </summary>
public sealed class ProvinceDefinitionModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float PosX { get; set; }
    public float PosY { get; set; }
    public List<string> ConnectedProvinceIds { get; set; } = new();
    public string Terrain { get; set; } = "Plains";
    public string NodeType { get; set; } = "Settlement";
    public string InitialOwnerFaction { get; set; } = "Neutral";
    public float GarrisonDefenseBonus { get; set; } = 1.0f;
    public int GoldYield { get; set; }
    public int FoodYield { get; set; }
    public int WoodYield { get; set; }
    public int StoneYield { get; set; }
    public int IronYield { get; set; }
}
