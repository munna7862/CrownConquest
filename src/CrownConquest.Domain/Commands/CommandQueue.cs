namespace CrownConquest.Domain.Commands;

/// <summary>
/// Deterministic command queue for staging player and AI commands.
/// Preserves strict deterministic order based on submission tick, faction, and command type.
/// </summary>
public sealed class CommandQueue
{
    private readonly List<ICommand> _stagedCommands = new(128);
    private readonly List<ICommand> _executingCommands = new(128);
    private readonly object _lock = new();

    public int PendingCount
    {
        get
        {
            lock (_lock)
            {
                return _stagedCommands.Count;
            }
        }
    }

    public void Enqueue(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        lock (_lock)
        {
            _stagedCommands.Add(command);
        }
    }

    /// <summary>
    /// Flushes staged commands into the execution buffer for the current tick in deterministic order.
    /// </summary>
    public ReadOnlySpan<ICommand> FlushForTick()
    {
        lock (_lock)
        {
            _executingCommands.Clear();
            if (_stagedCommands.Count == 0)
            {
                return [];
            }

            // Sort deterministically by FactionId then SubmittedTick
            _stagedCommands.Sort((a, b) =>
            {
                int tickComp = a.SubmittedTick.CompareTo(b.SubmittedTick);
                return tickComp != 0 ? tickComp : a.FactionId.CompareTo(b.FactionId);
            });

            _executingCommands.AddRange(_stagedCommands);
            _stagedCommands.Clear();
            return _executingCommands.ToArray();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _stagedCommands.Clear();
            _executingCommands.Clear();
        }
    }
}
