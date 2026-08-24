using System.Text.Json.Serialization;

namespace CrownConquest.Data.Models;

public sealed record MissionDefinitionModel(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("issuingFactionId")] string IssuingFactionId,
    [property: JsonPropertyName("targetFactionId")] string? TargetFactionId,
    [property: JsonPropertyName("targetProvinceId")] string TargetProvinceId,
    [property: JsonPropertyName("destinationProvinceId")] string? DestinationProvinceId,
    [property: JsonPropertyName("durationTicks")] int DurationTicks,
    [property: JsonPropertyName("targetQuantity")] int TargetQuantity,
    [property: JsonPropertyName("requiredFood")] int RequiredFood,
    [property: JsonPropertyName("requiredIron")] int RequiredIron,
    [property: JsonPropertyName("requiredGold")] int RequiredGold,
    [property: JsonPropertyName("goldReward")] int GoldReward,
    [property: JsonPropertyName("xpReward")] int XpReward,
    [property: JsonPropertyName("reputationReward")] int ReputationReward,
    [property: JsonPropertyName("isPrimaryCampaign")] bool IsPrimaryCampaign
);
