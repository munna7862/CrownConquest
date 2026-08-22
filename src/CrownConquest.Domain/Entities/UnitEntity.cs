using CrownConquest.Domain.Common;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Entities;

public enum UnitState
{
    Idle,
    Moving,
    Attacking,
    Dead
}

/// <summary>
/// Authoritative domain entity representing a combat-capable unit.
/// Entirely decoupled from Godot nodes.
/// </summary>
public sealed class UnitEntity
{
    public EntityId Id { get; }
    public FactionId FactionId { get; }
    public string UnitType { get; }

    public Vector2D Position { get; set; }
    public Vector2D? MoveTarget { get; set; }
    public EntityId AttackTargetId { get; set; }
    public UnitState State { get; set; }

    public float BaseMaxHealth { get; }
    public float MaxHealth => BaseMaxHealth + ((Veterancy.Level - 1) * 15f);
    public float CurrentHealth { get; private set; }

    public float BaseAttackDamage { get; }
    public float AttackDamage => BaseAttackDamage + ((Veterancy.Level - 1) * 2.5f);

    public float AttackRange { get; }
    public float MovementSpeed { get; }
    public int AttackCooldownTicks { get; }
    public int CooldownRemaining { get; private set; }
    public int KillXpValue { get; }

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
        int killXpValue = 50)
    {
        Id = id;
        FactionId = factionId;
        UnitType = unitType;
        Position = position;
        BaseMaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        BaseAttackDamage = attackDamage;
        AttackRange = attackRange;
        MovementSpeed = movementSpeed;
        AttackCooldownTicks = attackCooldownTicks;
        CooldownRemaining = 0;
        KillXpValue = killXpValue;
        State = UnitState.Idle;
        Veterancy = new VeterancyState(id);
    }

    public void Move(Vector2D destination)
    {
        if (!IsAlive) return;
        MoveTarget = destination;
        AttackTargetId = EntityId.None;
        State = UnitState.Moving;
    }

    public void Attack(EntityId targetId)
    {
        if (!IsAlive) return;
        AttackTargetId = targetId;
        MoveTarget = null;
        State = UnitState.Attacking;
    }

    public void Stop()
    {
        MoveTarget = null;
        AttackTargetId = EntityId.None;
        if (IsAlive)
        {
            State = UnitState.Idle;
        }
    }

    public void TakeDamage(
        float amount,
        EntityId attackerId,
        FactionId attackerFaction,
        ulong tick,
        DomainEventBus eventBus,
        out bool killed)
    {
        killed = false;
        if (!IsAlive) return;

        CurrentHealth = MathF.Max(0f, CurrentHealth - amount);

        eventBus.Publish(new DamageDealtEvent(
            tick,
            attackerId,
            Id,
            amount,
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
