namespace CrownConquest.Domain.World;

/// <summary>
/// Immutable definition of a campaign faction, including culture, home province, and trade properties.
/// </summary>
public sealed record FactionDefinition(
    string Id,
    string Name,
    string Culture,
    ProvinceId HomeProvinceId,
    int InitialReputation,
    string ColorHex,
    double TradeModifier,
    string Description
);
