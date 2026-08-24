using System;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Authoritative tactical terrain classification.
/// </summary>
public enum TerrainType
{
    Plains = 0,
    Forest = 1,
    Hills = 2,
    Marsh = 3,
    Road = 4,
    Water = 5,
    Rubble = 6
}

/// <summary>
/// Readonly immutable struct defining deterministic gameplay modifiers for terrain types.
/// </summary>
public readonly struct TerrainModifiers : IEquatable<TerrainModifiers>
{
    public float MovementSpeedMultiplier { get; init; }
    public int ElevationLevel { get; init; }
    public float RangedCoverMitigation { get; init; }
    public float ChargeSpeedMultiplier { get; init; }

    public TerrainModifiers(
        float movementSpeedMultiplier = 1.0f,
        int elevationLevel = 0,
        float rangedCoverMitigation = 0.0f,
        float chargeSpeedMultiplier = 1.0f)
    {
        MovementSpeedMultiplier = movementSpeedMultiplier;
        ElevationLevel = elevationLevel;
        RangedCoverMitigation = rangedCoverMitigation;
        ChargeSpeedMultiplier = chargeSpeedMultiplier;
    }

    public static TerrainModifiers Plains => new(1.0f, 0, 0.0f, 1.0f);
    public static TerrainModifiers Forest => new(0.8f, 0, 0.35f, 0.6f);
    public static TerrainModifiers Hills => new(0.85f, 1, 0.15f, 0.8f);
    public static TerrainModifiers Marsh => new(0.6f, -1, 0.0f, 0.4f);
    public static TerrainModifiers Road => new(1.25f, 0, 0.0f, 1.1f);
    public static TerrainModifiers Water => new(0.0f, 0, 0.0f, 0.0f);
    public static TerrainModifiers Rubble => new(0.75f, 0, 0.20f, 0.5f);

    public static TerrainModifiers GetDefault(TerrainType type) => type switch
    {
        TerrainType.Plains => Plains,
        TerrainType.Forest => Forest,
        TerrainType.Hills => Hills,
        TerrainType.Marsh => Marsh,
        TerrainType.Road => Road,
        TerrainType.Water => Water,
        TerrainType.Rubble => Rubble,
        _ => Plains
    };

    public bool Equals(TerrainModifiers other) =>
        MathF.Abs(MovementSpeedMultiplier - other.MovementSpeedMultiplier) < 0.0001f &&
        ElevationLevel == other.ElevationLevel &&
        MathF.Abs(RangedCoverMitigation - other.RangedCoverMitigation) < 0.0001f &&
        MathF.Abs(ChargeSpeedMultiplier - other.ChargeSpeedMultiplier) < 0.0001f;

    public override bool Equals(object? obj) => obj is TerrainModifiers other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(MovementSpeedMultiplier, ElevationLevel, RangedCoverMitigation, ChargeSpeedMultiplier);
    public static bool operator ==(TerrainModifiers left, TerrainModifiers right) => left.Equals(right);
    public static bool operator !=(TerrainModifiers left, TerrainModifiers right) => !left.Equals(right);
}
