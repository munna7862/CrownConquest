using System;

namespace CrownConquest.Domain.Economy;

/// <summary>
/// Immutable multi-resource cost struct.
/// </summary>
public readonly record struct ResourceCost(
    int Food = 0,
    int Wood = 0,
    int Gold = 0,
    int Stone = 0,
    int Iron = 0)
{
    public static readonly ResourceCost Zero = new(0, 0, 0, 0, 0);

    public bool IsZero => Food == 0 && Wood == 0 && Gold == 0 && Stone == 0 && Iron == 0;

    public bool HasNegativeValues => Food < 0 || Wood < 0 || Gold < 0 || Stone < 0 || Iron < 0;

    public int GetAmount(ResourceType type) => type switch
    {
        ResourceType.Food => Food,
        ResourceType.Wood => Wood,
        ResourceType.Gold => Gold,
        ResourceType.Stone => Stone,
        ResourceType.Iron => Iron,
        _ => 0
    };

    public static ResourceCost operator +(ResourceCost a, ResourceCost b) =>
        new(a.Food + b.Food, a.Wood + b.Wood, a.Gold + b.Gold, a.Stone + b.Stone, a.Iron + b.Iron);

    public static ResourceCost operator -(ResourceCost a, ResourceCost b) =>
        new(a.Food - b.Food, a.Wood - b.Wood, a.Gold - b.Gold, a.Stone - b.Stone, a.Iron - b.Iron);
}
