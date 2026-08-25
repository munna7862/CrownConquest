using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Presentation;

/// <summary>
/// Positional 2D audio cue descriptor with volume attenuation and stereo panning.
/// </summary>
public readonly record struct PositionalAudioCue(
    string SfxKey,
    Vector2D WorldPosition,
    float Volume,
    float Pan,
    float Pitch,
    ulong TriggerTick);

/// <summary>
/// Audio management system providing 2D positional panning, Euclidean distance attenuation,
/// and concurrency limiting across melee, ranged, siege, and gathering SFX.
/// </summary>
public sealed class PositionalCombatAudioSystem
{
    private readonly PositionalAudioCue[] _buffer;
    private int _writeIndex;
    private int _count;
    private readonly int _capacity;
    private readonly int _maxConcurrentSameType;
    private readonly (string SfxKey, ulong Tick, int Count)[] _concurrencyTracker;
    private int _trackerCount;

    public int PendingCueCount => _count;

    public PositionalCombatAudioSystem(int capacity = 128, int maxConcurrentSameType = 4)
    {
        _capacity = capacity;
        _buffer = new PositionalAudioCue[capacity];
        _writeIndex = 0;
        _count = 0;
        _maxConcurrentSameType = maxConcurrentSameType;
        _concurrencyTracker = new (string, ulong, int)[16];
        _trackerCount = 0;
    }

    /// <summary>
    /// Computes horizontal stereo panning (-1.0 = full left, 0.0 = center, +1.0 = full right).
    /// </summary>
    public static float ComputePan(Vector2D soundPosition, Vector2D cameraCenter, float viewportWidth = 1600f)
    {
        float dx = soundPosition.X - cameraCenter.X;
        float halfWidth = Math.Max(viewportWidth * 0.5f, 100f);
        float normalized = dx / halfWidth;
        return Math.Clamp(normalized, -1.0f, 1.0f);
    }

    /// <summary>
    /// Computes distance volume attenuation based on inverse distance model.
    /// </summary>
    public static float ComputeVolumeAttenuation(
        Vector2D soundPosition,
        Vector2D cameraCenter,
        float maxAudibleRange = 1200f,
        float referenceDistance = 300f)
    {
        float dx = soundPosition.X - cameraCenter.X;
        float dy = soundPosition.Y - cameraCenter.Y;
        float dist = MathF.Sqrt((dx * dx) + (dy * dy));

        if (dist >= maxAudibleRange) return 0f;
        if (dist <= 0.001f) return 1.0f;

        // Inverse distance falloff with linear cutoff at max range
        float attenuation = referenceDistance / (referenceDistance + dist);
        float rangeFalloff = 1.0f - (dist / maxAudibleRange);
        return Math.Clamp(attenuation * rangeFalloff * 1.5f, 0f, 1.0f);
    }

    /// <summary>
    /// Queues a positional audio cue with automatic panning, attenuation, and concurrency limiting.
    /// </summary>
    public bool TryQueueAudioCue(
        string sfxKey,
        Vector2D worldPosition,
        Vector2D cameraCenter,
        float baseVolume,
        float pitch,
        ulong currentTick,
        float viewportWidth = 1600f,
        float maxAudibleRange = 1200f)
    {
        // 1. Check concurrency limiter
        if (!CanPlaySound(sfxKey, currentTick))
        {
            return false;
        }

        // 2. Compute spatial parameters
        float pan = ComputePan(worldPosition, cameraCenter, viewportWidth);
        float distanceAttenuation = ComputeVolumeAttenuation(worldPosition, cameraCenter, maxAudibleRange);
        float finalVolume = Math.Clamp(baseVolume * distanceAttenuation, 0f, 1.0f);

        if (finalVolume < 0.05f)
        {
            return false; // Below audible threshold
        }

        // 3. Queue cue
        var cue = new PositionalAudioCue(
            SfxKey: sfxKey,
            WorldPosition: worldPosition,
            Volume: finalVolume,
            Pan: pan,
            Pitch: pitch,
            TriggerTick: currentTick);

        Push(cue);
        IncrementConcurrency(sfxKey, currentTick);
        return true;
    }

    public PositionalAudioCue GetPendingCue(int index)
    {
        if (index < 0 || index >= _count)
            throw new ArgumentOutOfRangeException(nameof(index));
        int readIndex = (_writeIndex - _count + index + _capacity) % _capacity;
        return _buffer[readIndex];
    }

    public void ConsumeAll() => _count = 0;

    private bool CanPlaySound(string sfxKey, ulong tick)
    {
        for (int i = 0; i < _trackerCount; i++)
        {
            if (_concurrencyTracker[i].SfxKey == sfxKey && _concurrencyTracker[i].Tick == tick)
            {
                return _concurrencyTracker[i].Count < _maxConcurrentSameType;
            }
        }
        return true;
    }

    private void IncrementConcurrency(string sfxKey, ulong tick)
    {
        for (int i = 0; i < _trackerCount; i++)
        {
            if (_concurrencyTracker[i].SfxKey == sfxKey && _concurrencyTracker[i].Tick == tick)
            {
                _concurrencyTracker[i] = (sfxKey, tick, _concurrencyTracker[i].Count + 1);
                return;
            }
            if (_concurrencyTracker[i].Tick != tick)
            {
                _concurrencyTracker[i] = (sfxKey, tick, 1);
                return;
            }
        }

        if (_trackerCount < _concurrencyTracker.Length)
        {
            _concurrencyTracker[_trackerCount++] = (sfxKey, tick, 1);
        }
        else
        {
            _concurrencyTracker[0] = (sfxKey, tick, 1);
        }
    }

    private void Push(PositionalAudioCue cue)
    {
        _buffer[_writeIndex] = cue;
        _writeIndex = (_writeIndex + 1) % _capacity;
        if (_count < _capacity) _count++;
    }
}
