using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.Economy;

/// <summary>
/// Authoritative grid placement validator for building construction.
/// Ensures buildings do not overlap boundaries, existing buildings, or harvestable nodes.
/// </summary>
public sealed class PlacementGrid
{
    public float CellSize { get; }

    public PlacementGrid(float cellSize = 1.0f)
    {
        CellSize = Math.Max(0.1f, cellSize);
    }

    public Vector2D SnapToGrid(Vector2D worldPosition)
    {
        float snappedX = MathF.Round(worldPosition.X / CellSize) * CellSize;
        float snappedY = MathF.Round(worldPosition.Y / CellSize) * CellSize;
        return new Vector2D(snappedX, snappedY);
    }

    public Rect2D CalculateBoundingBox(Vector2D centerPosition, Vector2D size)
    {
        return Rect2D.FromCenterAndExtents(centerPosition, size.X * 0.5f, size.Y * 0.5f);
    }

    public bool CanPlace(
        Vector2D centerPosition,
        Vector2D size,
        IEnumerable<BuildingEntity> existingBuildings,
        IEnumerable<ResourceNodeEntity> existingNodes,
        BattlefieldBounds bounds)
    {
        var box = CalculateBoundingBox(centerPosition, size);

        // 1. Must be entirely inside battlefield bounds
        if (box.MinX < bounds.MinX || box.MaxX > bounds.MaxX ||
            box.MinY < bounds.MinY || box.MaxY > bounds.MaxY)
        {
            return false;
        }

        // 2. Must not overlap any existing alive buildings
        foreach (var building in existingBuildings)
        {
            if (!building.IsAlive) continue;
            if (box.Intersects(building.BoundingBox))
            {
                return false;
            }
        }

        // 3. Must not overlap any existing active resource nodes
        foreach (var node in existingNodes)
        {
            if (node.IsDepleted) continue;
            var nodeBox = Rect2D.FromCenterAndExtents(node.Position, 0.8f, 0.8f);

            if (box.Intersects(nodeBox))
            {
                return false;
            }
        }

        return true;
    }
}
