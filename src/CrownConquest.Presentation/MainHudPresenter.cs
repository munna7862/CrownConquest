using System;
using CrownConquest.Application;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// HUD View Models
// ─────────────────────────────────────────────────

/// <summary>
/// View model for the command card showing available actions for the current selection.
/// </summary>
public readonly record struct CommandCardViewModel(
    string[] AvailableCommands,
    string[] ProductionOptions,
    bool CanAttackMove,
    bool CanPatrol,
    bool CanStop,
    bool CanGather,
    bool CanConstruct,
    bool CanRepair);

/// <summary>
/// View model summarizing a group of selected units.
/// </summary>
public readonly record struct UnitGroupSummaryViewModel(
    int TotalCount,
    float AverageHealthPercentage,
    int MeleeCount,
    int RangedCount,
    int CavalryCount,
    int SiegeCount,
    int WorkerCount,
    int HeroCount,
    string PrimaryUnitType);

/// <summary>
/// View model for the single-unit status panel with detailed stats.
/// </summary>
public readonly record struct UnitStatusPanelViewModel(
    EntityId UnitId,
    string UnitType,
    string DisplayName,
    float CurrentHealth,
    float MaxHealth,
    float HealthPercentage,
    float AttackDamage,
    float Armor,
    float AttackRange,
    string AttackType,
    float MovementSpeed,
    int Level,
    int CurrentXp,
    int XpToNextLevel,
    VeterancyRank Rank,
    string RankDisplayName,
    int KillCount,
    UnitState State,
    bool IsHero,
    bool IsWorker);

/// <summary>
/// View model for a notification entry in the HUD notification queue.
/// </summary>
public readonly record struct NotificationViewModel(
    ulong Tick,
    NotificationType Type,
    string Message,
    Vector2D? WorldPosition);

public enum NotificationType
{
    UnitLevelUp,
    VeterancyRankUp,
    BuildingCompleted,
    ProductionCompleted,
    ResearchCompleted,
    EraAdvanced,
    UnitKilled,
    HeroFallen,
    ResourceDepleted,
    UnderAttack
}

/// <summary>
/// Main HUD presenter that aggregates all HUD panels into a unified view model.
/// Subscribes to domain events for notification generation.
/// </summary>
public sealed class MainHudPresenter
{
    private readonly GameCoordinator _coordinator;
    private readonly DomainEventBus _eventBus;
    private readonly FactionId _factionId;
    private readonly ResourceBarHudPresenter _resourcePresenter;

    // Pre-allocated notification buffer to avoid allocations
    private readonly NotificationViewModel[] _notificationBuffer;
    private int _notificationCount;
    private readonly int _maxNotifications;

    public FactionId FactionId => _factionId;
    public int NotificationCount => _notificationCount;

    public MainHudPresenter(
        GameCoordinator coordinator,
        DomainEventBus eventBus,
        FactionId factionId,
        int maxNotifications = 32)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _factionId = factionId;
        _resourcePresenter = new ResourceBarHudPresenter(coordinator, factionId);
        _maxNotifications = maxNotifications;
        _notificationBuffer = new NotificationViewModel[maxNotifications];
        _notificationCount = 0;

        // Subscribe to events for notification generation
        _eventBus.Subscribe<UnitLevelUpEvent>(OnUnitLevelUp);
        _eventBus.Subscribe<VeterancyRankChangedEvent>(OnVeterancyRankChanged);
        _eventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        _eventBus.Subscribe<ProductionCompletedEvent>(OnProductionCompleted);
        _eventBus.Subscribe<EraAdvancementCompletedEvent>(OnEraAdvanced);
        _eventBus.Subscribe<HeroFallenEvent>(OnHeroFallen);
        _eventBus.Subscribe<ResourceNodeDepletedEvent>(OnResourceDepleted);
    }

    public ResourceBarViewModel GetResourceBarViewModel() => _resourcePresenter.GetViewModel();

    public CommandCardViewModel GetCommandCardForUnit(UnitEntity unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        bool isWorker = unit.IsWorker;
        bool isHero = unit.IsHero;
        string attackType = unit.AttackType;

        // Determine available commands based on unit type
        var commands = isWorker
            ? new[] { "move", "stop", "gather", "construct", "repair" }
            : isHero
                ? new[] { "move", "stop", "attack", "patrol", "ability1", "ability2", "ability3" }
                : new[] { "move", "stop", "attack", "patrol", "formation" };

        return new CommandCardViewModel(
            AvailableCommands: commands,
            ProductionOptions: Array.Empty<string>(),
            CanAttackMove: !isWorker,
            CanPatrol: !isWorker,
            CanStop: true,
            CanGather: isWorker,
            CanConstruct: isWorker,
            CanRepair: isWorker);
    }

    public UnitStatusPanelViewModel GetUnitStatusPanel(UnitEntity unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        return new UnitStatusPanelViewModel(
            UnitId: unit.Id,
            UnitType: unit.UnitType,
            DisplayName: FormatDisplayName(unit.UnitType),
            CurrentHealth: unit.CurrentHealth,
            MaxHealth: unit.MaxHealth,
            HealthPercentage: unit.MaxHealth > 0f ? unit.CurrentHealth / unit.MaxHealth : 0f,
            AttackDamage: unit.AttackDamage,
            Armor: unit.Armor,
            AttackRange: unit.AttackRange,
            AttackType: unit.AttackType,
            MovementSpeed: unit.EffectiveMovementSpeed,
            Level: unit.Veterancy.Level,
            CurrentXp: unit.Veterancy.CurrentXp,
            XpToNextLevel: unit.Veterancy.XpToNextLevel,
            Rank: unit.Veterancy.Rank,
            RankDisplayName: unit.Veterancy.Rank.GetDisplayName(),
            KillCount: unit.Veterancy.KillCount,
            State: unit.State,
            IsHero: unit.IsHero,
            IsWorker: unit.IsWorker);
    }

    public UnitGroupSummaryViewModel GetGroupSummary(UnitEntity[] units)
    {
        if (units.Length == 0)
        {
            return new UnitGroupSummaryViewModel(0, 0f, 0, 0, 0, 0, 0, 0, "");
        }

        float totalHealthPct = 0f;
        int melee = 0, ranged = 0, cavalry = 0, siege = 0, worker = 0, hero = 0;

        for (int i = 0; i < units.Length; i++)
        {
            var u = units[i];
            totalHealthPct += u.MaxHealth > 0f ? u.CurrentHealth / u.MaxHealth : 0f;

            if (u.IsHero) hero++;
            else if (u.IsWorker) worker++;
            else
            {
                switch (u.Archetype)
                {
                    case UnitArchetype.Cavalry: cavalry++; break;
                    case UnitArchetype.Siege: siege++; break;
                    case UnitArchetype.Archer: ranged++; break;
                    default: melee++; break;
                }
            }
        }

        return new UnitGroupSummaryViewModel(
            TotalCount: units.Length,
            AverageHealthPercentage: totalHealthPct / units.Length,
            MeleeCount: melee,
            RangedCount: ranged,
            CavalryCount: cavalry,
            SiegeCount: siege,
            WorkerCount: worker,
            HeroCount: hero,
            PrimaryUnitType: units[0].UnitType);
    }

    public NotificationViewModel GetNotification(int index)
    {
        if (index < 0 || index >= _notificationCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _notificationBuffer[index];
    }

    public void ClearNotifications() => _notificationCount = 0;

    private void PushNotification(NotificationViewModel notification)
    {
        if (_notificationCount < _maxNotifications)
        {
            _notificationBuffer[_notificationCount++] = notification;
        }
        else
        {
            // Ring buffer: overwrite oldest
            Array.Copy(_notificationBuffer, 1, _notificationBuffer, 0, _maxNotifications - 1);
            _notificationBuffer[_maxNotifications - 1] = notification;
        }
    }

    private void OnUnitLevelUp(in UnitLevelUpEvent evt)
    {
        PushNotification(new NotificationViewModel(
            evt.SimulationTick, NotificationType.UnitLevelUp,
            $"Unit reached level {evt.NewLevel}!", null));
    }

    private void OnVeterancyRankChanged(in VeterancyRankChangedEvent evt)
    {
        PushNotification(new NotificationViewModel(
            evt.SimulationTick, NotificationType.VeterancyRankUp,
            $"Unit promoted to {evt.NewRank.GetDisplayName()}!", null));
    }

    private void OnBuildingCompleted(in BuildingCompletedEvent evt)
    {
        if (evt.FactionId != _factionId) return;
        PushNotification(new NotificationViewModel(
            evt.SimulationTick, NotificationType.BuildingCompleted,
            $"{FormatDisplayName(evt.BuildingType)} completed.", evt.Position));
    }

    private void OnProductionCompleted(in ProductionCompletedEvent evt)
    {
        if (evt.FactionId != _factionId) return;
        PushNotification(new NotificationViewModel(
            evt.SimulationTick, NotificationType.ProductionCompleted,
            $"{FormatDisplayName(evt.UnitType)} trained.", null));
    }

    private void OnEraAdvanced(in EraAdvancementCompletedEvent evt)
    {
        if (evt.FactionId != _factionId) return;
        PushNotification(new NotificationViewModel(
            evt.SimulationTick, NotificationType.EraAdvanced,
            $"Advanced to {evt.NewEra}!", null));
    }

    private void OnHeroFallen(in HeroFallenEvent evt)
    {
        if (evt.FactionId != _factionId) return;
        PushNotification(new NotificationViewModel(
            evt.SimulationTick, NotificationType.HeroFallen,
            "Hero has fallen!", evt.Position));
    }

    private void OnResourceDepleted(in ResourceNodeDepletedEvent evt)
    {
        PushNotification(new NotificationViewModel(
            evt.SimulationTick, NotificationType.ResourceDepleted,
            $"{evt.Type} node depleted.", evt.Position));
    }

    public void Unregister()
    {
        _eventBus.Unsubscribe<UnitLevelUpEvent>(OnUnitLevelUp);
        _eventBus.Unsubscribe<VeterancyRankChangedEvent>(OnVeterancyRankChanged);
        _eventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        _eventBus.Unsubscribe<ProductionCompletedEvent>(OnProductionCompleted);
        _eventBus.Unsubscribe<EraAdvancementCompletedEvent>(OnEraAdvanced);
        _eventBus.Unsubscribe<HeroFallenEvent>(OnHeroFallen);
        _eventBus.Unsubscribe<ResourceNodeDepletedEvent>(OnResourceDepleted);
    }

    private static string FormatDisplayName(string typeId)
    {
        if (string.IsNullOrEmpty(typeId)) return "";
        // Convert snake_case to Title Case: "celtic_swordsman" -> "Celtic Swordsman"
        var parts = typeId.Split('_');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
            }
        }
        return string.Join(' ', parts);
    }
}
