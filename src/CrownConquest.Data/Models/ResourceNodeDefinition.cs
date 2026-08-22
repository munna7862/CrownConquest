namespace CrownConquest.Data.Models;

public sealed class ResourceNodeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Wood";
    public int MaxAmount { get; set; } = 300;
    public float HarvestRadius { get; set; } = 1.8f;
}
