using System;
using CrownConquest.Application;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

public readonly record struct ResourceBarViewModel(
    int Food,
    int Wood,
    int Gold,
    int Stone,
    int Iron,
    int CurrentPopulation,
    int MaxPopulation,
    bool IsPopCapped);

/// <summary>
/// Presenter maintaining real-time resource balances and population capacity for the HUD.
/// </summary>
public sealed class ResourceBarHudPresenter
{
    private readonly GameCoordinator _coordinator;
    public FactionId FactionId { get; }

    public ResourceBarHudPresenter(GameCoordinator coordinator, FactionId factionId)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        FactionId = factionId;
    }

    public ResourceBarViewModel GetViewModel()
    {
        var bank = _coordinator.GetResourceBank(FactionId);
        var popManager = _coordinator.GetPopulationManager(FactionId);

        return new ResourceBarViewModel(
            Food: bank.Food,
            Wood: bank.Wood,
            Gold: bank.Gold,
            Stone: bank.Stone,
            Iron: bank.Iron,
            CurrentPopulation: popManager.CurrentPopulation,
            MaxPopulation: popManager.CurrentMaxCapacity,
            IsPopCapped: popManager.IsPopCapped);
    }
}
