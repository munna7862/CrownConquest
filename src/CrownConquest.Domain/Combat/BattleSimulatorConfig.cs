using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Specifies the unit composition and configuration for a battle simulator army.
/// </summary>
public sealed class ArmyRosterConfig
{
    public FactionId FactionId { get; set; } = new(1);
    public string FactionName { get; set; } = "Faction 1";
    public Vector2D SpawnCenter { get; set; } = new(10f, 32f);
    public FormationType Formation { get; set; } = FormationType.Line;
    public List<UnitRosterEntry> Units { get; set; } = new();
    public HeroRosterEntry? AttachedHero { get; set; }

    public ArmyRosterConfig() { }

    public ArmyRosterConfig(FactionId factionId, string name, Vector2D spawnCenter, FormationType formation = FormationType.Line)
    {
        FactionId = factionId;
        FactionName = name;
        SpawnCenter = spawnCenter;
        Formation = formation;
    }

    public ArmyRosterConfig AddUnits(string unitType, int count, int level = 1, float customHp = 0, float customDamage = 0, float customArmor = 0)
    {
        Units.Add(new UnitRosterEntry(unitType, count, level, customHp, customDamage, customArmor));
        return this;
    }

    public ArmyRosterConfig SetHero(string heroType, HeroClass heroClass, int level = 1)
    {
        AttachedHero = new HeroRosterEntry(heroType, heroClass, level);
        return this;
    }
}

/// <summary>
/// Represents a single unit type grouping in a roster.
/// </summary>
public sealed record UnitRosterEntry(
    string UnitType,
    int Count,
    int Level = 1,
    float CustomHp = 0,
    float CustomDamage = 0,
    float CustomArmor = 0);

/// <summary>
/// Represents an attached hero in an army roster.
/// </summary>
public sealed record HeroRosterEntry(
    string HeroType,
    HeroClass HeroClass,
    int Level = 1);

/// <summary>
/// Full configuration for a headless deterministic battle simulation match.
/// </summary>
public sealed class BattleSimulatorConfig
{
    public ArmyRosterConfig TeamA { get; set; } = new(new FactionId(1), "Kingdom", new Vector2D(26f, 32f));
    public ArmyRosterConfig TeamB { get; set; } = new(new FactionId(2), "Imperium", new Vector2D(38f, 32f));
    public int MapWidth { get; set; } = 64;
    public int MapHeight { get; set; } = 64;
    public int MaxTicks { get; set; } = 3000;
    public int RandomSeed { get; set; } = 42;
    public TerrainType DefaultTerrain { get; set; } = TerrainType.Plains;
    public bool AutoEngage { get; set; } = true;

    public static BattleSimulatorConfig CreateStandardMatchup(
        string unitTypeA,
        int countA,
        string unitTypeB,
        int countB,
        int seed = 42)
    {
        var config = new BattleSimulatorConfig
        {
            RandomSeed = seed,
            TeamA = new ArmyRosterConfig(new FactionId(1), "Team A", new Vector2D(26f, 32f))
                .AddUnits(unitTypeA, countA),
            TeamB = new ArmyRosterConfig(new FactionId(2), "Team B", new Vector2D(38f, 32f))
                .AddUnits(unitTypeB, countB)
        };
        return config;
    }
}
