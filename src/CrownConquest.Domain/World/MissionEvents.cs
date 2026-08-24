using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.World;

public readonly record struct MissionStartedEvent(
    ulong SimulationTick,
    string MissionId,
    MissionType Type,
    string IssuingFactionId
) : IDomainEvent;

public readonly record struct MissionProgressUpdatedEvent(
    ulong SimulationTick,
    string MissionId,
    int CurrentProgress,
    int TargetQuantity
) : IDomainEvent;

public readonly record struct MissionCompletedEvent(
    ulong SimulationTick,
    string MissionId,
    MissionType Type,
    string IssuingFactionId,
    int GoldReward,
    int XpReward,
    int ReputationReward
) : IDomainEvent;

public readonly record struct MissionFailedEvent(
    ulong SimulationTick,
    string MissionId,
    MissionType Type,
    string Reason
) : IDomainEvent;

public readonly record struct MissionExpiredEvent(
    ulong SimulationTick,
    string MissionId,
    MissionType Type
) : IDomainEvent;

public readonly record struct FactionReputationChangedEvent(
    ulong SimulationTick,
    string FactionId,
    int OldReputation,
    int NewReputation,
    int Delta
) : IDomainEvent;

public readonly record struct FactionStandingChangedEvent(
    ulong SimulationTick,
    string FactionId,
    DiplomacyStanding OldStanding,
    DiplomacyStanding NewStanding
) : IDomainEvent;
