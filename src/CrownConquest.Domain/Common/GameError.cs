namespace CrownConquest.Domain.Common;

/// <summary>
/// Strongly-typed failure reasons for gameplay rules and command validations.
/// Avoids throwing exceptions during regular game loops.
/// </summary>
public readonly record struct GameError(string Code, string Message)
{
    public static readonly GameError None = new(string.Empty, string.Empty);
    public static readonly GameError EntityNotFound = new("ENTITY_NOT_FOUND", "Specified entity does not exist in simulation.");
    public static readonly GameError InvalidTarget = new("INVALID_TARGET", "Command target is invalid or already deceased.");
    public static readonly GameError OutOfRange = new("OUT_OF_RANGE", "Target is out of operational range.");
    public static readonly GameError InsufficientResources = new("INSUFFICIENT_RESOURCES", "Player lacks required resources.");
    public static readonly GameError PopulationCapReached = new("POP_CAP_REACHED", "Population limit has been reached.");
    public static readonly GameError CooldownActive = new("COOLDOWN_ACTIVE", "Ability or action is currently on cooldown.");

    public bool HasError => !string.IsNullOrEmpty(Code);

    public override string ToString() => HasError ? $"[{Code}] {Message}" : "None";
}
