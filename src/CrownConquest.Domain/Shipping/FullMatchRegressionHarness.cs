using System;
using System.Diagnostics;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.Shipping;

public sealed record FullMatchRegressionResult(
    bool IsSuccess,
    int TotalTicksExecuted,
    ulong FinalChecksum,
    int TotalCombatKills,
    int TotalEconomyGathered,
    int FinalActiveUnits,
    int FinalActiveBuildings,
    double ExecutionTimeMs,
    string Summary);

public static class FullMatchRegressionHarness
{
    public static FullMatchRegressionResult RunFullMatch(int ticks = 1000, int seed = 42)
    {
        var sw = Stopwatch.StartNew();
        var simConfig = new SimulationConfig
        {
            InitialRandomSeed = seed,
            TicksPerSecond = 20
        };

        var eventBus = new DomainEventBus();
        int totalKills = 0;
        int totalGathered = 0;

        eventBus.Subscribe<UnitKilledEvent>((in UnitKilledEvent _) => totalKills++);
        eventBus.Subscribe<ResourceHarvestedEvent>((in ResourceHarvestedEvent e) => totalGathered += e.AmountHarvested);

        var sim = new SimulationEngine(simConfig, eventBus);

        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        // Economy
        var b1 = sim.State.GetOrCreateResourceBank(f1);
        b1.Deposit(ResourceType.Food, 2000, 0);
        b1.Deposit(ResourceType.Wood, 2000, 0);
        b1.Deposit(ResourceType.Gold, 1000, 0);

        var b2 = sim.State.GetOrCreateResourceBank(f2);
        b2.Deposit(ResourceType.Food, 2000, 0);
        b2.Deposit(ResourceType.Wood, 2000, 0);
        b2.Deposit(ResourceType.Gold, 1000, 0);

        // Tech & Eras
        var era1 = new EraState(f1, CivilizationEra.Archaic);
        var era2 = new EraState(f2, CivilizationEra.Archaic);
        sim.State.SetEraState(f1, era1);
        sim.State.SetEraState(f2, era2);

        var tech1 = sim.State.GetOrCreateTechManager(f1);
        tech1.RestoreUnlockedTech("iron_forging");

        // Town Centers / Buildings
        var tc1 = new BuildingEntity(sim.State.GenerateEntityId(), f1, "town_center", new Vector2D(10f, 10f), new Vector2D(4f, 4f), maxHealth: 1500f, startsConstructed: true);
        var tc2 = new BuildingEntity(sim.State.GenerateEntityId(), f2, "town_center", new Vector2D(90f, 90f), new Vector2D(4f, 4f), maxHealth: 1500f, startsConstructed: true);
        sim.State.AddBuilding(tc1);
        sim.State.AddBuilding(tc2);

        // Hero for Faction 1
        var heroAttrs = new HeroAttributes(18, 14, 12);
        var heroState = new HeroState(HeroClass.Warlord, "Lord Aldric", heroAttrs, 25);
        var heroUnit = new UnitEntity(
            sim.State.GenerateEntityId(),
            f1,
            "hero_warlord",
            new Vector2D(20f, 20f),
            maxHealth: 350f,
            attackDamage: 30f,
            attackRange: 2.0f,
            movementSpeed: 3.8f,
            attackCooldownTicks: 18,
            killXpValue: 100,
            baseArmor: 5f,
            archetype: UnitArchetype.Infantry,
            heroState: heroState);
        sim.State.AddUnit(heroUnit);
        sim.SpatialGrid.Insert(heroUnit.Id, heroUnit.Position);

        // Armies
        for (int i = 0; i < 8; i++)
        {
            var u1 = new UnitEntity(
                sim.State.GenerateEntityId(),
                f1,
                "swordsman",
                new Vector2D(25f + (i * 3f), 30f),
                maxHealth: 120f,
                attackDamage: 18f,
                attackRange: 1.5f,
                movementSpeed: 3.4f,
                attackCooldownTicks: 15,
                killXpValue: 20,
                baseArmor: 3f,
                archetype: UnitArchetype.Infantry,
                formation: FormationType.Line);
            sim.State.AddUnit(u1);
            sim.SpatialGrid.Insert(u1.Id, u1.Position);

            var u2 = new UnitEntity(
                sim.State.GenerateEntityId(),
                f2,
                "spearman",
                new Vector2D(75f - (i * 3f), 30f),
                maxHealth: 110f,
                attackDamage: 16f,
                attackRange: 2.0f,
                movementSpeed: 3.2f,
                attackCooldownTicks: 16,
                killXpValue: 20,
                baseArmor: 2f,
                archetype: UnitArchetype.Spearman,
                formation: FormationType.Wedge);
            sim.State.AddUnit(u2);
            sim.SpatialGrid.Insert(u2.Id, u2.Position);
        }

        // Run full simulation
        for (int t = 0; t < ticks; t++)
        {
            sim.Tick();
        }

        sw.Stop();

        ulong finalChecksum = sim.State.ComputeStateChecksum();
        bool isSuccess = sim.State.CurrentTick == (ulong)ticks && finalChecksum != 0;

        string summary = isSuccess
            ? $"Full Match Regression passed: {ticks} ticks executed in {sw.Elapsed.TotalMilliseconds:F1}ms. Checksum={finalChecksum}, Kills={totalKills}, RemainingUnits={sim.State.ActiveUnits.Count}."
            : $"Full Match Regression failed at tick {sim.State.CurrentTick}/{ticks}.";

        return new FullMatchRegressionResult(
            isSuccess,
            (int)sim.State.CurrentTick,
            finalChecksum,
            totalKills,
            totalGathered,
            sim.State.ActiveUnits.Count,
            sim.State.ActiveBuildings.Count,
            sw.Elapsed.TotalMilliseconds,
            summary);
    }
}
