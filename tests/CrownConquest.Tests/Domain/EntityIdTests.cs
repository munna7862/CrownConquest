using CrownConquest.Domain.Common;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class EntityIdTests
{
    [Fact]
    public void EntityId_EqualityAndHashing_ShouldBeConsistent()
    {
        // Arrange
        var id1 = new EntityId(42);
        var id2 = new EntityId(42);
        var id3 = new EntityId(99);

        // Assert
        Assert.Equal(id1, id2);
        Assert.NotEqual(id1, id3);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
        Assert.True(id1 == id2);
        Assert.False(id1 == id3);
        Assert.True(id1.IsValid);
        Assert.False(EntityId.None.IsValid);
    }

    [Fact]
    public void FactionId_ValidationAndEquality_ShouldBeCorrect()
    {
        // Arrange
        var f1 = FactionId.Player1;
        var f2 = new FactionId(1);
        var fNeutral = FactionId.Neutral;

        // Assert
        Assert.Equal(f1, f2);
        Assert.NotEqual(f1, fNeutral);
        Assert.True(f1.IsValid);
        Assert.True(fNeutral.IsValid);
    }
}
