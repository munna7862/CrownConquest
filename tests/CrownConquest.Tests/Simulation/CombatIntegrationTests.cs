using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using Xunit;

namespace CrownConquest.Tests.Simulation;

public class CombatIntegrationTests
{
    [Fact]
    public void Selection_SinglePointSelection_ShouldSelectFriendlyUnitOnly()
    {
        var coordinator = new GameCoordinator();
        var selection = new SelectionManager(coordinator, FactionId.Player1);

        // Spawn P1 unit at (10, 10) and P2 unit at (15, 10)
        coordinator.DispatchCommand(new SpawnUnitCommand(FactionId.Player1, 0, "celtic_swordsman", new Vector2D(10f, 10f)));
        coordinator.DispatchCommand(new SpawnUnitCommand(FactionId.Player2, 0, "roman_legionary", new Vector2D(15f, 10f)));
        coordinator.Simulation.Tick();

        // Point click near P1 unit
        bool selectedP1 = selection.SelectPoint(new Vector2D(10.2f, 10.1f));
        Assert.True(selectedP1);
        Assert.Single(selection.SelectedUnitIds);
        Assert.Equal(new EntityId(1), selection.SelectedUnitIds[0]);

        // Point click near P2 unit (enemy) -> should NOT select enemy
        bool selectedP2 = selection.SelectPoint(new Vector2D(15.1f, 9.9f));
        Assert.False(selectedP2);
        Assert.Empty(selection.SelectedUnitIds);
    }

    [Fact]
    public void Selection_DragBoxSelection_FiltersFaction_ShouldSelectOnlyFriendlyUnits()
    {
        var coordinator = new GameCoordinator();
        var selection = new SelectionManager(coordinator, FactionId.Player1);

        // Spawn 3 P1 units and 2 P2 units inside (10, 10) to (30, 30)
        coordinator.DispatchCommand(new SpawnUnitCommand(FactionId.Player1, 0, "celtic_swordsman", new Vector2D(12f, 12f)));
        coordinator.DispatchCommand(new SpawnUnitCommand(FactionId.Player1, 0, "celtic_swordsman", new Vector2D(15f, 15f)));
        coordinator.DispatchCommand(new SpawnUnitCommand(FactionId.Player1, 0, "celtic_archer", new Vector2D(18f, 18f)));
        coordinator.DispatchCommand(new SpawnUnitCommand(FactionId.Player2, 0, "roman_legionary", new Vector2D(20f, 20f)));
        coordinator.DispatchCommand(new SpawnUnitCommand(FactionId.Player2, 0, "roman_veles", new Vector2D(25f, 25f)));
        coordinator.Simulation.Tick();

        var marqueeBox = new Rect2D(10f, 10f, 30f, 30f);
        int selectedCount = selection.SelectBox(marqueeBox);

        Assert.Equal(3, selectedCount);
        Assert.Equal(3, selection.SelectedUnitIds.Count);
        foreach (var id in selection.SelectedUnitIds)
        {
            Assert.True(coordinator.Simulation.State.TryGetUnit(id, out var u) && u?.FactionId == FactionId.Player1);
        }
    }

    [Fact]
    public void Movement_MultiUnitFormationSpacing_ShouldMoveToDistinctSlots()
    {
        var coordinator = new GameCoordinator();
        var selection = new SelectionManager(coordinator, FactionId.Player1);

        // Spawn 4 P1 units
        for (int i = 0; i < 4; i++)
        {
            coordinator.DispatchCommand(new SpawnUnitCommand(FactionId.Player1, 0, "celtic_swordsman", new Vector2D(10f, 10f + (i * 2f))));
        }
        coordinator.Simulation.Tick();

        selection.SelectBox(new Rect2D(5f, 5f, 20f, 20f));
        Assert.Equal(4, selection.SelectedUnitIds.Count);

        // Issue formation move towards (60, 60)
        selection.IssueMoveOrder(new Vector2D(60f, 60f));

        // Advance simulation until all units arrive
        coordinator.Simulation.SimulateTicks(400);

        var units = coordinator.Simulation.State.ActiveUnits;
        Assert.Equal(4, units.Count);

        // Verify units are around (60, 60) and not stacked on identical positions
        for (int i = 0; i < units.Count; i++)
        {
            Assert.True(units[i].Position.DistanceTo(new Vector2D(60f, 60f)) < 5.0f);
            for (int j = i + 1; j < units.Count; j++)
            {
                float distBetween = units[i].Position.DistanceTo(units[j].Position);
                Assert.True(distBetween >= 1.0f, $"Units {units[i].Id} and {units[j].Id} stacked too closely: {distBetween:F2}");
            }
        }
    }

    [Fact]
    public void Combat_MeleeEngagementAndCooldown_ShouldRespectCooldownTicks()
    {
        var sim = new SimulationEngine();
        int damageEventsCount = 0;

        sim.EventBus.Subscribe<DamageDealtEvent>((in DamageDealtEvent e) =>
        {
            if (e.AttackerId == new EntityId(1))
            {
                damageEventsCount++;
            }
        });

        // Spawn 1 melee attacker (AttackCooldown = 10 ticks) and 1 target
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(
            FactionId.Player1, 0, "celtic_swordsman", new Vector2D(10f, 10f),
            MaxHealth: 200f, AttackDamage: 10f, AttackRange: 2.0f, AttackCooldownTicks: 10));

        sim.CommandQueue.Enqueue(new SpawnUnitCommand(
            FactionId.Player2, 0, "roman_legionary", new Vector2D(11f, 10f),
            MaxHealth: 200f, AttackDamage: 0f, AttackRange: 2.0f, AttackCooldownTicks: 100));

        sim.Tick();

        var attackerId = new EntityId(1);
        var targetId = new EntityId(2);

        sim.CommandQueue.Enqueue(new AttackCommand(FactionId.Player1, sim.CurrentTick, [attackerId], targetId));

        // In 25 ticks: First strike at tick 2, second at tick 12, third at tick 22 -> exactly 3 strikes
        sim.SimulateTicks(25);

        Assert.Equal(3, damageEventsCount);
    }

    [Fact]
    public void Combat_RangedEngagementAtRange_ShouldAttackWithoutClosingToMelee()
    {
        var sim = new SimulationEngine();
        int damageEvents = 0;
        sim.EventBus.Subscribe<DamageDealtEvent>((in DamageDealtEvent e) => damageEvents++);

        // Spawn Archer with Range 8.0 at (10, 10)
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(
            FactionId.Player1, 0, "celtic_archer", new Vector2D(10f, 10f),
            MaxHealth: 100f, AttackDamage: 15f, AttackRange: 8.0f, AttackCooldownTicks: 10));

        // Spawn Target at (15, 10) -> distance = 5.0 (well within 8.0 range)
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(
            FactionId.Player2, 0, "roman_dummy", new Vector2D(15f, 10f),
            MaxHealth: 100f, AttackDamage: 0f, AttackRange: 1.5f));

        sim.Tick();

        var archerId = new EntityId(1);
        var targetId = new EntityId(2);

        sim.CommandQueue.Enqueue(new AttackCommand(FactionId.Player1, sim.CurrentTick, [archerId], targetId));
        sim.SimulateTicks(15);

        Assert.True(damageEvents > 0);
        Assert.True(sim.State.TryGetUnit(archerId, out var archer));
        Assert.NotNull(archer);

        // Archer did not need to move to melee range (1.5); should stay near starting position (10, 10)
        Assert.True(archer.Position.DistanceTo(new Vector2D(10f, 10f)) < 0.5f);
    }

    [Fact]
    public void Combat_AutoAcquireHostilesInAggroRange_ShouldAutomaticallyAttackNearbyEnemy()
    {
        var sim = new SimulationEngine();

        // Spawn Idle defender at (20, 20) with AggroRange = 10
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(
            FactionId.Player1, 0, "celtic_defender", new Vector2D(20f, 20f),
            MaxHealth: 100f, AttackDamage: 20f, AttackRange: 2.0f, AggroRange: 10.0f));

        // Spawn Enemy at (24, 20) -> distance 4.0 <= 10.0
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(
            FactionId.Player2, 0, "roman_invader", new Vector2D(24f, 20f),
            MaxHealth: 100f, AttackDamage: 0f, AttackRange: 1.5f));

        sim.Tick(); // Spawn both units

        // Step simulation 5 ticks without any explicit commands
        sim.SimulateTicks(5);

        Assert.True(sim.State.TryGetUnit(new EntityId(1), out var defender));
        Assert.NotNull(defender);

        // Defender should have auto-acquired Unit 2 as attack target
        Assert.Equal(new EntityId(2), defender.AttackTargetId);
        Assert.Equal(UnitState.Attacking, defender.State);
    }

    [Fact]
    public void Progression_ImmediateLevelUp_StatIncrease_ShouldScaleHealthAndDamage()
    {
        var sim = new SimulationEngine();

        // Spawn Attacker (Level 1, BaseMaxHealth=100, BaseDamage=15, HPBonus=20, DmgBonus=5)
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(
            FactionId.Player1, 0, "celtic_swordsman", new Vector2D(10f, 10f),
            MaxHealth: 100f, AttackDamage: 15f, AttackRange: 2.0f, HealthPerLevelBonus: 20f, DamagePerLevelBonus: 5f));

        // Spawn Target with 150 KillXp (enough for Level 2)
        sim.CommandQueue.Enqueue(new SpawnUnitCommand(
            FactionId.Player2, 0, "roman_target", new Vector2D(11f, 10f),
            MaxHealth: 10f, AttackDamage: 0f, KillXpValue: 150));

        sim.Tick();

        sim.CommandQueue.Enqueue(new AttackCommand(FactionId.Player1, sim.CurrentTick, [new EntityId(1)], new EntityId(2)));
        sim.SimulateTicks(10);

        Assert.True(sim.State.TryGetUnit(new EntityId(1), out var unit));
        Assert.NotNull(unit);
        Assert.Equal(2, unit.Veterancy.Level);

        // MaxHealth = 100 + (1 * 20) = 120
        Assert.Equal(120f, unit.MaxHealth);
        // AttackDamage = 15 + (1 * 5) = 20
        Assert.Equal(20f, unit.AttackDamage);
    }
}
