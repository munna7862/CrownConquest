using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// VFX Trigger System — Event-driven effect descriptors
// ─────────────────────────────────────────────────

/// <summary>
/// Types of visual effects that can be triggered.
/// </summary>
public enum VfxEffectType
{
    CombatHit,
    CombatHitCritical,
    ProjectileTrail,
    DeathExplosion,
    ConstructionDust,
    ConstructionComplete,
    LevelUpGlow,
    RankUpBurst,
    HealingAura,
    ChargeDust,
    RoutedPanic
}

/// <summary>
/// Describes a VFX to be played at a position with configurable intensity.
/// </summary>
public readonly record struct VfxTriggerDescriptor(
    VfxEffectType EffectType,
    Vector2D Position,
    float Intensity,
    float Scale,
    int FactionColorIndex,
    ulong TriggerTick);

/// <summary>
/// Presenter that converts domain combat and progression events into VFX trigger descriptors.
/// Uses a pre-allocated ring buffer to batch effects each frame.
/// </summary>
public sealed class VfxTriggerPresenter
{
    private readonly DomainEventBus _eventBus;
    private readonly VfxTriggerDescriptor[] _buffer;
    private int _writeIndex;
    private int _count;
    private readonly int _capacity;

    public int PendingEffectCount => _count;

    public VfxTriggerPresenter(DomainEventBus eventBus, int capacity = 128)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _capacity = capacity;
        _buffer = new VfxTriggerDescriptor[capacity];
        _writeIndex = 0;
        _count = 0;

        _eventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        _eventBus.Subscribe<UnitKilledEvent>(OnUnitKilled);
        _eventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        _eventBus.Subscribe<UnitLevelUpEvent>(OnLevelUp);
        _eventBus.Subscribe<VeterancyRankChangedEvent>(OnRankChanged);
    }

    /// <summary>
    /// Generates a VFX descriptor from a damage event (standalone usage).
    /// </summary>
    public static VfxTriggerDescriptor CreateCombatHitDescriptor(
        in DamageDealtEvent evt,
        Vector2D targetPosition)
    {
        float intensity = Math.Clamp(evt.DamageAmount / 50f, 0.2f, 2.0f);
        return new VfxTriggerDescriptor(
            EffectType: evt.IsCritical ? VfxEffectType.CombatHitCritical : VfxEffectType.CombatHit,
            Position: targetPosition,
            Intensity: intensity,
            Scale: 1.0f + (intensity * 0.3f),
            FactionColorIndex: 0,
            TriggerTick: evt.SimulationTick);
    }

    public VfxTriggerDescriptor GetPendingEffect(int index)
    {
        if (index < 0 || index >= _count)
            throw new ArgumentOutOfRangeException(nameof(index));
        int readIndex = (_writeIndex - _count + index + _capacity) % _capacity;
        return _buffer[readIndex];
    }

    public void ConsumeAll() => _count = 0;

    public void Unregister()
    {
        _eventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
        _eventBus.Unsubscribe<UnitKilledEvent>(OnUnitKilled);
        _eventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        _eventBus.Unsubscribe<UnitLevelUpEvent>(OnLevelUp);
        _eventBus.Unsubscribe<VeterancyRankChangedEvent>(OnRankChanged);
    }

    private void Push(VfxTriggerDescriptor descriptor)
    {
        _buffer[_writeIndex] = descriptor;
        _writeIndex = (_writeIndex + 1) % _capacity;
        if (_count < _capacity) _count++;
    }

    private void OnDamageDealt(in DamageDealtEvent evt)
    {
        float intensity = Math.Clamp(evt.DamageAmount / 50f, 0.2f, 2.0f);
        Push(new VfxTriggerDescriptor(
            evt.IsCritical ? VfxEffectType.CombatHitCritical : VfxEffectType.CombatHit,
            Position: new Vector2D(0f, 0f), // Position resolved by presentation layer from entity lookup
            Intensity: intensity,
            Scale: 1.0f + (intensity * 0.3f),
            FactionColorIndex: 0,
            TriggerTick: evt.SimulationTick));
    }

    private void OnUnitKilled(in UnitKilledEvent evt)
    {
        Push(new VfxTriggerDescriptor(
            VfxEffectType.DeathExplosion,
            evt.DeathPosition,
            Intensity: 1.0f,
            Scale: 1.5f,
            FactionColorIndex: SelectionFeedbackPresenter.GetFactionColorIndex(evt.CasualtyFaction),
            TriggerTick: evt.SimulationTick));
    }

    private void OnBuildingCompleted(in BuildingCompletedEvent evt)
    {
        Push(new VfxTriggerDescriptor(
            VfxEffectType.ConstructionComplete,
            evt.Position,
            Intensity: 1.0f,
            Scale: 2.0f,
            FactionColorIndex: SelectionFeedbackPresenter.GetFactionColorIndex(evt.FactionId),
            TriggerTick: evt.SimulationTick));
    }

    private void OnLevelUp(in UnitLevelUpEvent evt)
    {
        Push(new VfxTriggerDescriptor(
            VfxEffectType.LevelUpGlow,
            Position: new Vector2D(0f, 0f), // Resolved by entity lookup
            Intensity: 0.8f + (evt.NewLevel * 0.1f),
            Scale: 1.2f,
            FactionColorIndex: 0,
            TriggerTick: evt.SimulationTick));
    }

    private void OnRankChanged(in VeterancyRankChangedEvent evt)
    {
        Push(new VfxTriggerDescriptor(
            VfxEffectType.RankUpBurst,
            Position: new Vector2D(0f, 0f), // Resolved by entity lookup
            Intensity: 1.5f,
            Scale: 2.0f,
            FactionColorIndex: 0,
            TriggerTick: evt.SimulationTick));
    }
}
