using System;
using System.Collections.Generic;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Configuration for running a batch of automated battle simulations.
/// </summary>
public sealed class BatchBattleConfig
{
    public int Iterations { get; set; } = 100;
    public int BaseSeed { get; set; } = 1000;
    public BattleSimulatorConfig BaseMatchup { get; set; } = new();

    public static BatchBattleConfig Create(BattleSimulatorConfig matchup, int iterations = 100, int baseSeed = 1000)
    {
        return new BatchBattleConfig
        {
            Iterations = iterations,
            BaseSeed = baseSeed,
            BaseMatchup = matchup
        };
    }
}

/// <summary>
/// Aggregated statistical outcomes across a batch of automated battle simulations.
/// </summary>
public sealed record BatchBattleResult(
    int TotalBattles,
    int TeamAWins,
    int TeamBWins,
    int Draws,
    float WinRateA,
    float WinRateB,
    float DrawRate,
    float MeanDurationTicks,
    float StdDevDurationTicks,
    ulong MinDurationTicks,
    ulong MaxDurationTicks,
    float MeanCasualtiesA,
    float MeanCasualtiesB,
    float MeanDamageDealtA,
    float MeanDamageDealtB,
    float MeanTradeEfficiencyA,
    float MeanTradeEfficiencyB,
    IReadOnlyList<string> OutlierFlags,
    IReadOnlyList<BattleSimulatorResult> BattleResults);

/// <summary>
/// Statistical batch runner executing multi-battle balance trials and anomaly detection.
/// </summary>
public sealed class BatchBattleRunner
{
    private readonly BattleSimulatorEngine _simulator = new();

    public BatchBattleResult RunBatch(BatchBattleConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.Iterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config), "Iterations must be greater than 0.");
        }

        var results = new List<BattleSimulatorResult>(config.Iterations);
        int winsA = 0;
        int winsB = 0;
        int draws = 0;

        double sumDuration = 0;
        ulong minDuration = ulong.MaxValue;
        ulong maxDuration = 0;

        double sumCasualtiesA = 0;
        double sumCasualtiesB = 0;
        double sumDamageA = 0;
        double sumDamageB = 0;
        double sumTradeEffA = 0;
        double sumTradeEffB = 0;

        var baseMatchup = config.BaseMatchup;

        for (int i = 0; i < config.Iterations; i++)
        {
            var runConfig = new BattleSimulatorConfig
            {
                TeamA = baseMatchup.TeamA,
                TeamB = baseMatchup.TeamB,
                MapWidth = baseMatchup.MapWidth,
                MapHeight = baseMatchup.MapHeight,
                MaxTicks = baseMatchup.MaxTicks,
                RandomSeed = config.BaseSeed + i,
                DefaultTerrain = baseMatchup.DefaultTerrain,
                AutoEngage = baseMatchup.AutoEngage
            };

            var res = _simulator.ExecuteBattle(runConfig);
            results.Add(res);

            if (res.WinnerFaction == runConfig.TeamA.FactionId) winsA++;
            else if (res.WinnerFaction == runConfig.TeamB.FactionId) winsB++;
            else draws++;

            sumDuration += res.DurationTicks;
            if (res.DurationTicks < minDuration) minDuration = res.DurationTicks;
            if (res.DurationTicks > maxDuration) maxDuration = res.DurationTicks;

            sumCasualtiesA += res.CasualtiesA;
            sumCasualtiesB += res.CasualtiesB;
            sumDamageA += res.TotalDamageDealtA;
            sumDamageB += res.TotalDamageDealtB;
            sumTradeEffA += res.ResourceTradeEfficiencyA;
            sumTradeEffB += res.ResourceTradeEfficiencyB;
        }

        int count = config.Iterations;
        float meanDuration = (float)(sumDuration / count);
        float meanCasA = (float)(sumCasualtiesA / count);
        float meanCasB = (float)(sumCasualtiesB / count);
        float meanDmgA = (float)(sumDamageA / count);
        float meanDmgB = (float)(sumDamageB / count);
        float meanTradeA = (float)(sumTradeEffA / count);
        float meanTradeB = (float)(sumTradeEffB / count);

        // Compute standard deviation of duration
        double varianceSum = 0;
        for (int i = 0; i < results.Count; i++)
        {
            double diff = results[i].DurationTicks - meanDuration;
            varianceSum += diff * diff;
        }
        float stdDevDuration = (float)Math.Sqrt(varianceSum / count);

        float winRateA = (float)winsA / count;
        float winRateB = (float)winsB / count;
        float drawRate = (float)draws / count;

        // Anomaly / Outlier detection
        var outlierFlags = new List<string>();
        if (drawRate > 0.40f)
        {
            outlierFlags.Add($"High draw rate detected ({drawRate:P1}). Potential combat stall or range deadlock.");
        }
        if (stdDevDuration > meanDuration * 0.75f && meanDuration > 100)
        {
            outlierFlags.Add($"High battle duration volatility (StdDev: {stdDevDuration:F1} ticks, Mean: {meanDuration:F1} ticks).");
        }

        return new BatchBattleResult(
            count,
            winsA,
            winsB,
            draws,
            winRateA,
            winRateB,
            drawRate,
            meanDuration,
            stdDevDuration,
            minDuration == ulong.MaxValue ? 0 : minDuration,
            maxDuration,
            meanCasA,
            meanCasB,
            meanDmgA,
            meanDmgB,
            meanTradeA,
            meanTradeB,
            outlierFlags,
            results);
    }
}
