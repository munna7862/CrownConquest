using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Events;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class CivilizationEraTests
{
    [Fact]
    public void EraState_Advancement_ProgressionAndCompletion()
    {
        // TC-S04-001: EraState advances ticks and completes at duration
        var factionId = new FactionId(1);
        var eraState = new EraState(factionId, CivilizationEra.Archaic);
        var bus = new DomainEventBus();

        bool startedEventFired = false;
        bool completedEventFired = false;
        CivilizationEra transitionedEra = CivilizationEra.Archaic;

        bus.Subscribe<EraAdvancementStartedEvent>((in EraAdvancementStartedEvent e) =>
        {
            startedEventFired = true;
            Assert.Equal(CivilizationEra.Archaic, e.FromEra);
            Assert.Equal(CivilizationEra.Classical, e.TargetEra);
        });

        bus.Subscribe<EraAdvancementCompletedEvent>((in EraAdvancementCompletedEvent e) =>
        {
            completedEventFired = true;
            transitionedEra = e.NewEra;
        });

        Assert.Equal(CivilizationEra.Archaic, eraState.CurrentEra);
        Assert.False(eraState.IsAdvancing);
        Assert.Equal(0f, eraState.ProgressNormalized);

        // Start advancement (100 ticks, building 5)
        var buildingId = new EntityId(5);
        var cost = new ResourceCost(Food: 500, Gold: 200);
        bool started = eraState.TryStartAdvancement(CivilizationEra.Classical, 100, buildingId, cost, 1UL, bus);

        Assert.True(started);
        Assert.True(startedEventFired);
        Assert.True(eraState.IsAdvancing);
        Assert.Equal(CivilizationEra.Classical, eraState.TargetEra);

        // Advance 50 ticks (50% progress)
        eraState.AdvanceTicks(50, 51UL, bus, out bool completedEarly);
        Assert.False(completedEarly);
        Assert.Equal(0.5f, eraState.ProgressNormalized, 0.01f);
        Assert.Equal(CivilizationEra.Archaic, eraState.CurrentEra);

        // Advance remaining 50 ticks
        eraState.AdvanceTicks(50, 101UL, bus, out bool completed);
        Assert.True(completed);
        Assert.True(completedEventFired);
        Assert.False(eraState.IsAdvancing);
        Assert.Equal(CivilizationEra.Classical, eraState.CurrentEra);
        Assert.Equal(CivilizationEra.Classical, transitionedEra);
    }

    [Fact]
    public void EraState_Prerequisites_Validation()
    {
        // TC-S04-002: EraState rejects skipping eras or double advancing
        var factionId = new FactionId(1);
        var eraState = new EraState(factionId, CivilizationEra.Archaic);

        // Cannot skip directly to Imperial
        Assert.False(eraState.CanAdvance(CivilizationEra.Imperial, out string skipReason));
        Assert.Contains("Cannot advance directly", skipReason);

        // Can advance to Classical
        Assert.True(eraState.CanAdvance(CivilizationEra.Classical, out _));

        // Start advancing to Classical
        eraState.TryStartAdvancement(CivilizationEra.Classical, 100, new EntityId(1), ResourceCost.Zero, 1UL, null);

        // While advancing, cannot start another advancement
        Assert.False(eraState.CanAdvance(CivilizationEra.Classical, out string inProgressReason));
        Assert.Contains("already in progress", inProgressReason);
    }

    [Fact]
    public void EraState_Cancellation_RefundsCostAndResets()
    {
        var factionId = new FactionId(1);
        var eraState = new EraState(factionId, CivilizationEra.Archaic);
        var bus = new DomainEventBus();

        bool cancelEventFired = false;
        bus.Subscribe<EraAdvancementCancelledEvent>((in EraAdvancementCancelledEvent e) =>
        {
            cancelEventFired = true;
            Assert.Equal(500, e.RefundedCost.Food);
        });

        var cost = new ResourceCost(Food: 500, Gold: 200);
        eraState.TryStartAdvancement(CivilizationEra.Classical, 100, new EntityId(1), cost, 1UL, bus);
        eraState.AdvanceTicks(30, 31UL, bus, out _);

        var refund = eraState.CancelAdvancement(32UL, bus);

        Assert.True(cancelEventFired);
        Assert.Equal(500, refund.Food);
        Assert.Equal(200, refund.Gold);
        Assert.False(eraState.IsAdvancing);
        Assert.Equal(CivilizationEra.Archaic, eraState.CurrentEra);
    }

    [Fact]
    public void CivilizationEra_DisplayNamesAndNextEra()
    {
        Assert.Equal("Tribal / Archaic Era", CivilizationEra.Archaic.GetDisplayName());
        Assert.Equal("Bronze / Classical Era", CivilizationEra.Classical.GetDisplayName());
        Assert.Equal("Iron / Imperial Era", CivilizationEra.Imperial.GetDisplayName());
        Assert.Equal("Feudal / Sovereign Era", CivilizationEra.Feudal.GetDisplayName());

        Assert.Equal(CivilizationEra.Classical, CivilizationEra.Archaic.GetNextEra());
        Assert.Equal(CivilizationEra.Imperial, CivilizationEra.Classical.GetNextEra());
        Assert.Equal(CivilizationEra.Feudal, CivilizationEra.Imperial.GetNextEra());
        Assert.Null(CivilizationEra.Feudal.GetNextEra());
    }
}
