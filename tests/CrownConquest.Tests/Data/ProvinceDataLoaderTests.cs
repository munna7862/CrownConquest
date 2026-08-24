using System.IO;
using CrownConquest.Data.Loaders;
using Xunit;

namespace CrownConquest.Tests.Data;

public sealed class ProvinceDataLoaderTests
{
    [Fact]
    public void LoadProvincesFromJson_ValidJson_ReturnsAllProvinces()
    {
        string json = @"[
            {
                ""id"": ""prov_1"",
                ""name"": ""Province Alpha"",
                ""posX"": 100.0,
                ""posY"": 150.0,
                ""connectedProvinceIds"": [""prov_2""],
                ""terrain"": ""Plains"",
                ""nodeType"": ""Settlement"",
                ""initialOwnerFaction"": ""Player"",
                ""garrisonDefenseBonus"": 1.2,
                ""goldYield"": 10,
                ""foodYield"": 15,
                ""woodYield"": 5,
                ""stoneYield"": 5,
                ""ironYield"": 0
            },
            {
                ""id"": ""prov_2"",
                ""name"": ""Province Beta"",
                ""posX"": 250.0,
                ""posY"": 150.0,
                ""connectedProvinceIds"": [""prov_1""],
                ""terrain"": ""Hills"",
                ""nodeType"": ""Fortress"",
                ""initialOwnerFaction"": ""Enemy"",
                ""garrisonDefenseBonus"": 1.35,
                ""goldYield"": 5,
                ""foodYield"": 5,
                ""woodYield"": 10,
                ""stoneYield"": 20,
                ""ironYield"": 25
            }
        ]";

        var result = DataLoader.LoadProvincesFromJson(json);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("prov_1", result.Value[0].Id);
        Assert.Equal("Province Alpha", result.Value[0].Name);
        Assert.Equal(1.2f, result.Value[0].GarrisonDefenseBonus);
        Assert.Single(result.Value[0].ConnectedProvinceIds);
    }

    [Fact]
    public void LoadProvincesFromFile_DefinitionsFile_LoadsSuccessfully()
    {
        string path = Path.Combine("..", "..", "..", "..", "..", "data", "definitions", "provinces.json");
        if (!File.Exists(path))
        {
            path = Path.Combine("data", "definitions", "provinces.json");
        }

        if (File.Exists(path))
        {
            var result = DataLoader.LoadProvincesFromFile(path);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Count >= 8);
        }
    }

    [Fact]
    public void LoadProvincesFromJson_EmptyJson_ReturnsFailure()
    {
        var result = DataLoader.LoadProvincesFromJson("[]");
        Assert.True(result.IsFailure);
        Assert.Equal("EMPTY_DATA", result.Error.Code);
    }

    [Fact]
    public void LoadProvincesFromJson_InvalidDefenseBonus_ReturnsFailure()
    {
        string json = @"[
            {
                ""id"": ""prov_invalid"",
                ""name"": ""Invalid Province"",
                ""garrisonDefenseBonus"": -0.5
            }
        ]";

        var result = DataLoader.LoadProvincesFromJson(json);
        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_PROVINCE_STATS", result.Error.Code);
    }
}
