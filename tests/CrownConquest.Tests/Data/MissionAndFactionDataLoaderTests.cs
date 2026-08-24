using System.IO;
using CrownConquest.Data.Loaders;
using Xunit;

namespace CrownConquest.Tests.Data;

public class MissionAndFactionDataLoaderTests
{
    [Fact]
    public void LoadMissionsFromFile_ValidJson_ReturnsAllMissions()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "data", "definitions", "missions.json");
        if (!File.Exists(filePath))
        {
            filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "definitions", "missions.json"));
        }

        var result = DataLoader.LoadMissionsFromFile(filePath);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Count >= 5);

        var defend = result.Value.Find(m => m.Type == "Defend");
        Assert.NotNull(defend);
        Assert.Equal("mission_defend_ironhold", defend.Id);
        Assert.True(defend.DurationTicks > 0);

        var destroy = result.Value.Find(m => m.Type == "Destroy");
        Assert.NotNull(destroy);
        Assert.True(destroy.TargetQuantity > 0);

        var capture = result.Value.Find(m => m.Type == "Capture");
        Assert.NotNull(capture);

        var escort = result.Value.Find(m => m.Type == "Escort");
        Assert.NotNull(escort);
        Assert.NotNull(escort.DestinationProvinceId);

        var resource = result.Value.Find(m => m.Type == "ResourceControl");
        Assert.NotNull(resource);
        Assert.True(resource.RequiredGold > 0 || resource.RequiredIron > 0 || resource.RequiredFood > 0);
    }

    [Fact]
    public void LoadFactionsFromFile_ValidJson_ReturnsAllFactions()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "data", "definitions", "factions.json");
        if (!File.Exists(filePath))
        {
            filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "definitions", "factions.json"));
        }

        var result = DataLoader.LoadFactionsFromFile(filePath);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Count >= 5);

        var valoria = result.Value.Find(f => f.Id == "faction_valoria");
        Assert.NotNull(valoria);
        Assert.True(valoria.InitialReputation > 0);
        Assert.True(valoria.TradeModifier > 1.0);

        var ironfist = result.Value.Find(f => f.Id == "faction_ironfist");
        Assert.NotNull(ironfist);
        Assert.True(ironfist.InitialReputation < 0);
    }

    [Fact]
    public void LoadMissionsFromJson_InvalidData_ReturnsFailure()
    {
        string invalidJson = "[ { \"id\": \"\", \"durationTicks\": -5 } ]";
        var result = DataLoader.LoadMissionsFromJson(invalidJson);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void LoadFactionsFromJson_InvalidReputation_ReturnsFailure()
    {
        string invalidJson = "[ { \"id\": \"bad_faction\", \"initialReputation\": 500 } ]";
        var result = DataLoader.LoadFactionsFromJson(invalidJson);
        Assert.False(result.IsSuccess);
    }
}
