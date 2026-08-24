using System;

namespace CrownConquest.Domain.Entities;

/// <summary>
/// Core RPG attributes for Heroes in Crown & Conquest.
/// Strength: Increases Health (+20 per point) and Melee Damage (+1.5 per point).
/// Agility: Increases Armor (+0.2 per point), Movement Speed (+0.05 per point), and Attack Speed / Cooldown Reduction.
/// Willpower: Increases Max Mana (+15 per point), Mana Regen (+0.05 per tick), and Ability Potency (+3% per point).
/// </summary>
public readonly record struct HeroAttributes(int Strength, int Agility, int Willpower)
{
    public static readonly HeroAttributes Zero = new(0, 0, 0);

    public float BonusHealth => Strength * 20.0f;
    public float BonusAttackDamage => Strength * 1.5f;
    public float BonusArmor => Agility * 0.2f;
    public float BonusMovementSpeed => Agility * 0.05f;
    public int CooldownReductionTicks => (int)(Agility * 0.5f);

    public float MaxMana => 50.0f + (Willpower * 15.0f);
    public float ManaRegenPerTick => 0.10f + (Willpower * 0.05f);
    public float AbilityPotencyMultiplier => 1.0f + (Willpower * 0.03f);

    public static HeroAttributes operator +(HeroAttributes a, HeroAttributes b)
    {
        return new HeroAttributes(
            a.Strength + b.Strength,
            a.Agility + b.Agility,
            a.Willpower + b.Willpower);
    }
}
