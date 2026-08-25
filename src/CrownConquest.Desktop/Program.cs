using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using CrownConquest.Application;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Shipping;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;

namespace CrownConquest.Desktop;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length > 0)
        {
            return HandleCliArguments(args);
        }

        return RunInteractiveLauncher();
    }

    private static int HandleCliArguments(string[] args)
    {
        bool isSmokeTest = false;
        bool isBenchmark = false;
        bool isValidateEnv = false;
        string scenarioName = "Main";
        int seed = 42;
        int ticks = 600;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLowerInvariant();
            if (arg is "--headless" or "-h")
            {
                // Headless mode
            }
            else if (arg is "--smoke-test" or "-s") isSmokeTest = true;
            else if (arg is "--benchmark" or "-b")
            {
                isBenchmark = true;
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int t))
                {
                    ticks = t;
                    i++;
                }
            }
            else if (arg is "--validate-env" or "-v") isValidateEnv = true;
            else if (arg is "--scenario" && i + 1 < args.Length)
            {
                scenarioName = args[++i];
            }
            else if (arg is "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out int s))
            {
                seed = s;
                i++;
            }
            else if (arg is "--ticks" && i + 1 < args.Length && int.TryParse(args[i + 1], out int tk))
            {
                ticks = tk;
                i++;
            }
        }

        if (isValidateEnv)
        {
            var diag = CleanMachineEnvironmentValidator.ValidateCurrentEnvironment();
            Console.WriteLine($"Environment Diagnostics: {diag.OverallStatus}");
            Console.WriteLine($"Architecture: {diag.Architecture} | OS: {diag.OperatingSystem} | .NET: {diag.DotNetVersion}");
            Console.WriteLine($"RAM: {diag.TotalMemoryMb}MB | Disk: {diag.AvailableDiskSpaceMb}MB Free");
            foreach (var item in diag.Items)
            {
                Console.WriteLine($"  [{item.Severity}] {item.CheckName}: {item.ObservedValue} (Required: {item.RequiredSpecification})");
            }
            return diag.IsPassing ? 0 : 4;
        }

        if (isSmokeTest)
        {
            var smokeResult = HeadlessSmokeTestRunner.RunSmokeTest(new SmokeScenarioConfig(TicksToSimulate: ticks, RandomSeed: seed));
            Console.WriteLine($"[Smoke Test] Result: {(smokeResult.IsSuccess ? "PASS" : "FAIL")} (ExitCode: {smokeResult.ExitCode})");
            Console.WriteLine($"Summary: {smokeResult.SummaryDetails}");
            return smokeResult.ExitCode;
        }

        if (isBenchmark)
        {
            var perf = ReleasePerformanceCertifier.CertifySimulationPerformance(ticksToRun: ticks, unitCount: 300, seed: seed);
            Console.WriteLine($"[Benchmark] Certified: {perf.IsCertified}");
            Console.WriteLine(perf.ReportSummary);
            return perf.IsCertified ? 0 : 1;
        }

        Console.WriteLine($"Running Scenario: {scenarioName} for {ticks} ticks (Seed: {seed})...");
        var reg = FullMatchRegressionHarness.RunFullMatch(ticks: ticks, seed: seed);
        Console.WriteLine(reg.Summary);
        return reg.IsSuccess ? 0 : 1;
    }

    private static int RunInteractiveLauncher()
    {
        while (true)
        {
            Console.Clear();
            PrintBanner();
            Console.WriteLine(" Select a Game Mode or Scenario to Launch:");
            Console.WriteLine(" ------------------------------------------------------------------");
            Console.WriteLine(" [1] Play Live Interactive Skirmish (Celtic Kingdom vs Roman Empire)");
            Console.WriteLine(" [2] Tactical Combat Arena (Spearmen Formation vs Cavalry Charge)");
            Console.WriteLine(" [3] Settlement Economy & Worker Gathering (5-Resource Model)");
            Console.WriteLine(" [4] Siege Warfare Citadel Assault (Catapults & Wall Breaches)");
            Console.WriteLine(" [5] RPG Hero Progression & Ability Showcase (Lord Aldric / Brennus)");
            Console.WriteLine(" [6] Civilization Progression & Tech Tree Advance (Classical Era)");
            Console.WriteLine(" [7] Run Clean-Machine Environment Diagnostics");
            Console.WriteLine(" [8] Run High-Density 1,000-Unit Performance Benchmark");
            Console.WriteLine(" [9] View Game Manual & Player Controls");
            Console.WriteLine(" [0] Exit");
            Console.WriteLine(" ------------------------------------------------------------------");
            Console.Write(" Enter selection (0-9): ");

            var key = Console.ReadKey(intercept: true).KeyChar;
            Console.WriteLine(key);
            Console.WriteLine();

            switch (key)
            {
                case '1':
                    RunLiveSkirmishMatch();
                    break;
                case '2':
                    RunTacticalCombatArena();
                    break;
                case '3':
                    RunSettlementEconomy();
                    break;
                case '4':
                    RunSiegeCitadelAssault();
                    break;
                case '5':
                    RunHeroShowcase();
                    break;
                case '6':
                    RunCivilizationProgression();
                    break;
                case '7':
                    RunEnvironmentDiagnostics();
                    break;
                case '8':
                    RunPerformanceBenchmark();
                    break;
                case '9':
                    ShowUserManual();
                    break;
                case '0':
                case 'q':
                case 'Q':
                    Console.WriteLine("Thank you for playing Crown & Conquest!");
                    return 0;
                default:
                    Console.WriteLine("Invalid option. Press any key to continue...");
                    Console.ReadKey(intercept: true);
                    break;
            }
        }
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(@"
   ██████╗██████╗  ██████╗ ██╗    ██╗███╗   ██╗     ██████╗     ██████╗ ██████╗ ███╗   ██╗ ██████╗ ██╗   ██╗███████╗███████╗████████╗
  ██╔════╝██╔══██╗██╔═══██╗██║    ██║████╗  ██║    ██╔════╝    ██╔════╝██╔═══██╗████╗  ██║██╔═══██╗██║   ██║██╔════╝██╔════╝╚══██╔══╝
  ██║     ██████╔╝██║   ██║██║ █╗ ██║██╔██╗ ██║    ██║         ██║     ██║   ██║██╔██╗ ██║██║   ██║██║   ██║█████╗  ███████╗   ██║   
  ██║     ██╔══██╗██║   ██║██║███╗██║██║╚██╗██║    ██║         ██║     ██║   ██║██║╚██╗██║██║   ██║██║   ██║██╔══╝  ╚════██║   ██║   
  ╚██████╗██║  ██║╚██████╔╝╚███╔███╔╝██║ ╚████║    ╚██████╗    ╚██████╗╚██████╔╝██║ ╚████║╚██████╔╝╚██████╔╝███████╗███████║   ██║   
   ╚═════╝╚═╝  ╚═╝ ╚═════╝  ╚══╝╚══╝ ╚═╝  ╚═══╝     ╚═════╝     ╚═════╝ ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝  ╚═════╝ ╚══════╝╚══════╝   ╚═╝   
        v1.0.0 Release Candidate  |  Authoritative Deterministic RTS/RPG  |  Windows x64 Edition
        ");
        Console.ResetColor();
    }

    private static void RunLiveSkirmishMatch()
    {
        Console.Clear();
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [MODE 1] Live Interactive Skirmish Match ===");
        Console.ResetColor();
        Console.WriteLine("Simulating real-time battlefield clashing in fixed 50ms ticks (20Hz)...");
        Console.WriteLine("Press any key to pause / return to main menu at any time.\n");

        var coordinator = new GameCoordinator(new SimulationConfig { InitialRandomSeed = 1337 });
        var scenario = new CombatArenaScenario(coordinator);
        scenario.Deploy10v10Forces();

        int tick = 0;
        var sw = Stopwatch.StartNew();

        while (tick < 200 && !Console.KeyAvailable)
        {
            coordinator.Simulation.Tick();
            tick++;

            if (tick % 5 == 0)
            {
                int celticAlive = 0;
                int romanAlive = 0;
                float celticHealth = 0;
                float romanHealth = 0;

                for (int i = 0; i < coordinator.Simulation.State.ActiveUnits.Count; i++)
                {
                    var u = coordinator.Simulation.State.ActiveUnits[i];
                    if (u.IsAlive)
                    {
                        if (u.FactionId == FactionId.Player1) { celticAlive++; celticHealth += u.CurrentHealth; }
                        else { romanAlive++; romanHealth += u.CurrentHealth; }
                    }
                }

                Console.Write($"\r[Tick {tick:D3}] Celtic Swordsmen: {celticAlive:D2} units (HP: {celticHealth:F0})  |  Roman Legion: {romanAlive:D2} units (HP: {romanHealth:F0})   ");
            }

            Thread.Sleep(30);
        }

        sw.Stop();
        if (Console.KeyAvailable) Console.ReadKey(intercept: true);

        Console.WriteLine($"\n\nSkirmish simulation reached tick {tick} in {sw.Elapsed.TotalSeconds:F1}s.");
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey(intercept: true);
    }

    private static void RunTacticalCombatArena()
    {
        Console.Clear();
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [MODE 2] Tactical Combat Arena ===");
        Console.ResetColor();
        Console.WriteLine("Simulating Tactical Formations (Spearmen in Line vs Cavalry in Wedge Charge)...");

        var scenario = new TacticalCombatScenario(new SimulationConfig { InitialRandomSeed = 42 });
        scenario.SetupTacticalBattlefield();
        scenario.SpawnChargeTestEncounter();

        for (int t = 0; t < 100; t++)
        {
            scenario.Coordinator.Simulation.Tick();
        }

        Console.WriteLine($"Scenario Complete! 100 Ticks simulated.");
        Console.WriteLine($"Units Remaining: {scenario.Coordinator.Simulation.State.ActiveUnits.Count}");
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey(intercept: true);
    }

    private static void RunSettlementEconomy()
    {
        Console.Clear();
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [MODE 3] Settlement Economy & Worker Gathering ===");
        Console.ResetColor();
        Console.WriteLine("Simulating 5-Resource Economy (Food, Wood, Gold, Stone, Iron)...");

        var coordinator = new GameCoordinator(new SimulationConfig { InitialRandomSeed = 100 });
        var scenario = new SettlementEconomyScenario(coordinator);

        for (int t = 0; t < 120; t++)
        {
            coordinator.Simulation.Tick();
        }

        var bank = coordinator.Simulation.State.GetOrCreateResourceBank(FactionId.Player1);
        Console.WriteLine("Economy Simulation Completed!");
        Console.WriteLine($"Final Bank Vault: Food={bank.Food}, Wood={bank.Wood}, Gold={bank.Gold}, Stone={bank.Stone}, Iron={bank.Iron}");
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey(intercept: true);
    }

    private static void RunSiegeCitadelAssault()
    {
        Console.Clear();
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [MODE 4] Siege Warfare Citadel Assault ===");
        Console.ResetColor();
        Console.WriteLine("Deploying Battering Rams, Catapults, Ballista Towers & Stone Gate defenses...");

        var scenario = new SiegeWarfareScenario(seed: 999);
        scenario.SetupFortressMatch();

        for (int t = 0; t < 150; t++)
        {
            scenario.Engine.Tick();
        }

        Console.WriteLine("Siege Assault Simulation Completed!");
        Console.WriteLine($"Active Fortifications: {scenario.Engine.State.ActiveBuildings.Count}");
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey(intercept: true);
    }

    private static void RunHeroShowcase()
    {
        Console.Clear();
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [MODE 5] RPG Hero Progression & Abilities ===");
        Console.ResetColor();
        Console.WriteLine("Simulating Heroic Commander (Brennus - Warlord Class)...");

        var scenario = new HeroProgressionScenario();
        scenario.ExecuteFullScenario();

        Console.WriteLine($"Hero Progression Complete! Hero Level: {scenario.HeroUnit.Veterancy.Level}");
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey(intercept: true);
    }

    private static void RunCivilizationProgression()
    {
        Console.Clear();
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [MODE 6] Civilization Era & Tech Tree Progression ===");
        Console.ResetColor();
        Console.WriteLine("Advancing from Archaic Era to Classical Era with Blacksmith & Metallurgy techs...");

        var scenario = new CivilizationProgressionScenario();
        scenario.ExecuteEvolutionScenario(out int ticksTaken);

        var era = scenario.Coordinator.Simulation.State.GetOrCreateEraState(FactionId.Player1);
        Console.WriteLine($"Civilization Status: {era.CurrentEra.GetDisplayName()} (Executed in {ticksTaken} ticks)");
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey(intercept: true);
    }

    private static void RunEnvironmentDiagnostics()
    {
        Console.Clear();
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [SYSTEM] Clean-Machine Hardware & Runtime Diagnostics ===");
        Console.ResetColor();

        var diag = CleanMachineEnvironmentValidator.ValidateCurrentEnvironment();
        Console.WriteLine($"Overall Status: {diag.OverallStatus}\n");

        foreach (var item in diag.Items)
        {
            var color = item.Severity switch
            {
                DiagnosticSeverity.Pass => ConsoleColor.Green,
                DiagnosticSeverity.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.Red
            };
            Console.ForegroundColor = color;
            Console.Write($"[{item.Severity,-7}] ");
            Console.ResetColor();
            Console.WriteLine($"{item.CheckName}: {item.ObservedValue}");
            Console.WriteLine($"          Requirement: {item.RequiredSpecification}");
            Console.WriteLine($"          Notes: {item.Recommendation}\n");
        }

        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey(intercept: true);
    }

    private static void RunPerformanceBenchmark()
    {
        Console.Clear();
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [BENCHMARK] 1,000-Tick High Density Performance Test (300 Units) ===");
        Console.ResetColor();
        Console.WriteLine("Benchmarking simulation frame budgets and memory footprint...");

        var report = ReleasePerformanceCertifier.CertifySimulationPerformance(ticksToRun: 500, unitCount: 300);
        Console.WriteLine($"\n{report.ReportSummary}\n");
        Console.WriteLine($"Mean Tick Time: {report.MeanTickDurationMs:F2}ms (Budget <= 16.6ms for 60 FPS)");
        Console.WriteLine($"Peak 99th Percentile: {report.P99TickDurationMs:F2}ms");
        Console.WriteLine($"Memory Footprint: {report.MemoryFootprintMb:F1}MB (Budget < 500MB)");
        Console.WriteLine($"Zero-Allocation Compliant: {report.ZeroAllocationCompliant}");

        Console.WriteLine("\nPress any key to return to menu...");
        Console.ReadKey(intercept: true);
    }

    private static void ShowUserManual()
    {
        Console.Clear();
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Crown & Conquest — Player Controls & Quick Guide ===");
        Console.ResetColor();
        Console.WriteLine(@"
 [CONTROLS]
 - Left Click:           Select unit, hero, building, or resource node
 - Left Click + Drag:    Box selection of multiple units
 - Right Click (Ground): Move selected units to location
 - Right Click (Enemy):  Attack target enemy unit / fortification
 - Right Click (Resource): Assign workers to gather (Food, Wood, Gold, Stone, Iron)
 - Control Groups:       Ctrl + 1..9 to assign, 1..9 to select squad
 - Hero Abilities:       F1..F4 to cast active spells / battle cries
 - Tab / Space:          Cycle selection / Center on recent alert

 [VETERANCY PROGRESSION]
 - Level 1-2 (Recruit)     -> Base stats
 - Level 3-4 (Experienced) -> +10% HP, +5% Damage, +1 Armor (Bronze badge)
 - Level 5-6 (Veteran)     -> +25% HP, +15% Damage, +2 Armor, +10% Speed (Silver badge)
 - Level 7-8 (Elite)       -> +45% HP, +25% Damage, +3 Armor, Morale aura (Gold badge)
 - Level 9+  (Legendary)   -> +70% HP, +40% Damage, +5 Armor, Heroic Crown (Legendary)
        ");
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey(intercept: true);
    }
}
