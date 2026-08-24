using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Commands;

public sealed record AttachToHeroCommand(
    FactionId FactionId,
    ulong SubmittedTick,
    EntityId HeroId,
    EntityId[] UnitIds) : ICommand;

public sealed record DetachFromHeroCommand(
    FactionId FactionId,
    ulong SubmittedTick,
    EntityId HeroId,
    EntityId[] UnitIds) : ICommand;

public sealed record CastHeroAbilityCommand(
    FactionId FactionId,
    ulong SubmittedTick,
    EntityId HeroId,
    string AbilityId,
    EntityId TargetEntityId,
    Vector2D TargetPosition) : ICommand;

public sealed record AllocateHeroAttributeCommand(
    FactionId FactionId,
    ulong SubmittedTick,
    EntityId HeroId,
    string AttributeName) : ICommand;
