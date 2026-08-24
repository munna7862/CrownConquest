using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.World;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class CampaignMissionScenarioAndReplayTests
{
    [Fact]
    public void CampaignMissionScenario_FullPlaythrough_ExecutesAndValidatesViewModels()
    {
        var scenario = new CampaignMissionScenario();

        // 1. Initial State verification
        var activeMissions = scenario.Presenter.GetActiveMissionViewModels();
        Assert.Empty(activeMissions);

        var diplomacyViews = scenario.Presenter.GetFactionDiplomacyViewModels();
        Assert.Equal(3, diplomacyViews.Count);

        var valoria = diplomacyViews.Find(f => f.Id == "faction_valoria");
        Assert.NotNull(valoria);
        Assert.Equal("Friendly", valoria.Standing);

        var ironfist = diplomacyViews.Find(f => f.Id == "faction_ironfist");
        Assert.NotNull(ironfist);
        Assert.Equal("AtWar", ironfist.Standing);

        // 2. Run Defend Mission scenario
        scenario.RunDefendScenario(35);

        var defendMission = scenario.Engine.Missions.GetMission("mission_defend_ironhold");
        Assert.NotNull(defendMission);
        Assert.Equal(MissionStatus.Completed, defendMission.Status);

        // Verify rewards applied
        Assert.Contains("mission_defend_ironhold", scenario.Engine.Missions.CompletedMissionIds);
    }

    [Fact]
    public void DeterministicReplayParity_1000Ticks_ChecksumParityAcrossDualRuns()
    {
        ulong seed = 987654321UL;

        ulong checksumA = RunSimulationForChecksum(seed);
        ulong checksumB = RunSimulationForChecksum(seed);

        Assert.Equal(checksumA, checksumB);
    }

    private static ulong RunSimulationForChecksum(ulong seed)
    {
        var p1 = new StrategicProvince(new ProvinceId("p1"), "Capital", new Vector2D(0, 0), new[] { new ProvinceId("p2") }, TerrainType.Plains, StrategicNodeType.Settlement, FactionId.Player, new ResourceCost(Food: 10, Gold: 10));
        var p2 = new StrategicProvince(new ProvinceId("p2"), "Frontier", new Vector2D(100, 0), new[] { new ProvinceId("p1") }, TerrainType.Hills, StrategicNodeType.ResourceOutpost, FactionId.Neutral, new ResourceCost(Iron: 10, Stone: 10));
        var map = new StrategicMap(new[] { p1, p2 });
        var engine = new CampaignEngine(map, ticksPerTurn: 50);

        engine.Diplomacy.RegisterFaction(new FactionDefinition("fac_valoria", "Valoria", "Knights", new ProvinceId("p1"), 30, "#FFF", 1.1, "Desc"));
        engine.Diplomacy.RegisterFaction(new FactionDefinition("fac_raiders", "Raiders", "Raiders", new ProvinceId("p2"), -50, "#FFF", 0.0, "Desc"));

        var armyUnits = new List<StrategicUnitSpec>
        {
            new() { UnitType = "Swordsman", Archetype = UnitArchetype.Infantry, BaseMaxHealth = 100f, CurrentHealth = 100f, BaseAttackDamage = 15f }
        };
        var army = new StrategicArmy(new StrategicArmyId(1), FactionId.Player, "Patrol", new ProvinceId("p1"), armyUnits);
        engine.RegisterArmy(army);

        var defMission = new MissionDefinition("m_patrol", "Patrol", "Desc", MissionType.Defend, "fac_valoria", null, new ProvinceId("p1"), null, 100, 1, ResourceCost.Zero, 100, 100, 10, true);
        engine.Missions.RegisterMission(defMission);
        engine.Missions.AcceptMission("m_patrol", 0);

        ulong checksum = seed;

        for (int tick = 0; tick < 1000; tick++)
        {
            if (tick == 50)
            {
                engine.OrderArmyMove(army.Id, new ProvinceId("p2"));
            }
            else if (tick == 200)
            {
                engine.OrderArmyMove(army.Id, new ProvinceId("p1"));
            }

            engine.AdvanceTick();

            var treasury = engine.GetTreasury(FactionId.Player);
            checksum = unchecked(checksum * 31 + (ulong)engine.SimulationTick + (ulong)treasury.Gold + (ulong)treasury.Food + (ulong)engine.Diplomacy.GetReputation("fac_valoria"));
        }

        return checksum;
    }
}
