using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Calculates deterministic formation offsets and destination positions for unit squads.
/// </summary>
public static class FormationCalculator
{
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
}
