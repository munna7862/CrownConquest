using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.World;

/// <summary>
/// Authoritative calculator for movement durations and path traversal on the strategic world map.
/// </summary>
public static class StrategicMovementCalculator
{
    public const float DefaultArmySpeed = 50f;
    public const float TickDistanceScale = 10f;

    /// <summary>
    /// Computes the travel duration in campaign ticks between two province positions with terrain modifiers.
    /// </summary>
    public static int CalculateTravelTicks(
        Vector2D fromPos,
        Vector2D toPos,
        TerrainType destinationTerrain,
        float armySpeed = DefaultArmySpeed)
    {
        float dx = toPos.X - fromPos.X;
        float dy = toPos.Y - fromPos.Y;
        float distance = MathF.Sqrt((dx * dx) + (dy * dy));

        if (distance < 0.001f)
            return 1;

        float terrainMultiplier = TerrainModifiers.GetDefault(destinationTerrain).MovementSpeedMultiplier;
        if (terrainMultiplier <= 0.01f)
        {
            terrainMultiplier = 0.5f; // Fallback for impassable or slow terrain
        }

        float effectiveSpeed = Math.Max(1f, (armySpeed <= 0f ? DefaultArmySpeed : armySpeed) * terrainMultiplier);
        int ticks = (int)MathF.Round((distance / effectiveSpeed) * TickDistanceScale);

        return Math.Max(1, ticks);
    }
}
