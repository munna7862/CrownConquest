using System.IO;
using CrownConquest.Data.Loaders;
using Xunit;

namespace CrownConquest.Tests.Data;

public sealed class AiPersonalityDataLoaderTests
{
    [Fact]
    public void TC_S09_06_LoadAiPersonalitiesFromJson_ParsesValidProfiles()
    {
        string json = @"[
            {
                ""Id"": ""aggressive"",
                ""Name"": ""Aggressive Raider"",
                ""Archetype"": ""Aggressive"",
                ""Description"": ""Early rush"",
                ""RetreatOddsThreshold"": 0.25,
                ""RetreatHealthThreshold"": 0.20,
                ""TargetWorkerCount"": 12,
                ""AttackSquadThreshold"": 6,
                ""FlankingDesire"": 1.0,
                ""ElevationBias"": 1.0,
                ""HeroPreservation"": false,
                ""PreferredFormation"": ""Wedge"",
                ""BaseDefenseRadius"": 20.0
            }
        ]";

        var result = DataLoader.LoadAiPersonalitiesFromJson(json);
        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("aggressive", result.Value[0].Id);
        Assert.Equal(0.25f, result.Value[0].RetreatOddsThreshold);
    }

    [Fact]
    public void TC_S09_06_LoadAiPersonalitiesFromFile_LoadsDataFileSuccessfully()
    {
        string path = Path.Combine("..", "..", "..", "..", "..", "data", "definitions", "ai_personalities.json");
        if (!File.Exists(path))
        {
            path = Path.Combine("data", "definitions", "ai_personalities.json");
        }

        if (File.Exists(path))
        {
            var result = DataLoader.LoadAiPersonalitiesFromFile(path);
            Assert.True(result.IsSuccess, result.Error.Message);
            Assert.NotNull(result.Value);
            Assert.Equal(4, result.Value.Count);
        }
    }

    [Fact]
    public void TC_S09_06_LoadAiPersonalitiesFromJson_RejectsEmptyOrInvalidData()
    {
        var emptyRes = DataLoader.LoadAiPersonalitiesFromJson("[]");
        Assert.False(emptyRes.IsSuccess);

        string invalidStatsJson = @"[{ ""Id"": ""bad"", ""RetreatOddsThreshold"": 2.5 }]";
        var invalidRes = DataLoader.LoadAiPersonalitiesFromJson(invalidStatsJson);
        Assert.False(invalidRes.IsSuccess);
    }
}
