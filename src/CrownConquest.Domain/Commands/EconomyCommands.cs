using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Commands;

public sealed record GatherCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    EntityId[] WorkerIds,
    EntityId TargetNodeId) : ICommand;

public sealed record PlaceBuildingCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    string BuildingType,
    Vector2D Position) : ICommand;

public sealed record ConstructBuildingCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    EntityId[] WorkerIds,
    EntityId BuildingId) : ICommand;

public sealed record QueueProductionCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    EntityId BuildingId,
    string UnitType) : ICommand;

public sealed record CancelProductionCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    EntityId BuildingId,
    int QueueIndex) : ICommand;

public sealed record SetRallyPointCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    EntityId BuildingId,
    Vector2D RallyPoint) : ICommand;

public sealed record RepairBuildingCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    EntityId[] WorkerIds,
    EntityId BuildingId) : ICommand;

public sealed record ReseedFarmCommand(
    ulong SubmittedTick,
    FactionId FactionId,
    EntityId FarmId) : ICommand;

public sealed record SelectIdleWorkersCommand(
    ulong SubmittedTick,
    FactionId FactionId) : ICommand;

