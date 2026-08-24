using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.World;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class FactionDiplomacyAndMissionMathTests
{
    [Theory]
    [InlineData(-100, DiplomacyStanding.AtWar)]
    [InlineData(-60, DiplomacyStanding.AtWar)]
    [InlineData(-59, DiplomacyStanding.Hostile)]
    [InlineData(-20, DiplomacyStanding.Hostile)]
    [InlineData(-19, DiplomacyStanding.Neutral)]
    [InlineData(0, DiplomacyStanding.Neutral)]
    [InlineData(19, DiplomacyStanding.Neutral)]
    [InlineData(20, DiplomacyStanding.Friendly)]
    [InlineData(59, DiplomacyStanding.Friendly)]
    [InlineData(60, DiplomacyStanding.Allied)]
    [InlineData(100, DiplomacyStanding.Allied)]
    public void FactionDiplomacyManager_StandingThresholds_CalculatedAccurately(int reputation, DiplomacyStanding expectedStanding)
    {
        var standing = FactionDiplomacyManager.CalculateStanding(reputation);
        Assert.Equal(expectedStanding, standing);
    }

    [Fact]
    public void FactionDiplomacyManager_ModifyReputation_ClampsBetweenBounds()
    {
        var manager = new FactionDiplomacyManager();
        manager.RegisterFaction(new FactionDefinition("fac_1", "Test Faction", "Culture", new ProvinceId("p1"), 80, "#FFF", 1.0, "Desc"));

        // Increase beyond 100
        manager.ModifyReputation("fac_1", 50);
        Assert.Equal(100, manager.GetReputation("fac_1"));
        Assert.Equal(DiplomacyStanding.Allied, manager.GetStanding("fac_1"));

        // Decrease below -100
        manager.ModifyReputation("fac_1", -250);
        Assert.Equal(-100, manager.GetReputation("fac_1"));
        Assert.Equal(DiplomacyStanding.AtWar, manager.GetStanding("fac_1"));
    }

    [Fact]
    public void FactionDiplomacyManager_TradeModifiers_ReflectDiplomaticStanding()
    {
        var manager = new FactionDiplomacyManager();
        manager.RegisterFaction(new FactionDefinition("f_allied", "Allied", "Culture", new ProvinceId("p1"), 75, "#FFF", 1.25, "Desc"));
        manager.RegisterFaction(new FactionDefinition("f_friendly", "Friendly", "Culture", new ProvinceId("p1"), 35, "#FFF", 1.10, "Desc"));
        manager.RegisterFaction(new FactionDefinition("f_neutral", "Neutral", "Culture", new ProvinceId("p1"), 0, "#FFF", 1.00, "Desc"));
        manager.RegisterFaction(new FactionDefinition("f_hostile", "Hostile", "Culture", new ProvinceId("p1"), -40, "#FFF", 0.85, "Desc"));
        manager.RegisterFaction(new FactionDefinition("f_war", "AtWar", "Culture", new ProvinceId("p1"), -80, "#FFF", 0.00, "Desc"));

        Assert.Equal(1.25, manager.GetTradeBonusModifier("f_allied"));
        Assert.Equal(1.10, manager.GetTradeBonusModifier("f_friendly"));
        Assert.Equal(1.00, manager.GetTradeBonusModifier("f_neutral"));
        Assert.Equal(0.85, manager.GetTradeBonusModifier("f_hostile"));
        Assert.Equal(0.00, manager.GetTradeBonusModifier("f_war"));

        Assert.True(manager.IsAllied("f_allied"));
        Assert.True(manager.IsAtWar("f_war"));
    }

    [Fact]
    public void MissionEngine_MissionLifecycle_TransitionsCorrectly()
    {
        var engine = new MissionEngine();
        var def = new MissionDefinition("m1", "Test", "Desc", MissionType.Defend, "fac_1", null, new ProvinceId("p1"), null, 50, 1, ResourceCost.Zero, 100, 100, 10, true);

        engine.RegisterMission(def);

        Assert.True(engine.TryGetMission("m1", out var state));
        Assert.NotNull(state);
        Assert.Equal(MissionStatus.Inactive, state.Status);

        var acceptRes = engine.AcceptMission("m1", 10);
        Assert.True(acceptRes.IsSuccess);
        Assert.Equal(MissionStatus.Active, state.Status);
        Assert.Equal(10, state.StartTick);
        Assert.Single(engine.ActiveMissionIds);

        // Cannot accept twice
        var duplicateRes = engine.AcceptMission("m1", 20);
        Assert.False(duplicateRes.IsSuccess);

        // Abandon
        var abandonRes = engine.AbandonMission("m1", 30);
        Assert.True(abandonRes.IsSuccess);
        Assert.Equal(MissionStatus.Failed, state.Status);
        Assert.Empty(engine.ActiveMissionIds);
        Assert.Single(engine.FailedMissionIds);
    }

    [Fact]
    public void DefendObjective_HoldsUntilDuration_CompletesSuccessfully()
    {
        var prov = new StrategicProvince(new ProvinceId("p1"), "Prov 1", new Vector2D(0, 0), Array.Empty<ProvinceId>(), TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var map = new StrategicMap(new[] { prov });
        var campaign = new CampaignEngine(map);

        var def = new MissionDefinition("m_defend", "Defend", "Desc", MissionType.Defend, "valoria", null, new ProvinceId("p1"), null, 10, 1, ResourceCost.Zero, 100, 100, 10, true);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_defend", 0);

        for (int i = 0; i < 10; i++)
        {
            campaign.AdvanceTick();
        }

        var mission = campaign.Missions.GetMission("m_defend");
        Assert.NotNull(mission);
        Assert.Equal(MissionStatus.Completed, mission.Status);
    }

    [Fact]
    public void DestroyObjective_KillQuotaMet_CompletesMission()
    {
        var prov = new StrategicProvince(new ProvinceId("p1"), "Prov 1", new Vector2D(0, 0), Array.Empty<ProvinceId>(), TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var map = new StrategicMap(new[] { prov });
        var campaign = new CampaignEngine(map);

        var def = new MissionDefinition("m_destroy", "Destroy", "Desc", MissionType.Destroy, "valoria", "ironfist", new ProvinceId("p1"), null, 50, 3, ResourceCost.Zero, 200, 200, 15, true);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_destroy", 0);

        campaign.Missions.ReportCasualties("ironfist", new ProvinceId("p1"), 3, campaign.SimulationTick);
        campaign.AdvanceTick();

        var mission = campaign.Missions.GetMission("m_destroy");
        Assert.NotNull(mission);
        Assert.Equal(MissionStatus.Completed, mission.Status);
    }

    [Fact]
    public void CaptureObjective_HoldingTerritory_AccumulatesProgressAndCompletes()
    {
        var prov = new StrategicProvince(new ProvinceId("p_quarry"), "Quarry", new Vector2D(0, 0), Array.Empty<ProvinceId>(), TerrainType.Hills, StrategicNodeType.ResourceOutpost, FactionId.Player, ResourceCost.Zero);
        var map = new StrategicMap(new[] { prov });
        var campaign = new CampaignEngine(map);

        var def = new MissionDefinition("m_capture", "Capture", "Desc", MissionType.Capture, "nordheim", null, new ProvinceId("p_quarry"), null, 50, 5, ResourceCost.Zero, 250, 250, 20, false);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_capture", 0);

        for (int i = 0; i < 5; i++)
        {
            campaign.AdvanceTick();
        }

        var mission = campaign.Missions.GetMission("m_capture");
        Assert.NotNull(mission);
        Assert.Equal(MissionStatus.Completed, mission.Status);
    }

    [Fact]
    public void EscortObjective_ConvoyArrival_CompletesMission()
    {
        var p1 = new StrategicProvince(new ProvinceId("p_start"), "Start", new Vector2D(0, 0), new[] { new ProvinceId("p_end") }, TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var p2 = new StrategicProvince(new ProvinceId("p_end"), "End", new Vector2D(100, 0), new[] { new ProvinceId("p_start") }, TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var map = new StrategicMap(new[] { p1, p2 });
        var campaign = new CampaignEngine(map);

        var convoyArmy = new StrategicArmy(new StrategicArmyId(99), FactionId.Player, "Convoy", new ProvinceId("p_start"), new List<StrategicUnitSpec>
        {
            new() { UnitType = "Worker", Archetype = UnitArchetype.Worker, BaseMaxHealth = 100f, CurrentHealth = 100f }
        });
        campaign.RegisterArmy(convoyArmy);

        var def = new MissionDefinition("m_escort", "Escort", "Desc", MissionType.Escort, "valoria", null, new ProvinceId("p_start"), new ProvinceId("p_end"), 50, 1, ResourceCost.Zero, 300, 300, 25, true);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_escort", 0, convoyArmy.Id);

        // Move convoy to destination
        campaign.OrderArmyMove(convoyArmy.Id, new ProvinceId("p_end"));
        while (convoyArmy.IsInTransit)
        {
            campaign.AdvanceTick();
        }
        campaign.AdvanceTick();

        var mission = campaign.Missions.GetMission("m_escort");
        Assert.NotNull(mission);
        Assert.Equal(MissionStatus.Completed, mission.Status);
    }

    [Fact]
    public void ResourceControlObjective_TreasuryQuotaMet_CompletesMission()
    {
        var prov = new StrategicProvince(new ProvinceId("p1"), "Capital", new Vector2D(0, 0), Array.Empty<ProvinceId>(), TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var map = new StrategicMap(new[] { prov });
        var campaign = new CampaignEngine(map);

        var def = new MissionDefinition("m_res", "Harvest", "Desc", MissionType.ResourceControl, "valoria", null, new ProvinceId("p1"), null, 50, 300, new ResourceCost(Food: 100, Iron: 50, Gold: 150), 200, 200, 15, false);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_res", 0);

        // Set player treasury to satisfy requirements
        campaign.SetTreasury(FactionId.Player, new ResourceCost(Food: 120, Iron: 60, Gold: 200));
        campaign.AdvanceTick();

        var mission = campaign.Missions.GetMission("m_res");
        Assert.NotNull(mission);
        Assert.Equal(MissionStatus.Completed, mission.Status);
    }
}
