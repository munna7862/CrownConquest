namespace CrownConquest.Domain.Simulation;

/// <summary>
/// Immutable configuration parameters for the authoritative simulation engine.
/// </summary>
public sealed record SimulationConfig
{
    public int TicksPerSecond { get; init; } = 20;
    public float DeltaTime => 1.0f / TicksPerSecond;
    public int InitialRandomSeed { get; init; } = 42;
    public int MaxUnitsPerFaction { get; init; } = 200;
    public float MapWidth { get; init; } = 1000f;
    public float MapHeight { get; init; } = 1000f;

    public static readonly SimulationConfig Default = new();
}
