using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

public readonly record struct BuildingViewModel(
    EntityId Id,
    FactionId FactionId,
    string BuildingType,
    Vector2D Position,
    Vector2D GridSize,
    float CurrentHealth,
    float MaxHealth,
    float BuildProgressNormalized,
    bool IsConstructed,
    int ProductionQueueCount,
    string? ActiveProductionUnitType,
    float ActiveProductionProgressNormalized);

public readonly record struct ResourceNodeViewModel(
    EntityId Id,
    ResourceType ResourceType,
    Vector2D Position,
    int RemainingAmount,
    int MaxAmount,
    bool IsDepleted);

public readonly record struct WorkerPresentationViewModel(
    EntityId Id,
    FactionId FactionId,
    Vector2D Position,
    UnitState State,
    WorkerTaskState TaskState,
    ResourceType? CarriedType,
    int CarriedAmount,
    int CarryCapacity);

/// <summary>
/// Top-level presenter bridging settlement economy simulation to Godot UI HUD, minimap, and command cards.
/// </summary>
public sealed class SettlementEconomyPresenter
{
    private readonly SettlementEconomyScenario _scenario;
    private readonly ResourceBarHudPresenter _resourceBar;
    private readonly BuildingPlacementPreview _placementPreview;
    private readonly RtsCameraController _camera;
    private readonly List<string> _eventLog = new(128);

    public SettlementEconomyScenario Scenario => _scenario;
    public GameCoordinator Coordinator => _scenario.Coordinator;
    public ResourceBarHudPresenter ResourceBar => _resourceBar;
    public BuildingPlacementPreview PlacementPreview => _placementPreview;
    public RtsCameraController Camera => _camera;
    public IReadOnlyList<string> EventLog => _eventLog;

    public SettlementEconomyPresenter(
        SettlementEconomyScenario? scenario = null,
        RtsCameraController? camera = null)
    {
        _scenario = scenario ?? new SettlementEconomyScenario();
        _resourceBar = new ResourceBarHudPresenter(_scenario.Coordinator, _scenario.PlayerFaction);
        _placementPreview = new BuildingPlacementPreview(_scenario.Coordinator);
        _camera = camera ?? new RtsCameraController();

        // Subscribe to economy and construction events
        Coordinator.EventBus.Subscribe<ResourceDepositedEvent>(OnResourceDeposited);
        Coordinator.EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
        Coordinator.EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        Coordinator.EventBus.Subscribe<ProductionStartedEvent>(OnProductionStarted);
        Coordinator.EventBus.Subscribe<ProductionCompletedEvent>(OnProductionCompleted);
    }

    public List<BuildingViewModel> GetBuildingViewModels()
    {
        var buildings = Coordinator.Simulation.State.ActiveBuildings;
        var list = new List<BuildingViewModel>(buildings.Count);

        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            var currentProd = b.ProductionQueue.CurrentItem;

            list.Add(new BuildingViewModel(
                Id: b.Id,
                FactionId: b.FactionId,
                BuildingType: b.BuildingType,
                Position: b.Position,
                GridSize: b.GridSize,
                CurrentHealth: b.CurrentHealth,
                MaxHealth: b.MaxHealth,
                BuildProgressNormalized: b.BuildProgressNormalized,
                IsConstructed: b.IsConstructed,
                ProductionQueueCount: b.ProductionQueue.Count,
                ActiveProductionUnitType: currentProd?.UnitType,
                ActiveProductionProgressNormalized: currentProd?.ProgressNormalized ?? 0f));
        }

        return list;
    }

    public List<ResourceNodeViewModel> GetResourceNodeViewModels()
    {
        var nodes = Coordinator.Simulation.State.ActiveResourceNodes;
        var list = new List<ResourceNodeViewModel>(nodes.Count);

        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            list.Add(new ResourceNodeViewModel(
                Id: n.Id,
                ResourceType: n.ResourceType,
                Position: n.Position,
                RemainingAmount: n.RemainingAmount,
                MaxAmount: n.MaxAmount,
                IsDepleted: n.IsDepleted));
        }

        return list;
    }

    public List<WorkerPresentationViewModel> GetWorkerViewModels()
    {
        var units = Coordinator.Simulation.State.ActiveUnits;
        var list = new List<WorkerPresentationViewModel>();

        for (int i = 0; i < units.Count; i++)
        {
            var u = units[i];
            if (u.WorkerState != null)
            {
                list.Add(new WorkerPresentationViewModel(
                    Id: u.Id,
                    FactionId: u.FactionId,
                    Position: u.Position,
                    State: u.State,
                    TaskState: u.WorkerState.TaskState,
                    CarriedType: u.WorkerState.CarriedResourceType,
                    CarriedAmount: u.WorkerState.CarriedAmount,
                    CarryCapacity: u.WorkerState.CarryCapacity));
            }
        }

        return list;
    }

    private void OnResourceDeposited(in ResourceDepositedEvent e)
    {
        _eventLog.Add($"[Tick {e.SimulationTick}] Deposited +{e.AmountDeposited} {e.Type}. New Balance: {e.NewBankBalance}");
    }

    private void OnBuildingPlacedEvent(in BuildingPlacedEvent e)
    {
        _eventLog.Add($"[Tick {e.SimulationTick}] Placed blueprint for {e.BuildingType} {e.BuildingId} at {e.Position}");
    }

    private void OnBuildingPlaced(in BuildingPlacedEvent e) => OnBuildingPlacedEvent(in e);

    private void OnBuildingCompleted(in BuildingCompletedEvent e)
    {
        _eventLog.Add($"[Tick {e.SimulationTick}] 🏛️ {e.BuildingType} construction complete at {e.Position}!");
    }

    private void OnProductionStarted(in ProductionStartedEvent e)
    {
        _eventLog.Add($"[Tick {e.SimulationTick}] Started training {e.UnitType} at building {e.BuildingId} ({e.TotalDurationTicks} ticks)");
    }

    private void OnProductionCompleted(in ProductionCompletedEvent e)
    {
        _eventLog.Add($"[Tick {e.SimulationTick}] ⚔️ Trained unit {e.UnitType} {e.ProducedUnitId} ready for battle!");
    }
}
