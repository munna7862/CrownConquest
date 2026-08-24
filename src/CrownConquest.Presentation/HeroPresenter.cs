using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

public readonly record struct HeroAbilityCardPresentation(
    string AbilityId,
    string DisplayName,
    string Description,
    float ManaCost,
    int CooldownRemainingTicks,
    int TotalCooldownTicks,
    bool IsReady,
    float CooldownNormalized,
    bool CanCast);

/// <summary>
/// Presentation layer view model providing data binding and interaction for Hero HUD,
/// attributes, ability cards, squad leadership, and cooldown overlays.
/// </summary>
public sealed class HeroPresenter
{
    private readonly GameCoordinator _coordinator;
    private readonly FactionId _factionId;
    private EntityId _activeHeroId = EntityId.None;

    public EntityId ActiveHeroId => _activeHeroId;
    public bool HasActiveHero { get; private set; }
    public string HeroName { get; private set; } = string.Empty;
    public string HeroClassName { get; private set; } = string.Empty;
    public HeroClass Class { get; private set; }
    public int Level { get; private set; } = 1;
    public int CurrentXp { get; private set; }
    public int NextLevelThreshold { get; private set; }
    public float XpProgressNormalized { get; private set; }

    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public float HealthNormalized { get; private set; }

    public float CurrentMana { get; private set; }
    public float MaxMana { get; private set; }
    public float ManaNormalized { get; private set; }

    public int Strength { get; private set; }
    public int Agility { get; private set; }
    public int Willpower { get; private set; }
    public int AvailableAttributePoints { get; private set; }

    public int AttachedSquadCount { get; private set; }
    public int LeadershipCapacity { get; private set; }
    public IReadOnlyList<EntityId> AttachedUnitIds { get; private set; } = Array.Empty<EntityId>();

    public string AuraName { get; private set; } = string.Empty;
    public float AuraRadius { get; private set; }
    public float AuraDamageBonus { get; private set; }
    public float AuraArmorBonus { get; private set; }
    public float AuraSpeedBonus { get; private set; }

    public List<HeroAbilityCardPresentation> AbilityCards { get; } = new();

    public HeroPresenter(GameCoordinator coordinator, FactionId factionId, EntityId? initialHeroId = null)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _factionId = factionId;
        if (initialHeroId.HasValue)
        {
            _activeHeroId = initialHeroId.Value;
        }
        UpdateSnapshot();
    }

    public void SelectHero(EntityId heroId)
    {
        _activeHeroId = heroId;
        UpdateSnapshot();
    }

    public void UpdateSnapshot()
    {
        UnitEntity? heroUnit = null;

        if (_activeHeroId.IsValid && _coordinator.Simulation.State.TryGetUnit(_activeHeroId, out var unit) && unit != null && unit.IsHero)
        {
            heroUnit = unit;
        }
        else
        {
            // Auto-select first living friendly hero
            var units = _coordinator.Simulation.State.ActiveUnits;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].FactionId == _factionId && units[i].IsHero && units[i].IsAlive)
                {
                    heroUnit = units[i];
                    _activeHeroId = heroUnit.Id;
                    break;
                }
            }
        }

        if (heroUnit == null || heroUnit.HeroState == null)
        {
            HasActiveHero = false;
            AbilityCards.Clear();
            return;
        }

        HasActiveHero = true;
        var heroState = heroUnit.HeroState;

        HeroName = heroState.HeroName;
        Class = heroState.Class;
        HeroClassName = heroState.Class.GetDisplayName();
        Level = heroUnit.Veterancy.Level;
        CurrentXp = heroUnit.Veterancy.CurrentXp;
        NextLevelThreshold = heroUnit.Veterancy.NextLevelThreshold;
        XpProgressNormalized = heroUnit.Veterancy.ProgressNormalized;

        CurrentHealth = heroUnit.CurrentHealth;
        MaxHealth = heroUnit.MaxHealth;
        HealthNormalized = MaxHealth > 0f ? Math.Clamp(CurrentHealth / MaxHealth, 0f, 1f) : 0f;

        CurrentMana = heroState.CurrentMana;
        MaxMana = heroState.MaxMana;
        ManaNormalized = MaxMana > 0f ? Math.Clamp(CurrentMana / MaxMana, 0f, 1f) : 0f;

        var attrs = heroState.TotalAttributes;
        Strength = attrs.Strength;
        Agility = attrs.Agility;
        Willpower = attrs.Willpower;
        AvailableAttributePoints = heroState.AvailableAttributePoints;

        AttachedSquadCount = heroState.AttachedUnitIds.Count;
        LeadershipCapacity = heroState.LeadershipCapacity;
        AttachedUnitIds = heroState.AttachedUnitIds;

        if (heroState.ActiveAura != null)
        {
            AuraName = heroState.ActiveAura.AuraName;
            AuraRadius = heroState.ActiveAura.Radius;
            AuraDamageBonus = heroState.ActiveAura.DamageMultiplierBonus;
            AuraArmorBonus = heroState.ActiveAura.ArmorBonus;
            AuraSpeedBonus = heroState.ActiveAura.MovementSpeedMultiplierBonus;
        }

        // Ability cards
        AbilityCards.Clear();
        for (int i = 0; i < heroState.Abilities.Count; i++)
        {
            var ab = heroState.Abilities[i];
            bool canCast = heroUnit.IsAlive && ab.IsReady && heroState.CurrentMana >= ab.Definition.ManaCost;

            AbilityCards.Add(new HeroAbilityCardPresentation(
                ab.Definition.Id,
                ab.Definition.DisplayName,
                ab.Definition.Description,
                ab.Definition.ManaCost,
                ab.CooldownRemainingTicks,
                ab.Definition.CooldownTicks,
                ab.IsReady,
                ab.CooldownNormalized,
                canCast));
        }
    }

    public void CastAbility(string abilityId, EntityId targetEntityId, Vector2D targetPos)
    {
        if (!HasActiveHero || !_activeHeroId.IsValid) return;

        _coordinator.DispatchCommand(new CastHeroAbilityCommand(
            _factionId,
            _coordinator.CurrentTick,
            _activeHeroId,
            abilityId,
            targetEntityId,
            targetPos));
    }

    public void AttachUnits(params EntityId[] unitIds)
    {
        if (!HasActiveHero || !_activeHeroId.IsValid || unitIds.Length == 0) return;

        _coordinator.DispatchCommand(new AttachToHeroCommand(
            _factionId,
            _coordinator.CurrentTick,
            _activeHeroId,
            unitIds));
    }

    public void DetachUnits(params EntityId[] unitIds)
    {
        if (!HasActiveHero || !_activeHeroId.IsValid || unitIds.Length == 0) return;

        _coordinator.DispatchCommand(new DetachFromHeroCommand(
            _factionId,
            _coordinator.CurrentTick,
            _activeHeroId,
            unitIds));
    }

    public void AllocateAttribute(string attributeName)
    {
        if (!HasActiveHero || !_activeHeroId.IsValid) return;

        _coordinator.DispatchCommand(new AllocateHeroAttributeCommand(
            _factionId,
            _coordinator.CurrentTick,
            _activeHeroId,
            attributeName));
    }
}
