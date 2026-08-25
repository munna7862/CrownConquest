using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

/// <summary>
/// 8-directional facing compass for 2D sprites.
/// </summary>
public enum FacingDirection : byte
{
    North = 0,
    NorthEast = 1,
    East = 2,
    SouthEast = 3,
    South = 4,
    SouthWest = 5,
    West = 6,
    NorthWest = 7
}

/// <summary>
/// Visual weapon trail arc produced during melee strikes.
/// </summary>
public readonly record struct WeaponTrailDescriptor(
    bool IsActive,
    float StartAngleRadians,
    float SweepAngleRadians,
    float ArcRadius,
    float Alpha,
    RenderColor TrailColor);

/// <summary>
/// Complete visual snapshot of a directional unit sprite on the battlefield.
/// </summary>
public readonly record struct DirectionalUnitVisualState(
    EntityId UnitId,
    FactionId FactionId,
    string UnitType,
    Vector2D Position,
    Vector2D Heading,
    FacingDirection Facing,
    AnimationState AnimState,
    int FrameIndex,
    bool IsAttacking,
    WeaponTrailDescriptor WeaponTrail,
    float HealthPercentage,
    bool IsHero,
    VeterancyRank Rank,
    bool IsCorpse,
    bool IsMounted);

/// <summary>
/// Directional animation controller calculating 8-directional facing, walking cycle frames,
/// melee attack sweep trails, and death collapse states.
/// </summary>
public sealed class DirectionalSpriteController
{
    /// <summary>
    /// Converts a continuous 2D heading vector into the nearest discrete 8-directional facing.
    /// </summary>
    public static FacingDirection FromHeading(Vector2D heading)
    {
        if (heading.LengthSquared < 0.0001f)
            return FacingDirection.South; // Default facing

        // Math.Atan2 returns angle in radians [-PI, PI] where 0 is East (+X), PI/2 is South (+Y)
        float angle = MathF.Atan2(heading.Y, heading.X);
        float degrees = angle * (180f / MathF.PI);
        if (degrees < 0f) degrees += 360f;

        // 8 sectors of 45 degrees each, centered on cardinal directions
        int sector = (int)MathF.Floor((degrees + 22.5f) / 45f) % 8;
        return sector switch
        {
            0 => FacingDirection.East,
            1 => FacingDirection.SouthEast,
            2 => FacingDirection.South,
            3 => FacingDirection.SouthWest,
            4 => FacingDirection.West,
            5 => FacingDirection.NorthWest,
            6 => FacingDirection.North,
            7 => FacingDirection.NorthEast,
            _ => FacingDirection.South
        };
    }

    /// <summary>
    /// Gets the angle in radians for a given facing direction.
    /// </summary>
    public static float GetFacingAngle(FacingDirection facing) => facing switch
    {
        FacingDirection.East => 0f,
        FacingDirection.SouthEast => MathF.PI * 0.25f,
        FacingDirection.South => MathF.PI * 0.5f,
        FacingDirection.SouthWest => MathF.PI * 0.75f,
        FacingDirection.West => MathF.PI,
        FacingDirection.NorthWest => MathF.PI * 1.25f,
        FacingDirection.North => MathF.PI * 1.5f,
        FacingDirection.NorthEast => MathF.PI * 1.75f,
        _ => MathF.PI * 0.5f
    };

    /// <summary>
    /// Calculates frame index based on animation state, unit movement speed, and simulation tick count.
    /// </summary>
    public static int CalculateFrameIndex(AnimationState state, float speed, ulong tick, int totalFrames = 6)
    {
        if (totalFrames <= 0) return 0;

        return state switch
        {
            AnimationState.Idle => (int)((tick / 4) % (ulong)totalFrames),
            AnimationState.Walk => (int)(((tick * (ulong)Math.Max(1, (int)(speed * 0.1f))) / 2) % (ulong)totalFrames),
            AnimationState.Attack => (int)((tick / 2) % (ulong)Math.Min(4, totalFrames)),
            AnimationState.Death => Math.Min((int)(tick % (ulong)totalFrames), totalFrames - 1),
            _ => 0
        };
    }

    /// <summary>
    /// Generates a weapon trail descriptor during attack swings.
    /// </summary>
    public static WeaponTrailDescriptor GetWeaponTrail(UnitEntity unit, ulong currentTick)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (unit.State != UnitState.Attacking && unit.CooldownRemaining <= 0)
            return default;

        var facing = FromHeading(unit.HeadingDirection);
        float baseAngle = GetFacingAngle(facing);
        float sweepAngle = MathF.PI * 0.6f;
        float startAngle = baseAngle - (sweepAngle * 0.5f);
        float arcRadius = unit.AttackRange * 1.2f;

        var trailColor = unit.FactionId == FactionId.Player1 ? RenderColor.CelticBlue : RenderColor.RomanRed;
        if (unit.IsHero) trailColor = RenderColor.GoldRank;

        return new WeaponTrailDescriptor(
            IsActive: true,
            StartAngleRadians: startAngle,
            SweepAngleRadians: sweepAngle,
            ArcRadius: Math.Max(arcRadius, 1.8f),
            Alpha: 0.85f,
            TrailColor: trailColor);
    }

    /// <summary>
    /// Produces a complete directional unit visual state.
    /// </summary>
    public static DirectionalUnitVisualState GetVisualState(UnitEntity unit, ulong currentTick)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var animState = AnimationStateMapper.MapUnitState(unit.State);
        var facing = FromHeading(unit.HeadingDirection);
        int frameIndex = CalculateFrameIndex(animState, unit.MovementSpeed, currentTick);
        var weaponTrail = GetWeaponTrail(unit, currentTick);
        float hpPct = unit.MaxHealth > 0f ? Math.Clamp(unit.CurrentHealth / unit.MaxHealth, 0f, 1f) : 0f;

        bool isMounted = unit.UnitType.Contains("cavalry", StringComparison.OrdinalIgnoreCase) ||
                         unit.UnitType.Contains("equites", StringComparison.OrdinalIgnoreCase) ||
                         unit.UnitType.Contains("chariot", StringComparison.OrdinalIgnoreCase);

        return new DirectionalUnitVisualState(
            UnitId: unit.Id,
            FactionId: unit.FactionId,
            UnitType: unit.UnitType,
            Position: unit.Position,
            Heading: unit.HeadingDirection,
            Facing: facing,
            AnimState: animState,
            FrameIndex: frameIndex,
            IsAttacking: animState == AnimationState.Attack,
            WeaponTrail: weaponTrail,
            HealthPercentage: hpPct,
            IsHero: unit.IsHero,
            Rank: unit.Veterancy.Rank,
            IsCorpse: !unit.IsAlive,
            IsMounted: isMounted);
    }
}
