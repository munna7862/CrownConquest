using System;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Entities;

public enum UnitState
{
    Idle,
    Moving,
    Attacking,
    Gathering,
    Returning,
    Constructing,
    Repairing,
    Dead
}

/// <summary>
/// Authoritative domain entity representing a combat-capable or worker unit.
/// Entirely decoupled from Godot nodes.
/// </summary>
public sealed class UnitEntity
{
    public EntityId Id { get; }
    public FactionId FactionId { get; }
    public string UnitType { get; }
    public UnitArchetype Archetype { get; }

    public Vector2D Position { get; set; }
    public Vector2D? MoveTarget { get; set; }
    public EntityId AttackTargetId { get; set; }
    public UnitState State { get; set; }

    public WorkerGatherState? WorkerState { get; set; }
    public bool IsWorker => WorkerState != null;
    public bool IsIdleWorker => IsWorker && State == UnitState.Idle && IsAlive;

    public float BaseMaxHealth { get; }
    public float HealthPerLevelBonus { get; }
    public float MaxHealth => BaseMaxHealth + ((Veterancy.Level - 1) * HealthPerLevelBonus);
    public float CurrentHealth { get; private set; }

    public float BaseAttackDamage { get; }
    public float DamagePerLevelBonus { get; }
    public float AttackDamage => BaseAttackDamage + ((Veterancy.Level - 1) * DamagePerLevelBonus);

    public float BaseArmor { get; }
    public float ArmorPerLevelBonus { get; }
    public float Armor => BaseArmor + ((Veterancy.Level - 1) * ArmorPerLevelBonus);

    public float AttackRange { get; }
    public string AttackType { get; } // "melee" or "ranged"
    public float MovementSpeed { get; }
    public int AttackCooldownTicks { get; }
    public int CooldownRemaining { get; private set; }
    public int KillXpValue { get; }
    public float AggroRange { get; set; }

    public bool IsAlive => CurrentHealth > 0f && State != UnitState.Dead;

    public VeterancyState Veterancy { get; }

    public UnitEntity(
        EntityId id,
        FactionId factionId,
        string unitType,
        Vector2D position,
        float maxHealth = 100f,
        float attackDamage = 15f,
        float attackRange = 1.5f,
        float movementSpeed = 3.5f,
        int attackCooldownTicks = 20,
        int killXpValue = 50,
        float baseArmor = 0f,
        string attackType = "melee",
        float aggroRange = 10.0f,
        float healthPerLevelBonus = 15.0f,
        float damagePerLevelBonus = 2.5f,
        float armorPerLevelBonus = 1.0f,
        int[]? xpThresholds = null,
        WorkerGatherState? workerState = null,
        UnitArchetype? archetype = null)
    {
        Id = id;
        FactionId = factionId;
        UnitType = unitType;
        Archetype = archetype ?? UnitArchetypeExtensions.FromUnitType(unitType);
        Position = position;
        BaseMaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        HealthPerLevelBonus = healthPerLevelBonus;
        BaseAttackDamage = attackDamage;
        DamagePerLevelBonus = damagePerLevelBonus;
        BaseArmor = baseArmor;
        ArmorPerLevelBonus = armorPerLevelBonus;
        AttackRange = attackRange;
        AttackType = attackType;
        MovementSpeed = movementSpeed;
        AttackCooldownTicks = attackCooldownTicks;
        CooldownRemaining = 0;
        KillXpValue = killXpValue;
        AggroRange = aggroRange;
        State = UnitState.Idle;
        Veterancy = new VeterancyState(id, customThresholds: xpThresholds);
        WorkerState = workerState;
    }

    public void Move(Vector2D destination)
    {
        if (!IsAlive) return;
        MoveTarget = destination;
        AttackTargetId = EntityId.None;
        if (WorkerState != null)
        {
            WorkerState.ResetTask();
        }
        State = UnitState.Moving;
    }

    public void Attack(EntityId targetId)
    {
        if (!IsAlive) return;
        AttackTargetId = targetId;
        MoveTarget = null;
        if (WorkerState != null)
        {
            WorkerState.ResetTask();
        }
        State = UnitState.Attacking;
    }

    public void AssignGather(EntityId resourceNodeId)
    {
        if (!IsAlive || WorkerState == null) return;
        WorkerState.TargetResourceNodeId = resourceNodeId;
        WorkerState.TaskState = WorkerTaskState.MovingToResource;
        AttackTargetId = EntityId.None;
        State = UnitState.Gathering;
    }

    public void AssignConstruct(EntityId buildingId)
    {
        if (!IsAlive || WorkerState == null) return;
        WorkerState.TargetBuildingId = buildingId;
        WorkerState.TaskState = WorkerTaskState.MovingToConstruct;
        AttackTargetId = EntityId.None;
        State = UnitState.Constructing;
    }

    public void AssignRepair(EntityId buildingId)
    {
        if (!IsAlive || WorkerState == null) return;
        WorkerState.TargetBuildingId = buildingId;
        WorkerState.TaskState = WorkerTaskState.MovingToRepair;
        AttackTargetId = EntityId.None;
        State = UnitState.Repairing;
    }

    public void Stop()
    {
        MoveTarget = null;
        AttackTargetId = EntityId.None;
        if (WorkerState != null)
        {
            WorkerState.ResetTask();
        }
        if (IsAlive)
        {
            State = UnitState.Idle;
        }
    }

    public void TakeDamage(
        float rawAmount,
        EntityId attackerId,
        FactionId attackerFaction,
        ulong tick,
        DomainEventBus eventBus,
        out bool killed)
    {
        float effectiveDamage = CombatFormulas.CalculateEffectiveDamage(rawAmount, Armor);
        ApplyCalculatedDamage(effectiveDamage, attackerId, attackerFaction, tick, eventBus, out killed);
    }

    public void TakeCombatDamage(
        float calculatedEffectiveDamage,
        EntityId attackerId,
        FactionId attackerFaction,
        ulong tick,
        DomainEventBus eventBus,
        out bool killed)
    {
        ApplyCalculatedDamage(calculatedEffectiveDamage, attackerId, attackerFaction, tick, eventBus, out killed);
    }

    private void ApplyCalculatedDamage(
        float effectiveDamage,
        EntityId attackerId,
        FactionId attackerFaction,
        ulong tick,
        DomainEventBus eventBus,
        out bool killed)
    {
        killed = false;
        if (!IsAlive) return;

        CurrentHealth = MathF.Max(0f, CurrentHealth - effectiveDamage);

        eventBus.Publish(new DamageDealtEvent(
            tick,
            attackerId,
            Id,
            effectiveDamage,
            CurrentHealth,
            IsCritical: false));

        if (CurrentHealth <= 0f)
        {
            State = UnitState.Dead;
            MoveTarget = null;
            AttackTargetId = EntityId.None;
            killed = true;

            eventBus.Publish(new UnitKilledEvent(
                tick,
                Id,
                attackerId,
                FactionId,
                attackerFaction,
                Position));
        }
    }

    public void ApplyLevelUpBonus(float healthBonus)
    {
        if (IsAlive && healthBonus > 0f)
        {
            CurrentHealth = MathF.Min(MaxHealth, CurrentHealth + healthBonus);
        }
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
}
