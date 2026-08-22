using System.IO;
using CrownConquest.Data;
using CrownConquest.Data.Loaders;
using CrownConquest.Domain.Common;
using Xunit;

namespace CrownConquest.Tests.Data;

public class UnitFactoryTests
{
    [Fact]
    public void UnitDefinition_Validation_ShouldLoadAllDefinitionsWithValidStats()
    {
        string json = @"[
          {
            ""id"": ""celtic_swordsman"",
            ""displayName"": ""Celtic Swordsman"",
            ""faction"": ""celtic"",
            ""maxHealth"": 120.0,
            ""attackDamage"": 18.0,
            ""armor"": 3.0,
            ""attackRange"": 1.5,
            ""attackType"": ""melee"",
            ""movementSpeed"": 3.6,
            ""attackCooldownTicks"": 18,
            ""killXpValue"": 60,
            ""aggroRange"": 10.0,
            ""xpCurveId"": ""standard_infantry_curve""
          },
          {
            ""id"": ""celtic_archer"",
            ""displayName"": ""Celtic Archer"",
            ""faction"": ""celtic"",
            ""maxHealth"": 80.0,
            ""attackDamage"": 14.0,
            ""armor"": 1.0,
            ""attackRange"": 8.0,
            ""attackType"": ""ranged"",
            ""movementSpeed"": 3.8,
            ""attackCooldownTicks"": 22,
            ""killXpValue"": 50,
            ""aggroRange"": 12.0,
            ""xpCurveId"": ""ranged_unit_curve""
          }
        ]";

        var result = DataLoader.LoadUnitsFromJson(json);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        var factory = new UnitFactory(result.Value);
        var unitResult = factory.CreateUnit(new EntityId(1), FactionId.Player1, "celtic_swordsman", new Vector2D(10f, 10f));

        Assert.True(unitResult.IsSuccess);
        var unit = unitResult.Value;
        Assert.Equal("celtic_swordsman", unit.UnitType);
        Assert.Equal(120f, unit.BaseMaxHealth);
        Assert.Equal(18f, unit.BaseAttackDamage);
        Assert.Equal(3f, unit.BaseArmor);
        Assert.Equal(1.5f, unit.AttackRange);
        Assert.Equal("melee", unit.AttackType);
    }
}
