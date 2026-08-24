using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.World;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class StrategicCampaignInvariantTests
{
    [Fact]
    public void MovementInvariant_ArmyArrivesAtDestinationOnExactTick()
    {
        var prov1 = new StrategicProvince(new ProvinceId("p1"), "P1", new Vector2D(0, 0), connectedProvinceIds: new[] { new ProvinceId("p2") });
        var prov2 = new StrategicProvince(new ProvinceId("p2"), "P2", new Vector2D(100, 0), connectedProvinceIds: new[] { new ProvinceId("p1") });

        var map = new StrategicMap(new[] { prov1, prov2 });
        var engine = new CampaignEngine(map);

        var army = new StrategicArmy(
            id: new StrategicArmyId(1),
            factionId: FactionId.Player,
            name: "Vanguard",
            startingProvinceId: new ProvinceId("p1"),
            units: new[] { new StrategicUnitSpec { UnitType = "Infantry", Level = 1 } }
        );
        engine.RegisterArmy(army);

        int travelDuration = StrategicMovementCalculator.CalculateTravelTicks(prov1.Position, prov2.Position, prov2.Terrain, army.BaseMovementSpeed);

        var moveResult = engine.OrderArmyMove(army.Id, new ProvinceId("p2"));
        Assert.True(moveResult.IsSuccess);
        Assert.True(army.IsInTransit);
        Assert.Equal(travelDuration, army.MovementTicksRemaining);

        // Advance ticks up to right before arrival
        for (int t = 0; t < travelDuration - 1; t++)
        {
            engine.AdvanceTick();
            Assert.True(army.IsInTransit);
        }

        // Final tick arrival
        engine.AdvanceTick();
        Assert.False(army.IsInTransit);
        Assert.Equal(new ProvinceId("p2"), army.CurrentProvinceId);
    }

    [Fact]
    public void MultiWaypointInvariant_TraversesConnectedSequenceWithoutSkipping()
    {
        var p1 = new StrategicProvince(new ProvinceId("p1"), "P1", new Vector2D(0, 0), connectedProvinceIds: new[] { new ProvinceId("p2") });
        var p2 = new StrategicProvince(new ProvinceId("p2"), "P2", new Vector2D(50, 0), connectedProvinceIds: new[] { new ProvinceId("p1"), new ProvinceId("p3") });
        var p3 = new StrategicProvince(new ProvinceId("p3"), "P3", new Vector2D(100, 0), connectedProvinceIds: new[] { new ProvinceId("p2") });

        var map = new StrategicMap(new[] { p1, p2, p3 });
        var engine = new CampaignEngine(map);

        var army = new StrategicArmy(
            id: new StrategicArmyId(1),
            factionId: FactionId.Player,
            name: "Marchers",
            startingProvinceId: new ProvinceId("p1"),
            units: new[] { new StrategicUnitSpec { UnitType = "Infantry", Level = 1 } }
        );
        engine.RegisterArmy(army);

        engine.OrderArmyMove(army.Id, new ProvinceId("p3"));

        // Step until finished
        for (int t = 0; t < 200; t++)
        {
            engine.AdvanceTick();
            if (army.CurrentProvinceId == new ProvinceId("p3") && !army.IsInTransit)
            {
                break;
            }
        }

        Assert.Equal(new ProvinceId("p3"), army.CurrentProvinceId);
        Assert.False(army.IsInTransit);
    }

    [Fact]
    public void SurvivorProgressionRetentionInvariant_CasualtiesRemovedAndSurvivorsKeepXPAndLevel()
    {
        var prov = new StrategicProvince(
            id: new ProvinceId("p_battle"),
            name: "Battlefield",
            position: new Vector2D(0, 0),
            ownerFaction: FactionId.Enemy,
            garrisonDefenseBonus: 1.0f
        );

        // Weak garrison unit
        prov.GarrisonUnits.Add(new StrategicUnitSpec
        {
            UnitType = "Militia",
            Archetype = UnitArchetype.Infantry,
            BaseMaxHealth = 30f,
            CurrentHealth = 30f,
            BaseAttackDamage = 5f,
            Armor = 0f,
            Level = 1
        });

        // Strong attacker unit
        var strongAttacker = new StrategicUnitSpec
        {
            UnitType = "Knight",
            Archetype = UnitArchetype.Cavalry,
            BaseMaxHealth = 300f,
            CurrentHealth = 300f,
            BaseAttackDamage = 35f,
            Armor = 5f,
            Level = 1,
            CurrentXp = 0
        };

        var attackerArmy = new StrategicArmy(
            id: new StrategicArmyId(1),
            factionId: FactionId.Player,
            name: "Royal Knights",
            startingProvinceId: new ProvinceId("p_battle"),
            units: new[] { strongAttacker }
        );

        var setup = new BattleSetup(attackerArmy, prov);
        var result = BattleTransitionEngine.ExecuteBattle(setup, maxTicks: 500);

        Assert.True(result.AttackerWon);
        Assert.True(result.ProvinceCaptured);
        Assert.Equal(FactionId.Player, prov.OwnerFaction);
        Assert.Single(attackerArmy.Units);
        Assert.True(attackerArmy.Units[0].TotalKills >= 1);
        Assert.True(attackerArmy.Units[0].CurrentXp > 0);
    }

    [Fact]
    public void TotalDefeatInvariant_ArmyWithTotalCasualtiesIsDestroyedCleanly()
    {
        var prov = new StrategicProvince(
            id: new ProvinceId("p_fortress"),
            name: "High Fortress",
            position: new Vector2D(0, 0),
            ownerFaction: FactionId.Enemy,
            garrisonDefenseBonus: 1.5f
        );

        // Huge defender garrison
        prov.GarrisonUnits.Add(new StrategicUnitSpec
        {
            UnitType = "EliteGuard",
            Archetype = UnitArchetype.Infantry,
            BaseMaxHealth = 500f,
            CurrentHealth = 500f,
            BaseAttackDamage = 60f,
            Armor = 10f,
            Level = 5
        });

        var weakAttacker = new StrategicArmy(
            id: new StrategicArmyId(10),
            factionId: FactionId.Player,
            name: "Doomed Scouts",
            startingProvinceId: new ProvinceId("p_fortress"),
            units: new[] { new StrategicUnitSpec { UnitType = "Peasant", BaseMaxHealth = 20f, CurrentHealth = 20f, BaseAttackDamage = 2f, Armor = 0f } }
        );

        var map = new StrategicMap(new[] { prov });
        var engine = new CampaignEngine(map);
        engine.RegisterArmy(weakAttacker);

        var setup = new BattleSetup(weakAttacker, prov);
        var result = BattleTransitionEngine.ExecuteBattle(setup, maxTicks: 500);

        Assert.False(result.AttackerWon);
        Assert.Empty(weakAttacker.Units);
        Assert.False(weakAttacker.HasUnits);
    }
}
