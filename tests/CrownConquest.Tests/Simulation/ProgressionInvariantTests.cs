using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class ProgressionInvariantTests
{
    [Fact]
    public void Progression_KillXpAttribution_ShouldAwardSingleKillerImmediately()
    {
        var sim = new SimulationEngine();
        int xpEventsReceived = 0;
        int killEventsReceived = 0;
        EntityId killerReceived = EntityId.None;

        sim.EventBus.Subscribe<UnitGainedXpEvent>((in UnitGainedXpEvent e) =>
        {
            xpEventsReceived++;
            killerReceived = e.UnitId;
        });

        sim.EventBus.Subscribe<UnitKilledEvent>((in UnitKilledEvent e) =>
        {
            killEventsReceived++;
        });

        // Spawn Attacker (Celtic Swordsman, 100 HP, 50 DMG)
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(
            FactionId.Player1, 0, "celtic_swordsman", new Vector2D(5f, 5f),
            MaxHealth: 100f, AttackDamage: 50f, AttackRange: 2f, MovementSpeed: 4f,
            AttackCooldownTicks: 5, KillXpValue: 50));

        // Spawn Target (Roman Scout, 40 HP, 0 DMG)
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(
            FactionId.Player2, 0, "roman_scout", new Vector2D(6f, 5f),
            MaxHealth: 40f, AttackDamage: 0f, AttackRange: 2f, MovementSpeed: 4f,
            AttackCooldownTicks: 20, KillXpValue: 120));

        sim.Tick(); // Spawns units (Unit 1 and Unit 2)

        var attackerId = new EntityId(1);
        var targetId = new EntityId(2);

        // Order Unit 1 to attack Unit 2
        sim.CommandQueue.Enqueue(new AttackCommand(FactionId.Player1, sim.CurrentTick, [attackerId], targetId));

        // Run simulation until combat concludes (less than 20 ticks)
        sim.SimulateTicks(10);

        Assert.Equal(1, killEventsReceived);
        Assert.Equal(1, xpEventsReceived);
        Assert.Equal(attackerId, killerReceived);

        Assert.True(sim.State.TryGetUnit(attackerId, out var attacker));
        Assert.NotNull(attacker);
        Assert.Equal(1, attacker.Veterancy.KillCount);
        Assert.Equal(120, attacker.Veterancy.CurrentXp);
        // Threshold for Level 2 is 100 XP -> Level should now be 2 immediately!
        Assert.Equal(2, attacker.Veterancy.Level);

        // Dead unit should be removed from active state
        Assert.False(sim.State.TryGetUnit(targetId, out _));
    }

    [Fact]
    public void Progression_VeterancyRankTransitions_ShouldFollowSignatureMilestones()
    {
        var bus = new DomainEventBus();
        var state = new VeterancyState(new EntityId(1), initialLevel: 1);

        Assert.Equal(VeterancyRank.Recruit, state.Rank);

        // Level 2 (100 XP) -> Still Recruit
        state.AwardXp(100, 1, bus, out bool lvl2, out _);
        Assert.True(lvl2);
        Assert.Equal(2, state.Level);
        Assert.Equal(VeterancyRank.Recruit, state.Rank);

        // Level 3 (250 XP total -> need 150 XP) -> Experienced
        state.AwardXp(150, 2, bus, out bool lvl3, out bool rankChanged3);
        Assert.True(lvl3);
        Assert.True(rankChanged3);
        Assert.Equal(3, state.Level);
        Assert.Equal(VeterancyRank.Experienced, state.Rank);

        // Level 5 (700 XP total -> need 450 XP) -> Veteran
        state.AwardXp(450, 3, bus, out bool lvl5, out bool rankChanged5);
        Assert.True(lvl5);
        Assert.True(rankChanged5);
        Assert.Equal(5, state.Level);
        Assert.Equal(VeterancyRank.Veteran, state.Rank);

        // Level 7 (1350 XP total -> need 650 XP) -> Elite
        state.AwardXp(650, 4, bus, out bool lvl7, out bool rankChanged7);
        Assert.True(lvl7);
        Assert.True(rankChanged7);
        Assert.Equal(7, state.Level);
        Assert.Equal(VeterancyRank.Elite, state.Rank);

        // Level 9 (2200 XP total -> need 850 XP) -> Legendary
        state.AwardXp(850, 5, bus, out bool lvl9, out bool rankChanged9);
        Assert.True(lvl9);
        Assert.True(rankChanged9);
        Assert.Equal(9, state.Level);
        Assert.Equal(VeterancyRank.Legendary, state.Rank);
    }
}
