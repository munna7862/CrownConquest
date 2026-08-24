using System.Text.Json.Serialization;

namespace CrownConquest.Data.Models;

public sealed record FactionDefinitionModel(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("culture")] string Culture,
    [property: JsonPropertyName("homeProvinceId")] string HomeProvinceId,
    [property: JsonPropertyName("initialReputation")] int InitialReputation,
    [property: JsonPropertyName("colorHex")] string ColorHex,
    [property: JsonPropertyName("tradeModifier")] double TradeModifier,
    [property: JsonPropertyName("description")] string Description
);
