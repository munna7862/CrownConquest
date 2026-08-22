using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;

namespace CrownConquest.Domain.Commands;

public sealed record AdvanceEraCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    EntityId BuildingId,
    CivilizationEra TargetEra) : ICommand;

public sealed record CancelEraAdvancementCommand(
    ulong SubmittedTick,
    FactionId FactionId) : ICommand;

public sealed record StartResearchCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    EntityId BuildingId,
    string TechnologyId) : ICommand;

public sealed record CancelResearchCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    EntityId BuildingId,
    int QueueIndex = 0) : ICommand;
