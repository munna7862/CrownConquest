namespace CrownConquest.Data.Models;

/// <summary>
/// Data-driven unit blueprint loaded from JSON definitions.
/// </summary>
public sealed record UnitDefinition
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Faction { get; init; }
    public float MaxHealth { get; init; } = 100f;
    public float AttackDamage { get; init; } = 15f;
    public float AttackRange { get; init; } = 1.5f;
    public float MovementSpeed { get; init; } = 3.5f;
    public int AttackCooldownTicks { get; init; } = 20;
    public int KillXpValue { get; init; } = 50;
    public int GoldCost { get; init; } = 50;
    public int FoodCost { get; init; } = 30;
    public int TrainingTicks { get; init; } = 100;
}
