using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Commands;

/// <summary>
/// Immutable command representing an intent to mutate domain simulation state.
/// </summary>
public interface ICommand
{
    FactionId FactionId { get; }
    ulong SubmittedTick { get; }
}
