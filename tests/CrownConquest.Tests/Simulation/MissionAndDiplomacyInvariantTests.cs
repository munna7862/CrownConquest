using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.World;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class MissionAndDiplomacyInvariantTests
{
    [Fact]
    public void RewardAttributionInvariant_GoldAndReputationAwardedExactlyOnce()
    {
        var prov = new StrategicProvince(new ProvinceId("p1"), "Capital", new Vector2D(0, 0), Array.Empty<ProvinceId>(), TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var map = new StrategicMap(new[] { prov });
        var campaign = new CampaignEngine(map);

        campaign.Diplomacy.RegisterFaction(new FactionDefinition("fac_valoria", "Valoria", "Knights", new ProvinceId("p1"), 20, "#FFF", 1.1, "Desc"));
        campaign.SetTreasury(FactionId.Player, new ResourceCost(Gold: 100));

        var def = new MissionDefinition("m_reward", "Reward", "Desc", MissionType.Defend, "fac_valoria", null, new ProvinceId("p1"), null, 5, 1, ResourceCost.Zero, 250, 100, 15, true);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_reward", 0);

        // Tick past duration
        for (int i = 0; i < 10; i++)
        {
            campaign.AdvanceTick();
        }

        var mission = campaign.Missions.GetMission("m_reward");
        Assert.NotNull(mission);
        Assert.Equal(MissionStatus.Completed, mission.Status);

        // Gold: 100 base + 250 reward = 350
        Assert.Equal(350, campaign.GetTreasury(FactionId.Player).Gold);

        // Reputation: 20 base + 15 reward = 35
        Assert.Equal(35, campaign.Diplomacy.GetReputation("fac_valoria"));

        // Tick another 10 times, ensure rewards are not duplicate awarded
        for (int i = 0; i < 10; i++)
        {
            campaign.AdvanceTick();
        }

        Assert.Equal(350, campaign.GetTreasury(FactionId.Player).Gold);
        Assert.Equal(35, campaign.Diplomacy.GetReputation("fac_valoria"));
    }

    [Fact]
    public void MultiMissionConcurrencyInvariant_IndependentExecutionWithoutStateCollision()
    {
        var p1 = new StrategicProvince(new ProvinceId("p1"), "Prov 1", new Vector2D(0, 0), Array.Empty<ProvinceId>(), TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var p2 = new StrategicProvince(new ProvinceId("p2"), "Prov 2", new Vector2D(100, 0), Array.Empty<ProvinceId>(), TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Neutral, ResourceCost.Zero);
        var map = new StrategicMap(new[] { p1, p2 });
        var campaign = new CampaignEngine(map);

        var mDefend = new MissionDefinition("m_def", "Defend P1", "Desc", MissionType.Defend, "valoria", null, new ProvinceId("p1"), null, 10, 1, ResourceCost.Zero, 100, 100, 10, true);
        var mDestroy = new MissionDefinition("m_dest", "Destroy P1", "Desc", MissionType.Destroy, "valoria", "ironfist", new ProvinceId("p1"), null, 20, 2, ResourceCost.Zero, 150, 150, 10, true);
        var mCapture = new MissionDefinition("m_cap", "Capture P2", "Desc", MissionType.Capture, "valoria", null, new ProvinceId("p2"), null, 30, 5, ResourceCost.Zero, 200, 200, 10, false);

        campaign.Missions.RegisterMission(mDefend);
        campaign.Missions.RegisterMission(mDestroy);
        campaign.Missions.RegisterMission(mCapture);

        campaign.Missions.AcceptMission("m_def", 0);
        campaign.Missions.AcceptMission("m_dest", 0);
        campaign.Missions.AcceptMission("m_cap", 0);

        Assert.Equal(3, campaign.Missions.ActiveMissionIds.Count);

        // Fulfill destroy objective
        campaign.Missions.ReportCasualties("ironfist", new ProvinceId("p1"), 2, campaign.SimulationTick);
        campaign.AdvanceTick();

        Assert.Equal(MissionStatus.Completed, campaign.Missions.GetMission("m_dest")?.Status);
        Assert.Equal(MissionStatus.Active, campaign.Missions.GetMission("m_def")?.Status);
        Assert.Equal(MissionStatus.Active, campaign.Missions.GetMission("m_cap")?.Status);
    }

    [Fact]
    public void DiplomaticRepercussionInvariant_SymetricReputationModification()
    {
        var prov = new StrategicProvince(new ProvinceId("p1"), "Capital", new Vector2D(0, 0), Array.Empty<ProvinceId>(), TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var map = new StrategicMap(new[] { prov });
        var campaign = new CampaignEngine(map);

        campaign.Diplomacy.RegisterFaction(new FactionDefinition("fac_valoria", "Valoria", "Knights", new ProvinceId("p1"), 10, "#3B82F6", 1.0, "Desc"));
        campaign.Diplomacy.RegisterFaction(new FactionDefinition("fac_ironfist", "Ironfist", "Raiders", new ProvinceId("p1"), -10, "#EF4444", 0.85, "Desc"));

        var def = new MissionDefinition("m_clash", "Clash", "Desc", MissionType.Destroy, "fac_valoria", "fac_ironfist", new ProvinceId("p1"), null, 20, 1, ResourceCost.Zero, 100, 100, 30, true);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_clash", 0);

        campaign.Missions.ReportCasualties("fac_ironfist", new ProvinceId("p1"), 1, campaign.SimulationTick);
        campaign.AdvanceTick();

        Assert.Equal(MissionStatus.Completed, campaign.Missions.GetMission("m_clash")?.Status);

        // Valoria rep increased by +30: 10 + 30 = 40 (Friendly)
        Assert.Equal(40, campaign.Diplomacy.GetReputation("fac_valoria"));
        Assert.Equal(DiplomacyStanding.Friendly, campaign.Diplomacy.GetStanding("fac_valoria"));

        // Ironfist rep penalized by -15: -10 - 15 = -25 (Hostile)
        Assert.Equal(-25, campaign.Diplomacy.GetReputation("fac_ironfist"));
        Assert.Equal(DiplomacyStanding.Hostile, campaign.Diplomacy.GetStanding("fac_ironfist"));
    }

    [Fact]
    public void ExpirationInvariant_TimedMissionsExpireOnExactTick()
    {
        var prov = new StrategicProvince(new ProvinceId("p1"), "Capital", new Vector2D(0, 0), Array.Empty<ProvinceId>(), TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, ResourceCost.Zero);
        var map = new StrategicMap(new[] { prov });
        var campaign = new CampaignEngine(map);

        var def = new MissionDefinition("m_time_limited", "Destroy Fast", "Desc", MissionType.Destroy, "valoria", "ironfist", new ProvinceId("p1"), null, 15, 10, ResourceCost.Zero, 100, 100, 10, false);
        campaign.Missions.RegisterMission(def);
        campaign.Missions.AcceptMission("m_time_limited", 0);

        for (int i = 0; i < 14; i++)
        {
            campaign.AdvanceTick();
            Assert.Equal(MissionStatus.Active, campaign.Missions.GetMission("m_time_limited")?.Status);
        }

        // 15th tick -> expires
        campaign.AdvanceTick();
        Assert.Equal(MissionStatus.Expired, campaign.Missions.GetMission("m_time_limited")?.Status);
        Assert.Contains("m_time_limited", campaign.Missions.FailedMissionIds);
    }
}
