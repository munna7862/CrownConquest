namespace CrownConquest.Data.Models;

/// <summary>
/// Data-driven XP progression curves and stat scaling parameters.
/// </summary>
public sealed record ProgressionCurveDefinition
{
    public required string Id { get; init; }
    public required int[] LevelXpThresholds { get; init; }
    public float HealthPerLevelBonus { get; init; } = 15f;
    public float DamagePerLevelBonus { get; init; } = 2.5f;
}
