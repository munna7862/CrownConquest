using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Entities;

/// <summary>
/// Authoritative domain state for defensive towers (Watchtower, Guard Tower, Ballista Tower)
/// including autonomous arrow/ballista firing and garrison management.
/// </summary>
public sealed class TowerDefenseState
{
    public float BaseAttackDamage { get; }
    public float AttackRange { get; }
    public int AttackCooldownTicks { get; }
    public int CooldownRemaining { get; private set; }
    public int MaxGarrisonCapacity { get; }

    private readonly List<EntityId> _garrisonedUnitIds;
    public IReadOnlyList<EntityId> GarrisonedUnitIds => _garrisonedUnitIds;
    public int GarrisonCount => _garrisonedUnitIds.Count;
    public bool IsGarrisonFull => _garrisonedUnitIds.Count >= MaxGarrisonCapacity;

    public float GarrisonDamageBonusPerUnit { get; }
    public float EffectiveDamage => BaseAttackDamage * (1.0f + (GarrisonCount * GarrisonDamageBonusPerUnit));
    public bool IsBallistaTower { get; }

    public TowerDefenseState(
        float baseAttackDamage = 12.0f,
        float attackRange = 8.0f,
        int attackCooldownTicks = 20,
        int maxGarrisonCapacity = 4,
        float garrisonDamageBonusPerUnit = 0.20f,
        bool isBallistaTower = false)
    {
        BaseAttackDamage = MathF.Max(1.0f, baseAttackDamage);
        AttackRange = MathF.Max(1.0f, attackRange);
        AttackCooldownTicks = Math.Max(1, attackCooldownTicks);
        CooldownRemaining = 0;
        MaxGarrisonCapacity = Math.Max(0, maxGarrisonCapacity);
        GarrisonDamageBonusPerUnit = MathF.Max(0f, garrisonDamageBonusPerUnit);
        IsBallistaTower = isBallistaTower;
        _garrisonedUnitIds = new List<EntityId>(MaxGarrisonCapacity);
    }

    public void DecrementCooldown()
    {
        if (CooldownRemaining > 0)
        {
            CooldownRemaining--;
        }
    }

    public void ResetCooldown()
    {
        CooldownRemaining = AttackCooldownTicks;
    }

    public bool CanGarrison(EntityId unitId)
    {
        return !IsGarrisonFull && !_garrisonedUnitIds.Contains(unitId);
    }

    public bool TryGarrison(EntityId unitId)
    {
        if (!CanGarrison(unitId)) return false;
        _garrisonedUnitIds.Add(unitId);
        return true;
    }

    public bool TryUngarrison(EntityId unitId)
    {
        return _garrisonedUnitIds.Remove(unitId);
    }

    public List<EntityId> UngarrisonAll()
    {
        var list = new List<EntityId>(_garrisonedUnitIds);
        _garrisonedUnitIds.Clear();
        return list;
    }
}
