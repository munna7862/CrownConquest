using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

/// <summary>
/// Fog of War visibility states for tiles.
/// </summary>
public enum FogState : byte
{
    Unexplored = 0, // Pitch Black Shroud
    Explored = 1,   // Visited Fog (terrain and static buildings visible, enemy units hidden)
    Visible = 2     // Active Line of Sight (100% illuminated in real-time)
}

/// <summary>
/// Dynamic Line-of-Sight and Fog of War system calculating authoritative visibility grids,
/// enemy unit culling, and vision radii with zero per-tick dynamic heap allocations.
/// </summary>
public sealed class FogOfWarSystem
{
    private readonly byte[] _fogGrid;
    private readonly int _width;
    private readonly int _height;
    private readonly float _cellSize;
    private readonly float _invCellSize;

    public int Width => _width;
    public int Height => _height;
    public float CellSize => _cellSize;
    public ReadOnlySpan<byte> RawGrid => _fogGrid;

    public FogOfWarSystem(int width = 100, int height = 100, float cellSize = 2.0f)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));

        _width = width;
        _height = height;
        _cellSize = cellSize;
        _invCellSize = 1.0f / cellSize;
        _fogGrid = new byte[width * height]; // Defaults to 0 (Unexplored)
    }

    /// <summary>
    /// Updates line-of-sight vision masks for the observer faction.
    /// Resets previous visible cells to Explored, then stamps new vision circles.
    /// Guaranteed zero dynamic GC heap allocations.
    /// </summary>
    public void UpdateVision(
        IReadOnlyList<UnitEntity> alliedUnits,
        IReadOnlyList<BuildingEntity> alliedBuildings)
    {
        // 1. Demote previously visible cells to Explored
        int totalCells = _width * _height;
        for (int i = 0; i < totalCells; i++)
        {
            if (_fogGrid[i] == (byte)FogState.Visible)
            {
                _fogGrid[i] = (byte)FogState.Explored;
            }
        }

        // 2. Stamp vision circles for allied units
        if (alliedUnits != null)
        {
            for (int i = 0; i < alliedUnits.Count; i++)
            {
                var unit = alliedUnits[i];
                if (!unit.IsAlive) continue;

                float radiusTiles = GetUnitVisionRadiusTiles(unit);
                StampVisionCircle(unit.Position, radiusTiles);
            }
        }

        // 3. Stamp vision circles for allied buildings
        if (alliedBuildings != null)
        {
            for (int i = 0; i < alliedBuildings.Count; i++)
            {
                var building = alliedBuildings[i];
                if (!building.IsAlive) continue;

                float radiusTiles = GetBuildingVisionRadiusTiles(building);
                StampVisionCircle(building.Position, radiusTiles);
            }
        }
    }

    public FogState GetFogState(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return FogState.Unexplored;
        return (FogState)_fogGrid[(y * _width) + x];
    }

    public FogState GetFogStateAtWorld(Vector2D worldPos)
    {
        int x = (int)MathF.Floor(worldPos.X * _invCellSize);
        int y = (int)MathF.Floor(worldPos.Y * _invCellSize);
        return GetFogState(x, y);
    }

    public bool IsPositionVisible(Vector2D worldPos)
    {
        return GetFogStateAtWorld(worldPos) == FogState.Visible;
    }

    public bool IsPositionExplored(Vector2D worldPos)
    {
        var state = GetFogStateAtWorld(worldPos);
        return state == FogState.Explored || state == FogState.Visible;
    }

    /// <summary>
    /// Checks if an enemy unit is visible to the observer faction (must be in active Visible line of sight).
    /// </summary>
    public bool IsUnitVisibleToPlayer(UnitEntity unit, FactionId playerFaction)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (unit.FactionId == playerFaction) return true;
        if (!unit.IsAlive) return false;

        return IsPositionVisible(unit.Position);
    }

    /// <summary>
    /// Checks if a building is visible to the observer faction (visible if Explored or Visible).
    /// </summary>
    public bool IsBuildingVisibleToPlayer(BuildingEntity building, FactionId playerFaction)
    {
        ArgumentNullException.ThrowIfNull(building);
        if (building.FactionId == playerFaction) return true;

        return IsPositionExplored(building.Position);
    }

    public void RevealAllForTesting()
    {
        int totalCells = _width * _height;
        for (int i = 0; i < totalCells; i++)
        {
            _fogGrid[i] = (byte)FogState.Visible;
        }
    }

    private void StampVisionCircle(Vector2D worldPos, float radiusTiles)
    {
        int centerX = (int)MathF.Floor(worldPos.X * _invCellSize);
        int centerY = (int)MathF.Floor(worldPos.Y * _invCellSize);
        int radInt = (int)MathF.Ceiling(radiusTiles);
        float radSq = radiusTiles * radiusTiles;

        int minX = Math.Max(0, centerX - radInt);
        int maxX = Math.Min(_width - 1, centerX + radInt);
        int minY = Math.Max(0, centerY - radInt);
        int maxY = Math.Min(_height - 1, centerY + radInt);

        for (int y = minY; y <= maxY; y++)
        {
            float dy = y - centerY;
            float dySq = dy * dy;
            int rowOffset = y * _width;

            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - centerX;
                if ((dx * dx) + dySq <= radSq)
                {
                    _fogGrid[rowOffset + x] = (byte)FogState.Visible;
                }
            }
        }
    }

    public static float GetUnitVisionRadiusTiles(UnitEntity unit)
    {
        if (unit.IsHero) return 16f; // 32m
        if (unit.UnitType.Contains("archer", StringComparison.OrdinalIgnoreCase)) return 14f; // 28m
        if (unit.UnitType.Contains("cavalry", StringComparison.OrdinalIgnoreCase)) return 14f; // 28m
        if (unit.UnitType.Contains("equites", StringComparison.OrdinalIgnoreCase)) return 14f; // 28m
        return 12f; // 24m standard
    }

    public static float GetBuildingVisionRadiusTiles(BuildingEntity building)
    {
        if (building.BuildingType.Contains("watchtower", StringComparison.OrdinalIgnoreCase) ||
            building.BuildingType.Contains("tower", StringComparison.OrdinalIgnoreCase))
        {
            return 22f; // 44m
        }
        if (building.BuildingType.Contains("town_center", StringComparison.OrdinalIgnoreCase) ||
            building.BuildingType.Contains("Town Center", StringComparison.OrdinalIgnoreCase) ||
            building.BuildingType.Contains("fortress", StringComparison.OrdinalIgnoreCase))
        {
            return 20f; // 40m
        }
        return 16f; // 32m
    }
}
