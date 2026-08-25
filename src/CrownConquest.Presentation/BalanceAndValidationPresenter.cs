using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

#region View Models

/// <summary>
/// Immutable view model for battle simulator HUD displays.
/// </summary>
public readonly record struct BattleSimulationViewModel(
    string WinnerName,
    bool IsDraw,
    ulong DurationTicks,
    float DurationSeconds,
    int InitialA,
    int InitialB,
    int SurvivingA,
    int SurvivingB,
    int CasualtiesA,
    int CasualtiesB,
    float DamageDealtA,
    float DamageDealtB,
    int XpEarnedA,
    int XpEarnedB,
    float TradeEfficiencyA,
    float TradeEfficiencyB,
    string StatusSummary);

/// <summary>
/// Immutable view model for batch win-rate distribution graphs.
/// </summary>
public readonly record struct BatchBalanceViewModel(
    int TotalBattles,
    int WinsA,
    int WinsB,
    int Draws,
    float WinRateA,
    float WinRateB,
    float DrawRate,
    float MeanDurationTicks,
    float StdDevDurationTicks,
    string SummaryText,
    bool HasOutliers,
    string OutlierDetails);

/// <summary>
/// Immutable view model for faction matchup matrices and asymmetry ratings.
/// </summary>
public readonly record struct FactionBalanceViewModel(
    int TotalMatchups,
    float AsymmetryScore,
    string FormattedTextReport,
    bool HasBalanceWarnings);

/// <summary>
/// Immutable view model for soak test telemetry monitors.
/// </summary>
public readonly record struct SoakTelemetryViewModel(
    bool IsSuccessful,
    int TicksExecuted,
    long ElapsedMs,
    float TicksPerSecond,
    float PeakMemoryMb,
    float FinalMemoryMb,
    int UnitsSpawned,
    int UnitsKilled,
    bool IsMemoryBounded,
    string StatusMessage);

#endregion

/// <summary>
/// Presenter projecting simulation balance metrics, battle simulator outcomes, and soak telemetry into UI view models.
/// </summary>
public sealed class BalanceAndValidationPresenter
{
    private readonly BalanceValidationCoordinator _coordinator;

    public BalanceAndValidationPresenter(BalanceValidationCoordinator coordinator)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    public BattleSimulationViewModel BuildBattleViewModel(BattleSimulatorResult result, string nameA = "Team A", string nameB = "Team B")
    {
        ArgumentNullException.ThrowIfNull(result);

        string winner = result.WinnerFaction.HasValue
            ? (result.WinnerFaction.Value.Value == 1 ? nameA : nameB)
            : (result.IsDraw ? "Draw" : "Undecided");

        string summary = result.IsDraw
            ? $"Battle resolved in a Draw after {result.DurationTicks} ticks ({result.DurationSeconds:F1}s)."
            : $"{winner} achieved victory in {result.DurationTicks} ticks ({result.DurationSeconds:F1}s). Casualties: {nameA} ({result.CasualtiesA}/{result.InitialUnitsA}), {nameB} ({result.CasualtiesB}/{result.InitialUnitsB}).";

        return new BattleSimulationViewModel(
            winner,
            result.IsDraw,
            result.DurationTicks,
            result.DurationSeconds,
            result.InitialUnitsA,
            result.InitialUnitsB,
            result.SurvivingUnitsA,
            result.SurvivingUnitsB,
            result.CasualtiesA,
            result.CasualtiesB,
            result.TotalDamageDealtA,
            result.TotalDamageDealtB,
            result.TotalXpEarnedA,
            result.TotalXpEarnedB,
            result.ResourceTradeEfficiencyA,
            result.ResourceTradeEfficiencyB,
            summary);
    }

    public BatchBalanceViewModel BuildBatchViewModel(BatchBattleResult result, string nameA = "Team A", string nameB = "Team B")
    {
        ArgumentNullException.ThrowIfNull(result);

        string summary = $"{result.TotalBattles} battles evaluated. {nameA} Win Rate: {result.WinRateA:P1}, {nameB} Win Rate: {result.WinRateB:P1}, Draws: {result.DrawRate:P1}. Avg Duration: {result.MeanDurationTicks:F1} ticks (σ={result.StdDevDurationTicks:F1}).";
        bool hasOutliers = result.OutlierFlags.Count > 0;
        string outlierText = hasOutliers ? string.Join("; ", result.OutlierFlags) : "No anomalies detected.";

        return new BatchBalanceViewModel(
            result.TotalBattles,
            result.TeamAWins,
            result.TeamBWins,
            result.Draws,
            result.WinRateA,
            result.WinRateB,
            result.DrawRate,
            result.MeanDurationTicks,
            result.StdDevDurationTicks,
            summary,
            hasOutliers,
            outlierText);
    }

    public FactionBalanceViewModel BuildFactionViewModel(FactionBalanceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new FactionBalanceViewModel(
            report.TotalMatchups,
            report.OverallAsymmetryScore,
            report.GenerateFormattedReport(),
            report.BalanceWarnings.Count > 0);
    }

    public SoakTelemetryViewModel BuildSoakViewModel(SoakTestResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new SoakTelemetryViewModel(
            result.IsSuccessful,
            result.TotalTicksExecuted,
            result.ElapsedMilliseconds,
            result.TicksPerSecond,
            result.PeakMemoryMb,
            result.FinalMemoryMb,
            result.TotalUnitsSpawned,
            result.TotalUnitsKilled,
            result.IsMemoryBounded,
            result.SummaryDetails);
    }
}
