using CrownConquest.Domain.Common;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class ResultTests
{
    [Fact]
    public void Result_Success_ShouldUnwrapValue()
    {
        var result = Result<int>.Success(100);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(100, result.Value);
        Assert.Equal(GameError.None, result.Error);
    }

    [Fact]
    public void Result_Failure_ShouldContainErrorAndThrowOnValueAccess()
    {
        var error = GameError.InsufficientResources;
        var result = Result<int>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Result_NonGeneric_ShouldHandleSuccessAndFailure()
    {
        var success = Result.Success();
        var failure = Result.Failure(GameError.InvalidTarget);

        Assert.True(success.IsSuccess);
        Assert.True(failure.IsFailure);
        Assert.Equal(GameError.InvalidTarget, failure.Error);
    }
}
