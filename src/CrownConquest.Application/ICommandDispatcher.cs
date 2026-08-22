using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;

namespace CrownConquest.Application;

/// <summary>
/// Dispatches commands from players and AI controllers into the simulation.
/// </summary>
public interface ICommandDispatcher
{
    Result DispatchCommand(ICommand command);
}
