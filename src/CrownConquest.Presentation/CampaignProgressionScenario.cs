using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.World;

namespace CrownConquest.Presentation;

/// <summary>
/// Headless scenario executing an end-to-end strategic campaign progression playout.
/// </summary>
public sealed class CampaignProgressionScenario
{
    public CampaignEngine Engine { get; }
    public CampaignPresenter Presenter { get; }

    public StrategicArmyId PlayerArmyId { get; } = new(1);
    public StrategicArmyId EnemyArmyId { get; } = new(2);

    public ProvinceId CapitalProvinceId { get; } = new("prov_crownlands");
    public ProvinceId TargetProvinceId { get; } = new("prov_ironhold");
    public ProvinceId EnemyFortressId { get; } = new("prov_highpeak");

    public CampaignProgressionScenario()
    {
        // 1. Create strategic map
        var prov1 = new StrategicProvince(
            id: CapitalProvinceId,
            name: "The Crownlands",
            position: new Vector2D(100f, 100f),
            connectedProvinceIds: new[] { TargetProvinceId },
            terrain: TerrainType.Plains,
            nodeType: StrategicNodeType.Fortress,
            ownerFaction: FactionId.Player,
            resourceYields: new ResourceCost(Food: 20, Wood: 15, Gold: 30, Stone: 10, Iron: 5),
            garrisonDefenseBonus: 1.25f
        );

        var prov2 = new StrategicProvince(
            id: TargetProvinceId,
            name: "Ironhold Outpost",
            position: new Vector2D(220f, 100f),
            connectedProvinceIds: new[] { CapitalProvinceId, EnemyFortressId },
            terrain: TerrainType.Hills,
            nodeType: StrategicNodeType.ResourceOutpost,
            ownerFaction: FactionId.Neutral,
            resourceYields: new ResourceCost(Food: 5, Wood: 5, Gold: 5, Stone: 20, Iron: 30),
            garrisonDefenseBonus: 1.1f
        );
        // Neutral garrison defending Ironhold
        prov2.GarrisonUnits.Add(new StrategicUnitSpec
        {
            UnitType = "Spearman",
            Archetype = UnitArchetype.Spearman,
            BaseMaxHealth = 80f,
            CurrentHealth = 80f,
            BaseAttackDamage = 8f,
            Armor = 1f,
            Level = 1
        });

        var prov3 = new StrategicProvince(
            id: EnemyFortressId,
            name: "Highpeak Bastion",
            position: new Vector2D(360f, 100f),
            connectedProvinceIds: new[] { TargetProvinceId },
            terrain: TerrainType.Hills,
            nodeType: StrategicNodeType.Fortress,
            ownerFaction: FactionId.Enemy,
            resourceYields: new ResourceCost(Food: 15, Wood: 10, Gold: 20, Stone: 25, Iron: 20),
            garrisonDefenseBonus: 1.3f
        );

        var map = new StrategicMap(new[] { prov1, prov2, prov3 });
        Engine = new CampaignEngine(map, ticksPerTurn: 50);
        Presenter = new CampaignPresenter(Engine);

        // 2. Create Player Army with Veteran units and Hero
        var playerUnits = new List<StrategicUnitSpec>
        {
            new()
            {
                UnitType = "Swordsman",
                Archetype = UnitArchetype.Infantry,
                BaseMaxHealth = 120f,
                CurrentHealth = 120f,
                BaseAttackDamage = 18f,
                Armor = 3f,
                Level = 1
            },
            new()
            {
                UnitType = "Archer",
                Archetype = UnitArchetype.Archer,
                BaseMaxHealth = 70f,
                CurrentHealth = 70f,
                BaseAttackDamage = 14f,
                AttackRange = 80f,
                Armor = 0f,
                Level = 1
            }
        };

        var playerHero = new StrategicHeroSpec
        {
            HeroName = "Sir Roderick",
            Class = HeroClass.Warlord,
            BaseAttributes = new HeroAttributes(12, 10, 14),
            Level = 1
        };

        var playerArmy = new StrategicArmy(
            id: PlayerArmyId,
            factionId: FactionId.Player,
            name: "Royal Vanguard",
            startingProvinceId: CapitalProvinceId,
            units: playerUnits,
            hero: playerHero,
            baseMovementSpeed: 60f
        );

        Engine.RegisterArmy(playerArmy);
    }

    public void RunFullConquestScenario(int maxTicks = 1200)
    {
        // Step 1: Order player army to march from Crownlands to Ironhold
        Engine.OrderArmyMove(PlayerArmyId, TargetProvinceId);

        // Step 2: Tick until Ironhold is reached and conquered
        for (int t = 0; t < maxTicks; t++)
        {
            Engine.AdvanceTick();

            var army = Engine.GetArmy(PlayerArmyId);
            if (army != null && army.CurrentProvinceId == TargetProvinceId && !army.IsInTransit)
            {
                break;
            }
        }
    }
}
