using System;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Tactical combat squad formations.
/// </summary>
public enum FormationType
{
    Line = 0,
    ShieldWall = 1,
    Wedge = 2,
    Square = 3,
    Loose = 4,
    Column = 5
}

/// <summary>
/// Readonly immutable struct defining deterministic combat stat modifiers for tactical formations.
/// </summary>
public readonly struct FormationModifiers : IEquatable<FormationModifiers>
{
    public float MeleeDamageMultiplier { get; init; }
    public float ArmorBonus { get; init; }
    public float MovementSpeedMultiplier { get; init; }
    public float RangedDamageMitigation { get; init; }
    public float ChargeDamageMultiplier { get; init; }
    public bool CanBraceCavalry { get; init; }

    public FormationModifiers(
        float meleeDamageMultiplier = 1.0f,
        float armorBonus = 0.0f,
        float movementSpeedMultiplier = 1.0f,
        float rangedDamageMitigation = 0.0f,
        float chargeDamageMultiplier = 1.0f,
        bool canBraceCavalry = false)
    {
        MeleeDamageMultiplier = meleeDamageMultiplier;
        ArmorBonus = armorBonus;
        MovementSpeedMultiplier = movementSpeedMultiplier;
        RangedDamageMitigation = rangedDamageMitigation;
        ChargeDamageMultiplier = chargeDamageMultiplier;
        CanBraceCavalry = canBraceCavalry;
    }

    public static FormationModifiers Line => new(1.00f, 0.0f, 1.00f, 0.0f, 1.0f, false);
    public static FormationModifiers ShieldWall => new(0.95f, 4.0f, 0.70f, 0.50f, 0.0f, true);
    public static FormationModifiers Wedge => new(1.00f, -2.0f, 1.15f, 0.0f, 1.30f, false);
    public static FormationModifiers Square => new(0.90f, 2.0f, 0.80f, 0.20f, 0.5f, true);
    public static FormationModifiers Loose => new(0.85f, -2.0f, 1.10f, 0.40f, 0.5f, false);
    public static FormationModifiers Column => new(0.80f, -3.0f, 1.25f, -0.20f, 0.5f, false);

    public static FormationModifiers GetDefault(FormationType type) => type switch
    {
        FormationType.Line => Line,
        FormationType.ShieldWall => ShieldWall,
        FormationType.Wedge => Wedge,
        FormationType.Square => Square,
        FormationType.Loose => Loose,
        FormationType.Column => Column,
        _ => Line
    };

    public bool Equals(FormationModifiers other) =>
        MathF.Abs(MeleeDamageMultiplier - other.MeleeDamageMultiplier) < 0.0001f &&
        MathF.Abs(ArmorBonus - other.ArmorBonus) < 0.0001f &&
        MathF.Abs(MovementSpeedMultiplier - other.MovementSpeedMultiplier) < 0.0001f &&
        MathF.Abs(RangedDamageMitigation - other.RangedDamageMitigation) < 0.0001f &&
        MathF.Abs(ChargeDamageMultiplier - other.ChargeDamageMultiplier) < 0.0001f &&
        CanBraceCavalry == other.CanBraceCavalry;

    public override bool Equals(object? obj) => obj is FormationModifiers other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(MeleeDamageMultiplier, ArmorBonus, MovementSpeedMultiplier, RangedDamageMitigation, ChargeDamageMultiplier, CanBraceCavalry);
    public static bool operator ==(FormationModifiers left, FormationModifiers right) => left.Equals(right);
    public static bool operator !=(FormationModifiers left, FormationModifiers right) => !left.Equals(right);
}
