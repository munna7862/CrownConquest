using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Commands;

/// <summary>
/// Orders a unit or group of units (including siege engines) to attack a target building/wall/gate/tower.
/// </summary>
public sealed record AttackBuildingCommand(
    FactionId FactionId,
    EntityId[] UnitIds,
    EntityId TargetBuildingId,
    ulong SubmittedTick = 0) : ICommand;

/// <summary>
/// Orders a gate to toggle or set its state (Closed, Open, Locked).
/// </summary>
public sealed record ToggleGateCommand(
    FactionId FactionId,
    EntityId GateId,
    GateState? TargetState = null,
    ulong SubmittedTick = 0) : ICommand;

/// <summary>
/// Orders infantry/archers to garrison into a defensive tower.
/// </summary>
public sealed record GarrisonTowerCommand(
    FactionId FactionId,
    EntityId TowerId,
    EntityId[] UnitIds,
    ulong SubmittedTick = 0) : ICommand;

/// <summary>
/// Orders all garrisoned units inside a tower to ungarrison.
/// </summary>
public sealed record UngarrisonTowerCommand(
    FactionId FactionId,
    EntityId TowerId,
    ulong SubmittedTick = 0) : ICommand;
