using System;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// Adaptive Music State Machine
// ─────────────────────────────────────────────────

/// <summary>
/// Music states driven by gameplay context.
/// </summary>
public enum MusicState
{
    Peace,
    Skirmish,
    Battle,
    Victory,
    Defeat
}

/// <summary>
/// Describes the current music state with transition metadata.
/// </summary>
public readonly record struct MusicStateDescriptor(
    MusicState CurrentState,
    MusicState PreviousState,
    string TrackId,
    float Volume,
    float CrossfadeDuration,
    bool IsTransitioning);

/// <summary>
/// Adaptive music state machine that transitions between music states based on
/// combat intensity metrics from the simulation.
/// </summary>
public sealed class AdaptiveMusicPresenter
{
    private MusicState _currentState;
    private MusicState _previousState;
    private bool _isTransitioning;
    private float _transitionProgress;
    private readonly float _crossfadeDuration;

    // Combat intensity thresholds
    private readonly float _skirmishThreshold;
    private readonly float _battleThreshold;
    private int _peaceTicks;
    private readonly int _peaceDelayTicks;

    public MusicState CurrentState => _currentState;
    public MusicState PreviousState => _previousState;
    public bool IsTransitioning => _isTransitioning;
    public float TransitionProgress => _transitionProgress;

    public AdaptiveMusicPresenter(
        float skirmishThreshold = 0.2f,
        float battleThreshold = 0.6f,
        float crossfadeDuration = 3.0f,
        int peaceDelayTicks = 150)
    {
        _currentState = MusicState.Peace;
        _previousState = MusicState.Peace;
        _isTransitioning = false;
        _transitionProgress = 1.0f;
        _crossfadeDuration = crossfadeDuration;
        _skirmishThreshold = skirmishThreshold;
        _battleThreshold = battleThreshold;
        _peaceTicks = 0;
        _peaceDelayTicks = peaceDelayTicks;
    }

    /// <summary>
    /// Updates music state based on combat intensity (0.0 = peaceful, 1.0 = full battle).
    /// </summary>
    /// <param name="combatIntensity">Normalized combat intensity from 0.0 to 1.0.</param>
    /// <returns>Current music state descriptor.</returns>
    public MusicStateDescriptor Update(float combatIntensity)
    {
        combatIntensity = Math.Clamp(combatIntensity, 0f, 1f);

        // Don't change during victory/defeat terminal states
        if (_currentState == MusicState.Victory || _currentState == MusicState.Defeat)
        {
            AdvanceTransition();
            return GetDescriptor();
        }

        var targetState = DetermineTargetState(combatIntensity);

        if (targetState != _currentState)
        {
            TransitionTo(targetState);
        }

        AdvanceTransition();
        return GetDescriptor();
    }

    /// <summary>
    /// Forces a transition to victory or defeat music.
    /// </summary>
    public MusicStateDescriptor SetTerminalState(bool isVictory)
    {
        TransitionTo(isVictory ? MusicState.Victory : MusicState.Defeat);
        return GetDescriptor();
    }

    public MusicStateDescriptor GetDescriptor()
    {
        return new MusicStateDescriptor(
            CurrentState: _currentState,
            PreviousState: _previousState,
            TrackId: GetTrackId(_currentState),
            Volume: GetVolume(_currentState),
            CrossfadeDuration: _crossfadeDuration,
            IsTransitioning: _isTransitioning);
    }

    private MusicState DetermineTargetState(float combatIntensity)
    {
        if (combatIntensity >= _battleThreshold)
        {
            _peaceTicks = 0;
            return MusicState.Battle;
        }

        if (combatIntensity >= _skirmishThreshold)
        {
            _peaceTicks = 0;
            return MusicState.Skirmish;
        }

        // Delay returning to peace to avoid rapid oscillation
        if (_currentState != MusicState.Peace)
        {
            _peaceTicks++;
            if (_peaceTicks < _peaceDelayTicks)
            {
                return _currentState;
            }
        }

        return MusicState.Peace;
    }

    private void TransitionTo(MusicState newState)
    {
        _previousState = _currentState;
        _currentState = newState;
        _isTransitioning = true;
        _transitionProgress = 0f;
    }

    private void AdvanceTransition()
    {
        if (_isTransitioning)
        {
            _transitionProgress = Math.Min(1.0f, _transitionProgress + (1.0f / (_crossfadeDuration * 60f)));
            if (_transitionProgress >= 1.0f)
            {
                _isTransitioning = false;
            }
        }
    }

    /// <summary>
    /// Gets the music track identifier for a state.
    /// </summary>
    public static string GetTrackId(MusicState state) => state switch
    {
        MusicState.Peace => "mus_peace_medieval",
        MusicState.Skirmish => "mus_skirmish_tension",
        MusicState.Battle => "mus_battle_epic",
        MusicState.Victory => "mus_victory_fanfare",
        MusicState.Defeat => "mus_defeat_lament",
        _ => "mus_peace_medieval"
    };

    /// <summary>
    /// Gets the default volume for a music state.
    /// </summary>
    public static float GetVolume(MusicState state) => state switch
    {
        MusicState.Peace => 0.4f,
        MusicState.Skirmish => 0.6f,
        MusicState.Battle => 0.8f,
        MusicState.Victory => 0.7f,
        MusicState.Defeat => 0.5f,
        _ => 0.4f
    };
}
