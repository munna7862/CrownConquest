using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Authoritative builder for setting up and executing the 10v10 Celtic vs Roman combat arena scenario.
/// </summary>
public sealed class CombatArenaScenario
{
    public const int CelticSwordsmenCount = 6;
    public const int CelticArchersCount = 4;
    public const int RomanLegionariesCount = 6;
    public const int RomanVelesCount = 4;

    public GameCoordinator Coordinator { get; }
    public SelectionManager Selection { get; }
    public List<UnitLevelUpEvent> LevelUpEvents { get; } = new();
    public List<VeterancyRankChangedEvent> RankChangedEvents { get; } = new();
    public List<UnitKilledEvent> KilledEvents { get; } = new();

    public CombatArenaScenario(GameCoordinator? coordinator = null)
    {
        Coordinator = coordinator ?? new GameCoordinator();
        Selection = new SelectionManager(Coordinator, FactionId.Player1);

        // Record events for presentation and verification
        Coordinator.EventBus.Subscribe<UnitLevelUpEvent>((in UnitLevelUpEvent e) => LevelUpEvents.Add(e));
        Coordinator.EventBus.Subscribe<VeterancyRankChangedEvent>((in VeterancyRankChangedEvent e) => RankChangedEvents.Add(e));
        Coordinator.EventBus.Subscribe<UnitKilledEvent>((in UnitKilledEvent e) => KilledEvents.Add(e));
    }

    /// <summary>
    /// Deploys 10 Celtic units (West) vs 10 Roman units (East) onto the battlefield.
    /// </summary>
    public void Deploy10v10Forces()
    {
        // 1. Deploy 6 Celtic Swordsmen
        for (int i = 0; i < CelticSwordsmenCount; i++)
        {
            float y = 35f + (i * 5f);
            Coordinator.DispatchCommand(new SpawnUnitCommand(
                FactionId.Player1,
                SubmittedTick: 0,
                UnitType: "celtic_swordsman",
                Position: new Vector2D(25f, y),
                MaxHealth: 120f,
                AttackDamage: 18f,
                AttackRange: 1.5f,
                MovementSpeed: 3.6f,
                AttackCooldownTicks: 18,
                KillXpValue: 60,
                Armor: 3.0f,
                AttackType: "melee",
                AggroRange: 15.0f));
        }

        // 2. Deploy 4 Celtic Archers behind swordsmen
        for (int i = 0; i < CelticArchersCount; i++)
        {
            float y = 38f + (i * 7f);
            Coordinator.DispatchCommand(new SpawnUnitCommand(
                FactionId.Player1,
                SubmittedTick: 0,
                UnitType: "celtic_archer",
                Position: new Vector2D(18f, y),
                MaxHealth: 80f,
                AttackDamage: 14f,
                AttackRange: 8.0f,
                MovementSpeed: 3.8f,
                AttackCooldownTicks: 22,
                KillXpValue: 50,
                Armor: 1.0f,
                AttackType: "ranged",
                AggroRange: 16.0f));
        }

        // 3. Deploy 6 Roman Legionaries (East)
        for (int i = 0; i < RomanLegionariesCount; i++)
        {
            float y = 35f + (i * 5f);
            Coordinator.DispatchCommand(new SpawnUnitCommand(
                FactionId.Player2,
                SubmittedTick: 0,
                UnitType: "roman_legionary",
                Position: new Vector2D(75f, y),
                MaxHealth: 140f,
                AttackDamage: 16f,
                AttackRange: 1.5f,
                MovementSpeed: 3.2f,
                AttackCooldownTicks: 20,
                KillXpValue: 70,
                Armor: 5.0f,
                AttackType: "melee",
                AggroRange: 15.0f));
        }

        // 4. Deploy 4 Roman Veles behind legionaries
        for (int i = 0; i < RomanVelesCount; i++)
        {
            float y = 38f + (i * 7f);
            Coordinator.DispatchCommand(new SpawnUnitCommand(
                FactionId.Player2,
                SubmittedTick: 0,
                UnitType: "roman_veles",
                Position: new Vector2D(82f, y),
                MaxHealth: 85f,
                AttackDamage: 12f,
                AttackRange: 7.0f,
                MovementSpeed: 4.0f,
                AttackCooldownTicks: 16,
                KillXpValue: 45,
                Armor: 2.0f,
                AttackType: "ranged",
                AggroRange: 16.0f));
        }

        // Process spawn commands on tick 1
        Coordinator.Simulation.Tick();
    }

    /// <summary>
    /// Orders both armies to advance towards center for engagement.
    /// </summary>
    public void OrderArmiesToEngage()
    {
        var activeUnits = Coordinator.Simulation.State.ActiveUnits;
        var p1Units = new List<EntityId>();
        var p2Units = new List<EntityId>();

        for (int i = 0; i < activeUnits.Count; i++)
        {
            if (activeUnits[i].FactionId == FactionId.Player1)
                p1Units.Add(activeUnits[i].Id);
            else if (activeUnits[i].FactionId == FactionId.Player2)
                p2Units.Add(activeUnits[i].Id);
        }

        // Player 1 advances to center
        Coordinator.DispatchCommand(new MoveCommand(
            FactionId.Player1,
            Coordinator.CurrentTick,
            p1Units.ToArray(),
            new Vector2D(48f, 50f)));

        // Player 2 advances to center
        Coordinator.DispatchCommand(new MoveCommand(
            FactionId.Player2,
            Coordinator.CurrentTick,
            p2Units.ToArray(),
            new Vector2D(52f, 50f)));
    }
}
