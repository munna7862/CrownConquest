using CrownConquest.Application;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Root entry point for presentation environment and headless execution demo.
/// </summary>
public sealed class GameRoot
{
    public GameCoordinator Coordinator { get; }
    public PresentationEventBridge EventBridge { get; }

    public GameRoot(SimulationConfig? config = null)
    {
        var eventBus = new DomainEventBus();
        Coordinator = new GameCoordinator(config, eventBus);
        EventBridge = new PresentationEventBridge(eventBus);
    }

    public void SetupDemoMatch()
    {
        // Spawn 2 Celtic units
        Coordinator.DispatchCommand(new SpawnUnitCommand(
            FactionId.Player1,
            SubmittedTick: 0,
            UnitType: "celtic_swordsman",
            Position: new Vector2D(10f, 10f),
            MaxHealth: 120f,
            AttackDamage: 18f,
            AttackRange: 2.0f,
            MovementSpeed: 4.0f,
            AttackCooldownTicks: 10,
            KillXpValue: 50));

        // Spawn 1 Roman unit
        Coordinator.DispatchCommand(new SpawnUnitCommand(
            FactionId.Player2,
            SubmittedTick: 0,
            UnitType: "roman_legionary",
            Position: new Vector2D(14f, 10f),
            MaxHealth: 60f,
            AttackDamage: 10f,
            AttackRange: 2.0f,
            MovementSpeed: 3.5f,
            AttackCooldownTicks: 15,
            KillXpValue: 150));
    }
}
