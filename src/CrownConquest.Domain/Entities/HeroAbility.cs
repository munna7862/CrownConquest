using System;

namespace CrownConquest.Domain.Entities;

public enum AbilityTargetType
{
    Self,
    SingleTargetEnemy,
    SingleTargetAlly,
    PointAreaEnemy,
    PointAreaAlly,
    PointAreaAll
}

public enum AbilityEffectType
{
    Damage,
    Heal,
    Buff,
    Stun
}

/// <summary>
/// Static definition of a Hero ability.
/// </summary>
public sealed class HeroAbilityDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public float ManaCost { get; }
    public int CooldownTicks { get; }
    public float CastRange { get; }
    public float Radius { get; }
    public AbilityTargetType TargetType { get; }
    public AbilityEffectType EffectType { get; }
    public float BasePower { get; }

    public HeroAbilityDefinition(
        string id,
        string displayName,
        string description,
        float manaCost,
        int cooldownTicks,
        float castRange,
        float radius,
        AbilityTargetType targetType,
        AbilityEffectType effectType,
        float basePower)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        ManaCost = manaCost;
        CooldownTicks = cooldownTicks;
        CastRange = castRange;
        Radius = radius;
        TargetType = targetType;
        EffectType = effectType;
        BasePower = basePower;
    }
}
