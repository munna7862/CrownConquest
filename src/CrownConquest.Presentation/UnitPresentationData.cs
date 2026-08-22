using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Presentation;

/// <summary>
/// Immutable view-model representing presentation state for an active unit.
/// </summary>
public readonly record struct UnitPresentationViewModel(
    EntityId Id,
    FactionId FactionId,
    string UnitType,
    Vector2D Position,
    float CurrentHealth,
    float MaxHealth,
    float HealthPercentage,
    float Armor,
    float AttackDamage,
    float AttackRange,
    string AttackType,
    int Level,
    int CurrentXp,
    int XpToNextLevel,
    int KillCount,
    VeterancyRank Rank,
    string RankName,
    UnitState State,
    bool IsSelected);
