using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// Animation State Machine — Domain-to-Presentation mapping
// ─────────────────────────────────────────────────

/// <summary>
/// Presentation-layer animation states derived from domain UnitState.
/// </summary>
public enum AnimationState
{
    Idle,
    Walk,
    Attack,
    Death,
    Gather,
    Construct,
    Repair,
    Routed,
    LevelUp
}

/// <summary>
/// Describes a unit's current animation state with transition metadata.
/// </summary>
public readonly record struct AnimationStateDescriptor(
    AnimationState CurrentState,
    AnimationState PreviousState,
    bool HasTransitioned,
    float PlaybackSpeed,
    bool IsLooping);

/// <summary>
/// Stateless mapper that converts domain UnitState to presentation AnimationState
/// and generates animation descriptors with transition detection.
/// </summary>
public static class AnimationStateMapper
{
    /// <summary>
    /// Maps a domain UnitState to a presentation AnimationState.
    /// </summary>
    public static AnimationState MapUnitState(UnitState unitState) => unitState switch
    {
        UnitState.Idle => AnimationState.Idle,
        UnitState.Moving => AnimationState.Walk,
        UnitState.Attacking => AnimationState.Attack,
        UnitState.Gathering => AnimationState.Gather,
        UnitState.Returning => AnimationState.Walk,
        UnitState.Constructing => AnimationState.Construct,
        UnitState.Repairing => AnimationState.Repair,
        UnitState.Routed => AnimationState.Routed,
        UnitState.Dead => AnimationState.Death,
        _ => AnimationState.Idle
    };

    /// <summary>
    /// Generates an animation descriptor with transition detection.
    /// </summary>
    public static AnimationStateDescriptor GetDescriptor(
        UnitState currentDomainState,
        AnimationState previousAnimState)
    {
        var newState = MapUnitState(currentDomainState);
        bool transitioned = newState != previousAnimState;

        return new AnimationStateDescriptor(
            CurrentState: newState,
            PreviousState: previousAnimState,
            HasTransitioned: transitioned,
            PlaybackSpeed: GetPlaybackSpeed(newState),
            IsLooping: IsLoopingState(newState));
    }

    /// <summary>
    /// Gets the default playback speed for an animation state.
    /// </summary>
    public static float GetPlaybackSpeed(AnimationState state) => state switch
    {
        AnimationState.Idle => 0.8f,
        AnimationState.Walk => 1.0f,
        AnimationState.Attack => 1.2f,
        AnimationState.Death => 1.0f,
        AnimationState.Gather => 0.9f,
        AnimationState.Construct => 0.9f,
        AnimationState.Repair => 0.9f,
        AnimationState.Routed => 1.3f,    // Panicked, faster animation
        AnimationState.LevelUp => 1.0f,
        _ => 1.0f
    };

    /// <summary>
    /// Determines if an animation state should loop continuously.
    /// </summary>
    public static bool IsLoopingState(AnimationState state) => state switch
    {
        AnimationState.Idle => true,
        AnimationState.Walk => true,
        AnimationState.Gather => true,
        AnimationState.Construct => true,
        AnimationState.Repair => true,
        AnimationState.Routed => true,
        AnimationState.Attack => false,   // One-shot per attack
        AnimationState.Death => false,    // One-shot
        AnimationState.LevelUp => false,  // One-shot overlay
        _ => true
    };
}
