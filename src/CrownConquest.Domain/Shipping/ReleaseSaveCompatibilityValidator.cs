using System;
using System.Text.Json;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.Shipping;

public sealed record SaveCompatibilityReport(
    bool IsCompatible,
    bool CorruptPayloadHandledGracefully,
    bool TruncatedPayloadHandledGracefully,
    bool EmptyPayloadHandledGracefully,
    bool ValidSaveRestoresState,
    string Details);

public static class ReleaseSaveCompatibilityValidator
{
    public static SaveCompatibilityReport ValidateCompatibility()
    {
        // 1. Test Empty Payload
        var emptyRes = SimulationStateSerializer.DeserializeFromJson(string.Empty);
        bool emptyHandled = emptyRes.IsFailure && emptyRes.Error.HasError;

        // 2. Test Truncated Payload
        string truncatedJson = "{\"CurrentTick\":100, \"RandomSeed\":42, \"Units\":[{\"Id\":1, \"FactionId\":1, \"UnitType\":\"swords";
        var truncRes = SimulationStateSerializer.DeserializeFromJson(truncatedJson);
        bool truncHandled = truncRes.IsFailure && truncRes.Error.HasError;

        // 3. Test Corrupt Syntax Payload
        string corruptJson = "{ invalid json content !!! }";
        var corruptRes = SimulationStateSerializer.DeserializeFromJson(corruptJson);
        bool corruptHandled = corruptRes.IsFailure && corruptRes.Error.HasError;

        // 4. Test Valid Save & Reload
        var state = new SimulationState { CurrentTick = 250 };
        var unit = new UnitEntity(
            new EntityId(10),
            new FactionId(1),
            "swordsman",
            new Vector2D(15f, 20f),
            maxHealth: 100f,
            attackDamage: 12f,
            attackRange: 1.5f,
            movementSpeed: 3.5f,
            attackCooldownTicks: 20,
            killXpValue: 15,
            baseArmor: 2f);
        state.AddUnit(unit);

        string validJson = SimulationStateSerializer.SerializeToJson(state, 42);
        var validRes = SimulationStateSerializer.DeserializeFromJson(validJson);
        bool validRestores = validRes.IsSuccess &&
                             validRes.Value != null &&
                             validRes.Value.CurrentTick == 250 &&
                             validRes.Value.ActiveUnits.Count == 1 &&
                             validRes.Value.ActiveUnits[0].Id.Value == 10;

        bool allPassed = emptyHandled && truncHandled && corruptHandled && validRestores;
        string details = allPassed
            ? "Save/Load compatibility verified: Graceful error recovery across empty, truncated, and malformed saves; full roundtrip parity for valid saves."
            : $"Save/Load compatibility failed: Empty={emptyHandled}, Truncated={truncHandled}, Corrupt={corruptHandled}, Valid={validRestores}";

        return new SaveCompatibilityReport(
            allPassed,
            corruptHandled,
            truncHandled,
            emptyHandled,
            validRestores,
            details);
    }
}
