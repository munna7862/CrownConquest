using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.World;

namespace CrownConquest.Presentation;

/// <summary>
/// Headless end-to-end scenario demonstrating connected mission workflows across Defend, Destroy, Capture, Escort, and Resource Control.
/// </summary>
public sealed class CampaignMissionScenario
{
    public CampaignEngine Engine { get; }
    public CampaignMissionPresenter Presenter { get; }

    public ProvinceId CapitalProvinceId { get; } = new("prov_capital_valoria");
    public ProvinceId IronholdProvinceId { get; } = new("prov_ironhold");
    public ProvinceId QuarryProvinceId { get; } = new("prov_sunstone_quarry");
    public ProvinceId EnemyBastionId { get; } = new("prov_black_ridge");

    public StrategicArmyId PlayerArmyId { get; } = new(101);
    public StrategicArmyId ConvoyArmyId { get; } = new(102);
    public StrategicArmyId EnemyRaiderId { get; } = new(201);

    public CampaignMissionScenario()
    {
        // 1. Map provinces
        var p1 = new StrategicProvince(
            id: CapitalProvinceId,
            name: "Valoria Prime",
            position: new Vector2D(100f, 100f),
            connectedProvinceIds: new[] { IronholdProvinceId },
            terrain: TerrainType.Plains,
            nodeType: StrategicNodeType.Fortress,
            ownerFaction: FactionId.Player,
            resourceYields: new ResourceCost(Food: 50, Wood: 30, Gold: 50, Stone: 20, Iron: 20),
            garrisonDefenseBonus: 1.3f
        );

        var p2 = new StrategicProvince(
            id: IronholdProvinceId,
            name: "Ironhold Bastion",
            position: new Vector2D(250f, 100f),
            connectedProvinceIds: new[] { CapitalProvinceId, QuarryProvinceId, EnemyBastionId },
            terrain: TerrainType.Hills,
            nodeType: StrategicNodeType.Settlement,
            ownerFaction: FactionId.Player,
            resourceYields: new ResourceCost(Food: 10, Wood: 10, Gold: 15, Stone: 30, Iron: 40),
            garrisonDefenseBonus: 1.2f
        );

        var p3 = new StrategicProvince(
            id: QuarryProvinceId,
            name: "Sunstone Quarry",
            position: new Vector2D(250f, 250f),
            connectedProvinceIds: new[] { IronholdProvinceId },
            terrain: TerrainType.Hills,
            nodeType: StrategicNodeType.ResourceOutpost,
            ownerFaction: FactionId.Neutral,
            resourceYields: new ResourceCost(Food: 5, Wood: 5, Gold: 40, Stone: 50, Iron: 10),
            garrisonDefenseBonus: 1.0f
        );

        var p4 = new StrategicProvince(
            id: EnemyBastionId,
            name: "Black Ridge",
            position: new Vector2D(400f, 100f),
            connectedProvinceIds: new[] { IronholdProvinceId },
            terrain: TerrainType.Marsh,
            nodeType: StrategicNodeType.Settlement,
            ownerFaction: FactionId.Enemy,
            resourceYields: new ResourceCost(Food: 10, Wood: 10, Gold: 10, Stone: 10, Iron: 20),
            garrisonDefenseBonus: 1.1f
        );

        var map = new StrategicMap(new[] { p1, p2, p3, p4 });
        Engine = new CampaignEngine(map, ticksPerTurn: 50);
        Presenter = new CampaignMissionPresenter(Engine);

        // 2. Factions registration
        Engine.Diplomacy.RegisterFaction(new FactionDefinition("faction_valoria", "Kingdom of Valoria", "Knights", CapitalProvinceId, 50, "#3B82F6", 1.25, "Allied kingdom"));
        Engine.Diplomacy.RegisterFaction(new FactionDefinition("faction_nordheim", "Nordheim Clans", "Highlanders", QuarryProvinceId, 0, "#10B981", 1.0, "Highland clans"));
        Engine.Diplomacy.RegisterFaction(new FactionDefinition("faction_ironfist", "Ironfist Syndicate", "Raiders", EnemyBastionId, -60, "#EF4444", 0.0, "Hostile invaders"));

        // 3. Missions registration
        Engine.Missions.RegisterMission(new MissionDefinition(
            Id: "mission_defend_ironhold",
            Name: "Hold Ironhold",
            Description: "Defend Ironhold from enemy assaults for 30 ticks.",
            Type: MissionType.Defend,
            IssuingFactionId: "faction_valoria",
            TargetFactionId: "faction_ironfist",
            TargetProvinceId: IronholdProvinceId,
            DestinationProvinceId: null,
            DurationTicks: 30,
            TargetQuantity: 1,
            RequiredResources: ResourceCost.Zero,
            GoldReward: 200,
            XpReward: 300,
            ReputationReward: 20,
            IsPrimaryCampaign: true
        ));

        Engine.Missions.RegisterMission(new MissionDefinition(
            Id: "mission_destroy_raiders",
            Name: "Eliminate Raider Vanguard",
            Description: "Inflict at least 3 casualties on enemy raiders.",
            Type: MissionType.Destroy,
            IssuingFactionId: "faction_valoria",
            TargetFactionId: "faction_ironfist",
            TargetProvinceId: IronholdProvinceId,
            DestinationProvinceId: null,
            DurationTicks: 100,
            TargetQuantity: 3,
            RequiredResources: ResourceCost.Zero,
            GoldReward: 300,
            XpReward: 450,
            ReputationReward: 25,
            IsPrimaryCampaign: true
        ));

        Engine.Missions.RegisterMission(new MissionDefinition(
            Id: "mission_capture_quarry",
            Name: "Seize Sunstone Quarry",
            Description: "Control Sunstone Quarry for 10 consecutive ticks.",
            Type: MissionType.Capture,
            IssuingFactionId: "faction_nordheim",
            TargetFactionId: null,
            TargetProvinceId: QuarryProvinceId,
            DestinationProvinceId: null,
            DurationTicks: 150,
            TargetQuantity: 10,
            RequiredResources: ResourceCost.Zero,
            GoldReward: 250,
            XpReward: 350,
            ReputationReward: 30,
            IsPrimaryCampaign: false
        ));

        Engine.Missions.RegisterMission(new MissionDefinition(
            Id: "mission_escort_convoy",
            Name: "Supply Transport Escort",
            Description: "Escort supply caravan from Ironhold to Valoria Prime.",
            Type: MissionType.Escort,
            IssuingFactionId: "faction_valoria",
            TargetFactionId: null,
            TargetProvinceId: IronholdProvinceId,
            DestinationProvinceId: CapitalProvinceId,
            DurationTicks: 100,
            TargetQuantity: 1,
            RequiredResources: ResourceCost.Zero,
            GoldReward: 400,
            XpReward: 500,
            ReputationReward: 25,
            IsPrimaryCampaign: false
        ));

        Engine.Missions.RegisterMission(new MissionDefinition(
            Id: "mission_resource_stockpile",
            Name: "Resource Stockpile",
            Description: "Accumulate 100 Food, 50 Iron, and 150 Gold.",
            Type: MissionType.ResourceControl,
            IssuingFactionId: "faction_valoria",
            TargetFactionId: null,
            TargetProvinceId: CapitalProvinceId,
            DestinationProvinceId: null,
            DurationTicks: 100,
            TargetQuantity: 300,
            RequiredResources: new ResourceCost(Food: 100, Iron: 50, Gold: 150),
            GoldReward: 150,
            XpReward: 200,
            ReputationReward: 15,
            IsPrimaryCampaign: false
        ));

        // 4. Armies setup
        var playerUnits = new List<StrategicUnitSpec>
        {
            new() { UnitType = "Swordsman", Archetype = UnitArchetype.Infantry, BaseMaxHealth = 100f, CurrentHealth = 100f, BaseAttackDamage = 15f, Level = 1 }
        };
        var playerArmy = new StrategicArmy(PlayerArmyId, FactionId.Player, "Main Army", CapitalProvinceId, playerUnits);
        Engine.RegisterArmy(playerArmy);

        var convoyUnits = new List<StrategicUnitSpec>
        {
            new() { UnitType = "Transport", Archetype = UnitArchetype.Worker, BaseMaxHealth = 150f, CurrentHealth = 150f, BaseAttackDamage = 0f, Level = 1 }
        };
        var convoyArmy = new StrategicArmy(ConvoyArmyId, FactionId.Player, "Royal Caravan", IronholdProvinceId, convoyUnits, baseMovementSpeed: 50f);
        Engine.RegisterArmy(convoyArmy);
    }

    public void RunDefendScenario(int ticks = 35)
    {
        Engine.Missions.AcceptMission("mission_defend_ironhold", Engine.SimulationTick, PlayerArmyId);
        for (int i = 0; i < ticks; i++)
        {
            Engine.AdvanceTick();
        }
    }
}
