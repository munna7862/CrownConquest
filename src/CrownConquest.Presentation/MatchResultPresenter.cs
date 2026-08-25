using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Presentation;

/// <summary>
/// Match final outcome state.
/// </summary>
public enum MatchOutcome
{
    Ongoing,
    Victory,
    Defeat
}

/// <summary>
/// View model containing post-match combat and economic MVP statistics.
/// </summary>
public sealed record MatchResultSummaryViewModel(
    FactionId PlayerFaction,
    MatchOutcome Outcome,
    string BannerTitle,
    string BannerSubtitle,
    int TotalTicksExecuted,
    float MatchDurationSeconds,
    int TotalKills,
    int TotalCasualtiesLost,
    int UnitsTrained,
    int ResourcesHarvestedTotal,
    string MvpHeroName,
    int MvpHeroLevel,
    int MvpHeroKills,
    string HistoricalSummary);

/// <summary>
/// Presenter formatting end-of-match statistics and outcome summaries.
/// </summary>
public sealed class MatchResultPresenter
{
    public static MatchResultSummaryViewModel CreateSummary(
        FactionId playerFaction,
        MatchOutcome outcome,
        int totalTicks,
        int kills,
        int casualties,
        int unitsTrained,
        int resourcesHarvested,
        string mvpHeroName,
        int mvpHeroLevel,
        int mvpHeroKills)
    {
        float durationSec = totalTicks * 0.05f; // 20Hz ticks
        string title = outcome switch
        {
            MatchOutcome.Victory => "TRIUMPHANT VICTORY",
            MatchOutcome.Defeat => "BITTER DEFEAT",
            _ => "BATTLE IN PROGRESS"
        };

        string subtitle = outcome switch
        {
            MatchOutcome.Victory => "The Roman Praetorium has fallen! Gaul remains free!",
            MatchOutcome.Defeat => "The Celtic Hill Village has been conquered by the Roman Legion.",
            _ => "Engage the enemy forces and conquer their stronghold."
        };

        string history = outcome switch
        {
            MatchOutcome.Victory => "In a fierce struggle at the river crossing, the Celtic tribes united under Chieftain Brennus and shattered the Roman expeditionary legion.",
            MatchOutcome.Defeat => "Disciplined Roman cohorts broke through the river ford defenses and razed the Celtic settlement.",
            _ => "The clash between Celtic freedom fighters and imperial Roman legions rages on."
        };

        return new MatchResultSummaryViewModel(
            PlayerFaction: playerFaction,
            Outcome: outcome,
            BannerTitle: title,
            BannerSubtitle: subtitle,
            TotalTicksExecuted: totalTicks,
            MatchDurationSeconds: durationSec,
            TotalKills: kills,
            TotalCasualtiesLost: casualties,
            UnitsTrained: unitsTrained,
            ResourcesHarvestedTotal: resourcesHarvested,
            MvpHeroName: string.IsNullOrEmpty(mvpHeroName) ? "Brennus, Chieftain" : mvpHeroName,
            MvpHeroLevel: Math.Max(1, mvpHeroLevel),
            MvpHeroKills: mvpHeroKills,
            HistoricalSummary: history);
    }
}
