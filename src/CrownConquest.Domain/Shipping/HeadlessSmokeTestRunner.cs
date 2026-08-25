using System;
using System.Diagnostics;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.Shipping;

public sealed record SmokeScenarioConfig(
    int TicksToSimulate = 600,
    int RandomSeed = 42,
    int UnitsPerFaction = 6,
    bool PerformSaveReloadCheck = true,
    int MidSimulationSaveTick = 200);

public sealed record SmokeTestResult(
    bool IsSuccess,
    int ExitCode,
    int TotalTicksExecuted,
    int InitialAliveUnits,
    int FinalAliveUnits,
    int TotalKillsAwarded,
    int TotalResourcesHarvested,
    bool SaveLoadParityConfirmed,
    ulong PreSaveChecksum,
    ulong PostReloadChecksum,
    double DurationMs,
    string SummaryDetails);

public static class HeadlessSmokeTestRunner
{
    public const int ExitCodeSuccess = 0;
    public const int ExitCodeInvariantFailure = 1;
    public const int ExitCodeStateCorrupt = 2;
    public const int ExitCodeExecutionError = 3;

    public static SmokeTestResult RunSmokeTest(SmokeScenarioConfig? config = null)
    {
        config ??= new SmokeScenarioConfig();

        var stopwatch = Stopwatch.StartNew();
        var simConfig = new SimulationConfig
        {
            InitialRandomSeed = config.RandomSeed,
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

        // Economy initial funds
        sim.State.GetOrCreateResourceBank(f1).Deposit(ResourceType.Food, 1000, 0);
        sim.State.GetOrCreateResourceBank(f2).Deposit(ResourceType.Food, 1000, 0);

        // Add resource nodes
        var foodNode = new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Food, new Vector2D(10f, 10f), 5000, 5000);
        var woodNode = new ResourceNodeEntity(sim.State.GenerateEntityId(), ResourceType.Wood, new Vector2D(30f, 10f), 5000, 5000);
        sim.State.AddResourceNode(foodNode);
        sim.State.AddResourceNode(woodNode);

        // Spawn Workers
        var w1 = new UnitEntity(
            sim.State.GenerateEntityId(),
            f1,
            "worker",
            new Vector2D(10f, 12f),
            maxHealth: 60f,
            attackDamage: 5f,
            attackRange: 1.0f,
            movementSpeed: 3.5f,
            attackCooldownTicks: 20,
            killXpValue: 10,
            baseArmor: 0f,
            archetype: UnitArchetype.Worker,
            workerState: new WorkerGatherState(10, 1.0f, 1.0f));
        sim.State.AddUnit(w1);
        sim.SpatialGrid.Insert(w1.Id, w1.Position);
        w1.AssignGather(foodNode.Id);

        var w2 = new UnitEntity(
            sim.State.GenerateEntityId(),
            f2,
            "worker",
            new Vector2D(30f, 12f),
            maxHealth: 60f,
            attackDamage: 5f,
            attackRange: 1.0f,
            movementSpeed: 3.5f,
            attackCooldownTicks: 20,
            killXpValue: 10,
            baseArmor: 0f,
            archetype: UnitArchetype.Worker,
            workerState: new WorkerGatherState(10, 1.0f, 1.0f));
        sim.State.AddUnit(w2);
        sim.SpatialGrid.Insert(w2.Id, w2.Position);
        w2.AssignGather(woodNode.Id);

        // Spawn Combat Units
        for (int i = 0; i < config.UnitsPerFaction; i++)
        {
            var u1 = new UnitEntity(
                sim.State.GenerateEntityId(),
                f1,
                "swordsman",
                new Vector2D(15f + (i * 2f), 25f),
                maxHealth: 120f,
                attackDamage: 18f,
                attackRange: 1.5f,
                movementSpeed: 3.2f,
                attackCooldownTicks: 15,
                killXpValue: 25,
                baseArmor: 3f,
                archetype: UnitArchetype.Infantry);
            sim.State.AddUnit(u1);
            sim.SpatialGrid.Insert(u1.Id, u1.Position);

            var u2 = new UnitEntity(
                sim.State.GenerateEntityId(),
                f2,
                "spearman",
                new Vector2D(25f - (i * 2f), 25f),
                maxHealth: 110f,
                attackDamage: 16f,
                attackRange: 2.0f,
                movementSpeed: 3.0f,
                attackCooldownTicks: 16,
                killXpValue: 25,
                baseArmor: 2f,
                archetype: UnitArchetype.Spearman);
            sim.State.AddUnit(u2);
            sim.SpatialGrid.Insert(u2.Id, u2.Position);
        }

        int initialUnits = sim.State.ActiveUnits.Count;
        bool saveLoadParityConfirmed = true;
        ulong preSaveChecksum = 0;
        ulong postReloadChecksum = 0;

        int ticksTarget = config.TicksToSimulate;
        for (int t = 0; t < ticksTarget; t++)
        {
            sim.Tick();

            if (config.PerformSaveReloadCheck && t == config.MidSimulationSaveTick)
            {
                preSaveChecksum = sim.State.ComputeStateChecksum();
                string json = SimulationStateSerializer.SerializeToJson(sim.State, config.RandomSeed);
                var reloadResult = SimulationStateSerializer.DeserializeFromJson(json);

                if (!reloadResult.IsSuccess || reloadResult.Value == null)
                {
                    stopwatch.Stop();
                    return new SmokeTestResult(
                        false,
                        ExitCodeStateCorrupt,
                        (int)sim.State.CurrentTick,
                        initialUnits,
                        sim.State.ActiveUnits.Count,
                        totalKills,
                        totalGathered,
                        false,
                        preSaveChecksum,
                        0,
                        stopwatch.Elapsed.TotalMilliseconds,
                        $"Mid-simulation save deserialization failed: {reloadResult.Error.Message}");
                }

                postReloadChecksum = reloadResult.Value.ComputeStateChecksum();
                if (preSaveChecksum != postReloadChecksum)
                {
                    saveLoadParityConfirmed = false;
                }
            }
        }

        stopwatch.Stop();

        int finalAlive = 0;
        for (int i = 0; i < sim.State.ActiveUnits.Count; i++)
        {
            if (sim.State.ActiveUnits[i].IsAlive) finalAlive++;
        }

        bool isSuccess = saveLoadParityConfirmed && sim.State.CurrentTick == (ulong)ticksTarget;
        int exitCode = isSuccess ? ExitCodeSuccess : ExitCodeInvariantFailure;
        string details = isSuccess
            ? $"Headless smoke test completed cleanly in {stopwatch.Elapsed.TotalMilliseconds:F1}ms across {ticksTarget} ticks. Kills={totalKills}, Resources={totalGathered}."
            : $"Smoke test failed: Parity={saveLoadParityConfirmed}, FinalTicks={sim.State.CurrentTick}/{ticksTarget}";

        return new SmokeTestResult(
            isSuccess,
            exitCode,
            (int)sim.State.CurrentTick,
            initialUnits,
            finalAlive,
            totalKills,
            totalGathered,
            saveLoadParityConfirmed,
            preSaveChecksum,
            postReloadChecksum,
            stopwatch.Elapsed.TotalMilliseconds,
            details);
    }
}
