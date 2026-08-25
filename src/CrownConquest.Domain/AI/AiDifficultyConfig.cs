using System;

namespace CrownConquest.Domain.AI;

/// <summary>
/// Authoritative difficulty configuration controlling AI economic rates, decision latency, and tactical modifiers.
/// </summary>
public sealed record AiDifficultyConfig
{
    public AiDifficultyTier Tier { get; init; } = AiDifficultyTier.Normal;
    public float ResourceGatherMultiplier { get; init; } = 1.0f;
    public float BuildSpeedMultiplier { get; init; } = 1.0f;
    public int DecisionIntervalMultiplier { get; init; } = 1;
    public float TargetSwitchingThreshold { get; init; } = 1.0f;
    public float AggressionFactor { get; init; } = 1.0f;
    public float PerceptionRangeMultiplier { get; init; } = 1.0f;

    public static AiDifficultyConfig CreateEasy() => new()
    {
        Tier = AiDifficultyTier.Easy,
        ResourceGatherMultiplier = 0.75f,
        BuildSpeedMultiplier = 0.80f,
        DecisionIntervalMultiplier = 2,
        TargetSwitchingThreshold = 1.50f,
        AggressionFactor = 0.60f,
        PerceptionRangeMultiplier = 0.80f
    };

    public static AiDifficultyConfig CreateNormal() => new()
    {
        Tier = AiDifficultyTier.Normal,
        ResourceGatherMultiplier = 1.00f,
        BuildSpeedMultiplier = 1.00f,
        DecisionIntervalMultiplier = 1,
        TargetSwitchingThreshold = 1.00f,
        AggressionFactor = 1.00f,
        PerceptionRangeMultiplier = 1.00f
    };

    public static AiDifficultyConfig CreateHard() => new()
    {
        Tier = AiDifficultyTier.Hard,
        ResourceGatherMultiplier = 1.25f,
        BuildSpeedMultiplier = 1.20f,
        DecisionIntervalMultiplier = 1,
        TargetSwitchingThreshold = 0.80f,
        AggressionFactor = 1.30f,
        PerceptionRangeMultiplier = 1.20f
    };

    public static AiDifficultyConfig CreateBrutal() => new()
    {
        Tier = AiDifficultyTier.Brutal,
        ResourceGatherMultiplier = 1.50f,
        BuildSpeedMultiplier = 1.40f,
        DecisionIntervalMultiplier = 1,
        TargetSwitchingThreshold = 0.50f,
        AggressionFactor = 1.60f,
        PerceptionRangeMultiplier = 1.50f
    };

    public static AiDifficultyConfig CreateFromTier(AiDifficultyTier tier) => tier switch
    {
        AiDifficultyTier.Easy => CreateEasy(),
        AiDifficultyTier.Normal => CreateNormal(),
        AiDifficultyTier.Hard => CreateHard(),
        AiDifficultyTier.Brutal => CreateBrutal(),
        _ => CreateNormal()
    };

    public static AiDifficultyConfig CreateCustom(
        float gatherMult,
        float buildSpeedMult,
        int decisionIntervalMult,
        float targetSwitchThreshold,
        float aggression,
        float perceptionMult)
    {
        return new AiDifficultyConfig
        {
            Tier = AiDifficultyTier.Custom,
            ResourceGatherMultiplier = Math.Clamp(gatherMult, 0.1f, 5.0f),
            BuildSpeedMultiplier = Math.Clamp(buildSpeedMult, 0.1f, 5.0f),
            DecisionIntervalMultiplier = Math.Clamp(decisionIntervalMult, 1, 10),
            TargetSwitchingThreshold = Math.Clamp(targetSwitchThreshold, 0.1f, 5.0f),
            AggressionFactor = Math.Clamp(aggression, 0.1f, 5.0f),
            PerceptionRangeMultiplier = Math.Clamp(perceptionMult, 0.1f, 5.0f)
        };
    }
}
