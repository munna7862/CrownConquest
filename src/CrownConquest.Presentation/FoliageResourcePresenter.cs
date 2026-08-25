using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

/// <summary>
/// Illustrated foliage and natural resource visual types.
/// </summary>
public enum FoliageResourceType : byte
{
    OakTree = 0,
    PineTree = 1,
    TreeStump = 2,
    GoldVein = 3,
    StoneBoulder = 4,
    IronOutcropping = 5,
    BerryBush = 6
}

/// <summary>
/// Visual snapshot of a natural resource node with dynamic harvesting visuals,
/// sparkling highlights, and persistent stumps.
/// </summary>
public readonly record struct FoliageVisualState(
    EntityId NodeId,
    ResourceType ResourceType,
    FoliageResourceType FoliageType,
    Vector2D Position,
    int RemainingAmount,
    int MaxAmount,
    float DepletionRatio,
    float VisualScale,
    bool IsStump,
    float SparklePhase,
    int BerryClusterCount,
    bool EmitsMiningDust);

/// <summary>
/// Presenter calculating 2D natural resource visuals, tree foliage rustle,
/// gold ore glittering highlights, boulder fracturing stages, and berry bush fullness.
/// </summary>
public static class FoliageResourcePresenter
{
    public static FoliageResourceType GetFoliageType(ResourceType resourceType, EntityId id, bool isDepleted)
    {
        if (resourceType == ResourceType.Wood)
        {
            if (isDepleted) return FoliageResourceType.TreeStump;
            return (id.Value % 2 == 0) ? FoliageResourceType.OakTree : FoliageResourceType.PineTree;
        }

        return resourceType switch
        {
            ResourceType.Gold => FoliageResourceType.GoldVein,
            ResourceType.Stone => FoliageResourceType.StoneBoulder,
            ResourceType.Iron => FoliageResourceType.IronOutcropping,
            ResourceType.Food => FoliageResourceType.BerryBush,
            _ => FoliageResourceType.OakTree
        };
    }

    public static FoliageVisualState GetState(ResourceNodeEntity node, ulong currentTick = 0)
    {
        ArgumentNullException.ThrowIfNull(node);

        float depletionRatio = node.MaxAmount > 0 ? (float)node.RemainingAmount / node.MaxAmount : 0f;
        depletionRatio = Math.Clamp(depletionRatio, 0f, 1f);

        bool isDepleted = node.IsDepleted || node.RemainingAmount <= 0;
        var foliageType = GetFoliageType(node.ResourceType, node.Id, isDepleted);

        // Visual scale: Gold and boulders shrink as harvested (0.5 minimum)
        float scale = 1.0f;
        if (node.ResourceType == ResourceType.Gold || node.ResourceType == ResourceType.Stone || node.ResourceType == ResourceType.Iron)
        {
            scale = 0.5f + (0.5f * depletionRatio);
        }

        // Sparkling highlights phase for gold ore (0.0 to 1.0 cycle)
        float sparklePhase = (float)((currentTick + (ulong)(node.Id.Value * 7)) % 30) / 30f;

        // Berry clusters count (4 max -> 0 when empty)
        int berryCount = (int)MathF.Ceiling(depletionRatio * 4f);

        bool emitsMiningDust = (node.ResourceType == ResourceType.Stone || node.ResourceType == ResourceType.Iron) &&
                               !isDepleted && (currentTick % 10 < 3);

        return new FoliageVisualState(
            NodeId: node.Id,
            ResourceType: node.ResourceType,
            FoliageType: foliageType,
            Position: node.Position,
            RemainingAmount: node.RemainingAmount,
            MaxAmount: node.MaxAmount,
            DepletionRatio: depletionRatio,
            VisualScale: scale,
            IsStump: foliageType == FoliageResourceType.TreeStump,
            SparklePhase: sparklePhase,
            BerryClusterCount: berryCount,
            EmitsMiningDust: emitsMiningDust);
    }
}
