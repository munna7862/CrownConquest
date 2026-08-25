using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Generates comprehensive cross-faction balance reports and matchup matrices.
/// </summary>
public sealed class FactionBalanceReportGenerator
{
    private readonly BatchBattleRunner _batchRunner = new();

    public FactionBalanceReport GenerateReport(int battlesPerMatchup = 20, int baseSeed = 2000)
    {
        var factions = new List<(string Name, FactionId Id, ArmyRosterConfig Roster)>
        {
            ("Kingdom", new FactionId(1), CreateKingdomRoster(new FactionId(1))),
            ("Imperium", new FactionId(2), CreateImperiumRoster(new FactionId(2))),
            ("Caliphate", new FactionId(3), CreateCaliphateRoster(new FactionId(3))),
            ("Horde", new FactionId(4), CreateHordeRoster(new FactionId(4))),
            ("Republic", new FactionId(5), CreateRepublicRoster(new FactionId(5)))
        };

        var matchups = new List<FactionMatchupResult>();
        var factionWins = new Dictionary<string, int>();
        var factionTotalBattles = new Dictionary<string, int>();

        foreach (var (name, _, _) in factions)
        {
            factionWins[name] = 0;
            factionTotalBattles[name] = 0;
        }

        int matchupIdx = 0;
        for (int i = 0; i < factions.Count; i++)
        {
            for (int j = i + 1; j < factions.Count; j++)
            {
                var fA = factions[i];
                var fB = factions[j];

                // Configure matchup positions
                var rosterA = CloneRoster(fA.Roster, new Vector2D(14f, 32f));
                var rosterB = CloneRoster(fB.Roster, new Vector2D(50f, 32f));

                var simConfig = new BattleSimulatorConfig
                {
                    TeamA = rosterA,
                    TeamB = rosterB,
                    MaxTicks = 2500
                };

                var batchConfig = BatchBattleConfig.Create(simConfig, battlesPerMatchup, baseSeed + (matchupIdx * 100));
                var batchResult = _batchRunner.RunBatch(batchConfig);

                matchups.Add(new FactionMatchupResult(
                    fA.Name,
                    fB.Name,
                    batchResult.TotalBattles,
                    batchResult.TeamAWins,
                    batchResult.TeamBWins,
                    batchResult.Draws,
                    batchResult.WinRateA,
                    batchResult.WinRateB,
                    batchResult.DrawRate,
                    batchResult.MeanDurationTicks,
                    batchResult.MeanCasualtiesA,
                    batchResult.MeanCasualtiesB));

                factionWins[fA.Name] += batchResult.TeamAWins;
                factionTotalBattles[fA.Name] += batchResult.TotalBattles;

                factionWins[fB.Name] += batchResult.TeamBWins;
                factionTotalBattles[fB.Name] += batchResult.TotalBattles;

                matchupIdx++;
            }
        }

        var overallWinRates = new Dictionary<string, float>();
        var balanceWarnings = new List<string>();

        double asymmetryAccumulator = 0;

        foreach (var (name, wins) in factionWins)
        {
            int total = factionTotalBattles[name];
            float winRate = total > 0 ? (float)wins / total : 0.5f;
            overallWinRates[name] = winRate;

            double deviation = Math.Abs(winRate - 0.50);
            asymmetryAccumulator += deviation;

            if (winRate > 0.65f)
            {
                balanceWarnings.Add($"Faction '{name}' appears overtuned with a high win rate of {winRate:P1}.");
            }
            else if (winRate < 0.35f)
            {
                balanceWarnings.Add($"Faction '{name}' appears undertuned with a low win rate of {winRate:P1}.");
            }
        }

        float asymmetryScore = (float)(asymmetryAccumulator / factions.Count);

        return new FactionBalanceReport(
            DateTime.UtcNow,
            matchups.Count,
            asymmetryScore,
            matchups,
            overallWinRates,
            balanceWarnings);
    }

    private static ArmyRosterConfig CreateKingdomRoster(FactionId id) =>
        new ArmyRosterConfig(id, "Kingdom", new Vector2D(14f, 32f), FormationType.Line)
            .AddUnits("kingdom_swordsman", 6, customHp: 120, customDamage: 16, customArmor: 3)
            .AddUnits("kingdom_knight", 3, customHp: 160, customDamage: 22, customArmor: 3)
            .AddUnits("kingdom_archer", 4, customHp: 75, customDamage: 12, customArmor: 0)
            .SetHero("kingdom_warlord", HeroClass.Warlord, 1);

    private static ArmyRosterConfig CreateImperiumRoster(FactionId id) =>
        new ArmyRosterConfig(id, "Imperium", new Vector2D(14f, 32f), FormationType.Square)
            .AddUnits("imperium_legionary", 7, customHp: 130, customDamage: 15, customArmor: 4)
            .AddUnits("imperium_equite", 2, customHp: 155, customDamage: 20, customArmor: 3)
            .AddUnits("imperium_veles", 4, customHp: 70, customDamage: 13, customArmor: 0)
            .SetHero("imperium_centurion", HeroClass.Centurion, 1);

    private static ArmyRosterConfig CreateCaliphateRoster(FactionId id) =>
        new ArmyRosterConfig(id, "Caliphate", new Vector2D(14f, 32f), FormationType.Loose)
            .AddUnits("caliphate_swordsman", 5, customHp: 115, customDamage: 16, customArmor: 2)
            .AddUnits("caliphate_mamluk", 3, customHp: 165, customDamage: 24, customArmor: 3)
            .AddUnits("caliphate_composite_archer", 5, customHp: 75, customDamage: 14, customArmor: 0)
            .SetHero("caliphate_emir", HeroClass.Warlord, 1);

    private static ArmyRosterConfig CreateHordeRoster(FactionId id) =>
        new ArmyRosterConfig(id, "Horde", new Vector2D(14f, 32f), FormationType.Wedge)
            .AddUnits("horde_nomad_spear", 4, customHp: 110, customDamage: 14, customArmor: 1)
            .AddUnits("horde_lancer", 4, customHp: 150, customDamage: 25, customArmor: 2)
            .AddUnits("horde_horse_archer", 5, customHp: 85, customDamage: 13, customArmor: 1)
            .SetHero("horde_khan", HeroClass.Warlord, 1);

    private static ArmyRosterConfig CreateRepublicRoster(FactionId id) =>
        new ArmyRosterConfig(id, "Republic", new Vector2D(14f, 32f), FormationType.Line)
            .AddUnits("republic_pikeman", 6, customHp: 120, customDamage: 15, customArmor: 3)
            .AddUnits("republic_crossbow", 5, customHp: 80, customDamage: 15, customArmor: 1)
            .AddUnits("republic_scout_cav", 2, customHp: 145, customDamage: 18, customArmor: 2)
            .SetHero("republic_consul", HeroClass.Centurion, 1);

    private static ArmyRosterConfig CloneRoster(ArmyRosterConfig source, Vector2D spawnCenter)
    {
        var copy = new ArmyRosterConfig(source.FactionId, source.FactionName, spawnCenter, source.Formation);
        for (int i = 0; i < source.Units.Count; i++)
        {
            var u = source.Units[i];
            copy.Units.Add(new UnitRosterEntry(u.UnitType, u.Count, u.Level, u.CustomHp, u.CustomDamage, u.CustomArmor));
        }
        if (source.AttachedHero != null)
        {
            copy.AttachedHero = new HeroRosterEntry(source.AttachedHero.HeroType, source.AttachedHero.HeroClass, source.AttachedHero.Level);
        }
        return copy;
    }
}
