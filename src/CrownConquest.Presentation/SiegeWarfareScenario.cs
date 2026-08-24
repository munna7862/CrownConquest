using System;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Headless E2E scenario demonstrating a complete fortress assault match in Crown & Conquest.
/// </summary>
public sealed class SiegeWarfareScenario
{
    public SimulationEngine Engine { get; }
    public SiegeWarfarePresenter Presenter { get; }

    public EntityId DefenderTownCenterId { get; private set; }
    public EntityId DefenderWallId { get; private set; }
    public EntityId DefenderGateId { get; private set; }
    public EntityId DefenderTowerId { get; private set; }

    public EntityId AttackerRamId { get; private set; }
    public EntityId AttackerCatapultId { get; private set; }
    public EntityId AttackerBallistaId { get; private set; }
    public EntityId AttackerInfantryId { get; private set; }

    public SiegeWarfareScenario(int seed = 42)
    {
        var config = new SimulationConfig { InitialRandomSeed = seed, TicksPerSecond = 20 };
        Engine = new SimulationEngine(config);
        Presenter = new SiegeWarfarePresenter();
        Presenter.Bind(Engine.EventBus);
    }

    public void SetupFortressMatch()
    {
        var factionDef = new FactionId(1); // Celtic Defenders
        var factionAtk = new FactionId(2); // Roman Attackers

        // 1. Setup Defender Base & Fortifications
        DefenderTownCenterId = Engine.State.GenerateEntityId();
        var tc = new BuildingEntity(
            DefenderTownCenterId,
            factionDef,
            "town_center",
            new Vector2D(10f, 0f),
            new Vector2D(4f, 4f),
            maxHealth: 1200f,
            startsConstructed: true);
        Engine.State.AddBuilding(tc);

        DefenderTowerId = Engine.State.GenerateEntityId();
        var tower = new BuildingEntity(
            DefenderTowerId,
            factionDef,
            "guard_tower",
            new Vector2D(4f, 2f),
            new Vector2D(2f, 2f),
            maxHealth: 800f,
            startsConstructed: true);
        Engine.State.AddBuilding(tower);

        // Defender Archer to garrison into tower
        var archerId = Engine.State.GenerateEntityId();
        var archer = new UnitEntity(
            archerId,
            factionDef,
            "celtic_archer",
            new Vector2D(4f, 2f),
            maxHealth: 80f,
            attackDamage: 14f,
            attackRange: 8.0f,
            archetype: UnitArchetype.Archer);
        Engine.State.AddUnit(archer);
        tower.TowerDefense?.TryGarrison(archerId);

        DefenderWallId = Engine.State.GenerateEntityId();
        var wall = new BuildingEntity(
            DefenderWallId,
            factionDef,
            "wooden_wall",
            new Vector2D(0f, 0f),
            new Vector2D(1f, 1f),
            maxHealth: 200f, // Scaled for scenario test
            startsConstructed: true);
        Engine.State.AddBuilding(wall);

        DefenderGateId = Engine.State.GenerateEntityId();
        var gate = new BuildingEntity(
            DefenderGateId,
            factionDef,
            "wooden_gate",
            new Vector2D(0f, 4f),
            new Vector2D(2f, 1f),
            maxHealth: 300f,
            startsConstructed: true);
        Engine.State.AddBuilding(gate);

        // 2. Setup Attacker Siege Force
        AttackerRamId = Engine.State.GenerateEntityId();
        var ram = new UnitEntity(
            AttackerRamId,
            factionAtk,
            "roman_battering_ram",
            new Vector2D(-12f, 0f),
            maxHealth: 280f,
            attackDamage: 40f,
            attackRange: 1.8f,
            movementSpeed: 2.0f,
            attackCooldownTicks: 15,
            archetype: UnitArchetype.Siege);
        Engine.State.AddUnit(ram);

        AttackerCatapultId = Engine.State.GenerateEntityId();
        var catapult = new UnitEntity(
            AttackerCatapultId,
            factionAtk,
            "roman_catapult",
            new Vector2D(-14f, 2f),
            maxHealth: 160f,
            attackDamage: 40f,
            attackRange: 12.0f,
            movementSpeed: 2.0f,
            attackCooldownTicks: 20,
            archetype: UnitArchetype.Siege);
        Engine.State.AddUnit(catapult);

        AttackerBallistaId = Engine.State.GenerateEntityId();
        var ballista = new UnitEntity(
            AttackerBallistaId,
            factionAtk,
            "roman_ballista",
            new Vector2D(-14f, -2f),
            maxHealth: 140f,
            attackDamage: 45f,
            attackRange: 10.0f,
            movementSpeed: 2.5f,
            attackCooldownTicks: 18,
            archetype: UnitArchetype.Siege);
        Engine.State.AddUnit(ballista);

        AttackerInfantryId = Engine.State.GenerateEntityId();
        var legionary = new UnitEntity(
            AttackerInfantryId,
            factionAtk,
            "roman_legionary",
            new Vector2D(-15f, 0f),
            maxHealth: 140f,
            attackDamage: 16f,
            movementSpeed: 3.2f,
            archetype: UnitArchetype.Infantry);
        Engine.State.AddUnit(legionary);
    }

    public void RunAssault(int simulationTicks = 200)
    {
        var factionAtk = new FactionId(2);

        // Order Catapult to attack the wall
        Engine.CommandQueue.Enqueue(new AttackBuildingCommand(factionAtk, new[] { AttackerCatapultId }, DefenderWallId));

        // Order Ram to attack the wall
        Engine.CommandQueue.Enqueue(new AttackBuildingCommand(factionAtk, new[] { AttackerRamId }, DefenderWallId));

        // Order Ballista to attack tower
        Engine.CommandQueue.Enqueue(new AttackBuildingCommand(factionAtk, new[] { AttackerBallistaId }, DefenderTowerId));

        Engine.SimulateTicks(simulationTicks);
    }
}
