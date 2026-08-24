using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Pure combat formulas for damage calculation, armor mitigation, rock-paper-scissors archetype multipliers,
/// terrain modifiers, elevation advantages, tactical formations, morale penalties, and cavalry charge impacts.
/// Deterministic and allocation-free.
/// </summary>
public static class CombatFormulas
{
    public const float MinimumDamageFloor = 1.0f;
    public const float SpearmanVsCavalryMultiplier = 2.5f;
    public const float CavalryVsArcherMultiplier = 1.5f;
    public const float ArcherVsSpearmanMultiplier = 1.25f;

    public const float HighGroundDamageBonus = 0.25f; // +25%
    public const float LowGroundDamagePenalty = -0.15f; // -15%
    public const float HighGroundRangeBonus = 2.0f; // +2 tiles
    public const float SpearBraceRecoilPercentage = 0.50f; // 50% charge damage reflected

    public const float BatteringRamStructuralMultiplier = 5.0f; // 5x vs buildings/walls/gates
    public const float CatapultStructuralMultiplier = 4.0f; // 4x vs buildings/walls/towers
    public const float BallistaStructuralMultiplier = 2.5f; // 2.5x vs buildings
    public const float BatteringRamPierceMitigation = 0.80f; // 80% damage reduction from ranged
    public const float BallistaArmorPenetration = 0.60f; // 60% target armor ignored
    public const float CatapultMinRange = 3.0f;
    public const float CatapultMaxRange = 12.0f;
    public const float CatapultSplashRadius = 2.5f;

    /// <summary>
    /// Calculates archetype interaction damage multiplier based on RTS combat triangle.
    /// </summary>
    public static float GetArchetypeMultiplier(UnitArchetype attacker, UnitArchetype target)
    {
        if (attacker == UnitArchetype.Spearman && target == UnitArchetype.Cavalry)
        {
            return SpearmanVsCavalryMultiplier;
        }

        if (attacker == UnitArchetype.Cavalry && target == UnitArchetype.Archer)
        {
            return CavalryVsArcherMultiplier;
        }

        if (attacker == UnitArchetype.Archer && target == UnitArchetype.Spearman)
        {
            return ArcherVsSpearmanMultiplier;
        }

        return 1.0f;
    }

    /// <summary>
    /// Calculates effective damage dealt after armor mitigation and damage multipliers.
    /// Effective Damage = max(1.0, (RawDamage * Modifier) - Armor)
    /// </summary>
    public static float CalculateEffectiveDamage(float rawDamage, float targetArmor, float modifier = 1.0f)
    {
        float modifiedDamage = rawDamage * modifier;
        float mitigated = modifiedDamage - MathF.Max(0f, targetArmor);
        return MathF.Max(MinimumDamageFloor, mitigated);
    }

    /// <summary>
    /// Calculates elevation damage multiplier.
    /// </summary>
    public static float GetElevationDamageMultiplier(int attackerElevation, int targetElevation)
    {
        if (attackerElevation > targetElevation)
        {
            return 1.0f + HighGroundDamageBonus; // 1.25
        }
        if (attackerElevation < targetElevation)
        {
            return 1.0f + LowGroundDamagePenalty; // 0.85
        }
        return 1.0f;
    }

    /// <summary>
    /// Calculates elevation range bonus.
    /// </summary>
    public static float GetElevationRangeBonus(int attackerElevation, int targetElevation)
    {
        return attackerElevation > targetElevation ? HighGroundRangeBonus : 0f;
    }

    /// <summary>
    /// Calculates morale damage penalty multiplier for attacker.
    /// </summary>
    public static float GetMoraleDamageMultiplier(MoraleLevel level) => level switch
    {
        MoraleLevel.Confident => 1.0f,
        MoraleLevel.Steady => 1.0f,
        MoraleLevel.Wavering => 0.90f, // -10% damage
        MoraleLevel.Breaking => 0.75f, // -25% damage
        MoraleLevel.Routed => 0.0f,    // Cannot deal damage
        _ => 1.0f
    };

    /// <summary>
    /// Calculates morale armor modifier for target.
    /// </summary>
    public static float GetMoraleArmorBonus(MoraleLevel level) => level switch
    {
        MoraleLevel.Confident => 0.0f,
        MoraleLevel.Steady => 0.0f,
        MoraleLevel.Wavering => -1.0f,
        MoraleLevel.Breaking => -2.0f,
        MoraleLevel.Routed => -5.0f,
        _ => 0.0f
    };

    /// <summary>
    /// Evaluates if an attack is hitting target from the flank/rear based on target's forward movement vector or heading.
    /// </summary>
    public static bool IsFlankingAttack(Vector2D attackerPos, Vector2D targetPos, Vector2D? targetHeadingDirection)
    {
        if (!targetHeadingDirection.HasValue || targetHeadingDirection.Value.LengthSquared < 0.01f)
        {
            return false;
        }

        Vector2D toAttacker = (attackerPos - targetPos).Normalized();
        Vector2D targetForward = targetHeadingDirection.Value.Normalized();

        // Dot product < 0 means attacker is behind target's front hemisphere
        float dot = (targetForward.X * toAttacker.X) + (targetForward.Y * toAttacker.Y);
        return dot < 0.2f; // Flanking / rear angle
    }

    /// <summary>
    /// Full tactical combat damage resolution calculation factoring in Archetypes, Technologies, Formations,
    /// Morale, Elevation, Terrain Cover, Cavalry Charge, and Spear Bracing.
    /// </summary>
    public static (float Damage, bool ChargeBlocked, float RecoilDamage) CalculateTacticalCombatDamage(
        UnitArchetype attackerArchetype,
        float attackerRawAttack,
        TechModifiers attackerTech,
        float attackerAuraDamageBonus,
        FormationModifiers attackerFormation,
        MoraleLevel attackerMorale,
        TerrainModifiers attackerTerrain,
        bool isAttackerCharging,
        bool isRangedAttack,
        UnitArchetype targetArchetype,
        float targetRawArmor,
        TechModifiers targetTech,
        float targetAuraArmorBonus,
        FormationModifiers targetFormation,
        MoraleLevel targetMorale,
        TerrainModifiers targetTerrain,
        float customModifier = 1.0f)
    {
        // 1. Morale check: Routed attackers cannot attack
        if (attackerMorale == MoraleLevel.Routed)
        {
            return (0f, false, 0f);
        }

        // 2. Base attack and tech bonuses
        float techAttackBonus = attackerArchetype switch
        {
            UnitArchetype.Infantry or UnitArchetype.Spearman or UnitArchetype.Hero => attackerTech.MeleeAttackBonus,
            UnitArchetype.Archer => attackerTech.RangedAttackBonus,
            UnitArchetype.Cavalry => attackerTech.CavalryAttackBonus,
            _ => 0f
        };

        // 3. Base armor and tech bonuses
        float techArmorBonus = targetArchetype switch
        {
            UnitArchetype.Infantry or UnitArchetype.Spearman or UnitArchetype.Hero => targetTech.MeleeArmorBonus,
            UnitArchetype.Archer => targetTech.RangedArmorBonus,
            UnitArchetype.Cavalry => targetTech.CavalryArmorBonus,
            _ => 0f
        };

        // 4. Formation modifiers
        float formationAtkMultiplier = isRangedAttack ? 1.0f : attackerFormation.MeleeDamageMultiplier;
        float totalArmor = targetRawArmor + techArmorBonus + targetAuraArmorBonus + targetFormation.ArmorBonus + GetMoraleArmorBonus(targetMorale);

        // 5. Elevation multipliers
        float elevationMultiplier = isRangedAttack
            ? GetElevationDamageMultiplier(attackerTerrain.ElevationLevel, targetTerrain.ElevationLevel)
            : 1.0f;

        // 6. Terrain cover mitigation (ranged attacks only)
        float coverMultiplier = isRangedAttack
            ? MathF.Max(0.1f, 1.0f - targetTerrain.RangedCoverMitigation - targetFormation.RangedDamageMitigation)
            : 1.0f;

        // 7. Charge and Spear Bracing
        bool chargeBlocked = false;
        float recoilDamage = 0f;
        float chargeDamageMultiplier = 1.0f;

        if (attackerArchetype == UnitArchetype.Cavalry && isAttackerCharging)
        {
            // Check if target bracers negate charge (Spearmen or Shield Wall / Square)
            if (targetArchetype == UnitArchetype.Spearman || targetFormation.CanBraceCavalry)
            {
                chargeBlocked = true;
                chargeDamageMultiplier = 1.0f; // Charge negated
                float potentialChargeDamage = attackerRawAttack * (attackerFormation.ChargeDamageMultiplier > 0 ? attackerFormation.ChargeDamageMultiplier : 1.0f);
                recoilDamage = potentialChargeDamage * SpearBraceRecoilPercentage;
            }
            else
            {
                chargeDamageMultiplier = ChargeState.MaxChargeDamageMultiplier * (attackerFormation.ChargeDamageMultiplier > 0 ? attackerFormation.ChargeDamageMultiplier : 1.0f);
            }
        }

        // 8. Combine multipliers
        float archetypeMultiplier = GetArchetypeMultiplier(attackerArchetype, targetArchetype);
        float moraleAtkMultiplier = GetMoraleDamageMultiplier(attackerMorale);
        float siegePierceMitigation = (targetArchetype == UnitArchetype.Siege && isRangedAttack) ? (1.0f - BatteringRamPierceMitigation) : 1.0f;

        float totalAttack = (attackerRawAttack + techAttackBonus) * (1.0f + attackerAuraDamageBonus);
        float combinedModifier = customModifier
            * archetypeMultiplier
            * formationAtkMultiplier
            * elevationMultiplier
            * coverMultiplier
            * moraleAtkMultiplier
            * chargeDamageMultiplier
            * siegePierceMitigation;

        float effectiveDamage = CalculateEffectiveDamage(totalAttack, totalArmor, combinedModifier);
        return (effectiveDamage, chargeBlocked, recoilDamage);
    }

    /// <summary>
    /// Calculates standard combat damage factoring in veterancy, tech upgrades, and unit archetypes (backward compatible).
    /// </summary>
    public static float CalculateCombatDamage(
        UnitArchetype attackerArchetype,
        float attackerRawAttack,
        TechModifiers attackerTech,
        UnitArchetype targetArchetype,
        float targetRawArmor,
        TechModifiers targetTech,
        float customModifier = 1.0f)
    {
        var (damage, _, _) = CalculateTacticalCombatDamage(
            attackerArchetype, attackerRawAttack, attackerTech, 0f, FormationModifiers.Line, MoraleLevel.Steady, TerrainModifiers.Plains, false, attackerArchetype == UnitArchetype.Archer,
            targetArchetype, targetRawArmor, targetTech, 0f, FormationModifiers.Line, MoraleLevel.Steady, TerrainModifiers.Plains, customModifier);
        return damage;
    }

    /// <summary>
    /// Calculates full combat damage including technology bonuses and hero leadership aura bonuses (backward compatible).
    /// </summary>
    public static float CalculateCombatDamageWithAura(
        UnitArchetype attackerArchetype,
        float attackerRawAttack,
        TechModifiers attackerTech,
        float attackerAuraDamageBonus,
        UnitArchetype targetArchetype,
        float targetRawArmor,
        TechModifiers targetTech,
        float targetAuraArmorBonus,
        float customModifier = 1.0f)
    {
        var (damage, _, _) = CalculateTacticalCombatDamage(
            attackerArchetype, attackerRawAttack, attackerTech, attackerAuraDamageBonus, FormationModifiers.Line, MoraleLevel.Steady, TerrainModifiers.Plains, false, attackerArchetype == UnitArchetype.Archer,
            targetArchetype, targetRawArmor, targetTech, targetAuraArmorBonus, FormationModifiers.Line, MoraleLevel.Steady, TerrainModifiers.Plains, customModifier);
        return damage;
    }

    /// <summary>
    /// Evaluates if target position is within attack range of attacker factoring in range tech bonuses and elevation.
    /// </summary>
    public static bool IsInRange(Vector2D attackerPos, Vector2D targetPos, float attackRange, float rangeBonus = 0f, int attackerElevation = 0, int targetElevation = 0)
    {
        float elevationBonus = GetElevationRangeBonus(attackerElevation, targetElevation);
        float totalRange = MathF.Max(0.5f, attackRange + rangeBonus + elevationBonus);
        float rangeWithTolerance = totalRange + 0.1f;
        return attackerPos.DistanceSquaredTo(targetPos) <= (rangeWithTolerance * rangeWithTolerance);
    }

    /// <summary>
    /// Calculates distance between two points.
    /// </summary>
    public static float GetDistance(Vector2D a, Vector2D b)
    {
        return a.DistanceTo(b);
    }

    /// <summary>
    /// Calculates effective spell/ability damage factoring in hero ability potency and target armor/resistance.
    /// </summary>
    public static float CalculateHeroSpellDamage(float baseAbilityPower, float abilityPotencyMultiplier, float targetArmor, float armorPenetration = 0.5f)
    {
        float rawSpellPower = baseAbilityPower * abilityPotencyMultiplier;
        float effectiveArmor = MathF.Max(0f, targetArmor * (1.0f - armorPenetration));
        float finalDamage = rawSpellPower - effectiveArmor;
        return MathF.Max(MinimumDamageFloor, finalDamage);
    }

    /// <summary>
    /// Calculates structural damage dealt by a unit against buildings, walls, gates, and towers.
    /// </summary>
    public static float CalculateStructuralCombatDamage(
        UnitArchetype attackerArchetype,
        string attackerUnitType,
        float attackerRawAttack,
        TechModifiers attackerTech,
        float buildingArmor = 0f,
        float customModifier = 1.0f)
    {
        float multiplier = 1.0f;
        var lower = attackerUnitType.ToLowerInvariant();
        if (lower.Contains("ram"))
        {
            multiplier = BatteringRamStructuralMultiplier; // 5.0x
        }
        else if (lower.Contains("catapult") || lower.Contains("onager") || lower.Contains("trebuchet"))
        {
            multiplier = CatapultStructuralMultiplier; // 4.0x
        }
        else if (lower.Contains("ballista") || lower.Contains("scorpion"))
        {
            multiplier = BallistaStructuralMultiplier; // 2.5x
        }
        else if (attackerArchetype == UnitArchetype.Siege)
        {
            multiplier = 4.0f;
        }

        float techBonus = attackerArchetype switch
        {
            UnitArchetype.Infantry or UnitArchetype.Spearman or UnitArchetype.Hero => attackerTech.MeleeAttackBonus,
            UnitArchetype.Archer => attackerTech.RangedAttackBonus,
            UnitArchetype.Cavalry => attackerTech.CavalryAttackBonus,
            _ => 0f
        };

        float totalAttack = (attackerRawAttack + techBonus) * multiplier * customModifier;
        float effective = totalAttack - MathF.Max(0f, buildingArmor);
        return MathF.Max(MinimumDamageFloor, effective);
    }

    /// <summary>
    /// Calculates area of effect splash damage with linear distance falloff (100% at center to 50% at edge).
    /// </summary>
    public static float CalculateAreaOfEffectDamage(
        float baseDamage,
        float distanceToCenter,
        float splashRadius)
    {
        if (splashRadius <= 0.001f || distanceToCenter <= 0.001f)
        {
            return MathF.Max(MinimumDamageFloor, baseDamage);
        }

        if (distanceToCenter > splashRadius)
        {
            return 0f;
        }

        float ratio = distanceToCenter / splashRadius;
        float falloff = 1.0f - (0.5f * ratio);
        return MathF.Max(MinimumDamageFloor, baseDamage * falloff);
    }

    /// <summary>
    /// Calculates direct armor piercing damage (e.g. Ballista bolts ignoring a portion of armor).
    /// </summary>
    public static float CalculateArmorPiercingDamage(
        float rawDamage,
        float targetArmor,
        float armorPenetration = BallistaArmorPenetration)
    {
        float effectiveArmor = MathF.Max(0f, targetArmor * (1.0f - Math.Clamp(armorPenetration, 0f, 1f)));
        float mitigated = rawDamage - effectiveArmor;
        return MathF.Max(MinimumDamageFloor, mitigated);
    }

    /// <summary>
    /// Evaluates if target position is within min and max attack range of a siege weapon (e.g. Catapult min 3.0, max 12.0).
    /// </summary>
    public static bool IsSiegeInRange(
        Vector2D attackerPos,
        Vector2D targetPos,
        float minRange,
        float maxRange,
        float rangeBonus = 0f,
        int attackerElevation = 0,
        int targetElevation = 0)
    {
        float elevationBonus = GetElevationRangeBonus(attackerElevation, targetElevation);
        float totalMaxRange = MathF.Max(0.5f, maxRange + rangeBonus + elevationBonus) + 0.1f;
        float totalMinRange = MathF.Max(0f, minRange - 0.1f);
        float distSq = attackerPos.DistanceSquaredTo(targetPos);

        return distSq <= (totalMaxRange * totalMaxRange) && distSq >= (totalMinRange * totalMinRange);
    }
}
