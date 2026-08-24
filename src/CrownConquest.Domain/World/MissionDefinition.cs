using CrownConquest.Domain.Economy;

namespace CrownConquest.Domain.World;

/// <summary>
/// Immutable definition of a campaign mission, containing objectives, constraints, and rewards.
/// </summary>
public sealed record MissionDefinition(
    string Id,
    string Name,
    string Description,
    MissionType Type,
    string IssuingFactionId,
    string? TargetFactionId,
    ProvinceId TargetProvinceId,
    ProvinceId? DestinationProvinceId,
    int DurationTicks,
    int TargetQuantity,
    ResourceCost RequiredResources,
    int GoldReward,
    int XpReward,
    int ReputationReward,
    bool IsPrimaryCampaign
);
