using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class PlacementGridTests
{
    [Fact]
    public void PlacementGrid_ValidAndInvalidPlacement()
    {
        // TC-S02-004: Validate grid placement against bounds, existing buildings, and resource nodes
        var grid = new PlacementGrid(cellSize: 1.0f);
        var bounds = new BattlefieldBounds(0f, 0f, 100f, 100f);

        var existingBuildings = new List<BuildingEntity>
        {
            new(new EntityId(1), new FactionId(1), "town_center", new Vector2D(50f, 50f), new Vector2D(4f, 4f), startsConstructed: true)
        };

        var existingNodes = new List<ResourceNodeEntity>
        {
            new(new EntityId(2), ResourceType.Wood, new Vector2D(20f, 20f), maxAmount: 300)
        };

        // 1. Valid placement on open grass
        bool canPlaceOpen = grid.CanPlace(
            new Vector2D(60f, 60f),
            new Vector2D(3f, 3f),
            existingBuildings,
            existingNodes,
            bounds);
        Assert.True(canPlaceOpen);

        // 2. Invalid: Overlaps existing Town Center
        bool canPlaceOverlapBuilding = grid.CanPlace(
            new Vector2D(51f, 50f),
            new Vector2D(3f, 3f),
            existingBuildings,
            existingNodes,
            bounds);
        Assert.False(canPlaceOverlapBuilding);

        // 3. Invalid: Overlaps resource tree
        bool canPlaceOverlapTree = grid.CanPlace(
            new Vector2D(20.5f, 20.5f),
            new Vector2D(2f, 2f),
            existingBuildings,
            existingNodes,
            bounds);
        Assert.False(canPlaceOverlapTree);

        // 4. Invalid: Out of map bounds
        bool canPlaceOutOfBounds = grid.CanPlace(
            new Vector2D(1f, 1f),
            new Vector2D(4f, 4f),
            existingBuildings,
            existingNodes,
            bounds);
        Assert.False(canPlaceOutOfBounds);
    }

    [Fact]
    public void PlacementGrid_SnapToGrid_CorrectCoordinates()
    {
        var grid = new PlacementGrid(cellSize: 1.0f);

        var rawPos = new Vector2D(12.34f, 45.89f);
        var snapped = grid.SnapToGrid(rawPos);

        Assert.Equal(12.0f, snapped.X);
        Assert.Equal(46.0f, snapped.Y);
    }
}
