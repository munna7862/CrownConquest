using System;
using CrownConquest.Application;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;

namespace CrownConquest.Presentation;

public readonly record struct PlacementPreviewResult(
    Vector2D SnappedPosition,
    Vector2D GridSize,
    Rect2D FootprintBox,
    bool CanAfford,
    bool IsGridValid,
    bool IsValid,
    string StatusMessage);

/// <summary>
/// Evaluates visual ghost preview and grid validation during building placement mode.
/// </summary>
public sealed class BuildingPlacementPreview
{
    private readonly GameCoordinator _coordinator;

    public BuildingPlacementPreview(GameCoordinator coordinator)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    public PlacementPreviewResult Evaluate(
        FactionId factionId,
        string buildingType,
        Vector2D worldCursorPosition,
        Vector2D gridSize,
        ResourceCost cost)
    {
        var state = _coordinator.Simulation.State;
        var snappedPos = state.PlacementGrid.SnapToGrid(worldCursorPosition);
        var footprint = state.PlacementGrid.CalculateBoundingBox(snappedPos, gridSize);

        bool isGridValid = state.PlacementGrid.CanPlace(
            snappedPos,
            gridSize,
            state.ActiveBuildings,
            state.ActiveResourceNodes,
            _coordinator.Simulation.Bounds);

        var bank = _coordinator.GetResourceBank(factionId);
        bool canAfford = bank.CanAfford(cost);

        bool isValid = isGridValid && canAfford;
        string message = isValid ? "Valid Placement" :
            !canAfford ? "Insufficient Resources" : "Terrain or Building Obstruction";

        return new PlacementPreviewResult(
            SnappedPosition: snappedPos,
            GridSize: gridSize,
            FootprintBox: footprint,
            CanAfford: canAfford,
            IsGridValid: isGridValid,
            IsValid: isValid,
            StatusMessage: message);
    }

    public static (Vector2D GridSize, ResourceCost Cost, int PopProvided, string DisplayName) GetBlueprintConfig(string buildingType)
    {
        return buildingType.ToLowerInvariant() switch
        {
            "house" => (new Vector2D(2f, 2f), new ResourceCost(Wood: 50), 5, "House"),
            "barracks" => (new Vector2D(3f, 3f), new ResourceCost(Wood: 150), 0, "Barracks"),
            "blacksmith" => (new Vector2D(3f, 3f), new ResourceCost(Wood: 150, Stone: 50), 0, "Blacksmith"),
            "watchtower" or "tower" => (new Vector2D(2f, 2f), new ResourceCost(Wood: 50, Stone: 125), 0, "Watchtower"),
            "farm" => (new Vector2D(2f, 2f), new ResourceCost(Wood: 60), 0, "Farm"),
            "stables" or "stable" => (new Vector2D(3f, 3f), new ResourceCost(Wood: 150, Gold: 50), 0, "Stables"),
            _ => (new Vector2D(2f, 2f), new ResourceCost(Wood: 100), 0, "Building")
        };
    }
}
