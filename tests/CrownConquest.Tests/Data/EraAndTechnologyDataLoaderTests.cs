using System;
using System.IO;
using CrownConquest.Data.Loaders;
using Xunit;

namespace CrownConquest.Tests.Data;

public sealed class EraAndTechnologyDataLoaderTests
{
    [Fact]
    public void Data_Loaders_ErasAndTechnologies_Validation()
    {
        // TC-S04-018: DataLoader parses eras.json and technologies.json definitions
        string baseDir = AppContext.BaseDirectory;
        string erasPath = Path.Combine(baseDir, "..", "..", "..", "..", "..", "data", "definitions", "eras.json");
        string techsPath = Path.Combine(baseDir, "..", "..", "..", "..", "..", "data", "definitions", "technologies.json");

        if (File.Exists(erasPath))
        {
            var erasResult = DataLoader.LoadErasFromFile(erasPath);
            Assert.True(erasResult.IsSuccess, $"Failed to load eras: {erasResult.Error.Message}");
            var eras = erasResult.Value!;
            Assert.Equal(4, eras.Count);
            Assert.Equal("archaic", eras[0].Id);
            Assert.Equal("classical", eras[1].Id);
            Assert.Equal("imperial", eras[2].Id);
            Assert.Equal("feudal", eras[3].Id);
        }

        if (File.Exists(techsPath))
        {
            var techsResult = DataLoader.LoadTechnologiesFromFile(techsPath);
            Assert.True(techsResult.IsSuccess, $"Failed to load technologies: {techsResult.Error.Message}");
            var techs = techsResult.Value!;
            Assert.True(techs.Count >= 6, "Expected at least 6 technology definitions.");

            var forging = techs.Find(t => t.Id == "forging");
            Assert.NotNull(forging);
            Assert.Equal(2, forging!.MeleeAttackBonus);
            Assert.Equal(1, forging.RequiredEra);

            var ironWeapons = techs.Find(t => t.Id == "iron_weapons");
            Assert.NotNull(ironWeapons);
            Assert.Contains("forging", ironWeapons!.RequiredTechIds);
            Assert.Equal(2, ironWeapons.RequiredEra);
        }
    }

    [Fact]
    public void DataLoader_Eras_FromJsonString()
    {
        string json = @"
        [
            {
                ""id"": ""archaic"",
                ""era"": 0,
                ""displayName"": ""Archaic"",
                ""durationTicks"": 0
            },
            {
                ""id"": ""classical"",
                ""era"": 1,
                ""displayName"": ""Classical"",
                ""durationTicks"": 100,
                ""foodCost"": 500
            }
        ]";

        var result = DataLoader.LoadErasFromJson(json);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("archaic", result.Value[0].Id);
        Assert.Equal(500, result.Value[1].FoodCost);
    }

    [Fact]
    public void DataLoader_Technologies_FromJsonString()
    {
        string json = @"
        [
            {
                ""id"": ""forging"",
                ""displayName"": ""Forging"",
                ""category"": ""Military"",
                ""requiredEra"": 1,
                ""foodCost"": 150,
                ""goldCost"": 50,
                ""researchDurationTicks"": 40,
                ""meleeAttackBonus"": 2
            }
        ]";

        var result = DataLoader.LoadTechnologiesFromJson(json);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("forging", result.Value[0].Id);
        Assert.Equal(2, result.Value[0].MeleeAttackBonus);
    }
}
