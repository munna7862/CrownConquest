using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.World;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public sealed class StrategicCampaignScenarioAndReplayTests
{
    [Fact]
    public void CampaignProgressionScenario_ExecutesFullMarchAndConquest()
    {
        var scenario = new CampaignProgressionScenario();

        // Check initial state
        var initialProvinces = scenario.Presenter.GetProvinceViewModels();
        Assert.Equal(3, initialProvinces.Count);
        Assert.Single(scenario.Presenter.GetArmyViewModels());

        // Run full scenario
        scenario.RunFullConquestScenario(maxTicks: 800);

        // Verify player army conquered Ironhold
        var army = scenario.Engine.GetArmy(scenario.PlayerArmyId);
        Assert.NotNull(army);
        Assert.Equal(scenario.TargetProvinceId, army.CurrentProvinceId);

        var ironhold = scenario.Engine.Map.GetProvince(scenario.TargetProvinceId);
        Assert.NotNull(ironhold);
        Assert.Equal(FactionId.Player, ironhold.OwnerFaction);

        // Presenter reflects updated territory control
        var dist = scenario.Presenter.GetTerritoryControlDistribution();
        Assert.True(dist.ContainsKey(FactionId.Player.ToString()));
        Assert.True(dist[FactionId.Player.ToString()] >= 0.66f);
    }

    [Fact]
    public void DeterministicCampaignReplay_1000Ticks_BitExactParity()
    {
        ulong Run1000TickSimulation(ulong seed)
        {
            var p1 = new StrategicProvince(
                id: new ProvinceId("p1"),
                name: "West",
                position: new Vector2D(0, 0),
                connectedProvinceIds: new[] { new ProvinceId("p2") },
                terrain: TerrainType.Plains,
                ownerFaction: FactionId.Player,
                resourceYields: new ResourceCost(Food: 10, Wood: 10, Gold: 20, Stone: 5, Iron: 5)
            );
            var p2 = new StrategicProvince(
                id: new ProvinceId("p2"),
                name: "East",
                position: new Vector2D(150, 0),
                connectedProvinceIds: new[] { new ProvinceId("p1") },
                terrain: TerrainType.Hills,
                ownerFaction: FactionId.Enemy,
                resourceYields: new ResourceCost(Food: 5, Wood: 5, Gold: 10, Stone: 15, Iron: 20)
            );

            var map = new StrategicMap(new[] { p1, p2 });
            var engine = new CampaignEngine(map, ticksPerTurn: 40);

            var army1 = new StrategicArmy(
                id: new StrategicArmyId(1),
                factionId: FactionId.Player,
                name: "Army 1",
                startingProvinceId: new ProvinceId("p1"),
                units: new[] { new StrategicUnitSpec { UnitType = "Infantry", Level = 1, BaseMaxHealth = 100f, CurrentHealth = 100f } }
            );

            var army2 = new StrategicArmy(
                id: new StrategicArmyId(2),
                factionId: FactionId.Enemy,
                name: "Army 2",
                startingProvinceId: new ProvinceId("p2"),
                units: new[] { new StrategicUnitSpec { UnitType = "Infantry", Level = 1, BaseMaxHealth = 100f, CurrentHealth = 100f } }
            );

            engine.RegisterArmy(army1);
            engine.RegisterArmy(army2);

            for (int t = 0; t < 1000; t++)
            {
                if (t == 50)
                {
                    engine.OrderArmyMove(army1.Id, new ProvinceId("p2"));
                }
                engine.AdvanceTick();
            }

            // Compute deterministic checksum of final state
            ulong hash = 14695981039346656037UL;
            hash = (hash ^ (ulong)engine.SimulationTick) * 1099511628211UL;
            hash = (hash ^ (ulong)engine.CampaignTurn) * 1099511628211UL;

            var treasury = engine.GetTreasury(FactionId.Player);
            hash = (hash ^ (ulong)treasury.Gold) * 1099511628211UL;
            hash = (hash ^ (ulong)treasury.Iron) * 1099511628211UL;

            foreach (var a in engine.GetAllArmies())
            {
                hash = (hash ^ (ulong)a.Id.Value) * 1099511628211UL;
                hash = (hash ^ (ulong)a.UnitCount) * 1099511628211UL;
            }

            return hash;
        }

        ulong checksum1 = Run1000TickSimulation(42);
        ulong checksum2 = Run1000TickSimulation(42);

        Assert.Equal(checksum1, checksum2);
    }
}
