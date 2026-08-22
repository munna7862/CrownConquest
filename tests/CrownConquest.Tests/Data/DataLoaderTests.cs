using CrownConquest.Data.Loaders;
using Xunit;

namespace CrownConquest.Tests.Data;

public class DataLoaderTests
{
    [Fact]
    public void DataLoader_UnitsJson_ShouldLoadAndValidate()
    {
        string json = @"
        [
          {
            ""id"": ""test_warrior"",
            ""displayName"": ""Test Warrior"",
            ""faction"": ""celtic"",
            ""maxHealth"": 100.0,
            ""attackDamage"": 15.0,
            ""attackRange"": 1.5,
            ""movementSpeed"": 3.5,
            ""attackCooldownTicks"": 20,
            ""killXpValue"": 50,
            ""goldCost"": 50,
            ""foodCost"": 30,
            ""trainingTicks"": 80
          }
        ]";

        var result = DataLoader.LoadUnitsFromJson(json);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("test_warrior", result.Value[0].Id);
        Assert.Equal(100.0f, result.Value[0].MaxHealth);
    }

    [Fact]
    public void DataLoader_ProgressionCurves_ShouldRejectNonMonotonicThresholds()
    {
        string invalidJson = @"
        [
          {
            ""id"": ""broken_curve"",
            ""levelXpThresholds"": [0, 100, 80, 200],
            ""healthPerLevelBonus"": 15.0,
            ""damagePerLevelBonus"": 2.5
          }
        ]";

        var result = DataLoader.LoadProgressionCurvesFromJson(invalidJson);

        Assert.False(result.IsSuccess);
        Assert.Equal("NON_MONOTONIC_THRESHOLDS", result.Error.Code);
    }
}
