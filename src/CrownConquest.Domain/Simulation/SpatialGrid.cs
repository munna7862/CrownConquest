using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Simulation;

/// <summary>
/// High-performance 2D spatial grid for fast circular, rectangular, raycast, and nearest-entity queries.
/// Avoids per-frame GC allocations through internal reusable buffer pools.
/// </summary>
public sealed class SpatialGrid
{
    private readonly float _cellSize;
    private readonly float _invCellSize;
    private readonly Dictionary<long, List<EntityId>> _grid;
    private readonly Dictionary<EntityId, long> _entityCellMap;

    public float CellSize => _cellSize;
    public int ActiveCellCount => _grid.Count;
    public int TotalIndexedEntities => _entityCellMap.Count;

    public SpatialGrid(float cellSize = 8.0f)
    {
        _cellSize = cellSize > 0f ? cellSize : 8.0f;
        _invCellSize = 1.0f / _cellSize;
        _grid = new Dictionary<long, List<EntityId>>(512);
        _entityCellMap = new Dictionary<EntityId, long>(512);
    }

    public int MaxEntitiesPerCell
    {
        get
        {
            int max = 0;
            foreach (var list in _grid.Values)
            {
                if (list.Count > max) max = list.Count;
            }
            return max;
        }
    }

    public void Clear()
    {
        foreach (var cellList in _grid.Values)
        {
            cellList.Clear();
        }
        _entityCellMap.Clear();
    }

    public void Insert(EntityId id, Vector2D position)
    {
        long cellKey = GetCellKey(position);
        if (!_grid.TryGetValue(cellKey, out var list))
        {
            list = new List<EntityId>(8);
            _grid[cellKey] = list;
        }

        list.Add(id);
        _entityCellMap[id] = cellKey;
    }

    public void UpdatePosition(EntityId id, Vector2D oldPos, Vector2D newPos)
    {
        long oldKey = GetCellKey(oldPos);
        long newKey = GetCellKey(newPos);

        if (oldKey == newKey) return;

        if (_grid.TryGetValue(oldKey, out var oldList))
        {
            oldList.Remove(id);
        }

        if (!_grid.TryGetValue(newKey, out var newList))
        {
            newList = new List<EntityId>(8);
            _grid[newKey] = newList;
        }

        newList.Add(id);
        _entityCellMap[id] = newKey;
    }

    public void Remove(EntityId id)
    {
        if (_entityCellMap.TryGetValue(id, out long cellKey))
        {
            if (_grid.TryGetValue(cellKey, out var list))
            {
                list.Remove(id);
            }
            _entityCellMap.Remove(id);
        }
    }

    public void QueryRadius(Vector2D center, float radius, Func<EntityId, Vector2D?> positionLookup, List<EntityId> results)
    {
        results.Clear();
        float radiusSq = radius * radius;

        int minCellX = (int)MathF.Floor((center.X - radius) * _invCellSize);
        int maxCellX = (int)MathF.Floor((center.X + radius) * _invCellSize);
        int minCellY = (int)MathF.Floor((center.Y - radius) * _invCellSize);
        int maxCellY = (int)MathF.Floor((center.Y + radius) * _invCellSize);

        for (int cx = minCellX; cx <= maxCellX; cx++)
        {
            for (int cy = minCellY; cy <= maxCellY; cy++)
            {
                long key = MakeCellKey(cx, cy);
                if (_grid.TryGetValue(key, out var cellUnits))
                {
                    int count = cellUnits.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var entityId = cellUnits[i];
                        var pos = positionLookup(entityId);
                        if (pos.HasValue && pos.Value.DistanceSquaredTo(center) <= radiusSq)
                        {
                            results.Add(entityId);
                        }
                    }
                }
            }
        }
    }

    public void QueryBox(Rect2D box, Func<EntityId, Vector2D?> positionLookup, List<EntityId> results)
    {
        results.Clear();

        int minCellX = (int)MathF.Floor(box.MinX * _invCellSize);
        int maxCellX = (int)MathF.Floor(box.MaxX * _invCellSize);
        int minCellY = (int)MathF.Floor(box.MinY * _invCellSize);
        int maxCellY = (int)MathF.Floor(box.MaxY * _invCellSize);

        for (int cx = minCellX; cx <= maxCellX; cx++)
        {
            for (int cy = minCellY; cy <= maxCellY; cy++)
            {
                long key = MakeCellKey(cx, cy);
                if (_grid.TryGetValue(key, out var cellUnits))
                {
                    int count = cellUnits.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var entityId = cellUnits[i];
                        var pos = positionLookup(entityId);
                        if (pos.HasValue && box.Contains(pos.Value))
                        {
                            results.Add(entityId);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds the nearest enemy entity within maxRadius using Chebyshev ring expansion for fast early-exit.
    /// </summary>
    public EntityId? QueryNearestEnemy(
        Vector2D center,
        float maxRadius,
        FactionId friendlyFaction,
        Func<EntityId, (Vector2D Position, FactionId Faction, bool IsAlive)?> entityLookup)
    {
        float maxRadiusSq = maxRadius * maxRadius;
        int centerCellX = (int)MathF.Floor(center.X * _invCellSize);
        int centerCellY = (int)MathF.Floor(center.Y * _invCellSize);
        int maxCellRadius = (int)MathF.Ceiling(maxRadius * _invCellSize);

        EntityId? closestEnemy = null;
        float closestDistSq = maxRadiusSq;

        for (int r = 0; r <= maxCellRadius; r++)
        {
            // Iterate perimeter of square of radius r (Chebyshev ring)
            int minX = centerCellX - r;
            int maxX = centerCellX + r;
            int minY = centerCellY - r;
            int maxY = centerCellY + r;

            for (int cx = minX; cx <= maxX; cx++)
            {
                for (int cy = minY; cy <= maxY; cy++)
                {
                    // Only evaluate perimeter cells for r > 0
                    if (r > 0 && cx > minX && cx < maxX && cy > minY && cy < maxY)
                    {
                        continue;
                    }

                    long key = MakeCellKey(cx, cy);
                    if (_grid.TryGetValue(key, out var cellUnits))
                    {
                        int count = cellUnits.Count;
                        for (int i = 0; i < count; i++)
                        {
                            var candidateId = cellUnits[i];
                            var info = entityLookup(candidateId);
                            if (info.HasValue && info.Value.IsAlive && info.Value.Faction != friendlyFaction)
                            {
                                float distSq = info.Value.Position.DistanceSquaredTo(center);
                                if (distSq <= closestDistSq)
                                {
                                    closestDistSq = distSq;
                                    closestEnemy = candidateId;
                                }
                            }
                        }
                    }
                }
            }

            // Early exit if an enemy was found within this ring, and next ring cannot possibly contain a closer entity
            float ringInnerEdgeDist = MathF.Max(0f, (r * _cellSize) - _cellSize);
            if (closestEnemy.HasValue && (ringInnerEdgeDist * ringInnerEdgeDist) > closestDistSq)
            {
                break;
            }
        }

        return closestEnemy;
    }

    /// <summary>
    /// Performs a directional ray query along a segment from origin to origin + direction * maxDistance.
    /// </summary>
    public void QueryRay(
        Vector2D origin,
        Vector2D direction,
        float maxDistance,
        float rayThickness,
        Func<EntityId, (Vector2D Position, float Radius, bool IsAlive)?> entityLookup,
        List<EntityId> results)
    {
        results.Clear();
        if (maxDistance <= 0.001f || direction.LengthSquared < 0.0001f) return;

        Vector2D dirNorm = direction.Normalized();
        Vector2D target = origin + (dirNorm * maxDistance);
        float lineLengthSq = maxDistance * maxDistance;

        // Bounding box of ray segment expanded by thickness
        float minX = MathF.Min(origin.X, target.X) - rayThickness;
        float maxX = MathF.Max(origin.X, target.X) + rayThickness;
        float minY = MathF.Min(origin.Y, target.Y) - rayThickness;
        float maxY = MathF.Max(origin.Y, target.Y) + rayThickness;

        int minCellX = (int)MathF.Floor(minX * _invCellSize);
        int maxCellX = (int)MathF.Floor(maxX * _invCellSize);
        int minCellY = (int)MathF.Floor(minY * _invCellSize);
        int maxCellY = (int)MathF.Floor(maxY * _invCellSize);

        for (int cx = minCellX; cx <= maxCellX; cx++)
        {
            for (int cy = minCellY; cy <= maxCellY; cy++)
            {
                long key = MakeCellKey(cx, cy);
                if (_grid.TryGetValue(key, out var cellUnits))
                {
                    int count = cellUnits.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var candidateId = cellUnits[i];
                        var info = entityLookup(candidateId);
                        if (info.HasValue && info.Value.IsAlive)
                        {
                            Vector2D p = info.Value.Position;
                            float combinedRadius = info.Value.Radius + rayThickness;

                            // Distance from point p to segment origin -> target
                            Vector2D ap = p - origin;
                            float t = Math.Clamp(Vector2D.Dot(ap, dirNorm), 0f, maxDistance);
                            Vector2D closestPoint = origin + (dirNorm * t);

                            if (p.DistanceSquaredTo(closestPoint) <= (combinedRadius * combinedRadius))
                            {
                                results.Add(candidateId);
                            }
                        }
                    }
                }
            }
        }
    }

    private long GetCellKey(Vector2D position)
    {
        int cx = (int)MathF.Floor(position.X * _invCellSize);
        int cy = (int)MathF.Floor(position.Y * _invCellSize);
        return MakeCellKey(cx, cy);
    }

    private static long MakeCellKey(int cx, int cy)
    {
        return ((long)cx << 32) | (uint)cy;
    }
}
