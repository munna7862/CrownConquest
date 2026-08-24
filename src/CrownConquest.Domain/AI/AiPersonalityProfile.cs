using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.AI;

/// <summary>
/// AI personality archetypes governing macro strategy, economic goals, risk tolerance, and tactical decisions.
/// </summary>
public enum AiPersonalityType
{
    Aggressive,
    Defensive,
    Expansionist,
    Tactical
}

/// <summary>
/// Domain representation of an AI faction personality profile.
/// </summary>
public sealed class AiPersonalityProfile
{
    public AiPersonalityType PersonalityType { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public float RetreatOddsThreshold { get; set; }
    public float RetreatHealthThreshold { get; set; }
    public int TargetWorkerCount { get; set; }
    public int AttackSquadThreshold { get; set; }
    public float FlankingDesire { get; set; }
    public float ElevationBias { get; set; }
    public bool HeroPreservation { get; set; }
    public FormationType PreferredFormation { get; set; }
    public float BaseDefenseRadius { get; set; }

    public AiPersonalityProfile(
        AiPersonalityType personalityType,
        string name,
        string description,
        float retreatOddsThreshold,
        float retreatHealthThreshold,
        int targetWorkerCount,
        int attackSquadThreshold,
        float flankingDesire,
        float elevationBias,
        bool heroPreservation,
        FormationType preferredFormation,
        float baseDefenseRadius)
    {
        PersonalityType = personalityType;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        RetreatOddsThreshold = Math.Clamp(retreatOddsThreshold, 0.05f, 0.95f);
        RetreatHealthThreshold = Math.Clamp(retreatHealthThreshold, 0.05f, 0.95f);
        TargetWorkerCount = Math.Max(1, targetWorkerCount);
        AttackSquadThreshold = Math.Max(1, attackSquadThreshold);
        FlankingDesire = Math.Clamp(flankingDesire, 0f, 1.0f);
        ElevationBias = Math.Max(0.1f, elevationBias);
        HeroPreservation = heroPreservation;
        PreferredFormation = preferredFormation;
        BaseDefenseRadius = Math.Max(5.0f, baseDefenseRadius);
    }

    public static AiPersonalityProfile CreateAggressive()
    {
        return new AiPersonalityProfile(
            personalityType: AiPersonalityType.Aggressive,
            name: "Aggressive Raider",
            description: "Prioritizes fast military production, early cavalry harassment, wedge formations, and aggressive flanking.",
            retreatOddsThreshold: 0.25f,
            retreatHealthThreshold: 0.20f,
            targetWorkerCount: 12,
            attackSquadThreshold: 6,
            flankingDesire: 1.0f,
            elevationBias: 1.0f,
            heroPreservation: false,
            preferredFormation: FormationType.Wedge,
            baseDefenseRadius: 20.0f);
    }

    public static AiPersonalityProfile CreateDefensive()
    {
        return new AiPersonalityProfile(
            personalityType: AiPersonalityType.Defensive,
            name: "Defensive Bastion",
            description: "Focuses on stone fortifications, defensive towers, Square formations, high-ground staging, and cautious retreats.",
            retreatOddsThreshold: 0.55f,
            retreatHealthThreshold: 0.40f,
            targetWorkerCount: 18,
            attackSquadThreshold: 12,
            flankingDesire: 0.2f,
            elevationBias: 1.4f,
            heroPreservation: false,
            preferredFormation: FormationType.Square,
            baseDefenseRadius: 45.0f);
    }

    public static AiPersonalityProfile CreateExpansionist()
    {
        return new AiPersonalityProfile(
            personalityType: AiPersonalityType.Expansionist,
            name: "Imperial Expansionist",
            description: "Rapidly expands economy and worker population, secures secondary nodes, and builds late-game deathball armies.",
            retreatOddsThreshold: 0.45f,
            retreatHealthThreshold: 0.30f,
            targetWorkerCount: 24,
            attackSquadThreshold: 16,
            flankingDesire: 0.5f,
            elevationBias: 1.1f,
            heroPreservation: false,
            preferredFormation: FormationType.Line,
            baseDefenseRadius: 30.0f);
    }

    public static AiPersonalityProfile CreateTactical()
    {
        return new AiPersonalityProfile(
            personalityType: AiPersonalityType.Tactical,
            name: "Tactical Mastermind",
            description: "Focuses on hero preservation, focus fire coordination, flanking maneuvers, high ground bonuses, and formation counter-picks.",
            retreatOddsThreshold: 0.45f,
            retreatHealthThreshold: 0.30f,
            targetWorkerCount: 15,
            attackSquadThreshold: 8,
            flankingDesire: 0.85f,
            elevationBias: 1.5f,
            heroPreservation: true,
            preferredFormation: FormationType.Line,
            baseDefenseRadius: 30.0f);
    }
}
