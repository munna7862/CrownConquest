namespace CrownConquest.Domain.Simulation;

/// <summary>
/// Deterministic linear congruential pseudo-random number generator (LCG).
/// Guarantees bit-exact identical sequence across platforms, runtimes, and architectures.
/// </summary>
public sealed class SimulationRandom
{
    private uint _state;

    public uint Seed { get; }

    public SimulationRandom(int seed)
    {
        Seed = (uint)seed;
        _state = Seed != 0 ? Seed : 1;
    }

    /// <summary>
    /// Next 32-bit unsigned integer.
    /// </summary>
    public uint NextUInt()
    {
        _state = (_state * 1664525u) + 1013904223u;
        return _state;
    }

    /// <summary>
    /// Next float in range [0.0f, 1.0f).
    /// </summary>
    public float NextFloat()
    {
        return (NextUInt() & 0x00FFFFFF) / (float)0x01000000;
    }

    /// <summary>
    /// Next integer in range [minInclusive, maxExclusive).
    /// </summary>
    public int NextRange(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
        {
            return minInclusive;
        }

        uint range = (uint)(maxExclusive - minInclusive);
        return (int)(minInclusive + (NextUInt() % range));
    }

    /// <summary>
    /// Next float in range [minInclusive, maxInclusive].
    /// </summary>
    public float NextFloatRange(float minInclusive, float maxInclusive)
    {
        return minInclusive + (NextFloat() * (maxInclusive - minInclusive));
    }
}
