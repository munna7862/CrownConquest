using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Entities;

/// <summary>
/// Domain model capturing RPG Hero state, attributes, abilities, mana, squad leadership, and auras.
/// </summary>
public sealed class HeroState
{
    private readonly List<EntityId> _attachedUnitIds = new(32);
    private readonly List<HeroAbilityState> _abilities = new(8);

    public HeroClass Class { get; }
    public string HeroName { get; }
    public HeroAttributes BaseAttributes { get; private set; }
    public HeroAttributes AllocatedAttributes { get; private set; }
    public int AvailableAttributePoints { get; private set; }

    public int StrengthPerLevel { get; }
    public int AgilityPerLevel { get; }
    public int WillpowerPerLevel { get; }

    public HeroAttributes TotalAttributes => BaseAttributes + AllocatedAttributes;

    public float CurrentMana { get; private set; }
    public float MaxMana => TotalAttributes.MaxMana;
    public float ManaRegenPerTick => TotalAttributes.ManaRegenPerTick;
    public float AbilityPotencyMultiplier => TotalAttributes.AbilityPotencyMultiplier;

    public int BaseLeadershipCapacity { get; }
    public int CurrentLevel { get; internal set; } = 1;

    public int LeadershipCapacity => BaseLeadershipCapacity + ((CurrentLevel - 1) * 2) + (TotalAttributes.Strength / 4);

    public IReadOnlyList<EntityId> AttachedUnitIds => _attachedUnitIds;
    public IReadOnlyList<HeroAbilityState> Abilities => _abilities;
    public HeroAura? ActiveAura { get; set; }

    public HeroState(
        HeroClass heroClass,
        string heroName,
        HeroAttributes baseAttributes,
        int baseLeadershipCapacity = 15,
        HeroAura? aura = null,
        int strengthPerLevel = 2,
        int agilityPerLevel = 1,
        int willpowerPerLevel = 1)
    {
        Class = heroClass;
        HeroName = heroName;
        BaseAttributes = baseAttributes;
        AllocatedAttributes = HeroAttributes.Zero;
        AvailableAttributePoints = 0;
        BaseLeadershipCapacity = baseLeadershipCapacity;
        ActiveAura = aura;
        StrengthPerLevel = strengthPerLevel;
        AgilityPerLevel = agilityPerLevel;
        WillpowerPerLevel = willpowerPerLevel;
        CurrentMana = TotalAttributes.MaxMana;
    }

    public void AddAbility(HeroAbilityDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!TryGetAbility(definition.Id, out _))
        {
            _abilities.Add(new HeroAbilityState(definition));
        }
    }

    public bool TryGetAbility(string abilityId, out HeroAbilityState? abilityState)
    {
        for (int i = 0; i < _abilities.Count; i++)
        {
            if (string.Equals(_abilities[i].Definition.Id, abilityId, StringComparison.OrdinalIgnoreCase))
            {
                abilityState = _abilities[i];
                return true;
            }
        }
        abilityState = null;
        return false;
    }

    public bool AttachUnit(EntityId unitId)
    {
        if (_attachedUnitIds.Count >= LeadershipCapacity)
        {
            return false;
        }

        if (!_attachedUnitIds.Contains(unitId))
        {
            _attachedUnitIds.Add(unitId);
            return true;
        }

        return false;
    }

    public bool DetachUnit(EntityId unitId)
    {
        return _attachedUnitIds.Remove(unitId);
    }

    public void ClearAttachedUnits()
    {
        _attachedUnitIds.Clear();
    }

    public void RegenerateMana()
    {
        CurrentMana = MathF.Min(MaxMana, CurrentMana + ManaRegenPerTick);
    }

    public bool ConsumeMana(float amount)
    {
        if (CurrentMana >= amount)
        {
            CurrentMana -= amount;
            return true;
        }
        return false;
    }

    public void RestoreMana(float amount)
    {
        CurrentMana = MathF.Min(MaxMana, CurrentMana + MathF.Max(0f, amount));
    }

    public void OnLevelUp(int newLevel)
    {
        int levelDelta = newLevel - CurrentLevel;
        if (levelDelta <= 0) return;

        CurrentLevel = newLevel;

        // Auto-scale base attributes per level
        BaseAttributes = new HeroAttributes(
            BaseAttributes.Strength + (StrengthPerLevel * levelDelta),
            BaseAttributes.Agility + (AgilityPerLevel * levelDelta),
            BaseAttributes.Willpower + (WillpowerPerLevel * levelDelta));

        // Award 1 free allocateable attribute point per level
        AvailableAttributePoints += levelDelta;

        // Recover 30% max mana on level up
        RestoreMana(MaxMana * 0.30f);
    }

    public bool AllocateAttribute(string attributeName)
    {
        if (AvailableAttributePoints <= 0) return false;

        var lower = attributeName.ToLowerInvariant();
        if (lower.Contains("str") || lower.Contains("strength"))
        {
            AllocatedAttributes = new HeroAttributes(
                AllocatedAttributes.Strength + 1,
                AllocatedAttributes.Agility,
                AllocatedAttributes.Willpower);
            AvailableAttributePoints--;
            return true;
        }
        if (lower.Contains("agi") || lower.Contains("agility"))
        {
            AllocatedAttributes = new HeroAttributes(
                AllocatedAttributes.Strength,
                AllocatedAttributes.Agility + 1,
                AllocatedAttributes.Willpower);
            AvailableAttributePoints--;
            return true;
        }
        if (lower.Contains("wil") || lower.Contains("will") || lower.Contains("willpower"))
        {
            AllocatedAttributes = new HeroAttributes(
                AllocatedAttributes.Strength,
                AllocatedAttributes.Agility,
                AllocatedAttributes.Willpower + 1);
            AvailableAttributePoints--;
            return true;
        }

        return false;
    }

    public void TickCooldowns()
    {
        for (int i = 0; i < _abilities.Count; i++)
        {
            _abilities[i].DecrementCooldown();
        }
    }
}
