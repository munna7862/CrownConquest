using System;

namespace CrownConquest.Data.Models;

/// <summary>
/// External data definition model for AI personality archetypes.
/// </summary>
public sealed class AiPersonalityDefinitionModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float RetreatOddsThreshold { get; set; } = 0.45f;
    public float RetreatHealthThreshold { get; set; } = 0.30f;
    public int TargetWorkerCount { get; set; } = 15;
    public int AttackSquadThreshold { get; set; } = 8;
    public float FlankingDesire { get; set; } = 0.5f;
    public float ElevationBias { get; set; } = 1.0f;
    public bool HeroPreservation { get; set; } = false;
    public string PreferredFormation { get; set; } = "Line";
    public float BaseDefenseRadius { get; set; } = 30.0f;
}
