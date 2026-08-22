using CrownConquest.Domain.Common;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class Vector2DTests
{
    [Fact]
    public void Vector2D_ArithmeticAndDistance_ShouldBeAccurate()
    {
        var v1 = new Vector2D(3f, 4f);
        var v2 = new Vector2D(1f, 2f);

        var sum = v1 + v2;
        var diff = v1 - v2;
        var scaled = v1 * 2f;

        Assert.Equal(new Vector2D(4f, 6f), sum);
        Assert.Equal(new Vector2D(2f, 2f), diff);
        Assert.Equal(new Vector2D(6f, 8f), scaled);
        Assert.Equal(5f, v1.Length, precision: 5);
        Assert.Equal(25f, v1.LengthSquared, precision: 5);
    }

    [Fact]
    public void Vector2D_Normalization_ShouldHandleZeroAndUnitVectors()
    {
        var zero = Vector2D.Zero;
        Assert.Equal(Vector2D.Zero, zero.Normalized());

        var v = new Vector2D(0f, 10f);
        var norm = v.Normalized();
        Assert.Equal(1.0f, norm.Length, precision: 5);
        Assert.Equal(new Vector2D(0f, 1f), norm);
    }

    [Fact]
    public void Vector2D_MoveTowards_ShouldClampToTarget()
    {
        var current = new Vector2D(0f, 0f);
        var target = new Vector2D(10f, 0f);

        var step1 = current.MoveTowards(target, 4f);
        Assert.Equal(new Vector2D(4f, 0f), step1);

        var step2 = step1.MoveTowards(target, 10f);
        Assert.Equal(target, step2);
    }
}
