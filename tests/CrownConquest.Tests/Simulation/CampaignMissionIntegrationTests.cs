using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.World;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class CampaignMissionIntegrationTests
{
    [Fact]
    public void DefendCaptureCampaignIntegration_ConquestTriggersMissionCompletion()
    {
        var p1 = new StrategicProvince(new ProvinceId("prov_base"), "Base", new Vector2D(0, 0), new[] { new ProvinceId("prov_target") }, TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var p2 = new StrategicProvince(new ProvinceId("prov_target"), "Target", new Vector2D(100, 0), new[] { new ProvinceId("prov_base") }, TerrainType.Hills, StrategicNodeType.Settlement, FactionId.Enemy, ResourceCost.Zero);
        var map = new StrategicMap(new[] { p1, p2 });
        var campaign = new CampaignEngine(map);

        var playerUnits = new List<StrategicUnitSpec>
        {
            new() { UnitType = "Swordsman", Archetype = UnitArchetype.Infantry, BaseMaxHealth = 150f, CurrentHealth = 150f, BaseAttackDamage = 25f, Armor = 4f }
        };
        var playerArmy = new StrategicArmy(new StrategicArmyId(1), FactionId.Player, "Assault Vanguard", new ProvinceId("prov_base"), playerUnits);
        campaign.RegisterArmy(playerArmy);

        var def = new MissionDefinition("m_conquest", "Conquer Target", "Desc", MissionType.Capture, "valoria", null, new ProvinceId("prov_target"), null, 100, 1, ResourceCost.Zero, 500, 500, 30, true);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_conquest", 0);

        // March to target province and conquer it
        campaign.OrderArmyMove(playerArmy.Id, new ProvinceId("prov_target"));

        while (playerArmy.IsInTransit)
        {
            campaign.AdvanceTick();
        }
        campaign.AdvanceTick();

        // Target province conquered and mission evaluated
        Assert.Equal(FactionId.Player, p2.OwnerFaction);
        Assert.Equal(MissionStatus.Completed, campaign.Missions.GetMission("m_conquest")?.Status);
    }

    [Fact]
    public void EscortConvoyIntegration_CaravanArrivesAtDestinationSafely()
    {
        var p1 = new StrategicProvince(new ProvinceId("prov_1"), "Origin", new Vector2D(0, 0), new[] { new ProvinceId("prov_2") }, TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var p2 = new StrategicProvince(new ProvinceId("prov_2"), "Destination", new Vector2D(100, 0), new[] { new ProvinceId("prov_1") }, TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var map = new StrategicMap(new[] { p1, p2 });
        var campaign = new CampaignEngine(map);

        var convoyUnits = new List<StrategicUnitSpec>
        {
            new() { UnitType = "Transport", Archetype = UnitArchetype.Worker, BaseMaxHealth = 100f, CurrentHealth = 100f }
        };
        var convoyArmy = new StrategicArmy(new StrategicArmyId(55), FactionId.Player, "Caravan", new ProvinceId("prov_1"), convoyUnits);
        campaign.RegisterArmy(convoyArmy);

        var def = new MissionDefinition("m_escort", "Escort Caravan", "Desc", MissionType.Escort, "valoria", null, new ProvinceId("prov_1"), new ProvinceId("prov_2"), 80, 1, ResourceCost.Zero, 350, 300, 20, true);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_escort", 0, convoyArmy.Id);

        campaign.OrderArmyMove(convoyArmy.Id, new ProvinceId("prov_2"));

        while (convoyArmy.IsInTransit)
        {
            campaign.AdvanceTick();
        }
        campaign.AdvanceTick();

        Assert.Equal(new ProvinceId("prov_2"), convoyArmy.CurrentProvinceId);
        Assert.Equal(MissionStatus.Completed, campaign.Missions.GetMission("m_escort")?.Status);
    }

    [Fact]
    public void CampaignSaveLoadRoundtrip_PreservesMissionsAndDiplomacyState()
    {
        var p1 = new StrategicProvince(new ProvinceId("p1"), "Capital", new Vector2D(0, 0), Array.Empty<ProvinceId>(), TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var map = new StrategicMap(new[] { p1 });
        var campaign = new CampaignEngine(map);

        campaign.Diplomacy.RegisterFaction(new FactionDefinition("fac_test", "Test Faction", "Culture", new ProvinceId("p1"), 45, "#FFF", 1.1, "Desc"));
        campaign.Diplomacy.SetReputation("fac_test", 55);

        var def = new MissionDefinition("m_active", "Active Quest", "Desc", MissionType.Defend, "fac_test", null, new ProvinceId("p1"), null, 50, 1, ResourceCost.Zero, 200, 200, 15, true);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_active", 10);

        for (int i = 0; i < 5; i++)
        {
            campaign.AdvanceTick();
        }

        // Serialize
        string json = CampaignSerializer.SerializeToJson(campaign);
        Assert.NotNull(json);

        // Deserialize
        var desResult = CampaignSerializer.DeserializeFromJson(json);
        Assert.True(desResult.IsSuccess, desResult.Error.Message);

        var loadedCampaign = desResult.Value!;
        Assert.Equal(campaign.SimulationTick, loadedCampaign.SimulationTick);
        Assert.Equal(55, loadedCampaign.Diplomacy.GetReputation("fac_test"));
        Assert.Equal(DiplomacyStanding.Friendly, loadedCampaign.Diplomacy.GetStanding("fac_test"));

        var loadedMission = loadedCampaign.Missions.GetMission("m_active");
        Assert.NotNull(loadedMission);
        Assert.Equal(MissionStatus.Active, loadedMission.Status);
        Assert.Equal(5, loadedMission.ElapsedTicks);
    }
}
