using System;

namespace CrownConquest.Domain.Common;

/// <summary>
/// Immutable 2D axis-aligned bounding box (AABB) for spatial queries and selection marquee.
/// </summary>
public readonly record struct Rect2D
{
    public float MinX { get; }
    public float MinY { get; }
    public float MaxX { get; }
    public float MaxY { get; }

    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;
    public Vector2D Center => new((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f);

    public Rect2D(float minX, float minY, float maxX, float maxY)
    {
        MinX = MathF.Min(minX, maxX);
        MinY = MathF.Min(minY, maxY);
        MaxX = MathF.Max(minX, maxX);
        MaxY = MathF.Max(minY, maxY);
    }

    public static Rect2D FromPoints(Vector2D p1, Vector2D p2)
    {
        return new Rect2D(p1.X, p1.Y, p2.X, p2.Y);
    }

    public static Rect2D FromCenterAndExtents(Vector2D center, float halfWidth, float halfHeight)
    {
        return new Rect2D(center.X - halfWidth, center.Y - halfHeight, center.X + halfWidth, center.Y + halfHeight);
    }

    public bool Contains(Vector2D point)
    {
        return point.X >= MinX && point.X <= MaxX &&
               point.Y >= MinY && point.Y <= MaxY;
    }

    public bool Intersects(Rect2D other)
    {
        return MinX <= other.MaxX && MaxX >= other.MinX &&
               MinY <= other.MaxY && MaxY >= other.MinY;
    }

    public Rect2D Expand(float margin)
    {
        return new Rect2D(MinX - margin, MinY - margin, MaxX + margin, MaxY + margin);
    }

    public override string ToString() => $"Rect2D([{MinX:F1}, {MinY:F1}] to [{MaxX:F1}, {MaxY:F1}])";
}
