using System;

namespace CrownConquest.Data.Models;

public sealed class TerrainDefinitionModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float MovementSpeedMultiplier { get; set; } = 1.0f;
    public int ElevationLevel { get; set; } = 0;
    public float RangedCoverMitigation { get; set; } = 0.0f;
    public float ChargeSpeedMultiplier { get; set; } = 1.0f;
    public string Description { get; set; } = string.Empty;
}
