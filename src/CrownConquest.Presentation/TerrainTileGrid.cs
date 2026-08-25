using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Presentation;

/// <summary>
/// Types of 2D terrain tiles in the multi-layered tileset.
/// </summary>
public enum TerrainTileType : byte
{
    Grass = 0,
    FlowerGrass = 1,
    Dirt = 2,
    CobblestoneRoad = 3,
    DirtRoad = 4,
    ShallowWater = 5,
    DeepWater = 6,
    CliffElevation = 7,
    Rubble = 8
}

/// <summary>
/// Individual terrain tile descriptor in the battlefield grid.
/// </summary>
public readonly record struct TerrainTile(
    TerrainTileType Type,
    int Elevation,
    float MovementMultiplier,
    bool IsPassable,
    byte AutoTileBitmask,
    byte VariationSeed);

/// <summary>
/// Multi-layered 2D terrain grid supporting auto-tiling bitmasks, military roads with speed multipliers,
/// animated shoreline wave foam, and impassable cliff/water boundaries.
/// </summary>
public sealed class TerrainTileGrid
{
    private readonly TerrainTileType[] _tiles;
    private readonly byte[] _bitmasks;
    private readonly byte[] _variationSeeds;
    private readonly int _width;
    private readonly int _height;
    private readonly float _tileSize;
    private float _wavePhase;

    public int Width => _width;
    public int Height => _height;
    public float TileSize => _tileSize;
    public float WavePhase => _wavePhase;

    public TerrainTileGrid(int width = 100, int height = 100, float tileSize = 2.0f, int seed = 42)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (tileSize <= 0f) throw new ArgumentOutOfRangeException(nameof(tileSize));

        _width = width;
        _height = height;
        _tileSize = tileSize;
        _tiles = new TerrainTileType[width * height];
        _bitmasks = new byte[width * height];
        _variationSeeds = new byte[width * height];
        _wavePhase = 0f;

        var rng = new Random(seed);
        for (int i = 0; i < _tiles.Length; i++)
        {
            _variationSeeds[i] = (byte)rng.Next(0, 256);
            _tiles[i] = (_variationSeeds[i] % 12 == 0) ? TerrainTileType.FlowerGrass : TerrainTileType.Grass;
        }

        RecomputeAllBitmasks();
    }

    public TerrainTileType GetTile(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return TerrainTileType.DeepWater;
        return _tiles[(y * _width) + x];
    }

    public void SetTile(int x, int y, TerrainTileType type)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;
        _tiles[(y * _width) + x] = type;
        UpdateNeighborBitmasks(x, y);
    }

    public TerrainTile GetTileAtWorld(Vector2D worldPos)
    {
        int x = (int)Math.Floor(worldPos.X / _tileSize);
        int y = (int)Math.Floor(worldPos.Y / _tileSize);
        return GetTileInfo(x, y);
    }

    public TerrainTile GetTileInfo(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
        {
            return new TerrainTile(
                TerrainTileType.DeepWater,
                Elevation: 0,
                MovementMultiplier: 0.0f,
                IsPassable: false,
                AutoTileBitmask: 0,
                VariationSeed: 0);
        }

        int index = (y * _width) + x;
        var type = _tiles[index];
        float speedMult = GetMovementMultiplierForType(type);
        bool passable = IsPassableType(type);
        int elevation = type == TerrainTileType.CliffElevation ? 1 : (type == TerrainTileType.DeepWater ? -1 : 0);

        return new TerrainTile(
            type,
            Elevation: elevation,
            MovementMultiplier: speedMult,
            IsPassable: passable,
            AutoTileBitmask: _bitmasks[index],
            VariationSeed: _variationSeeds[index]);
    }

    public float GetMovementMultiplier(Vector2D worldPos)
    {
        var tile = GetTileAtWorld(worldPos);
        return tile.MovementMultiplier;
    }

    public bool IsPassable(Vector2D worldPos)
    {
        var tile = GetTileAtWorld(worldPos);
        return tile.IsPassable;
    }

    public static float GetMovementMultiplierForType(TerrainTileType type) => type switch
    {
        TerrainTileType.Grass => 1.0f,
        TerrainTileType.FlowerGrass => 1.0f,
        TerrainTileType.Dirt => 1.05f,
        TerrainTileType.CobblestoneRoad => 1.25f,
        TerrainTileType.DirtRoad => 1.15f,
        TerrainTileType.ShallowWater => 0.40f,
        TerrainTileType.DeepWater => 0.0f,
        TerrainTileType.CliffElevation => 0.0f,
        TerrainTileType.Rubble => 0.75f,
        _ => 1.0f
    };

    public static bool IsPassableType(TerrainTileType type) => type switch
    {
        TerrainTileType.DeepWater => false,
        TerrainTileType.CliffElevation => false,
        _ => true
    };

    /// <summary>
    /// Computes 4-bit cardinal neighbor auto-tile bitmask (North=1, East=2, South=4, West=8).
    /// </summary>
    public byte ComputeAutoTileBitmask(int x, int y, TerrainTileType targetType)
    {
        byte mask = 0;
        if (IsMatchingNeighbor(x, y - 1, targetType)) mask |= 1; // North
        if (IsMatchingNeighbor(x + 1, y, targetType)) mask |= 2; // East
        if (IsMatchingNeighbor(x, y + 1, targetType)) mask |= 4; // South
        if (IsMatchingNeighbor(x - 1, y, targetType)) mask |= 8; // West
        return mask;
    }

    public void UpdateWaveTicks(float deltaPhase = 0.05f)
    {
        _wavePhase = (_wavePhase + deltaPhase) % 1.0f;
    }

    public void RecomputeAllBitmasks()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int index = (y * _width) + x;
                _bitmasks[index] = ComputeAutoTileBitmask(x, y, _tiles[index]);
            }
        }
    }

    private void UpdateNeighborBitmasks(int cx, int cy)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = cx + dx;
                int ny = cy + dy;
                if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                {
                    int index = (ny * _width) + nx;
                    _bitmasks[index] = ComputeAutoTileBitmask(nx, ny, _tiles[index]);
                }
            }
        }
    }

    private bool IsMatchingNeighbor(int x, int y, TerrainTileType targetType)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return false;
        var neighborType = _tiles[(y * _width) + x];
        if (neighborType == targetType) return true;

        // Roads connect to roads
        if ((targetType == TerrainTileType.CobblestoneRoad || targetType == TerrainTileType.DirtRoad) &&
            (neighborType == TerrainTileType.CobblestoneRoad || neighborType == TerrainTileType.DirtRoad))
        {
            return true;
        }

        // Water connects to water
        if ((targetType == TerrainTileType.ShallowWater || targetType == TerrainTileType.DeepWater) &&
            (neighborType == TerrainTileType.ShallowWater || neighborType == TerrainTileType.DeepWater))
        {
            return true;
        }

        return false;
    }
}
