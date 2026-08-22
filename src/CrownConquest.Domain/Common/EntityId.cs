namespace CrownConquest.Domain.Common;

/// <summary>
/// Strongly-typed identifier for all domain entities in the authoritative simulation.
/// Uses a readonly struct to guarantee zero heap allocations.
/// </summary>
public readonly record struct EntityId(int Value) : IComparable<EntityId>
{
    public static readonly EntityId None = new(0);

    public bool IsValid => Value > 0;

    public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

    public override string ToString() => $"Entity({Value})";
}
