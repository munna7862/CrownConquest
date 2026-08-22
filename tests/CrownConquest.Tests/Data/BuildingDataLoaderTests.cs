using System.IO;
using CrownConquest.Data.Loaders;
using Xunit;

namespace CrownConquest.Tests.Data;

public class BuildingDataLoaderTests
{
    [Fact]
    public void BuildingDefinition_Validation()
    {
        // TC-S02-007: Load and validate building definitions from JSON
        string buildingJsonPath = Path.Combine("..", "..", "..", "..", "..", "data", "definitions", "buildings.json");
        if (!File.Exists(buildingJsonPath))
        {
            buildingJsonPath = Path.Combine("data", "definitions", "buildings.json");
        }

        var result = DataLoader.LoadBuildingsFromFile(buildingJsonPath);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value);

        // Verify Town Center definition
        var tc = result.Value.Find(b => b.Id == "town_center");
        Assert.NotNull(tc);
        Assert.Equal(4.0f, tc.GridWidth);
        Assert.Equal(4.0f, tc.GridHeight);
        Assert.Equal(1200.0f, tc.MaxHealth);
        Assert.Equal(10, tc.PopulationProvided);
        Assert.Equal(275, tc.WoodCost);
        Assert.Equal(100, tc.StoneCost);
        Assert.Contains("Food", tc.AcceptedDropOffs);
        Assert.Contains("Wood", tc.AcceptedDropOffs);

        // Verify Barracks definition
        var barracks = result.Value.Find(b => b.Id == "barracks");
        Assert.NotNull(barracks);
        Assert.Equal(3.0f, barracks.GridWidth);
        Assert.Equal(150, barracks.WoodCost);

        // Verify House definition
        var house = result.Value.Find(b => b.Id == "house");
        Assert.NotNull(house);
        Assert.Equal(5, house.PopulationProvided);
        Assert.Equal(50, house.WoodCost);
    }

    [Fact]
    public void ResourceNodeDefinition_Validation()
    {
        string resourceJsonPath = Path.Combine("..", "..", "..", "..", "..", "data", "definitions", "resources.json");
        if (!File.Exists(resourceJsonPath))
        {
            resourceJsonPath = Path.Combine("data", "definitions", "resources.json");
        }

        var result = DataLoader.LoadResourcesFromFile(resourceJsonPath);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value);

        var tree = result.Value.Find(r => r.Id == "tree");
        Assert.NotNull(tree);
        Assert.Equal("Wood", tree.ResourceType);
        Assert.True(tree.MaxAmount > 0);

        var gold = result.Value.Find(r => r.Id == "gold_mine");
        Assert.NotNull(gold);
        Assert.Equal("Gold", gold.ResourceType);
        Assert.True(gold.MaxAmount > 0);
    }
}
