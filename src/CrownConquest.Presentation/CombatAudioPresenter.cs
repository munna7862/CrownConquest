using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// Combat Audio Trigger System
// ─────────────────────────────────────────────────

/// <summary>
/// Categories of sound effects.
/// </summary>
public enum SfxCategory
{
    WeaponImpact,
    UnitVoiceBark,
    UnitDeath,
    BuildingConstruction,
    BuildingComplete,
    UnitSpawn,
    LevelUp,
    RankUp,
    Gathering,
    HeroAbility
}

/// <summary>
/// Sub-categories for weapon impact sounds.
/// </summary>
public enum WeaponSubCategory
{
    Melee,
    Ranged,
    Siege,
    Cavalry
}

/// <summary>
/// Describes an audio effect to be played.
/// </summary>
public readonly record struct AudioTriggerDescriptor(
    SfxCategory Category,
    WeaponSubCategory WeaponType,
    Vector2D Position,
    float Volume,
    float Pitch,
    int SoundBankIndex,
    ulong TriggerTick);

/// <summary>
/// Presenter that converts domain events into audio trigger descriptors.
/// Uses a pre-allocated ring buffer for batching audio triggers.
/// </summary>
public sealed class CombatAudioPresenter
{
    private readonly DomainEventBus _eventBus;
    private readonly AudioTriggerDescriptor[] _buffer;
    private int _writeIndex;
    private int _count;
    private readonly int _capacity;
    private int _soundBankSeed;

    public int PendingTriggerCount => _count;

    public CombatAudioPresenter(DomainEventBus eventBus, int capacity = 128, int soundBankSeed = 42)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _capacity = capacity;
        _buffer = new AudioTriggerDescriptor[capacity];
        _writeIndex = 0;
        _count = 0;
        _soundBankSeed = soundBankSeed;

        _eventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        _eventBus.Subscribe<UnitKilledEvent>(OnUnitKilled);
        _eventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
        _eventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        _eventBus.Subscribe<UnitLevelUpEvent>(OnLevelUp);
    }

    /// <summary>
    /// Creates an audio descriptor from a damage event (standalone usage).
    /// </summary>
    public static AudioTriggerDescriptor CreateWeaponImpactDescriptor(
        float damageAmount,
        string attackType,
        Vector2D position,
        ulong tick)
    {
        float volume = Math.Clamp(damageAmount / 40f, 0.3f, 1.0f);
        var weaponType = attackType switch
        {
            "ranged" => WeaponSubCategory.Ranged,
            "siege" => WeaponSubCategory.Siege,
            "cavalry" => WeaponSubCategory.Cavalry,
            _ => WeaponSubCategory.Melee
        };

        return new AudioTriggerDescriptor(
            Category: SfxCategory.WeaponImpact,
            WeaponType: weaponType,
            Position: position,
            Volume: volume,
            Pitch: 0.9f + (volume * 0.2f),
            SoundBankIndex: 0,
            TriggerTick: tick);
    }

    public AudioTriggerDescriptor GetPendingTrigger(int index)
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
        _eventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
        _eventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        _eventBus.Unsubscribe<UnitLevelUpEvent>(OnLevelUp);
    }

    private void Push(AudioTriggerDescriptor descriptor)
    {
        _buffer[_writeIndex] = descriptor;
        _writeIndex = (_writeIndex + 1) % _capacity;
        if (_count < _capacity) _count++;
    }

    private int NextSoundBankIndex()
    {
        // Simple deterministic pseudo-random for sound variation
        _soundBankSeed = (_soundBankSeed * 1103515245 + 12345) & 0x7fffffff;
        return _soundBankSeed % 4; // 4 variants per sound
    }

    private void OnDamageDealt(in DamageDealtEvent evt)
    {
        float volume = Math.Clamp(evt.DamageAmount / 40f, 0.3f, 1.0f);
        Push(new AudioTriggerDescriptor(
            SfxCategory.WeaponImpact,
            WeaponSubCategory.Melee, // Default, resolved by entity lookup in full integration
            Position: new Vector2D(0f, 0f),
            Volume: volume,
            Pitch: 0.9f + (volume * 0.2f),
            SoundBankIndex: NextSoundBankIndex(),
            TriggerTick: evt.SimulationTick));
    }

    private void OnUnitKilled(in UnitKilledEvent evt)
    {
        Push(new AudioTriggerDescriptor(
            SfxCategory.UnitDeath,
            WeaponSubCategory.Melee,
            evt.DeathPosition,
            Volume: 0.8f,
            Pitch: 1.0f,
            SoundBankIndex: NextSoundBankIndex(),
            TriggerTick: evt.SimulationTick));
    }

    private void OnUnitSpawned(in UnitSpawnedEvent evt)
    {
        Push(new AudioTriggerDescriptor(
            SfxCategory.UnitSpawn,
            WeaponSubCategory.Melee,
            evt.Position,
            Volume: 0.5f,
            Pitch: 1.0f,
            SoundBankIndex: 0,
            TriggerTick: evt.SimulationTick));
    }

    private void OnBuildingCompleted(in BuildingCompletedEvent evt)
    {
        Push(new AudioTriggerDescriptor(
            SfxCategory.BuildingComplete,
            WeaponSubCategory.Melee,
            evt.Position,
            Volume: 0.7f,
            Pitch: 1.0f,
            SoundBankIndex: 0,
            TriggerTick: evt.SimulationTick));
    }

    private void OnLevelUp(in UnitLevelUpEvent evt)
    {
        Push(new AudioTriggerDescriptor(
            SfxCategory.LevelUp,
            WeaponSubCategory.Melee,
            Position: new Vector2D(0f, 0f),
            Volume: 0.6f,
            Pitch: 1.0f + (evt.NewLevel * 0.05f),
            SoundBankIndex: 0,
            TriggerTick: evt.SimulationTick));
    }
}
