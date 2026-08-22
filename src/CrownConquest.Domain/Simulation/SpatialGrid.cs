using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Simulation;

/// <summary>
/// High-performance 2D spatial grid for fast circular and rectangular entity queries.
/// Avoids per-frame GC allocations through internal reusable buffer pools.
/// </summary>
public sealed class SpatialGrid
{
    private readonly float _cellSize;
    private readonly float _invCellSize;
    private readonly Dictionary<long, List<EntityId>> _grid;
    private readonly Dictionary<EntityId, long> _entityCellMap;

    public float CellSize => _cellSize;

    public SpatialGrid(float cellSize = 5.0f)
    {
        _cellSize = cellSize > 0f ? cellSize : 5.0f;
        _invCellSize = 1.0f / _cellSize;
        _grid = new Dictionary<long, List<EntityId>>(256);
        _entityCellMap = new Dictionary<EntityId, long>(256);
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
