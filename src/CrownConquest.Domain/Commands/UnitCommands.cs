using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Commands;

/// <summary>
/// Command to spawn a new unit at a given position.
/// </summary>
public readonly record struct SpawnUnitCommand(
    FactionId FactionId,
    ulong SubmittedTick,
    string UnitType,
    Vector2D Position,
    float MaxHealth = 100f,
    float AttackDamage = 15f,
    float AttackRange = 1.5f,
    float MovementSpeed = 3.5f,
    int AttackCooldownTicks = 20,
    int KillXpValue = 50,
    float Armor = 0f,
    string AttackType = "melee",
    float AggroRange = 10.0f,
    float HealthPerLevelBonus = 15.0f,
    float DamagePerLevelBonus = 2.5f,
    int[]? XpThresholds = null) : ICommand;

/// <summary>
/// Command to order one or more units to move to a destination.
/// </summary>
public readonly record struct MoveCommand(
    FactionId FactionId,
    ulong SubmittedTick,
    EntityId[] UnitIds,
    Vector2D Destination) : ICommand;

/// <summary>
/// Command to move units in formation with automatic spatial slot offsets.
/// </summary>
public readonly record struct FormationMoveCommand(
    FactionId FactionId,
    ulong SubmittedTick,
    EntityId[] UnitIds,
    Vector2D DestinationCentroid,
    float Spacing = 2.0f) : ICommand;

/// <summary>
/// Command to order units to attack a specific enemy target.
/// </summary>
public readonly record struct AttackCommand(
    FactionId FactionId,
    ulong SubmittedTick,
    EntityId[] UnitIds,
    EntityId TargetEntityId) : ICommand;

/// <summary>
/// Command to halt units in place.
/// </summary>
public readonly record struct StopCommand(
    FactionId FactionId,
    ulong SubmittedTick,
    EntityId[] UnitIds) : ICommand;

/// <summary>
/// Command to select one or more units for a faction.
/// </summary>
public readonly record struct SelectUnitsCommand(
    FactionId FactionId,
    ulong SubmittedTick,
    EntityId[] UnitIds,
    bool ClearPrevious = true) : ICommand;
