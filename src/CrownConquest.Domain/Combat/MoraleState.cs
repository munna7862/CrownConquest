using System;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Tactical combat morale tiers.
/// </summary>
public enum MoraleLevel
{
    Confident = 0,
    Steady = 1,
    Wavering = 2,
    Breaking = 3,
    Routed = 4
}

/// <summary>
/// Authoritative state tracking a unit's morale and panic thresholds.
/// Fully deterministic without allocations.
/// </summary>
public sealed class MoraleState
{
    public float MaxMorale { get; }
    public float CurrentMorale { get; private set; }
    public bool IsRouted => CurrentMorale <= 0.001f;

    public MoraleLevel Level => CurrentMorale switch
    {
        <= 0.001f => MoraleLevel.Routed,
        < 25.0f => MoraleLevel.Breaking,
        < 50.0f => MoraleLevel.Wavering,
        < 80.0f => MoraleLevel.Steady,
        _ => MoraleLevel.Confident
    };

    public MoraleState(float maxMorale = 100.0f, float? initialMorale = null)
    {
        MaxMorale = Math.Max(10.0f, maxMorale);
        CurrentMorale = Math.Clamp(initialMorale ?? MaxMorale, 0f, MaxMorale);
    }

    public void ApplyShock(float amount)
    {
        if (amount <= 0f) return;
        CurrentMorale = MathF.Max(0f, CurrentMorale - amount);
    }

    public void Recover(float amount)
    {
        if (amount <= 0f) return;
        CurrentMorale = MathF.Min(MaxMorale, CurrentMorale + amount);
    }

    public void Rally(float amount = 25.0f)
    {
        CurrentMorale = MathF.Min(MaxMorale, MathF.Max(25.0f, CurrentMorale + amount));
    }

    public void SetMorale(float morale)
    {
        CurrentMorale = Math.Clamp(morale, 0f, MaxMorale);
    }
}
