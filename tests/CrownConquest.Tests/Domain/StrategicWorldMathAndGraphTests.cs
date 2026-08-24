using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.World;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class StrategicWorldMathAndGraphTests
{
    [Fact]
    public void StrategicMap_AddProvincesAndFindPath_ReturnsCorrectSequentialPath()
    {
        var provA = new StrategicProvince(
            id: new ProvinceId("prov_a"),
            name: "Province A",
            position: new Vector2D(0f, 0f),
            connectedProvinceIds: new[] { new ProvinceId("prov_b") }
        );

        var provB = new StrategicProvince(
            id: new ProvinceId("prov_b"),
            name: "Province B",
            position: new Vector2D(100f, 0f),
            connectedProvinceIds: new[] { new ProvinceId("prov_a"), new ProvinceId("prov_c") }
        );

        var provC = new StrategicProvince(
            id: new ProvinceId("prov_c"),
            name: "Province C",
            position: new Vector2D(200f, 0f),
            connectedProvinceIds: new[] { new ProvinceId("prov_b") }
        );

        var map = new StrategicMap(new[] { provA, provB, provC });

        var path = map.FindPath("prov_a", "prov_c");

        Assert.Equal(2, path.Count);
        Assert.Equal(new ProvinceId("prov_b"), path[0]);
        Assert.Equal(new ProvinceId("prov_c"), path[1]);
    }

    [Fact]
    public void StrategicArmy_CombatPower_ComputesWeightedTotalPower()
    {
        var units = new List<StrategicUnitSpec>
        {
            new()
            {
                UnitType = "Infantry",
                BaseMaxHealth = 100f,
                CurrentHealth = 100f,
                BaseAttackDamage = 15f,
                Armor = 2f,
                Level = 1
            },
            new()
            {
                UnitType = "Archer",
                BaseMaxHealth = 80f,
                CurrentHealth = 80f,
                BaseAttackDamage = 20f,
                Armor = 1f,
                Level = 2
            }
        };

        var hero = new StrategicHeroSpec
        {
            HeroName = "Champion",
            Class = HeroClass.Warlord,
            BaseAttributes = new HeroAttributes(10, 10, 10),
            Level = 3
        };

        var army = new StrategicArmy(
            id: new StrategicArmyId(1),
            factionId: FactionId.Player,
            name: "First Legion",
            startingProvinceId: new ProvinceId("prov_a"),
            units: units,
            hero: hero
        );

        Assert.True(army.TotalCombatPower > 500f);
        Assert.Equal(2, army.UnitCount);
        Assert.NotNull(army.AttachedHero);
    }

    [Fact]
    public void StrategicMovementCalculator_TerrainModifiers_ScalesTravelDuration()
    {
        var startPos = new Vector2D(0f, 0f);
        var endPos = new Vector2D(100f, 0f);

        int plainsTicks = StrategicMovementCalculator.CalculateTravelTicks(startPos, endPos, TerrainType.Plains, armySpeed: 50f);
        int marshTicks = StrategicMovementCalculator.CalculateTravelTicks(startPos, endPos, TerrainType.Marsh, armySpeed: 50f);
        int roadTicks = StrategicMovementCalculator.CalculateTravelTicks(startPos, endPos, TerrainType.Road, armySpeed: 50f);

        // Road speed is faster -> takes fewer or equal ticks than Plains
        Assert.True(roadTicks <= plainsTicks);
        // Marsh is slow -> takes more ticks than Plains
        Assert.True(marshTicks > plainsTicks);
    }

    [Fact]
    public void StrategicTerritoryManager_TracksOwnershipAndDistribution()
    {
        var prov1 = new StrategicProvince(new ProvinceId("p1"), "P1", new Vector2D(0, 0), ownerFaction: FactionId.Player);
        var prov2 = new StrategicProvince(new ProvinceId("p2"), "P2", new Vector2D(10, 0), ownerFaction: FactionId.Player);
        var prov3 = new StrategicProvince(new ProvinceId("p3"), "P3", new Vector2D(20, 0), ownerFaction: FactionId.Enemy);
        var prov4 = new StrategicProvince(new ProvinceId("p4"), "P4", new Vector2D(30, 0), ownerFaction: FactionId.Neutral);

        var map = new StrategicMap(new[] { prov1, prov2, prov3, prov4 });
        var manager = new StrategicTerritoryManager(map);

        Assert.Equal(2, manager.GetControlledProvinceCount(FactionId.Player));
        Assert.Equal(1, manager.GetControlledProvinceCount(FactionId.Enemy));
        Assert.Equal(0.5f, manager.GetControlPercentage(FactionId.Player));

        // Transfer ownership
        manager.TransferOwnership(new ProvinceId("p3"), FactionId.Player);
        Assert.Equal(3, manager.GetControlledProvinceCount(FactionId.Player));
        Assert.Equal(0.75f, manager.GetControlPercentage(FactionId.Player));
    }
}
