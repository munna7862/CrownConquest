using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class CommandQueueTests
{
    [Fact]
    public void CommandQueue_DeterministicOrdering_ShouldSortByTickAndFaction()
    {
        var queue = new CommandQueue();

        // Enqueue out of order
        queue.Enqueue(new MoveCommand(FactionId.Player2, SubmittedTick: 2, [], Vector2D.Zero));
        queue.Enqueue(new MoveCommand(FactionId.Player1, SubmittedTick: 1, [], Vector2D.Zero));
        queue.Enqueue(new MoveCommand(FactionId.Player2, SubmittedTick: 1, [], Vector2D.Zero));

        var flushed = queue.FlushForTick();

        Assert.Equal(3, flushed.Length);
        // Tick 1, Player 1
        Assert.Equal(1UL, flushed[0].SubmittedTick);
        Assert.Equal(FactionId.Player1, flushed[0].FactionId);

        // Tick 1, Player 2
        Assert.Equal(1UL, flushed[1].SubmittedTick);
        Assert.Equal(FactionId.Player2, flushed[1].FactionId);

        // Tick 2, Player 2
        Assert.Equal(2UL, flushed[2].SubmittedTick);
        Assert.Equal(FactionId.Player2, flushed[2].FactionId);

        // Queue should now be empty
        Assert.Equal(0, queue.PendingCount);
    }
}
