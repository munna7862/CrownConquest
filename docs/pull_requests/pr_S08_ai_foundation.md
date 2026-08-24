# Sprint 08 Pull Request: Autonomous AI Foundation

## Summary
Implements the comprehensive autonomous AI system for Crown & Conquest, including perception (fog-of-war memory and base threat heatmaps), combat evaluation & retreat decision logic, dynamic resource priority scoring, build order progression state machines, combined-arms army composition, squad lifecycle controllers (Assembling, Defending, Attacking, Retreating, Patrolling), and time-sliced deterministic simulation updates.

## Changes Included
- **Domain AI Subsystem (`CrownConquest.Domain.AI`):**
  - [`PerceivedEntityRecord.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/AI/PerceivedEntityRecord.cs): Immutable entity memory snapshot.
  - [`AiPerceptionState.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/AI/AiPerceptionState.cs): Vision scanning, base defense threat detection, and resource node memory.
  - [`AiCombatEvaluator.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/AI/AiCombatEvaluator.cs): Unit combat power calculation, squad strength ratio comparison, and tactical retreat evaluator ($R_{combat} < 0.45$).
  - [`AiTargetingMatrix.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/AI/AiTargetingMatrix.cs): Counter-archetype prioritization and strategic structure targeting score matrix.
  - [`AiResourcePriority.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/AI/AiResourcePriority.cs): Adaptive dynamic weights based on stockpile shortages and current needs.
  - [`AiBuildOrderPlan.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/AI/AiBuildOrderPlan.cs): Step progression state machine with standard early-game build template.
  - [`AiArmySquad.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/AI/AiArmySquad.cs): State machine managing army lifecycle, rallying, and engagement.
  - [`AiFactionController.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/AI/AiFactionController.cs): Autonomous controller with time-sliced deterministic loops for workers, army, building placement, and production queues.
- **Simulation Engine Integration:**
  - Added controller registration and time-sliced tick scheduling `UpdateAi(tick)` in [`SimulationEngine.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/Simulation/SimulationEngine.cs).
- **Presentation Layer:**
  - Added [`AiFoundationPresenter.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Presentation/AiFoundationPresenter.cs) and [`AiFoundationScenario.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Presentation/AiFoundationScenario.cs) for bot vs bot headless demonstration and telemetry tracking.
- **Test Automation:**
  - Added 18 comprehensive tests across Tiers 1-4 (`AiMathAndStateTests.cs`, `AiInvariantTests.cs`, `AiIntegrationTests.cs`, `AiScenarioAndReplayTests.cs`).
  - Total test suite: 174 tests passing (100% green).

## Verification Results
- `dotnet build`: 0 Warnings, 0 Errors
- `dotnet test`: 174 Passed, 0 Failed, 0 Skipped
- Replay Determinism: 1,000-tick bit-exact checksum equality verified.
