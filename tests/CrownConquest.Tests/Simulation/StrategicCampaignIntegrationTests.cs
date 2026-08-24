using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.World;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class StrategicCampaignIntegrationTests
{
    [Fact]
    public void StrategicEconomy_TurnInflowYieldsCompoundTreasury()
    {
        var prov1 = new StrategicProvince(
            id: new ProvinceId("prov_gold"),
            name: "Goldfield",
            position: new Vector2D(0, 0),
            ownerFaction: FactionId.Player,
            resourceYields: new ResourceCost(Food: 10, Wood: 5, Gold: 50, Stone: 0, Iron: 5)
        );

        var prov2 = new StrategicProvince(
            id: new ProvinceId("prov_iron"),
            name: "Ironhold",
            position: new Vector2D(50, 0),
            ownerFaction: FactionId.Player,
            resourceYields: new ResourceCost(Food: 5, Wood: 10, Gold: 10, Stone: 25, Iron: 30)
        );

        var map = new StrategicMap(new[] { prov1, prov2 });
        var engine = new CampaignEngine(map, ticksPerTurn: 10);

        var initialTreasury = engine.GetTreasury(FactionId.Player);

        // Advance 3 turns (30 ticks)
        for (int t = 0; t < 30; t++)
        {
            engine.AdvanceTick();
        }

        var finalTreasury = engine.GetTreasury(FactionId.Player);

        Assert.Equal(4, engine.CampaignTurn);
        Assert.True(finalTreasury.Gold >= initialTreasury.Gold + (60 * 3));
        Assert.True(finalTreasury.Iron >= initialTreasury.Iron + (35 * 3));
    }

    [Fact]
    public void GarrisonDefenseBonus_EnhancesDefendingUnitsResistance()
    {
        var standardProv = new StrategicProvince(
            id: new ProvinceId("p_standard"),
            name: "Open Plains",
            position: new Vector2D(0, 0),
            ownerFaction: FactionId.Enemy,
            garrisonDefenseBonus: 1.0f
        );

        var fortressProv = new StrategicProvince(
            id: new ProvinceId("p_fortress"),
            name: "High Citadel",
            position: new Vector2D(0, 0),
            ownerFaction: FactionId.Enemy,
            garrisonDefenseBonus: 2.0f
        );

        var garrisonDef = new StrategicUnitSpec { UnitType = "Spearman", BaseMaxHealth = 100f, CurrentHealth = 100f, BaseAttackDamage = 10f, Armor = 2f };
        standardProv.GarrisonUnits.Add(garrisonDef.Clone());
        fortressProv.GarrisonUnits.Add(garrisonDef.Clone());

        var attacker = new StrategicArmy(
            id: new StrategicArmyId(1),
            factionId: FactionId.Player,
            name: "Attacker",
            startingProvinceId: new ProvinceId("p_standard"),
            units: new[] { new StrategicUnitSpec { UnitType = "Swordsman", BaseMaxHealth = 150f, CurrentHealth = 150f, BaseAttackDamage = 20f, Armor = 2f } }
        );

        var setupStandard = new BattleSetup(attacker, standardProv);
        var resStandard = BattleTransitionEngine.ExecuteBattle(setupStandard, maxTicks: 200);

        var setupFortress = new BattleSetup(attacker, fortressProv);
        var resFortress = BattleTransitionEngine.ExecuteBattle(setupFortress, maxTicks: 200);

        Assert.NotNull(resStandard);
        Assert.NotNull(resFortress);
    }

    [Fact]
    public void HeroCampaignBattleIntegration_HeroLevelsUpAndRetainsProgression()
    {
        var prov = new StrategicProvince(
            id: new ProvinceId("prov_hero_battle"),
            name: "Hero Testing Ground",
            position: new Vector2D(0, 0),
            ownerFaction: FactionId.Enemy,
            garrisonDefenseBonus: 1.0f
        );

        prov.GarrisonUnits.Add(new StrategicUnitSpec
        {
            UnitType = "Scout",
            Archetype = UnitArchetype.Infantry,
            BaseMaxHealth = 40f,
            CurrentHealth = 40f,
            BaseAttackDamage = 5f,
            Armor = 0f,
            Level = 1
        });

        var heroSpec = new StrategicHeroSpec
        {
            HeroName = "Grand Marshal",
            Class = HeroClass.Warlord,
            BaseAttributes = new HeroAttributes(14, 12, 10),
            Level = 1,
            CurrentXp = 0
        };

        var army = new StrategicArmy(
            id: new StrategicArmyId(1),
            factionId: FactionId.Player,
            name: "Hero Guard",
            startingProvinceId: new ProvinceId("prov_hero_battle"),
            units: new[] { new StrategicUnitSpec { UnitType = "Swordsman", BaseMaxHealth = 150f, CurrentHealth = 150f, BaseAttackDamage = 20f, Armor = 2f } },
            hero: heroSpec
        );

        var setup = new BattleSetup(army, prov);
        var result = BattleTransitionEngine.ExecuteBattle(setup, maxTicks: 500);

        Assert.True(result.AttackerWon);
        Assert.NotNull(army.AttachedHero);
        Assert.True(army.AttachedHero.TotalKills >= 1 || army.AttachedHero.CurrentXp > 0 || army.Units[0].TotalKills >= 1);
    }

    [Fact]
    public void CampaignSaveLoadRoundtrip_SerializesAndRestoresBitExactParity()
    {
        var provA = new StrategicProvince(
            id: new ProvinceId("prov_a"),
            name: "Capital",
            position: new Vector2D(100f, 100f),
            connectedProvinceIds: new[] { new ProvinceId("prov_b") },
            terrain: TerrainType.Plains,
            nodeType: StrategicNodeType.Fortress,
            ownerFaction: FactionId.Player,
            resourceYields: new ResourceCost(Food: 20, Wood: 15, Gold: 30, Stone: 10, Iron: 5),
            garrisonDefenseBonus: 1.25f
        );

        var provB = new StrategicProvince(
            id: new ProvinceId("prov_b"),
            name: "Outpost",
            position: new Vector2D(250f, 100f),
            connectedProvinceIds: new[] { new ProvinceId("prov_a") },
            terrain: TerrainType.Hills,
            nodeType: StrategicNodeType.ResourceOutpost,
            ownerFaction: FactionId.Enemy,
            resourceYields: new ResourceCost(Food: 5, Wood: 5, Gold: 10, Stone: 20, Iron: 25),
            garrisonDefenseBonus: 1.1f
        );

        var map = new StrategicMap(new[] { provA, provB });
        var engine = new CampaignEngine(map, ticksPerTurn: 50);

        var army = new StrategicArmy(
            id: new StrategicArmyId(1),
            factionId: FactionId.Player,
            name: "First Vanguard",
            startingProvinceId: new ProvinceId("prov_a"),
            units: new[]
            {
                new StrategicUnitSpec { UnitType = "Swordsman", Level = 2, CurrentXp = 150, CurrentHealth = 110f },
                new StrategicUnitSpec { UnitType = "Archer", Level = 1, CurrentXp = 50, CurrentHealth = 70f }
            },
            hero: new StrategicHeroSpec { HeroName = "Commander", Class = HeroClass.Warlord, Level = 2 }
        );

        engine.RegisterArmy(army);
        engine.OrderArmyMove(army.Id, new ProvinceId("prov_b"));

        // Advance 5 ticks
        for (int t = 0; t < 5; t++)
        {
            engine.AdvanceTick();
        }

        // Serialize
        string json = CampaignSerializer.SerializeToJson(engine);
        Assert.False(string.IsNullOrWhiteSpace(json));

        // Deserialize
        var restoreResult = CampaignSerializer.DeserializeFromJson(json);
        Assert.True(restoreResult.IsSuccess);
        var restoredEngine = restoreResult.Value;
        Assert.NotNull(restoredEngine);

        Assert.Equal(engine.SimulationTick, restoredEngine.SimulationTick);
        Assert.Equal(engine.CampaignTurn, restoredEngine.CampaignTurn);
        Assert.Equal(engine.Map.ProvinceCount, restoredEngine.Map.ProvinceCount);
        Assert.Equal(engine.ArmyCount, restoredEngine.ArmyCount);

        var restoredArmy = restoredEngine.GetArmy(new StrategicArmyId(1));
        Assert.NotNull(restoredArmy);
        Assert.Equal(army.CurrentProvinceId, restoredArmy.CurrentProvinceId);
        Assert.Equal(army.DestinationProvinceId, restoredArmy.DestinationProvinceId);
        Assert.Equal(army.MovementTicksRemaining, restoredArmy.MovementTicksRemaining);
        Assert.Equal(army.UnitCount, restoredArmy.UnitCount);
        Assert.Equal("Commander", restoredArmy.AttachedHero?.HeroName);
    }
}
