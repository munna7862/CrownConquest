namespace CrownConquest.Domain.World;

/// <summary>
/// Strongly-typed identifier for a strategic army on the campaign map.
/// </summary>
public readonly record struct StrategicArmyId(int Value)
{
    public static readonly StrategicArmyId Invalid = new(0);
    public bool IsValid => Value > 0;

    public override string ToString() => $"Army_{Value}";
}
