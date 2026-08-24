using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Commands;

public sealed record SetFormationCommand(
    FactionId FactionId,
    EntityId UnitId,
    FormationType Formation,
    ulong SubmittedTick = 0) : ICommand;

public sealed record SetSquadFormationCommand(
    FactionId FactionId,
    IReadOnlyList<EntityId> UnitIds,
    FormationType Formation,
    ulong SubmittedTick = 0) : ICommand;

public sealed record RallyUnitCommand(
    FactionId FactionId,
    EntityId UnitId,
    ulong SubmittedTick = 0) : ICommand;

public sealed record RallySquadCommand(
    FactionId FactionId,
    Vector2D Center,
    float Radius = 10.0f,
    ulong SubmittedTick = 0) : ICommand;
