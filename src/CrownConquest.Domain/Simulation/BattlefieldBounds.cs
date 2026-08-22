using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Simulation;

/// <summary>
/// Authoritative definition of playable 2D battlefield boundary.
/// Enforces map limits and clamps unit positions.
/// </summary>
public sealed class BattlefieldBounds
{
    public float MinX { get; }
    public float MinY { get; }
    public float MaxX { get; }
    public float MaxY { get; }

    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;
    public Rect2D BoundsRect => new(MinX, MinY, MaxX, MaxY);

    public BattlefieldBounds(float minX = 0f, float minY = 0f, float maxX = 100f, float maxY = 100f)
    {
        if (maxX <= minX || maxY <= minY)
        {
            throw new ArgumentException("Battlefield dimensions must be strictly positive.");
        }

        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    public static BattlefieldBounds Default => new(0f, 0f, 100f, 100f);

    public bool IsInside(Vector2D position)
    {
        return position.X >= MinX && position.X <= MaxX &&
               position.Y >= MinY && position.Y <= MaxY;
    }

    public Vector2D Clamp(Vector2D position, float margin = 0.5f)
    {
        float clampedX = MathF.Max(MinX + margin, MathF.Min(MaxX - margin, position.X));
        float clampedY = MathF.Max(MinY + margin, MathF.Min(MaxY - margin, position.Y));
        return new Vector2D(clampedX, clampedY);
    }
}
