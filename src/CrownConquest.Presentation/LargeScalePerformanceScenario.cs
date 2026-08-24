using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Profiling;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Headless scenario harness for large-scale battle benchmarks (100, 250, 500 units).
/// Verifies performance targets, tick budgets, memory stability, and spatial query throughput.
/// </summary>
public sealed class LargeScalePerformanceScenario
{
    public GameCoordinator Coordinator { get; }
    public PerformanceHudPresenter Presenter { get; }
    public List<UnitKilledEvent> KilledEvents { get; } = new(512);

    public LargeScalePerformanceScenario(GameCoordinator? coordinator = null)
    {
        Coordinator = coordinator ?? new GameCoordinator();
        Presenter = new PerformanceHudPresenter();

        Coordinator.EventBus.Subscribe<UnitKilledEvent>((in UnitKilledEvent e) => KilledEvents.Add(e));
    }

    /// <summary>
    /// Deploys an opposing army with total units = unitCount (split equally between Faction 1 and Faction 2).
    /// </summary>
    public void DeployArmies(int totalUnits = 100)
    {
        int unitsPerSide = Math.Max(1, totalUnits / 2);
        int rows = Math.Max(1, (int)Math.Sqrt(unitsPerSide));
        int cols = (int)Math.Ceiling((double)unitsPerSide / rows);

        // Faction 1: West Army (starts at X: 30)
        int f1Spawned = 0;
        for (int r = 0; r < rows && f1Spawned < unitsPerSide; r++)
        {
            for (int c = 0; c < cols && f1Spawned < unitsPerSide; c++)
            {
                float x = 20f + (c * 2.0f);
                float y = 20f + (r * 2.5f);
                string unitType = GetUnitTypeForIndex(f1Spawned);

                Coordinator.DispatchCommand(new SpawnUnitCommand(
                    FactionId.Player1,
                    SubmittedTick: 0,
                    UnitType: unitType,
                    Position: new Vector2D(x, y),
                    MaxHealth: 120f,
                    AttackDamage: 15f,
                    AttackRange: unitType.Contains("archer") ? 8.0f : 1.5f,
                    MovementSpeed: 3.5f,
                    AttackCooldownTicks: 20,
                    KillXpValue: 50,
                    Armor: 2.0f,
                    AttackType: unitType.Contains("archer") ? "ranged" : "melee",
                    AggroRange: 12.0f));

                f1Spawned++;
            }
        }

        // Faction 2: East Army (starts at X: 80)
        int f2Spawned = 0;
        for (int r = 0; r < rows && f2Spawned < unitsPerSide; r++)
        {
            for (int c = 0; c < cols && f2Spawned < unitsPerSide; c++)
            {
                float x = 80f - (c * 2.0f);
                float y = 20f + (r * 2.5f);
                string unitType = GetUnitTypeForIndex(f2Spawned);

                Coordinator.DispatchCommand(new SpawnUnitCommand(
                    new FactionId(2),
                    SubmittedTick: 0,
                    UnitType: unitType,
                    Position: new Vector2D(x, y),
                    MaxHealth: 120f,
                    AttackDamage: 15f,
                    AttackRange: unitType.Contains("archer") ? 8.0f : 1.5f,
                    MovementSpeed: 3.5f,
                    AttackCooldownTicks: 20,
                    KillXpValue: 50,
                    Armor: 2.0f,
                    AttackType: unitType.Contains("archer") ? "ranged" : "melee",
                    AggroRange: 12.0f));

                f2Spawned++;
            }
        }

        // Step 1 tick to flush spawn commands
        Coordinator.Simulation.Tick();
    }

    /// <summary>
    /// Issues movement orders for both armies to charge into the center of the battlefield.
    /// </summary>
    public void OrderCenterCharge()
    {
        var f1Units = new List<EntityId>();
        var f2Units = new List<EntityId>();

        var allUnits = Coordinator.Simulation.State.ActiveUnits;
        for (int i = 0; i < allUnits.Count; i++)
        {
            var u = allUnits[i];
            if (u.FactionId == FactionId.Player1) f1Units.Add(u.Id);
            else if (u.FactionId.Value == 2) f2Units.Add(u.Id);
        }

        if (f1Units.Count > 0)
        {
            Coordinator.DispatchCommand(new MoveCommand(
                FactionId.Player1,
                Coordinator.CurrentTick,
                f1Units.ToArray(),
                new Vector2D(55f, 30f)));
        }

        if (f2Units.Count > 0)
        {
            Coordinator.DispatchCommand(new MoveCommand(
                new FactionId(2),
                Coordinator.CurrentTick,
                f2Units.ToArray(),
                new Vector2D(45f, 30f)));
        }
    }

    /// <summary>
    /// Executes clash simulation for specified tick count and returns performance telemetry snapshot.
    /// </summary>
    public PerformanceMetrics RunClash(int tickCount = 100)
    {
        for (int t = 0; t < tickCount; t++)
        {
            Coordinator.Simulation.Tick();
        }

        return Coordinator.Simulation.Profiler.GetSnapshot(
            Coordinator.CurrentTick,
            Coordinator.Simulation.State.ActiveUnits.Count,
            Coordinator.Simulation.State.ActiveBuildings.Count);
    }

    private static string GetUnitTypeForIndex(int index)
    {
        return (index % 4) switch
        {
            0 => "swordsman",
            1 => "archer",
            2 => "spearman",
            _ => "cavalry"
        };
    }
}
