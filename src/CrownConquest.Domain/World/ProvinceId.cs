namespace CrownConquest.Domain.World;

/// <summary>
/// Strongly-typed identifier for a strategic world province or region.
/// </summary>
public readonly record struct ProvinceId(string Value)
{
    public static readonly ProvinceId Invalid = new(string.Empty);
    public bool IsValid => !string.IsNullOrWhiteSpace(Value);

    public override string ToString() => Value;

    public static implicit operator string(ProvinceId id) => id.Value;
    public static implicit operator ProvinceId(string value) => new(value);
}
