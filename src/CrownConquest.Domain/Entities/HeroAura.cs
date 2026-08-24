using System;

namespace CrownConquest.Domain.Entities;

/// <summary>
/// Passive leadership aura bestowed by a Hero onto attached army squad members within its radius.
/// </summary>
public sealed class HeroAura
{
    public string AuraName { get; }
    public float Radius { get; }
    public float DamageMultiplierBonus { get; }
    public float ArmorBonus { get; }
    public float MovementSpeedMultiplierBonus { get; }

    public HeroAura(
        string auraName,
        float radius = 12.0f,
        float damageMultiplierBonus = 0.15f,
        float armorBonus = 2.0f,
        float movementSpeedMultiplierBonus = 0.10f)
    {
        AuraName = auraName;
        Radius = radius;
        DamageMultiplierBonus = damageMultiplierBonus;
        ArmorBonus = armorBonus;
        MovementSpeedMultiplierBonus = movementSpeedMultiplierBonus;
    }
}
