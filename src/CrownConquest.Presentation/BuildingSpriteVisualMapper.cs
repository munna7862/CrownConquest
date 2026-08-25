using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

/// <summary>
/// Construction phase of a building structure.
/// </summary>
public enum BuildingConstructionStage : byte
{
    Scaffolding = 0,    // 0% - 33% (Wooden poles and foundation ropes)
    HalfBuilt = 1,      // 34% - 99% (Timber framing, stone courses, partial roof)
    Completed = 2       // 100% (Full architectural structure)
}

/// <summary>
/// Visual damage and smoke/fire state of a building based on health percentage.
/// </summary>
public enum BuildingDamageVfxState : byte
{
    None = 0,               // HP >= 50%
    LightSmoke = 1,         // 25% <= HP < 50% (Thin grey smoke plumes)
    HeavyFireAndSmoke = 2   // HP < 25% (Blazing orange flames & billowing black smoke)
}

/// <summary>
/// Architectural culture / style of a building.
/// </summary>
public enum ArchitecturalStyle : byte
{
    CelticThatched = 0,
    RomanMasonry = 1
}

/// <summary>
/// Comprehensive visual descriptor for 2D illustrated building rendering.
/// </summary>
public readonly record struct BuildingSpriteDescriptor(
    EntityId BuildingId,
    FactionId FactionId,
    string BuildingType,
    ArchitecturalStyle Style,
    Vector2D Position,
    Vector2D Size,
    BuildingConstructionStage Stage,
    float BuildProgress,
    float HealthPercentage,
    BuildingDamageVfxState DamageVfx,
    bool HasChimneySmoke,
    Vector2D ChimneyPosition,
    int ScaffoldingPlankCount);

/// <summary>
/// Maps simulation BuildingEntity state into illustrated 2D building sprite descriptors,
/// scaffolding stages, architectural styles, and dynamic fire/smoke damage particles.
/// </summary>
public static class BuildingSpriteVisualMapper
{
    public static BuildingConstructionStage GetStage(BuildingEntity building)
    {
        ArgumentNullException.ThrowIfNull(building);
        if (building.IsConstructed || building.BuildProgressNormalized >= 1.0f)
            return BuildingConstructionStage.Completed;

        if (building.BuildProgressNormalized < 0.34f)
            return BuildingConstructionStage.Scaffolding;

        return BuildingConstructionStage.HalfBuilt;
    }

    public static BuildingDamageVfxState GetDamageVfxState(BuildingEntity building)
    {
        ArgumentNullException.ThrowIfNull(building);
        if (!building.IsConstructed || building.MaxHealth <= 0f)
            return BuildingDamageVfxState.None;

        float hpPct = building.CurrentHealth / building.MaxHealth;
        if (hpPct >= 0.50f) return BuildingDamageVfxState.None;
        if (hpPct >= 0.25f) return BuildingDamageVfxState.LightSmoke;
        return BuildingDamageVfxState.HeavyFireAndSmoke;
    }

    public static ArchitecturalStyle GetStyle(FactionId faction)
    {
        return faction == FactionId.Player2 ? ArchitecturalStyle.RomanMasonry : ArchitecturalStyle.CelticThatched;
    }

    public static BuildingSpriteDescriptor GetDescriptor(BuildingEntity building, ulong currentTick = 0)
    {
        ArgumentNullException.ThrowIfNull(building);

        var stage = GetStage(building);
        var damageVfx = GetDamageVfxState(building);
        var style = GetStyle(building.FactionId);
        float hpPct = building.MaxHealth > 0f ? Math.Clamp(building.CurrentHealth / building.MaxHealth, 0f, 1f) : 0f;

        bool isBlacksmith = building.BuildingType.Equals("blacksmith", StringComparison.OrdinalIgnoreCase) ||
                            building.BuildingType.Equals("Blacksmith", StringComparison.OrdinalIgnoreCase);
        bool hasChimney = isBlacksmith && stage == BuildingConstructionStage.Completed && building.IsAlive;

        // Chimney position offset
        var chimneyPos = new Vector2D(building.Position.X + (building.GridSize.X * 0.25f), building.Position.Y - (building.GridSize.Y * 0.35f));

        int plankCount = stage switch
        {
            BuildingConstructionStage.Scaffolding => 6,
            BuildingConstructionStage.HalfBuilt => 3,
            _ => 0
        };

        return new BuildingSpriteDescriptor(
            BuildingId: building.Id,
            FactionId: building.FactionId,
            BuildingType: building.BuildingType,
            Style: style,
            Position: building.Position,
            Size: building.GridSize,
            Stage: stage,
            BuildProgress: building.BuildProgressNormalized,
            HealthPercentage: hpPct,
            DamageVfx: damageVfx,
            HasChimneySmoke: hasChimney,
            ChimneyPosition: chimneyPos,
            ScaffoldingPlankCount: plankCount);
    }
}
