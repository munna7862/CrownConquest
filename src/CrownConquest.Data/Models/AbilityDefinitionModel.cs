using System;

namespace CrownConquest.Data.Models;

public sealed class AbilityDefinitionModel
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float ManaCost { get; set; } = 0f;
    public int CooldownTicks { get; set; } = 0;
    public float CastRange { get; set; } = 0f;
    public float Radius { get; set; } = 0f;
    public string TargetType { get; set; } = "SingleTargetEnemy";
    public string EffectType { get; set; } = "Damage";
    public float BasePower { get; set; } = 0f;
}
