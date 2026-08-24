using System;
using System.IO;
using CrownConquest.Data.Loaders;
using Xunit;

namespace CrownConquest.Tests.Data;

public sealed class TacticalDataLoaderTests
{
    [Fact]
    public void TC_S06_18_DataLoader_LoadsTerrainDefinitionsSuccessfully()
    {
        string baseDir = AppContext.BaseDirectory;
        string searchDir = baseDir;
        while (!Directory.Exists(Path.Combine(searchDir, "data", "definitions")) && Directory.GetParent(searchDir) != null)
        {
            searchDir = Directory.GetParent(searchDir)!.FullName;
        }

        string path = Path.Combine(searchDir, "data", "definitions", "terrain.json");
        var result = DataLoader.LoadTerrainFromFile(path);
        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Count >= 5);

        var plains = result.Value.Find(t => t.Id == "plains");
        Assert.NotNull(plains);
        Assert.Equal(1.0f, plains.MovementSpeedMultiplier);

        var forest = result.Value.Find(t => t.Id == "forest");
        Assert.NotNull(forest);
        Assert.Equal(0.35f, forest.RangedCoverMitigation);

        var hills = result.Value.Find(t => t.Id == "hills");
        Assert.NotNull(hills);
        Assert.Equal(1, hills.ElevationLevel);
    }

    [Fact]
    public void TC_S06_18_DataLoader_LoadsFormationsDefinitionsSuccessfully()
    {
        string baseDir = AppContext.BaseDirectory;
        string searchDir = baseDir;
        while (!Directory.Exists(Path.Combine(searchDir, "data", "definitions")) && Directory.GetParent(searchDir) != null)
        {
            searchDir = Directory.GetParent(searchDir)!.FullName;
        }

        string path = Path.Combine(searchDir, "data", "definitions", "formations.json");
        var result = DataLoader.LoadFormationsFromFile(path);
        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Count >= 5);

        var shieldWall = result.Value.Find(f => f.Id == "shield_wall");
        Assert.NotNull(shieldWall);
        Assert.Equal(4.0f, shieldWall.ArmorBonus);
        Assert.True(shieldWall.CanBraceCavalry);

        var wedge = result.Value.Find(f => f.Id == "wedge");
        Assert.NotNull(wedge);
        Assert.Equal(1.30f, wedge.ChargeDamageMultiplier);
    }
}
