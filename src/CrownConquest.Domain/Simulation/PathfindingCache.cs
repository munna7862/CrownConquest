using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Simulation;

/// <summary>
/// High-performance cache for pre-computed pathfinding routes and waypoints.
/// Uses quantised grid cell keys and reusable waypoint buffers to avoid per-movement heap allocations.
/// </summary>
public sealed class PathfindingCache
{
    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        public readonly int StartX;
        public readonly int StartY;
        public readonly int TargetX;
        public readonly int TargetY;

        public CacheKey(int sx, int sy, int tx, int ty)
        {
            StartX = sx;
            StartY = sy;
            TargetX = tx;
            TargetY = ty;
        }

        public bool Equals(CacheKey other) =>
            StartX == other.StartX && StartY == other.StartY && TargetX == other.TargetX && TargetY == other.TargetY;

        public override bool Equals(object? obj) => obj is CacheKey key && Equals(key);

        public override int GetHashCode() =>
            HashCode.Combine(StartX, StartY, TargetX, TargetY);
    }

    private sealed class CachedRoute
    {
        public readonly List<Vector2D> Waypoints;
        public ulong LastAccessTick;

        public CachedRoute(int capacity = 8)
        {
            Waypoints = new List<Vector2D>(capacity);
        }
    }

    private readonly float _quantizationSize;
    private readonly float _invQuantization;
    private readonly int _maxCapacity;
    private readonly Dictionary<CacheKey, CachedRoute> _cache;
    private readonly List<CacheKey> _lruKeys;

    private ulong _totalHits;
    private ulong _totalMisses;

    public int Count => _cache.Count;
    public int MaxCapacity => _maxCapacity;
    public ulong TotalHits => _totalHits;
    public ulong TotalMisses => _totalMisses;
    public double HitRate => (_totalHits + _totalMisses) > 0 ? (double)_totalHits / (_totalHits + _totalMisses) : 0.0;

    public PathfindingCache(int maxCapacity = 256, float quantizationSize = 2.0f)
    {
        _maxCapacity = maxCapacity > 0 ? maxCapacity : 256;
        _quantizationSize = quantizationSize > 0f ? quantizationSize : 2.0f;
        _invQuantization = 1.0f / _quantizationSize;
        _cache = new Dictionary<CacheKey, CachedRoute>(_maxCapacity);
        _lruKeys = new List<CacheKey>(_maxCapacity);
    }

    public void Clear()
    {
        _cache.Clear();
        _lruKeys.Clear();
        _totalHits = 0;
        _totalMisses = 0;
    }

    public bool TryGetRoute(Vector2D start, Vector2D target, ulong currentTick, List<Vector2D> outWaypoints)
    {
        ArgumentNullException.ThrowIfNull(outWaypoints);
        outWaypoints.Clear();

        CacheKey key = MakeKey(start, target);
        if (_cache.TryGetValue(key, out var cached))
        {
            cached.LastAccessTick = currentTick;
            _totalHits++;
            for (int i = 0; i < cached.Waypoints.Count; i++)
            {
                outWaypoints.Add(cached.Waypoints[i]);
            }
            return true;
        }

        _totalMisses++;
        return false;
    }

    public void StoreRoute(Vector2D start, Vector2D target, IReadOnlyList<Vector2D> waypoints, ulong currentTick)
    {
        ArgumentNullException.ThrowIfNull(waypoints);

        CacheKey key = MakeKey(start, target);
        if (!_cache.TryGetValue(key, out var cached))
        {
            if (_cache.Count >= _maxCapacity)
            {
                EvictOldest();
            }

            cached = new CachedRoute(waypoints.Count);
            _cache[key] = cached;
            _lruKeys.Add(key);
        }

        cached.Waypoints.Clear();
        for (int i = 0; i < waypoints.Count; i++)
        {
            cached.Waypoints.Add(waypoints[i]);
        }
        cached.LastAccessTick = currentTick;
    }

    private void EvictOldest()
    {
        if (_lruKeys.Count == 0) return;

        int oldestIndex = 0;
        ulong oldestTick = ulong.MaxValue;

        for (int i = 0; i < _lruKeys.Count; i++)
        {
            var k = _lruKeys[i];
            if (_cache.TryGetValue(k, out var cached) && cached.LastAccessTick < oldestTick)
            {
                oldestTick = cached.LastAccessTick;
                oldestIndex = i;
            }
        }

        var evictKey = _lruKeys[oldestIndex];
        _cache.Remove(evictKey);
        _lruKeys.RemoveAt(oldestIndex);
    }

    private CacheKey MakeKey(Vector2D start, Vector2D target)
    {
        int sx = (int)MathF.Floor(start.X * _invQuantization);
        int sy = (int)MathF.Floor(start.Y * _invQuantization);
        int tx = (int)MathF.Floor(target.X * _invQuantization);
        int ty = (int)MathF.Floor(target.Y * _invQuantization);
        return new CacheKey(sx, sy, tx, ty);
    }
}
