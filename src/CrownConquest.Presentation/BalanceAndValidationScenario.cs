using System;
using CrownConquest.Application;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Playable & headless scenario orchestrating end-to-end battle simulator runs, batch balance benchmarks,
/// faction reports, progression curve audits, mid-battle save/load parity tests, and soak testing.
/// </summary>
public sealed class BalanceAndValidationScenario
{
    private readonly BalanceValidationCoordinator _coordinator = new();
    private readonly BalanceAndValidationPresenter _presenter;

    public BalanceValidationCoordinator Coordinator => _coordinator;
    public BalanceAndValidationPresenter Presenter => _presenter;

    public BattleSimulationViewModel? LatestBattleVm { get; private set; }
    public BatchBalanceViewModel? LatestBatchVm { get; private set; }
    public FactionBalanceViewModel? LatestFactionVm { get; private set; }
    public ProgressionValidationReport? LatestProgressionReport { get; private set; }
    public SaveLoadValidationResult? LatestSaveLoadResult { get; private set; }
    public SoakTelemetryViewModel? LatestSoakVm { get; private set; }

    public BalanceAndValidationScenario()
    {
        _presenter = new BalanceAndValidationPresenter(_coordinator);
    }

    /// <summary>
    /// Executes the full multi-step balance and validation scenario workflow.
    /// </summary>
    public bool RunCompleteScenario()
    {
        // 1. Single Deterministic Battle Simulation
        var battleConfig = BattleSimulatorConfig.CreateStandardMatchup("spearman", 10, "cavalry", 10, seed: 1337);
        var battleResult = _coordinator.RunSingleBattle(battleConfig);
        LatestBattleVm = _presenter.BuildBattleViewModel(battleResult, "Spearmen", "Cavalry");

        if (battleResult.DurationTicks == 0 || battleResult.InitialUnitsA != 10)
        {
            return false;
        }

        // 2. Batch Balance Runs (50 iterations)
        var batchConfig = BatchBattleConfig.Create(battleConfig, iterations: 50, baseSeed: 3000);
        var batchResult = _coordinator.RunBatchBattles(batchConfig);
        LatestBatchVm = _presenter.BuildBatchViewModel(batchResult, "Spearmen", "Cavalry");

        if (batchResult.TotalBattles != 50)
        {
            return false;
        }

        // 3. Faction Balance Matrix & Diagnostics Report
        var factionReport = _coordinator.GenerateFactionReport(battlesPerMatchup: 10, seed: 4000);
        LatestFactionVm = _presenter.BuildFactionViewModel(factionReport);

        if (factionReport.TotalMatchups != 10)
        {
            return false;
        }

        // 4. Progression Curve & Veterancy Invariant Validation
        LatestProgressionReport = _coordinator.ValidateProgression();
        if (!LatestProgressionReport.IsValid)
        {
            return false;
        }

        // 5. Mid-Battle Save/Load State Parity
        LatestSaveLoadResult = _coordinator.ValidateSaveLoadParity(initialTicks: 50, continuationTicks: 50, seed: 42);
        if (!LatestSaveLoadResult.IsMatch)
        {
            return false;
        }

        // 6. Fast Soak Test (1000 ticks)
        var soakConfig = SoakTestConfig.CreateFast(1000);
        var soakResult = _coordinator.RunSoakTest(soakConfig);
        LatestSoakVm = _presenter.BuildSoakViewModel(soakResult);

        return soakResult.IsSuccessful;
    }
}
