using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Calculates deterministic formation offsets and destination positions for unit squads.
/// Fully deterministic and allocation-efficient.
/// </summary>
public static class FormationCalculator
{
    /// <summary>
    /// Computes formation slot positions centered around centroid facing a given heading angle (or target direction).
    /// </summary>
    public static Vector2D[] CalculateFormationSlots(
        FormationType formation,
        Vector2D centroid,
        int unitCount,
        float spacing = 2.0f,
        float headingAngleRadians = 0f)
    {
        if (unitCount <= 0) return Array.Empty<Vector2D>();
        if (unitCount == 1) return [centroid];

        var localOffsets = formation switch
        {
            FormationType.Line => CalculateLineOffsets(unitCount, spacing),
            FormationType.ShieldWall => CalculateShieldWallOffsets(unitCount, spacing * 0.75f),
            FormationType.Wedge => CalculateWedgeOffsets(unitCount, spacing),
            FormationType.Square => CalculateSquareOffsets(unitCount, spacing),
            FormationType.Loose => CalculateLooseOffsets(unitCount, spacing * 1.5f),
            FormationType.Column => CalculateColumnOffsets(unitCount, spacing),
            _ => CalculateLineOffsets(unitCount, spacing)
        };

        float cos = MathF.Cos(headingAngleRadians);
        float sin = MathF.Sin(headingAngleRadians);

        var worldSlots = new Vector2D[unitCount];
        for (int i = 0; i < unitCount; i++)
        {
            var offset = localOffsets[i];
            // Rotate offset by heading angle (0 rad = facing +Y or +X, let standard 2D rotation)
            float rotX = (offset.X * cos) - (offset.Y * sin);
            float rotY = (offset.X * sin) + (offset.Y * cos);
            worldSlots[i] = new Vector2D(centroid.X + rotX, centroid.Y + rotY);
        }

        return worldSlots;
    }

    /// <summary>
    /// Computes grid formation slots centered around a target centroid.
    /// </summary>
    public static Vector2D[] CalculateGridFormation(
        Vector2D centroid,
        int unitCount,
        float spacing = 2.0f,
        int preferredColumns = 4)
    {
        if (unitCount <= 0) return Array.Empty<Vector2D>();
        if (unitCount == 1) return [centroid];

        int cols = Math.Min(unitCount, Math.Max(1, preferredColumns));
        int rows = (int)MathF.Ceiling((float)unitCount / cols);

        float halfWidth = (cols - 1) * spacing * 0.5f;
        float halfHeight = (rows - 1) * spacing * 0.5f;

        var slots = new Vector2D[unitCount];
        for (int i = 0; i < unitCount; i++)
        {
            int col = i % cols;
            int row = i / cols;

            float offsetX = (col * spacing) - halfWidth;
            float offsetY = (row * spacing) - halfHeight;

            slots[i] = new Vector2D(centroid.X + offsetX, centroid.Y + offsetY);
        }

        return slots;
    }

    private static Vector2D[] CalculateLineOffsets(int count, float spacing)
    {
        // Wide line (max 8 per rank, 1-2 ranks)
        int cols = Math.Min(count, 8);
        int rows = (int)MathF.Ceiling((float)count / cols);
        return GenerateGridOffsets(count, cols, rows, spacing, spacing);
    }

    private static Vector2D[] CalculateShieldWallOffsets(int count, float spacing)
    {
        // Tight 2-deep interlocking rank
        int cols = (int)MathF.Ceiling((float)count / 2f);
        int rows = 2;
        return GenerateGridOffsets(count, cols, rows, spacing, spacing * 0.8f);
    }

    private static Vector2D[] CalculateColumnOffsets(int count, float spacing)
    {
        // 2 columns wide marching file
        int cols = Math.Min(count, 2);
        int rows = (int)MathF.Ceiling((float)count / cols);
        return GenerateGridOffsets(count, cols, rows, spacing, spacing);
    }

    private static Vector2D[] CalculateSquareOffsets(int count, float spacing)
    {
        // Compact square
        int cols = (int)MathF.Ceiling(MathF.Sqrt(count));
        int rows = (int)MathF.Ceiling((float)count / cols);
        return GenerateGridOffsets(count, cols, rows, spacing, spacing);
    }

    private static Vector2D[] CalculateLooseOffsets(int count, float spacing)
    {
        // Staggered loose skirmish
        int cols = Math.Min(count, 5);
        int rows = (int)MathF.Ceiling((float)count / cols);
        float halfWidth = (cols - 1) * spacing * 0.5f;
        float halfHeight = (rows - 1) * spacing * 0.5f;

        var offsets = new Vector2D[count];
        for (int i = 0; i < count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float stagger = (row % 2 == 1) ? (spacing * 0.5f) : 0f;
            float ox = ((col * spacing) + stagger) - halfWidth;
            float oy = (row * spacing) - halfHeight;
            offsets[i] = new Vector2D(ox, oy);
        }
        return offsets;
    }

    private static Vector2D[] CalculateWedgeOffsets(int count, float spacing)
    {
        // Triangular arrowhead:
        // Rank 0: 1 unit (apex at 0, 0)
        // Rank 1: 2 units
        // Rank 2: 3 units ...
        var offsets = new Vector2D[count];
        int assigned = 0;
        int rank = 0;

        while (assigned < count)
        {
            int unitsInRank = rank + 1;
            float rankWidth = rank * spacing;
            float startX = -rankWidth * 0.5f;
            float y = -rank * spacing; // behind apex

            for (int u = 0; u < unitsInRank && assigned < count; u++)
            {
                float x = unitsInRank == 1 ? 0f : (startX + (u * spacing));
                offsets[assigned++] = new Vector2D(x, y);
            }
            rank++;
        }

        // Center the centroid of the wedge
        float totalY = 0f;
        for (int i = 0; i < count; i++) totalY += offsets[i].Y;
        float avgY = totalY / count;
        for (int i = 0; i < count; i++)
        {
            offsets[i] = new Vector2D(offsets[i].X, offsets[i].Y - avgY);
        }

        return offsets;
    }

    private static Vector2D[] GenerateGridOffsets(int count, int cols, int rows, float spacingX, float spacingY)
    {
        float halfWidth = (cols - 1) * spacingX * 0.5f;
        float halfHeight = (rows - 1) * spacingY * 0.5f;

        var offsets = new Vector2D[count];
        for (int i = 0; i < count; i++)
        {
            int col = i % cols;
            int row = i / cols;

            float ox = (col * spacingX) - halfWidth;
            float oy = (row * spacingY) - halfHeight;
            offsets[i] = new Vector2D(ox, oy);
        }
        return offsets;
    }
}
