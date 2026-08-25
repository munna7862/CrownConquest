using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Detailed combat and survival statistics per unit archetype.
/// </summary>
public sealed record ArchetypeBattleMetrics(
    UnitArchetype Archetype,
    int InitialCount,
    int SurvivingCount,
    int Kills,
    int Deaths,
    float DamageDealt,
    float DamageTaken,
    int XpGained)
{
    public float CasualtyRate => InitialCount > 0 ? (float)Deaths / InitialCount : 0f;
    public float SurvivalRate => InitialCount > 0 ? (float)SurvivingCount / InitialCount : 0f;
    public float KillDeathRatio => Deaths > 0 ? (float)Kills / Deaths : Kills;
}

/// <summary>
/// Complete statistical outcome and telemetry report of a simulated battle.
/// </summary>
public sealed record BattleSimulatorResult(
    FactionId? WinnerFaction,
    bool IsDraw,
    ulong DurationTicks,
    float DurationSeconds,
    int InitialUnitsA,
    int InitialUnitsB,
    int SurvivingUnitsA,
    int SurvivingUnitsB,
    int CasualtiesA,
    int CasualtiesB,
    float SurvivingHpA,
    float SurvivingHpB,
    float TotalDamageDealtA,
    float TotalDamageDealtB,
    int TotalXpEarnedA,
    int TotalXpEarnedB,
    float ResourceTradeEfficiencyA,
    float ResourceTradeEfficiencyB,
    ulong FinalStateChecksum,
    IReadOnlyDictionary<UnitArchetype, ArchetypeBattleMetrics> ArchetypeStatsA,
    IReadOnlyDictionary<UnitArchetype, ArchetypeBattleMetrics> ArchetypeStatsB)
{
    public float CasualtyRatioA => InitialUnitsA > 0 ? (float)CasualtiesA / InitialUnitsA : 0f;
    public float CasualtyRatioB => InitialUnitsB > 0 ? (float)CasualtiesB / InitialUnitsB : 0f;
    public float DamageRatioA => TotalDamageDealtB > 0 ? TotalDamageDealtA / TotalDamageDealtB : (TotalDamageDealtA > 0 ? 1f : 0f);
}
