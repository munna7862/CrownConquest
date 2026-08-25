using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Presentation;

/// <summary>
/// Types of combat VFX particles.
/// </summary>
public enum CombatParticleType
{
    Spark,
    BloodSplash,
    LevelUpRuneRing,
    DustPuff,
    DebrisCrater,
    FireEmber,
    SmokePlume
}

/// <summary>
/// Individual particle or rune burst descriptor.
/// </summary>
public readonly record struct CombatParticleDescriptor(
    CombatParticleType Type,
    Vector2D Position,
    Vector2D Velocity,
    float Scale,
    float Alpha,
    float MaxRadius,
    float CurrentRadius,
    int LifeTicks,
    int MaxLifeTicks,
    ulong TriggerTick);

/// <summary>
/// High-impact combat visual effects presenter managing sparks, blood splashes,
/// golden level-up rune rings, and building fire/smoke particles.
/// </summary>
public sealed class CombatVfxPresenter
{
    private readonly CombatParticleDescriptor[] _buffer;
    private int _writeIndex;
    private int _count;
    private readonly int _capacity;

    public int PendingParticleCount => _count;

    public CombatVfxPresenter(int capacity = 256)
    {
        _capacity = capacity;
        _buffer = new CombatParticleDescriptor[capacity];
        _writeIndex = 0;
        _count = 0;
    }

    /// <summary>
    /// Creates spark particles for melee weapon hits.
    /// </summary>
    public static CombatParticleDescriptor CreateHitSparkDescriptor(
        Vector2D position,
        Vector2D impactDirection,
        float damage,
        int seed,
        ulong tick)
    {
        float angleScatter = (((seed % 60) - 30) * MathF.PI) / 180f;
        float speed = 25f + Math.Clamp(damage * 0.8f, 5f, 50f);

        float baseAngle = MathF.Atan2(impactDirection.Y, impactDirection.X) + MathF.PI + angleScatter;
        var vel = new Vector2D(MathF.Cos(baseAngle) * speed, MathF.Sin(baseAngle) * speed);

        return new CombatParticleDescriptor(
            Type: CombatParticleType.Spark,
            Position: position,
            Velocity: vel,
            Scale: 1.0f + (damage / 40f),
            Alpha: 1.0f,
            MaxRadius: 15f,
            CurrentRadius: 2f,
            LifeTicks: 0,
            MaxLifeTicks: 8,
            TriggerTick: tick);
    }

    /// <summary>
    /// Creates blood splash particle for casualties.
    /// </summary>
    public static CombatParticleDescriptor CreateBloodSplashDescriptor(
        Vector2D position,
        ulong tick)
    {
        return new CombatParticleDescriptor(
            Type: CombatParticleType.BloodSplash,
            Position: position,
            Velocity: new Vector2D(0f, 0f),
            Scale: 1.5f,
            Alpha: 0.9f,
            MaxRadius: 25f,
            CurrentRadius: 5f,
            LifeTicks: 0,
            MaxLifeTicks: 30, // Persists longer as ground stain
            TriggerTick: tick);
    }

    /// <summary>
    /// Creates golden rune ring bursting descriptor on unit Level-Up.
    /// </summary>
    public static CombatParticleDescriptor CreateLevelUpRuneDescriptor(
        Vector2D position,
        int newLevel,
        ulong tick)
    {
        float levelScale = 1.0f + (newLevel * 0.15f);
        return new CombatParticleDescriptor(
            Type: CombatParticleType.LevelUpRuneRing,
            Position: position,
            Velocity: new Vector2D(0f, -15f), // Upward floating drift
            Scale: levelScale,
            Alpha: 1.0f,
            MaxRadius: 40f * levelScale,
            CurrentRadius: 6f,
            LifeTicks: 0,
            MaxLifeTicks: 24, // 1.2s at 20Hz
            TriggerTick: tick);
    }

    /// <summary>
    /// Creates projectile impact debris crater descriptor.
    /// </summary>
    public static CombatParticleDescriptor CreateImpactDebrisDescriptor(
        Vector2D position,
        float impactSize,
        ulong tick)
    {
        return new CombatParticleDescriptor(
            Type: CombatParticleType.DebrisCrater,
            Position: position,
            Velocity: new Vector2D(0f, 0f),
            Scale: impactSize,
            Alpha: 1.0f,
            MaxRadius: 30f * impactSize,
            CurrentRadius: 4f,
            LifeTicks: 0,
            MaxLifeTicks: 20,
            TriggerTick: tick);
    }

    public void PushParticle(in CombatParticleDescriptor descriptor)
    {
        _buffer[_writeIndex] = descriptor;
        _writeIndex = (_writeIndex + 1) % _capacity;
        if (_count < _capacity) _count++;
    }

    public CombatParticleDescriptor GetPendingParticle(int index)
    {
        if (index < 0 || index >= _count)
            throw new ArgumentOutOfRangeException(nameof(index));
        int readIndex = (_writeIndex - _count + index + _capacity) % _capacity;
        return _buffer[readIndex];
    }

    public void ConsumeAll() => _count = 0;
}
