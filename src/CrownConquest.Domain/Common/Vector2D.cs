namespace CrownConquest.Domain.Common;

/// <summary>
/// Deterministic 2D position/velocity vector for pure domain simulation.
/// Completely decoupled from Godot.Vector2.
/// </summary>
public readonly record struct Vector2D(float X, float Y)
{
    public static readonly Vector2D Zero = new(0f, 0f);
    public static readonly Vector2D One = new(1f, 1f);
    public static readonly Vector2D UnitX = new(1f, 0f);
    public static readonly Vector2D UnitY = new(0f, 1f);

    public float LengthSquared => (X * X) + (Y * Y);

    public float Length => MathF.Sqrt(LengthSquared);

    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);

    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);

    public static Vector2D operator -(Vector2D a) => new(-a.X, -a.Y);

    public static Vector2D operator *(Vector2D a, float scalar) => new(a.X * scalar, a.Y * scalar);

    public static Vector2D operator *(float scalar, Vector2D a) => new(a.X * scalar, a.Y * scalar);

    public static Vector2D operator /(Vector2D a, float scalar)
    {
        if (MathF.Abs(scalar) < 1e-6f)
        {
            return Zero;
        }
        return new(a.X / scalar, a.Y / scalar);
    }

    public float DistanceTo(Vector2D other) => (this - other).Length;

    public float DistanceSquaredTo(Vector2D other) => (this - other).LengthSquared;

    public Vector2D Normalized()
    {
        float len = Length;
        return len > 1e-6f ? this / len : Zero;
    }

    public Vector2D MoveTowards(Vector2D target, float maxDistanceDelta)
    {
        Vector2D diff = target - this;
        float distSq = diff.LengthSquared;
        if (distSq <= maxDistanceDelta * maxDistanceDelta || distSq < 1e-6f)
        {
            return target;
        }

        float dist = MathF.Sqrt(distSq);
        return this + (diff / dist * maxDistanceDelta);
    }

    public override string ToString() => $"({X:F2}, {Y:F2})";
}
