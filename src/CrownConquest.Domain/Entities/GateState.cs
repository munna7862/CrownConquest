using System;

namespace CrownConquest.Domain.Entities;

/// <summary>
/// Authoritative state of a fortress gate portal.
/// </summary>
public enum GateState
{
    Closed = 0,
    Open = 1,
    Locked = 2
}

/// <summary>
/// Gate behavior helper and state tracker.
/// </summary>
public sealed class GateDefenseState
{
    public GateState State { get; private set; }
    public bool IsPassableForFriendlies => State == GateState.Open || State == GateState.Closed; // Closed gates auto-open for friendlies
    public bool IsPassableForEnemies => State == GateState.Open;

    public GateDefenseState(GateState initialState = GateState.Closed)
    {
        State = initialState;
    }

    public bool TrySetState(GateState newState)
    {
        State = newState;
        return true;
    }

    public void Toggle()
    {
        State = State switch
        {
            GateState.Closed => GateState.Open,
            GateState.Open => GateState.Closed,
            GateState.Locked => GateState.Open,
            _ => GateState.Closed
        };
    }
}
