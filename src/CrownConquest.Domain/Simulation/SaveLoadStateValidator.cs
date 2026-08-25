using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Simulation;

/// <summary>
/// Result of a mid-simulation save/load parity test.
/// </summary>
public sealed record SaveLoadValidationResult(
    bool IsMatch,
    ulong OriginalChecksum,
    ulong RestoredChecksum,
    ulong SnapshotTick,
    ulong FinalTick,
    int OriginalAliveUnits,
    int RestoredAliveUnits,
    string DivergenceDetails);

/// <summary>
/// Domain validator verifying bit-for-bit simulation parity across save/load state serialization roundtrips.
/// </summary>
public sealed class SaveLoadStateValidator
{
    public SaveLoadValidationResult ValidateMidSimulationParity(
        int initialTicks = 100,
        int continuationTicks = 100,
        int seed = 42)
    {
        var config = new SimulationConfig
        {
            InitialRandomSeed = seed,
            TicksPerSecond = 20
        };

        var eventBus1 = new DomainEventBus();
        var sim1 = new SimulationEngine(config, eventBus1);

        // Setup armies for faction 1 and faction 2
        var f1 = new FactionId(1);
        var f2 = new FactionId(2);

        sim1.State.GetOrCreateResourceBank(f1).Deposit(ResourceType.Food, 500, 0);
        sim1.State.GetOrCreateResourceBank(f2).Deposit(ResourceType.Food, 500, 0);

        for (int i = 0; i < 5; i++)
        {
            var u1 = new UnitEntity(
                sim1.State.GenerateEntityId(),
                f1,
                "swordsman",
                new Vector2D(10f + i, 20f),
                120f,
                15f,
                1.5f,
                3.5f,
                18,
                50,
                3f,
                "melee",
                12f,
                archetype: UnitArchetype.Infantry);
            sim1.State.AddUnit(u1);
            sim1.SpatialGrid.Insert(u1.Id, u1.Position);

            var u2 = new UnitEntity(
                sim1.State.GenerateEntityId(),
                f2,
                "spearman",
                new Vector2D(30f - i, 20f),
                110f,
                14f,
                2.0f,
                3.2f,
                20,
                50,
                2f,
                "melee",
                12f,
                archetype: UnitArchetype.Spearman);
            sim1.State.AddUnit(u2);
            sim1.SpatialGrid.Insert(u2.Id, u2.Position);
        }

        // Run initial ticks
        for (int t = 0; t < initialTicks; t++)
        {
            sim1.Tick();
        }

        ulong snapshotTick = sim1.State.CurrentTick;

        // Serialize state to JSON
        string jsonState = SimulationStateSerializer.SerializeToJson(sim1.State, seed);

        // Deserialize state
        var deserializeResult = SimulationStateSerializer.DeserializeFromJson(jsonState);
        if (!deserializeResult.IsSuccess || deserializeResult.Value == null)
        {
            return new SaveLoadValidationResult(
                false,
                sim1.State.ComputeStateChecksum(),
                0,
                snapshotTick,
                snapshotTick,
                sim1.State.ActiveUnits.Count,
                0,
                $"Deserialization failed: {deserializeResult.Error.Message}");
        }

        var restoredState = deserializeResult.Value;
        var eventBus2 = new DomainEventBus();
        var sim2 = new SimulationEngine(config, eventBus2, null, restoredState);

        // Continue running both engines in parallel
        for (int t = 0; t < continuationTicks; t++)
        {
            sim1.Tick();
            sim2.Tick();
        }

        ulong originalChecksum = sim1.State.ComputeStateChecksum();
        ulong restoredChecksum = sim2.State.ComputeStateChecksum();

        bool isMatch = originalChecksum == restoredChecksum;
        string details = isMatch
            ? "Full state bit-for-bit parity confirmed."
            : $"Checksum mismatch after {continuationTicks} post-reload ticks. Original={originalChecksum}, Restored={restoredChecksum}";

        int alive1 = 0;
        for (int i = 0; i < sim1.State.ActiveUnits.Count; i++) if (sim1.State.ActiveUnits[i].IsAlive) alive1++;

        int alive2 = 0;
        for (int i = 0; i < sim2.State.ActiveUnits.Count; i++) if (sim2.State.ActiveUnits[i].IsAlive) alive2++;

        return new SaveLoadValidationResult(
            isMatch,
            originalChecksum,
            restoredChecksum,
            snapshotTick,
            sim1.State.CurrentTick,
            alive1,
            alive2,
            details);
    }
}
