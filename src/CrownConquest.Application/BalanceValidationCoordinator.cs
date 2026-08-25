using System;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Application;

/// <summary>
/// Application coordinator managing automated battle simulation trials, batch balance benchmarks,
/// faction reports, progression validation, and soak stability testing.
/// </summary>
public sealed class BalanceValidationCoordinator
{
    private readonly BattleSimulatorEngine _battleSimulator = new();
    private readonly BatchBattleRunner _batchRunner = new();
    private readonly FactionBalanceReportGenerator _reportGenerator = new();
    private readonly SaveLoadStateValidator _saveLoadValidator = new();
    private readonly SimulationSoakHarness _soakHarness = new();

    public BattleSimulatorResult? LatestBattleResult { get; private set; }
    public BatchBattleResult? LatestBatchResult { get; private set; }
    public FactionBalanceReport? LatestFactionReport { get; private set; }
    public ProgressionValidationReport? LatestProgressionReport { get; private set; }
    public SaveLoadValidationResult? LatestSaveLoadResult { get; private set; }
    public SoakTestResult? LatestSoakResult { get; private set; }

    public BattleSimulatorResult RunSingleBattle(BattleSimulatorConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        LatestBattleResult = _battleSimulator.ExecuteBattle(config);
        return LatestBattleResult;
    }

    public BatchBattleResult RunBatchBattles(BatchBattleConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        LatestBatchResult = _batchRunner.RunBatch(config);
        return LatestBatchResult;
    }

    public FactionBalanceReport GenerateFactionReport(int battlesPerMatchup = 20, int seed = 2000)
    {
        LatestFactionReport = _reportGenerator.GenerateReport(battlesPerMatchup, seed);
        return LatestFactionReport;
    }

    public ProgressionValidationReport ValidateProgression()
    {
        LatestProgressionReport = ProgressionBalanceValidator.ValidateProgressionInvariants();
        return LatestProgressionReport;
    }

    public SaveLoadValidationResult ValidateSaveLoadParity(int initialTicks = 100, int continuationTicks = 100, int seed = 42)
    {
        LatestSaveLoadResult = _saveLoadValidator.ValidateMidSimulationParity(initialTicks, continuationTicks, seed);
        return LatestSaveLoadResult;
    }

    public SoakTestResult RunSoakTest(SoakTestConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        LatestSoakResult = _soakHarness.RunSoakTest(config);
        return LatestSoakResult;
    }
}
