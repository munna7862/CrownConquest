using System;
using System.Collections.Generic;
using System.Text;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Pairwise battle statistics between two specific factions.
/// </summary>
public sealed record FactionMatchupResult(
    string FactionA,
    string FactionB,
    int TotalBattles,
    int WinsA,
    int WinsB,
    int Draws,
    float WinRateA,
    float WinRateB,
    float DrawRate,
    float MeanDurationTicks,
    float MeanCasualtiesA,
    float MeanCasualtiesB);

/// <summary>
/// Cross-faction balance evaluation and asymmetry diagnostics report.
/// </summary>
public sealed record FactionBalanceReport(
    DateTime Timestamp,
    int TotalMatchups,
    float OverallAsymmetryScore,
    IReadOnlyList<FactionMatchupResult> Matchups,
    IReadOnlyDictionary<string, float> FactionOverallWinRates,
    IReadOnlyList<string> BalanceWarnings)
{
    public string GenerateFormattedReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine($" CROWN & CONQUEST — FACTION BALANCE & ASYMMETRY REPORT ({Timestamp:u})");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"Total Matchup Pairs Evaluated: {TotalMatchups}");
        sb.AppendLine($"Overall Balance Asymmetry Index: {OverallAsymmetryScore:F3} (0.000 = Perfectly Balanced)");
        sb.AppendLine();

        sb.AppendLine("--- FACTION AGGREGATE WIN RATES ---");
        foreach (var (faction, winRate) in FactionOverallWinRates)
        {
            sb.AppendLine($"  - {faction,-12}: {winRate,6:P1} win rate");
        }
        sb.AppendLine();

        sb.AppendLine("--- PAIRWISE MATCHUP MATRIX ---");
        sb.AppendLine(string.Format("{0,-12} vs {1,-12} | {2,8} | {3,8} | {4,8} | {5,10}", "Faction A", "Faction B", "Win A", "Win B", "Draw", "Avg Ticks"));
        sb.AppendLine(new string('-', 68));
        for (int i = 0; i < Matchups.Count; i++)
        {
            var m = Matchups[i];
            sb.AppendLine(string.Format("{0,-12} vs {1,-12} | {2,7:P1} | {3,7:P1} | {4,7:P1} | {5,10:F1}",
                m.FactionA, m.FactionB, m.WinRateA, m.WinRateB, m.DrawRate, m.MeanDurationTicks));
        }
        sb.AppendLine();

        if (BalanceWarnings.Count > 0)
        {
            sb.AppendLine("--- BALANCE WARNINGS & ANOMALIES ---");
            for (int i = 0; i < BalanceWarnings.Count; i++)
            {
                sb.AppendLine($"  [WARNING] {BalanceWarnings[i]}");
            }
        }
        else
        {
            sb.AppendLine("--- ALL FACTION MATCHUPS WITHIN ACCEPTABLE BALANCE TOLERANCE (±15%) ---");
        }

        sb.AppendLine("================================================================================");
        return sb.ToString();
    }
}
