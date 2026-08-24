using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// Minimap View Models & Presenter
// ─────────────────────────────────────────────────

/// <summary>
/// Describes a single blip on the minimap representing a unit.
/// </summary>
public readonly record struct MinimapUnitBlip(
    EntityId UnitId,
    float MinimapX,
    float MinimapY,
    int FactionColorIndex,
    MinimapBlipType BlipType,
    bool IsSelected);

/// <summary>
/// Describes a building marker on the minimap.
/// </summary>
public readonly record struct MinimapBuildingBlip(
    EntityId BuildingId,
    float MinimapX,
    float MinimapY,
    int FactionColorIndex,
    bool IsCompleted);

/// <summary>
/// Type of minimap blip for visual differentiation.
/// </summary>
public enum MinimapBlipType
{
    Infantry,
    Ranged,
    Cavalry,
    Siege,
    Hero,
    Worker
}

/// <summary>
/// Presenter that projects world-space entities onto a normalized minimap viewport.
/// Uses pre-allocated buffers for zero per-frame allocations.
/// </summary>
public sealed class MinimapPresenter
{
    private readonly float _worldWidth;
    private readonly float _worldHeight;

    private readonly MinimapUnitBlip[] _unitBlips;
    private readonly MinimapBuildingBlip[] _buildingBlips;
    private int _activeUnitBlipCount;
    private int _activeBuildingBlipCount;
    private readonly int _maxBlips;

    public int ActiveUnitBlipCount => _activeUnitBlipCount;
    public int ActiveBuildingBlipCount => _activeBuildingBlipCount;
    public float WorldWidth => _worldWidth;
    public float WorldHeight => _worldHeight;

    public MinimapPresenter(float worldWidth, float worldHeight, int maxBlips = 512)
    {
        _worldWidth = worldWidth > 0f ? worldWidth : 200f;
        _worldHeight = worldHeight > 0f ? worldHeight : 200f;
        _maxBlips = maxBlips;
        _unitBlips = new MinimapUnitBlip[maxBlips];
        _buildingBlips = new MinimapBuildingBlip[maxBlips];
        _activeUnitBlipCount = 0;
        _activeBuildingBlipCount = 0;
    }

    /// <summary>
    /// Projects a world position to normalized minimap coordinates [0, 1].
    /// </summary>
    public (float x, float y) ProjectToMinimap(Vector2D worldPosition)
    {
        float x = Math.Clamp(worldPosition.X / _worldWidth, 0f, 1f);
        float y = Math.Clamp(worldPosition.Y / _worldHeight, 0f, 1f);
        return (x, y);
    }

    /// <summary>
    /// Updates all minimap blips from current unit and building state.
    /// </summary>
    public void UpdateBlips(
        UnitEntity[] units,
        int unitCount,
        BuildingEntity[] buildings,
        int buildingCount,
        EntityId[] selectedUnitIds,
        int selectedCount)
    {
        _activeUnitBlipCount = 0;
        _activeBuildingBlipCount = 0;

        for (int i = 0; i < unitCount && _activeUnitBlipCount < _maxBlips; i++)
        {
            var unit = units[i];
            if (!unit.IsAlive) continue;

            var (mx, my) = ProjectToMinimap(unit.Position);
            bool isSelected = IsInArray(unit.Id, selectedUnitIds, selectedCount);
            int colorIndex = SelectionFeedbackPresenter.GetFactionColorIndex(unit.FactionId);
            var blipType = GetBlipType(unit);

            _unitBlips[_activeUnitBlipCount++] = new MinimapUnitBlip(
                unit.Id, mx, my, colorIndex, blipType, isSelected);
        }

        for (int i = 0; i < buildingCount && _activeBuildingBlipCount < _maxBlips; i++)
        {
            var building = buildings[i];
            var (mx, my) = ProjectToMinimap(building.Position);
            int colorIndex = SelectionFeedbackPresenter.GetFactionColorIndex(building.FactionId);

            _buildingBlips[_activeBuildingBlipCount++] = new MinimapBuildingBlip(
                building.Id, mx, my, colorIndex, building.IsConstructed);
        }
    }

    public MinimapUnitBlip GetUnitBlip(int index)
    {
        if (index < 0 || index >= _activeUnitBlipCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _unitBlips[index];
    }

    public MinimapBuildingBlip GetBuildingBlip(int index)
    {
        if (index < 0 || index >= _activeBuildingBlipCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _buildingBlips[index];
    }

    private static bool IsInArray(EntityId id, EntityId[] array, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (array[i] == id) return true;
        }
        return false;
    }

    private static MinimapBlipType GetBlipType(UnitEntity unit)
    {
        if (unit.IsHero) return MinimapBlipType.Hero;
        if (unit.IsWorker) return MinimapBlipType.Worker;
        return unit.Archetype switch
        {
            UnitArchetype.Cavalry => MinimapBlipType.Cavalry,
            UnitArchetype.Siege => MinimapBlipType.Siege,
            UnitArchetype.Archer => MinimapBlipType.Ranged,
            _ => MinimapBlipType.Infantry
        };
    }
}
