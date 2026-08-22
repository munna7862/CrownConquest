using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Economy;

/// <summary>
/// Authoritative state representing a faction's civilization era and advancement progress.
/// </summary>
public sealed class EraState
{
    public FactionId FactionId { get; }
    public CivilizationEra CurrentEra { get; private set; }
    public bool IsAdvancing { get; private set; }
    public CivilizationEra? TargetEra { get; private set; }
    public int ProgressTicks { get; private set; }
    public int DurationTicks { get; private set; }
    public EntityId AdvancementBuildingId { get; private set; }
    public ResourceCost AdvancementCost { get; private set; }

    public float ProgressNormalized => (IsAdvancing && DurationTicks > 0)
        ? Math.Clamp((float)ProgressTicks / DurationTicks, 0f, 1f)
        : (CurrentEra == CivilizationEra.Feudal ? 1f : 0f);

    public EraState(FactionId factionId, CivilizationEra startingEra = CivilizationEra.Archaic)
    {
        FactionId = factionId;
        CurrentEra = startingEra;
        IsAdvancing = false;
        TargetEra = null;
        ProgressTicks = 0;
        DurationTicks = 0;
        AdvancementBuildingId = EntityId.None;
        AdvancementCost = ResourceCost.Zero;
    }

    public bool CanAdvance(CivilizationEra targetEra, out string reason)
    {
        if (IsAdvancing)
        {
            reason = "Era advancement already in progress.";
            return false;
        }

        var expectedNext = CurrentEra.GetNextEra();
        if (expectedNext == null)
        {
            reason = "Already reached maximum civilization era.";
            return false;
        }

        if (targetEra != expectedNext.Value)
        {
            reason = $"Cannot advance directly to {targetEra}. Next era is {expectedNext.Value}.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool TryStartAdvancement(
        CivilizationEra targetEra,
        int durationTicks,
        EntityId buildingId,
        ResourceCost cost,
        ulong tick,
        DomainEventBus? eventBus)
    {
        if (!CanAdvance(targetEra, out _)) return false;

        IsAdvancing = true;
        TargetEra = targetEra;
        DurationTicks = Math.Max(1, durationTicks);
        ProgressTicks = 0;
        AdvancementBuildingId = buildingId;
        AdvancementCost = cost;

        eventBus?.Publish(new EraAdvancementStartedEvent(
            tick,
            FactionId,
            buildingId,
            CurrentEra,
            targetEra,
            DurationTicks));

        return true;
    }

    public void AdvanceTicks(
        int ticks,
        ulong tick,
        DomainEventBus? eventBus,
        out bool completed)
    {
        completed = false;
        if (!IsAdvancing || TargetEra == null) return;

        ProgressTicks += Math.Max(0, ticks);
        eventBus?.Publish(new EraAdvancementProgressEvent(
            tick,
            FactionId,
            TargetEra.Value,
            ProgressTicks,
            DurationTicks));

        if (ProgressTicks >= DurationTicks)
        {
            var oldEra = CurrentEra;
            var newEra = TargetEra.Value;

            CurrentEra = newEra;
            IsAdvancing = false;
            TargetEra = null;
            ProgressTicks = 0;
            DurationTicks = 0;
            AdvancementBuildingId = EntityId.None;
            AdvancementCost = ResourceCost.Zero;
            completed = true;

            eventBus?.Publish(new EraAdvancementCompletedEvent(
                tick,
                FactionId,
                oldEra,
                newEra));
        }
    }

    public ResourceCost CancelAdvancement(ulong tick, DomainEventBus? eventBus)
    {
        if (!IsAdvancing || TargetEra == null) return ResourceCost.Zero;

        var target = TargetEra.Value;
        var refund = AdvancementCost;

        IsAdvancing = false;
        TargetEra = null;
        ProgressTicks = 0;
        DurationTicks = 0;
        AdvancementBuildingId = EntityId.None;
        AdvancementCost = ResourceCost.Zero;

        eventBus?.Publish(new EraAdvancementCancelledEvent(
            tick,
            FactionId,
            target,
            refund));

        return refund;
    }
}
