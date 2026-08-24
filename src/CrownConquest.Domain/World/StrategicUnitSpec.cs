using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.World;

/// <summary>
/// Persistent state snapshot of an individual unit within a strategic army.
/// Preserves XP, level, kills, veterancy rank, and current health across battles.
/// </summary>
public sealed class StrategicUnitSpec
{
    public string UnitType { get; set; } = "Infantry";
    public UnitArchetype Archetype { get; set; } = UnitArchetype.Infantry;
    public float CurrentHealth { get; set; } = 100f;
    public float BaseMaxHealth { get; set; } = 100f;
    public float HealthPerLevelBonus { get; set; } = 15f;
    public float BaseAttackDamage { get; set; } = 12f;
    public float DamagePerLevelBonus { get; set; } = 2f;
    public float AttackRange { get; set; } = 20f;
    public float AttackCooldown { get; set; } = 1.0f;
    public float Armor { get; set; } = 2f;
    public float MoveSpeed { get; set; } = 50f;

    public int Level { get; set; } = 1;
    public int CurrentXp { get; set; } = 0;
    public int TotalKills { get; set; } = 0;
    public VeterancyRank Rank { get; set; } = VeterancyRank.Recruit;

    public float MaxHealth => BaseMaxHealth + ((Level - 1) * HealthPerLevelBonus);
    public float AttackDamage => BaseAttackDamage + ((Level - 1) * DamagePerLevelBonus);
    public bool IsAlive => CurrentHealth > 0.001f;

    public float CombatPower => (MaxHealth * 0.5f) + (AttackDamage * 10f) + (Armor * 5f) + (Level * 15f);

    public StrategicUnitSpec Clone()
    {
        return new StrategicUnitSpec
        {
            UnitType = UnitType,
            Archetype = Archetype,
            CurrentHealth = CurrentHealth,
            BaseMaxHealth = BaseMaxHealth,
            HealthPerLevelBonus = HealthPerLevelBonus,
            BaseAttackDamage = BaseAttackDamage,
            DamagePerLevelBonus = DamagePerLevelBonus,
            AttackRange = AttackRange,
            AttackCooldown = AttackCooldown,
            Armor = Armor,
            MoveSpeed = MoveSpeed,
            Level = Level,
            CurrentXp = CurrentXp,
            TotalKills = TotalKills,
            Rank = Rank
        };
    }
}
