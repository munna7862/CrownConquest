namespace CrownConquest.Domain.Common;

/// <summary>
/// Strongly-typed identifier for game factions (players, AI, neutral).
/// </summary>
public readonly record struct FactionId(int Value) : IComparable<FactionId>
{
    public static readonly FactionId Neutral = new(0);
    public static readonly FactionId Player1 = new(1);
    public static readonly FactionId Player2 = new(2);

    public bool IsValid => Value >= 0;

    public int CompareTo(FactionId other) => Value.CompareTo(other.Value);

    public override string ToString() => $"Faction({Value})";
}
