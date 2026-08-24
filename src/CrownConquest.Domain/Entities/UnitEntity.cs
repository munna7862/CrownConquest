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
    Routed,
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

    public HeroState? HeroState { get; set; }
    public bool IsHero => HeroState != null || Archetype == UnitArchetype.Hero;

    public FormationType Formation { get; set; } = FormationType.Line;
    public FormationModifiers FormationModifiers => FormationModifiers.GetDefault(Formation);

    public MoraleState Morale { get; }
    public bool IsRouted => Morale.IsRouted || State == UnitState.Routed;

    public ChargeState Charge { get; }
    public TerrainType CurrentTerrain { get; set; } = TerrainType.Plains;
    public TerrainModifiers TerrainModifiers => TerrainModifiers.GetDefault(CurrentTerrain);

    public Vector2D HeadingDirection { get; set; } = new Vector2D(1f, 0f);

    public float BaseMaxHealth { get; }
    public float HealthPerLevelBonus { get; }
    public float MaxHealth => BaseMaxHealth + ((Veterancy.Level - 1) * HealthPerLevelBonus) + (HeroState?.TotalAttributes.BonusHealth ?? 0f);
    public float CurrentHealth { get; private set; }

    public float BaseAttackDamage { get; }
    public float DamagePerLevelBonus { get; }
    public float AttackDamage => BaseAttackDamage + ((Veterancy.Level - 1) * DamagePerLevelBonus) + (HeroState?.TotalAttributes.BonusAttackDamage ?? 0f);

    public float BaseArmor { get; }
    public float ArmorPerLevelBonus { get; }
    public float Armor => BaseArmor + ((Veterancy.Level - 1) * ArmorPerLevelBonus) + (HeroState?.TotalAttributes.BonusArmor ?? 0f) + FormationModifiers.ArmorBonus + CombatFormulas.GetMoraleArmorBonus(Morale.Level);

    public float AttackRange { get; }
    public string AttackType { get; } // "melee" or "ranged"
    public float BaseMovementSpeed { get; }

    public float EffectiveMovementSpeed
    {
        get
        {
            float baseSpeed = BaseMovementSpeed + (HeroState?.TotalAttributes.BonusMovementSpeed ?? 0f);
            float terrainMult = TerrainModifiers.MovementSpeedMultiplier;
            float formationMult = FormationModifiers.MovementSpeedMultiplier;
            float chargeMult = (Archetype == UnitArchetype.Cavalry && Charge.IsCharging) ? Charge.CurrentSpeedMultiplier * TerrainModifiers.ChargeSpeedMultiplier : 1.0f;
            float routedMult = IsRouted ? 1.15f : 1.0f; // Panic sprint

            return MathF.Max(0.5f, baseSpeed * terrainMult * formationMult * chargeMult * routedMult);
        }
    }

    public float MovementSpeed => EffectiveMovementSpeed;
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
        UnitArchetype? archetype = null,
        HeroState? heroState = null,
        float maxMorale = 100.0f,
        FormationType formation = FormationType.Line,
        int initialLevel = 1,
        int initialXp = 0,
        float? initialCurrentHealth = null)
    {
        Id = id;
        FactionId = factionId;
        UnitType = unitType;
        Archetype = archetype ?? (heroState != null ? UnitArchetype.Hero : UnitArchetypeExtensions.FromUnitType(unitType));
        Position = position;
        BaseMaxHealth = maxHealth;
        HealthPerLevelBonus = healthPerLevelBonus;
        BaseAttackDamage = attackDamage;
        DamagePerLevelBonus = damagePerLevelBonus;
        BaseArmor = baseArmor;
        ArmorPerLevelBonus = armorPerLevelBonus;
        AttackRange = attackRange;
        AttackType = attackType;
        BaseMovementSpeed = movementSpeed;
        AttackCooldownTicks = attackCooldownTicks;
        CooldownRemaining = 0;
        KillXpValue = killXpValue;
        AggroRange = aggroRange;
        State = UnitState.Idle;
        Veterancy = new VeterancyState(id, initialLevel: initialLevel, initialXp: initialXp, customThresholds: xpThresholds);
        WorkerState = workerState;
        HeroState = heroState;
        Morale = new MoraleState(maxMorale);
        Charge = new ChargeState();
        Formation = formation;
        CurrentHealth = initialCurrentHealth.HasValue && initialCurrentHealth.Value > 0f 
            ? MathF.Min(MaxHealth, initialCurrentHealth.Value) 
            : MaxHealth;
    }

    public void Move(Vector2D destination)
    {
        if (!IsAlive || IsRouted) return;
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
        if (!IsAlive || IsRouted) return;
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
        if (!IsAlive || IsRouted || WorkerState == null) return;
        WorkerState.TargetResourceNodeId = resourceNodeId;
        WorkerState.TaskState = WorkerTaskState.MovingToResource;
        AttackTargetId = EntityId.None;
        State = UnitState.Gathering;
    }

    public void AssignConstruct(EntityId buildingId)
    {
        if (!IsAlive || IsRouted || WorkerState == null) return;
        WorkerState.TargetBuildingId = buildingId;
        WorkerState.TaskState = WorkerTaskState.MovingToConstruct;
        AttackTargetId = EntityId.None;
        State = UnitState.Constructing;
    }

    public void AssignRepair(EntityId buildingId)
    {
        if (!IsAlive || IsRouted || WorkerState == null) return;
        WorkerState.TargetBuildingId = buildingId;
        WorkerState.TaskState = WorkerTaskState.MovingToRepair;
        AttackTargetId = EntityId.None;
        State = UnitState.Repairing;
    }

    public void Stop()
    {
        if (IsRouted) return;
        MoveTarget = null;
        AttackTargetId = EntityId.None;
        Charge.Reset();
        if (WorkerState != null)
        {
            WorkerState.ResetTask();
        }
        if (IsAlive)
        {
            State = UnitState.Idle;
        }
    }

    public void SetFormation(FormationType newFormation)
    {
        if (!IsAlive || IsRouted) return;
        Formation = newFormation;
    }

    public void Route(Vector2D safeDestination)
    {
        if (!IsAlive) return;
        State = UnitState.Routed;
        MoveTarget = safeDestination;
        AttackTargetId = EntityId.None;
        Charge.Reset();
        if (WorkerState != null)
        {
            WorkerState.ResetTask();
        }
    }

    public void Rally(float recoveryAmount = 25.0f)
    {
        if (!IsAlive) return;
        Morale.Rally(recoveryAmount);
        if (Morale.CurrentMorale >= 25.0f && State == UnitState.Routed)
        {
            State = UnitState.Idle;
            MoveTarget = null;
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

    public void TakeRecoilDamage(
        float recoilDamage,
        EntityId bracedTargetId,
        ulong tick,
        DomainEventBus eventBus,
        out bool killed)
    {
        killed = false;
        if (!IsAlive || recoilDamage <= 0f) return;

        CurrentHealth = MathF.Max(0f, CurrentHealth - recoilDamage);
        Charge.Discharge();

        eventBus.Publish(new DamageDealtEvent(
            tick,
            bracedTargetId,
            Id,
            recoilDamage,
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
                bracedTargetId,
                FactionId,
                FactionId.Neutral,
                Position));
        }
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

        // Apply minor morale hit on taking damage (-2)
        Morale.ApplyShock(2.0f);

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
            Charge.Reset();
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
        if (HeroState != null)
        {
            HeroState.OnLevelUp(Veterancy.Level);
        }

        if (IsAlive && healthBonus > 0f)
        {
            CurrentHealth = MathF.Min(MaxHealth, CurrentHealth + healthBonus);
        }
    }

    public void Heal(float amount)
    {
        if (IsAlive && amount > 0f)
        {
            CurrentHealth = MathF.Min(MaxHealth, CurrentHealth + amount);
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
        int reduction = HeroState?.TotalAttributes.CooldownReductionTicks ?? 0;
        CooldownRemaining = Math.Max(5, AttackCooldownTicks - reduction);
    }
}
