using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Authoritative deterministic 2D terrain grid supporting fast cell lookups and spatial terrain queries.
/// Allocation-free during runtime gameplay ticks.
/// </summary>
public sealed class TerrainGrid
{
    private readonly TerrainType[] _grid;
    public int Width { get; }
    public int Height { get; }
    public float CellSize { get; }
    public float HalfWidth => (Width * CellSize) * 0.5f;
    public float HalfHeight => (Height * CellSize) * 0.5f;

    public TerrainGrid(int width = 64, int height = 64, float cellSize = 1.0f, TerrainType defaultTerrain = TerrainType.Plains)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        CellSize = Math.Max(0.1f, cellSize);
        _grid = new TerrainType[Width * Height];

        if (defaultTerrain != TerrainType.Plains)
        {
            Array.Fill(_grid, defaultTerrain);
        }
    }

    public (int X, int Y) WorldToGrid(Vector2D worldPos)
    {
        int gx = (int)MathF.Floor((worldPos.X + HalfWidth) / CellSize);
        int gy = (int)MathF.Floor((worldPos.Y + HalfHeight) / CellSize);
        return (Math.Clamp(gx, 0, Width - 1), Math.Clamp(gy, 0, Height - 1));
    }

    public Vector2D GridToWorld(int gx, int gy)
    {
        float wx = ((gx + 0.5f) * CellSize) - HalfWidth;
        float wy = ((gy + 0.5f) * CellSize) - HalfHeight;
        return new Vector2D(wx, wy);
    }

    public bool IsInBounds(int gx, int gy)
    {
        return gx >= 0 && gx < Width && gy >= 0 && gy < Height;
    }

    public TerrainType GetTerrain(int gx, int gy)
    {
        if (!IsInBounds(gx, gy)) return TerrainType.Plains;
        return _grid[(gy * Width) + gx];
    }

    public TerrainType GetTerrainAt(Vector2D worldPos)
    {
        var (gx, gy) = WorldToGrid(worldPos);
        return GetTerrain(gx, gy);
    }

    public TerrainModifiers GetModifiersAt(Vector2D worldPos)
    {
        var type = GetTerrainAt(worldPos);
        return TerrainModifiers.GetDefault(type);
    }

    public void SetTerrain(int gx, int gy, TerrainType type)
    {
        if (IsInBounds(gx, gy))
        {
            _grid[(gy * Width) + gx] = type;
        }
    }

    public void SetTerrainRect(int startX, int startY, int width, int height, TerrainType type)
    {
        int endX = Math.Min(Width, startX + width);
        int endY = Math.Min(Height, startY + height);
        int sx = Math.Max(0, startX);
        int sy = Math.Max(0, startY);

        for (int y = sy; y < endY; y++)
        {
            int rowOffset = y * Width;
            for (int x = sx; x < endX; x++)
            {
                _grid[rowOffset + x] = type;
            }
        }
    }
}
